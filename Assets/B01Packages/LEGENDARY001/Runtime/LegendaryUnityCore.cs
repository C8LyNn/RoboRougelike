// B01-22 LEGENDARY-UNITY-001 | Unity 6000.3 package, source-only, no R3 storage duplication.
// Important: Unity host MUST implement IAuthoritativeR3Port over R3VehicleModel.Parts/Inventory.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoboRougelike.B01.Legendary
{
    [Serializable] public sealed class LegendaryFootprint
    {
        public int Width, Height, Depth;
        public LegendaryFootprint(int w, int h, int d) { Width=w; Height=h; Depth=d; }
    }
    [Serializable] public sealed class LegendaryMassCalibration
    {
        public float[] TestGridKg;
        public float LowerBoundKg, UpperBoundKg;
        public bool FinalMassApproved;
        public LegendaryMassCalibration(float[] grid, float min, float max)
        { TestGridKg=grid; LowerBoundKg=min; UpperBoundKg=max; FinalMassApproved=false; }
    }
    [Serializable] public sealed class LegendaryDefinition
    {
        public string Id, DisplayName, BasePartKind, WeaponFamily, MountSocketKind, AbilityId, WeakSide;
        public LegendaryFootprint Footprint;
        public LegendaryMassCalibration Mass;
        public bool AbilityImplemented;
        public LegendaryDefinition(string id,string name,string kind,string family,string socket,
            LegendaryFootprint footprint,LegendaryMassCalibration mass,string ability,string weakness)
        { Id=id; DisplayName=name; BasePartKind=kind; WeaponFamily=family; MountSocketKind=socket;
          Footprint=footprint; Mass=mass; AbilityId=ability; WeakSide=weakness; AbilityImplemented=false; }
    }
    public static class LegendaryCatalog
    {
        public static LegendaryDefinition[] CreatePilotDefinitions() => new[] {
            new LegendaryDefinition("LEG-WPN-001","Przebijacz","Gun","Railgun","WeaponBase",
                new LegendaryFootprint(3,2,2),new LegendaryMassCalibration(new[]{220f,435f,650f},180f,700f),
                "PENETRATING_RAIL_SHOT","rear-breech"),
            new LegendaryDefinition("LEG-MOV-001","Serce Zawisu","HoverEngine",null,"MobilityHub",
                new LegendaryFootprint(3,3,2),new LegendaryMassCalibration(new[]{150f,465f,780f},120f,850f),
                "HOVER_LATERAL_BURST","underside-emitter")
        };
        public static bool Validate(LegendaryDefinition[] items,out string reason)
        {
            if(items==null||items.Length==0) { reason="CATALOG_EMPTY"; return false; }
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var d in items)
            {
                if(d==null||string.IsNullOrEmpty(d.Id)||!System.Text.RegularExpressions.Regex.IsMatch(
                       d.Id,@"^LEG-(WPN|MOV|DEF|SUP|DEP|ECO)-[0-9]{3}$")||
                   !ids.Add(d.Id)||string.IsNullOrEmpty(d.DisplayName)||
                   (d.BasePartKind!="Gun"&&d.BasePartKind!="HoverEngine")||
                   ((d.BasePartKind=="Gun") != !string.IsNullOrEmpty(d.WeaponFamily))||
                   ((d.BasePartKind=="Gun"&&d.MountSocketKind!="WeaponBase")||
                    (d.BasePartKind=="HoverEngine"&&d.MountSocketKind!="MobilityHub"))||
                   d.Footprint==null||d.Footprint.Width<=0||d.Footprint.Height<=0||d.Footprint.Depth<=0||
                   d.Mass==null||d.Mass.FinalMassApproved||d.Mass.TestGridKg==null||
                   d.Mass.TestGridKg.Length<3||d.Mass.LowerBoundKg<=0||
                   d.Mass.UpperBoundKg<=d.Mass.LowerBoundKg||
                   d.Mass.TestGridKg.Any(m=>float.IsNaN(m)||m<d.Mass.LowerBoundKg||m>d.Mass.UpperBoundKg)||
                   d.AbilityImplemented)
                { reason="CATALOG_INVALID_OR_DUPLICATE";return false; }
            }
            reason="OK";return true;
        }
    }

    // Value-only observation of a REAL owned R3PartState. No ownership or inventory lives here.
    [Serializable] public sealed class PartObservation
    {
        public string InstanceId, BasePartKind, WeaponFamily;
        public float Health;
        public PartObservation(string id,string kind,string family,float hp)
        { InstanceId=id; BasePartKind=kind; WeaponFamily=family; Health=hp; }
    }
    public struct LegendaryPlacement
    {
        public int X,Y,Z,RotX,RotY,RotZ;
        public string SocketToken;
        public LegendaryPlacement(int x,int y,int z,string socketToken)
        { X=x;Y=y;Z=z;RotX=0;RotY=0;RotZ=0;SocketToken=socketToken; }
    }

    // ALL state mutations go to the game's authoritative R3 model.
    // A port must refuse operations when it cannot ensure atomicity and preservation of InstanceId.
    public interface IAuthoritativeR3Port
    {
        string RobotId { get; }
        IReadOnlyList<PartObservation> Mounted { get; }  // R3VehicleModel.Parts
        IReadOnlyList<PartObservation> Inventory { get; } // R3VehicleModel.Inventory
        bool HasCertifiedSocket(string socketKind, string socketToken, LegendaryPlacement position);
        bool CanMountExisting(string instanceId, LegendaryPlacement position, out string error);
        bool MountExisting(string instanceId, LegendaryPlacement position, out string error);
        bool CanUnmountSafely(string instanceId, out string error);
        bool UnmountExisting(string instanceId, out string error);
        bool RepairExisting(string instanceId, out string error);
        bool ReceiveSalvage(PartObservation reward, out string error);
        // Optional host transaction; return false without changing either vehicle if unavailable.
        bool TransferInventoryAtomically(IAuthoritativeR3Port target,string instanceId,out string error);
    }

    [Serializable] public sealed class LegendaryBinding
    {
        public string InstanceId, DefinitionId;
        public LegendaryBinding(string id,string definition) { InstanceId=id; DefinitionId=definition; }
    }
    [Serializable] public sealed class LegendaryPersistence
    {
        public int Version=1;
        public LegendaryBinding[] Bindings;
        public string[] RewardEventIds;
        public LegendaryPersistence() {}
    }

    public sealed class LegendaryUnityService
    {
        private readonly object gate=new object();
        private readonly IAuthoritativeR3Port[] hosts;
        private readonly Dictionary<string,LegendaryDefinition> catalog;
        // Not inventory! Metadata only for physical IDs present in authoritative R3.
        private Dictionary<string,string> bindings=new Dictionary<string,string>(StringComparer.Ordinal);
        private HashSet<string> rewards=new HashSet<string>(StringComparer.Ordinal);

        public LegendaryUnityService(IEnumerable<IAuthoritativeR3Port> ports, LegendaryDefinition[] definitions)
        {
            if(!LegendaryCatalog.Validate(definitions,out var error))throw new ArgumentException(error);
            hosts=ports==null?throw new ArgumentNullException(nameof(ports)):ports.ToArray();
            if(hosts.Length==0||hosts.Any(p=>p==null||string.IsNullOrEmpty(p.RobotId))||
               hosts.Select(p=>p.RobotId).Distinct(StringComparer.Ordinal).Count()!=hosts.Length)
                throw new ArgumentException("DUPLICATE_OR_MISSING_ROBOT");
            catalog=definitions.ToDictionary(d=>d.Id,d=>d,StringComparer.Ordinal);
        }

        private bool Observe(out Dictionary<string,PartObservation> owned,out string reason)
        {
            owned=new Dictionary<string,PartObservation>(StringComparer.Ordinal);
            foreach(var port in hosts)
            {
                if(port.Mounted==null||port.Inventory==null){reason="INVALID_R3_VIEW";return false;}
                foreach(var part in port.Mounted.Concat(port.Inventory))
                {
                    if(part==null||!Guid.TryParseExact(part.InstanceId,"N",out _)||
                       float.IsNaN(part.Health)||part.Health<0||part.Health>1||
                       string.IsNullOrEmpty(part.BasePartKind)||
                       (part.BasePartKind=="Gun"&&string.IsNullOrEmpty(part.WeaponFamily))||
                       !owned.TryAdd(part.InstanceId,part))
                    {reason="DUPLICATE_INSTANCE_OR_INVALID_R3_VIEW";return false;}
                }
            }
            reason="OK";return true;
        }
        private IAuthoritativeR3Port GetPort(string id) =>
            hosts.FirstOrDefault(p=>p.RobotId==id);
        private bool ValidBindings(Dictionary<string,string> items, Dictionary<string,PartObservation> owned,
            out string reason)
        {
            foreach(var pair in items)
            {
                if(!catalog.TryGetValue(pair.Value,out var def)||!owned.TryGetValue(pair.Key,out var physical)||
                   def.BasePartKind!=physical.BasePartKind||
                   (def.BasePartKind=="Gun"&&def.WeaponFamily!=physical.WeaponFamily))
                {reason="MISSING_OR_MISMATCHED_PHYSICAL_INSTANCE";return false;}
            }
            foreach(var port in hosts)
            {
                var kinds=new HashSet<string>(StringComparer.Ordinal);
                var families=new HashSet<string>(StringComparer.Ordinal);
                foreach(var p in port.Mounted)
                {
                    if(p.BasePartKind=="Gun") families.Add(p.WeaponFamily);
                    if(items.TryGetValue(p.InstanceId,out var definition)&&!kinds.Add(definition))
                    {reason="LEGEND_ALREADY_EQUIPPED";return false;}
                }
                if(families.Count>2){reason="WEAPON_FAMILY_3";return false;}
            }
            reason="OK";return true;
        }
        private bool EnsureWorld(out Dictionary<string,PartObservation> snapshot,out string error)
        {
            if(!Observe(out snapshot,out error))return false;
            return ValidBindings(bindings,snapshot,out error);
        }
        private static bool Matches(PartObservation part,LegendaryDefinition def) =>
            part.BasePartKind==def.BasePartKind &&
            (def.BasePartKind!="Gun"||part.WeaponFamily==def.WeaponFamily);

        public bool TryTagExistingInventory(string robotId,string instanceId,string definitionId,out string error)
        {
            lock(gate)
            {
                var port=GetPort(robotId);
                if(port==null||!catalog.TryGetValue(definitionId??"",out var def))
                {error="UNKNOWN_ROBOT_OR_DEFINITION";return false;}
                if(!EnsureWorld(out var world,out error))return false;
                var part=port.Inventory.FirstOrDefault(p=>p.InstanceId==instanceId);
                if(part==null||!world.ContainsKey(instanceId)||!Matches(part,def)||bindings.ContainsKey(instanceId))
                {error="UNOWNED_MISMATCHED_OR_ALREADY_BOUND";return false;}
                bindings.Add(instanceId,definitionId);error="OK";return true;
            }
        }

        public bool TryGrant(string robotId,string rewardEventId,PartObservation part,string definitionId,out string error)
        {
            lock(gate)
            {
                var port=GetPort(robotId);
                if(port==null||part==null||string.IsNullOrWhiteSpace(rewardEventId)||
                   !catalog.TryGetValue(definitionId??"",out var def))
                {error="INVALID_REWARD";return false;}
                if(rewards.Contains(rewardEventId)){error="REWARD_ALREADY_APPLIED";return false;}
                if(!EnsureWorld(out var world,out error))return false;
                if(!Guid.TryParseExact(part.InstanceId,"N",out _)||world.ContainsKey(part.InstanceId)||
                   bindings.ContainsKey(part.InstanceId))
                {error="DUPLICATE_INSTANCE";return false;}
                if(!Matches(part,def)||part.Health<=0||part.Health>1)
                {error="INVALID_REWARD_PART";return false;}
                if(!port.ReceiveSalvage(part,out error))return false; // R3 is authoritative.
                if(port.Inventory.Count(p=>p.InstanceId==part.InstanceId)!=1)
                {error="HOST_CONTRACT_BROKEN";return false;} // block further use until repaired
                bindings.Add(part.InstanceId,definitionId);
                rewards.Add(rewardEventId);
                error="OK";return true;
            }
        }

        public bool CanEquip(string robotId,string instanceId,LegendaryPlacement position,out string error)
        {
            lock(gate)
            {
                var port=GetPort(robotId);
                if(port==null){error="UNKNOWN_ROBOT";return false;}
                if(!EnsureWorld(out _,out error))return false;
                if(!bindings.TryGetValue(instanceId??"",out var definitionId)||
                   !catalog.TryGetValue(definitionId,out var def))
                {error="LEGEND_NOT_REGISTERED";return false;}
                var part=port.Inventory.FirstOrDefault(p=>p.InstanceId==instanceId);
                if(part==null){error="PART_NOT_IN_INVENTORY";return false;}
                if(part.Health<=0){error="DESTROYED_ITEM";return false;}
                if(port.Mounted.Any(p=>bindings.TryGetValue(p.InstanceId,out var d)&&d==definitionId))
                {error="LEGEND_ALREADY_EQUIPPED";return false;}
                if(def.BasePartKind=="Gun")
                {
                    var families=new HashSet<string>(
                        port.Mounted.Where(p=>p.BasePartKind=="Gun").Select(p=>p.WeaponFamily),
                        StringComparer.Ordinal);
                    families.Add(def.WeaponFamily);
                    if(families.Count>2){error="WEAPON_FAMILY_3";return false;}
                }
                if(string.IsNullOrWhiteSpace(position.SocketToken)||
                   !port.HasCertifiedSocket(def.MountSocketKind,position.SocketToken,position))
                {error="MOUNT_ADAPTER_PENDING_OR_SOCKET_MISMATCH";return false;}
                if(!port.CanMountExisting(instanceId,position,out error))return false;
                error="OK";return true;
            }
        }

        public bool TryEquip(string robotId,string instanceId,LegendaryPlacement position,out string error)
        {
            lock(gate)
            {
                if(!CanEquip(robotId,instanceId,position,out error))return false;
                var port=GetPort(robotId);
                if(!port.MountExisting(instanceId,position,out error))return false;
                if(port.Mounted.Count(p=>p.InstanceId==instanceId)!=1 ||
                   port.Inventory.Any(p=>p.InstanceId==instanceId))
                {error="HOST_CONTRACT_BROKEN";return false;}
                error="OK";return true;
            }
        }

        public bool TryUnequip(string robotId,string instanceId,out string error)
        {
            lock(gate)
            {
                var port=GetPort(robotId);
                if(port==null||!bindings.ContainsKey(instanceId??""))
                {error="UNKNOWN_ROBOT_OR_LEGEND";return false;}
                if(!EnsureWorld(out _,out error))return false;
                if(!port.Mounted.Any(p=>p.InstanceId==instanceId))
                {error="PART_NOT_MOUNTED";return false;}
                if(!port.CanUnmountSafely(instanceId,out error))return false;
                if(!port.UnmountExisting(instanceId,out error))return false;
                if(port.Inventory.Count(p=>p.InstanceId==instanceId)!=1 ||
                    port.Mounted.Any(p=>p.InstanceId==instanceId))
                {error="HOST_CONTRACT_BROKEN";return false;}
                error="OK";return true;
            }
        }
        public bool TryRepair(string robotId,string instanceId,out string error)
        {
            lock(gate)
            {
                var port=GetPort(robotId);
                if(port==null||!bindings.ContainsKey(instanceId??""))
                {error="UNKNOWN_ROBOT_OR_LEGEND";return false;}
                if(!EnsureWorld(out _,out error))return false;
                if(!port.Mounted.Concat(port.Inventory).Any(p=>p.InstanceId==instanceId))
                {error="PART_NOT_OWNED";return false;}
                if(!port.RepairExisting(instanceId,out error))return false;
                if(!port.Mounted.Concat(port.Inventory).Any(p=>p.InstanceId==instanceId&&p.Health>0))
                {error="HOST_CONTRACT_BROKEN";return false;}
                error="OK";return true;
            }
        }
        public bool TryTransfer(string fromRobot,string toRobot,string instanceId,out string error)
        {
            lock(gate)
            {
                var a=GetPort(fromRobot);var b=GetPort(toRobot);
                if(a==null||b==null||a==b||!bindings.ContainsKey(instanceId??""))
                {error="INVALID_TRANSFER";return false;}
                if(!EnsureWorld(out _,out error))return false;
                if(!a.Inventory.Any(p=>p.InstanceId==instanceId))
                {error="PART_NOT_IN_INVENTORY";return false;}
                if(!a.TransferInventoryAtomically(b,instanceId,out error))
                    return false; // host must not mutate on rejected operation
                if(a.Inventory.Any(p=>p.InstanceId==instanceId)||
                   b.Inventory.Count(p=>p.InstanceId==instanceId)!=1)
                {error="HOST_CONTRACT_BROKEN";return false;}
                error="OK";return true;
            }
        }

        public bool TrySaveMetadata(out string json,out string error)
        {
            lock(gate)
            {
                json=null;
                if(!EnsureWorld(out _,out error))return false;
                var data=new LegendaryPersistence{
                    Bindings=bindings.OrderBy(p=>p.Key,StringComparer.Ordinal)
                        .Select(p=>new LegendaryBinding(p.Key,p.Value)).ToArray(),
                    RewardEventIds=rewards.OrderBy(x=>x,StringComparer.Ordinal).ToArray()
                };
                json=JsonUtility.ToJson(data,true);
                error="OK";return true;
            }
        }
        public bool TryRestoreMetadata(string json,out string error)
        {
            lock(gate)
            {
                LegendaryPersistence data;
                try {data=JsonUtility.FromJson<LegendaryPersistence>(json);}
                catch(Exception){error="CORRUPT_SAVE";return false;}
                if(data==null||data.Version!=1||data.Bindings==null||data.RewardEventIds==null)
                {error="CORRUPT_SAVE";return false;}
                var next=new Dictionary<string,string>(StringComparer.Ordinal);
                var events=new HashSet<string>(StringComparer.Ordinal);
                foreach(var item in data.Bindings)
                {
                    if(item==null||!Guid.TryParseExact(item.InstanceId,"N",out _)||
                       string.IsNullOrEmpty(item.DefinitionId)||
                       !next.TryAdd(item.InstanceId,item.DefinitionId))
                    {error="CORRUPT_OR_DUPLICATE_INSTANCE";return false;}
                }
                foreach(var ev in data.RewardEventIds)
                    if(string.IsNullOrWhiteSpace(ev)||!events.Add(ev))
                    {error="CORRUPT_OR_DUPLICATE_EVENT";return false;}
                if(!Observe(out var world,out error))return false;
                if(!ValidBindings(next,world,out error))return false;
                bindings=next;rewards=events;error="OK";return true;
            }
        }
        public bool TryGetDefinition(string physicalId,out LegendaryDefinition definition)
        {
            lock(gate)
            {
                definition=null;
                return physicalId!=null && bindings.TryGetValue(physicalId,out var id) &&
                       catalog.TryGetValue(id,out definition);
            }
        }
        public bool CheckInvariants(out string error)
        {
            lock(gate) return EnsureWorld(out _,out error);
        }
    }
}

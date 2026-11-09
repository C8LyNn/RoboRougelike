using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R4;
using RoboRougelike.Mechanics.MOV009;

namespace RoboRougelike.R3
{
    // Traverses part identities through all their occupied grid cells. The
    // footprint override is also useful for validating future catalog shapes;
    // it does not enable any unreleased shape in gameplay.
    public static class R3ConnectionTraversal
    {
        public static HashSet<string> Reachable(IEnumerable<R3PartState> parts,IEnumerable<string> initial,
            bool deadOnly,Func<R3PartState,IEnumerable<Vector3Int>> footprint=null)
        {
            footprint??=R3VehicleModel.OccupiedCells;
            var items=parts.ToArray();
            var cells=items.SelectMany(p=>footprint(p).Select(cell=>new{cell,part=p}))
                .ToDictionary(item=>item.cell,item=>item.part);
            var reached=new HashSet<string>(initial,StringComparer.Ordinal);
            var queue=new Queue<R3PartState>(items.Where(p=>reached.Contains(p.Id)));
            while(queue.Count>0)
            {
                var current=queue.Dequeue();
                foreach(var cell in footprint(current))foreach(var direction in R3MountingGrid.FaceDirections)
                    if(cells.TryGetValue(cell+direction,out var next)&&!reached.Contains(next.Id)&&
                        (!deadOnly||next.Health<=0)&&R3MountingGrid.Compatible(current,next,direction))
                    {reached.Add(next.Id);queue.Enqueue(next);}
            }
            return reached;
        }
    }

    [Serializable] public sealed class R3WheelCheckpoint
    {public string id;public Vector3Int cell,rotation;public int contacts;}
    [Serializable] public sealed class R3GroupCheckpoint
    {public string id;public bool secondary;}
    [Serializable] public sealed class R3ModelCheckpoint
    {
        public R3PartState[] mounted,inventory;
        public R3WheelCheckpoint[] wheels;
        public R3GroupCheckpoint[] groups;
        public int scrap;
        public bool suspensionEnabled,gyroEnabled,hasA,hasB;
        public int groupA,groupB;
        public ActiveSuspensionSupportMode suspensionMode;
        public float suspensionHeight,boosterThrust,boosterDuration,hoverHeight,grappleRemaining;
        // Version 0 checkpoints predate energy and start with the core's charge.
        public int energyVersion;
        public float energy;
        // Old saves lack this marker and keep their historical no-refill behavior.
        public bool hasInitialEnergyPrepared,initialEnergyPrepared;
        public R3DriveKind drive;
    }
    public sealed partial class R3VehicleModel
    {
        // This is an in-memory load notice, not an additional save format or writer.
        public int LegacyPartsMovedToInventory { get; private set; }
        public int LegacyPortMasksNormalized { get; private set; }
        public int LegacyPortPartsMovedToInventory { get; private set; }

        static bool LegacyWidePart(R3PartKind kind) => kind==R3PartKind.BonusModule||
            kind==R3PartKind.DirectionalShield||kind==R3PartKind.PointDefense||
            kind==R3PartKind.StructuralScanner||kind==R3PartKind.ScannerPulse||
            kind==R3PartKind.IndependentSteering;

        static IEnumerable<Vector3Int> LegacyCells(R3PartState part)
        {
            yield return part.Cell;
            if(!LegacyWidePart(part.Kind))yield break;
            yield return part.Cell+R3MountingGrid.Rotate(Vector3Int.right,part.RotationQuarterTurns);
            yield return part.Cell+R3MountingGrid.Rotate(Vector3Int.forward,part.RotationQuarterTurns);
            yield return part.Cell+R3MountingGrid.Rotate(Vector3Int.right+Vector3Int.forward,part.RotationQuarterTurns);
        }

        // Reproduce the former grid/socket validation before attempting migration.
        // A disconnected or overlapping legacy construction is never repaired implicitly.
        static bool LegacyConnected(IEnumerable<R3PartState> parts)
        {
            var cells=new Dictionary<Vector3Int,R3PartState>();
            foreach(var part in parts)foreach(var cell in LegacyCells(part))
                if(!cells.TryAdd(cell,part))return false;
            var core=cells.Values.FirstOrDefault(p=>p.Kind==R3PartKind.Core);
            if(core==null)return false;
            var visited=new HashSet<Vector3Int>{core.Cell};var queue=new Queue<Vector3Int>();queue.Enqueue(core.Cell);
            while(queue.Count>0)
            {
                var current=queue.Dequeue();
                foreach(var direction in R3MountingGrid.FaceDirections)
                {
                    var next=current+direction;
                    if(cells.TryGetValue(next,out var other)&&!visited.Contains(next)&&
                        R3MountingGrid.Compatible(cells[current],other,direction))
                    {visited.Add(next);queue.Enqueue(next);}
                }
            }
            return visited.Count==cells.Count;
        }

        static HashSet<string> ReachableCheckpointParts(R3PartState[] parts,HashSet<string> initial,bool deadOnly)
            =>R3ConnectionTraversal.Reachable(parts,initial,deadOnly);
        static float Remaining(float at,float now)=>at>now?at-now:0;
        static R3PartState SavePart(R3PartState source,float now)
        {
            var copy=source.Copy();copy.DeviceReadyAt=Remaining(copy.DeviceReadyAt,now);
            // A positive deadline is also the marker that a reload is pending.
            // An inventory gun may expire while it has no active binding to
            // finish that reload. Preserve the marker until remount refreshes it.
            copy.ReloadReadyAt=copy.ReloadReadyAt>0?Mathf.Max(.0001f,copy.ReloadReadyAt-now):0;
            if(float.IsPositiveInfinity(copy.DefenseRechargeAge))copy.DefenseRechargeAge=float.MaxValue;
            return copy;
        }
        public R3ModelCheckpoint MakeCheckpoint(float now)
        {
            SyncWeaponGroups();
            return new R3ModelCheckpoint {
                mounted=Parts.Select(p=>SavePart(p,now)).ToArray(),inventory=inventory.Select(p=>SavePart(p,now)).ToArray(),
                wheels=factoryWheels.Select(p=>new R3WheelCheckpoint{id=p.Key,cell=p.Value.Cell,rotation=p.Value.Rotation,contacts=p.Value.Contacts}).ToArray(),
                groups=weaponGroupPreference.Select(p=>new R3GroupCheckpoint{id=p.Key,secondary=p.Value}).ToArray(),scrap=Scrap,
                energyVersion=1,energy=Energy.Current,hasInitialEnergyPrepared=true,initialEnergyPrepared=InitialEnergyPrepared,
                suspensionEnabled=SuspensionEnabled,suspensionMode=SuspensionMode,suspensionHeight=SuspensionHeightOffset,
                gyroEnabled=GyroEnabled,drive=SelectedDrive,boosterThrust=BoosterThrustNewtons,boosterDuration=BoosterDurationSeconds,
                hoverHeight=HoverHeightMeters,grappleRemaining=Remaining(GrappleReadyAt,now),
                hasA=weaponGroupA.HasValue,groupA=(int)(weaponGroupA??0),hasB=weaponGroupB.HasValue,groupB=(int)(weaponGroupB??0)
            };
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static bool ValidCheckpointPart(R3PartState p)
        {
            return p!=null&&!string.IsNullOrWhiteSpace(p.Id)&&p.Id.Length<=128&&ValidEconomicKind(p.Kind,p.Weapon)&&ValidWheelStyle(p)&&ValidArmorShape(p)&&
                p.Tier>=0&&p.Tier<=3&&(p.Tier<=1||SupportsFusion(p.Kind,p.Weapon))&&
                Finite(p.Health)&&p.Health>=0&&p.Health<=1&&Finite(p.DeviceReadyAt)&&p.DeviceReadyAt>=0&&
                Finite(p.ReloadReadyAt)&&p.ReloadReadyAt>=0&&Finite(p.DisruptionRemaining)&&p.DisruptionRemaining>=0&&
                Finite(p.ThermalHeat)&&p.ThermalHeat>=0&&Finite(p.DefenseCharge)&&Finite(p.DefenseRechargeAge)&&
                Enum.IsDefined(typeof(RoboRougelike.Mechanics.IndependentSteering.SteeringMode),p.SteeringMode)&&
                p.RotationQuarterTurns.x>=0&&p.RotationQuarterTurns.x<4&&p.RotationQuarterTurns.y>=0&&p.RotationQuarterTurns.y<4&&
                p.RotationQuarterTurns.z>=0&&p.RotationQuarterTurns.z<4&&p.ContactMask>=0&&p.ContactMask<=63&&
                Math.Abs((long)p.Cell.x)<=100&&Math.Abs((long)p.Cell.y)<=100&&Math.Abs((long)p.Cell.z)<=100&&
                (p.Kind!=R3PartKind.BonusModule||(!string.IsNullOrWhiteSpace(p.UpgradeId)&&
                  Enum.IsDefined(typeof(RoboRougelike.R1.R1UpgradeEffect),p.UpgradeEffect)&&Finite(p.UpgradeAmount)&&p.UpgradeAmount>=0&&p.UpgradeAmount<=1));
        }
        public static bool TryRestoreCheckpoint(R3ModelCheckpoint data,R3VehicleKind kind,R3GarageEconomy economy,float now,out R3VehicleModel model,out string error)
        {
            model=null;error="Niepoprawny zapis konstrukcji.";
            if(data==null||!Enum.IsDefined(typeof(R3VehicleKind),kind)||data.mounted==null||data.inventory==null||
                data.wheels==null||data.groups==null||data.mounted.Length>2000||data.inventory.Length>InventoryCapacity||
                data.wheels.Length>2000||data.groups.Length>4000||data.scrap<0||data.scrap>100000||
                !Enum.IsDefined(typeof(R3DriveKind),data.drive)||!Enum.IsDefined(typeof(ActiveSuspensionSupportMode),data.suspensionMode)||
                !Finite(data.suspensionHeight)||data.suspensionHeight<-.35f||data.suspensionHeight>.35f||
                !Finite(data.boosterThrust)||data.boosterThrust<500||data.boosterThrust>6000||
                !Finite(data.boosterDuration)||data.boosterDuration<.2f||data.boosterDuration>2||
                !Finite(data.hoverHeight)||data.hoverHeight<.7f||data.hoverHeight>1.6f||
                !Finite(data.grappleRemaining)||data.grappleRemaining<0)return false;
            var all=data.mounted.Concat(data.inventory).ToArray();
            if(all.Any(p=>!ValidCheckpointPart(p))||all.Select(p=>p.Id).Distinct(StringComparer.Ordinal).Count()!=all.Length||
                data.mounted.Count(p=>p.Kind==R3PartKind.Core)!=1||data.inventory.Any(p=>p.Kind==R3PartKind.Core)||
                data.mounted.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Weapon).Distinct().Count()>2||
                data.mounted.SelectMany(OccupiedCells).GroupBy(c=>c).Any(g=>g.Count()>1))return false;
            var mounted=data.mounted;var inventory=data.inventory;int migratedCount=0;
            if(!Connected(mounted)||!Connected(mounted.Where(p=>p.Health>0)))
            {
                // Only a valid pre-energy checkpoint can use the former footprint.
                if(data.energyVersion!=0||!LegacyConnected(mounted)||!LegacyConnected(mounted.Where(p=>p.Health>0)))return false;
                var alive=mounted.Where(p=>p.Health>0).ToArray();
                var core=alive.Single(p=>p.Kind==R3PartKind.Core);
                var reachable=ReachableCheckpointParts(alive,new HashSet<string>{core.Id},false);
                reachable=ReachableCheckpointParts(mounted,reachable,true);
                var detached=mounted.Where(p=>!reachable.Contains(p.Id)).ToArray();
                if(inventory.Length+detached.Length>InventoryCapacity)
                {error="Stary zapis wymaga przeniesienia części do magazynu, ale brakuje w nim miejsca. Zapis pozostaje bez zmian.";return false;}
                mounted=mounted.Where(p=>reachable.Contains(p.Id)).ToArray();
                inventory=inventory.Concat(detached).ToArray();migratedCount=detached.Length;
                if(!Connected(mounted)||!Connected(mounted.Where(p=>p.Health>0)))return false;
            }
            // Old checkpoints could give wheels and guns six logical contacts,
            // including faces where their actual mounting socket does not exist.
            // Normalize copies only; if a formerly legal side-mounted assembly
            // loses support, preserve its parts and HP in inventory instead.
            var portNormalizedIds=new HashSet<string>(StringComparer.Ordinal);
            var normalized=mounted.Select(p=>p.Copy()).ToArray();
            foreach(var part in normalized.Where(p=>p.Kind==R3PartKind.Gun||p.Kind==R3PartKind.Wheel))
            {
                int actual=PlacementContacts(part.Kind,part);
                if(part.ContactMask!=actual){part.ContactMask=actual;portNormalizedIds.Add(part.Id);}
            }
            int portMovedCount=0;
            if(portNormalizedIds.Count>0)
            {
                if(!Connected(normalized)||!Connected(normalized.Where(p=>p.Health>0)))
                {
                    var alive=normalized.Where(p=>p.Health>0).ToArray();
                    var core=alive.SingleOrDefault(p=>p.Kind==R3PartKind.Core);
                    if(core==null)return false;
                    var reachable=ReachableCheckpointParts(alive,new HashSet<string>{core.Id},false);
                    reachable=ReachableCheckpointParts(normalized,reachable,true);
                    var detached=normalized.Where(p=>!reachable.Contains(p.Id)).ToArray();
                    if(inventory.Length+detached.Length>InventoryCapacity)
                    {error="Nie można dopasować starych punktów mocowania: magazyn jest pełny. Zapis pozostaje bez zmian.";return false;}
                    normalized=normalized.Where(p=>reachable.Contains(p.Id)).ToArray();
                    inventory=inventory.Concat(detached).ToArray();portMovedCount=detached.Length;
                    if(!Connected(normalized)||!Connected(normalized.Where(p=>p.Health>0)))return false;
                }
                mounted=normalized;
            }
            if(data.wheels.Any(p=>p==null||string.IsNullOrWhiteSpace(p.id)||p.contacts<0||p.contacts>63)||
                data.wheels.Select(p=>p.id).Distinct(StringComparer.Ordinal).Count()!=data.wheels.Length||
                data.groups.Any(p=>p==null||string.IsNullOrWhiteSpace(p.id))||
                data.groups.Select(p=>p.id).Distinct(StringComparer.Ordinal).Count()!=data.groups.Length||
                (data.hasA&&!Enum.IsDefined(typeof(R4WeaponKind),data.groupA))||(data.hasB&&!Enum.IsDefined(typeof(R4WeaponKind),data.groupB))||
                (data.hasA&&data.hasB&&data.groupA==data.groupB))return false;
            var restored=new R3VehicleModel(R3VehicleCatalog.Create(kind),economy);
            restored.Parts.Clear();restored.inventory.Clear();
            foreach(var source in mounted){var p=source.Copy();p.Tier=NormalizedTier(p.Tier);p.DeviceReadyAt=p.DeviceReadyAt>0?now+p.DeviceReadyAt:0;p.ReloadReadyAt=p.ReloadReadyAt>0?now+p.ReloadReadyAt:0;restored.Parts.Add(p);}
            foreach(var source in inventory){var p=source.Copy();p.ContactMask=StoredWheelContacts(p);p.Tier=NormalizedTier(p.Tier);p.DeviceReadyAt=p.DeviceReadyAt>0?now+p.DeviceReadyAt:0;p.ReloadReadyAt=p.ReloadReadyAt>0?now+p.ReloadReadyAt:0;restored.inventory.Add(p);}
            restored.LegacyPartsMovedToInventory=migratedCount;
            restored.LegacyPortMasksNormalized=portNormalizedIds.Count;
            restored.LegacyPortPartsMovedToInventory=portMovedCount;
            restored.factoryWheels.Clear();foreach(var wheel in data.wheels)
            {
                int contacts=wheel.contacts;
                var normalizedWheel=mounted.Concat(inventory).FirstOrDefault(p=>p.Id==wheel.id&&p.Kind==R3PartKind.Wheel);
                if(normalizedWheel!=null&&portNormalizedIds.Contains(wheel.id))contacts=PlacementContacts(R3PartKind.Wheel,normalizedWheel);
                restored.factoryWheels.Add(wheel.id,new FactoryWheelPose(new R3PartState(wheel.cell,R3PartKind.Wheel){RotationQuarterTurns=wheel.rotation,ContactMask=contacts}));
            }
            restored.weaponGroupPreference.Clear();foreach(var group in data.groups)restored.weaponGroupPreference.Add(group.id,group.secondary);
            restored.weaponGroupA=data.hasA?(R4WeaponKind?)data.groupA:null;restored.weaponGroupB=data.hasB?(R4WeaponKind?)data.groupB:null;
            restored.Scrap=data.scrap;restored.SelectedDrive=data.drive;restored.SuspensionEnabled=data.suspensionEnabled;
            restored.SuspensionMode=data.suspensionMode;restored.SuspensionHeightOffset=data.suspensionHeight;restored.GyroEnabled=data.gyroEnabled;
            restored.BoosterThrustNewtons=data.boosterThrust;restored.BoosterDurationSeconds=data.boosterDuration;restored.HoverHeightMeters=data.hoverHeight;
            restored.GrappleReadyAt=data.grappleRemaining>0?now+data.grappleRemaining:0;
            if(data.energyVersion<0||data.energyVersion>1||
                (data.energyVersion==1&&(!Finite(data.energy)||data.energy<0)))return false;
            restored.RefreshEnergyLayout();
            restored.Energy.Restore(data.energyVersion==0?VehicleEnergyState.CoreCapacity:data.energy);
            restored.InitialEnergyPrepared=!data.hasInitialEnergyPrepared||data.initialEnergyPrepared;
            model=restored;error=null;return true;
        }
    }
}

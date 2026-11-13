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
                        (!deadOnly||next.Health<=0)&&R3MountingGrid.Compatible(current,cell,next,cell+direction,footprint))
                    {reached.Add(next.Id);queue.Enqueue(next);}
            }
            return reached;
        }
    }

    [Serializable] public sealed class R3WheelCheckpoint
    {public string id;public Vector3Int cell,rotation;public int contacts;}
    [Serializable] public sealed class R3GroupCheckpoint
    {public string id;public bool secondary;}
    [Serializable] public sealed class R3WeaponSlotCheckpoint
    {public string id;public int slot;}
    [Serializable] public sealed class R3ActiveSlotCheckpoint
    {public int kind,slot;}
    [Serializable] public sealed class R3ModelCheckpoint
    {
        public R3PartState[] mounted,inventory;
        public R3WheelCheckpoint[] wheels;
        public R3GroupCheckpoint[] groups;
        // Version 0 saves have only the former two-group fields above.
        public int weaponSlotsVersion;
        public R3WeaponSlotCheckpoint[] weaponSlots;
        public int activeSlotsVersion;
        public int[] activeSlots;
        public R3ActiveSlotCheckpoint[] activePreferences;
        public int constructionBudgetVersion,constructionBudgetLimit,constructionBudgetMilestonesApplied;
        public float repairPulseRemaining,boosterRemaining,overdriveRemaining,rapidFireRemaining,doubleShotOverdriveRemaining,targetedEmpRemaining,areaEmpRemaining;
        public int scrap;
        public bool suspensionEnabled,gyroEnabled,hasA,hasB,hasC,hasD;
        public int groupA,groupB,groupC,groupD;
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
        public int LegacyGunScalePending { get; private set; }

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
            GetActiveSlots();
            return new R3ModelCheckpoint {
                mounted=Parts.Select(p=>SavePart(p,now)).ToArray(),inventory=inventory.Select(p=>SavePart(p,now)).ToArray(),
                wheels=factoryWheels.Select(p=>new R3WheelCheckpoint{id=p.Key,cell=p.Value.Cell,rotation=p.Value.Rotation,contacts=p.Value.Contacts}).ToArray(),
                groups=weaponGroupPreference.Select(p=>new R3GroupCheckpoint{id=p.Key,secondary=p.Value}).ToArray(),scrap=Scrap,
                weaponSlotsVersion=1,weaponSlots=weaponSlotPreference.Select(p=>new R3WeaponSlotCheckpoint{id=p.Key,slot=p.Value}).ToArray(),
                activeSlotsVersion=1,activeSlots=activeSlots.Select(p=>p.HasValue?(int)p.Value:-1).ToArray(),
                activePreferences=activeSlotPreference.Select(p=>new R3ActiveSlotCheckpoint{kind=(int)p.Key,slot=p.Value}).ToArray(),
                constructionBudgetVersion=1,constructionBudgetLimit=ConstructionBudgetLimit,constructionBudgetMilestonesApplied=ConstructionBudgetMilestonesApplied,
                repairPulseRemaining=Remaining(RepairPulseReadyAt,now),boosterRemaining=Remaining(BoosterReadyAt,now),overdriveRemaining=Remaining(OverdriveReadyAt,now),rapidFireRemaining=Remaining(RapidFireReadyAt,now),doubleShotOverdriveRemaining=Remaining(DoubleShotOverdriveReadyAt,now),targetedEmpRemaining=Remaining(TargetedEmpReadyAt,now),areaEmpRemaining=Remaining(AreaEmpReadyAt,now),
                energyVersion=1,energy=Energy.Current,hasInitialEnergyPrepared=true,initialEnergyPrepared=InitialEnergyPrepared,
                suspensionEnabled=SuspensionEnabled,suspensionMode=SuspensionMode,suspensionHeight=SuspensionHeightOffset,
                gyroEnabled=GyroEnabled,drive=SelectedDrive,boosterThrust=BoosterThrustNewtons,boosterDuration=BoosterDurationSeconds,
                hoverHeight=HoverHeightMeters,grappleRemaining=Remaining(GrappleReadyAt,now),
                hasA=weaponGroupA.HasValue,groupA=(int)(weaponGroupA??0),hasB=weaponGroupB.HasValue,groupB=(int)(weaponGroupB??0),
                hasC=weaponGroupC.HasValue,groupC=(int)(weaponGroupC??0),hasD=weaponGroupD.HasValue,groupD=(int)(weaponGroupD??0)
            };
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static bool ValidCheckpointPart(R3PartState p)
        {
            return p!=null&&!string.IsNullOrWhiteSpace(p.Id)&&p.Id.Length<=128&&ValidEconomicKind(p.Kind,p.Weapon)&&ValidWheelStyle(p)&&ValidArmorShape(p)&&ValidFootprint(p)&&
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
        public static bool TryRestoreCheckpoint(R3ModelCheckpoint data,R3VehicleKind kind,R3GarageEconomy economy,float now,out R3VehicleModel model,out string error,int variant=0)
        {
            model=null;error="Niepoprawny zapis konstrukcji.";
            if(data==null||!Enum.IsDefined(typeof(R3VehicleKind),kind)||variant<0||variant>=4||data.mounted==null||data.inventory==null||
                data.wheels==null||data.groups==null||data.mounted.Length>2000||data.inventory.Length>InventoryCapacity||
                data.constructionBudgetVersion<0||data.constructionBudgetVersion>1||
                (data.constructionBudgetVersion==1&&(data.constructionBudgetLimit<0||data.constructionBudgetMilestonesApplied<0))||
                data.wheels.Length>2000||data.groups.Length>4000||data.scrap<0||data.scrap>100000||
                !Enum.IsDefined(typeof(R3DriveKind),data.drive)||!Enum.IsDefined(typeof(ActiveSuspensionSupportMode),data.suspensionMode)||
                !Finite(data.suspensionHeight)||data.suspensionHeight<-.35f||data.suspensionHeight>.35f||
                !Finite(data.boosterThrust)||data.boosterThrust<500||data.boosterThrust>6000||
                !Finite(data.boosterDuration)||data.boosterDuration<.2f||data.boosterDuration>2||
                !Finite(data.hoverHeight)||data.hoverHeight<.7f||data.hoverHeight>1.6f||
                !Finite(data.grappleRemaining)||data.grappleRemaining<0||!Finite(data.repairPulseRemaining)||data.repairPulseRemaining<0||
                !Finite(data.boosterRemaining)||data.boosterRemaining<0||!Finite(data.overdriveRemaining)||data.overdriveRemaining<0||!Finite(data.rapidFireRemaining)||data.rapidFireRemaining<0||!Finite(data.doubleShotOverdriveRemaining)||data.doubleShotOverdriveRemaining<0||!Finite(data.targetedEmpRemaining)||data.targetedEmpRemaining<0||!Finite(data.areaEmpRemaining)||data.areaEmpRemaining<0)return false;
            var all=data.mounted.Concat(data.inventory).ToArray();
            if(data.energyVersion==0&&all.Any(p=>p!=null&&p.FootprintRevision!=0))return false;
            if(all.Any(p=>!ValidCheckpointPart(p))||all.Select(p=>p.Id).Distinct(StringComparer.Ordinal).Count()!=all.Length||
                data.mounted.Count(p=>p.Kind==R3PartKind.Core)!=1||data.inventory.Any(p=>p.Kind==R3PartKind.Core)||
                data.mounted.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Weapon).Distinct().Count()>4||
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
            bool equipmentScaleNormalized=false;
            foreach(var part in normalized.Where(p=>p.Kind==R3PartKind.Gun||p.Kind==R3PartKind.Wheel))
            {
                if(!UseEnlargedEquipment)
                {
                    int before=part.FootprintRevision;
                    NormalizeStoredEquipmentFootprint(part);
                    equipmentScaleNormalized|=before!=part.FootprintRevision;
                }
                int sideLeft=R3MountingGrid.FaceBit(Vector3Int.left),sideRight=R3MountingGrid.FaceBit(Vector3Int.right);
                bool fixedSide=part.Kind==R3PartKind.Wheel&&HasSideMount(part.WheelStyle)&&
                    (part.ContactMask==sideLeft||part.ContactMask==sideRight);
                int actual=fixedSide?part.ContactMask:PlacementContacts(part.Kind,part);
                if(part.ContactMask!=actual){part.ContactMask=actual;portNormalizedIds.Add(part.Id);}
            }
            int portMovedCount=0;
            if(portNormalizedIds.Count>0||equipmentScaleNormalized)
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
            // If the larger variant is enabled again, expanding a one-cell
            // gun or wheel in place can overlap other parts. Move the affected
            // equipment and disconnected parts to inventory transactionally.
            // Never modify the checkpoint if there is not enough space.
            int pendingGunScale=0;
            var oldGuns=UseEnlargedEquipment?mounted.Where(p=>p.Kind==R3PartKind.Gun&&p.FootprintRevision==0&&SupportsGunScale2(p.Weapon)).ToArray():Array.Empty<R3PartState>();
            var oldWheels=UseEnlargedEquipment?mounted.Where(p=>p.Kind==R3PartKind.Wheel&&p.FootprintRevision==0&&HasSizedSideWheel(p.WheelStyle)).ToArray():Array.Empty<R3PartState>();
            var oldEquipment=oldGuns.Concat(oldWheels).ToArray();
            if(oldEquipment.Length>0)
            {
                var others=mounted.Where(p=>!oldEquipment.Contains(p)).ToArray();
                var alive=others.Where(p=>p.Health>0).ToArray();
                var core=alive.SingleOrDefault(p=>p.Kind==R3PartKind.Core);
                if(core!=null)
                {
                    var reachable=ReachableCheckpointParts(alive,new HashSet<string>{core.Id},false);
                    reachable=ReachableCheckpointParts(others,reachable,true);
                    var detached=others.Where(p=>!reachable.Contains(p.Id)).ToArray();
                    if(inventory.Length+oldEquipment.Length+detached.Length<=InventoryCapacity)
                    {
                        mounted=others.Where(p=>reachable.Contains(p.Id)).ToArray();
                        inventory=inventory.Concat(oldEquipment).Concat(detached).ToArray();
                        migratedCount+=oldEquipment.Length+detached.Length;
                    }
                    else if(oldWheels.Length>0)
                    {error="Brak miejsca w magazynie na powiększone koła. Zapis pozostaje bez zmian.";return false;}
                    else pendingGunScale=oldGuns.Length;
                }
                else if(oldWheels.Length>0)
                {error="Nie można bezpiecznie powiększyć kół bez sprawnego rdzenia. Zapis pozostaje bez zmian.";return false;}
                else pendingGunScale=oldGuns.Length;
            }
            if(data.wheels.Any(p=>p==null||string.IsNullOrWhiteSpace(p.id)||p.contacts<0||p.contacts>63)||
                data.wheels.Select(p=>p.id).Distinct(StringComparer.Ordinal).Count()!=data.wheels.Length||
                data.groups.Any(p=>p==null||string.IsNullOrWhiteSpace(p.id))||
                data.groups.Select(p=>p.id).Distinct(StringComparer.Ordinal).Count()!=data.groups.Length||
                (data.hasA&&!Enum.IsDefined(typeof(R4WeaponKind),data.groupA))||(data.hasB&&!Enum.IsDefined(typeof(R4WeaponKind),data.groupB))||
                (data.hasA&&data.hasB&&data.groupA==data.groupB)||
                data.weaponSlotsVersion<0||data.weaponSlotsVersion>1||
                (data.weaponSlotsVersion==1&&(data.weaponSlots==null||data.weaponSlots.Length>4000||
                    data.weaponSlots.Any(p=>p==null||string.IsNullOrWhiteSpace(p.id)||p.slot<0||p.slot>=4)||
                    data.weaponSlots.Select(p=>p.id).Distinct(StringComparer.Ordinal).Count()!=data.weaponSlots.Length||
                    (data.hasC&&!Enum.IsDefined(typeof(R4WeaponKind),data.groupC))||
                    (data.hasD&&!Enum.IsDefined(typeof(R4WeaponKind),data.groupD))||
                    new[]{data.hasA?(int?)data.groupA:null,data.hasB?(int?)data.groupB:null,
                          data.hasC?(int?)data.groupC:null,data.hasD?(int?)data.groupD:null}
                        .Where(p=>p.HasValue).GroupBy(p=>p.Value).Any(g=>g.Count()>1)))||
                data.activeSlotsVersion<0||data.activeSlotsVersion>1||
                (data.activeSlotsVersion==1&&(data.activeSlots==null||data.activeSlots.Length!=4||data.activePreferences==null||
                    data.activePreferences.Length>Enum.GetValues(typeof(R3PartKind)).Length||
                    data.activeSlots.Any(p=>p!=-1&&(!Enum.IsDefined(typeof(R3PartKind),p)||!IsActiveSlotPart((R3PartKind)p)))||
                    data.activeSlots.Where(p=>p!=-1).GroupBy(p=>p).Any(g=>g.Count()>1)||
                    data.activePreferences.Any(p=>p==null||p.slot<0||p.slot>=4||!Enum.IsDefined(typeof(R3PartKind),p.kind)||!IsActiveSlotPart((R3PartKind)p.kind))||
                    data.activePreferences.Select(p=>p.kind).Distinct().Count()!=data.activePreferences.Length)))return false;
            var restored=new R3VehicleModel(R3VehicleCatalog.Create(kind,variant),economy);
            restored.Parts.Clear();restored.inventory.Clear();
            foreach(var source in mounted){var p=source.Copy();p.Tier=NormalizedTier(p.Tier);p.DeviceReadyAt=p.DeviceReadyAt>0?now+p.DeviceReadyAt:0;p.ReloadReadyAt=p.ReloadReadyAt>0?now+p.ReloadReadyAt:0;restored.Parts.Add(p);}
            foreach(var source in inventory){var p=source.Copy();p.ContactMask=StoredWheelContacts(p);NormalizeStoredEquipmentFootprint(p);p.Tier=NormalizedTier(p.Tier);p.DeviceReadyAt=p.DeviceReadyAt>0?now+p.DeviceReadyAt:0;p.ReloadReadyAt=p.ReloadReadyAt>0?now+p.ReloadReadyAt:0;restored.inventory.Add(p);}
            restored.LegacyPartsMovedToInventory=migratedCount;
            restored.LegacyPortMasksNormalized=portNormalizedIds.Count;
            restored.LegacyPortPartsMovedToInventory=portMovedCount;
            restored.LegacyGunScalePending=pendingGunScale;
            restored.factoryWheels.Clear();foreach(var wheel in data.wheels)
            {
                int contacts=wheel.contacts;
                var normalizedWheel=mounted.Concat(inventory).FirstOrDefault(p=>p.Id==wheel.id&&p.Kind==R3PartKind.Wheel);
                if(normalizedWheel!=null&&portNormalizedIds.Contains(wheel.id))contacts=PlacementContacts(R3PartKind.Wheel,normalizedWheel);
                restored.factoryWheels.Add(wheel.id,new FactoryWheelPose(new R3PartState(wheel.cell,R3PartKind.Wheel){RotationQuarterTurns=wheel.rotation,ContactMask=contacts}));
            }
            restored.weaponGroupPreference.Clear();foreach(var group in data.groups)restored.weaponGroupPreference.Add(group.id,group.secondary);
            restored.weaponGroupA=data.hasA?(R4WeaponKind?)data.groupA:null;restored.weaponGroupB=data.hasB?(R4WeaponKind?)data.groupB:null;
            if(data.weaponSlotsVersion==1)
            {
                foreach(var slot in data.weaponSlots)restored.weaponSlotPreference.Add(slot.id,slot.slot);
                restored.weaponGroupC=data.hasC?(R4WeaponKind?)data.groupC:null;
                restored.weaponGroupD=data.hasD?(R4WeaponKind?)data.groupD:null;
            }
            if(data.activeSlotsVersion==1)
            {
                for(int i=0;i<4;i++)restored.activeSlots[i]=data.activeSlots[i]<0?(R3PartKind?)null:(R3PartKind)data.activeSlots[i];
                foreach(var preference in data.activePreferences)restored.activeSlotPreference.Add((R3PartKind)preference.kind,preference.slot);
            }
            restored.Scrap=data.scrap;restored.SelectedDrive=data.drive;restored.SuspensionEnabled=data.suspensionEnabled;
            if(data.constructionBudgetVersion==1){restored.ConstructionBudgetLimit=data.constructionBudgetLimit;restored.ConstructionBudgetMilestonesApplied=data.constructionBudgetMilestonesApplied;}
            restored.RepairPulseReadyAt=data.repairPulseRemaining>0?now+data.repairPulseRemaining:0;
            restored.OverdriveReadyAt=data.overdriveRemaining>0?now+data.overdriveRemaining:0;
            restored.RapidFireReadyAt=data.rapidFireRemaining>0?now+data.rapidFireRemaining:0;
            restored.DoubleShotOverdriveReadyAt=data.doubleShotOverdriveRemaining>0?now+data.doubleShotOverdriveRemaining:0;
            restored.TargetedEmpReadyAt=data.targetedEmpRemaining>0?now+data.targetedEmpRemaining:0;
            restored.AreaEmpReadyAt=data.areaEmpRemaining>0?now+data.areaEmpRemaining:0;
            restored.BoosterReadyAt=data.boosterRemaining>0?now+data.boosterRemaining:0;
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

using System;
using System.Linq;
using UnityEngine;
using RoboRougelike.R4;
using RoboRougelike.Mechanics.MOV009;

namespace RoboRougelike.R3
{
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
        public R3DriveKind drive;
    }
    public sealed partial class R3VehicleModel
    {
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
                suspensionEnabled=SuspensionEnabled,suspensionMode=SuspensionMode,suspensionHeight=SuspensionHeightOffset,
                gyroEnabled=GyroEnabled,drive=SelectedDrive,boosterThrust=BoosterThrustNewtons,boosterDuration=BoosterDurationSeconds,
                hoverHeight=HoverHeightMeters,grappleRemaining=Remaining(GrappleReadyAt,now),
                hasA=weaponGroupA.HasValue,groupA=(int)(weaponGroupA??0),hasB=weaponGroupB.HasValue,groupB=(int)(weaponGroupB??0)
            };
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static bool ValidCheckpointPart(R3PartState p)
        {
            return p!=null&&!string.IsNullOrWhiteSpace(p.Id)&&p.Id.Length<=128&&ValidEconomicKind(p.Kind,p.Weapon)&&
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
                data.mounted.SelectMany(OccupiedCells).GroupBy(c=>c).Any(g=>g.Count()>1)||
                !Connected(data.mounted)||!Connected(data.mounted.Where(p=>p.Health>0)))return false;
            if(data.wheels.Any(p=>p==null||string.IsNullOrWhiteSpace(p.id)||p.contacts<0||p.contacts>63)||
                data.wheels.Select(p=>p.id).Distinct(StringComparer.Ordinal).Count()!=data.wheels.Length||
                data.groups.Any(p=>p==null||string.IsNullOrWhiteSpace(p.id))||
                data.groups.Select(p=>p.id).Distinct(StringComparer.Ordinal).Count()!=data.groups.Length||
                (data.hasA&&!Enum.IsDefined(typeof(R4WeaponKind),data.groupA))||(data.hasB&&!Enum.IsDefined(typeof(R4WeaponKind),data.groupB))||
                (data.hasA&&data.hasB&&data.groupA==data.groupB))return false;
            var restored=new R3VehicleModel(R3VehicleCatalog.Create(kind),economy);
            restored.Parts.Clear();restored.inventory.Clear();
            foreach(var source in data.mounted){var p=source.Copy();p.DeviceReadyAt=p.DeviceReadyAt>0?now+p.DeviceReadyAt:0;p.ReloadReadyAt=p.ReloadReadyAt>0?now+p.ReloadReadyAt:0;restored.Parts.Add(p);}
            foreach(var source in data.inventory){var p=source.Copy();p.DeviceReadyAt=p.DeviceReadyAt>0?now+p.DeviceReadyAt:0;p.ReloadReadyAt=p.ReloadReadyAt>0?now+p.ReloadReadyAt:0;restored.inventory.Add(p);}
            restored.factoryWheels.Clear();foreach(var wheel in data.wheels)
                restored.factoryWheels.Add(wheel.id,new FactoryWheelPose(new R3PartState(wheel.cell,R3PartKind.Wheel){RotationQuarterTurns=wheel.rotation,ContactMask=wheel.contacts}));
            restored.weaponGroupPreference.Clear();foreach(var group in data.groups)restored.weaponGroupPreference.Add(group.id,group.secondary);
            restored.weaponGroupA=data.hasA?(R4WeaponKind?)data.groupA:null;restored.weaponGroupB=data.hasB?(R4WeaponKind?)data.groupB:null;
            restored.Scrap=data.scrap;restored.SelectedDrive=data.drive;restored.SuspensionEnabled=data.suspensionEnabled;
            restored.SuspensionMode=data.suspensionMode;restored.SuspensionHeightOffset=data.suspensionHeight;restored.GyroEnabled=data.gyroEnabled;
            restored.BoosterThrustNewtons=data.boosterThrust;restored.BoosterDurationSeconds=data.boosterDuration;restored.HoverHeightMeters=data.hoverHeight;
            restored.GrappleReadyAt=data.grappleRemaining>0?now+data.grappleRemaining:0;
            model=restored;error=null;return true;
        }
    }
}

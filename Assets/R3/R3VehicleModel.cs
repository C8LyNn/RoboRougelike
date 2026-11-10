using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R4;
using RoboRougelike.R1;

namespace RoboRougelike.R3
{
    // Append new values: checkpoints store these numeric IDs.
    public enum R3PartKind { Armor, Wheel, Gun, Core, Suspension, Booster, JumpDrive, Blink, Gyroscope, OmniDrive, MagneticGrapple, HoverEngine, CrawlerTrack, BonusModule, DirectionalShield, PointDefense, StructuralScanner, ScannerPulse, IndependentSteering, Battery, RepairPulse, DrivePower, EnergyRegenerator }
    public enum R3WheelStyle { Offroad, Slick, LargeOffroad, Monster, SmallFork, BalloonFork, SpikedFork, TwinRoad, SteerRoad, SteerOffroad, CasterSlick, SwingOffroad }
    // Zero preserves the shape of every pre-existing checkpoint.
    public enum R3ArmorShape { Cube, Wedge, HalfBlock, Pyramid, RoundEdge, HalfWedge, Dome }
    public enum R3DriveKind { Wheels, Tracks, Omni, Hover }
    public enum R3VehicleKind { Standard, Light, Heavy }
    [Serializable]
    public sealed class R3PartState
    {
        public Vector3Int Cell;
        public R3PartKind Kind;
        // 0 is accepted only as a legacy serialized tier and normalized to T1.
        public int Tier = 1;
        public float Health = 1;
        public string Id = Guid.NewGuid().ToString("N");
        public string Source = "Zakup / pojazd startowy";
        public R4WeaponKind Weapon;
        public R3WheelStyle WheelStyle;
        // Zero is the one-cell footprint written by all existing saves.
        // Revision 1 reserves a 3x3 tire plane for large side wheels.
        public int FootprintRevision;
        public R3ArmorShape ArmorShape;
        // A bonus is a physical, single inventory item occupying one grid cell.
        public string UpgradeId, UpgradeTitle, UpgradeDescription;
        public R1UpgradeEffect UpgradeEffect;
        public float UpgradeAmount;
        // Rotation in 90-degree steps around local X/Y/Z; mask encodes six
        // optional face-centre mounting contacts in local orientation.
        public Vector3Int RotationQuarterTurns;
        public int ContactMask=R3MountingGrid.AllFaces;
        public RoboRougelike.Mechanics.IndependentSteering.SteeringMode SteeringMode;
        public float DeviceReadyAt;
        // Persistent resources belong to the owned part, including during garage rebuilds.
        public int Ammo=-1;
        public float ReloadReadyAt;
        public float DisruptionRemaining;
        public float ThermalHeat;
        public bool ThermalLocked;
        public float DefenseCharge=-1,DefenseRechargeAge=float.PositiveInfinity;
        public R3PartState(Vector3Int cell, R3PartKind kind, float health = 1) { Cell = cell; Kind = kind; Health = health; }
        public R3PartState Copy() => new R3PartState(Cell, Kind, Health){Id=Id,Source=Source,Weapon=Weapon,WheelStyle=WheelStyle,FootprintRevision=FootprintRevision,ArmorShape=ArmorShape,Tier=Tier,SteeringMode=SteeringMode,DeviceReadyAt=DeviceReadyAt,Ammo=Ammo,ReloadReadyAt=ReloadReadyAt,ThermalHeat=ThermalHeat,ThermalLocked=ThermalLocked,DisruptionRemaining=DisruptionRemaining,DefenseCharge=DefenseCharge,DefenseRechargeAge=DefenseRechargeAge,
            UpgradeId=UpgradeId,UpgradeTitle=UpgradeTitle,UpgradeDescription=UpgradeDescription,
            UpgradeEffect=UpgradeEffect,UpgradeAmount=UpgradeAmount,
            RotationQuarterTurns=RotationQuarterTurns,ContactMask=ContactMask};
    }
    public sealed class R3VehicleDesign
    {
        public string Name, Description;
        public float Speed, TurnSpeed, CoreHp = 100;
        public readonly List<R3PartState> Parts = new List<R3PartState>();
    }
    [Serializable]
    public sealed class R3GarageEconomy
    {
        public int ArmorCost = 5, WheelCost = 15, GunCost = 30, MaxRepairCost = 3;
        public int Price(R3PartKind kind) => kind switch {
            R3PartKind.Suspension=>25, R3PartKind.Wheel=>WheelCost, R3PartKind.Gun=>GunCost,
            R3PartKind.Booster=>35, R3PartKind.JumpDrive=>45, R3PartKind.Blink=>60,
            R3PartKind.Gyroscope=>30, R3PartKind.OmniDrive=>40, R3PartKind.MagneticGrapple=>45,
            R3PartKind.HoverEngine=>35, R3PartKind.CrawlerTrack=>45,
            R3PartKind.BonusModule=>60,R3PartKind.DirectionalShield=>60,R3PartKind.PointDefense=>70,R3PartKind.StructuralScanner=>40,R3PartKind.ScannerPulse=>30,R3PartKind.IndependentSteering=>45,R3PartKind.Battery=>25,R3PartKind.RepairPulse=>50,R3PartKind.DrivePower=>45,R3PartKind.EnergyRegenerator=>40, _=>ArmorCost };
    }
    public sealed partial class R3VehicleModel
    {
        public const int InventoryCapacity=200;
        public static int NormalizedTier(int tier)=>tier<=0?1:Math.Min(3,tier);
        public static float TierHealth(int tier)=>NormalizedTier(tier)==3?3.6f:NormalizedTier(tier)==2?2f:1f;
        public static float TierMass(int tier)=>NormalizedTier(tier)==3?1.3f:NormalizedTier(tier)==2?1.15f:1f;
        public static float TierWheelTraction(int tier)=>NormalizedTier(tier)==3?1.8f:NormalizedTier(tier)==2?1.35f:1f;
        public static bool HasSizedSideWheel(R3WheelStyle style)=>style==R3WheelStyle.LargeOffroad||style==R3WheelStyle.Monster;
        public static bool SupportsGunScale2(R4WeaponKind weapon)=>
            weapon==R4WeaponKind.Basic||weapon==R4WeaponKind.Automatic||weapon==R4WeaponKind.Heavy||
            weapon==R4WeaponKind.RocketLauncher||weapon==R4WeaponKind.Shotgun||weapon==R4WeaponKind.Railgun||
            weapon==R4WeaponKind.BeamCutter||weapon==R4WeaponKind.EmpEmitter||weapon==R4WeaponKind.RotarySaw;
        // The larger geometry and versioned footprints remain supported for a
        // future iteration, but current purchases and restored vehicles use 1x.
        static bool useEnlargedEquipment=false;
        public static bool UseEnlargedEquipment => useEnlargedEquipment;
#if UNITY_EDITOR
        // Editor tests may verify the retained larger variant in isolation.
        public static void SetEquipmentScaleForEditorTests(bool enlarged)=>useEnlargedEquipment=enlarged;
#endif
        public static int NewPurchaseFootprint(R3PartKind kind,R4WeaponKind weapon,R3WheelStyle wheelStyle)=>
            UseEnlargedEquipment&&kind==R3PartKind.Wheel&&HasSizedSideWheel(wheelStyle)?1:
            UseEnlargedEquipment&&kind==R3PartKind.Gun&&SupportsGunScale2(weapon)?2:0;
        public static void NormalizeStoredEquipmentFootprint(R3PartState part)
        {
            if(part==null)return;
            if(part.Kind==R3PartKind.Gun&&SupportsGunScale2(part.Weapon))
                part.FootprintRevision=UseEnlargedEquipment?2:0;
            else if(part.Kind==R3PartKind.Wheel&&HasSizedSideWheel(part.WheelStyle))
                part.FootprintRevision=UseEnlargedEquipment?1:0;
        }
        public static bool HasForkMount(R3WheelStyle style)=>style==R3WheelStyle.SmallFork||style==R3WheelStyle.BalloonFork||style==R3WheelStyle.SpikedFork||style==R3WheelStyle.TwinRoad||style==R3WheelStyle.SteerRoad||style==R3WheelStyle.SteerOffroad||style==R3WheelStyle.CasterSlick||style==R3WheelStyle.SwingOffroad;
        public static bool HasSideMount(R3WheelStyle style)=>!HasForkMount(style);
        public static bool ValidWheelOrientation(Vector3Int turns)=>
            R3MountingGrid.Rotate(Vector3Int.up,turns)==Vector3Int.up&&
            Math.Abs(R3MountingGrid.Rotate(Vector3Int.right,turns).x)==1;
        public static int StoredWheelContacts(R3PartState part)=>part!=null&&part.Kind==R3PartKind.Wheel&&HasSideMount(part.WheelStyle)?
            R3MountingGrid.FaceBit(Vector3Int.right)|R3MountingGrid.FaceBit(Vector3Int.left):part?.ContactMask??R3MountingGrid.AllFaces;
        // Existing vehicles retain their serialized ports; a turret being newly
        // installed must use its local mounting base, not an arbitrary side.
        public static int PlacementContacts(R3PartKind kind,R3PartState part=null)=>
            kind==R3PartKind.Gun?R3MountingGrid.FaceBit(Vector3Int.down):StoredWheelContacts(part);
        // An unmounted side wheel can be fitted on either side; once installed
        // only the real axle side participates in connectivity and damage.
        public int SideWheelContactAt(R3PartState wheel)
        {
            if(wheel==null||wheel.Kind!=R3PartKind.Wheel||!HasSideMount(wheel.WheelStyle))return 0;
            var candidate=wheel.Copy();candidate.ContactMask=StoredWheelContacts(candidate);
            var axis=R3MountingGrid.Rotate(Vector3Int.right,candidate.RotationQuarterTurns);
            var plus=At(candidate.Cell+axis);var minus=At(candidate.Cell-axis);
            bool positive=plus!=null&&plus.Health>0&&R3MountingGrid.Compatible(candidate,candidate.Cell,plus,candidate.Cell+axis);
            bool negative=minus!=null&&minus.Health>0&&R3MountingGrid.Compatible(candidate,candidate.Cell,minus,candidate.Cell-axis);
            if(!positive&&!negative)return 0;
            bool mirror=positive!=negative?positive:(candidate.Cell.x<0?axis.x>0:axis.x<0);
            return R3MountingGrid.FaceBit(mirror?Vector3Int.right:Vector3Int.left);
        }
        public static bool CanSwapWheelAppearance(R3WheelStyle style)=>style==R3WheelStyle.Offroad||style==R3WheelStyle.Slick;
        static float AuthoredWheelRadius(R3WheelStyle style)=>style==R3WheelStyle.SwingOffroad ? .33f : style==R3WheelStyle.CasterSlick ? .17f : style==R3WheelStyle.SteerOffroad ? .48f : style==R3WheelStyle.SteerRoad ? .34f : style==R3WheelStyle.TwinRoad ? .33f : style==R3WheelStyle.SpikedFork ? .37f : style==R3WheelStyle.SmallFork ? .205f : style==R3WheelStyle.BalloonFork ? .5f : style==R3WheelStyle.Monster ? .8f : style==R3WheelStyle.LargeOffroad ? .56f : .39f;
        public static float WheelArtScale(R3WheelStyle style)=>!UseEnlargedEquipment&&HasSizedSideWheel(style) ? .39f/AuthoredWheelRadius(style) : 1f;
        public static float WheelRadius(R3WheelStyle style)=>AuthoredWheelRadius(style)*WheelArtScale(style);
        public static float WheelWidth(R3WheelStyle style)=>(style==R3WheelStyle.SwingOffroad ? .26f : style==R3WheelStyle.CasterSlick ? .12f : style==R3WheelStyle.SteerOffroad ? .34f : style==R3WheelStyle.SteerRoad ? .26f : style==R3WheelStyle.TwinRoad ? .16f : style==R3WheelStyle.SpikedFork ? .30f : style==R3WheelStyle.SmallFork ? .15f : style==R3WheelStyle.BalloonFork ? .43f : style==R3WheelStyle.Monster ? .58f : .42f)*WheelArtScale(style);
        public static float WheelAxleOffset(R3WheelStyle style)
        {
            float authored=style==R3WheelStyle.Monster ? .142f : style==R3WheelStyle.LargeOffroad ? .062f : .022f;
            // Scaling is about the visual axle; shift the physical axle so the
            // scaled mounting socket still lies on the same block face.
            return (.26f+authored)*WheelArtScale(style)-.26f;
        }
        public static float ForkAxleDrop(R3WheelStyle style)=>style==R3WheelStyle.SwingOffroad ? .30f : style==R3WheelStyle.CasterSlick ? .13f : style==R3WheelStyle.SteerOffroad ? .455f : style==R3WheelStyle.SteerRoad ? .315f : style==R3WheelStyle.TwinRoad ? .20f : style==R3WheelStyle.SpikedFork ? .24f : style==R3WheelStyle.BalloonFork ? .37f : .075f;
        public static float WheelTraction(R3WheelStyle style)=>style==R3WheelStyle.CasterSlick ? .5f : style==R3WheelStyle.SmallFork ? .65f : style==R3WheelStyle.BalloonFork||style==R3WheelStyle.SteerOffroad?1.15f:style==R3WheelStyle.Monster?1.5f:style==R3WheelStyle.LargeOffroad?1.25f:1f;
        public static float WheelMass(R3WheelStyle style)=>style==R3WheelStyle.CasterSlick ? 1f/3f : style==R3WheelStyle.SmallFork ? .5f : style==R3WheelStyle.BalloonFork||style==R3WheelStyle.SteerOffroad?4f/3f:style==R3WheelStyle.Monster?2f:style==R3WheelStyle.LargeOffroad?1.5f:1f;
        public static int TierEffectUnits(int tier)=>NormalizedTier(tier)==3?4:NormalizedTier(tier)==2?2:1;
        // Passive capacity is reserved per physical module, independently of tier.
        public static float TierPassiveExtra(int tier)=>0f;
        public static float TierActivationCost(int tier)=>NormalizedTier(tier)==3?1.4f:NormalizedTier(tier)==2?1.2f:1f;
        public static bool SupportsFusion(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic)=>kind==R3PartKind.Armor||kind==R3PartKind.Wheel||
            kind==R3PartKind.Battery||kind==R3PartKind.RepairPulse||kind==R3PartKind.DrivePower||kind==R3PartKind.EnergyRegenerator||kind==R3PartKind.BonusModule||
            (kind==R3PartKind.Gun&&weapon>=R4WeaponKind.Basic&&weapon<=R4WeaponKind.EmpEmitter);
        public R3VehicleDesign Design { get; }
        public List<R3PartState> Parts { get; }
        private readonly List<R3PartState> inventory=new List<R3PartState>();
        public IReadOnlyList<R3PartState> Inventory => inventory;
        public int Scrap { get; private set; }
        public VehicleEnergyState Energy { get; } = new VehicleEnergyState();
        internal bool InitialEnergyPrepared;
        public void PrepareInitialEnergy(){if(InitialEnergyPrepared)return;RefreshEnergyLayout();Energy.Fill();InitialEnergyPrepared=true;}
        private Func<R3PartState,string> installationGate;
        internal void SetInstallationGate(Func<R3PartState,string> gate) { installationGate=gate; }
        private Func<IReadOnlyList<R3PartState>,bool> restorationGate;
        internal void SetRestorationGate(Func<IReadOnlyList<R3PartState>,bool> gate){restorationGate=gate;}
        readonly struct FactoryWheelPose {
            public readonly Vector3Int Cell,Rotation;public readonly int Contacts;
            public FactoryWheelPose(R3PartState p){Cell=p.Cell;Rotation=p.RotationQuarterTurns;Contacts=p.ContactMask;}
        }
        readonly Dictionary<string,FactoryWheelPose> factoryWheels=new Dictionary<string,FactoryWheelPose>(StringComparer.Ordinal);
        internal bool IsFactoryWheel(R3PartState part)=>part!=null&&part.Kind==R3PartKind.Wheel&&
            factoryWheels.TryGetValue(part.Id,out var pose)&&pose.Cell==part.Cell&&pose.Rotation==part.RotationQuarterTurns&&
            (pose.Contacts==part.ContactMask||(pose.Contacts==R3MountingGrid.AllFaces&&part.ContactMask==StoredWheelContacts(part)));
        public bool SuspensionEnabled = true;
        public RoboRougelike.Mechanics.MOV009.ActiveSuspensionSupportMode SuspensionMode = RoboRougelike.Mechanics.MOV009.ActiveSuspensionSupportMode.FullSupport;
        public float SuspensionHeightOffset;
        public R3DriveKind SelectedDrive;
        public bool GyroEnabled=true;
        public float BoosterThrustNewtons=3000, BoosterDurationSeconds=1;
        public float HoverHeightMeters=1;
        public float GrappleReadyAt;
        // Assignments follow mounted types, including damaged plans that can be repaired.
        R4WeaponKind? weaponGroupA, weaponGroupB;
        readonly Dictionary<string,bool> weaponGroupPreference=new Dictionary<string,bool>(StringComparer.Ordinal);
        public R4WeaponKind? WeaponGroupA { get { SyncWeaponGroups(); return weaponGroupA; } }
        public R4WeaponKind? WeaponGroupB { get { SyncWeaponGroups(); return weaponGroupB; } }
        public void SyncWeaponGroups()
        {
            var types=Parts.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Weapon).Distinct().Take(2).ToList();
            if(weaponGroupA.HasValue&&!types.Contains(weaponGroupA.Value))weaponGroupA=null;
            if(weaponGroupB.HasValue&&!types.Contains(weaponGroupB.Value))weaponGroupB=null;
            foreach(var kind in types)
            {
                if(weaponGroupA==kind||weaponGroupB==kind)continue;
                var remembered=Parts.FirstOrDefault(p=>p.Kind==R3PartKind.Gun&&p.Weapon==kind&&
                    !string.IsNullOrEmpty(p.Id)&&weaponGroupPreference.ContainsKey(p.Id));
                if(remembered!=null&&weaponGroupPreference[remembered.Id]&&!weaponGroupB.HasValue)weaponGroupB=kind;
                else if(remembered!=null&&!weaponGroupPreference[remembered.Id]&&!weaponGroupA.HasValue)weaponGroupA=kind;
                else if(!weaponGroupA.HasValue)weaponGroupA=kind;
                else if(!weaponGroupB.HasValue)weaponGroupB=kind;
            }
            RememberWeaponGroups();
        }
        void RememberWeaponGroups()
        {
            foreach(var part in Parts.Where(p=>p.Kind==R3PartKind.Gun&&!string.IsNullOrEmpty(p.Id)))
            {
                if(weaponGroupA==part.Weapon)weaponGroupPreference[part.Id]=false;
                else if(weaponGroupB==part.Weapon)weaponGroupPreference[part.Id]=true;
            }
        }
        public void SwapWeaponGroups()
        { SyncWeaponGroups(); var old=weaponGroupA; weaponGroupA=weaponGroupB; weaponGroupB=old; RememberWeaponGroups(); }
        public R3GarageEconomy Economy { get; }
        public R3VehicleModel(R3VehicleDesign design, R3GarageEconomy economy = null)
        { Design = design; Parts = design.Parts.Select(p => {var part=p.Copy();part.Id=Guid.NewGuid().ToString("N");return part;}).ToList(); Economy = economy ?? new R3GarageEconomy();
            foreach(var part in Parts.Where(p=>p.Kind==R3PartKind.Wheel))factoryWheels.Add(part.Id,new FactoryWheelPose(part)); RefreshEnergyLayout(); Energy.Fill(); }
        public static readonly Vector3Int[] Neighbours = { Vector3Int.left, Vector3Int.right, Vector3Int.up,
            Vector3Int.down, Vector3Int.forward, Vector3Int.back };
        public R3VehicleModel DeepCopy()
        {
            var design=new R3VehicleDesign{Name=Design.Name,Description=Design.Description,Speed=Design.Speed,TurnSpeed=Design.TurnSpeed,CoreHp=Design.CoreHp};
            design.Parts.AddRange(Design.Parts.Select(p=>p.Copy()));
            var copy=new R3VehicleModel(design,new R3GarageEconomy{ArmorCost=Economy.ArmorCost,WheelCost=Economy.WheelCost,GunCost=Economy.GunCost,MaxRepairCost=Economy.MaxRepairCost});
            copy.Parts.Clear();copy.Parts.AddRange(Parts.Select(p=>p.Copy()));copy.inventory.AddRange(inventory.Select(p=>p.Copy()));copy.Scrap=Scrap;
            copy.factoryWheels.Clear();foreach(var item in factoryWheels)copy.factoryWheels.Add(item.Key,item.Value);
            copy.SuspensionEnabled=SuspensionEnabled;copy.SuspensionMode=SuspensionMode;copy.SuspensionHeightOffset=SuspensionHeightOffset;
            copy.RefreshEnergyLayout();copy.Energy.Restore(Energy.Current);copy.InitialEnergyPrepared=InitialEnergyPrepared;
            copy.SelectedDrive=SelectedDrive;copy.GyroEnabled=GyroEnabled;copy.BoosterThrustNewtons=BoosterThrustNewtons;
            copy.BoosterDurationSeconds=BoosterDurationSeconds;copy.GrappleReadyAt=GrappleReadyAt;
            copy.HoverHeightMeters=HoverHeightMeters;
            SyncWeaponGroups();copy.weaponGroupA=weaponGroupA;copy.weaponGroupB=weaponGroupB;
            foreach(var preference in weaponGroupPreference)copy.weaponGroupPreference[preference.Key]=preference.Value;
            return copy;
        }
        public const int MaxScrap=100000;
        public int PreviewAward(int amount) => (int)Math.Min(Math.Max(0L,(long)MaxScrap-Scrap),Math.Max(0L,(long)amount));
        public void Award(int amount) { Scrap = (int)Math.Min((long)MaxScrap, Scrap + (long)Math.Max(0, amount)); }
        public static bool IsPassiveEnergyPart(R3PartKind kind) =>
            kind==R3PartKind.Suspension||kind==R3PartKind.Gyroscope||kind==R3PartKind.BonusModule||
            kind==R3PartKind.DirectionalShield||kind==R3PartKind.IndependentSteering||kind==R3PartKind.DrivePower||kind==R3PartKind.EnergyRegenerator;
        public void RefreshEnergyLayout()
        {
            var alive=Parts.Where(p=>p.Health>0).ToArray();
            Energy.SetAdditionalPassiveReservation(alive.Where(p=>IsPassiveEnergyPart(p.Kind)).Sum(p=>TierPassiveExtra(p.Tier)));
            Energy.Refresh(alive.Where(p=>p.Kind==R3PartKind.Battery).Sum(p=>TierEffectUnits(p.Tier)),
                alive.Count(p=>IsPassiveEnergyPart(p.Kind)),alive.Any(p=>p.Kind==R3PartKind.Core));
            Energy.SetRegeneratorCount(alive.Where(p=>p.Kind==R3PartKind.EnergyRegenerator).Sum(p=>TierEffectUnits(p.Tier)));
        }
        // The legacy overload describes a one-cell item. Versioned owned
        // items use their own physical footprint via OccupiedCells(part).
        public static IEnumerable<Vector3Int> OccupiedCells(Vector3Int anchor,R3PartKind kind) =>
            OccupiedCells(anchor,kind,default);
        public static bool UsesWideFootprint(R3PartKind kind) => false;
        public static IEnumerable<Vector3Int> OccupiedCells(Vector3Int anchor,R3PartKind kind,Vector3Int turns)
        {
            yield return anchor;
        }
        public static IEnumerable<Vector3Int> OccupiedCells(R3PartState part)
        {
            if(part.FootprintRevision==2&&part.Kind==R3PartKind.Gun&&SupportsGunScale2(part.Weapon))
            {
                for(int y=0;y<=1;y++)for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                    yield return part.Cell+R3MountingGrid.Rotate(new Vector3Int(x,y,z),part.RotationQuarterTurns);
                yield break;
            }
            if(part.FootprintRevision==1&&part.Kind==R3PartKind.Wheel&&HasSizedSideWheel(part.WheelStyle))
            {
                for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
                    yield return part.Cell+R3MountingGrid.Rotate(new Vector3Int(0,y,z),part.RotationQuarterTurns);
                yield break;
            }
            yield return part.Cell;
        }
        public R3PartState At(Vector3Int cell) => Parts.FirstOrDefault(p=>OccupiedCells(p).Contains(cell));
        private bool HasAliveExternalNeighbour(Vector3Int anchor,R3PartKind kind,
            Vector3Int turns=default,int contacts=R3MountingGrid.AllFaces,R3PartState template=null)
        {
            var candidate=template?.Copy()??new R3PartState(anchor,kind);
            candidate.Cell=anchor;candidate.RotationQuarterTurns=turns;candidate.ContactMask=contacts;
            var footprint=new HashSet<Vector3Int>(OccupiedCells(candidate));
            foreach(var cell in footprint)
                foreach(var direction in Neighbours)
                {
                    if(footprint.Contains(cell+direction))continue;
                    var neighbour=At(cell+direction);
                    if(neighbour!=null&&neighbour.Health>0&&
                       R3MountingGrid.Compatible(candidate,cell,neighbour,cell+direction))return true;
                }
            return false;
        }
        public static bool ValidEconomicKind(R3PartKind kind,R4WeaponKind weapon) =>
            Enum.IsDefined(typeof(R3PartKind),kind) &&
            (kind!=R3PartKind.Gun||Enum.IsDefined(typeof(R4WeaponKind),weapon));
        public static bool ValidArmorShape(R3PartState part)=>part!=null&&Enum.IsDefined(typeof(R3ArmorShape),part.ArmorShape)&&
            (part.ArmorShape==R3ArmorShape.Cube||(part.Kind==R3PartKind.Armor&&R4_1.R41PartGeometry.FullArmorContacts(part.ArmorShape)!=0&&
                part.ContactMask==R4_1.R41PartGeometry.FullArmorContacts(part.ArmorShape)));
        public static bool ValidWheelStyle(R3PartState part)=>part!=null&&Enum.IsDefined(typeof(R3WheelStyle),part.WheelStyle)&&
            (part.Kind==R3PartKind.Wheel?(!HasForkMount(part.WheelStyle)||part.ContactMask==R3MountingGrid.FaceBit(Vector3Int.up)):
                part.WheelStyle==R3WheelStyle.Offroad);
        public static bool ValidFootprint(R3PartState part)=>part!=null&&(part.FootprintRevision==0||
            (part.FootprintRevision==1&&part.Kind==R3PartKind.Wheel&&HasSizedSideWheel(part.WheelStyle))||
            (part.FootprintRevision==2&&part.Kind==R3PartKind.Gun&&SupportsGunScale2(part.Weapon)));
        public static bool ValidEconomicPart(R3PartState part) => part!=null && ValidEconomicKind(part.Kind,part.Weapon) && ValidWheelStyle(part) && ValidArmorShape(part) && ValidFootprint(part) && part.Tier>=0&&part.Tier<=3&&
            (part.Tier<=1||SupportsFusion(part.Kind,part.Weapon))&&
            !float.IsNaN(part.Health)&&!float.IsInfinity(part.Health)&&part.Health>=0&&part.Health<=1;
        public int Price(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic) => !ValidEconomicKind(kind,weapon)?0:Math.Max(1,
            kind==R3PartKind.Gun&&weapon!=R4WeaponKind.Basic?R4WeaponDefinition.Get(weapon).Price:Economy.Price(kind));
        public bool Owns(string id) => Parts.Concat(inventory).Any(p=>p.Id==id);
        // Mounted wreck plans reserve their type too: rebuilding them must not
        // turn a legal two-type design into a three-type vehicle. Inventory
        // parts remain unrestricted until the player actually mounts them.
        const int MaxWeaponTypes=2;
        const string WeaponTypeLimitError="Maksymalnie 2 różne typy broni na pojeździe.";
        HashSet<R4WeaponKind> MountedWeaponTypes() => new HashSet<R4WeaponKind>(
            Parts.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Weapon));
        bool InvalidWeaponTypes => MountedWeaponTypes().Count>MaxWeaponTypes;
        public string PlacementError(Vector3Int cell, R3PartKind kind, bool owned=false,
            R4WeaponKind weapon=R4WeaponKind.Basic,Vector3Int rotation=default,
            int contactMask=R3MountingGrid.AllFaces)
            => CheckPlacement(cell,kind,owned,weapon,rotation,contactMask,null);
        public string OwnedPlacementError(string id,Vector3Int cell,Vector3Int rotation)
        {
            var part=inventory.FirstOrDefault(p=>p.Id==id);
            return part==null?"Brak części w magazynie.":!ValidEconomicPart(part)?"Nieprawidłowe dane części.":part.Health<=0?"Odtwórz zniszczoną część przed montażem.":
                CheckPlacement(cell,part.Kind,true,part.Weapon,rotation,PlacementContacts(part.Kind,part),part.Id);
        }
        string CheckPlacement(Vector3Int cell,R3PartKind kind,bool owned,R4WeaponKind weapon,Vector3Int rotation,int contactMask,string ownedId)
        {
            if(kind==R3PartKind.Gun)contactMask=PlacementContacts(kind);
            if (kind == R3PartKind.Core) return "Rdzeń jest jedyny i nie można go kupić.";
            if (!Enum.IsDefined(typeof(R3PartKind),kind)) return "Nieznany typ części.";
            if(kind==R3PartKind.Wheel&&!ValidWheelOrientation(rotation))
                return "Koło musi stać pionowo, a jego oś powinna być skierowana na bok pojazdu.";
            if(kind==R3PartKind.Gun&&!Enum.IsDefined(typeof(R4WeaponKind),weapon))return "Nieznany rodzaj broni.";
            var stored=ownedId!=null?inventory.FirstOrDefault(p=>p.Id==ownedId):null;
            var candidate=stored?.Copy()??new R3PartState(cell,kind){Weapon=weapon,
                FootprintRevision=owned?0:NewPurchaseFootprint(kind,weapon,R3WheelStyle.Offroad)};
            candidate.Cell=cell;candidate.RotationQuarterTurns=rotation;candidate.ContactMask=contactMask;
            if(kind==R3PartKind.Wheel&&HasSideMount(candidate.WheelStyle))
                contactMask=candidate.ContactMask=SideWheelContactAt(candidate);
            var footprint=OccupiedCells(candidate).ToArray();
            if(footprint.Any(c=>Math.Abs(c.x)>6||Math.Abs(c.z)>7||c.y<0||c.y>5))return "Moduł wykracza poza siatkę montażową.";
            if(footprint.Any(c=>At(c)!=null))return "Zajęte pole części. Utraconą część odtwórz lub usuń jej plan.";
            if(Parts.Sum(p=>OccupiedCells(p).Count())+footprint.Length>180)return "Limit 180 pól części.";
            if(kind==R3PartKind.BonusModule&&!owned)return "Moduły bonusów zdobywa się wyłącznie jako nagrody.";
            if(kind==R3PartKind.Gun)
            {
                var types=MountedWeaponTypes();
                if(types.Count>MaxWeaponTypes||(types.Count==MaxWeaponTypes&&!types.Contains(weapon)))return WeaponTypeLimitError;
                // A gun has a one-cell clearance around its own 1x1x1 mounting
                // cell. Existing constructions remain loadable and repairable;
                // the rule applies to each new placement, including inventory.
                if(Parts.Any(p=>p.Kind==R3PartKind.Gun&&
                    Math.Abs((long)p.Cell.x-cell.x)<=1&&Math.Abs((long)p.Cell.y-cell.y)<=1&&Math.Abs((long)p.Cell.z-cell.z)<=1))
                    return "Zachowaj co najmniej jedno wolne pole odstępu między broniami.";
            }
            if (kind == R3PartKind.Wheel && Parts.Count(p => p.Kind == kind) >= 16) return "Limit 16 kół.";
            if (kind == R3PartKind.Suspension && Parts.Count(p => p.Kind == kind) >= 8) return "Limit 8 modułów zawieszenia.";
            int limit=kind switch {R3PartKind.Booster=>4,R3PartKind.JumpDrive=>2,R3PartKind.Blink=>1,
                R3PartKind.Gyroscope=>2,R3PartKind.MagneticGrapple=>2,R3PartKind.DirectionalShield=>2,R3PartKind.PointDefense=>2,R3PartKind.StructuralScanner=>1,R3PartKind.ScannerPulse=>1,R3PartKind.IndependentSteering=>1,_=>8};
            if(kind>=R3PartKind.Booster&&kind!=R3PartKind.Battery&&kind!=R3PartKind.RepairPulse&&kind!=R3PartKind.DrivePower&&kind!=R3PartKind.EnergyRegenerator&&Parts.Count(p=>p.Kind==kind)>=limit)return "Limit modułów: "+limit+".";
            if(kind==R3PartKind.CrawlerTrack&&cell.x==0)return "Gąsienicę zamontuj po lewej lub prawej stronie.";
            if (!HasAliveExternalNeighbour(cell,kind,rotation,contactMask,candidate))
                return "Brak zgodnych punktów mocowania ze sprawną konstrukcją.";
            if (!owned && Scrap < Price(kind,weapon)) return "Za mało Złomu.";
            if(installationGate==null)return null;
            return installationGate(candidate);
        }
        public bool Buy(Vector3Int cell,R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic,
            Vector3Int rotation=default)
        {
            rotation=R3MountingGrid.Normalize(rotation);
            if(kind==R3PartKind.BonusModule||PlacementError(cell,kind,false,weapon,rotation)!=null)return false;
            Scrap-=Price(kind,weapon);
            var part=new R3PartState(cell,kind){Weapon=weapon,RotationQuarterTurns=rotation,ContactMask=PlacementContacts(kind),
                FootprintRevision=NewPurchaseFootprint(kind,weapon,R3WheelStyle.Offroad)};
            if(kind==R3PartKind.Wheel&&HasSideMount(part.WheelStyle))part.ContactMask=SideWheelContactAt(part);
            Parts.Add(part);
            return true;
        }
        public bool PurchaseToInventory(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic,R3WheelStyle wheelStyle=R3WheelStyle.Offroad,R3ArmorShape armorShape=R3ArmorShape.Cube)
        {
            if(kind==R3PartKind.Core||kind==R3PartKind.BonusModule||!ValidEconomicKind(kind,weapon)||inventory.Count>=InventoryCapacity||
                !Enum.IsDefined(typeof(R3WheelStyle),wheelStyle)||(kind!=R3PartKind.Wheel&&wheelStyle!=R3WheelStyle.Offroad)||
                (HasSizedSideWheel(wheelStyle)&&!R4_1.R4WheelArt.SizedWheelAvailable(wheelStyle))||
                (HasForkMount(wheelStyle)&&!R4_1.R4WheelArt.ForkWheelAvailable(wheelStyle))||
                !Enum.IsDefined(typeof(R3ArmorShape),armorShape)||(kind!=R3PartKind.Armor&&armorShape!=R3ArmorShape.Cube)||
                (armorShape!=R3ArmorShape.Cube&&!R4_1.R41PartGeometry.ArmorShapeAvailable(armorShape)))return false;
            int price=Price(kind,weapon);if(price<=0||Scrap<price)return false;
            Scrap-=price;inventory.Add(new R3PartState(default,kind){Weapon=weapon,WheelStyle=wheelStyle,ArmorShape=armorShape,
                FootprintRevision=NewPurchaseFootprint(kind,weapon,wheelStyle),
                ContactMask=kind==R3PartKind.Wheel?(HasForkMount(wheelStyle)?R3MountingGrid.FaceBit(Vector3Int.up):
                    R3MountingGrid.FaceBit(Vector3Int.right)|R3MountingGrid.FaceBit(Vector3Int.left)):
                    armorShape==R3ArmorShape.Cube?R3MountingGrid.AllFaces:R4_1.R41PartGeometry.FullArmorContacts(armorShape)});return true;
        }        public int RepairCost(R3PartState part)
        {
            if (!ValidEconomicPart(part) || part.Health >= 1f) return 0;
            // Apply the existing proportional repair rate continuously to owned wrecks.
            // A one-scrap purchase has no positive, strictly lower integer price;
            // fail closed instead of giving away an owned part or charging full price.
            int purchase=OwnedPartValue(part);
            if(part.Health<=0&&purchase<=1)return 0;
            int quote=Math.Max(1,Mathf.CeilToInt((1-part.Health)*purchase*0.35f));
            return part.Health<=0?Math.Min(purchase-1,quote):quote;
        }
        public bool Repair(Vector3Int cell)
        {
            var part = At(cell); int price = RepairCost(part);
            if (part == null || price <= 0 || Scrap < price) return false;
            if(part.Kind==R3PartKind.Gun&&InvalidWeaponTypes)return false;
            if (part.Health<=0&&!HasAliveExternalNeighbour(part.Cell,part.Kind,
                part.RotationQuarterTurns,part.ContactMask,part))return false;
            if(part.Health<=0&&restorationGate!=null&&!restorationGate(new[]{part}))return false;
            Scrap -= price; part.Health = 1; return true;
        }
        public int RepairAffordable()
        {
            int count = 0; bool changed;
            do
            {
                changed = false;
                foreach (var part in Parts.OrderBy(p => p.Kind == R3PartKind.Core ? 0 : p.Kind == R3PartKind.Gun ? 1 : p.Kind == R3PartKind.Wheel ? 2 : 3))
                    if (Repair(part.Cell)) { count++; changed = true; }
            } while (changed);
            return count;
        }
        public int AffordableRepairCost
        {
            get
            {
                var preview=DeepCopy();
                if(restorationGate!=null)
                {
                    // Validate every hypothetical rebuilt part against earlier
                    // hypothetical repairs as one batch. Keep snapshots dead so
                    // the physical gate also checks their recreated geometry.
                    var restored=new List<R3PartState>();
                    preview.restorationGate=parts=>
                    {
                        var batch=restored.Concat(parts).ToArray();
                        if(!restorationGate(batch))return false;
                        restored.AddRange(parts.Select(p=>p.Copy()));
                        return true;
                    };
                }
                preview.RepairAffordable();return Scrap-preview.Scrap;
            }
        }
        public bool Remove(Vector3Int cell)
        {
            var part = At(cell);
            if (part == null || part.Kind == R3PartKind.Core) return false;
            var rest = Parts.Where(p => p != part).ToList();
            // Edits may temporarily be undeployable; keep the core, structure and inventory safe.
            if (inventory.Count>=InventoryCapacity ||
                !Connected(rest) || !Connected(rest.Where(p => p.Health > 0))) return false;
            Parts.Remove(part);part.ContactMask=StoredWheelContacts(part);NormalizeStoredEquipmentFootprint(part);inventory.Add(part); return true;
        }
        public bool RotateMounted(Vector3Int cell,Vector3Int quarterTurns)
        {
            var part=At(cell);
            if(part==null||part.Kind==R3PartKind.Core||part.Health<=0)return false;
            var oldRotation=part.RotationQuarterTurns;var oldContacts=part.ContactMask;
            var next=R3MountingGrid.Normalize(oldRotation+quarterTurns);
            if(next==oldRotation)return false;
            if(part.Kind==R3PartKind.Wheel&&!ValidWheelOrientation(next))return false;
            bool accepted=false;
            try
            {
                part.RotationQuarterTurns=next;
                if(part.Kind==R3PartKind.Gun)part.ContactMask=PlacementContacts(part.Kind,part);
                else if(part.Kind==R3PartKind.Wheel&&HasSideMount(part.WheelStyle))
                    part.ContactMask=SideWheelContactAt(part);
                var cells=OccupiedCells(part).ToArray();
                if(cells.Any(c=>Math.Abs(c.x)>6||Math.Abs(c.z)>7||c.y<0||c.y>5))return false;
                var otherCells=new HashSet<Vector3Int>(Parts.Where(p=>p!=part).SelectMany(OccupiedCells));
                if(cells.Any(otherCells.Contains)||!Connected(Parts)||!Connected(Parts.Where(p=>p.Health>0)))return false;
                if(installationGate!=null&&installationGate(part)!=null)return false;
                accepted=true;return true;
            }
            finally {if(!accepted){part.RotationQuarterTurns=oldRotation;part.ContactMask=oldContacts;}}
        }
        public bool ReceiveSalvage(IEnumerable<R3PartState> recovered)
        {
            if(recovered==null)return false;
            var batch=recovered.ToArray();
            if(inventory.Count+batch.Length>InventoryCapacity)return false;
            if(batch.Any(p=>!ValidEconomicPart(p)||p.Kind==R3PartKind.Armor||p.Kind==R3PartKind.Core||
                (p.Kind==R3PartKind.BonusModule&&(string.IsNullOrWhiteSpace(p.UpgradeId)||
                    !Enum.IsDefined(typeof(R1UpgradeEffect),p.UpgradeEffect)||float.IsNaN(p.UpgradeAmount)||
                    float.IsInfinity(p.UpgradeAmount)||p.UpgradeAmount<0||p.UpgradeAmount>1))||
                string.IsNullOrWhiteSpace(p.Id)||Owns(p.Id))||
                batch.Select(p=>p.Id).Distinct().Count()!=batch.Length)return false;
            inventory.AddRange(batch.Select(p=>{var copy=p.Copy();copy.ContactMask=StoredWheelContacts(copy);NormalizeStoredEquipmentFootprint(copy);return copy;}));return true;
        }
        public static R3PartState CreateBonusReward(R1UpgradeDefinition bonus)
        {
            if(bonus==null||string.IsNullOrWhiteSpace(bonus.Id)||
                !Enum.IsDefined(typeof(R1UpgradeEffect),bonus.Effect)||
                float.IsNaN(bonus.Amount)||float.IsInfinity(bonus.Amount))return null;
            return new R3PartState(default,R3PartKind.BonusModule){
                Id=Guid.NewGuid().ToString("N"),Source="Bonus za zwycięstwo",
                UpgradeId=bonus.Id,UpgradeTitle=string.IsNullOrWhiteSpace(bonus.Title)?bonus.Id:bonus.Title,
                UpgradeDescription=bonus.Description,UpgradeEffect=bonus.Effect,
                UpgradeAmount=Mathf.Clamp01(bonus.Amount)};
        }
        public bool ReceiveBonuses(IEnumerable<R1UpgradeDefinition> rewards)
        {
            if(rewards==null)return false;
            var parts=rewards.Select(CreateBonusReward).ToArray();
            return ReceiveSalvage(parts);
        }
        public bool Mount(string id,Vector3Int cell)
        {
            var stored=inventory.FirstOrDefault(p=>p.Id==id);
            return stored!=null&&Mount(id,cell,stored.RotationQuarterTurns);
        }
        public bool Mount(string id,Vector3Int cell,Vector3Int rotation)
        {
            var part=inventory.FirstOrDefault(p=>p.Id==id);
            rotation=R3MountingGrid.Normalize(rotation);
            if(part==null||part.Health<=0||
                OwnedPlacementError(part.Id,cell,rotation)!=null)return false;
            inventory.Remove(part);part.Cell=cell;part.RotationQuarterTurns=rotation;part.ContactMask=PlacementContacts(part.Kind,part);
            if(part.Kind==R3PartKind.Wheel&&HasSideMount(part.WheelStyle))part.ContactMask=SideWheelContactAt(part);
            Parts.Add(part);return true;
        }
        public bool RepairInventory(string id)
        {
            var part=inventory.FirstOrDefault(p=>p.Id==id);int cost=RepairCost(part);
            if(part==null||cost<=0||Scrap<cost)return false;
            Scrap-=cost;part.Health=1;return true;
        }
        int OwnedPartValue(R3PartState part)=>Price(part.Kind,part.Weapon)*(NormalizedTier(part.Tier)==3?9:NormalizedTier(part.Tier)==2?3:1);
        public int DismantleValue(R3PartState part) => !ValidEconomicPart(part)?0:Mathf.FloorToInt(OwnedPartValue(part)*0.25f*Mathf.Clamp01(part.Health));
        public static bool SameFusionType(R3PartState a,R3PartState b) => a!=null&&b!=null&&a.Kind==b.Kind&&
            NormalizedTier(a.Tier)==NormalizedTier(b.Tier)&&
            (a.Kind!=R3PartKind.Gun||(a.Weapon==b.Weapon&&a.FootprintRevision==b.FootprintRevision))&&
            (a.Kind!=R3PartKind.Wheel||(a.WheelStyle==b.WheelStyle&&a.FootprintRevision==b.FootprintRevision))&&
            (a.Kind!=R3PartKind.Armor||a.ArmorShape==b.ArmorShape)&&
            (a.Kind!=R3PartKind.BonusModule||(!string.IsNullOrWhiteSpace(a.UpgradeId)&&a.UpgradeId==b.UpgradeId&&
                Enum.IsDefined(typeof(R1UpgradeEffect),a.UpgradeEffect)&&a.UpgradeEffect==b.UpgradeEffect&&
                !float.IsNaN(a.UpgradeAmount)&&!float.IsInfinity(a.UpgradeAmount)&&a.UpgradeAmount>=0&&a.UpgradeAmount<=1&&a.UpgradeAmount==b.UpgradeAmount));
        public bool FuseInventory(IEnumerable<string> ids)
        {
            if(ids==null)return false;
            var requested=ids.ToArray();
            if(requested.Length!=3||requested.Any(string.IsNullOrWhiteSpace)||requested.Distinct(StringComparer.Ordinal).Count()!=3)return false;
            var parts=requested.Select(id=>inventory.FirstOrDefault(p=>p.Id==id)).ToArray();
            if(parts.Any(p=>!ValidEconomicPart(p))||!SupportsFusion(parts[0].Kind,parts[0].Weapon))return false;
            if(parts.Any(p=>float.IsNaN(p.DeviceReadyAt)||float.IsInfinity(p.DeviceReadyAt)||p.DeviceReadyAt<0||
                float.IsNaN(p.ReloadReadyAt)||float.IsInfinity(p.ReloadReadyAt)||p.ReloadReadyAt<0||
                float.IsNaN(p.DisruptionRemaining)||float.IsInfinity(p.DisruptionRemaining)||p.DisruptionRemaining<0||
                float.IsNaN(p.ThermalHeat)||float.IsInfinity(p.ThermalHeat)||p.ThermalHeat<0))return false;
            int tier=NormalizedTier(parts[0].Tier);
            if(tier>=3||parts.Any(p=>!SameFusionType(parts[0],p)))return false;
            // A new physical item represents all three inputs. Preserve average
            // condition so fusion never repairs damaged items for free.
            var result=parts[0].Copy();result.Id=Guid.NewGuid().ToString("N");result.Tier=tier+1;
            result.Health=parts.Average(p=>p.Health);result.Source="Fuzja trzech części T"+tier;
            result.DeviceReadyAt=parts.Max(p=>p.DeviceReadyAt);result.ReloadReadyAt=parts.Max(p=>p.ReloadReadyAt);
            result.DisruptionRemaining=parts.Max(p=>p.DisruptionRemaining);
            result.ThermalHeat=parts.Max(p=>p.ThermalHeat);result.ThermalLocked=parts.Any(p=>p.ThermalLocked);
            if(result.Kind==R3PartKind.Gun)
            {
                int capacity=result.Weapon==R4WeaponKind.Shotgun?RoboRougelike.WeaponIntegration.RRLShotgunBinding.MagazineSalvos:
                    result.Weapon==R4WeaponKind.Railgun?RoboRougelike.WeaponIntegration.RRLRailgunBinding.Magazine:
                    result.Weapon==R4WeaponKind.RocketLauncher?RoboRougelike.WeaponIntegration.RRLRocketBinding.Magazine:0;
                if(capacity>0&&!parts.All(p=>p.Ammo<0))
                    result.Ammo=parts.Min(p=>p.Ammo<0?capacity:Mathf.Clamp(p.Ammo,0,capacity));
            }
            foreach(var part in parts)inventory.Remove(part);
            inventory.Add(result);return true;
        }
        public bool Dismantle(string id)
        {
            var part=inventory.FirstOrDefault(p=>p.Id==id);if(!ValidEconomicPart(part))return false;
            int quote=DismantleValue(part);
            inventory.Remove(part);weaponGroupPreference.Remove(part.Id);Award(quote);return true;
        }
        public static bool Connected(IEnumerable<R3PartState> parts)
        {
            var cells=new Dictionary<Vector3Int,R3PartState>();
            foreach(var part in parts)
                foreach(var cell in OccupiedCells(part))
                    if(!cells.TryAdd(cell,part))return false;
            var core = cells.Values.FirstOrDefault(p => p.Kind == R3PartKind.Core);
            if (core == null) return false;
            var visited = new HashSet<Vector3Int> { core.Cell }; var queue = new Queue<Vector3Int>(); queue.Enqueue(core.Cell);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var n in Neighbours)
                { var next=current+n;
                    if(cells.TryGetValue(next,out var other)&&!visited.Contains(next)&&
                        R3MountingGrid.Compatible(cells[current],current,other,next))
                    {visited.Add(next);queue.Enqueue(next);}
                }
            }
            return visited.Count == cells.Count;
        }
        private static string MissingDriveRequirement(R3DriveKind drive)=>drive switch {
            R3DriveKind.Wheels=>"Koła: po jednym z obu stron.",
            R3DriveKind.Tracks=>"Gąsienice: po jednej z obu stron.",
            R3DriveKind.Omni=>"Zamontuj sprawny moduł Omni.",
            R3DriveKind.Hover=>"Zamontuj sprawny silnik Hover.",
            _=>"Wybierz dostępny napęd."};
        public string DeployError
        {
            get
            {
                if(InvalidWeaponTypes)return WeaponTypeLimitError;
                var alive = Parts.Where(p => p.Health > 0).ToList();
                if (!alive.Any(p => p.Kind == R3PartKind.Core)) return "Rdzeń zniszczony.";
                if (!Connected(alive)) return "Konstrukcja zawiera odłączone części.";
                if (!alive.Any(p => p.Kind == R3PartKind.Gun)) return "Brak sprawnej broni.";
                if(!HasMobility(alive,SelectedDrive))return "Brak sprawnego napędu. "+MissingDriveRequirement(SelectedDrive);
                return null;
            }
        }
        public bool CanDeploy => DeployError == null;
        // Critical garage warnings use only live parts physically connected to
        // the live core. They do not change the existing deployment policy.
        public string CriticalReadinessMessage
        {
            get
            {
                var alive=Parts.Where(p=>p!=null&&p.Health>0).ToList();
                var core=alive.FirstOrDefault(p=>p.Kind==R3PartKind.Core);
                if(core==null)return "Rdzeń zniszczony.";
                var cells=new Dictionary<Vector3Int,R3PartState>();
                foreach(var part in alive)foreach(var cell in OccupiedCells(part))cells.TryAdd(cell,part);
                var visited=new HashSet<Vector3Int>();var queue=new Queue<Vector3Int>();
                if(core!=null){visited.Add(core.Cell);queue.Enqueue(core.Cell);}
                while(queue.Count>0)
                {
                    var current=queue.Dequeue();
                    foreach(var direction in Neighbours)
                    {
                        var next=current+direction;
                        if(cells.TryGetValue(next,out var other)&&!visited.Contains(next)&&
                            R3MountingGrid.Compatible(cells[current],current,other,next))
                        {visited.Add(next);queue.Enqueue(next);}
                    }
                }
                var operational=visited.Select(c=>cells[c]).Distinct().ToArray();
                bool noWeapon=!operational.Any(p=>p.Kind==R3PartKind.Gun&&ValidEconomicKind(p.Kind,p.Weapon));
                bool noDrive=!HasMobility(operational,SelectedDrive);
                if(noWeapon&&noDrive)return "Brak sprawnej broni • Brak sprawnego napędu. "+MissingDriveRequirement(SelectedDrive);
                if(noWeapon)return "Brak sprawnej broni.";
                return noDrive?"Brak sprawnego napędu. "+MissingDriveRequirement(SelectedDrive):null;
            }
        }
        public static bool HasMobility(IEnumerable<R3PartState> source,R3DriveKind drive,bool healthy=true)
        {
            var parts=source.Where(p=>!healthy||p.Health>0);
            if(drive==R3DriveKind.Wheels||drive==R3DriveKind.Tracks)
            {
                var kind=drive==R3DriveKind.Wheels?R3PartKind.Wheel:R3PartKind.CrawlerTrack;
                return parts.Any(p=>p.Kind==kind&&p.Cell.x<0)&&parts.Any(p=>p.Kind==kind&&p.Cell.x>0);
            }
            return drive==R3DriveKind.Omni?parts.Any(p=>p.Kind==R3PartKind.OmniDrive):
                drive==R3DriveKind.Hover&&parts.Any(p=>p.Kind==R3PartKind.HoverEngine);
        }
        public bool EmergencyRepair()
        {
            if(InvalidWeaponTypes)return false;
            var plan=EmergencyRepairPlan();
            if(plan==null||plan.Count==0||plan.Any(p=>!ValidEconomicPart(p)||RepairCost(p)<=0))return false;
            int price=plan.Sum(RepairCost);
            if(Scrap<price)return false;
            if(restorationGate!=null&&!restorationGate(plan))return false;
            Scrap-=price;
            foreach(var part in plan)part.Health=1;
            return true;
        }
        public int EmergencyRepairCost => EmergencyRepairPlan()?.Sum(RepairCost)??0;
        private List<R3PartState> EmergencyRepairPlan()
        {
            if (InvalidWeaponTypes || CanDeploy || !Parts.Any(p => p.Kind == R3PartKind.Core && p.Health > 0)) return null;
            var byCell=Parts.SelectMany(p=>OccupiedCells(p).Select(cell=>new{cell,part=p})).ToDictionary(x=>x.cell,x=>x.part); var core = Parts.Single(p => p.Kind == R3PartKind.Core);
            var previous = new Dictionary<Vector3Int, Vector3Int> { [core.Cell] = core.Cell };
            var costs = new Dictionary<Vector3Int,long> { [core.Cell]=0 };
            var pending = new List<Vector3Int>{core.Cell};
            while (pending.Count > 0)
            {
                var current=pending.OrderBy(cell=>costs[cell]).First();pending.Remove(current);
                foreach (var n in Neighbours)
                {
                    var next=current+n;
                    if(!byCell.ContainsKey(next)||
                        !R3MountingGrid.Compatible(byCell[current],current,byCell[next],next))continue;
                    var nextPart=byCell[next];
                    bool enteringWreck=nextPart.Health<=0&&!ReferenceEquals(nextPart,byCell[current]);
                    int repairCost=enteringWreck?RepairCost(nextPart):0;
                    // A zero quote on a wreck means it cannot be repaired;
                    // do not prefer an unusable route over a payable one.
                    if(enteringWreck&&repairCost<=0)continue;
                    long cost=costs[current]+repairCost;
                    if(costs.TryGetValue(next,out long old)&&old<=cost)continue;
                    costs[next]=cost;previous[next]=current;
                    if(!pending.Contains(next))pending.Add(next);
                }
            }
            Func<R3PartState,bool> reachable=p=>costs.ContainsKey(p.Cell);
            // Deployment requires every surviving fragment to reconnect. If
            // an unrepairable bridge prevents that, do not charge for a plan
            // that would leave the vehicle just as undeployable.
            if(Parts.Any(p=>p.Health>0&&!reachable(p)))return null;
            Func<R3PartState,int> distance=p=>Math.Abs(p.Cell.x-core.Cell.x)+Math.Abs(p.Cell.y-core.Cell.y)+Math.Abs(p.Cell.z-core.Cell.z);
            var required = new List<R3PartState>();
            if(!HasMobility(Parts,SelectedDrive,false))return null;
            var mobility=SelectedDrive==R3DriveKind.Tracks?R3PartKind.CrawlerTrack:SelectedDrive==R3DriveKind.Omni?R3PartKind.OmniDrive:
                SelectedDrive==R3DriveKind.Hover?R3PartKind.HoverEngine:R3PartKind.Wheel;
            var selectors=new List<Func<R3PartState,bool>>{p=>p.Kind==R3PartKind.Gun};
            if(SelectedDrive==R3DriveKind.Wheels||SelectedDrive==R3DriveKind.Tracks)
            {selectors.Add(p=>p.Kind==mobility&&p.Cell.x<0);selectors.Add(p=>p.Kind==mobility&&p.Cell.x>0);}
            else selectors.Add(p=>p.Kind==mobility);
            foreach(var selector in selectors)
            {
                var target=Parts.Where(p=>selector(p)&&reachable(p)).OrderBy(p=>costs[p.Cell]).ThenBy(distance).FirstOrDefault();
                if(target==null)return null;required.Add(target);
            }
            var repair=new HashSet<R3PartState>();
            foreach (var target in required)
                for (var cell = target.Cell; previous.ContainsKey(cell); cell = previous[cell])
                {
                    if (byCell[cell].Health <= 0) repair.Add(byCell[cell]);
                    if (cell == core.Cell) break;
                }
            // A disconnected live fragment also needs its bridge restored before deployment.
            foreach(var target in Parts.Where(p=>p.Health>0&&reachable(p)))
                for(var cell=target.Cell;cell!=core.Cell;cell=previous[cell])
                    if(byCell[cell].Health<=0)repair.Add(byCell[cell]);
            return repair.ToList();
        }
    }
}

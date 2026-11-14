using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R4;
using RoboRougelike.R1;

namespace RoboRougelike.R3
{
    // Append new values: checkpoints store these numeric IDs.
    public enum R3PartKind { Armor, Wheel, Gun, Core, Suspension, Booster, JumpDrive, Blink, Gyroscope, OmniDrive, MagneticGrapple, HoverEngine, CrawlerTrack, BonusModule, DirectionalShield, PointDefense, StructuralScanner, ScannerPulse, IndependentSteering, Battery, RepairPulse, DrivePower, EnergyRegenerator, SalvageOptimizer, Overdrive, RapidFire, DoubleShot, DoubleShotOverdrive, TargetedEmp, AreaEmp, ChargeCapacity, ChargeRegenerator, RechargePulse, RampDeployer, WallDeployer, JumpPadDeployer, BlastTrapDeployer, EmpTrapDeployer, RepelTrapDeployer, DamageTrapDeployer, SlowTrapDeployer, ImmobilizeTrapDeployer, FlyingDroneGenerator, GroundDroneGenerator, OffensiveTurretGenerator, HealingTurretGenerator, SupportTurretGenerator, ShieldTurretGenerator }
    public enum R3WheelStyle { Offroad, Slick, LargeOffroad, Monster, SmallFork, BalloonFork, SpikedFork, TwinRoad, SteerRoad, SteerOffroad, CasterSlick, SwingOffroad }
    // Zero preserves the shape of every pre-existing checkpoint.
    public enum R3ArmorShape { Cube, Wedge, HalfBlock, Pyramid, RoundEdge, HalfWedge, Dome }
    public enum R3DriveKind { Wheels, Tracks, Omni, Hover }
    public enum R3VehicleKind { Standard, Light, Heavy }
    public enum R3ConstructionSize { Small, Medium, Large }
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
        // A discounted vendor pack can have a lower total scrap value than
        // its individual blocks would otherwise yield. Old saves default to
        // false, retaining their original dismantling value.
        public bool HasDismantleCap;
        public int DismantleCap;
        public R4WeaponKind Weapon;
        public R3WheelStyle WheelStyle;
        // Zero is the one-cell footprint written by all existing saves.
        // Revision 1 reserves a 3x3 tire plane for large side wheels.
        public int FootprintRevision;
        public R3ArmorShape ArmorShape;
        // A bonus is a physical, single inventory item occupying one grid cell.
        public string UpgradeId, UpgradeTitle, UpgradeDescription;
        public R1UpgradeEffect UpgradeEffect;
        public R1ModifierOperation UpgradeOperation;
        public R1BonusScope UpgradeScope;
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
        public float DefenseCharge=-1,DefenseRechargeAge=float.PositiveInfinity,DefenseCapacity;
        public R3PartState(Vector3Int cell, R3PartKind kind, float health = 1) { Cell = cell; Kind = kind; Health = health; }
        public R3PartState Copy() => new R3PartState(Cell, Kind, Health){Id=Id,Source=Source,HasDismantleCap=HasDismantleCap,DismantleCap=DismantleCap,Weapon=Weapon,WheelStyle=WheelStyle,FootprintRevision=FootprintRevision,ArmorShape=ArmorShape,Tier=Tier,SteeringMode=SteeringMode,DeviceReadyAt=DeviceReadyAt,Ammo=Ammo,ReloadReadyAt=ReloadReadyAt,ThermalHeat=ThermalHeat,ThermalLocked=ThermalLocked,DisruptionRemaining=DisruptionRemaining,DefenseCharge=DefenseCharge,DefenseRechargeAge=DefenseRechargeAge,DefenseCapacity=DefenseCapacity,
            UpgradeId=UpgradeId,UpgradeTitle=UpgradeTitle,UpgradeDescription=UpgradeDescription,
            UpgradeEffect=UpgradeEffect,UpgradeOperation=UpgradeOperation,UpgradeScope=UpgradeScope,UpgradeAmount=UpgradeAmount,
            RotationQuarterTurns=RotationQuarterTurns,ContactMask=ContactMask};
    }
    public sealed class R3VehicleDesign
    {
        public string Name, Description;
        public float Speed, TurnSpeed, CoreHp = 100;
        public readonly List<R3PartState> Parts = new List<R3PartState>();
    }
    [Serializable]
    public sealed class R3ConstructionSettings
    {
        public int StartingBudget=200, StarterReserve=20, LightStarterExtraReserve=40, BlockCost=2, HalfBlockCost=1, WeaponCost=6, WeaponTierTwoCost=8, WeaponTierThreeCost=10;
        public int WheelCost=2, ActiveModuleCost=4, PassiveModuleCost=3, DriveModuleCost=3;
        public int TierTwoMultiplier=2, TierThreeMultiplier=3;
        public int StageInterval=2, StageBudgetIncrease=20, SmallPhysicalParts=40, MediumPhysicalParts=80;
        public R3ConstructionSettings Copy()=>new R3ConstructionSettings {
            StartingBudget=StartingBudget,StarterReserve=StarterReserve,LightStarterExtraReserve=LightStarterExtraReserve,BlockCost=BlockCost,HalfBlockCost=HalfBlockCost,WeaponCost=WeaponCost,
            WeaponTierTwoCost=WeaponTierTwoCost,WeaponTierThreeCost=WeaponTierThreeCost,
            WheelCost=WheelCost,ActiveModuleCost=ActiveModuleCost,PassiveModuleCost=PassiveModuleCost,
            DriveModuleCost=DriveModuleCost,TierTwoMultiplier=TierTwoMultiplier,TierThreeMultiplier=TierThreeMultiplier,
            StageInterval=StageInterval,StageBudgetIncrease=StageBudgetIncrease,SmallPhysicalParts=SmallPhysicalParts,MediumPhysicalParts=MediumPhysicalParts};
        public int Cost(R3PartState part)
        {
            if(part==null)return 0;
            int tier=R3VehicleModel.NormalizedTier(part.Tier);
            if(part.Kind==R3PartKind.Gun)return Math.Max(0,tier==3?WeaponTierThreeCost:tier==2?WeaponTierTwoCost:WeaponCost);
            int baseCost=part.Kind==R3PartKind.Armor?
                (part.ArmorShape==R3ArmorShape.HalfBlock||part.ArmorShape==R3ArmorShape.HalfWedge?HalfBlockCost:BlockCost):
                part.Kind==R3PartKind.Core?BlockCost:
                part.Kind==R3PartKind.Wheel?WheelCost:(R3VehicleModel.IsActiveSlotPart(part.Kind)||R3VehicleModel.IsDeployableSlotPart(part.Kind))?ActiveModuleCost:
                R3VehicleModel.IsPassiveSlotPart(part.Kind)?PassiveModuleCost:DriveModuleCost;
            int multiplier=tier==3?TierThreeMultiplier:tier==2?TierTwoMultiplier:1;
            return (int)Math.Min(int.MaxValue,(long)Math.Max(0,baseCost)*Math.Max(0,multiplier));
        }
    }
    [Serializable]
    public sealed class R3GarageEconomy
    {
        public int ArmorCost = 5, WheelCost = 15, GunCost = 30, MaxRepairCost = 3;
        [Range(1,R3BlueprintLibrary.MaximumSlots)] public int BlueprintSlotCount=R3BlueprintLibrary.DefaultSlots;
        public float StandardPurchaseMultiplier=3f;
        public float SalvageOptimizerBonus=.25f;
        public float PhysicalLootDropChance=.2f;
        public float StructuralScanRange=60f,StructuralScanEnergy=5f,StructuralScanCooldown=8f;
        public int MissedLootRecoveryCost=10;
        public int SafeZoneInterval=2;
        public float SafeZoneRepairFraction=.25f;
        public int SafeVendorOfferCount=4;
        public float SafeVendorPriceMultiplier=.5f;
        public int SalvageOptimizerExtraOffersPerUnit=1;
        public float OverdriveBonusPerUnit=.2f,OverdriveEnergyPerCopy=15f,OverdriveDuration=5f,OverdriveCooldown=15f;
        public float RapidFireReductionPerUnit=.2f,RapidFireEnergyPerCopy=15f,RapidFireDuration=5f,RapidFireCooldown=15f;
        public float DoubleShotChancePerUnit=.2f;
        public float DoubleShotOverdriveEnergyPerCopy=15f,DoubleShotOverdriveDuration=5f,DoubleShotOverdriveCooldown=15f;
        public int DoubleShotOverdriveExtraPerUnit=1;
        public int DeviceBaseCharges=3,CapacityChargesPerUnit=1,RechargePulseChargesPerUnit=1;
        public float ChargesPerSecondPerRegenerator=.05f,RechargePulseEnergyPerCopy=15f,RechargePulseCooldown=12f;
        public float RampEnergyPerCopy=10f,RampCooldown=8f,RampLifetime=30f;
        public float WallEnergyPerCopy=10f,WallCooldown=8f,WallLifetime=30f,JumpPadEnergyPerCopy=10f,JumpPadCooldown=8f,JumpPadLifetime=30f,JumpPadLaunchSpeed=6f;
        public float TrapEnergyPerCopy=10f,TrapCooldown=8f,TrapLifetime=30f,TrapTriggerRadius=3f,TrapBlastDamage=60f,TrapEmpDuration=3f,TrapRepelSpeed=6f,TrapDamagePerSecond=12f,TrapDamageDuration=5f,TrapSlowMultiplier=.5f,TrapSlowDuration=2f,TrapImmobilizeDuration=2f;
        public float DroneEnergyPerCopy=20f,DroneCooldown=5f,DroneLifetime=45f,DroneSpeed=7f,DroneFireInterval=1f,DroneDamage=12f;
        public int DroneActivePerUnit=1;
        public float TurretEnergyPerCopy=20f,TurretCooldown=8f,TurretLifetime=30f,TurretFireInterval=1f,TurretDamage=15f;
        public float HealingTurretRadius=8f,HealingTurretRate=.08f;
        public float SupportTurretRadius=8f,SupportTurretEnergyPerSecond=8f; public float ShieldTurretRadius=5f,ShieldTurretHp=200f;
        public int BlockGrantQuantity=5,DriveGrantQuantity=2;
        public R3ConstructionSettings Construction = new R3ConstructionSettings();
        public int Price(R3PartKind kind) => kind switch {
            R3PartKind.Suspension=>25, R3PartKind.Wheel=>WheelCost, R3PartKind.Gun=>GunCost,
            R3PartKind.Booster=>35, R3PartKind.JumpDrive=>45, R3PartKind.Blink=>60,
            R3PartKind.Gyroscope=>30, R3PartKind.OmniDrive=>40, R3PartKind.MagneticGrapple=>45,
            R3PartKind.HoverEngine=>35, R3PartKind.CrawlerTrack=>45,
            R3PartKind.BonusModule=>60,R3PartKind.DirectionalShield=>60,R3PartKind.PointDefense=>70,R3PartKind.StructuralScanner=>40,R3PartKind.ScannerPulse=>30,R3PartKind.IndependentSteering=>45,R3PartKind.Battery=>25,R3PartKind.RepairPulse=>50,R3PartKind.DrivePower=>45,R3PartKind.EnergyRegenerator=>40,R3PartKind.SalvageOptimizer=>40,R3PartKind.Overdrive=>40,R3PartKind.RapidFire=>40,R3PartKind.DoubleShot=>40,R3PartKind.DoubleShotOverdrive=>40,R3PartKind.TargetedEmp=>50,R3PartKind.AreaEmp=>50,R3PartKind.ChargeCapacity=>40,R3PartKind.ChargeRegenerator=>40,R3PartKind.RechargePulse=>40,R3PartKind.RampDeployer=>50,R3PartKind.WallDeployer=>50,R3PartKind.JumpPadDeployer=>50,R3PartKind.BlastTrapDeployer=>50,R3PartKind.EmpTrapDeployer=>50,R3PartKind.RepelTrapDeployer=>50,R3PartKind.DamageTrapDeployer=>50,R3PartKind.SlowTrapDeployer=>50,R3PartKind.ImmobilizeTrapDeployer=>50,R3PartKind.FlyingDroneGenerator=>60,R3PartKind.GroundDroneGenerator=>60,R3PartKind.OffensiveTurretGenerator=>60,R3PartKind.HealingTurretGenerator=>60,R3PartKind.SupportTurretGenerator=>60,R3PartKind.ShieldTurretGenerator=>60, _=>ArmorCost };
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
        public static bool ValidBonusOperation(R1UpgradeEffect effect,R1ModifierOperation operation)=>
            Enum.IsDefined(typeof(R1ModifierOperation),operation)&&
            ((effect!=R1UpgradeEffect.Repair&&effect!=R1UpgradeEffect.HybridBlastRounds)||operation==R1ModifierOperation.Increased);
        public static bool ValidBonusScope(R1UpgradeEffect effect,R1BonusScope scope)=>
            Enum.IsDefined(typeof(R1BonusScope),scope)&&(scope==R1BonusScope.Global||effect==R1UpgradeEffect.Damage);
        public static bool SupportsFusion(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic)=>kind==R3PartKind.Armor||kind==R3PartKind.Wheel||kind==R3PartKind.Suspension||kind==R3PartKind.OmniDrive||kind==R3PartKind.HoverEngine||kind==R3PartKind.CrawlerTrack||kind==R3PartKind.DirectionalShield||kind==R3PartKind.Booster||kind==R3PartKind.MagneticGrapple||kind==R3PartKind.JumpDrive||kind==R3PartKind.Blink||kind==R3PartKind.Gyroscope||kind==R3PartKind.ScannerPulse||kind==R3PartKind.StructuralScanner||
            kind==R3PartKind.Battery||kind==R3PartKind.RepairPulse||kind==R3PartKind.DrivePower||kind==R3PartKind.EnergyRegenerator||kind==R3PartKind.SalvageOptimizer||kind==R3PartKind.Overdrive||kind==R3PartKind.RapidFire||kind==R3PartKind.DoubleShot||kind==R3PartKind.DoubleShotOverdrive||kind==R3PartKind.TargetedEmp||kind==R3PartKind.AreaEmp||kind==R3PartKind.ChargeCapacity||kind==R3PartKind.ChargeRegenerator||kind==R3PartKind.RechargePulse||kind==R3PartKind.RampDeployer||kind==R3PartKind.WallDeployer||kind==R3PartKind.JumpPadDeployer||kind==R3PartKind.BlastTrapDeployer||kind==R3PartKind.EmpTrapDeployer||kind==R3PartKind.RepelTrapDeployer||kind==R3PartKind.DamageTrapDeployer||kind==R3PartKind.SlowTrapDeployer||kind==R3PartKind.ImmobilizeTrapDeployer||kind==R3PartKind.FlyingDroneGenerator||kind==R3PartKind.GroundDroneGenerator||kind==R3PartKind.OffensiveTurretGenerator||kind==R3PartKind.HealingTurretGenerator||kind==R3PartKind.SupportTurretGenerator||kind==R3PartKind.ShieldTurretGenerator||kind==R3PartKind.BonusModule||kind==R3PartKind.PointDefense||
            (kind==R3PartKind.Gun&&weapon>=R4WeaponKind.Basic&&weapon<=R4WeaponKind.EmpEmitter);
        public R3VehicleDesign Design { get; }
        // A disposable workshop model; never serialized into an expedition.
        public bool CreativeMode { get; internal set; }
        public int AvailableInventoryCapacity => CreativeMode ? int.MaxValue : InventoryCapacity;
        public List<R3PartState> Parts { get; }
        private readonly List<R3PartState> inventory=new List<R3PartState>();
        public IReadOnlyList<R3PartState> Inventory => inventory;
        public int Scrap { get; private set; }
        public VehicleEnergyState Energy { get; } = new VehicleEnergyState();
        public VehicleChargeState Charges { get; } = new VehicleChargeState();
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
        R4WeaponKind? weaponGroupA, weaponGroupB, weaponGroupC, weaponGroupD;
        readonly Dictionary<string,bool> weaponGroupPreference=new Dictionary<string,bool>(StringComparer.Ordinal);
        readonly Dictionary<string,int> weaponSlotPreference=new Dictionary<string,int>(StringComparer.Ordinal);
        readonly R3PartKind?[] activeSlots=new R3PartKind?[4];
        readonly Dictionary<R3PartKind,int> activeSlotPreference=new Dictionary<R3PartKind,int>();
        readonly R3PartKind?[] deployableSlots=new R3PartKind?[4];
        readonly Dictionary<R3PartKind,int> deployableSlotPreference=new Dictionary<R3PartKind,int>();
        public static bool IsDeployableSlotPart(R3PartKind kind)=>kind==R3PartKind.RampDeployer||kind==R3PartKind.WallDeployer||kind==R3PartKind.JumpPadDeployer||kind==R3PartKind.BlastTrapDeployer||kind==R3PartKind.EmpTrapDeployer||kind==R3PartKind.RepelTrapDeployer||kind==R3PartKind.DamageTrapDeployer||kind==R3PartKind.SlowTrapDeployer||kind==R3PartKind.ImmobilizeTrapDeployer||kind==R3PartKind.FlyingDroneGenerator||kind==R3PartKind.GroundDroneGenerator||kind==R3PartKind.OffensiveTurretGenerator||kind==R3PartKind.HealingTurretGenerator||kind==R3PartKind.SupportTurretGenerator||kind==R3PartKind.ShieldTurretGenerator;
        public static bool IsActiveSlotPart(R3PartKind kind)=>
            kind==R3PartKind.Booster||kind==R3PartKind.JumpDrive||kind==R3PartKind.Blink||
            kind==R3PartKind.MagneticGrapple||kind==R3PartKind.StructuralScanner||
            kind==R3PartKind.ScannerPulse||kind==R3PartKind.RepairPulse||kind==R3PartKind.Overdrive||kind==R3PartKind.RapidFire||kind==R3PartKind.DoubleShotOverdrive||kind==R3PartKind.TargetedEmp||kind==R3PartKind.AreaEmp||kind==R3PartKind.RechargePulse;
        public static bool IsPassiveSlotPart(R3PartKind kind)=>
            kind==R3PartKind.Suspension||kind==R3PartKind.Gyroscope||kind==R3PartKind.BonusModule||
            kind==R3PartKind.DirectionalShield||kind==R3PartKind.PointDefense||kind==R3PartKind.IndependentSteering||
            kind==R3PartKind.Battery||kind==R3PartKind.DrivePower||kind==R3PartKind.EnergyRegenerator||kind==R3PartKind.SalvageOptimizer||kind==R3PartKind.DoubleShot||kind==R3PartKind.ChargeCapacity||kind==R3PartKind.ChargeRegenerator;
        static string ModuleTypeKey(R3PartState part)=>part.Kind==R3PartKind.BonusModule?
            "bonus:"+(part.UpgradeId??string.Empty):"part:"+(int)part.Kind;
        bool ExceedsModuleTypeLimit(R3PartState candidate,bool active)
        {
            var types=Parts.Where(p=>active?IsActiveSlotPart(p.Kind):IsPassiveSlotPart(p.Kind))
                .Select(ModuleTypeKey).Distinct().ToArray();
            return types.Length>=(active?4:8)&&!types.Contains(ModuleTypeKey(candidate));
        }
        public R3PartKind?[] GetActiveSlots()
        {
            var types=Parts.Where(p=>IsActiveSlotPart(p.Kind)).Select(p=>p.Kind).Distinct().ToArray();
            for(int i=0;i<activeSlots.Length;i++)if(activeSlots[i].HasValue&&!types.Contains(activeSlots[i].Value))activeSlots[i]=null;
            foreach(var kind in types)
            {
                if(activeSlots.Contains(kind))continue;
                int available=activeSlotPreference.TryGetValue(kind,out var old)&&old>=0&&old<4&&!activeSlots[old].HasValue?
                    old:Array.FindIndex(activeSlots,p=>!p.HasValue);
                if(available>=0){activeSlots[available]=kind;activeSlotPreference[kind]=available;}
            }
            return (R3PartKind?[])activeSlots.Clone();
        }
        public bool AssignActiveSlot(R3PartKind kind,int slot)
        {
            if(slot<0||slot>=4)return false;
            GetActiveSlots();int old=Array.FindIndex(activeSlots,p=>p==kind);
            if(old<0&&(!CreativeMode||!Parts.Any(p=>p.Kind==kind&&IsActiveSlotPart(p.Kind))))return false;
            var displaced=activeSlots[slot];activeSlots[slot]=kind;if(old>=0)activeSlots[old]=displaced;
            for(int i=0;i<4;i++)if(activeSlots[i].HasValue)activeSlotPreference[activeSlots[i].Value]=i;
            return true;
        }
        public string ActiveSlotLabel(R3PartKind kind)
        {
            int slot=Array.FindIndex(GetActiveSlots(),p=>p==kind);
            return slot<0?"—":RoboRougelike.R1.R1ControlProfile.Display(RoboRougelike.R1.R1ControlProfile.Current.Binding("Active"+(slot+1)));
        }
        public R3PartKind?[] GetDeployableSlots()
        {
            var types=Parts.Where(p=>IsDeployableSlotPart(p.Kind)).Select(p=>p.Kind).Distinct().ToArray();
            for(int i=0;i<deployableSlots.Length;i++)if(deployableSlots[i].HasValue&&!types.Contains(deployableSlots[i].Value))deployableSlots[i]=null;
            foreach(var kind in types)
            {
                if(deployableSlots.Contains(kind))continue;
                int available=deployableSlotPreference.TryGetValue(kind,out var old)&&old>=0&&old<4&&!deployableSlots[old].HasValue?
                    old:Array.FindIndex(deployableSlots,p=>!p.HasValue);
                if(available>=0){deployableSlots[available]=kind;deployableSlotPreference[kind]=available;}
            }
            return (R3PartKind?[])deployableSlots.Clone();
        }
        public bool AssignDeployableSlot(R3PartKind kind,int slot)
        {
            if(slot<0||slot>=4)return false;
            GetDeployableSlots();int old=Array.FindIndex(deployableSlots,p=>p==kind);
            if(old<0&&(!CreativeMode||!Parts.Any(p=>p.Kind==kind&&IsDeployableSlotPart(p.Kind))))return false;
            var displaced=deployableSlots[slot];deployableSlots[slot]=kind;if(old>=0)deployableSlots[old]=displaced;
            for(int i=0;i<4;i++)if(deployableSlots[i].HasValue)deployableSlotPreference[deployableSlots[i].Value]=i;
            return true;
        }
        public string DeployableSlotLabel(R3PartKind kind)
        {
            int slot=Array.FindIndex(GetDeployableSlots(),p=>p==kind);
            return slot<0?"—":RoboRougelike.R1.R1ControlProfile.Display(RoboRougelike.R1.R1ControlProfile.Current.Binding("Deploy"+(slot+1)));
        }
        public R4WeaponKind? WeaponGroupA { get { SyncWeaponGroups(); return weaponGroupA; } }
        public R4WeaponKind? WeaponGroupB { get { SyncWeaponGroups(); return weaponGroupB; } }
        public R4WeaponKind? WeaponAtSlot(int slot)
        {
            if(slot<0||slot>=4)return null;
            SyncWeaponGroups();return CurrentWeaponSlots()[slot];
        }
        public R4WeaponKind?[] GetWeaponSlots(){SyncWeaponGroups();return CurrentWeaponSlots();}
        R4WeaponKind?[] CurrentWeaponSlots()=>new[]{weaponGroupA,weaponGroupB,weaponGroupC,weaponGroupD};
        void SetWeaponSlots(R4WeaponKind?[] slots)
        {weaponGroupA=slots[0];weaponGroupB=slots[1];weaponGroupC=slots[2];weaponGroupD=slots[3];}
        public void SyncWeaponGroups()
        {
            var types=Parts.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Weapon).Distinct().ToList();
            var slots=CurrentWeaponSlots();
            for(int i=0;i<slots.Length;i++)if(slots[i].HasValue&&!types.Contains(slots[i].Value))slots[i]=null;
            foreach(var kind in types)
            {
                if(slots.Contains(kind))continue;
                var remembered=Parts.FirstOrDefault(p=>p.Kind==R3PartKind.Gun&&p.Weapon==kind&&
                    !string.IsNullOrEmpty(p.Id)&&(weaponSlotPreference.ContainsKey(p.Id)||weaponGroupPreference.ContainsKey(p.Id)));
                int preferred=remembered==null?-1:weaponSlotPreference.TryGetValue(remembered.Id,out var saved)?saved:
                    weaponGroupPreference[remembered.Id]?1:0;
                int available=preferred>=0&&preferred<slots.Length&&!slots[preferred].HasValue?preferred:Array.FindIndex(slots,s=>!s.HasValue);
                if(available>=0)slots[available]=kind;
            }
            SetWeaponSlots(slots);
            RememberWeaponGroups();
        }
        void RememberWeaponGroups()
        {
            var slots=CurrentWeaponSlots();
            foreach(var part in Parts.Where(p=>p.Kind==R3PartKind.Gun&&!string.IsNullOrEmpty(p.Id)))
            {
                int slot=Array.IndexOf(slots,(R4WeaponKind?)part.Weapon);
                if(slot>=0)weaponSlotPreference[part.Id]=slot;
                if(slot==0||slot==1)weaponGroupPreference[part.Id]=slot==1;
            }
        }
        public bool AssignWeaponSlot(R4WeaponKind kind,int slot)
        {
            if(slot<0||slot>=4)return false;
            SyncWeaponGroups();var slots=CurrentWeaponSlots();int old=Array.FindIndex(slots,s=>s==kind);
            if(old<0&&(!CreativeMode||!Parts.Any(p=>p.Kind==R3PartKind.Gun&&p.Weapon==kind)))return false;
            var displaced=slots[slot];slots[slot]=kind;if(old>=0)slots[old]=displaced;
            SetWeaponSlots(slots);RememberWeaponGroups();return true;
        }
        public void SwapWeaponGroups()
        { SyncWeaponGroups(); var old=weaponGroupA; weaponGroupA=weaponGroupB; weaponGroupB=old; RememberWeaponGroups(); }
        public R3GarageEconomy Economy { get; }
        readonly R3ConstructionSettings fallbackConstructionSettings=new R3ConstructionSettings();
        R3ConstructionSettings ConstructionSettings=>Economy.Construction??fallbackConstructionSettings;
        public int ConstructionBudgetLimit {get;private set;}
        public int ConstructionBudgetMilestonesApplied {get;private set;}
        public int SafeZoneLastCompleted {get;private set;}
        public float RepairPulseReadyAt {get;internal set;}
        public float OverdriveReadyAt {get;internal set;}
        public float RapidFireReadyAt {get;internal set;}
        public float DoubleShotOverdriveReadyAt {get;internal set;}
        public float TargetedEmpReadyAt {get;internal set;}
        public float AreaEmpReadyAt {get;internal set;}
        public float RechargePulseReadyAt {get;internal set;}
        public float RampDeployerReadyAt {get;internal set;}
        public float WallDeployerReadyAt {get;internal set;}
        public float JumpPadDeployerReadyAt {get;internal set;}
        public float BlastTrapDeployerReadyAt {get;internal set;}
        public float EmpTrapDeployerReadyAt {get;internal set;}
        public float RepelTrapDeployerReadyAt {get;internal set;}
        public float DamageTrapDeployerReadyAt {get;internal set;}
        public float SlowTrapDeployerReadyAt {get;internal set;}
        public float ImmobilizeTrapDeployerReadyAt {get;internal set;}
        public float FlyingDroneReadyAt {get;internal set;}
        public float GroundDroneReadyAt {get;internal set;}
        public float OffensiveTurretReadyAt {get;internal set;}
        public float HealingTurretReadyAt {get;internal set;}
        public float SupportTurretReadyAt {get;internal set;} public float ShieldTurretReadyAt {get;internal set;}
        public float BoosterReadyAt {get;internal set;}
        public float JumpReadyAt {get;internal set;}
        public float BlinkReadyAt {get;internal set;}
        public float ScannerPulseReadyAt {get;internal set;}
        public float StructuralScannerReadyAt {get;internal set;}
        public long ConstructionBudgetUsed=>Parts.Sum(p=>(long)ConstructionPartCost(p));
        public int PhysicalPartCount=>Parts.Count(p=>p.Health>0);
        public R3ConstructionSize ConstructionSizeClass=>PhysicalPartCount<=Math.Max(0,ConstructionSettings.SmallPhysicalParts)?R3ConstructionSize.Small:
            PhysicalPartCount<=Math.Max(ConstructionSettings.SmallPhysicalParts,ConstructionSettings.MediumPhysicalParts)?R3ConstructionSize.Medium:R3ConstructionSize.Large;
        public string ConstructionSizeLabel=>ConstructionSizeClass==R3ConstructionSize.Small?"Mały":ConstructionSizeClass==R3ConstructionSize.Medium?"Średni":"Duży";
        public int ApplyCompletedBudgetMilestones(int completedEncounters)
        {
            int interval=ConstructionSettings.StageInterval;
            if(interval<=0)return 0;
            int target=Math.Max(0,completedEncounters)/interval;
            if(target<=ConstructionBudgetMilestonesApplied)return 0;
            long increase=(long)(target-ConstructionBudgetMilestonesApplied)*Math.Max(0,ConstructionSettings.StageBudgetIncrease);
            int previous=ConstructionBudgetLimit;
            ConstructionBudgetLimit=(int)Math.Min(int.MaxValue,(long)previous+increase);
            ConstructionBudgetMilestonesApplied=target;
            return ConstructionBudgetLimit-previous;
        }
        public bool CanEnterSafeZone(int completedEncounters)
        {
            int interval=Economy.SafeZoneInterval;float repair=Economy.SafeZoneRepairFraction;
            return !CreativeMode&&interval>0&&completedEncounters>0&&completedEncounters%interval==0&&
               completedEncounters>SafeZoneLastCompleted&&!float.IsNaN(repair)&&!float.IsInfinity(repair)&&repair>=0&&repair<=1;
        }
        public bool TryEnterSafeZone(int completedEncounters)
        {
            if(!CanEnterSafeZone(completedEncounters))return false;
            float repair=Economy.SafeZoneRepairFraction;
            foreach(var part in Parts)if(part.Health>0&&part.Health<1)part.Health=Math.Min(1,part.Health+repair);
            var shieldBonus=new R1ModifierGroup();
            foreach(var bonus in Parts)
                if(bonus.Kind==R3PartKind.BonusModule&&bonus.Health>0&&bonus.UpgradeEffect==R1UpgradeEffect.ShieldDurability)
                    shieldBonus.Add(bonus.UpgradeOperation,bonus.UpgradeAmount,TierEffectUnits(bonus.Tier));
            float shieldMultiplier=shieldBonus.Apply(1);
            foreach(var part in Parts.Concat(inventory))
            {
                // Refill partial magazines as well as finishing pending reloads.
                int capacity=part.Kind==R3PartKind.PointDefense?40:
                    part.Kind!=R3PartKind.Gun?0:
                    part.Weapon==R4WeaponKind.Shotgun?RoboRougelike.WeaponIntegration.RRLShotgunBinding.MagazineSalvos:
                    part.Weapon==R4WeaponKind.Railgun?RoboRougelike.WeaponIntegration.RRLRailgunBinding.Magazine:
                    part.Weapon==R4WeaponKind.RocketLauncher?RoboRougelike.WeaponIntegration.RRLRocketBinding.Magazine:0;
                if(capacity>0)part.Ammo=capacity;
                if(part.Kind==R3PartKind.DirectionalShield)
                {
                    part.DefenseCharge=(float)Math.Min(float.MaxValue,(double)RoboRougelike.WeaponIntegration.RRLShieldBinding.BaseCapacity*TierEffectUnits(part.Tier)*shieldMultiplier);
                    part.DefenseCapacity=part.DefenseCharge;
                    part.DefenseRechargeAge=float.PositiveInfinity;
                }
                part.DeviceReadyAt=0;part.ReloadReadyAt=0;part.DisruptionRemaining=0;part.ThermalHeat=0;part.ThermalLocked=false;
            }
            RepairPulseReadyAt=OverdriveReadyAt=RapidFireReadyAt=DoubleShotOverdriveReadyAt=TargetedEmpReadyAt=AreaEmpReadyAt=RechargePulseReadyAt=0;
            RampDeployerReadyAt=WallDeployerReadyAt=JumpPadDeployerReadyAt=BlastTrapDeployerReadyAt=EmpTrapDeployerReadyAt=RepelTrapDeployerReadyAt=DamageTrapDeployerReadyAt=SlowTrapDeployerReadyAt=ImmobilizeTrapDeployerReadyAt=0;
            FlyingDroneReadyAt=GroundDroneReadyAt=OffensiveTurretReadyAt=HealingTurretReadyAt=SupportTurretReadyAt=ShieldTurretReadyAt=BoosterReadyAt=JumpReadyAt=BlinkReadyAt=GrappleReadyAt=0;
            ScannerPulseReadyAt=StructuralScannerReadyAt=0;
            RefreshEnergyLayout();Energy.Fill();Charges.RefillAll();SafeZoneLastCompleted=completedEncounters;return true;
        }
        public int ConstructionPartCost(R3PartState part)=>ConstructionSettings.Cost(part);
        public R3VehicleModel(R3VehicleDesign design, R3GarageEconomy economy = null)
        { Design = design; Parts = design.Parts.Select(p => {var part=p.Copy();part.Id=Guid.NewGuid().ToString("N");return part;}).ToList(); Economy = economy ?? new R3GarageEconomy();
            ConstructionBudgetLimit=Math.Max(0,ConstructionSettings.StartingBudget);
            foreach(var part in Parts.Where(p=>p.Kind==R3PartKind.Wheel))factoryWheels.Add(part.Id,new FactoryWheelPose(part)); RefreshEnergyLayout(); Energy.Fill(); }
        public static readonly Vector3Int[] Neighbours = { Vector3Int.left, Vector3Int.right, Vector3Int.up,
            Vector3Int.down, Vector3Int.forward, Vector3Int.back };
        public R3VehicleModel DeepCopy()
        {
            var design=new R3VehicleDesign{Name=Design.Name,Description=Design.Description,Speed=Design.Speed,TurnSpeed=Design.TurnSpeed,CoreHp=Design.CoreHp};
            design.Parts.AddRange(Design.Parts.Select(p=>p.Copy()));
            var copy=new R3VehicleModel(design,new R3GarageEconomy{ArmorCost=Economy.ArmorCost,WheelCost=Economy.WheelCost,GunCost=Economy.GunCost,MaxRepairCost=Economy.MaxRepairCost,StandardPurchaseMultiplier=Economy.StandardPurchaseMultiplier,SalvageOptimizerBonus=Economy.SalvageOptimizerBonus,PhysicalLootDropChance=Economy.PhysicalLootDropChance,MissedLootRecoveryCost=Economy.MissedLootRecoveryCost,SafeZoneInterval=Economy.SafeZoneInterval,SafeZoneRepairFraction=Economy.SafeZoneRepairFraction,SafeVendorOfferCount=Economy.SafeVendorOfferCount,SafeVendorPriceMultiplier=Economy.SafeVendorPriceMultiplier,SalvageOptimizerExtraOffersPerUnit=Economy.SalvageOptimizerExtraOffersPerUnit,OverdriveBonusPerUnit=Economy.OverdriveBonusPerUnit,OverdriveEnergyPerCopy=Economy.OverdriveEnergyPerCopy,OverdriveDuration=Economy.OverdriveDuration,OverdriveCooldown=Economy.OverdriveCooldown,RapidFireReductionPerUnit=Economy.RapidFireReductionPerUnit,RapidFireEnergyPerCopy=Economy.RapidFireEnergyPerCopy,RapidFireDuration=Economy.RapidFireDuration,RapidFireCooldown=Economy.RapidFireCooldown,DoubleShotChancePerUnit=Economy.DoubleShotChancePerUnit,DoubleShotOverdriveEnergyPerCopy=Economy.DoubleShotOverdriveEnergyPerCopy,DoubleShotOverdriveDuration=Economy.DoubleShotOverdriveDuration,DoubleShotOverdriveCooldown=Economy.DoubleShotOverdriveCooldown,DoubleShotOverdriveExtraPerUnit=Economy.DoubleShotOverdriveExtraPerUnit,DeviceBaseCharges=Economy.DeviceBaseCharges,CapacityChargesPerUnit=Economy.CapacityChargesPerUnit,RechargePulseChargesPerUnit=Economy.RechargePulseChargesPerUnit,ChargesPerSecondPerRegenerator=Economy.ChargesPerSecondPerRegenerator,RechargePulseEnergyPerCopy=Economy.RechargePulseEnergyPerCopy,RechargePulseCooldown=Economy.RechargePulseCooldown,RampEnergyPerCopy=Economy.RampEnergyPerCopy,RampCooldown=Economy.RampCooldown,RampLifetime=Economy.RampLifetime,WallEnergyPerCopy=Economy.WallEnergyPerCopy,WallCooldown=Economy.WallCooldown,WallLifetime=Economy.WallLifetime,JumpPadEnergyPerCopy=Economy.JumpPadEnergyPerCopy,JumpPadCooldown=Economy.JumpPadCooldown,JumpPadLifetime=Economy.JumpPadLifetime,JumpPadLaunchSpeed=Economy.JumpPadLaunchSpeed,TrapEnergyPerCopy=Economy.TrapEnergyPerCopy,TrapCooldown=Economy.TrapCooldown,TrapLifetime=Economy.TrapLifetime,TrapTriggerRadius=Economy.TrapTriggerRadius,TrapBlastDamage=Economy.TrapBlastDamage,TrapEmpDuration=Economy.TrapEmpDuration,TrapRepelSpeed=Economy.TrapRepelSpeed,TrapDamagePerSecond=Economy.TrapDamagePerSecond,TrapDamageDuration=Economy.TrapDamageDuration,TrapSlowMultiplier=Economy.TrapSlowMultiplier,TrapSlowDuration=Economy.TrapSlowDuration,TrapImmobilizeDuration=Economy.TrapImmobilizeDuration,DroneEnergyPerCopy=Economy.DroneEnergyPerCopy,DroneCooldown=Economy.DroneCooldown,DroneLifetime=Economy.DroneLifetime,DroneSpeed=Economy.DroneSpeed,DroneFireInterval=Economy.DroneFireInterval,DroneDamage=Economy.DroneDamage,DroneActivePerUnit=Economy.DroneActivePerUnit,TurretEnergyPerCopy=Economy.TurretEnergyPerCopy,TurretCooldown=Economy.TurretCooldown,TurretLifetime=Economy.TurretLifetime,TurretFireInterval=Economy.TurretFireInterval,TurretDamage=Economy.TurretDamage,HealingTurretRadius=Economy.HealingTurretRadius,HealingTurretRate=Economy.HealingTurretRate,SupportTurretRadius=Economy.SupportTurretRadius,SupportTurretEnergyPerSecond=Economy.SupportTurretEnergyPerSecond,ShieldTurretRadius=Economy.ShieldTurretRadius,ShieldTurretHp=Economy.ShieldTurretHp,BlockGrantQuantity=Economy.BlockGrantQuantity,DriveGrantQuantity=Economy.DriveGrantQuantity,Construction=ConstructionSettings.Copy()});
            copy.Economy.StructuralScanRange=Economy.StructuralScanRange;copy.Economy.StructuralScanEnergy=Economy.StructuralScanEnergy;copy.Economy.StructuralScanCooldown=Economy.StructuralScanCooldown;
            copy.Economy.BlueprintSlotCount=Economy.BlueprintSlotCount;
            foreach(var index in safeVendorExtraSold)copy.safeVendorExtraSold.Add(index);
            copy.CreativeMode=CreativeMode;copy.ConstructionBudgetLimit=ConstructionBudgetLimit;copy.ConstructionBudgetMilestonesApplied=ConstructionBudgetMilestonesApplied;copy.SafeZoneLastCompleted=SafeZoneLastCompleted;copy.SafeVendorStage=SafeVendorStage;copy.SafeVendorSeed=SafeVendorSeed;copy.SafeVendorSoldMask=SafeVendorSoldMask;copy.SafeVendorRevision=SafeVendorRevision;copy.RepairPulseReadyAt=RepairPulseReadyAt;copy.OverdriveReadyAt=OverdriveReadyAt;copy.RapidFireReadyAt=RapidFireReadyAt;copy.DoubleShotOverdriveReadyAt=DoubleShotOverdriveReadyAt;copy.TargetedEmpReadyAt=TargetedEmpReadyAt;copy.AreaEmpReadyAt=AreaEmpReadyAt;copy.RechargePulseReadyAt=RechargePulseReadyAt;copy.RampDeployerReadyAt=RampDeployerReadyAt;copy.WallDeployerReadyAt=WallDeployerReadyAt;copy.JumpPadDeployerReadyAt=JumpPadDeployerReadyAt;copy.BlastTrapDeployerReadyAt=BlastTrapDeployerReadyAt;copy.EmpTrapDeployerReadyAt=EmpTrapDeployerReadyAt;copy.RepelTrapDeployerReadyAt=RepelTrapDeployerReadyAt;copy.DamageTrapDeployerReadyAt=DamageTrapDeployerReadyAt;copy.SlowTrapDeployerReadyAt=SlowTrapDeployerReadyAt;copy.ImmobilizeTrapDeployerReadyAt=ImmobilizeTrapDeployerReadyAt;copy.FlyingDroneReadyAt=FlyingDroneReadyAt;copy.GroundDroneReadyAt=GroundDroneReadyAt;copy.OffensiveTurretReadyAt=OffensiveTurretReadyAt;copy.HealingTurretReadyAt=HealingTurretReadyAt;copy.SupportTurretReadyAt=SupportTurretReadyAt;copy.ShieldTurretReadyAt=ShieldTurretReadyAt;copy.BoosterReadyAt=BoosterReadyAt;copy.Parts.Clear();copy.Parts.AddRange(Parts.Select(p=>p.Copy()));copy.inventory.AddRange(inventory.Select(p=>p.Copy()));copy.Scrap=Scrap;
            copy.factoryWheels.Clear();foreach(var item in factoryWheels)copy.factoryWheels.Add(item.Key,item.Value);
            copy.SuspensionEnabled=SuspensionEnabled;copy.SuspensionMode=SuspensionMode;copy.SuspensionHeightOffset=SuspensionHeightOffset;
            copy.RefreshEnergyLayout();copy.Energy.Restore(Energy.Current);copy.Charges.Restore(Charges.Snapshot());copy.InitialEnergyPrepared=InitialEnergyPrepared;
            copy.SelectedDrive=SelectedDrive;copy.GyroEnabled=GyroEnabled;copy.BoosterThrustNewtons=BoosterThrustNewtons;
            copy.BoosterDurationSeconds=BoosterDurationSeconds;copy.GrappleReadyAt=GrappleReadyAt;copy.JumpReadyAt=JumpReadyAt;copy.BlinkReadyAt=BlinkReadyAt;
            copy.ScannerPulseReadyAt=ScannerPulseReadyAt;copy.StructuralScannerReadyAt=StructuralScannerReadyAt;
            copy.HoverHeightMeters=HoverHeightMeters;
            SyncWeaponGroups();copy.weaponGroupA=weaponGroupA;copy.weaponGroupB=weaponGroupB;copy.weaponGroupC=weaponGroupC;copy.weaponGroupD=weaponGroupD;
            foreach(var preference in weaponGroupPreference)copy.weaponGroupPreference[preference.Key]=preference.Value;
            foreach(var preference in weaponSlotPreference)copy.weaponSlotPreference[preference.Key]=preference.Value;
            GetActiveSlots();Array.Copy(activeSlots,copy.activeSlots,4);
            foreach(var preference in activeSlotPreference)copy.activeSlotPreference[preference.Key]=preference.Value;
            GetDeployableSlots();Array.Copy(deployableSlots,copy.deployableSlots,4);
            foreach(var preference in deployableSlotPreference)copy.deployableSlotPreference[preference.Key]=preference.Value;
            return copy;
        }
        public const int MaxScrap=100000;
        public int PreviewAward(int amount) => (int)Math.Min(Math.Max(0L,(long)MaxScrap-Scrap),Math.Max(0L,(long)amount));
        public void Award(int amount) { Scrap = (int)Math.Min((long)MaxScrap, Scrap + (long)Math.Max(0, amount)); }
        public bool TrySpendScrap(int amount)
        {if(amount<0||amount>Scrap)return false;Scrap-=amount;return true;}
        public static bool IsPassiveEnergyPart(R3PartKind kind) =>
            kind==R3PartKind.Suspension||kind==R3PartKind.Gyroscope||kind==R3PartKind.BonusModule||
            kind==R3PartKind.DirectionalShield||kind==R3PartKind.IndependentSteering||kind==R3PartKind.DrivePower||kind==R3PartKind.EnergyRegenerator||kind==R3PartKind.SalvageOptimizer||kind==R3PartKind.DoubleShot||kind==R3PartKind.ChargeCapacity||kind==R3PartKind.ChargeRegenerator;
        public int RewardBasketOfferCount
        {
            get
            {
                long units=Parts.Where(p=>p.Kind==R3PartKind.SalvageOptimizer&&p.Health>0).Sum(p=>(long)TierEffectUnits(p.Tier));
                return (int)Math.Min(int.MaxValue,3+units*Math.Max(0,(long)Economy.SalvageOptimizerExtraOffersPerUnit));
            }
        }
        public void RefreshEnergyLayout()
        {
            var alive=Parts.Where(p=>p.Health>0).ToArray();
            var capacity=new R1ModifierGroup();var regeneration=new R1ModifierGroup();
            foreach(var part in alive)
            {
                if(part.Kind!=R3PartKind.BonusModule)continue;
                if(part.UpgradeEffect==R1UpgradeEffect.EnergyCapacity)capacity.Add(part.UpgradeOperation,part.UpgradeAmount,TierEffectUnits(part.Tier));
                if(part.UpgradeEffect==R1UpgradeEffect.EnergyRegeneration)regeneration.Add(part.UpgradeOperation,part.UpgradeAmount,TierEffectUnits(part.Tier));
            }
            Energy.SetResourceMultipliers(capacity.Apply(1),regeneration.Apply(1));
            Energy.SetAdditionalPassiveReservation(alive.Where(p=>IsPassiveEnergyPart(p.Kind)).Sum(p=>TierPassiveExtra(p.Tier)));
            Energy.Refresh(alive.Where(p=>p.Kind==R3PartKind.Battery).Sum(p=>TierEffectUnits(p.Tier)),
                alive.Count(p=>IsPassiveEnergyPart(p.Kind)),alive.Any(p=>p.Kind==R3PartKind.Core));
            Energy.SetRegeneratorCount(alive.Where(p=>p.Kind==R3PartKind.EnergyRegenerator).Sum(p=>TierEffectUnits(p.Tier)));
            long chargeUnits=alive.Where(p=>p.Kind==R3PartKind.ChargeCapacity).Sum(p=>(long)TierEffectUnits(p.Tier));
            Charges.SetCapacityBonus((int)Math.Min(int.MaxValue,chargeUnits*Math.Max(0,(long)Economy.CapacityChargesPerUnit)));
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
            (part.Tier<=1||SupportsFusion(part.Kind,part.Weapon))&&part.DismantleCap>=0&&
            !float.IsNaN(part.Health)&&!float.IsInfinity(part.Health)&&part.Health>=0&&part.Health<=1;
        int BasePrice(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic)=>!ValidEconomicKind(kind,weapon)?0:Math.Max(1,
            kind==R3PartKind.Gun&&weapon!=R4WeaponKind.Basic?R4WeaponDefinition.Get(weapon).Price:Economy.Price(kind));
        public int Price(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic)
        {
            if(!ValidEconomicKind(kind,weapon)||CreativeMode)return 0;
            float multiplier=Economy.StandardPurchaseMultiplier;
            if(float.IsNaN(multiplier)||float.IsInfinity(multiplier))multiplier=3f;
            return (int)Math.Min(int.MaxValue,Math.Max(1d,Math.Ceiling(BasePrice(kind,weapon)*(double)Math.Max(0,multiplier))));
        }
        public int GrantQuantity(R3PartKind kind,R3ArmorShape shape=R3ArmorShape.Cube)
        {
            if(kind==R3PartKind.Armor&&shape==R3ArmorShape.Cube)return Math.Max(1,Economy.BlockGrantQuantity);
            if(kind==R3PartKind.Wheel||kind==R3PartKind.OmniDrive||kind==R3PartKind.HoverEngine||
                kind==R3PartKind.CrawlerTrack||kind==R3PartKind.Booster||kind==R3PartKind.JumpDrive||kind==R3PartKind.DrivePower)
                return Math.Max(1,Economy.DriveGrantQuantity);
            return 1;
        }
        public int PurchaseQuantity(R3PartKind kind,R3ArmorShape shape=R3ArmorShape.Cube)=>CreativeMode?1:GrantQuantity(kind,shape);
        public bool Owns(string id) => Parts.Concat(inventory).Any(p=>p.Id==id);
        // Mounted wreck plans reserve their type too: rebuilding them must not
        // turn a legal four-type design into a five-type vehicle. Inventory
        // parts remain unrestricted until the player actually mounts them.
        const int MaxWeaponTypes=4;
        const string WeaponTypeLimitError="Maksymalnie 4 różne typy broni na pojeździe.";
        HashSet<R4WeaponKind> MountedWeaponTypes() => new HashSet<R4WeaponKind>(
            Parts.Where(p=>p.Kind==R3PartKind.Gun).Select(p=>p.Weapon));
        bool InvalidWeaponTypes => !CreativeMode&&MountedWeaponTypes().Count>MaxWeaponTypes;
        public bool WeaponInGroup(R4WeaponKind weapon,bool secondary)=>WeaponInSlot(weapon,secondary?1:0);
        public static bool MatchesWeaponGroup(R4WeaponKind weapon,bool secondary,R4WeaponKind? a,R4WeaponKind? b,bool creative) =>
            secondary ? (b==weapon || (creative&&weapon!=a&&weapon!=b)) : a==weapon;
        public bool WeaponInSlot(R4WeaponKind weapon,int slot)
        {
            if(slot<0||slot>=4)return false;
            return MatchesWeaponSlot(weapon,slot,GetWeaponSlots(),CreativeMode);
        }
        public static bool MatchesWeaponSlot(R4WeaponKind weapon,int slot,R4WeaponKind?[] slots,bool creative)=>
            slot>=0&&slot<4&&slots!=null&&slots.Length==4&&
            (slots[slot]==weapon||(creative&&slot==1&&!slots.Contains(weapon)));
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
            if(!CreativeMode&&footprint.Any(c=>Math.Abs(c.x)>6||Math.Abs(c.z)>7||c.y<0||c.y>5))return "Moduł wykracza poza siatkę montażową.";
            if(footprint.Any(c=>At(c)!=null))return "Zajęte pole części. Utraconą część odtwórz lub usuń jej plan.";
            if(!CreativeMode&&ConstructionBudgetUsed+ConstructionPartCost(candidate)>ConstructionBudgetLimit)
                return $"Budżet konstrukcji: {ConstructionBudgetUsed}/{ConstructionBudgetLimit} pkt; część kosztuje {ConstructionPartCost(candidate)} pkt.";
            if(kind==R3PartKind.BonusModule&&!owned)return "Moduły bonusów zdobywa się wyłącznie jako nagrody.";
            if(kind==R3PartKind.Gun)
            {
                var types=MountedWeaponTypes();
                if(!CreativeMode&&(types.Count>MaxWeaponTypes||(types.Count==MaxWeaponTypes&&!types.Contains(weapon))))return WeaponTypeLimitError;
                // A gun has a one-cell clearance around its own 1x1x1 mounting
                // cell. Existing constructions remain loadable and repairable;
                // the rule applies to each new placement, including inventory.
                if(Parts.Any(p=>p.Kind==R3PartKind.Gun&&
                    Math.Abs((long)p.Cell.x-cell.x)<=1&&Math.Abs((long)p.Cell.y-cell.y)<=1&&Math.Abs((long)p.Cell.z-cell.z)<=1))
                    return "Zachowaj co najmniej jedno wolne pole odstępu między broniami.";
            }
            if(!CreativeMode&&IsActiveSlotPart(kind)&&ExceedsModuleTypeLimit(candidate,true))return "Maksymalnie 4 różne rodzaje modułów aktywnych.";
            if(!CreativeMode&&IsPassiveSlotPart(kind)&&ExceedsModuleTypeLimit(candidate,false))return "Maksymalnie 8 różnych rodzajów modułów pasywnych.";
            if(!CreativeMode&&IsDeployableSlotPart(kind)&&Parts.Where(p=>IsDeployableSlotPart(p.Kind)).Select(p=>p.Kind).Distinct().Count()>=4&&
                !Parts.Any(p=>p.Kind==kind))return "Maksymalnie 4 różne rodzaje urządzeń rozstawianych.";
            if(kind==R3PartKind.CrawlerTrack&&cell.x==0)return "Gąsienicę zamontuj po lewej lub prawej stronie.";
            if (!HasAliveExternalNeighbour(cell,kind,rotation,contactMask,candidate))
                return "Brak zgodnych punktów mocowania ze sprawną konstrukcją.";
            if (!owned && Scrap < Price(kind,weapon)) return "Za mało Złomu.";
            if(!owned&&(long)inventory.Count+PurchaseQuantity(kind)-1>AvailableInventoryCapacity)return "Za mało miejsca w magazynie na pozostałe części pakietu.";
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
            for(int i=1;i<PurchaseQuantity(kind);i++)
            {
                var copy=part.Copy();copy.Id=Guid.NewGuid().ToString("N");copy.Cell=default;copy.ContactMask=StoredWheelContacts(copy);
                inventory.Add(copy);
            }
            return true;
        }
        public bool PurchaseToInventory(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic,R3WheelStyle wheelStyle=R3WheelStyle.Offroad,R3ArmorShape armorShape=R3ArmorShape.Cube)
            => PurchaseToInventoryAtPrice(kind,weapon,wheelStyle,armorShape,Price(kind,weapon));
        bool PurchaseToInventoryAtPrice(R3PartKind kind,R4WeaponKind weapon,R3WheelStyle wheelStyle,R3ArmorShape armorShape,int price)
        {
            if(kind==R3PartKind.Core||kind==R3PartKind.BonusModule||!ValidEconomicKind(kind,weapon)||
                (long)inventory.Count+PurchaseQuantity(kind,armorShape)>AvailableInventoryCapacity||
                !Enum.IsDefined(typeof(R3WheelStyle),wheelStyle)||(kind!=R3PartKind.Wheel&&wheelStyle!=R3WheelStyle.Offroad)||
                (HasSizedSideWheel(wheelStyle)&&!R4_1.R4WheelArt.SizedWheelAvailable(wheelStyle))||
                (HasForkMount(wheelStyle)&&!R4_1.R4WheelArt.ForkWheelAvailable(wheelStyle))||
                !Enum.IsDefined(typeof(R3ArmorShape),armorShape)||(kind!=R3PartKind.Armor&&armorShape!=R3ArmorShape.Cube)||
                (armorShape!=R3ArmorShape.Cube&&!R4_1.R41PartGeometry.ArmorShapeAvailable(armorShape)))return false;
            if((price<=0&&!CreativeMode)||Scrap<price)return false;
            Scrap-=price;var purchased=new R3PartState(default,kind){Weapon=weapon,WheelStyle=wheelStyle,ArmorShape=armorShape,
                FootprintRevision=NewPurchaseFootprint(kind,weapon,wheelStyle),
                ContactMask=kind==R3PartKind.Wheel?(HasForkMount(wheelStyle)?R3MountingGrid.FaceBit(Vector3Int.up):
                    R3MountingGrid.FaceBit(Vector3Int.right)|R3MountingGrid.FaceBit(Vector3Int.left)):
                    armorShape==R3ArmorShape.Cube?R3MountingGrid.AllFaces:R4_1.R41PartGeometry.FullArmorContacts(armorShape)};
            inventory.Add(purchased);
            for(int i=1;i<PurchaseQuantity(kind,armorShape);i++){var copy=purchased.Copy();copy.Id=Guid.NewGuid().ToString("N");inventory.Add(copy);}
            return true;
        }        public int RepairCost(R3PartState part)
        {
            if (!ValidEconomicPart(part) || part.Health >= 1f || CreativeMode) return 0;
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
            if (part == null || !ValidEconomicPart(part) || part.Health>=1 || (price <= 0&&!CreativeMode) || Scrap < price) return false;
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
            if (inventory.Count>=AvailableInventoryCapacity ||
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
                if(!CreativeMode&&cells.Any(c=>Math.Abs(c.x)>6||Math.Abs(c.z)>7||c.y<0||c.y>5))return false;
                var otherCells=new HashSet<Vector3Int>(Parts.Where(p=>p!=part).SelectMany(OccupiedCells));
                if(cells.Any(otherCells.Contains)||!Connected(Parts)||!Connected(Parts.Where(p=>p.Health>0)))return false;
                if(installationGate!=null&&installationGate(part)!=null)return false;
                accepted=true;return true;
            }
            finally {if(!accepted){part.RotationQuarterTurns=oldRotation;part.ContactMask=oldContacts;}}
        }
        public bool ReceiveSalvage(IEnumerable<R3PartState> recovered)=>ReceiveSalvageCore(recovered,false);
        internal bool ReceivePhysicalSalvage(IEnumerable<R3PartState> recovered)=>ReceiveSalvageCore(recovered,true);
        bool ReceiveSalvageCore(IEnumerable<R3PartState> recovered,bool allowArmor)
        {
            if(recovered==null)return false;
            var batch=recovered.ToArray();
            if((long)inventory.Count+batch.Length>AvailableInventoryCapacity)return false;
            if(batch.Any(p=>!ValidEconomicPart(p)||(!allowArmor&&p.Kind==R3PartKind.Armor)||p.Kind==R3PartKind.Core||
                (p.Kind==R3PartKind.BonusModule&&(string.IsNullOrWhiteSpace(p.UpgradeId)||
                    !Enum.IsDefined(typeof(R1UpgradeEffect),p.UpgradeEffect)||!ValidBonusOperation(p.UpgradeEffect,p.UpgradeOperation)||!ValidBonusScope(p.UpgradeEffect,p.UpgradeScope)||float.IsNaN(p.UpgradeAmount)||
                    float.IsInfinity(p.UpgradeAmount)||p.UpgradeAmount<0||p.UpgradeAmount>1))||
                string.IsNullOrWhiteSpace(p.Id)||Owns(p.Id))||
                batch.Select(p=>p.Id).Distinct().Count()!=batch.Length)return false;
            inventory.AddRange(batch.Select(p=>{var copy=p.Copy();copy.ContactMask=StoredWheelContacts(copy);NormalizeStoredEquipmentFootprint(copy);return copy;}));return true;
        }
        public static R3PartState CreateBonusReward(R1UpgradeDefinition bonus)
        {
            if(bonus==null||string.IsNullOrWhiteSpace(bonus.Id)||
                !Enum.IsDefined(typeof(R1UpgradeEffect),bonus.Effect)||!ValidBonusOperation(bonus.Effect,bonus.Operation)||!ValidBonusScope(bonus.Effect,bonus.Scope)||
                float.IsNaN(bonus.Amount)||float.IsInfinity(bonus.Amount))return null;
            return new R3PartState(default,R3PartKind.BonusModule){
                Id=Guid.NewGuid().ToString("N"),Source="Bonus za zwycięstwo",
                UpgradeId=bonus.Id,UpgradeTitle=string.IsNullOrWhiteSpace(bonus.Title)?bonus.Id:bonus.Title,
                UpgradeDescription=bonus.Description,UpgradeEffect=bonus.Effect,UpgradeOperation=bonus.Operation,UpgradeScope=bonus.Scope,
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
            if(part==null||!ValidEconomicPart(part)||part.Health>=1||(cost<=0&&!CreativeMode)||Scrap<cost)return false;
            Scrap-=cost;part.Health=1;return true;
        }
        int OwnedPartValue(R3PartState part)=>(int)Math.Min(int.MaxValue,(long)BasePrice(part.Kind,part.Weapon)*(NormalizedTier(part.Tier)==3?9:NormalizedTier(part.Tier)==2?3:1));
        int DismantleFullValue(R3PartState part)
        {
            int normal=Mathf.FloorToInt(OwnedPartValue(part)*0.25f);
            return part.HasDismantleCap?Math.Min(normal,part.DismantleCap):normal;
        }
        public int DismantleValue(R3PartState part)
        {
            if(!ValidEconomicPart(part))return 0;
            int normal=Mathf.FloorToInt(OwnedPartValue(part)*0.25f*Mathf.Clamp01(part.Health));
            return part.HasDismantleCap?Math.Min(normal,part.DismantleCap):normal;
        }
        public static bool SameBonusType(R3PartState a,R3PartState b)=>a!=null&&b!=null&&
            a.Kind==R3PartKind.BonusModule&&b.Kind==R3PartKind.BonusModule&&
            !string.IsNullOrWhiteSpace(a.UpgradeId)&&a.UpgradeId==b.UpgradeId&&
            Enum.IsDefined(typeof(R1UpgradeEffect),a.UpgradeEffect)&&a.UpgradeEffect==b.UpgradeEffect&&
            ValidBonusOperation(a.UpgradeEffect,a.UpgradeOperation)&&a.UpgradeOperation==b.UpgradeOperation&&
            ValidBonusScope(a.UpgradeEffect,a.UpgradeScope)&&a.UpgradeScope==b.UpgradeScope&&
            !float.IsNaN(a.UpgradeAmount)&&!float.IsInfinity(a.UpgradeAmount)&&a.UpgradeAmount>=0&&a.UpgradeAmount<=1&&a.UpgradeAmount==b.UpgradeAmount;
        public static bool SameFusionType(R3PartState a,R3PartState b) => a!=null&&b!=null&&a.Kind==b.Kind&&
            NormalizedTier(a.Tier)==NormalizedTier(b.Tier)&&
            (a.Kind!=R3PartKind.Gun||(a.Weapon==b.Weapon&&a.FootprintRevision==b.FootprintRevision))&&
            (a.Kind!=R3PartKind.Wheel||(a.WheelStyle==b.WheelStyle&&a.FootprintRevision==b.FootprintRevision))&&
            (a.Kind!=R3PartKind.Armor||a.ArmorShape==b.ArmorShape)&&
            (a.Kind!=R3PartKind.BonusModule||SameBonusType(a,b));
        public static bool SameMountVariant(R3PartState a,R3PartState b)=>
            SameFusionType(a,b)&&PlacementContacts(a.Kind,a)==PlacementContacts(b.Kind,b);
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
            if(parts[0].Kind==R3PartKind.DirectionalShield&&parts.Any(p=>float.IsNaN(p.DefenseCharge)||float.IsInfinity(p.DefenseCharge)||float.IsNaN(p.DefenseCapacity)||float.IsInfinity(p.DefenseCapacity)||p.DefenseCapacity<0||
                float.IsNaN(p.DefenseRechargeAge)||float.IsNegativeInfinity(p.DefenseRechargeAge)||p.DefenseRechargeAge<0))return false;
            int tier=NormalizedTier(parts[0].Tier);
            if(tier>=3||parts.Any(p=>!SameFusionType(parts[0],p)))return false;
            // A new physical item represents all three inputs. Preserve average
            // condition so fusion never repairs damaged items for free.
            var result=parts[0].Copy();result.Id=Guid.NewGuid().ToString("N");result.Tier=tier+1;
            result.Health=parts.Average(p=>p.Health);result.Source="Fuzja trzech części T"+tier;
            if(parts.Any(p=>p.HasDismantleCap))
            {
                // The output cannot recover more scrap than its inputs could
                // individually recover at full health, including mixed packs.
                result.HasDismantleCap=true;
                result.DismantleCap=(int)Math.Min(int.MaxValue,parts.Sum(p=>(long)DismantleFullValue(p)));
            }
            result.DeviceReadyAt=parts.Max(p=>p.DeviceReadyAt);result.ReloadReadyAt=parts.Max(p=>p.ReloadReadyAt);
            result.DisruptionRemaining=parts.Max(p=>p.DisruptionRemaining);
            result.ThermalHeat=parts.Max(p=>p.ThermalHeat);result.ThermalLocked=parts.Any(p=>p.ThermalLocked);
            if(result.Kind==R3PartKind.DirectionalShield)
            {
                float oldCapacity=RoboRougelike.WeaponIntegration.RRLShieldBinding.BaseCapacity*TierEffectUnits(tier);
                if(parts.Any(p=>p.DefenseCharge>=0))
                {
                    float fraction=parts.Average(p=>p.DefenseCharge<0?1f:Mathf.Clamp01(p.DefenseCharge/(p.DefenseCapacity>0?p.DefenseCapacity:oldCapacity)));
                    result.DefenseCapacity=RoboRougelike.WeaponIntegration.RRLShieldBinding.BaseCapacity*TierEffectUnits(result.Tier);
                    result.DefenseCharge=result.DefenseCapacity*fraction;
                }
                else result.DefenseCharge=-1;
                result.DefenseRechargeAge=parts.Min(p=>p.DefenseRechargeAge);
            }
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
            inventory.Remove(part);weaponGroupPreference.Remove(part.Id);weaponSlotPreference.Remove(part.Id);Award(quote);return true;
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
                if (CreativeMode) return null;
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

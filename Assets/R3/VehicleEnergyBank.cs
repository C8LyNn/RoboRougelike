using System.Collections.Generic;
using UnityEngine;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.Mechanics.Booster;
using RoboRougelike.Mechanics.MOV002JumpDrive;
using RoboRougelike.Mechanics.MOV003Blink;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;
using RoboRougelike.Mechanics.MOV005OmniDrive;
using RoboRougelike.Mechanics.MOV006MagneticGrapple;
using RoboRougelike.Mechanics.MOV007;
using RoboRougelike.Mechanics.MOV008;
using RoboRougelike.Mechanics.MOV009;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.Mechanics.BeamCutter;
using RoboRougelike.Weapons.RotarySaw;
using RoboRougelike.Mechanics.Rockets;
using RoboRougelike.Mechanics.PointDefense;
using RoboRougelike.Defenses.DirectionalShield;
using RoboRougelike.Mechanics.StructuralScan;
using RoboRougelike.R1;

namespace RoboRougelike.R3
{
    /// <summary>One real vehicle's energy, shared by existing device adapters.</summary>
    [DisallowMultipleComponent]
    public sealed class VehicleEnergyBank : MonoBehaviour, IBoosterEnergySource,
        IJumpDriveEnergySource, IBlinkEnergySource, IGyroEnergySource, IOmniEnergySource,
        IGrappleEnergySource, IHoverEnergySource, ICrawlerEnergySource,
        IActiveSuspensionEnergySource, IEmpEnergyBank, IBeamEnergySource,
        IRotarySawEnergySource, IRocketEnergySource, IPointDefenseEnergySource,
        IShieldEnergySource, IStructuralScanEnergy
    {
        R3VehicleAssembly assembly;
        P12PipelineSafeDamageable enemyCore;
        readonly List<VehicleEnergyBattery> enemyBatteries=new List<VehicleEnergyBattery>();
        readonly List<RoboRougelike.WeaponIntegration.RRLShieldBinding> enemyShields=new List<RoboRougelike.WeaponIntegration.RRLShieldBinding>();
        VehicleEnergyState state = new VehicleEnergyState();
        float chargeRegeneration;
        readonly R1ModifierGroup capacityBonus=new R1ModifierGroup(),regenerationBonus=new R1ModifierGroup();
        public float Current { get { Refresh(); return state.Current; } }
        public float Maximum { get { Refresh(); return state.Maximum; } }

        public void Configure(R3VehicleAssembly owner)
        {
            assembly=owner;
            state=owner.Model.Energy;
            Refresh();
        }

        public void ConfigureEnemy(P12PipelineSafeDamageable core)
        {
            enemyCore=core;
            Refresh();
        }

        public void Refresh()
        {
            if(assembly!=null)
            {
                // A temporarily disabled entire actor keeps its stored charge;
                // individual disabled parts do not provide or reserve capacity.
                if(!assembly.gameObject.activeInHierarchy)return;
                int batteries=0,passive=0,regenerators=0;long chargeCapacity=0,chargeRegenerators=0;float extra=0;bool coreAlive=false;
                capacityBonus.Reset();regenerationBonus.Reset();
                foreach(var part in assembly.Parts)
                {
                    if(part==null||!part.gameObject.activeInHierarchy||!part.Operational||!part.transform.IsChildOf(transform))continue;
                    if(R3VehicleModel.IsPassiveEnergyPart(part.Kind))
                    {passive++;extra+=R3VehicleModel.TierPassiveExtra(part.Tier);}
                    if(part.Kind==R3PartKind.Core)coreAlive=true;
                    if(part.Kind==R3PartKind.BonusModule)
                    {
                        if(part.UpgradeEffect==R1UpgradeEffect.EnergyCapacity)capacityBonus.Add(part.UpgradeOperation,part.UpgradeAmount,R3VehicleModel.TierEffectUnits(part.Tier));
                        if(part.UpgradeEffect==R1UpgradeEffect.EnergyRegeneration)regenerationBonus.Add(part.UpgradeOperation,part.UpgradeAmount,R3VehicleModel.TierEffectUnits(part.Tier));
                    }
                    if(part.Kind==R3PartKind.Battery)batteries+=R3VehicleModel.TierEffectUnits(part.Tier);
                    if(part.Kind==R3PartKind.EnergyRegenerator&&
                       !RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))
                        regenerators+=R3VehicleModel.TierEffectUnits(part.Tier);
                    if(!RoboRougelike.WeaponIntegration.RRLEmpPartState.IsBlocked(part.gameObject))
                    {
                        if(part.Kind==R3PartKind.ChargeCapacity)chargeCapacity+=R3VehicleModel.TierEffectUnits(part.Tier);
                        if(part.Kind==R3PartKind.ChargeRegenerator)chargeRegenerators+=R3VehicleModel.TierEffectUnits(part.Tier);
                    }
                }
                state.SetResourceMultipliers(capacityBonus.Apply(1),regenerationBonus.Apply(1));
                state.SetAdditionalPassiveReservation(extra);
                state.Refresh(batteries,passive,coreAlive);
                state.SetRegeneratorCount(regenerators);
                var config=assembly.Model.Economy;
                assembly.Model.Charges.SetCapacityBonus((int)System.Math.Min(int.MaxValue,chargeCapacity*System.Math.Max(0,(long)config.CapacityChargesPerUnit)));
                float rate=config.ChargesPerSecondPerRegenerator;
                chargeRegeneration=float.IsNaN(rate)||float.IsInfinity(rate)||rate<0?0:(float)System.Math.Min(float.MaxValue,chargeRegenerators*(double)rate);
            }
            else if(enemyCore!=null)
            {
                int batteries=0,passive=0;
                GetComponentsInChildren(false,enemyBatteries);
                foreach(var battery in enemyBatteries)
                    if(battery!=null&&battery.IsAvailable(transform))batteries+=R3VehicleModel.TierEffectUnits(battery.Tier);
                enemyBatteries.Clear();
                GetComponentsInChildren(false,enemyShields);
                foreach(var shield in enemyShields)
                    if(shield!=null&&shield.Device!=null&&shield.Device.HardwarePresent)passive++;
                enemyShields.Clear();
                state.SetResourceMultipliers(1,1);state.SetAdditionalPassiveReservation(0);
                state.Refresh(batteries,passive,!enemyCore.IsDead&&enemyCore.transform.IsChildOf(transform));
                state.SetRegeneratorCount(0);
            }
        }

        void FixedUpdate(){Refresh();var body=GetComponent<Rigidbody>();bool stopped=assembly!=null&&body!=null&&body.linearVelocity.sqrMagnitude<.0025f&&body.angularVelocity.sqrMagnitude<.01f;state.AdvanceWithMotion(Time.fixedDeltaTime,stopped);if(assembly!=null)assembly.Model.Charges.Advance(Time.fixedDeltaTime,chargeRegeneration);}
        public bool CanSpend(float amount){Refresh();return !float.IsNaN(amount)&&!float.IsInfinity(amount)&&amount>=0&&amount<=state.Current;}
        public bool TrySpend(float amount){Refresh();return state.TrySpend(amount);}
        public bool TryConsume(float amount)=>TrySpend(amount);
        public bool TryConsumeEnergy(float amount)=>TrySpend(amount);
        public bool TrySpendEnergy(float amount)=>TrySpend(amount);
        public void Refund(float amount)=>state.Recharge(amount);
        public void RefundEnergy(float amount)=>state.Recharge(amount);
        public void Recharge(float amount){Refresh();state.Recharge(amount);}
        public void ApplyFieldCharge(){Refresh();state.ApplyFieldCharge();}
        public void ApplyEnergyPickup(){Refresh();state.ApplyEnergyPickup();}
    }

    /// <summary>Marks an enemy's real damageable battery, not a second energy pool.</summary>
    public sealed class VehicleEnergyBattery : MonoBehaviour
    {
        public int Tier {get;private set;}=1;
        public void Configure(R3PartState state){Tier=R3VehicleModel.NormalizedTier(state?.Tier??1);}
        public bool IsAvailable(Transform owner)
        {
            var hp=GetComponent<P12PipelineSafeDamageable>();
            return hp!=null&&!hp.IsDead&&transform.IsChildOf(owner);
        }
    }
}

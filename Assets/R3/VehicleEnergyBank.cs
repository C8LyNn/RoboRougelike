using System.Linq;
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
        VehicleEnergyState state = new VehicleEnergyState();
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
                var alive=assembly.Parts.Where(p=>p!=null&&p.Operational&&p.transform.IsChildOf(transform)).ToArray();
                state.Refresh(alive.Count(p=>p.Kind==R3PartKind.Battery),
                    alive.Count(p=>R3VehicleModel.IsPassiveEnergyPart(p.Kind)),
                    alive.Any(p=>p.Kind==R3PartKind.Core));
            }
            else if(enemyCore!=null)
            {
                var batteries=GetComponentsInChildren<VehicleEnergyBattery>().Count(b=>b.IsAvailable(transform));
                var passive=GetComponentsInChildren<RoboRougelike.WeaponIntegration.RRLShieldBinding>()
                    .Count(s=>s.Device!=null&&s.Device.HardwareAvailable);
                state.Refresh(batteries,passive,!enemyCore.IsDead&&enemyCore.transform.IsChildOf(transform));
            }
        }

        void FixedUpdate(){Refresh();var body=GetComponent<Rigidbody>();bool stopped=assembly!=null&&body!=null&&body.linearVelocity.sqrMagnitude<.0025f&&body.angularVelocity.sqrMagnitude<.01f;state.AdvanceWithMotion(Time.fixedDeltaTime,stopped);}
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
        public bool IsAvailable(Transform owner)
        {
            var hp=GetComponent<P12PipelineSafeDamageable>();
            return hp!=null&&!hp.IsDead&&transform.IsChildOf(owner);
        }
    }
}

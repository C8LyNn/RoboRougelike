using System.Collections.Generic;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.WeaponIntegration
{
    // Reuses the existing weapon identity, power bank, HP and hit routing.
    // The values below are intentionally provisional for playtesting.
    [DisallowMultipleComponent]
    public sealed class RRLDamageAuraBinding : MonoBehaviour, IRRLDeviceLifecycle
    {
        public const float Radius=6f, DamagePerSecond=25f, EnergyPerSecond=35f;
        public RRLDeviceBinding Device {get;private set;}
        public R3PartState State=>Device.State;
        public bool IsActive {get;private set;}
        VehicleEnergyBank energy;
        P12PipelineSafeTankController tank;
        float enemyDamageScale=1;
        bool previousHeld;
        readonly Dictionary<Transform,Collider> targets=new Dictionary<Transform,Collider>();
        readonly List<Collider> unblockedTargets=new List<Collider>();
        readonly List<R3ShieldDome.BlastBoundary> shieldBoundaries=new List<R3ShieldDome.BlastBoundary>();
        readonly List<R3ShieldDome.BlastBoundary> exposedShields=new List<R3ShieldDome.BlastBoundary>();
        Collider[] overlaps=new Collider[128];
        public void Bind(R3PartState state,Transform owner,float damageScale=1)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();
            Device.Bind(state,owner);energy=owner.GetComponent<VehicleEnergyBank>();tank=owner.GetComponent<P12PipelineSafeTankController>();
            enemyDamageScale=damageScale;Device.Damageable.Died+=Stop;
        }
        public void SetTriggerHeld(bool held)
        {
            bool pressed=held&&!previousHeld;previousHeld=held;
            if(!pressed)return;
            if(IsActive){Stop();return;}
            Fire();
        }
        // Enemies request the aura while in range; repeated requests do not toggle it.
        public bool Fire()
        {
            var run=R1RunController.Instance;
            if(run!=null&&run.Player!=null&&Device!=null&&Device.Owner==run.Player.transform&&run.EmergencyRescueAttackBlocked)return false;
            if(Device==null||!Device.Available||energy==null||!energy.CanSpend(EnergyPerSecond*Time.fixedDeltaTime))return false;
            IsActive=true;return true;
        }
        public void RefreshAvailability(){if(Device==null||!Device.HardwareAvailable)Stop();}
        public void CaptureState(){} // Active state is intentionally not persisted across fights.
        void FixedUpdate()
        {
            if(!IsActive)return;
            var run=R1RunController.Instance;
            if(run!=null&&run.Player!=null&&Device!=null&&Device.Owner==run.Player.transform&&run.EmergencyRescueAttackBlocked)return;
            if(Device==null||!Device.HardwareAvailable||energy==null){Stop();return;}
            // Pause and scene transitions must not consume energy or deal damage.
            if(!Device.Available)return;
            float step=Time.fixedDeltaTime;
            if(step<=0||!energy.TrySpend(EnergyPerSecond*R3VehicleModel.TierActivationCost(State.Tier)*step)){Stop();return;}
            // FixedUpdate must use the physics pose, including the first step
            // after a spawn or reset when Transform interpolation can lag.
            targets.Clear();Vector3 origin=Device.Body.position;
            int count;
            while(true)
            {
                count=Physics.OverlapSphereNonAlloc(origin,Radius,overlaps,~0,QueryTriggerInteraction.Ignore);
                if(count<overlaps.Length)break;
                if(overlaps.Length>=4096){overlaps=Physics.OverlapSphere(origin,Radius,~0,QueryTriggerInteraction.Ignore);count=overlaps.Length;break;}
                System.Array.Resize(ref overlaps,overlaps.Length*2);
            }
            for(int i=0;i<count;i++)
            {
                var collider=overlaps[i];
                if(!RRLHitRouter.CanHit(collider,Device.Owner))continue;
                var enemy=collider.GetComponentInParent<P12PipelineSafeEnemyTurret>();
                var player=collider.GetComponentInParent<R1VehicleState>();
                Transform root=enemy!=null?enemy.transform:player!=null?player.transform:null;
                if(root==null||collider.GetComponentInParent<P12PipelineSafeDamageable>()==null)continue;
                if(!targets.TryGetValue(root,out var current)||
                   (collider.ClosestPoint(origin)-origin).sqrMagnitude<(current.ClosestPoint(origin)-origin).sqrMagnitude)
                    targets[root]=collider;
            }
            float scale=tank!=null?tank.PackageDamageScale:enemyDamageScale;
            float damage=DamagePerSecond*R3VehicleModel.TierEffectUnits(State.Tier)*scale*step;
            R3ShieldDome.CollectExposedBlastBoundaries(origin,Radius,Device.Owner,shieldBoundaries,exposedShields);unblockedTargets.Clear();
            foreach(var collider in targets.Values)
            {
                if(collider==null)continue;var point=collider.ClosestPoint(origin);
                if(!R3ShieldDome.TryFirstHit(origin,point,Device.Owner,out _,out _))unblockedTargets.Add(collider);
            }
            foreach(var collider in unblockedTargets)
                if(collider!=null)RRLHitRouter.Apply(collider,damage,collider.ClosestPoint(origin),collider.transform.position-origin,Device.Owner);
            foreach(var boundary in exposedShields)
                if(boundary.Dome!=null)boundary.Dome.Absorb(damage,boundary.Point,(boundary.Point-origin).normalized);
        }
        void Stop(){IsActive=false;}
        void OnDisable(){Stop();previousHeld=false;}
        void OnDestroy(){if(Device?.Damageable!=null)Device.Damageable.Died-=Stop;}
    }
}

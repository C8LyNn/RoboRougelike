using UnityEngine;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.PackageIntegration;
using RoboRougelike.Mechanics.Rockets;
using RoboRougelike.VFX001;

namespace RoboRougelike.VFX001Integration
{
    /// <summary>Settled gameplay notifications feed the original cosmetic-only v5 pool.</summary>
    [DisallowMultipleComponent]
    public sealed class Vfx001R6LiveBridge : MonoBehaviour
    {
        [SerializeField] private bool visualEffectsEnabled=true;
        private R1RunController run;
        private VfxPoolManager pool;
        private VfxFeedbackOrchestrator orchestrator;

        public static void EnsureFor(R1RunController owner)
        {
            if(owner!=null&&owner.GetComponent<Vfx001R6LiveBridge>()==null)
                owner.gameObject.AddComponent<Vfx001R6LiveBridge>();
        }
        private void Awake()
        {
            run=GetComponent<R1RunController>();
            var root=new GameObject("VFX001 Live Pool (cosmetics only)");root.transform.SetParent(transform,false);
            pool=root.AddComponent<VfxPoolManager>();orchestrator=root.AddComponent<VfxFeedbackOrchestrator>();
            // No transform jolt or camera receiver is bound to physical/control roots.
            orchestrator.Bind(pool);
        }
        private bool InEncounter=>run!=null&&run.Progress!=null&&!run.Transitioning&&
            (run.Progress.State==R1RunState.Combat||run.Progress.State==R1RunState.TestDrive);
        private bool CanEmit=>visualEffectsEnabled&&InEncounter&&!run.Paused;
        private void OnEnable()
        {
            R6CombatFeedback.ResolvedHit+=OnHit;R3Debris.Separated+=OnDetached;
            RocketProjectile.Detonated+=OnExplosion;RRLCoverAdapter.ResolvedHit+=OnCoverHit;
        }
        private void OnDisable()
        {
            R6CombatFeedback.ResolvedHit-=OnHit;R3Debris.Separated-=OnDetached;
            RocketProjectile.Detonated-=OnExplosion;RRLCoverAdapter.ResolvedHit-=OnCoverHit;
            if(pool!=null)pool.ClearAll();R6CombatFeedback.ClearTransientVisuals();
        }
        private void LateUpdate()
        {
            if(InEncounter)return;pool.ClearAll();R6CombatFeedback.ClearTransientVisuals();
        }
        private void OnHit(P12PipelineSafeDamageable part,Vector3 point,bool destroyed)
        {
            if(!CanEmit||part==null)return;
            var mounted=part.GetComponent<R3MountedPart>();var block=part.GetComponent<P12_PipelineSafePlayerBlock>();
            var enemy=part.GetComponentInParent<P12PipelineSafeEnemyTurret>();
            bool core=mounted?.Kind==R3PartKind.Core||block?.IsCore==true||(enemy!=null&&enemy.Core==part);
            var material=core?ImpactMaterialType.Core:mounted?.Kind==R3PartKind.Wheel?ImpactMaterialType.Rubber:ImpactMaterialType.Metal;
            var kind=destroyed?(core?VfxEventKind.CoreDestroyed:VfxEventKind.Explosion):VfxEventKind.Hit;
            orchestrator.Emit(new VfxEvent(kind,point,point-part.transform.position,material,destroyed?1:.45f,part.GetInstanceID()));
        }
        private void OnDetached(R3Debris part)
        {
            if(CanEmit&&part!=null)orchestrator.Emit(new VfxEvent(VfxEventKind.Detached,part.transform.position,
                Vector3.up,ImpactMaterialType.Metal,.5f,part.GetInstanceID()));
        }
        private void OnExplosion(RocketProjectile rocket,Vector3 point,Vector3 normal)
        {
            if(CanEmit)orchestrator.Emit(new VfxEvent(VfxEventKind.Explosion,point,normal,ImpactMaterialType.Metal,1,rocket.GetInstanceID()));
        }
        private void OnCoverHit(RRLCoverAdapter cover,Vector3 point,Vector3 direction,bool broken)
        {
            if(CanEmit)orchestrator.Emit(new VfxEvent(broken?VfxEventKind.Explosion:VfxEventKind.Hit,point,-direction,
                ImpactMaterialType.Metal,broken?.8f:.45f,cover.GetInstanceID()));
        }
        public void SetVisualEffectsEnabled(bool enabled)
        {
            visualEffectsEnabled=enabled;if(!enabled&&pool!=null)pool.ClearAll();
        }
    }
}

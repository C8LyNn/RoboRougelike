using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.SEN001;
using RoboRougelike.Mechanics.HAZ001;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.PackageIntegration
{
    // One informational trigger per actual actor. Never changes its HP/body or visual mesh.
    [DisallowMultipleComponent]
    public sealed class RRLPulseSignal : MonoBehaviour
    {
        public const int SignalLayer=2; // Built-in IgnoreRaycast; normal combat queries ignore triggers.
        public ScannerDetectable Tag {get;private set;}
        SphereCollider volume;P12PipelineSafeDamageable core;
        void Create(ScannerSignalKind kind)
        {
            if(Tag==null)
            {
                var child=new GameObject("Scanner signal");child.layer=SignalLayer;child.transform.SetParent(transform,false);child.transform.localPosition=Vector3.up*.5f;
                Tag=child.AddComponent<ScannerDetectable>();volume=child.AddComponent<SphereCollider>();volume.radius=.2f;volume.isTrigger=true;
            }
            Tag.SetSignalKind(kind);Tag.enabled=true;volume.enabled=true;
        }
        static RRLPulseSignal Ensure(GameObject owner)
        {var signal=owner.GetComponent<RRLPulseSignal>();return signal!=null?signal:owner.AddComponent<RRLPulseSignal>();}
        void Enemy(P12PipelineSafeEnemyTurret owner)
        {
            Create(ScannerSignalKind.Enemy);
            if(core!=owner.Core){if(core!=null)core.Died-=Lost;core=owner.Core;if(core!=null)core.Died+=Lost;}
            if(owner.IsDefeated)Lost();
        }
        void Lost(){if(Tag!=null)Tag.enabled=false;if(volume!=null)volume.enabled=false;}
        void OnDestroy(){if(core!=null)core.Died-=Lost;}
        public static void Prepare(R1RunController run)
        {
            foreach(var enemy in RRLScanWorld.LiveEnemies(run))Ensure(enemy.gameObject).Enemy(enemy);
            // Bounded by the existing70-piece debris lifetime/cap, only on explicit pulse.
            foreach(var wreck in Object.FindObjectsByType<R3Debris>(FindObjectsSortMode.None))
                if(wreck!=null&&wreck.isActiveAndEnabled)Ensure(wreck.gameObject).Create(ScannerSignalKind.Salvage);
            var layout=run?.Arena?.Layout;
            if(layout!=null&&layout.gameObject.activeInHierarchy)
                foreach(var hazard in layout.GetComponentsInChildren<TelegraphedHazardZone>())Ensure(hazard.gameObject).Create(ScannerSignalKind.Hazard);
        }
    }
}

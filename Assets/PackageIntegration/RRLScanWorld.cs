using System.Collections.Generic;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R6;
using RoboRougelike.Mechanics.StructuralScan;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.PackageIntegration
{
    public enum RRLScanKind { Structural, Pulse }
    public interface IRRLScanDevice
    {
        RoboRougelike.WeaponIntegration.RRLDeviceBinding Device {get;}
        RRLScanKind Kind {get;}
        float CooldownLeft {get;}
        bool TryActivate();
    }
    // Activation-time registry only; no scene search or mutable HP snapshot owner.
    public static class RRLScanWorld
    {
        public static IEnumerable<P12PipelineSafeEnemyTurret> LiveEnemies(R1RunController run)
        {
            if(run==null)yield break;
            if(run.Encounter!=null)foreach(var enemy in run.Encounter.Enemies)
                if(enemy!=null&&!enemy.IsDefeated&&enemy.gameObject.activeInHierarchy)yield return enemy;
            var target=run.GetComponent<R6TestRange>()?.TrainingTarget;
            if(target!=null&&!target.IsDefeated&&target.gameObject.activeInHierarchy)yield return target;
        }
        public static bool InEncounter(R1RunController run)=>run?.Progress!=null&&!run.Transitioning&&
            (run.Progress.State==R1RunState.Combat||run.Progress.State==R1RunState.TestDrive);
        public static void Register(StructuralScanRegistry registry,R1RunController run)
        {
            registry.Clear();
            foreach(var enemy in LiveEnemies(run))foreach(var part in enemy.GetComponentsInChildren<P12PipelineSafeDamageable>())
            {
                if(part==null||part.IsDead||!part.gameObject.activeInHierarchy)continue;
                var scan=part.GetComponent<COM001LiveScanPartAdapter>();if(scan==null)scan=part.gameObject.AddComponent<COM001LiveScanPartAdapter>();
                var functional=part.GetComponent<P12PipelineSafeFunctionalModule>();
                var type=part==enemy.Core?StructuralPartType.Core:
                    part==enemy.WeaponBlock||functional?.ModuleType==P12FunctionalModuleType.Weapon?StructuralPartType.Weapon:
                    functional?.ModuleType==P12FunctionalModuleType.Wheel||part.name.StartsWith("Koło")?StructuralPartType.Wheel:StructuralPartType.Armor;
                scan.Bind(enemy.transform,part,enemy.GetInstanceID(),2,type);if(scan.IsAttached)registry.Register(scan);
            }
        }
    }
}

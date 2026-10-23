using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.Mechanics.BeamCutter;
using RogueMachine.P12PipelineSafe;

public class RRLW6BeamPhysicalTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator MountedBeamUsesRealRayHeatAndPreservesThermalStateAcrossRebuild()
    {
        Assert.That(Enum.TryParse("BeamCutter",out R4WeaponKind kind),Is.True,"WPN003 absent from real garage catalog");
        var run=R1RunController.Instance;var palette=run.GetComponent<R3VisualPalette>();
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));model.Award(500);
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=kind;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(100,4,100),Quaternion.identity);
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.First(p=>p.Id==state.Id);
        var module=gun.GetComponent<BeamCutterModule>();Assert.That(module,Is.Not.Null,"Use original module on physical gun");
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4;
        target.transform.localScale=new Vector3(2,2,.5f);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);
        Physics.SyncTransforms();assembly.SendMessage("SetContinuousFireHeld",true,SendMessageOptions.RequireReceiver);assembly.FireGuns();
        float end=Time.time+.2f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(4f,10f),"Original35DPS integrated over actual fixed steps, not multiplied twice");
        Assert.That(module.HeatFraction,Is.GreaterThan(0));
        assembly.SendMessage("SetContinuousFireHeld",false,SendMessageOptions.RequireReceiver);
        float heat=module.HeatFraction;assembly.Capture();
        var restored=R3VehicleAssembly.Build(model.DeepCopy(),new R1RunStats(),palette,new Vector3(150,4,100),Quaternion.identity);
        var restoredModule=restored.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id).GetComponent<BeamCutterModule>();
        Assert.That(restoredModule.HeatFraction,Is.EqualTo(heat).Within(.0001),"Rebuild cannot erase heat/lockout");
        float hpAtRelease=hp.CurrentHp;end=Time.time+.1f;while(Time.time<end)yield return null;
        Assert.That(hp.CurrentHp,Is.EqualTo(hpAtRelease),"Release stops continuous damage");
        gun.Damageable.ApplyDamage(10000,gun.transform.position,Vector3.up);
        module.SetTriggerHeld(true);Assert.That(module.ManualFixedStep(.2f).Status,Is.EqualTo(BeamStepStatus.Inoperable));
        assembly.Capture();Assert.That(model.Repair(gun.Cell),Is.True);Assert.That(model.DeepCopy().Parts.Single(p=>p.Id==state.Id).Weapon,Is.EqualTo(kind));
    }
    [UnityTest] public IEnumerator PausedGarageMountingBeamKeepsVehicleInCamera()
    {
        var run=R1RunController.Instance;RRLW2TestFlow.DefeatAll(run);
        float deadline=Time.realtimeSinceStartup+10;
        while(run.Progress.State!=R1RunState.Salvage&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Salvage));
        Assert.That(run.ConfirmRewards(Array.Empty<string>(),new[]{0}),Is.True);run.VehicleModel.Award(200);
        Assert.That(Time.timeScale,Is.Zero);
        Assert.That(run.GarageBuyWeapon(new Vector3Int(1,2,0),R4WeaponKind.BeamCutter),Is.True);
        int ready=Time.frameCount+3;while(Time.frameCount<ready)yield return null;
        var beam=run.Player.GetComponentInChildren<BeamCutterModule>();Assert.That(beam,Is.Not.Null);
        Assert.That(beam.GetComponentInChildren<LineRenderer>().enabled,Is.False,"An idle beam is already hidden by the source module");
        var camera=Camera.main;var bounds=run.Player.Core.GetComponent<Renderer>().bounds;
        var viewport=camera.WorldToViewportPoint(bounds.center);
        Assert.That(GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),bounds),Is.True,
            $"Physical core must stay in the paused garage view; viewport={viewport}, camera={camera.transform.position}, core={bounds.center}");
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}

    [UnityTest] public IEnumerator FourEnemyEncounterActuallyEquipsBeamAndSalvagesItsThermalState()
    {
        var run=R1RunController.Instance;
        // Use the same pristine scene template as the actual run controller.
        var template=(P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",
            System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,run.Player.transform,new R1EncounterDefinition{EnemyCount=3},run.Salvage,"beam-fixture");
        Assert.That(run.Encounter.Enemies.Count,Is.EqualTo(4));
        var enemy=run.Encounter.Enemies.FirstOrDefault(e=>e.GetComponentInChildren<BeamCutterModule>()!=null);
        Assert.That(enemy,Is.Not.Null,"Gauntlet supports at most4 enemies: beam equipment must be reachable");
        run.Encounter.SetCombatActive(false);
        // Normal gameplay starts the spawned enemy before a projectile can kill it.
        yield return null;
        var binding=enemy.GetComponentInChildren<RoboRougelike.WeaponIntegration.RRLBeamBinding>();
        binding.Module.RestoreThermalState(60,true);binding.CaptureState();string id=binding.State.Id;
        enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        var salvage=run.Salvage.Items.SingleOrDefault(p=>p.Id==id);
        Assert.That(salvage,Is.Not.Null,"Missing "+id+" from actual ledger: "+string.Join(",",run.Salvage.Items.Select(p=>p.Id)));
        var item=salvage.ToPart();
        Assert.That(item.Weapon,Is.EqualTo(R4WeaponKind.BeamCutter));Assert.That(item.ThermalHeat,Is.EqualTo(60));
        Assert.That(item.ThermalLocked,Is.True);yield return null;
    }
}

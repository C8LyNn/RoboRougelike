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
using RoboRougelike.Weapons.RotarySaw;
using RogueMachine.P12PipelineSafe;

public class RRLW6SawPhysicalTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
    }
    [UnityTest] public IEnumerator ActualSawCutsOneLogicalPartJamsOnHardWallAndPreservesHeat()
    {
        Assert.That(Enum.TryParse("RotarySaw",out R4WeaponKind kind),Is.True,"WPN005 missing actual weapon");
        var run=R1RunController.Instance;var palette=run.GetComponent<R3VisualPalette>();
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=kind;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(100,4,100),Quaternion.identity);
        // A static cutting target needs a supported rig; gravity must remain enabled.
        var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.transform.position=new Vector3(100,2.85f,100);platform.transform.localScale=new Vector3(12,1,12);
        Physics.SyncTransforms();yield return MOV009IntegrationTests.Seconds(.7f);
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.Single(p=>p.Id==state.Id);
        var module=gun.GetComponent<RotarySawModule>();Assert.That(module,Is.Not.Null);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*.45f;
        target.transform.localScale=Vector3.one*.2f;var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);
        var extra=new GameObject("second collider same saw target");extra.transform.SetParent(target.transform,false);extra.AddComponent<BoxCollider>();
        Physics.SyncTransforms();assembly.SetContinuousFireHeld(true);
        float end=Time.time+.45f;while(Time.time<end)yield return null;
        Assert.That(hp.CurrentHp,Is.LessThan(499));Assert.That(module.DamageEventsLastTick,Is.EqualTo(1));
        Assert.That(module.Motor.Heat,Is.GreaterThan(0));
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=gun.Muzzle.position+gun.Muzzle.forward*.65f;wall.transform.localScale=Vector3.one*.15f;
        Physics.SyncTransforms();float before=hp.CurrentHp;end=Time.time+.06f;while(Time.time<end)yield return null;
        Assert.That(module.Motor.JamRemaining,Is.GreaterThan(1));Assert.That(hp.CurrentHp,Is.EqualTo(before),"Hard contact rejects entire cut batch");
        assembly.Capture();float heat=module.Motor.Heat;
        var rebuilt=R3VehicleAssembly.Build(model.DeepCopy(),new R1RunStats(),palette,new Vector3(150,4,100),Quaternion.identity);
        var restored=rebuilt.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id).GetComponent<RotarySawModule>();
        Assert.That(restored.Motor.Heat,Is.EqualTo(heat).Within(.001));Assert.That(restored.Motor.JamRemaining,Is.GreaterThan(1));
        gun.Damageable.ApplyDamage(10000,gun.transform.position,Vector3.up);Assert.That(module.IsOperational,Is.False);
        assembly.Capture();model.Award(500);Assert.That(model.Repair(gun.Cell),Is.True);
        var repaired=model.DeepCopy().Parts.Single(p=>p.Id==state.Id);
        Assert.That(repaired.Health,Is.EqualTo(1));Assert.That(repaired.Weapon,Is.EqualTo(kind));
        Assert.That(repaired.ThermalHeat,Is.EqualTo(state.ThermalHeat));Assert.That(repaired.DeviceReadyAt,Is.EqualTo(state.DeviceReadyAt));
    }
    [UnityTest] public IEnumerator WastelandFourEnemyEscortActuallyUsesMeleeApproachAndSawMotor()
    {
        Assert.That(Enum.TryParse("RotarySaw",out R4WeaponKind kind),Is.True,"Live saw not registered");
        var run=R1RunController.Instance;
        var template=(P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(run);
        run.Encounter.Spawn(template,run.Player.transform,new R1EncounterDefinition{EnemyCount=3,Arena=RoboRougelike.R5.R5ArenaKind.Wasteland},run.Salvage,"saw-fixture");
        run.Encounter.SetCombatActive(false);yield return null;
        var enemy=run.Encounter.Enemies.FirstOrDefault(e=>e.GetComponentInChildren<RotarySawModule>()!=null);
        Assert.That(enemy,Is.Not.Null,"Saw AI must be reachable in actual arena roster");
        var body=enemy.GetComponent<Rigidbody>();run.Player.GetComponent<Rigidbody>().position=new Vector3(100,4,100);body.position=new Vector3(100,4,108);
        // This case tests melee approach/cutting, with the initial aim already facing the target.
        enemy.GetComponentsInChildren<Transform>().Single(t=>t.name=="Enemy_Yaw").rotation=Quaternion.LookRotation(Vector3.back);
        enemy.SetCombatActive(true);Physics.SyncTransforms();float distance=Vector3.Distance(body.position,run.Player.transform.position);
        float end=Time.time+1;while(Time.time<end)yield return null;
        Assert.That(Vector3.Distance(body.position,run.Player.transform.position),Is.LessThan(distance-1));
        Assert.That(enemy.GetComponentInChildren<RotarySawModule>().Motor.Rpm,Is.GreaterThan(0));
    }
    [UnityTest] public IEnumerator GarageSawPreviewShowsTheActualWorldSizedCuttingDisc()
    {
        var preview=GameObject.CreatePrimitive(PrimitiveType.Cube);preview.transform.position=new Vector3(100,4,100);preview.transform.localScale=Vector3.one*.52f;
        RoboRougelike.R4_1.R41PartGeometry.Payload(preview.transform,R3PartKind.Gun,R4WeaponKind.RotarySaw,preview.GetComponent<Renderer>().sharedMaterial,false,out var center);
        var disc=center.Find("WPN005 visual rotor");Assert.That(disc,Is.Not.Null,"Garage must preview the physical saw disc, not an empty gun anchor");
        var size=disc.GetComponent<Renderer>().bounds.size;
        Assert.That(size.x,Is.EqualTo(.28f).Within(.01));Assert.That(size.y,Is.EqualTo(1.8f).Within(.01));Assert.That(size.z,Is.EqualTo(1.8f).Within(.01));
        Assert.That(disc.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True,"Visual disc does not add a second collision body");yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}

using System.Linq;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R1;
using RoboRougelike.WeaponIntegration;
using RoboRougelike.Weapons.RotarySaw;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogContinuousTierTests
{
    [TestCase(R4WeaponKind.BeamCutter)]
    [TestCase(R4WeaponKind.RotarySaw)]
    public void FusionKeepsHighestHeatAndAnyOverheatLock(R4WeaponKind kind)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(R3PartKind.Gun,kind),Is.True);
        var input=model.Inventory.ToArray();input[0].ThermalHeat=.2f;input[1].ThermalHeat=.8f;input[2].ThermalHeat=.5f;input[2].ThermalLocked=true;
        Assert.That(model.FuseInventory(input.Select(p=>p.Id).ToArray()),Is.True);
        var result=model.Inventory.Single();Assert.That(result.Tier,Is.EqualTo(2));
        Assert.That(result.ThermalHeat,Is.EqualTo(.8f));Assert.That(result.ThermalLocked,Is.True);
    }
}

public sealed class BacklogContinuousTierPhysicsTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        for(int i=0;i<300&&R1RunController.Instance?.Progress==null;i++)yield return null;
        run=R1RunController.Instance;Assert.That(run.StartRun()&&run.ChooseRoute(0),Is.True);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
    }
    R3MountedPart Build(R4WeaponKind weapon,out VehicleEnergyBank bank)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=weapon;state.Tier=2;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().useGravity=false;bank=vehicle.GetComponent<VehicleEnergyBank>();
        return vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
    }
    [UnityTest] public IEnumerator BeamTierTwoConsumesScaledEnergyAndHitsRealHp()
    {
        var gun=Build(R4WeaponKind.BeamCutter,out var bank);var binding=gun.GetComponent<RRLBeamBinding>();
        var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4;
        item.transform.localScale=new Vector3(3,3,.5f);var hp=item.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);
        Physics.SyncTransforms();float energyBefore=bank.Current;
        binding.Module.SetTriggerHeld(true);var step=binding.Module.ManualFixedStep(.1f);
        Assert.That(step.Status.ToString(),Is.EqualTo("Firing"));
        Assert.That(hp.CurrentHp,Is.EqualTo(493).Within(.01));
        Assert.That(bank.Current,Is.EqualTo(energyBefore-.72f).Within(.01));
        yield return null;
    }
    [UnityTest] public IEnumerator BeamHitsHostileDomeBeforeProtectedTarget()
    {
        var gun=Build(R4WeaponKind.BeamCutter,out var bank);var binding=gun.GetComponent<RRLBeamBinding>();
        var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.transform.position=gun.Muzzle.position+gun.Muzzle.forward*8;
        item.transform.localScale=new Vector3(3,3,.5f);var target=item.AddComponent<P12PipelineSafeDamageable>();target.Configure(500,false);
        var dome=new GameObject("shield beam fixture");dome.transform.position=gun.Muzzle.position+gun.Muzzle.forward*5+gun.Muzzle.up;
        dome.AddComponent<Rigidbody>().isKinematic=true;dome.AddComponent<SphereCollider>().isTrigger=true;
        var hp=dome.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);dome.AddComponent<R3ShieldDome>().Configure(2,2);
        Physics.SyncTransforms();binding.Module.SetTriggerHeld(true);var step=binding.Module.ManualFixedStep(.1f);
        Assert.That(step.Status.ToString(),Is.EqualTo("Firing"));Assert.That(hp.CurrentHp,Is.LessThan(500));
        Assert.That(target.CurrentHp,Is.EqualTo(500),"The beam must not damage the protected target");
        Object.Destroy(dome);Object.Destroy(item);yield return null;
    }
    [UnityTest] public IEnumerator BeamDamagesDomeEvenWithoutTargetBehindIt()
    {
        var gun=Build(R4WeaponKind.BeamCutter,out var bank);var binding=gun.GetComponent<RRLBeamBinding>();
        var dome=new GameObject("shield beam empty fixture");dome.transform.position=gun.Muzzle.position+gun.Muzzle.forward*5+gun.Muzzle.up;
        dome.AddComponent<Rigidbody>().isKinematic=true;dome.AddComponent<SphereCollider>().isTrigger=true;
        var hp=dome.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);dome.AddComponent<R3ShieldDome>().Configure(2,2);
        Physics.SyncTransforms();binding.SetTriggerHeld(true);
        float deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline&&hp.CurrentHp>=500)yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(hp.CurrentHp,Is.LessThan(500),"A beam must hit the dome even when it otherwise hits nothing");
        binding.SetTriggerHeld(false);Object.Destroy(dome);
    }
    [UnityTest] public IEnumerator SawTierTwoScalesRealDamageSinkAndMotorEnergySetting()
    {
        var gun=Build(R4WeaponKind.RotarySaw,out var bank);var binding=gun.GetComponent<RRLSawBinding>();
        var config=(RotarySawConfiguration)typeof(RotarySawModule).GetField("configuration",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(binding.Module);
        Assert.That(config.energyPerSecond,Is.EqualTo(7.2f).Within(.001));
        var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.transform.position=gun.Muzzle.position+gun.Muzzle.forward;
        var hp=item.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);var collider=item.GetComponent<Collider>();
        Assert.That(binding.TryResolve(collider,out var target),Is.True);
        binding.ApplyDamage(new SawDamageEvent(target.partId,10,item.transform.position,Vector3.up,collider,binding.Module));
        Assert.That(hp.CurrentHp,Is.EqualTo(480).Within(.01));
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}

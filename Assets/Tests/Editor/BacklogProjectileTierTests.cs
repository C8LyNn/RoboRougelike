using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R1;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogProjectileTierTests
{
    [TestCase(R4WeaponKind.Shotgun)]
    [TestCase(R4WeaponKind.Railgun)]
    [TestCase(R4WeaponKind.RocketLauncher)]
    public void SameProjectileWeaponFusesAndDoesNotCreateAmmo(R4WeaponKind weapon)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(R3PartKind.Gun,weapon),Is.True);
        var input=model.Inventory.ToArray();input[0].Ammo=-1;input[1].Ammo=2;input[2].Ammo=5;
        input[1].ReloadReadyAt=50;
        Assert.That(model.FuseInventory(input.Select(p=>p.Id).ToArray()),Is.True);
        var result=model.Inventory.Single();Assert.That(result.Tier,Is.EqualTo(2));
        Assert.That(result.Weapon,Is.EqualTo(weapon));Assert.That(result.Ammo,Is.EqualTo(2));
        Assert.That(result.ReloadReadyAt,Is.EqualTo(50));
    }
}

public sealed class BacklogProjectileTierPhysicsTests
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
        vehicle.GetComponent<Rigidbody>().isKinematic=true;bank=vehicle.GetComponent<VehicleEnergyBank>();
        return vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
    }
    P12PipelineSafeDamageable Target(R3MountedPart gun,float distance,float width=4)
    {
        var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.transform.position=gun.Muzzle.position+gun.Muzzle.forward*distance;
        item.transform.localScale=new Vector3(width,4,1);
        var hp=item.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);Physics.SyncTransforms();return hp;
    }
    [UnityTest] public IEnumerator ShotgunTierTwoConsumesSixEnergyAndDamagesWithRealPellets()
    {
        var gun=Build(R4WeaponKind.Shotgun,out var energy);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLShotgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-6).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.35f;while(Time.time<end)yield return null;
        Assert.That(hp.CurrentHp,Is.EqualTo(356).Within(.2),"Eight real pellets each deal 18 damage.");
    }
    [UnityTest] public IEnumerator RailgunTierTwoConsumesEnergyAndPenetratesRealTarget()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLRailgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-10.8f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.35f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(100f,130f));
    }
    [UnityTest] public IEnumerator RocketTierTwoConsumesEnergyAndExplodesAgainstRealTarget()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy);var hp=Target(gun,6,2);float before=energy.Current;
        var binding=gun.GetComponent<RRLRocketBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-9f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(9));
        float end=Time.time+.8f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(100f,120f));
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}

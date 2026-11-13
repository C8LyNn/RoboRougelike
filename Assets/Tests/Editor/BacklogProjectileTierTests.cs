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

public sealed class RailgunFriendlyFixture : MonoBehaviour,IRRLTeamMember
{ public int TeamId=>1; }
public sealed class RailgunWallFixture : MonoBehaviour,IRRLHitReceiver
{
    public int HitCount {get;private set;}
    public bool ReceiveHit(float damage,Vector3 point,Vector3 direction,Transform source){HitCount++;return true;}
}

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
    R3MountedPart Build(R4WeaponKind weapon,out VehicleEnergyBank bank,bool doubleShot=false)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=weapon;state.Tier=2;
        if(doubleShot)
        {
            model.Economy.DoubleShotChancePerUnit=1;model.Award(1000);
            var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct()
                .First(c=>model.PlacementError(c,R3PartKind.DoubleShot)==null);
            Assert.That(model.Buy(cell,R3PartKind.DoubleShot),Is.True);
        }
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
    [UnityTest] public IEnumerator DoubleShotDuplicatesShotgunPelletsWithoutExtraAmmoOrEnergy()
    {
        var gun=Build(R4WeaponKind.Shotgun,out var energy,true);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLShotgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-6).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.4f;while(Time.time<end)yield return null;
        Assert.That(hp.CurrentHp,Is.EqualTo(212).Within(.2),"Sixteen pellets deal 18 damage each, without recursive procs.");
    }
    [UnityTest] public IEnumerator RailgunTierTwoConsumesEnergyAndPenetratesRealTarget()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLRailgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-10.8f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.35f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(100f,130f));
    }
    [UnityTest] public IEnumerator DoubleShotDuplicatesRailgunWithoutExtraAmmoOrEnergy()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy,true);var hp=Target(gun,6);float before=energy.Current;
        var binding=gun.GetComponent<RRLRailgunBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-10.8f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(11));
        float end=Time.time+.4f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(200f,260f),"Two real rail projectiles must hit without recursive procs.");
    }
    [UnityTest] public IEnumerator RailgunDoesNotVanishAmongManyIgnoredFriendlyColliders()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);
        var fixture=new GameObject("Ignored friendly colliders");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*2.5f;
        fixture.AddComponent<RailgunFriendlyFixture>();
        for(int i=0;i<150;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.05f;
        Physics.SyncTransforms();
        int crowded=Physics.SphereCastAll(gun.Muzzle.position,.075f,gun.Muzzle.forward,3f,~0,QueryTriggerInteraction.Ignore)
            .Count(h=>h.collider.gameObject==fixture);
        Assert.That(crowded,Is.EqualTo(150),"The real cast must overflow the old 128-hit buffer.");
        try
        {
            Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
            float end=Time.time+.5f;while(Time.time<end)yield return null;
            Assert.That(hp.CurrentHp,Is.LessThan(500),"Ignored friendly colliders must not destroy the projectile before it reaches a valid target.");
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTest] public IEnumerator CrowdedRailgunStillStopsAtWorldWallBeforeTarget()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);
        var fixture=new GameObject("Ignored friendly colliders");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*2.5f;
        fixture.AddComponent<RailgunFriendlyFixture>();
        for(int i=0;i<150;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.05f;
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name="Solid wall";wall.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4f;
        var receiver=wall.AddComponent<RailgunWallFixture>();
        wall.transform.localScale=new Vector3(4,4,.25f);Physics.SyncTransforms();
        try
        {
            Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
            float end=Time.time+.5f;while(Time.time<end)yield return null;
            Assert.That(receiver.HitCount,Is.EqualTo(1),"The projectile must actually reach the wall.");
            Assert.That(hp.CurrentHp,Is.EqualTo(500),"Complete queries must still process the first solid obstacle before the target.");
        }
        finally{Object.Destroy(fixture);Object.Destroy(wall);}
    }
    [UnityTest] public IEnumerator RailgunFallsBackToCompleteQueryBeyondMaximumBuffer()
    {
        var gun=Build(R4WeaponKind.Railgun,out var energy);var hp=Target(gun,6);
        var fixture=new GameObject("Extreme friendly collider crowd");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*2.5f;
        fixture.AddComponent<RailgunFriendlyFixture>();
        for(int i=0;i<4100;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.05f;
        Physics.SyncTransforms();
        Assert.That(Physics.SphereCastAll(gun.Muzzle.position,.075f,gun.Muzzle.forward,3f,~0,QueryTriggerInteraction.Ignore)
            .Count(h=>h.collider.gameObject==fixture),Is.EqualTo(4100));
        try
        {
            Assert.That(gun.GetComponent<RRLRailgunBinding>().Fire(),Is.True);
            float end=Time.time+.5f;while(Time.time<end)yield return null;
            Assert.That(hp.CurrentHp,Is.LessThan(500),"The bounded buffer must fall back to a complete query without losing the shot.");
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTest] public IEnumerator RocketTierTwoConsumesEnergyAndExplodesAgainstRealTarget()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy);var hp=Target(gun,6,2);float before=energy.Current;
        var binding=gun.GetComponent<RRLRocketBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-9f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(9));
        float end=Time.time+.8f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(100f,120f));
    }
    [UnityTest] public IEnumerator DoubleShotDuplicatesRocketWithoutExtraAmmoOrEnergy()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy,true);var hp=Target(gun,6,2);float before=energy.Current;
        var binding=gun.GetComponent<RRLRocketBinding>();Assert.That(binding.Fire(),Is.True);
        Assert.That(energy.Current,Is.EqualTo(before-9f).Within(.001));Assert.That(binding.State.Ammo,Is.EqualTo(9));
        float end=Time.time+1f;while(Time.time<end)yield return null;
        Assert.That(500-hp.CurrentHp,Is.InRange(200f,240f),"Two real rockets must explode without recursive procs.");
    }
    [UnityTest] public IEnumerator RocketBlastDamagesTargetAmongHundredsOfUnrelatedColliders()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy);var hp=Target(gun,6,2);
        var fixture=new GameObject("Crowded explosion fixture");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6f+gun.Muzzle.right*2f;
        for(int i=0;i<300;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.02f;
        Physics.SyncTransforms();
        Assert.That(Physics.OverlapSphere(fixture.transform.position,1).Count(c=>c.gameObject==fixture),Is.EqualTo(300));
        try
        {
            Assert.That(gun.GetComponent<RRLRocketBinding>().Fire(),Is.True);
            float end=Time.time+1f;while(Time.time<end)yield return null;
            Assert.That(hp.CurrentHp,Is.LessThan(500),"Unrelated nearby colliders must not erase an otherwise valid explosion.");
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTest] public IEnumerator RocketBlastStillFailsClosedBeyondConfiguredBound()
    {
        var gun=Build(R4WeaponKind.RocketLauncher,out var energy);var hp=Target(gun,6,2);
        var fixture=new GameObject("Extreme explosion saturation fixture");
        fixture.transform.position=gun.Muzzle.position+gun.Muzzle.forward*6f+gun.Muzzle.right*2f;
        for(int i=0;i<1030;i++)fixture.AddComponent<BoxCollider>().size=Vector3.one*.02f;
        Physics.SyncTransforms();
        try
        {
            LogAssert.Expect(LogType.Warning,"WPN-004: blast overlap overflow; no explosion damage applied.");
            Assert.That(gun.GetComponent<RRLRocketBinding>().Fire(),Is.True);
            float end=Time.time+1f;while(Time.time<end)yield return null;
            Assert.That(hp.CurrentHp,Is.EqualTo(500),"An incomplete query must not apply a partial explosion.");
        }
        finally{Object.Destroy(fixture);}
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}

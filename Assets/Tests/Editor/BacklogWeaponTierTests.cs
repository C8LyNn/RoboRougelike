using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R1;
using RogueMachine.P12PipelineSafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogWeaponTierTests
{
    [Test] public void ThreeIdenticalBasicGunsFuseToOneTierTwoGun()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(500);
        for(int i=0;i<3;i++)Assert.That(model.PurchaseToInventory(R3PartKind.Gun,R4WeaponKind.Basic),Is.True);
        Assert.That(model.FuseInventory(model.Inventory.Select(p=>p.Id).ToArray()),Is.True);
        var gun=model.Inventory.Single();Assert.That(gun.Weapon,Is.EqualTo(R4WeaponKind.Basic));Assert.That(gun.Tier,Is.EqualTo(2));
    }
    [Test] public void DifferentWeaponsAndUnsupportedPartsNeverFuse()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        Assert.That(model.PurchaseToInventory(R3PartKind.Gun,R4WeaponKind.Basic),Is.True);
        Assert.That(model.PurchaseToInventory(R3PartKind.Gun,R4WeaponKind.Automatic),Is.True);
        Assert.That(model.PurchaseToInventory(R3PartKind.Gun,R4WeaponKind.Heavy),Is.True);
        var mixed=model.Inventory.Select(p=>p.Id).ToArray();
        Assert.That(model.FuseInventory(mixed),Is.False);Assert.That(model.Inventory.Select(p=>p.Id),Is.EqualTo(mixed));
        Assert.That(R3VehicleModel.SupportsFusion(R3PartKind.StructuralScanner),Is.False);
    }
}

public sealed class BacklogWeaponTierPhysicsTests
{
    [UnitySetUp] public IEnumerator Setup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);yield return new EnterPlayMode();}
    [UnityTest] public IEnumerator DoubleShotTriggersOnceWithoutExtraEnergyOrRecursiveShots()
    {
        Assert.That(System.Enum.TryParse("DoubleShot",out R3PartKind kind),Is.True);
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));model.Award(1000);
        var chance=typeof(R3GarageEconomy).GetField("DoubleShotChancePerUnit");Assert.That(chance,Is.Not.Null);chance.SetValue(model.Economy,1f);
        var cell=model.Parts.SelectMany(p=>R3VehicleModel.Neighbours.Select(d=>p.Cell+d)).Distinct().First(c=>model.PlacementError(c,kind)==null);
        Assert.That(model.Buy(cell,kind),Is.True);
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4f;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var bank=vehicle.GetComponent<VehicleEnergyBank>();float before=bank.Current;Physics.SyncTransforms();
        Assert.That(assembly.ClearProjectilePath(gun),Is.True);assembly.FireGroup(false);
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>920.01f&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(920).Within(.01),"One original and exactly one extra 40-damage projectile must hit.");
        Assert.That(bank.Current,Is.EqualTo(before).Within(.001),"Extra basic shot cannot spend energy.");
        var module=assembly.Parts.Single(p=>p.Kind==kind);
        module.Damageable.RestoreHealthRatio(.01f);Assert.That(assembly.RollDoubleShotExtras(4),Is.EqualTo(1),"A damaged live passive keeps its full effect.");
        module.Tier=2;Assert.That(assembly.RollDoubleShotExtras(4),Is.EqualTo(2),"Chances above 100% produce guaranteed additional projectiles.");
        Assert.That(assembly.RollDoubleShotExtras(1),Is.EqualTo(1),"Existing projectile capacity cannot be exceeded.");module.Tier=1;
        module.gameObject.SetActive(false);Assert.That(assembly.RollDoubleShotExtras(4),Is.Zero);
        var randomState=Random.state;
        try
        {
            Random.InitState(817);float expected=Random.value;Random.InitState(817);
            Assert.That(assembly.RollDoubleShotExtras(4),Is.Zero);
            Assert.That(Random.value,Is.EqualTo(expected),"An absent bonus must not advance the game's random stream.");
        }
        finally{Random.state=randomState;}
        module.gameObject.SetActive(true);
        var receiver=module.GetComponent<RoboRougelike.Mechanics.EMP.EmpReceiver>();Assert.That(receiver,Is.Not.Null);
        receiver.ApplyEmp(2);Assert.That(assembly.RollDoubleShotExtras(4),Is.Zero);
        Object.Destroy(vehicle.gameObject);Object.Destroy(target);Object.Destroy(paletteObject);Object.Destroy(material);
    }
    [UnityTest] public IEnumerator TierTwoBasicGunDealsDoublePhysicalDamageWithoutEnergy()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var gunState=model.Parts.First(p=>p.Kind==R3PartKind.Gun);gunState.Tier=2;
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        var body=vehicle.GetComponent<Rigidbody>();body.isKinematic=true;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.Single(p=>p.Id==gunState.Id);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4f;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var bank=vehicle.GetComponent<VehicleEnergyBank>();Assert.That(bank.TrySpend(bank.Current),Is.True);
        Physics.SyncTransforms();assembly.FireGroup(false);
        Assert.That(bank.Current,Is.Zero,"The basic gun must remain free at every tier.");
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(920).Within(.01),"A real projectile deals 2× the 40 HP base damage.");
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
    }
    [UnityTest] public IEnumerator TierTwoAutomaticGunSpendsThreeEnergyAndDealsDoubleDamage()
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var gunState=model.Parts.First(p=>p.Kind==R3PartKind.Gun);gunState.Weapon=R4WeaponKind.Automatic;gunState.Tier=2;
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var assembly=vehicle.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.Single(p=>p.Id==gunState.Id);
        var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=gun.Muzzle.position+gun.Muzzle.forward*4f;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
        var bank=vehicle.GetComponent<VehicleEnergyBank>();float before=bank.Current;
        Physics.SyncTransforms();assembly.FireGroup(false);
        Assert.That(bank.Current,Is.EqualTo(before-3f).Within(.001));
        float deadline=Time.realtimeSinceStartup+5;
        while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
        Assert.That(hp.CurrentHp,Is.EqualTo(976).Within(.01),"A real projectile deals 2× the 12 HP base damage.");
        UnityEngine.Object.Destroy(vehicle.gameObject);UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(paletteObject);UnityEngine.Object.Destroy(material);
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}

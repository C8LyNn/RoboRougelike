using System.Linq;
using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R1;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.WeaponIntegration;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BacklogEmpTierTests
{
    [Test] public void ThreeEmpEmittersFuseWithoutResettingCooldown()
    {
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        model.Award(1000);
        for (int i = 0; i < 3; i++)
            Assert.That(model.PurchaseToInventory(R3PartKind.Gun, R4WeaponKind.EmpEmitter), Is.True);
        var emitters = model.Inventory.Where(p => p.Weapon == R4WeaponKind.EmpEmitter).ToArray();
        emitters[0].DeviceReadyAt = 5;
        emitters[1].DeviceReadyAt = 12;
        emitters[2].DeviceReadyAt = 8;
        Assert.That(model.FuseInventory(emitters.Select(p => p.Id)), Is.True);
        var upgraded = model.Inventory.Single();
        Assert.That(upgraded.Tier, Is.EqualTo(2));
        Assert.That(upgraded.Weapon, Is.EqualTo(R4WeaponKind.EmpEmitter));
        Assert.That(upgraded.DeviceReadyAt, Is.EqualTo(12));
    }
}

public sealed class BacklogEmpTierPhysicsTests
{
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    [UnityTest] public IEnumerator DamageableWithoutRendererStillSettlesDamage()
    {
        var invisible=new GameObject("Niewidoczny element z HP");
        var hp=invisible.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(100,false);
        Assert.That(invisible.GetComponent<Renderer>(),Is.Null);
        Assert.DoesNotThrow(()=>hp.ApplyDamage(10,invisible.transform.position,Vector3.up));
        Assert.That(hp.CurrentHp,Is.EqualTo(90).Within(.001));
        bool died=false;hp.Died+=()=>died=true;
        Assert.DoesNotThrow(()=>hp.ApplyDamage(100,invisible.transform.position,Vector3.up));
        Assert.That(hp.IsDead,Is.True);Assert.That(died,Is.True,"Cosmetic feedback must not interrupt death callbacks.");
        Object.Destroy(invisible);yield return null;
    }
    [UnityTest] public IEnumerator MuzzleWithoutRendererStillProducesNoException()
    {
        var barrel=new GameObject("Lufa bez renderera");
        barrel.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Configure(100,false);
        Assert.DoesNotThrow(()=>RoboRougelike.R6.R6CombatFeedback.Muzzle(barrel.transform,R4WeaponKind.Basic));
        Object.Destroy(barrel);yield return null;
    }

    [UnityTest] public IEnumerator TierTwoAuraSpendsEnergyAndDamagesAnEnemyCollider()
    {
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state = model.Parts.First(p => p.Kind == R3PartKind.Gun);
        state.Weapon = R4WeaponKind.EmpEmitter; state.Tier = 2;
        var paletteObject = new GameObject("QA palette");
        var palette = paletteObject.AddComponent<R3VisualPalette>();
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for (int i = 0; i < palette.Parts.Length; i++) palette.Parts[i] = material;
        for (int i = 0; i < palette.Weapons.Length; i++) palette.Weapons[i] = material;
        var vehicle = R3VehicleAssembly.Build(model, new R1RunStats(), palette, new Vector3(0,20,0), Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic = true;
        var gun = vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p => p.Id == state.Id);
        var enemy = new GameObject("enemy fixture");
        enemy.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().enabled=false;
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.SetParent(enemy.transform);
        target.transform.position = vehicle.GetComponent<Rigidbody>().position + Vector3.forward * 2f;
        var hp=target.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);
        var bank = vehicle.GetComponent<VehicleEnergyBank>(); float before = bank.Current;
        Physics.SyncTransforms();
        var binding = gun.GetComponent<RRLDamageAuraBinding>();
        Assert.That(binding.Fire(), Is.True);
        typeof(RRLDamageAuraBinding).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(binding,null);
        Assert.That(bank.Current, Is.EqualTo(before-RRLDamageAuraBinding.EnergyPerSecond*1.2f*Time.fixedDeltaTime).Within(.001f));
        Assert.That(hp.CurrentHp, Is.EqualTo(500-RRLDamageAuraBinding.DamagePerSecond*2*Time.fixedDeltaTime).Within(.001f));
        Object.Destroy(vehicle.gameObject); Object.Destroy(enemy); Object.Destroy(paletteObject); Object.Destroy(material);
        yield return null;
    }
    [UnityTest] public IEnumerator AuraDamagesHostileDomeAndProtectsEnemyInside()=>AuraDomeScenario(false,true);
    [UnityTest] public IEnumerator AuraDamagesHostileDomeEvenWithoutTargetInside()=>AuraDomeScenario(false,false);
    [UnityTest] public IEnumerator FriendlyDomeDoesNotBlockAuraAgainstEnemy()=>AuraDomeScenario(true,true);
    [UnityTest] public IEnumerator AuraDoesNotDamageInnerDomeBehindHostileOuterDome()=>AuraDomeScenario(false,true,true);
    IEnumerator AuraDomeScenario(bool friendly,bool hasTarget,bool nested=false)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.EmpEmitter;
        var paletteObject=new GameObject("QA palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
        for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
        vehicle.GetComponent<Rigidbody>().isKinematic=true;
        var gun=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
        var dome=GameObject.CreatePrimitive(PrimitiveType.Cube);dome.name="Osłona celu";dome.GetComponent<Collider>().enabled=false;
        dome.transform.position=vehicle.GetComponent<Rigidbody>().position+Vector3.forward*4;
        var sphere=dome.AddComponent<SphereCollider>();sphere.isTrigger=true;
        var shield=dome.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();shield.Configure(500,false);
        dome.AddComponent<R3ShieldDome>().Configure(friendly?1:2,2);
        GameObject inner=null;RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable innerHp=null;
        if(nested)
        {
            inner=GameObject.CreatePrimitive(PrimitiveType.Cube);inner.name="Wewnętrzna osłona";inner.GetComponent<Collider>().enabled=false;inner.transform.position=dome.transform.position;
            var innerSphere=inner.AddComponent<SphereCollider>();innerSphere.isTrigger=true;
            innerHp=inner.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();innerHp.Configure(500,false);
            inner.AddComponent<R3ShieldDome>().Configure(2,1);
        }
        GameObject enemy=null;RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable hp=null;
        if(hasTarget)
        {
            enemy=new GameObject("Osłonięty przeciwnik");enemy.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeEnemyTurret>().enabled=false;
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.transform.SetParent(enemy.transform);part.transform.position=dome.transform.position;
            hp=part.AddComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>();hp.Configure(500,false);
        }
        Physics.SyncTransforms();var aura=gun.GetComponent<RRLDamageAuraBinding>();Assert.That(aura.Fire(),Is.True);
        typeof(RRLDamageAuraBinding).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(aura,null);
        if(friendly){Assert.That(shield.CurrentHp,Is.EqualTo(500).Within(.001));Assert.That(hp.CurrentHp,Is.LessThan(500));}
        else {Assert.That(shield.CurrentHp,Is.LessThan(500));if(hp!=null)Assert.That(hp.CurrentHp,Is.EqualTo(500).Within(.001));}
        if(nested)Assert.That(innerHp.CurrentHp,Is.EqualTo(500).Within(.001));
        if(inner!=null)Object.Destroy(inner);if(enemy!=null)Object.Destroy(enemy);Object.Destroy(dome);Object.Destroy(vehicle.gameObject);Object.Destroy(paletteObject);Object.Destroy(material);
        yield return null;
    }

    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
}

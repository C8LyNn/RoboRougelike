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

    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
}

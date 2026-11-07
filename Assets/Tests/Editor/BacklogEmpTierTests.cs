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

    [UnityTest] public IEnumerator TierTwoEmpSpendsEnergyAndDisruptsARealCollider()
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
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.transform.position = gun.Muzzle.position + gun.Muzzle.forward * 2f;
        var receiver = target.AddComponent<EmpReceiver>(); receiver.ConfigureOwnership(target.transform, 12345, 2);
        var bank = vehicle.GetComponent<VehicleEnergyBank>(); float before = bank.Current;
        Physics.SyncTransforms();
        var binding = gun.GetComponent<RRLEmpBinding>();
        Assert.That(binding.Fire(), Is.True);
        Assert.That(bank.Current, Is.EqualTo(before - 15f).Within(.001f));
        Assert.That(receiver.IsDisrupted, Is.True);
        Assert.That(receiver.RemainingSeconds, Is.GreaterThan(3f));
        Object.Destroy(vehicle.gameObject); Object.Destroy(target); Object.Destroy(paletteObject); Object.Destroy(material);
        yield return null;
    }

    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
}

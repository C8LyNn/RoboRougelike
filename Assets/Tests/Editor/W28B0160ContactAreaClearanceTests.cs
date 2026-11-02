using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WeaponIntegration;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.Weapons.RotarySaw;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

// B01-60 independent tests on actual Codex W27 #824 gameplay classes.
// These exercise contact/radial semantics, NOT the projectile path rules.
// Native Unity/PhysX results remain NOT_RUN until a real Editor executes them.
public sealed class W28B0160ContactAreaClearanceTests
{
    [UnityTearDown]
    public IEnumerator ExitIsolatedScene()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
    }

    static R3VisualPalette MakePalette(out GameObject paletteObject, out Material material)
    {
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        paletteObject = new GameObject("B01-60 W28 isolated palette");
        var palette = paletteObject.AddComponent<R3VisualPalette>();
        for (int i = 0; i < palette.Parts.Length; i++) palette.Parts[i] = material;
        for (int i = 0; i < palette.Weapons.Length; i++) palette.Weapons[i] = material;
        return palette;
    }

    static R3MountedPart MakeGun(R4WeaponKind weapon, R3VisualPalette palette,
        out R3VehicleAssembly assembly, out R3PartState canonical)
    {
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        canonical = model.Parts.First(p => p.Kind == R3PartKind.Gun);
        canonical.Weapon = weapon;
        var vehicle = R3VehicleAssembly.Build(model, new R1RunStats(), palette,
            new Vector3(0, 25, 0), Quaternion.identity);
        assembly = vehicle.GetComponent<R3VehicleAssembly>();
        Assert.IsNotNull(assembly, "real R3 assembly required; no mock binding");
        Assert.AreEqual(weapon, model.WeaponGroupA,
            "correct LPM group must follow newly mounted weapon archetype");
        return assembly.Parts.Single(p => p.Id == canonical.Id);
    }

    [UnityTest]
    public IEnumerator RotarySaw_DynamicallyAttachedOwnerColliderNeverBecomesContactVictim()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        var palette = MakePalette(out var colors, out var material);
        var gun = MakeGun(R4WeaponKind.RotarySaw, palette, out var assembly, out var canonical);
        yield return null;
        var saw = gun.GetComponent<RRLSawBinding>();
        Assert.IsNotNull(saw, "original game saw binding must exist");
        Assert.IsNotNull(saw.Module, "real RotarySawModule required");
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "B01-60 own module dynamically attached AFTER Bind";
        block.transform.SetParent(assembly.transform, true);
        block.transform.position = gun.transform.position + gun.transform.forward * .45f;
        block.transform.localScale = Vector3.one * .22f;
        Physics.SyncTransforms();
        var collider = block.GetComponent<Collider>();
        Assert.IsFalse(saw.TryResolve(collider, out _),
            "late-added owned piece must be ignored by contact damage router");
        saw.SetMotorRequested(true);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.IsFalse(saw.TryResolve(collider, out _),
            "spinning saw must not become self-damaging after movement");
        Assert.IsTrue(saw.Module.IsMotorRequested,
            "mechanical contact test must run while motor is requested");

        // The very same collider is no longer owned after physical detachment:
        // test that the router is based on CURRENT ownership, not old registration.
        block.transform.SetParent(null, true);
        Physics.SyncTransforms();
        Assert.IsTrue(saw.TryResolve(collider, out _),
            "unparented collider is external and cannot retain false ownership");
        Object.Destroy(block);
        Object.Destroy(assembly.gameObject);
        Object.Destroy(colors);
        Object.Destroy(material);
    }

    [UnityTest]
    public IEnumerator Emp_OwnElectronicsNeverDisrupt_EnemyBehindWallDoesNotDisrupt_ButPulseCostsCooldown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        var palette = MakePalette(out var colors, out var material);
        var gun = MakeGun(R4WeaponKind.EmpEmitter, palette, out var assembly, out var canonical);
        yield return null;
        var emp = gun.GetComponent<RRLEmpBinding>();
        Assert.IsNotNull(emp, "actual RRLEmpBinding required");
        Assert.IsNotNull(emp.Module, "actual area-scanning EmpEmitter required");

        Vector3 origin = gun.transform.position;
        Vector3 direction = gun.transform.forward;
        var owned = GameObject.CreatePrimitive(PrimitiveType.Cube);
        owned.name = "B01-60 own receiver in active EMP field";
        owned.transform.SetParent(assembly.transform, true);
        owned.transform.position = origin + direction * 1f;
        owned.transform.localScale = Vector3.one * .25f;
        var ownReceiver = owned.AddComponent<EmpReceiver>();
        ownReceiver.ConfigureOwnership(assembly.transform, assembly.gameObject.GetInstanceID(), 1);

        var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemy.name = "B01-60 real outsider receiver";
        enemy.transform.position = origin + direction * 4f;
        enemy.transform.localScale = Vector3.one * .25f;
        var enemyReceiver = enemy.AddComponent<EmpReceiver>();
        enemyReceiver.ConfigureOwnership(enemy.transform, enemy.GetInstanceID(), 2);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "B01-60 solid cover between EMP and outsider";
        wall.transform.position = origin + direction * 2.5f;
        wall.transform.rotation = gun.transform.rotation;
        wall.transform.localScale = new Vector3(2f, 2f, .35f);
        Physics.SyncTransforms();

        float readyBefore = canonical.DeviceReadyAt;
        Assert.IsTrue(emp.Fire(), "physical EMP pulse valid despite occluded enemy");
        Assert.IsFalse(ownReceiver.IsDisrupted, "EMP may not disable own module");
        Assert.IsFalse(enemyReceiver.IsDisrupted, "solid wall must shield other vehicle");
        Assert.Greater(emp.LastReport.ReceiversOccluded, 0,
            "physical occlusion must actually be observed, not merely no target");
        Assert.Greater(canonical.DeviceReadyAt, readyBefore,
            "successfully emitted AREA pulse costs cooldown even when wall shields target");

        float firstReady = canonical.DeviceReadyAt;
        assembly.FireGroup(false);
        Assert.AreEqual(firstReady, canonical.DeviceReadyAt,
            "second LPM pulse during cooldown cannot charge cooldown a second time");

        wall.SetActive(false);
        emp.Module.RestoreCooldown(0);
        canonical.DeviceReadyAt = Time.time;  // explicit test-only cooldown advance
        gun.NextFireTime = 0;
        Physics.SyncTransforms();
        assembly.FireGroup(false);            // exercise the actual LPM game group route
        Assert.IsTrue(enemyReceiver.IsDisrupted,
            "unobstructed area pulse must resume through actual LPM weapon group");
        Assert.IsFalse(ownReceiver.IsDisrupted,
            "self components still safe after successive group activation");
        Object.Destroy(wall);
        Object.Destroy(enemy);
        Object.Destroy(owned);
        Object.Destroy(assembly.gameObject);
        Object.Destroy(colors);
        Object.Destroy(material);
    }
}

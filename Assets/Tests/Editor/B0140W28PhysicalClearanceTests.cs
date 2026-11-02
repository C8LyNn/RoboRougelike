// B01-40 / P17 W28 independent receiver regression.
// This uses the actual current #824 runtime, PhysX queries, and two actual weapon groups.
// SOURCE-ONLY CI DOES NOT EXECUTE THESE TESTS. They require Unity 6000.3.14f1.
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class B0140W28PhysicalClearanceTests
{
    [UnitySetUp] public IEnumerator Prepare()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
    }

    sealed class RigCase
    {
        public GameObject Root, PaletteRoot;
        public Material Material;
        public R3VehicleAssembly Assembly;
        public R3MountedPart Gun;
        public R3PartState State;

        public void Dispose()
        {
            if (Root != null) Object.Destroy(Root);
            if (PaletteRoot != null) Object.Destroy(PaletteRoot);
            if (Material != null) Object.Destroy(Material);
            R1EncounterManager.ClearTransientObjects();
        }
    }

    static RigCase CreateRig(R4WeaponKind kind, float horizontalOffset)
    {
        var c = new RigCase();
        c.Material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        c.PaletteRoot = new GameObject("B01-40 QA palette");
        var palette = c.PaletteRoot.AddComponent<R3VisualPalette>();
        for (int i = 0; i < palette.Parts.Length; i++) palette.Parts[i] = c.Material;
        for (int i = 0; i < palette.Weapons.Length; i++) palette.Weapons[i] = c.Material;
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
        c.State = model.Parts.Single(p => p.Kind == R3PartKind.Gun);
        c.State.Weapon = kind;
        var rig = R3VehicleAssembly.Build(model, new R1RunStats(), palette,
            new Vector3(horizontalOffset, 20, 0), Quaternion.identity);
        Assert.That(rig, Is.Not.Null, kind + " fixture real vehicle");
        c.Root = rig.gameObject;
        c.Assembly = rig.GetComponent<R3VehicleAssembly>();
        c.Gun = c.Assembly.Parts.Single(p => p.Id == c.State.Id);
        Assert.That(c.Gun.GunPivot, Is.Not.Null);
        Assert.That(c.Gun.Muzzle, Is.Not.Null);
        return c;
    }

    static GameObject PlaceOwnedDynamicBlocker(RigCase c)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "B01-40 LIVE owned moving geometry";
        cube.transform.SetParent(c.Root.transform, true);
        cube.transform.rotation = c.Gun.Muzzle.rotation;
        cube.transform.localScale = Vector3.one * .3f;
        var rb = cube.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.position = c.Gun.Muzzle.position + c.Gun.Muzzle.forward * .7f;
        Physics.SyncTransforms();
        return cube;
    }

    static void CheckSuppression(RigCase c, string phase)
    {
        int ammo = c.State.Ammo;
        float partReady = c.Gun.NextFireTime;
        float deviceReady = c.State.DeviceReadyAt;
        int priorProjectiles = Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None)
            .Count(p => p.PartId == c.Gun.Id);
        c.Assembly.FireGroup(false);
        Assert.That(c.Gun.NextFireTime, Is.EqualTo(partReady), phase + " consumed part cooldown");
        Assert.That(c.State.DeviceReadyAt, Is.EqualTo(deviceReady), phase + " consumed device cooldown");
        Assert.That(c.State.Ammo, Is.EqualTo(ammo), phase + " consumed ammo");
        Assert.That(Object.FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None)
            .Count(p => p.PartId == c.Gun.Id), Is.EqualTo(priorProjectiles),
            phase + " emitted a projectile through owner's Rigidbody");
    }

    [UnityTest]
    public IEnumerator EveryProjectileClass_TracksMovingOwnerColliderAndNeverSpendsOnBlockedFire()
    {
        var kinds = new[] { R4WeaponKind.Basic, R4WeaponKind.Automatic,
            R4WeaponKind.Heavy, R4WeaponKind.Shotgun,
            R4WeaponKind.Railgun, R4WeaponKind.RocketLauncher };
        for (int i = 0; i < kinds.Length; i++)
        {
            RigCase c = CreateRig(kinds[i], i * 45f);
            yield return null;
            var blocker = PlaceOwnedDynamicBlocker(c);
            Assert.That(c.Assembly.ClearProjectilePath(c.Gun), Is.False,
                kinds[i] + " physical owned Rigidbody must block");
            CheckSuppression(c, kinds[i] + " moving physical blocker");

            // Actual kinematic Rigidbody moves to another physical location.
            var moving = blocker.GetComponent<Rigidbody>();
            moving.MovePosition(c.Gun.Muzzle.position + c.Gun.Muzzle.right * 8f);
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
            Assert.That(c.Assembly.ClearProjectilePath(c.Gun), Is.True,
                kinds[i] + " must recover after real PhysX MovePosition");

            // Move back without spending any resources while blocked.
            moving.MovePosition(c.Gun.Muzzle.position + c.Gun.Muzzle.forward * .7f);
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
            Assert.That(c.Assembly.ClearProjectilePath(c.Gun), Is.False,
                kinds[i] + " must re-evaluate on every moved collider");
            CheckSuppression(c, kinds[i] + " blocker returned");

            // A foreign physical obstacle BEFORE owner geometry must be hittable:
            // owned geometry behind first external contact cannot veto the shot.
            var outside = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outside.name = "B01-40 external cover";
            outside.transform.position = c.Gun.Muzzle.position + c.Gun.Muzzle.forward * .24f;
            outside.transform.localScale = Vector3.one * .12f;
            Physics.SyncTransforms();
            Assert.That(c.Assembly.ClearProjectilePath(c.Gun), Is.True,
                kinds[i] + " external cover must be preferred over later owner geometry");

            Object.Destroy(outside);
            Object.Destroy(blocker);
            c.Dispose();
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator DifferentLpmRpmGroupsAreIndependent_WhenOwnedArmorBlocksOnlyOneGun()
    {
        var c = CreateRig(R4WeaponKind.Basic, 0);
        c.Dispose();
        yield return null;
        // Use the original R3 two-gun fixture layout from WeaponGroupTests.
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        model.Parts.Add(new R3PartState(new Vector3Int(1, 2, 0), R3PartKind.Gun)
            { Weapon = R4WeaponKind.Automatic });
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var paletteRoot = new GameObject("B01-40 two groups");
        var palette = paletteRoot.AddComponent<R3VisualPalette>();
        for (int i = 0; i < palette.Parts.Length; i++) palette.Parts[i] = material;
        for (int i = 0; i < palette.Weapons.Length; i++) palette.Weapons[i] = material;
        var rig = R3VehicleAssembly.Build(model, new R1RunStats(), palette,
            new Vector3(60, 20, 0), Quaternion.identity);
        yield return null;
        var assembly = rig.GetComponent<R3VehicleAssembly>();
        Assert.That(model.WeaponGroupA, Is.EqualTo(R4WeaponKind.Basic));
        Assert.That(model.WeaponGroupB, Is.EqualTo(R4WeaponKind.Automatic));
        var primary = assembly.Parts.Single(p => p.Kind == R3PartKind.Gun && p.Weapon == R4WeaponKind.Basic);
        var secondary = assembly.Parts.Single(p => p.Kind == R3PartKind.Gun && p.Weapon == R4WeaponKind.Automatic);
        var first = GameObject.CreatePrimitive(PrimitiveType.Cube);
        first.name = "B01-40 primary blocked";
        first.transform.SetParent(rig.transform, true);
        first.transform.position = primary.Muzzle.position + primary.Muzzle.forward * .7f;
        first.transform.localScale = Vector3.one * .3f;
        var second = GameObject.CreatePrimitive(PrimitiveType.Cube);
        second.name = "B01-40 secondary blocked";
        second.transform.SetParent(rig.transform, true);
        second.transform.position = secondary.Muzzle.position + secondary.Muzzle.forward * .7f;
        second.transform.localScale = Vector3.one * .3f;
        Physics.SyncTransforms();
        assembly.FireGroup(false);
        assembly.FireGroup(true);
        Assert.That(primary.NextFireTime, Is.Zero, "LPM blocked: no cooldown");
        Assert.That(secondary.NextFireTime, Is.Zero, "RPM blocked: no cooldown");

        second.SetActive(false);
        Physics.SyncTransforms();
        assembly.FireGroup(false);
        assembly.FireGroup(true);
        Assert.That(primary.NextFireTime, Is.Zero, "LPM still blocked");
        Assert.That(secondary.NextFireTime, Is.GreaterThan(0), "RPM independent fire");
        first.SetActive(false);
        Physics.SyncTransforms();
        assembly.FireGroup(false);
        Assert.That(primary.NextFireTime, Is.GreaterThan(0), "LPM resumes after clearing");
        Object.Destroy(rig.gameObject);
        Object.Destroy(paletteRoot);
        Object.Destroy(material);
        yield return null;
    }
}

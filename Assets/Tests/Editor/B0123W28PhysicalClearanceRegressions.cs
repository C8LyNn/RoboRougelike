using System.Collections;
using NUnit.Framework;
using RoboRougelike.R3;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// W28 regression probes against the real W27 R3VehicleAssembly clearance API.
/// Each test uses Unity Collider / Rigidbody / Physics queries, never a stub.
/// No production AI, garage, weapon or damage class is modified.
/// </summary>
public sealed class B0123W28PhysicalClearanceRegressions
{
    private const float Radius = .08f;
    private const float Range = 6f;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
    }

    private static R3VehicleAssembly CreateAssembly(out R3MountedPart gun)
    {
        var root = new GameObject("B01-23 W28 physical owner");
        root.transform.position = new Vector3(0f, 40f, 0f);
        var assembly = root.AddComponent<R3VehicleAssembly>();
        gun = AddGun(assembly, Vector3.zero);
        return assembly;
    }

    private static R3MountedPart AddGun(R3VehicleAssembly assembly, Vector3 offset)
    {
        var gunObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gunObject.name = "R3 owned gun";
        gunObject.transform.SetParent(assembly.transform, false);
        gunObject.transform.localPosition = offset;
        gunObject.transform.localScale = Vector3.one * .1f;
        var gun = gunObject.AddComponent<R3MountedPart>();
        var pivot = new GameObject("R3 gun pivot").transform;
        pivot.SetParent(gunObject.transform, false);
        pivot.localScale = Vector3.one * 10f; // Undo small primitive scale.
        var muzzle = new GameObject("R3 muzzle").transform;
        muzzle.SetParent(pivot, false);
        muzzle.localPosition = Vector3.forward;
        gun.GunPivot = pivot;
        gun.Muzzle = muzzle;
        return gun;
    }

    private static GameObject CreateOwnedBlocker(R3VehicleAssembly assembly, Vector3 worldPoint, float size = .5f)
    {
        var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.name = "B01-23 owned dynamic armor";
        blocker.transform.SetParent(assembly.transform, true);
        blocker.transform.position = worldPoint;
        blocker.transform.localScale = Vector3.one * size;
        return blocker;
    }

    private static bool Clear(R3VehicleAssembly assembly, R3MountedPart gun)
    {
        Physics.SyncTransforms();
        return assembly.ClearProjectilePath(gun, gun.Muzzle.forward, Range, Radius);
    }

    [UnityTest]
    public IEnumerator LiveRigidbodyCrossingOwnedFiringLaneBlocksThenReleases()
    {
        var assembly = CreateAssembly(out var gun);
        var lane = gun.Muzzle.position + gun.Muzzle.forward * 2f;
        var blocker = CreateOwnedBlocker(assembly, lane + Vector3.left * 2f);
        var rigidbody = blocker.AddComponent<Rigidbody>();
        rigidbody.mass = 3f;
        rigidbody.useGravity = false;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rigidbody.linearVelocity = Vector3.right * 6f;
        Assert.That(Clear(assembly, gun), Is.True, "initial lane must be clear");

        bool sawBlocked = false;
        bool sawClearAfter = false;
        for (int i = 0; i < 100; i++)
        {
            yield return new WaitForFixedUpdate();
            bool clear = Clear(assembly, gun);
            if (!clear) sawBlocked = true;
            if (sawBlocked && clear) { sawClearAfter = true; break; }
        }
        Assert.That(sawBlocked, Is.True, "the genuinely moving owned Rigidbody must obstruct the firing lane");
        Assert.That(sawClearAfter, Is.True, "clearance must recover after the Rigidbody leaves the lane");
        Assert.That(rigidbody.mass, Is.EqualTo(3f), "read-only clearance must not mutate vehicle mass");
        Object.Destroy(assembly.gameObject);
    }

    [UnityTest]
    public IEnumerator RotatingMuzzleRechecksSweptBarrelAndNewForwardPath()
    {
        var assembly = CreateAssembly(out var gun);
        var blocker = CreateOwnedBlocker(assembly, gun.Muzzle.position + Vector3.forward * .5f, .36f);
        Assert.That(Clear(assembly, gun), Is.False, "front barrel lane is obstructed");

        gun.GunPivot.localRotation = Quaternion.Euler(0f, 90f, 0f);
        Assert.That(Clear(assembly, gun), Is.True, "yaw must immediately invalidate stale front-lane occupancy");
        blocker.transform.position = gun.Muzzle.position + gun.Muzzle.forward * .5f;
        Assert.That(Clear(assembly, gun), Is.False, "new rotated lane must be re-evaluated");
        Object.Destroy(assembly.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator DetachedOwnPartIsNotMistakenForStillAttachedArmor()
    {
        var assembly = CreateAssembly(out var gun);
        var blocker = CreateOwnedBlocker(assembly, gun.Muzzle.position + Vector3.forward * 1.5f);
        Assert.That(Clear(assembly, gun), Is.False, "attached armor is self-obstruction");
        blocker.transform.SetParent(null, true);
        Assert.That(Clear(assembly, gun), Is.True,
            "detached physical piece is external cover, not owner's active collision envelope");
        // Not a claim that the projectile can pass through debris:
        // the real projectile must still collide with the detached piece as world cover.
        Object.Destroy(blocker);
        Object.Destroy(assembly.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SecondMountedGunBlocksFirstWithoutPoisoningItsOwnLane()
    {
        var assembly = CreateAssembly(out var front);
        var rear = front;
        var second = AddGun(assembly, new Vector3(0f, 0f, 2f));
        Assert.That(Clear(assembly, rear), Is.False, "another mounted gun is owned structure, not the firing gun");
        Assert.That(Clear(assembly, second), Is.True, "front gun must not be blocked by its own collider");
        Object.Destroy(assembly.gameObject);
        yield return null;
    }
}

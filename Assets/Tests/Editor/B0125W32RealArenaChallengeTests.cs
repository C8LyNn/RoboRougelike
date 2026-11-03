using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R3;
using RoboRougelike.R5;
using B01.MapJumpPad001;

public sealed class B0125W32RealArenaChallengeTests
{
    // W33 correction: do NOT test only an arbitrary 3.4m vehicle. Original
    // R3VehicleCatalog provides Light=20, Standard=83 and Heavy=139 cells.
    // R3VehicleAssembly places cell centres on its existing 0.55m grid.
    // This is a conservative factory-grid XZ proxy, NOT real mesh/PhysX hull.
    private static Vector3 FactoryEnvelope(R3VehicleKind kind)
    {
        var design = R3VehicleCatalog.Create(kind);
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;
        foreach (var part in design.Parts)
        {
            minX = Mathf.Min(minX, part.Cell.x);
            maxX = Mathf.Max(maxX, part.Cell.x);
            minZ = Mathf.Min(minZ, part.Cell.z);
            maxZ = Mathf.Max(maxZ, part.Cell.z);
        }
        int expected = kind == R3VehicleKind.Light ? 20 :
            kind == R3VehicleKind.Standard ? 83 : 139;
        Assert.That(design.Parts.Count, Is.EqualTo(expected),
            "Production R3 factory changed: review arena vehicle envelope");
        const float originalGridMeters = .55f;
        const float horizontalMarginMeters = .30f; // total, 0.15m per side
        float width = (maxX - minX + 1) * originalGridMeters +
            horizontalMarginMeters;
        float length = (maxZ - minZ + 1) * originalGridMeters +
            horizontalMarginMeters;
        Assert.That(width, Is.GreaterThan(0f));
        Assert.That(length, Is.GreaterThan(0f));
        return new Vector3(width, 1.2f, length);
    }

    private static void AssertCoverClear(R5Arena arena, Vector3 center,
        Vector3 envelopeSize, string context)
    {
        var envelope = new Bounds(new Vector3(center.x, 1.1f, center.z),
            envelopeSize);
        foreach (var collider in arena.Layout.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null || !collider.enabled || collider.isTrigger ||
                !collider.gameObject.activeInHierarchy)
                continue;
            Assert.That(collider.bounds.Intersects(envelope), Is.False,
                context + ": real solid cover intersects factory-grid envelope: " +
                collider.name + " at " + collider.bounds);
        }
    }

    [UnityTest]
    public IEnumerator AllThreeExistingR5ArenasRetainWideCentralSpawnToCombatCorridor()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        var host = new GameObject("B01-25 W33 canonical R5 geometry QA");
        var arena = host.AddComponent<R5Arena>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        Assert.That(shader, Is.Not.Null);
        var material = new Material(shader);
        arena.Initialize(material);

        foreach (R5ArenaKind kind in Enum.GetValues(typeof(R5ArenaKind)))
        {
            arena.Build(kind);
            yield return null; // Original R5 retires the previous layout.
            Physics.SyncTransforms();
            foreach (R3VehicleKind vehicle in Enum.GetValues(typeof(R3VehicleKind)))
            {
                Vector3 envelope = FactoryEnvelope(vehicle);
                for (int z = -22; z <= 18; z += 2)
                    AssertCoverClear(arena, new Vector3(0f, 1.1f, z),
                        envelope, kind + "/" + vehicle + " corridor z=" + z);
            }
        }
        UnityEngine.Object.Destroy(host);
        UnityEngine.Object.Destroy(material);
    }

    [UnityTest]
    public IEnumerator RealSalvageYardJumpPadHasWideIngressAndPredictedClearTouchdown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        var host = new GameObject("B01-25 W33 existing R5 jump pad route QA");
        var arena = host.AddComponent<R5Arena>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        Assert.That(shader, Is.Not.Null);
        var material = new Material(shader);
        arena.Initialize(material);
        arena.Build(R5ArenaKind.SalvageYard);
        Physics.SyncTransforms();

        var pads = arena.Layout.GetComponentsInChildren<MapJumpPad>();
        Assert.That(pads.Length, Is.EqualTo(1));
        var pad = pads[0];
        Assert.That(pad.IsActive, Is.True);
        Assert.That(pad.FootprintMeters, Is.EqualTo(Vector2.one));
        Assert.That(pad.GetComponent<BoxCollider>().isTrigger, Is.True);
        Assert.That(Vector3.Dot(pad.transform.forward, Vector3.forward),
            Is.GreaterThan(.999f), "Expected live R5 pad launches toward +Z");

        Assert.That(Physics.gravity.y, Is.LessThan(-1f));
        const float upwardChange = 8.5f;
        const float forwardChange = 2f;
        float estimatedFlightSeconds = 2f * upwardChange / -Physics.gravity.y;
        Vector3 estimatedTouchdown = pad.transform.position +
            pad.transform.forward * forwardChange * estimatedFlightSeconds;

        foreach (R3VehicleKind vehicle in Enum.GetValues(typeof(R3VehicleKind)))
        {
            Vector3 envelope = FactoryEnvelope(vehicle);
            for (int step = 0; step <= 12; step++)
            {
                Vector3 approach = Vector3.Lerp(arena.PlayerSpawn,
                    pad.transform.position, step / 12f);
                AssertCoverClear(arena, approach, envelope,
                    vehicle + " actual R5 pad ingress step " + step);
                Vector3 landing = Vector3.Lerp(pad.transform.position,
                    estimatedTouchdown, step / 12f);
                AssertCoverClear(arena, landing, envelope,
                    vehicle + " predicted landing step " + step);
            }
        }
        // This does not prove the real R3 multi-collider car launches;
        // independent B01-60 #901 owns the real PhysX impulse acceptance.
        UnityEngine.Object.Destroy(host);
        UnityEngine.Object.Destroy(material);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (Application.isPlaying)
            yield return new ExitPlayMode();
    }
}

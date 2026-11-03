using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R5;
using B01.MapJumpPad001;

public sealed class B0125W32RealArenaChallengeTests
{
    // Probe a 3.4 m wide vehicle envelope against the ACTUAL generated
    // solid cover. This is geometry acceptance, not a full driving test.
    private static void AssertCoverClear(R5Arena arena, Vector3 center, string context)
    {
        var envelope = new Bounds(new Vector3(center.x, 1.1f, center.z),
            new Vector3(3.4f, 1.2f, 3.4f));
        foreach (var collider in arena.Layout.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null || !collider.enabled || collider.isTrigger ||
                !collider.gameObject.activeInHierarchy)
                continue;
            Assert.That(collider.bounds.Intersects(envelope), Is.False,
                context + ": solid cover intersects vehicle envelope: " +
                collider.name + " at " + collider.bounds);
        }
    }

    [UnityTest]
    public IEnumerator AllThreeExistingR5ArenasRetainWideCentralSpawnToCombatCorridor()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        var host = new GameObject("B01-25 W32 canonical R5 geometry QA");
        var arena = host.AddComponent<R5Arena>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        Assert.That(shader, Is.Not.Null);
        var material = new Material(shader);
        arena.Initialize(material);

        foreach (R5ArenaKind kind in Enum.GetValues(typeof(R5ArenaKind)))
        {
            arena.Build(kind);
            yield return null; // Old layout is retired by the existing R5 Build.
            Physics.SyncTransforms();
            for (int z = -20; z <= 18; z += 2)
                AssertCoverClear(arena, new Vector3(0f, 1.1f, z),
                    kind + " central passage z=" + z);
        }

        UnityEngine.Object.Destroy(host);
        UnityEngine.Object.Destroy(material);
    }

    [UnityTest]
    public IEnumerator RealSalvageYardJumpPadHasWideIngressAndPredictedClearTouchdown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        var host = new GameObject("B01-25 W32 jump pad route QA");
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

        // Probe the ACTUAL straight ingress from the production spawn to
        // the production pad. Do not teleport or modify the actual player.
        for (int step = 0; step <= 12; step++)
        {
            Vector3 sample = Vector3.Lerp(arena.PlayerSpawn,
                pad.transform.position, step / 12f);
            AssertCoverClear(arena, sample, "Salvage Yard approach step " + step);
        }

        // A geometry-only ballistic estimate, NOT proof that the compound
        // real player receives the impulse; independent QA #901 owns that.
        Assert.That(Physics.gravity.y, Is.LessThan(-1f));
        const float upwardChange = 8.5f;
        const float forwardChange = 2f;
        float estimatedFlightSeconds = 2f * upwardChange / -Physics.gravity.y;
        Vector3 estimatedTouchdown = pad.transform.position +
            pad.transform.forward * forwardChange * estimatedFlightSeconds;
        for (int step = 0; step <= 12; step++)
        {
            Vector3 sample = Vector3.Lerp(pad.transform.position,
                estimatedTouchdown, step / 12f);
            AssertCoverClear(arena, sample, "Salvage Yard landing step " + step);
        }

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

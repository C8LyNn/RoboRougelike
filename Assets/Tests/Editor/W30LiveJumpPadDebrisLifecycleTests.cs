using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R5;
using B01.MapJumpPad001;

// Original live R5 arena, original imported #810 pad, real Unity Physics.
// Native Unity6000.3.14f1 Editor G1 NOT_RUN in this producer branch.
public sealed class W30LiveJumpPadDebrisLifecycleTests
{
    [UnityTest]
    public IEnumerator DestroyedLiveArenaBodyDoesNotLeakLaunchHistory()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();

        var host = new GameObject("W30 actual R5 arena / debris lifecycle");
        var arena = host.AddComponent<R5Arena>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        Assert.NotNull(shader);
        var material = new Material(shader);
        arena.Initialize(material);
        arena.Build(R5ArenaKind.SalvageYard);
        var pads = arena.Layout.GetComponentsInChildren<MapJumpPad>();
        Assert.That(pads.Length, Is.EqualTo(1));
        var pad = pads[0];
        var rubble = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rubble.name = "W30 real Rigidbody transient wreckage";
        rubble.transform.localScale = new Vector3(.3f, .3f, .3f);
        rubble.transform.position = pad.transform.position + Vector3.up * .29f;
        var body = rubble.AddComponent<Rigidbody>();
        body.mass = 10f;
        body.useGravity = false;
        Physics.SyncTransforms();

        for (int i = 0; i < 4 && pad.CooldownHistoryBodies == 0; i++)
            yield return new WaitForFixedUpdate();
        Assert.That(pad.CooldownHistoryBodies, Is.EqualTo(1),
            "the real R5 arena trigger must have launched one physical body");

        Object.Destroy(rubble); // No explicit OnTriggerExit is guaranteed.
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(pad.CooldownHistoryBodies, Is.EqualTo(0),
            "actual destroyed R3 debris must not remain in cooldown ledger");
        Assert.That(pad.TrackedContactBodies, Is.EqualTo(0));

        Object.Destroy(host);
        Object.Destroy(material);
    }

    [UnityTest]
    public IEnumerator DisabledMidTriggerCompoundColliderDoesNotStickInContactLedger()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();

        var host = new GameObject("W30 multi-collider disabled debris case");
        var arena = host.AddComponent<R5Arena>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        Assert.NotNull(shader);
        var material = new Material(shader);
        arena.Initialize(material);
        arena.Build(R5ArenaKind.SalvageYard);
        var pad = arena.Layout.GetComponentInChildren<MapJumpPad>();
        Assert.NotNull(pad);
        // Hold the body kinematic to inspect overlap bookkeeping before launch.
        var debris = new GameObject("real compound attached physics module");
        debris.transform.position = pad.transform.position + Vector3.up * .29f;
        var body = debris.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
        child.transform.SetParent(debris.transform, false);
        child.transform.localScale = Vector3.one * .3f;
        var collider = child.GetComponent<BoxCollider>();
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Assert.That(pad.TrackedContactBodies, Is.EqualTo(1),
            "actual compound collider must create a real trigger contact");
        collider.enabled = false; // This may not call OnTriggerExit.
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Assert.That(pad.TrackedContactBodies, Is.EqualTo(0),
            "disabled detached child collider cannot block future entries");

        Object.Destroy(debris);
        Object.Destroy(host);
        Object.Destroy(material);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
    }
}

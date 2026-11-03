using System.Collections;
using System.Linq;
using B01.MapJumpPad001;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R5;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

// Independent B01-60 W31 game acceptance, NOT a replacement JumpPad or drive.
// Original W29 tested a detached 10-kg CUBE, not the actual R3 vehicle.
// Both tests boot the original scene/run/vehicle and use the real R5 pad.
public sealed class B0160W31RealR3JumpPadTests
{
    private const string GameplayScene = "Assets/scene 1.unity";

    private static IEnumerator StartActualR3CombatInSalvageYard()
    {
        EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Single);
        yield return new EnterPlayMode();

        float deadline = Time.realtimeSinceStartup + 15f;
        while (R1RunController.Instance?.Progress == null &&
               Time.realtimeSinceStartup < deadline)
            yield return null;
        var run = R1RunController.Instance;
        Assert.That(run, Is.Not.Null, "must boot actual scene 1 and R3 run");
        Assert.That(run.StartRun(), Is.True, "actual run progression must start");
        Assert.That(run.ChooseRoute(0), Is.True, "actual R1 route/encounter must start");

        while ((run.Transitioning || run.Player == null) &&
               Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(run.Transitioning, Is.False, "actual encounter transition timed out");
        Assert.That(run.Player, Is.Not.Null, "actual R3 Player must have spawned");
        Assert.That(run.Player.GetComponent<R3VehicleAssembly>(), Is.Not.Null,
            "must test real mounted R3 car, NOT a synthetic cube");

        // Keep the authentic vehicle/physics and live encounter, but prevent
        // enemy AI from attacking before the physical pad outcome is observed.
        run.Encounter.SetCombatActive(false);
        run.Player.Controller.SetGameplayInputEnabled(false);
        // Real R5Arena.Build path, same as original R5ArenaTests. The offered
        // route may be Wasteland; explicitly select existing Salvage Yard.
        run.Arena.Build(R5ArenaKind.SalvageYard);
        Physics.SyncTransforms();
        Assert.That(run.Arena.Kind, Is.EqualTo(R5ArenaKind.SalvageYard));
        Assert.That(run.Arena.Layout.GetComponentsInChildren<MapJumpPad>().Length,
            Is.EqualTo(1), "actual R5 Salvage Yard must have ONE real pad");
    }

    private static (R1RunController run, MapJumpPad pad, Rigidbody body)
        ActualVehicleAndPad()
    {
        var run = R1RunController.Instance;
        Assert.That(run, Is.Not.Null);
        var pads = run.Arena.Layout.GetComponentsInChildren<MapJumpPad>();
        Assert.That(pads.Length, Is.EqualTo(1));
        var body = run.Player.GetComponent<Rigidbody>();
        Assert.That(body, Is.Not.Null, "actual R3 car physics body missing");
        Assert.That(body.isKinematic, Is.False, "real R3 vehicle must use PhysX");
        Assert.That(body.mass, Is.GreaterThan(20f),
            "this must be the real multi-part R3 rig, not a 10kg test cube");
        Assert.That(run.Player.GetComponentsInChildren<Collider>()
            .Count(c => c.enabled && !c.isTrigger &&
                c.attachedRigidbody == body), Is.GreaterThan(1),
            "a true compound multi-collider R3 vehicle is required");
        Assert.That(pads[0].GetComponent<BoxCollider>().isTrigger, Is.True);
        return (run, pads[0], body);
    }

    private static void PlaceActualR3VehicleAbovePad(R1RunController run,
        MapJumpPad pad, Rigidbody body)
    {
        var landing = pad.transform.position + Vector3.up * 2.3f;
        run.Player.MoveTo(landing, Quaternion.identity);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        Physics.SyncTransforms();
        Assert.That((body.position - landing).sqrMagnitude,
            Is.LessThan(.0001f),
            "must reposition the original R3 car, not a fake proxy");
    }

    [UnityTest]
    public IEnumerator SalvageYard_ActualMultiColliderR3VehicleGetsOneRealUpwardImpulse()
    {
        yield return StartActualR3CombatInSalvageYard();
        var p = ActualVehicleAndPad();
        Assert.That(p.pad.IsActive, Is.True);
        Assert.That(p.pad.CanLaunch(p.body), Is.True,
            "real mounted R3 Rigidbody must be eligible for the existing pad");
        // The old arcade tank could freeze Y while R6 enables real physics.
        // Do not silently change either component in an acceptance test.
        Assert.That(p.body.useGravity, Is.True,
            "actual R6 chassis must preserve gravity to reach the trigger");
        Assert.That((p.body.constraints & RigidbodyConstraints.FreezePositionY),
            Is.EqualTo(RigidbodyConstraints.None),
            "real playable vehicle cannot fly if Y translation stays frozen");

        PlaceActualR3VehicleAbovePad(p.run, p.pad, p.body);

        float maxUpVelocity = float.NegativeInfinity;
        float maxHeight = float.NegativeInfinity;
        for (int i = 0; i < 150; i++)
        {
            yield return new WaitForFixedUpdate();
            Assert.That(p.body, Is.Not.Null,
                "the actual vehicle must survive the real in-arena jump");
            maxUpVelocity = Mathf.Max(maxUpVelocity, p.body.linearVelocity.y);
            maxHeight = Mathf.Max(maxHeight, p.body.position.y);
        }
        Debug.Log("[B01-60 W31 R3 real JumpPad] mass=" + p.body.mass +
            " peakYspeed=" + maxUpVelocity + " peakHeight=" + maxHeight);
        Assert.That(maxUpVelocity, Is.GreaterThan(5f),
            "actual full R3 car must be launched; detached cube tests are insufficient");
        Assert.That(maxUpVelocity, Is.LessThan(15f),
            "compound wheel/core/armor colliders must not stack launch impulses");
        Assert.That(maxHeight, Is.GreaterThan(2.3f),
            "actual full vehicle must physically leave the pad, not only gain transient speed");
    }

    [UnityTest]
    public IEnumerator SalvageYard_DisabledRealPadCannotLaunchFullR3Vehicle()
    {
        yield return StartActualR3CombatInSalvageYard();
        var p = ActualVehicleAndPad();
        p.pad.SetActive(false);
        Assert.That(p.pad.CanLaunch(p.body), Is.False,
            "existing pad's true gameplay activation gate must fail closed");
        PlaceActualR3VehicleAbovePad(p.run, p.pad, p.body);

        float maxUpVelocity = float.NegativeInfinity;
        for (int i = 0; i < 130; i++)
        {
            yield return new WaitForFixedUpdate();
            Assert.That(p.body, Is.Not.Null);
            maxUpVelocity = Mathf.Max(maxUpVelocity, p.body.linearVelocity.y);
        }
        Debug.Log("[B01-60 W31 R3 disabled JumpPad] peakYspeed=" + maxUpVelocity);
        Assert.That(maxUpVelocity, Is.LessThan(4f),
            "disabled real pad must not inject the >=8.5m/s impulse");
        Assert.That(p.pad.IsActive, Is.False);
    }

    [UnityTearDown]
    public IEnumerator CleanupRealScene()
    {
        if (Application.isPlaying)
        {
            Time.timeScale = 1f;
            yield return new ExitPlayMode();
        }
    }
}

// B01-61 PASS16 — original Codex #310 game source aa7a917f177a9b1f07203868fb7d1d871eb3ee46
// QA-ONLY. AUTHORED, NOT COMPILED/RUN by B01-61; Codex owns test integration.
// Run in disposable licensed Unity6000.3.14f1 Editor test assembly in batchmode.
// Distinct from Codex WeaponTriggerSafetyTests (camera-loss beam) and B01-61
// garage→map camera identity; this tests real input latch across pause and
// moving-camera physical target following.
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RogueMachine.P12PipelineSafe;

public sealed class B0161Pass16InputCameraRegressionTests
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static FieldInfo Field(string name)
    {
        var f=typeof(P12PipelineSafeTankController).GetField(name,Private);
        Assert.That(f,Is.Not.Null,"Codex original P12 controller field changed: "+name);
        return f;
    }
    static bool Value(P12PipelineSafeTankController c,string name)=>(bool)Field(name).GetValue(c);
    static void SampleActualInput(P12PipelineSafeTankController c)
    {
        var method=typeof(P12PipelineSafeTankController).GetMethod("Update",Private);
        Assert.That(method,Is.Not.Null,"Codex original Update input reader changed");
        method.Invoke(c,null);
    }
    static IEnumerator EnterDisposableRange(System.Action<R1RunController> ready)
    {
        Assert.That(Application.isBatchMode,Is.True,"Never automate the user's active Unity editor");
        EditorSceneManager.OpenScene("Assets/scene 1.unity",OpenSceneMode.Single);
        yield return new EnterPlayMode();
        var deadline=Time.realtimeSinceStartup+30f;
        while((R1RunController.Instance?.Progress==null)&&Time.realtimeSinceStartup<deadline)
            yield return null;
        var run=R1RunController.Instance;
        Assert.That(run,Is.Not.Null);
        Assert.That(run.OpenSandboxGarage(),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);
        deadline=Time.realtimeSinceStartup+30f;
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);
        Assert.That(run.Player,Is.Not.Null);
        ready(run);
    }

    [UnityTest]
    public IEnumerator HeldPrimaryThroughPauseIsBlockedUntilActualReleaseAndRepress()
    {
#if !ENABLE_INPUT_SYSTEM
        Assert.Ignore("Requires ENABLE_INPUT_SYSTEM original controller branch");
#endif
        R1RunController run=null;
        yield return EnterDisposableRange(x=>run=x);
        var controller=run.Player.Controller;
        var virtualMouse=InputSystem.AddDevice<Mouse>();
        virtualMouse.MakeCurrent();
        try
        {
            Assert.That(controller.GameplayInputEnabled,Is.True);
            InputState.Change(virtualMouse.leftButton,1f);
            SampleActualInput(controller);
            Assert.That(Value(controller,"fireHeld"),Is.True,
                "Positive control: authentic Update reads mounted virtual left button");

            run.TogglePause();
            Assert.That(run.Paused,Is.True);
            Assert.That(controller.GameplayInputEnabled,Is.False);
            Assert.That(Value(controller,"fireHeld"),Is.False);
            Assert.That(Value(controller,"secondaryFireHeld"),Is.False);
            Assert.That(Value(controller,"waitForPrimaryRelease"),Is.True,
                "A game pause must latch the existing pressed LPM");

            run.TogglePause();
            Assert.That(run.Paused,Is.False);
            Assert.That(controller.GameplayInputEnabled,Is.True);
            SampleActualInput(controller); // mouse still physically held
            Assert.That(Value(controller,"fireHeld"),Is.False,
                "Held LPM across pause cannot autonomously restart fire");

            InputState.Change(virtualMouse.leftButton,0f);
            SampleActualInput(controller);
            Assert.That(Value(controller,"waitForPrimaryRelease"),Is.False,
                "Real LPM release disarms only the release latch");
            Assert.That(Value(controller,"fireHeld"),Is.False);

            InputState.Change(virtualMouse.leftButton,1f);
            SampleActualInput(controller);
            Assert.That(Value(controller,"fireHeld"),Is.True,
                "Fresh real LPM repress must restore correct firing readiness");
        }
        finally
        {
            controller.SetGameplayInputEnabled(false);
            InputSystem.RemoveDevice(virtualMouse);
        }
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator MovingVehicleRepositionsActiveCameraWithoutReplacingTargetRoot()
    {
        R1RunController run=null;
        yield return EnterDisposableRange(x=>run=x);
        var follow=Object.FindFirstObjectByType<P12PipelineSafeCamera>();
        Assert.That(follow,Is.Not.Null);
        Assert.That(follow.enabled,Is.True);
        Assert.That(follow.ReceiverRig,Is.Not.Null);
        Assert.That(follow.ReceiverRig.enabled,Is.False,
            "Single owner of actual camera step should remain the existing facade");
        Assert.That(follow.TargetRoot,Is.SameAs(run.Player.transform));
        var camera=follow.GetComponent<Camera>();
        Assert.That(camera,Is.Not.Null);
        var previous=camera.transform.position;
        var moved=run.Player.transform.position+Vector3.right*3f;
        run.Player.transform.position=moved;
        Physics.SyncTransforms();
        for(int i=0;i<12;i++)yield return null;
        Assert.That(follow.TargetRoot,Is.SameAs(run.Player.transform),
            "Moving actual player must not orphan the camera's configured target");
        Assert.That(Vector3.Distance(camera.transform.position,previous),Is.GreaterThan(.02f),
            "Camera should respond visibly to actual translated player target");
        Assert.That(float.IsNaN(follow.SmoothedZoomDistance),Is.False);
        Assert.That(float.IsInfinity(follow.SmoothedZoomDistance),Is.False);
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if(Application.isPlaying)
        {
            Time.timeScale=1;
            yield return new ExitPlayMode();
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    }
}

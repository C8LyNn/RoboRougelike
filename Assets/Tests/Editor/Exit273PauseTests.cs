using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;

public sealed class Exit273PauseTests
{
    R1RunController run;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");
        yield return new EnterPlayMode();
        while (R1RunController.Instance?.Progress == null) yield return null;
        run = R1RunController.Instance;
        Assert.That(run.PrepareUnifiedRun(), Is.True);
        yield return null;
    }
    [UnityTest] public IEnumerator GaragePauseResumesWithoutStartingSimulation()
    {
        Assert.That(run.Progress.State, Is.EqualTo(R1RunState.Garage));
        Assert.That(Time.timeScale, Is.Zero);
        run.TogglePause();
        Assert.That(run.Paused, Is.True, "Esc must open the pause menu in the garage");
        run.TogglePause();
        Assert.That(run.Paused, Is.False);
        Assert.That(Time.timeScale, Is.Zero, "resuming garage must not start the physics simulation");
        yield return null;
    }
    [UnityTest] public IEnumerator CombatPauseReturnsToMenuAndCleansActivity()
    {
        Assert.That(run.ChooseCheckpointRace(), Is.True);
        float deadline = Time.realtimeSinceStartup + 15;
        while (run.Transitioning && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(run.Transitioning, Is.False);
        run.TogglePause();
        Assert.That(run.Paused, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        var previous = run;
        run.RestartRun();
        deadline = Time.realtimeSinceStartup + 15;
        while ((R1RunController.Instance == previous || R1RunController.Instance?.Progress == null) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(R1RunController.Instance, Is.Not.SameAs(previous));
        Assert.That(R1RunController.Instance.Progress.State, Is.EqualTo(R1RunState.Ready));
        Assert.That(R1RunController.Instance.GeneratedWorld, Is.Null);
    }
    [UnityTest] public IEnumerator ShutdownPreparationIsIdempotentAndReleasesCursor()
    {
        run.TogglePause();
        var prepare = typeof(R1RunController).GetMethod("PrepareShutdown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(prepare, Is.Not.Null);
        Assert.That(prepare.Invoke(run,new object[]{"NativeTest"}), Is.True);
        Assert.That(prepare.Invoke(run,new object[]{"DuplicateTest"}), Is.False);
        Assert.That(run.QuitRequested, Is.True);
        Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
        Assert.That(Cursor.visible, Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(1));
        var previous = run;
        run.RestartRun();
        yield return null;
        Assert.That(R1RunController.Instance, Is.SameAs(previous), "No scene load during shutdown");
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using UnityEngine.InputSystem;

public sealed class Exit273PauseTests
{
    R1RunController run;
    RC2EditorInputProbe probe;
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
    [UnityTest] public IEnumerator PauseControlsCloseBeforeResumingAndKeepSimulationPaused()
    {
        run.TogglePause();Assert.That(run.Paused,Is.True);
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var move=typeof(R1RunController).GetMethod("MovePauseSelection",flags);
        var confirm=typeof(R1RunController).GetMethod("ConfirmPauseSelection",flags);
        for(int i=0;i<3;i++)move.Invoke(run,new object[]{1});
        Assert.That(run.PauseSelection,Is.EqualTo(3),"The controls option must be reachable with the keyboard or pad.");
        confirm.Invoke(run,null);var hud=run.GetComponent<R1RunHUD>();
        var property=typeof(R1RunHUD).GetProperty("PauseControlsOpen",flags);
        Assert.That(property,Is.Not.Null);Assert.That(property.GetValue(hud),Is.True);
        run.HandleEscape();Assert.That(run.Paused,Is.True);Assert.That(property.GetValue(hud),Is.False);
        yield return null;run.HandleEscape();Assert.That(run.Paused,Is.False);Assert.That(Time.timeScale,Is.Zero);
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
    [UnityTest] public IEnumerator RKeyStillResetsPausedTestRange()
    {
        Assert.That(run.EnterTestRange(), Is.True);
        float deadline=Time.realtimeSinceStartup+15;
        while(run.Transitioning && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning, Is.False);
        int previousPlayer=run.Player.GetInstanceID();
        run.TogglePause();Assert.That(run.Paused, Is.True);
        probe=new GameObject("EXIT273 native range R probe").AddComponent<RC2EditorInputProbe>();
        probe.InjectKey=UnityEngine.InputSystem.Key.R;
        yield return new WaitForSecondsRealtime(.2f);
        for(int i=0;i<8&&run.Paused;i++)
        {if(UnityEditor.EditorApplication.isPaused)UnityEditor.EditorApplication.Step();yield return null;}
        Assert.That(probe.PressObserved, Is.True, "Native player input must observe R");
        Assert.That(run.Paused, Is.False, "The pause menu must preserve the range R reset shortcut");
        while(run.Transitioning && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Player.GetInstanceID(), Is.Not.EqualTo(previousPlayer));
        Assert.That(run.Progress.State, Is.EqualTo(R1RunState.TestDrive));
    }
    [UnityTest] public IEnumerator RangeResetUsesReboundKeyWhileDrivingAndPaused()
    {
        var profile=R1ControlProfile.Current;profile.ResetDefaults();
        try
        {
            Assert.That(profile.TryBind("DefenseStatusPage","F6"),Is.True);
            Assert.That(profile.TryBind("ResetRange","P"),Is.True);
            Assert.That(run.EnterTestRange(),Is.True);float deadline=Time.realtimeSinceStartup+20;
            while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            int previous=run.Player.GetInstanceID();yield return PressRangeKey(Key.R);
            Assert.That(run.Player.GetInstanceID(),Is.EqualTo(previous));Assert.That(run.Transitioning,Is.False);
            yield return PressRangeKey(Key.P);while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(run.Player.GetInstanceID(),Is.Not.EqualTo(previous));
            previous=run.Player.GetInstanceID();run.TogglePause();Assert.That(run.Paused,Is.True);
            yield return PressRangeKey(Key.P);while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(run.Paused,Is.False);Assert.That(run.Player.GetInstanceID(),Is.Not.EqualTo(previous));
        }
        finally{profile.ResetDefaults();}
    }
    [UnityTest] public IEnumerator ExtraMouseButtonResetsRangeWhenRunningAndPaused()
    {
        var profile=R1ControlProfile.Current;profile.ResetDefaults();
        try
        {
            Assert.That(profile.TryBind("ResetRange","MouseBack"),Is.True);
            Assert.That(run.EnterTestRange(),Is.True);float deadline=Time.realtimeSinceStartup+20;
            while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            var method=typeof(R1RunHUD).GetMethod("TryRangeMouseReset",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            Assert.That(method,Is.Not.Null);
            for(int i=0;i<2;i++)
            {
                if(i==1){run.TogglePause();Assert.That(run.Paused,Is.True);}
                int previous=run.Player.GetInstanceID();var used=new Event{type=EventType.MouseDown,button=3};used.Use();
                method.Invoke(null,new object[]{run,used});Assert.That(run.Transitioning,Is.False,"UI-consumed input must not reset the range.");
                var input=new Event{type=EventType.MouseDown,button=3};
                Assert.That(R1ControlProfile.IsBindingMouseDown(input),Is.True);Assert.That(profile.MatchesEvent("ResetRange",input),Is.True);
                method.Invoke(null,new object[]{run,input});
                Assert.That(run.Transitioning,Is.True,"The configured extra button must trigger the range reset.");
                deadline=Time.realtimeSinceStartup+20;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(run.Player.GetInstanceID(),Is.Not.EqualTo(previous));Assert.That(run.Paused,Is.False);
            }
        }
        finally{profile.ResetDefaults();}
    }
    [UnityTest] public IEnumerator ControlsPanelDoesNotResetPausedRangeOnR()
    {
        Assert.That(run.EnterTestRange(),Is.True);float deadline=Time.realtimeSinceStartup+15;
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        run.TogglePause();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var move=typeof(R1RunController).GetMethod("MovePauseSelection",flags);
        var confirm=typeof(R1RunController).GetMethod("ConfirmPauseSelection",flags);
        for(int i=0;i<5;i++)move.Invoke(run,new object[]{1});
        Assert.That(run.PauseSelection,Is.EqualTo(5));confirm.Invoke(run,null);
        var panel=typeof(R1RunHUD).GetProperty("PauseControlsOpen",flags);var hud=run.GetComponent<R1RunHUD>();
        Assert.That(panel.GetValue(hud),Is.True);int player=run.Player.GetInstanceID();
        yield return PressRangeKey(Key.R);
        Assert.That(run.Paused,Is.True);Assert.That(panel.GetValue(hud),Is.True);
        Assert.That(run.Player.GetInstanceID(),Is.EqualTo(player));Assert.That(run.Transitioning,Is.False);
        run.HandleEscape();Assert.That(panel.GetValue(hud),Is.False);Assert.That(run.Paused,Is.True);
    }
    [UnityTest] public IEnumerator RangePauseActionsSelectAndReturnToGarage()
    {
        Assert.That(run.EnterTestRange(),Is.True);
        float deadline=Time.realtimeSinceStartup+15;
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);run.TogglePause();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var move=typeof(R1RunController).GetMethod("MovePauseSelection",flags);
        var confirm=typeof(R1RunController).GetMethod("ConfirmPauseSelection",flags);
        Assert.That(move,Is.Not.Null);Assert.That(confirm,Is.Not.Null);
        for(int i=0;i<3;i++)move.Invoke(run,new object[]{1});
        Assert.That(run.PauseSelection,Is.EqualTo(3),"Range pause has separate reset, garage, menu and quit options.");
        move.Invoke(run,new object[]{-1});Assert.That(run.PauseSelection,Is.EqualTo(2));
        confirm.Invoke(run,null);
        deadline=Time.realtimeSinceStartup+15;
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        Assert.That(run.Paused,Is.False);
    }
    [UnityTest] public IEnumerator RangePauseRescueResumesWithoutDamageAndGrantsImmunity()
    {
        Assert.That(run.EnterTestRange(),Is.True);float deadline=Time.realtimeSinceStartup+15;
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);run.TogglePause();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var move=typeof(R1RunController).GetMethod("MovePauseSelection",flags);
        var confirm=typeof(R1RunController).GetMethod("ConfirmPauseSelection",flags);
        for(int i=0;i<6;i++)move.Invoke(run,new object[]{1});
        Assert.That(run.PauseSelection,Is.EqualTo(6));float hp=run.Player.Core.CurrentHp;
        confirm.Invoke(run,null);
        Assert.That(run.Paused,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));
        Assert.That(run.EmergencyRescueActive,Is.True);Assert.That(run.EmergencyRescueAttackBlocked,Is.True);
        Assert.That(run.Player.Core.CurrentHp,Is.EqualTo(hp).Within(.01f));
        run.Player.Core.ApplyDamage(20,run.Player.Core.transform.position,Vector3.up);
        Assert.That(run.Player.Core.CurrentHp,Is.EqualTo(hp).Within(.01f),"Rescue on the range must protect against incoming damage.");
        yield return null;
    }
    [UnityTest] public IEnumerator DisabledTestVehicleSelectsResetInsteadOfUnavailableResume()
    {
        Assert.That(run.EnterTestRange(),Is.True);
        float deadline=Time.realtimeSinceStartup+15;
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);
        var core=run.Player.Core;Assert.That(core,Is.Not.Null);
        core.ApplyDamage(100000,core.transform.position,Vector3.up);
        typeof(R1RunController).GetMethod("UpdateRange",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(run,null);
        Assert.That(run.RangeDisabled,Is.True);
        Assert.That(run.Paused,Is.True);
        Assert.That(run.PauseSelection,Is.EqualTo(1),"An unavailable resume action cannot be the selected option.");
        yield return null;
    }
    IEnumerator PressRangeKey(Key key)
    {
        probe=new GameObject("range pause test input").AddComponent<RC2EditorInputProbe>();
        probe.InjectKey=key;
        float deadline=Time.realtimeSinceStartup+5;
        while(!probe.Sent&&Time.realtimeSinceStartup<deadline)
        {if(UnityEditor.EditorApplication.isPaused)UnityEditor.EditorApplication.Step();yield return null;}
        Assert.That(probe.Sent&&probe.PressObserved,Is.True,$"The virtual {key} press must reach the real input update.");
        Object.DestroyImmediate(probe.gameObject);probe=null;yield return null;
    }
    [UnityTest] public IEnumerator RangePauseKeyboardNavigatesBackToGarage()
    {
        Assert.That(run.EnterTestRange(),Is.True);
        float deadline=Time.realtimeSinceStartup+15;
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Transitioning,Is.False);run.TogglePause();
        yield return PressRangeKey(Key.DownArrow);
        Assert.That(run.PauseSelection,Is.EqualTo(1));
        yield return PressRangeKey(Key.DownArrow);
        Assert.That(run.PauseSelection,Is.EqualTo(2));
        yield return PressRangeKey(Key.Enter);
        deadline=Time.realtimeSinceStartup+15;
        while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        Assert.That(run.Paused,Is.False);
    }
    [UnityTest] public IEnumerator ArrowAssignedToWeaponStillNavigatesThePauseMenu()
    {
        var profile=R1ControlProfile.Current;profile.ResetDefaults();
        try
        {
            Assert.That(profile.TryBind("Weapon3","DownArrow"),Is.True);
            Assert.That(run.EnterTestRange(),Is.True);
            float deadline=Time.realtimeSinceStartup+15;
            while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(run.Transitioning,Is.False);run.TogglePause();
            yield return PressRangeKey(Key.DownArrow);
            Assert.That(run.Paused,Is.True);
            Assert.That(run.PauseSelection,Is.EqualTo(1),"Pause navigation keeps its own context.");
        }
        finally{profile.ResetDefaults();}
    }
    [UnityTest] public IEnumerator ReassignedPauseNavigationUsesNewKeysAndRespectsControlsOverlay()
    {
        var profile=R1ControlProfile.Current;profile.ResetDefaults();
        try
        {
            Assert.That(profile.TryBind("PausePrevious","J"),Is.True);
            Assert.That(profile.TryBind("PauseNext","K"),Is.True);
            Assert.That(profile.TryBind("PauseConfirm","L"),Is.True);
            run.TogglePause();Assert.That(run.Paused,Is.True);
            var hud=run.GetComponent<R1RunHUD>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var open=typeof(R1RunHUD).GetMethod("OpenPauseControls",flags);var close=typeof(R1RunHUD).GetMethod("ClosePauseControls",flags);
            open.Invoke(hud,null);yield return PressRangeKey(Key.K);
            Assert.That(run.PauseSelection,Is.Zero,"The settings overlay owns input.");close.Invoke(hud,null);
            yield return PressRangeKey(Key.DownArrow);Assert.That(run.PauseSelection,Is.Zero,"The old direction must no longer select.");
            yield return PressRangeKey(Key.K);Assert.That(run.PauseSelection,Is.EqualTo(1));
            yield return PressRangeKey(Key.J);Assert.That(run.PauseSelection,Is.Zero);
            yield return PressRangeKey(Key.Enter);Assert.That(run.Paused,Is.True,"The old confirmation key must no longer activate the item.");
            yield return PressRangeKey(Key.L);Assert.That(run.Paused,Is.False);
            var hint=typeof(R1RunHUD).GetMethod("PauseNavigationHint",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Assert.That((string)hint.Invoke(null,null),Does.Contain("J").And.Contain("K").And.Contain("L"));
        }
        finally{profile.ResetDefaults();}
    }
    [UnityTest] public IEnumerator MousePauseNavigationRespectsUsedEventsAndControlsOverlay()
    {
        var profile=R1ControlProfile.Current;profile.ResetDefaults();
        try
        {
            Assert.That(profile.TryBind("PauseNext","MouseBack"),Is.True);
            Assert.That(profile.TryBind("PausePrevious","MouseForward"),Is.True);
            Assert.That(profile.TryBind("PauseConfirm","MouseMiddle"),Is.True);
            run.TogglePause();var hud=run.GetComponent<R1RunHUD>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var handle=typeof(R1RunController).GetMethod("TryPauseNavigationMouse",flags);
            var open=typeof(R1RunHUD).GetMethod("OpenPauseControls",flags);var close=typeof(R1RunHUD).GetMethod("ClosePauseControls",flags);
            Assert.That(handle,Is.Not.Null);open.Invoke(hud,null);
            handle.Invoke(run,new object[]{new Event{type=EventType.MouseDown,button=3}});Assert.That(run.PauseSelection,Is.Zero);
            close.Invoke(hud,null);var used=new Event{type=EventType.MouseDown,button=3};used.Use();handle.Invoke(run,new object[]{used});
            Assert.That(run.PauseSelection,Is.Zero,"UI-consumed input must not navigate the pause menu.");
            var next=new Event{type=EventType.MouseDown,button=3};handle.Invoke(run,new object[]{next});
            Assert.That(run.PauseSelection,Is.EqualTo(1));Assert.That(next.type,Is.EqualTo(EventType.Used));
            handle.Invoke(run,new object[]{new Event{type=EventType.MouseDown,button=4}});Assert.That(run.PauseSelection,Is.Zero);
            // Outside OnGUI Unity changes synthetic middle-button events to Ignore.
            // Exercise confirmation with an extra button, whose synthetic event is supported.
            Assert.That(profile.TryBind("PauseNext","K"),Is.True);
            Assert.That(profile.TryBind("PauseConfirm","MouseBack"),Is.True);
            var confirmInput=new Event{type=EventType.MouseDown,button=3};
            Assert.That(R1ControlProfile.IsBindingMouseDown(confirmInput),Is.True);
            Assert.That(profile.MatchesEvent("PauseConfirm",confirmInput),Is.True);
            Assert.That(handle.Invoke(run,new object[]{confirmInput}),Is.EqualTo(true));Assert.That(run.Paused,Is.False);
        }
        finally{profile.ResetDefaults();}
        yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(probe!=null)Object.DestroyImmediate(probe.gameObject);
        if (Application.isPlaying) { Time.timeScale = 1; yield return new ExitPlayMode(); }
    }
}

public sealed class MainMenuControlsTests
{
    RC2EditorInputProbe probe;
    [UnityTest] public IEnumerator MainMenuControlsBlockStartingRunAndCloseOnEscape()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        var run=R1RunController.Instance;var hud=run.GetComponent<R1RunHUD>();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var open=typeof(R1RunHUD).GetMethod("OpenMainControls",flags);
        Assert.That(open,Is.Not.Null,"The main menu needs a controls screen.");open.Invoke(hud,null);
        var property=typeof(R1RunHUD).GetProperty("MainControlsOpen",flags);
        Assert.That(property.GetValue(hud),Is.True);
        probe=new GameObject("main controls input").AddComponent<RC2EditorInputProbe>();probe.InjectKey=Key.Enter;
        float deadline=Time.realtimeSinceStartup+5;
        while(!probe.Sent&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(probe.PressObserved,Is.True);yield return null;
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Ready));
        Object.DestroyImmediate(probe.gameObject);probe=null;yield return null;
        run.HandleEscape();Assert.That(property.GetValue(hud),Is.False);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Ready));
    }
    [UnityTearDown] public IEnumerator Cleanup(){if(probe!=null)Object.DestroyImmediate(probe.gameObject);if(Application.isPlaying)yield return new ExitPlayMode();}
}

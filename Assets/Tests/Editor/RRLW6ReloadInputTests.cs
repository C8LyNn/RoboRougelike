using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WeaponIntegration;

public class RRLW6ReloadInputTests
{
    Keyboard keyboard;
    InputSettings oldSettings,settings;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        oldSettings=InputSystem.settings;settings=Object.Instantiate(oldSettings);InputSystem.settings=settings;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();
    }
    [UnityTest] public IEnumerator KeyboardBufferTReachesActualRangeReloadController()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        Assert.That(run.GarageBuyWeapon(new Vector3Int(1,2,0),R4WeaponKind.Shotgun),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        yield return null;
        var gun=run.Player.GetComponentInChildren<RRLShotgunBinding>();
        Assert.That(gun,Is.Not.Null);Assert.That(run.Player.GetComponent<RRLWeaponControls>(),Is.Not.Null);
        Assert.That(gun.Fire(),Is.True);Assert.That(gun.State.Ammo,Is.EqualTo(11));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.T));
        InputSystem.Update();
        Assert.That(keyboard.tKey.isPressed,Is.True,"The fixture must deliver real keyboard input");
        // EditMode's input buffer needs an explicit pump; this is an API boundary fixture.
        run.Player.GetComponent<RRLWeaponControls>().SendMessage("Update");
        yield return null;
        Assert.That(gun.State.ReloadReadyAt,Is.GreaterThan(Time.time),"Physical keyboard T must start production reload, not only BeginReload API fixtures");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        float deadline=Time.time+2;while(Time.time<deadline&&gun.State.Ammo<12)yield return null;
        Assert.That(gun.State.Ammo,Is.EqualTo(12));
    }
    [UnityTest] public IEnumerator GuiKeyboardTUsesSameReloadWithPauseAndOwnerGates()
    {
        var run=R1RunController.Instance;Assert.That(run.OpenSandboxGarage(),Is.True);
        Assert.That(run.GarageBuyWeapon(new Vector3Int(1,2,0),R4WeaponKind.Shotgun),Is.True);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;yield return null;
        var controls=run.Player.GetComponent<RRLWeaponControls>();var gun=run.Player.GetComponentInChildren<RRLShotgunBinding>();
        var handler=typeof(RRLWeaponControls).GetMethod("HandleGuiInput");
        Assert.That(handler,Is.Not.Null,"Windows GUI key events need the same reload path as InputSystem (existing Esc/garage compatibility)");
        Assert.That(gun.Fire(),Is.True);
        var evt=new Event{type=EventType.KeyDown,keyCode=KeyCode.T};
        run.TogglePause();Assert.That((bool)handler.Invoke(controls,new object[]{evt}),Is.False);Assert.That(gun.State.ReloadReadyAt,Is.Zero);
        run.TogglePause();Assert.That((bool)handler.Invoke(controls,new object[]{evt}),Is.True);
        Assert.That(gun.State.ReloadReadyAt,Is.GreaterThan(Time.time));
        Assert.That((bool)handler.Invoke(controls,new object[]{evt}),Is.False,"Repeated GUI event cannot restart or refill reload");
        float deadline=Time.time+2;while(Time.time<deadline&&gun.State.Ammo<12)yield return null;
        Assert.That(gun.State.Ammo,Is.EqualTo(12));
        Assert.That(gun.Fire(),Is.True);
        evt=new Event{type=EventType.KeyDown,keyCode=KeyCode.None,character='t'};
        Assert.That((bool)handler.Invoke(controls,new object[]{evt}),Is.True,"Unicode GUI T uses the same guarded reload");
        deadline=Time.time+2;while(Time.time<deadline&&gun.State.Ammo<12)yield return null;
        Assert.That(gun.State.Ammo,Is.EqualTo(12));
        var other=new GameObject("Unowned controls").AddComponent<RRLWeaponControls>();
        Assert.That((bool)handler.Invoke(other,new object[]{evt}),Is.False);
        gun.GetComponent<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().ApplyDamage(10000,gun.transform.position,Vector3.up);
        Assert.That((bool)handler.Invoke(controls,new object[]{evt}),Is.False,"Destroyed weapon cannot reload through GUI fallback");
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
        if(oldSettings!=null)InputSystem.settings=oldSettings;if(settings!=null)Object.Destroy(settings);
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }
}

using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.MOVSeries;
using RoboRougelike.Mechanics.Booster;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;
using RoboRougelike.Mechanics.MOV007;
using RoboRougelike.Mechanics.MOV009;
using RogueMachine.P12PipelineSafe;

public class MOVSeriesGarageTests
{
    R1RunController run;Keyboard keyboard;Mouse mouse;InputSettings oldSettings,settings;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        yield return Until(()=>R1RunController.Instance?.Progress!=null);run=R1RunController.Instance;
        run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.OpenSandboxGarage(),Is.True);
    }
    static IEnumerator Until(Func<bool> condition)
    {
        float end=Time.realtimeSinceStartup+15;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(condition(),Is.True,"Timed out waiting for game transition.");
    }
    bool Invoke(string name,params object[] args)
    {
        var method=typeof(R1RunController).GetMethod(name);Assert.That(method,Is.Not.Null,"Missing real R1 action: "+name);
        return (bool)method.Invoke(run,args);
    }
    void Buy(Vector3Int cell,R3PartKind kind)=>Assert.That(run.GarageBuy(cell,kind),Is.True,kind+" at "+cell);
    void Corners(R3PartKind kind)
    {foreach(int x in new[]{-2,2})foreach(int z in new[]{-2,2})Buy(new Vector3Int(x,0,z),kind);}
    void FullConstruction()
    {
        Corners(R3PartKind.HoverEngine);
        foreach(int x in new[]{-2,2})Buy(new Vector3Int(x,0,0),R3PartKind.CrawlerTrack);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-3,3})Buy(new Vector3Int(x,0,z),R3PartKind.Suspension);
        Buy(new Vector3Int(-1,1,0),R3PartKind.Booster);Buy(new Vector3Int(1,1,0),R3PartKind.JumpDrive);
        Buy(new Vector3Int(0,1,1),R3PartKind.Blink);Buy(new Vector3Int(0,1,-1),R3PartKind.Gyroscope);
        Buy(new Vector3Int(-1,1,2),R3PartKind.MagneticGrapple);Buy(new Vector3Int(0,0,3),R3PartKind.OmniDrive);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(15));
        foreach(var part in run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind>=R3PartKind.Booster))
        {
            Assert.That(part.Damageable.MaxHp,Is.EqualTo(part.Kind==R3PartKind.CrawlerTrack?140:100));
            Assert.That(part.GetComponent<P12PipelineSafeFunctionalModule>(),Is.Null,"Movement geometry must not register as a weapon.");
        }
    }
    static string Snapshot(R3VehicleModel m)=>m.Scrap+"/"+m.SelectedDrive+"/"+m.GyroEnabled+"/"+
        string.Join(";",m.Parts.OrderBy(p=>p.Id).Select(p=>$"{p.Id}:{p.Kind}:{p.Cell}:{p.Health}:{p.DeviceReadyAt}"))+"/"+
        string.Join(";",m.Inventory.OrderBy(p=>p.Id).Select(p=>$"{p.Id}:{p.Kind}:{p.Health}:{p.DeviceReadyAt}"));
    void VirtualInput()
    {
        oldSettings=InputSystem.settings;settings=UnityEngine.Object.Instantiate(oldSettings);InputSystem.settings=settings;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
    }
    IEnumerator KeyPress(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return null;yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
    }
    [UnityTest] public IEnumerator AllOriginalPartsSettingsInventoryAndResourcesSurviveRangeDamageResetAndReturn()
    {
        FullConstruction();Assert.That(Invoke("ConfigureDrive",R3DriveKind.Hover),Is.True);
        Assert.That(Invoke("ConfigureMovement",2500f,1.25f,false,1.2f),Is.True);
        var wheel=run.VehicleModel.Parts.First(p=>p.Kind==R3PartKind.Wheel);string inventoryId=wheel.Id;
        Assert.That(run.GarageRemove(wheel.Cell),Is.True);var original=run.VehicleModel;string before=Snapshot(original);
        Assert.That(run.EnterTestRange(),Is.True);yield return Until(()=>!run.Transitioning);
        Assert.That(run.VehicleModel,Is.Not.SameAs(original));Assert.That(Snapshot(run.VehicleModel),Is.EqualTo(before));
        Assert.That(run.Player.GetComponent<HoverVehicleController>().LastSupportedPodCount,Is.InRange(0,4));
        Assert.That(Invoke("UseBooster"),Is.True);var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.Booster);
        part.Damageable.ApplyDamage(10000,part.transform.position,Vector3.zero);
        run.TogglePause();Assert.That(Invoke("ConfigureMovement",3000f,.8f,true,1.4f),Is.True);
        Assert.That(Snapshot(original),Is.EqualTo(before));Assert.That(run.ResetTestRange(),Is.True);yield return Until(()=>!run.Transitioning);
        Assert.That(Snapshot(run.VehicleModel),Is.EqualTo(before));Assert.That(run.VehicleModel.Inventory.Single().Id,Is.EqualTo(inventoryId));
        Assert.That(run.ReturnFromTestRange(),Is.True);yield return Until(()=>!run.Transitioning);
        Assert.That(run.VehicleModel,Is.SameAs(original));Assert.That(Snapshot(original),Is.EqualTo(before));
        Assert.That(run.VehicleModel.BoosterThrustNewtons,Is.EqualTo(2500));Assert.That(run.VehicleModel.BoosterDurationSeconds,Is.EqualTo(1.25f));
        var height=typeof(R3VehicleModel).GetField("HoverHeightMeters");Assert.That(height,Is.Not.Null);Assert.That(height.GetValue(original),Is.EqualTo(1.2f));
    }
    [UnityTest] public IEnumerator PublicActionsRejectGarageTransitionPauseAndDeadDevicesWithoutResettingCooldown()
    {
        foreach(string action in new[]{"UseBooster","UseJump","UseBlink","UseGrapple","ToggleGyro"})Assert.That(Invoke(action),Is.False);
        FullConstruction();Assert.That(run.EnterTestRange(),Is.True);Assert.That(run.Transitioning,Is.True);
        Assert.That(Invoke("UseBooster"),Is.False);yield return Until(()=>!run.Transitioning);yield return MOVSeriesDeviceTests.Seconds(.6f);
        Assert.That(Invoke("UseBooster"),Is.True);var b=run.Player.GetComponentInChildren<VehicleBooster>();
        float gate=run.VehicleModel.Parts.Single(p=>p.Kind==R3PartKind.Booster).DeviceReadyAt;
        run.TogglePause();Assert.That(run.Paused,Is.True);Assert.That(b.IsBoosting,Is.False,"Pause cancels before the stopped physics loop.");
        Assert.That(run.Player.GetComponentInChildren<GyroStabilizer>().IsRequestedActive,Is.False);
        foreach(string action in new[]{"UseBooster","UseJump","UseBlink","UseGrapple","ToggleGyro"})Assert.That(Invoke(action),Is.False);
        Assert.That(Invoke("ConfigureMovement",2400f,.8f,false,1.1f),Is.True);
        Assert.That(run.VehicleModel.Parts.Single(p=>p.Kind==R3PartKind.Booster).DeviceReadyAt,Is.EqualTo(gate));
        run.TogglePause();Assert.That(Invoke("UseBooster"),Is.False);yield return MOVSeriesDeviceTests.FixedStep();
        b.GetComponent<R3MountedPart>().Damageable.ApplyDamage(10000,b.transform.position,Vector3.zero);Assert.That(Invoke("UseBooster"),Is.False);
    }
    [UnityTest] public IEnumerator ActualKeyboardActivatesAbilitiesAndExistingMouseStillFires()
    {
        FullConstruction();VirtualInput();Assert.That(run.EnterTestRange(),Is.True);yield return Until(()=>!run.Transitioning);yield return MOVSeriesDeviceTests.Seconds(.6f);
        yield return KeyPress(Key.LeftShift);Assert.That(run.Player.GetComponentInChildren<VehicleBooster>().IsBoosting,Is.True);
        bool gyro=run.VehicleModel.GyroEnabled;yield return KeyPress(Key.G);Assert.That(run.VehicleModel.GyroEnabled,Is.EqualTo(!gyro));
        yield return KeyPress(Key.Q);Assert.That(run.VehicleModel.Parts.Single(p=>p.Kind==R3PartKind.Blink).DeviceReadyAt,Is.GreaterThan(Time.time));
        yield return KeyPress(Key.Space);Assert.That(run.VehicleModel.Parts.Single(p=>p.Kind==R3PartKind.JumpDrive).DeviceReadyAt,Is.GreaterThan(Time.time));
        yield return MOVSeriesDeviceTests.Seconds(1.4f);yield return KeyPress(Key.E);Assert.That(run.Player.GetComponent<MOVSeriesVehicleAdapter>().GrappleAttached,Is.True);
        yield return KeyPress(Key.E);Assert.That(run.Player.GetComponent<MOVSeriesVehicleAdapter>().GrappleAttached,Is.False);
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));
        yield return Until(()=>UnityEngine.Object.FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Any(s=>s.name=="P17_PlayerPhysicalShell"));
        InputSystem.QueueStateEvent(mouse,new MouseState());
    }
    [UnityTest] public IEnumerator NoWheelOmniUsesWasdStrafeIndependentYawAndKeepsWeaponOnline()
    {
        Corners(R3PartKind.OmniDrive);Buy(new Vector3Int(-1,1,0),R3PartKind.Gyroscope);
        Assert.That(Invoke("ConfigureDrive",R3DriveKind.Omni),Is.True);
        foreach(var wheel in run.VehicleModel.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray())Assert.That(run.GarageRemove(wheel.Cell),Is.True);
        VirtualInput();Assert.That(run.EnterTestRange(),Is.True);yield return Until(()=>!run.Transitioning);yield return MOVSeriesDeviceTests.Seconds(.5f);
        Assert.That(run.Player.Modules.ActiveWheelCount,Is.Zero);Assert.That(run.Player.DefeatReason,Is.Null);
        float x=run.Player.transform.position.x;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A));yield return MOVSeriesDeviceTests.Seconds(.6f);
        Assert.That(run.Player.transform.position.x,Is.LessThan(x-.2f));
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,run.Player.transform.eulerAngles.y)),Is.LessThan(5));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.C));yield return MOVSeriesDeviceTests.Seconds(.5f);
        Assert.That(run.Player.GetComponent<Rigidbody>().angularVelocity.y,Is.GreaterThan(.2f));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());Assert.That(run.Player.Modules.WeaponOnline,Is.True);Assert.That(run.RangeDisabled,Is.False);
    }
    [UnityTest] public IEnumerator HoverKeyboardMovesTurnsAndSuppressesRequestedSuspension()
    {
        Corners(R3PartKind.HoverEngine);Buy(new Vector3Int(-1,1,0),R3PartKind.Gyroscope);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-3,3})Buy(new Vector3Int(x,0,z),R3PartKind.Suspension);
        Assert.That(run.ConfigureSuspension(true,ActiveSuspensionSupportMode.FullSupport,.3f),Is.True);
        Assert.That(Invoke("ConfigureDrive",R3DriveKind.Hover),Is.True);VirtualInput();
        Assert.That(run.EnterTestRange(),Is.True);yield return Until(()=>!run.Transitioning);yield return MOVSeriesDeviceTests.Seconds(1.5f);
        Debug.Log($"MOV007 input state={run.Progress.State} paused={run.Paused} scale={Time.timeScale} mode={run.VehicleModel.SelectedDrive} control={run.Player.Controller.GameplayInputEnabled} enabled={run.Player.GetComponent<HoverVehicleController>().hoverModeEnabled} supports={run.Player.GetComponent<HoverVehicleController>().LastSupportedPodCount} gravity={run.Player.GetComponent<Rigidbody>().useGravity} constraints={run.Player.GetComponent<Rigidbody>().constraints} y={run.Player.GetComponent<Rigidbody>().position.y:F3}");
        Assert.That(run.Player.GetComponent<Rigidbody>().position.y,Is.InRange(.85f,1.25f));
        Assert.That(run.Player.GetComponent<ActiveSuspensionController>().LastTotalLiftNewtons,Is.Zero);
        float x0=run.Player.transform.position.x;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A));yield return MOVSeriesDeviceTests.Seconds(.6f);
        Assert.That(run.Player.transform.position.x,Is.LessThan(x0-.2f));InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.C));yield return MOVSeriesDeviceTests.Seconds(.5f);
        Assert.That(run.Player.GetComponent<Rigidbody>().angularVelocity.y,Is.GreaterThan(.2f));InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        run.TogglePause();Assert.That(Invoke("ConfigureDrive",R3DriveKind.Wheels),Is.True);run.TogglePause();yield return MOVSeriesDeviceTests.FixedStep();
        Assert.That(run.Player.GetComponent<ActiveSuspensionController>().SuspensionEnabled,Is.True);
        yield return MOVSeriesDeviceTests.Seconds(.8f);
        Assert.That(run.Player.GetComponent<ActiveSuspensionController>().LastTotalLiftNewtons,Is.GreaterThan(0),"Leaving Hover restores real spring forces, not only a flag.");
    }
    [UnityTest] public IEnumerator EarnedGarageDevicesWorkInNextRealGauntletCombatAlongsideEnemiesAndWeapons()
    {
        var previous=run;run.RestartRun();yield return Until(()=>R1RunController.Instance!=null&&R1RunController.Instance!=previous&&R1RunController.Instance.Progress!=null);
        run=R1RunController.Instance;run.SelectVehicle(R3VehicleKind.Light);Assert.That(run.StartRun(),Is.True);
        Assert.That(run.ChooseRoute(0),Is.True);yield return Until(()=>!run.Transitioning&&run.Progress.State==R1RunState.Combat);
        run.Encounter.SetCombatActive(false);foreach(var e in run.Encounter.Enemies)e.Core.ApplyDamage(10000,e.Core.transform.position,Vector3.zero);
        float deadline=Time.realtimeSinceStartup+15;
        while(run.Progress.State!=R1RunState.UpgradeChoice&&Time.realtimeSinceStartup<deadline)
        {if(run.Progress.State==R1RunState.Salvage)Assert.That(run.ConfirmSalvage(Array.Empty<string>()),Is.True);yield return null;}
        Assert.That(run.Progress.State,Is.EqualTo(R1RunState.UpgradeChoice));run.ChooseUpgrade(0);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Garage));
        int earned=run.VehicleModel.Scrap;Assert.That(earned,Is.GreaterThanOrEqualTo(65));
        Buy(new Vector3Int(-1,1,0),R3PartKind.Booster);Buy(new Vector3Int(1,1,0),R3PartKind.Gyroscope);
        Assert.That(run.VehicleModel.Scrap,Is.EqualTo(earned-65));string id=run.VehicleModel.Parts.Single(p=>p.Kind==R3PartKind.Booster).Id;
        Assert.That(run.StartNextEncounter(),Is.True);if(run.Progress.State==R1RunState.Route)Assert.That(run.ChooseRoute(0),Is.True);
        yield return Until(()=>!run.Transitioning&&run.Progress.State==R1RunState.Combat);VirtualInput();
        var positions=run.Encounter.Enemies.Select(e=>e.transform.position).ToArray();
        Assert.That(Invoke("UseBooster"),Is.True);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));yield return MOVSeriesDeviceTests.Seconds(.6f);
        Assert.That(run.Player.GetComponent<Rigidbody>().linearVelocity.magnitude,Is.GreaterThan(.5f));
        Assert.That(run.Player.GetComponentInChildren<VehicleBooster>().IsBoosting,Is.True);
        Assert.That(run.Player.GetComponentInChildren<VehicleBooster>().GetComponent<R3MountedPart>().Id,Is.EqualTo(id));
        Assert.That(run.Player.Modules.WeaponOnline,Is.True);Assert.That(run.Progress.State,Is.EqualTo(R1RunState.Combat));
        Assert.That(run.Encounter.Enemies.Where((e,i)=>Vector3.Distance(e.transform.position,positions[i])>.1f).Any(),Is.True);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState());
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
        if(oldSettings!=null)InputSystem.settings=oldSettings;if(settings!=null)UnityEngine.Object.Destroy(settings);
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }
}

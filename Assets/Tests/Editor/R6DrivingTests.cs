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
using RoboRougelike.R4;
using RoboRougelike.R6;

public class R6DrivingTests
{
    R1RunController run;Keyboard keyboard;InputSettings oldSettings,settings;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
        oldSettings=InputSystem.settings;settings=Object.Instantiate(oldSettings);InputSystem.settings=settings;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();run.StartRun();run.ChooseRoute(0);
        while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);
    }
    IEnumerator Seconds(float duration){float until=Time.time+duration;while(Time.time<until)yield return null;}
    [UnityTest] public IEnumerator PhysicalMassSetsAccelerationAndBrakingWhileReverseRemainsUsable()
    {
        var palette=Object.FindFirstObjectByType<R3VisualPalette>();
        var vehicles=new[]{R3VehicleKind.Light,R3VehicleKind.Standard,R3VehicleKind.Heavy}.Select((kind,index)=>
            R3VehicleAssembly.Build(new R3VehicleModel(R3VehicleCatalog.Create(kind)),new R1RunStats(),palette,new Vector3(-18+index*18,.7f,-31),Quaternion.identity)).ToArray();
        foreach(var vehicle in vehicles)vehicle.Controller.SetGameplayInputEnabled(true);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return Seconds(.6f);
        float[] speeds=vehicles.Select(v=>v.GetComponent<Rigidbody>().linearVelocity.z).ToArray();
        Debug.Log("R6 0.6s acceleration Light/Standard/Heavy m/s: "+string.Join(", ",speeds));
        Assert.That(speeds[0],Is.GreaterThan(speeds[1]+.25f));Assert.That(speeds[1],Is.GreaterThan(speeds[2]+.25f));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Seconds(.2f);
        for(int i=0;i<3;i++){float speed=vehicles[i].GetComponent<Rigidbody>().linearVelocity.z;Assert.That(speed,Is.InRange(.2f,speeds[i]));}
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));yield return Seconds(.65f);
        foreach(var vehicle in vehicles)Assert.That(vehicle.GetComponent<Rigidbody>().linearVelocity.z,Is.LessThan(.3f),"Opposite throttle must brake.");
        yield return Seconds(.7f);foreach(var vehicle in vehicles)Assert.That(vehicle.GetComponent<Rigidbody>().linearVelocity.z,Is.LessThan(-.5f));
    }
    [UnityTest] public IEnumerator RealVehicleContactTransfersMomentumWithoutExplosiveBounce()
    {
        var enemy=run.Encounter.Enemies.Single();var other=enemy.GetComponent<Rigidbody>();
        other.position=new Vector3(0,0,-21);run.Player.MoveTo(new Vector3(0,.7f,-25.5f),Quaternion.identity);Physics.SyncTransforms();
        var body=run.Player.GetComponent<Rigidbody>();body.linearVelocity=Vector3.forward*5;float start=other.position.z;
        float until=Time.time+1.2f,maxSpeed=0,maxSpin=0;
        while(Time.time<until)
        {
            maxSpeed=Mathf.Max(maxSpeed,body.linearVelocity.magnitude,other.linearVelocity.magnitude);
            maxSpin=Mathf.Max(maxSpin,body.angularVelocity.magnitude,other.angularVelocity.magnitude);
            Assert.That(body.position.y,Is.EqualTo(.7f).Within(.01f));yield return null;
        }
        Debug.Log($"R6 physical push: displacement={other.position.z-start:0.00}, peak speed={maxSpeed:0.00}, spin={maxSpin:0.00}");
        Assert.That(other.position.z-start,Is.GreaterThan(.15f));Assert.That(maxSpeed,Is.LessThan(6));Assert.That(maxSpin,Is.LessThan(3.2f));
    }
    [UnityTest] public IEnumerator HeavyWeaponAndAsymmetricWheelLossChangeMassBalanceAndSteering()
    {
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var drive=run.Player.GetComponent<R6VehicleDrive>();var body=run.Player.GetComponent<Rigidbody>();
        drive.RefreshMass();float before=body.mass;
        var wheel=assembly.Parts.First(p=>p.Kind==R3PartKind.Wheel&&p.Cell.x<0);
        wheel.Damageable.ApplyDamage(10000,wheel.transform.position,Vector3.left);run.Player.Structure.EvaluateStructure();drive.RefreshMass();
        Assert.That(body.mass,Is.LessThan(before));Assert.That(Mathf.Abs(drive.WheelImbalance),Is.GreaterThan(.02f));
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return Seconds(1);
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,body.rotation.eulerAngles.y)),Is.GreaterThan(.1f));
        var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));var heavy=model.DeepCopy();
        heavy.Parts.First(p=>p.Kind==R3PartKind.Gun).Weapon=R4WeaponKind.Heavy;
        var palette=Object.FindFirstObjectByType<R3VisualPalette>();
        var a=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(-25,.7f,-30),Quaternion.identity).GetComponent<Rigidbody>();
        var b=R3VehicleAssembly.Build(heavy,new R1RunStats(),palette,new Vector3(25,.7f,-30),Quaternion.identity).GetComponent<Rigidbody>();
        Assert.That(b.mass-a.mass,Is.EqualTo(R4WeaponDefinition.Get(R4WeaponKind.Heavy).Mass-R4WeaponDefinition.Get(R4WeaponKind.Basic).Mass).Within(.01f));
        Assert.That(b.centerOfMass.y,Is.GreaterThan(a.centerOfMass.y));
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(oldSettings!=null)InputSystem.settings=oldSettings;
        if(settings!=null)Object.Destroy(settings);if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }
}

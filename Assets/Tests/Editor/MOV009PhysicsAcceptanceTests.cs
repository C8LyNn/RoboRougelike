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
using RoboRougelike.MOV009Integration;
using RoboRougelike.Mechanics.MOV009;

public class MOV009PhysicsAcceptanceTests
{
    R3VisualPalette palette;GameObject fixtures;Keyboard keyboard;InputSettings oldSettings,settings;
    readonly R3VehicleKind[] kinds={R3VehicleKind.Light,R3VehicleKind.Standard,R3VehicleKind.Heavy};
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;
        R1RunController.Instance.SelectVehicle(R3VehicleKind.Light);R1RunController.Instance.OpenSandboxGarage();
        Time.timeScale=1;palette=UnityEngine.Object.FindFirstObjectByType<R3VisualPalette>();fixtures=new GameObject("MOV009 acceptance fixtures");
        oldSettings=InputSystem.settings;settings=UnityEngine.Object.Instantiate(oldSettings);InputSystem.settings=settings;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();
    }
    static IEnumerator Seconds(float value){float end=Time.time+value;while(Time.time<end)yield return null;}
    GameObject Floor(float x)
    {
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(fixtures.transform);
        floor.transform.position=new Vector3(x,-.5f,20);floor.transform.localScale=new Vector3(12,1,70);return floor;
    }
    R3VehicleModel Model(R3VehicleKind kind)
    {
        var model=new R3VehicleModel(R3VehicleCatalog.Create(kind));model.Award(1000);
        foreach(var cell in MOV009ModelTests.Cells(kind))Assert.That(model.Buy(cell,R3PartKind.Suspension),Is.True);
        return model;
    }
    R1VehicleState Build(R3VehicleModel model,float x,float z=0)
    {
        var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(x,.7f,z),Quaternion.identity);
        vehicle.transform.SetParent(fixtures.transform,true);return vehicle;
    }
    static float Tilt(Rigidbody body)=>Vector3.Angle(body.transform.up,Vector3.up);
    [UnityTest] public IEnumerator OffAssistAndFullAreComparedOnAllThreeRealStarters()
    {
        for(int k=0;k<3;k++)
        {
            var vehicles=new R1VehicleState[3];
            for(int mode=0;mode<3;mode++)
            {
                float x=100+mode*20;Floor(x);var model=Model(kinds[k]);model.SuspensionEnabled=mode!=0;
                model.SuspensionMode=mode==1?ActiveSuspensionSupportMode.AssistOnly:ActiveSuspensionSupportMode.FullSupport;
                vehicles[mode]=Build(model,x);
            }
            float[] min={999,999,999},max={-999,-999,-999};float end=Time.time+2.5f;
            while(Time.time<end)
            {
                if(Time.time>end-1)for(int mode=0;mode<3;mode++){float y=vehicles[mode].transform.position.y;min[mode]=Mathf.Min(min[mode],y);max[mode]=Mathf.Max(max[mode],y);}
                yield return null;
            }
            for(int mode=0;mode<3;mode++)
            {
                var body=vehicles[mode].GetComponent<Rigidbody>();var c=vehicles[mode].GetComponent<ActiveSuspensionController>();
                Debug.Log($"MOV009 COMPARE {kinds[k]} mode={mode} mass={body.mass:F0} y={body.position.y:F3} hullClearance={body.position.y-.26f:F3} tilt={Tilt(body):F2} vy={body.linearVelocity.y:F3} bob={max[mode]-min[mode]:F3} lift={c.LastTotalLiftNewtons:F1}");
                Assert.That(body.position.y,Is.InRange(.55f,1));Assert.That(max[mode]-min[mode],Is.LessThan(.10f));
                Assert.That(Tilt(body),Is.LessThan(5));Assert.That(Mathf.Abs(body.linearVelocity.y),Is.LessThan(.5f));
                Assert.That(c.LastTotalLiftNewtons,Is.LessThanOrEqualTo(body.mass*18+.1f));
                if(mode==0)Assert.That(c.LastTotalLiftNewtons,Is.EqualTo(0));
            }
            Assert.That(vehicles[2].transform.position.y,Is.GreaterThan(vehicles[0].transform.position.y+.02f));
            foreach(var v in vehicles){v.gameObject.SetActive(false);UnityEngine.Object.Destroy(v.gameObject);}
            foreach(Transform child in fixtures.transform)if(child!=null){child.gameObject.SetActive(false);UnityEngine.Object.Destroy(child.gameObject);}
            yield return null;
        }
    }
    [UnityTest] public IEnumerator IncreasedRealConstructionMassSettlesAndLosesOneThenTwoSupports()
    {
        Floor(100);var model=Model(R3VehicleKind.Standard);
        for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++)if(x!=0||z!=0)Assert.That(model.Buy(new Vector3Int(x,2,z),R3PartKind.Armor),Is.True);
        var vehicle=Build(model,100);var body=vehicle.GetComponent<Rigidbody>();var adapter=vehicle.GetComponent<MOV009VehicleAdapter>();
        Assert.That(body.mass,Is.EqualTo(968).Within(.1));
        var parts=vehicle.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.Suspension).ToArray();
        for(int lost=0;lost<=2;lost++)
        {
            if(lost>0)parts[lost-1].Damageable.ApplyDamage(10000,parts[lost-1].transform.position,Vector3.zero);
            yield return Seconds(1.5f);
            Debug.Log($"MOV009 MASS/LOSS lost={lost} mass={body.mass:F0} y={body.position.y:F3} tilt={Tilt(body):F2} vy={body.linearVelocity.y:F3} lift={adapter.Controller.LastTotalLiftNewtons:F1} k={adapter.StiffnessPerModule:F1} cap={adapter.MaximumForcePerModule:F1}");
            Assert.That(adapter.OperationalSupports,Is.EqualTo(4-lost));Assert.That(body.position.y,Is.InRange(.35f,1.2f));
            Assert.That(Tilt(body),Is.LessThan(25));Assert.That(body.linearVelocity.magnitude,Is.LessThan(5));
        }
    }
    [UnityTest] public IEnumerator TunedPackageSupportsDemonstratorMasses50To1000Kg()
    {
        int[] masses={50,100,500,1000};var bodies=new Rigidbody[4];
        for(int i=0;i<4;i++)
        {
            float x=100+i*20;Floor(x);var root=new GameObject("MOV009 demo "+masses[i]);root.transform.SetParent(fixtures.transform);root.transform.position=new Vector3(x,.72f,0);
            root.AddComponent<BoxCollider>().size=new Vector3(2,.5f,2);var body=root.AddComponent<Rigidbody>();body.mass=masses[i];bodies[i]=body;
            var c=root.AddComponent<ActiveSuspensionController>();c.SetSupportMode(ActiveSuspensionSupportMode.FullSupport);
            float supported=masses[i]/4f,k=supported*9.81f/.12f;
            for(int j=0;j<4;j++)
            {
                var point=new GameObject("Demo support");point.transform.SetParent(root.transform,false);point.transform.localPosition=new Vector3(j%2==0?-.7f:.7f,-.35f,j<2?-.65f:.65f);
                point.AddComponent<ActiveSuspensionModule>().ConfigureSpring(.55f,k,.85f*2*Mathf.Sqrt(k*supported),supported*9.81f*2.4f);
            }
        }
        yield return Seconds(2.5f);
        foreach(var body in bodies)
        {
            var c=body.GetComponent<ActiveSuspensionController>();Debug.Log($"MOV009 DEMO mass={body.mass:F0} y={body.position.y:F3} vy={body.linearVelocity.y:F3} lift={c.LastTotalLiftNewtons:F1}");
            Assert.That(body.position.y,Is.InRange(.65f,.9f));Assert.That(Mathf.Abs(body.linearVelocity.y),Is.LessThan(.25f));
            Assert.That(c.LastTotalLiftNewtons,Is.InRange(body.mass*9,body.mass*10.5f));
        }
    }
    [UnityTest] public IEnumerator ActualWasdDrivesEveryStarterAcrossSixBlockTerrainCases()
    {
        var type=Type.GetType("RoboRougelike.MOV009Integration.MOV009TerrainCourse, Assembly-CSharp");
        Assert.That(type,Is.Not.Null,"Missing shared playable block terrain course.");var method=type.GetMethod("BuildLane");Assert.That(method,Is.Not.Null);
        foreach(string terrain in new[]{"Flat","LowSteps","Uneven","Slope","Edge","Adjacent"})
        {
            var vehicles=new R1VehicleState[3];var peakY=new float[3];var peakVy=new float[3];var peakTilt=new float[3];
            for(int i=0;i<3;i++)
            {
                method.Invoke(null,new object[]{fixtures.transform,new Vector3(100+i*20,0,0),palette.Parts[0],terrain,true});
                vehicles[i]=Build(Model(kinds[i]),100+i*20);
            }
            yield return Seconds(1);foreach(var v in vehicles)v.Controller.SetGameplayInputEnabled(true);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));float end=Time.time+7;
            while(Time.time<end)
            {
                for(int i=0;i<3;i++)
                {
                    var b=vehicles[i].GetComponent<Rigidbody>();peakY[i]=Mathf.Max(peakY[i],b.position.y);peakVy[i]=Mathf.Max(peakVy[i],Mathf.Abs(b.linearVelocity.y));peakTilt[i]=Mathf.Max(peakTilt[i],Tilt(b));
                    Assert.That(float.IsNaN(b.position.y)||float.IsInfinity(b.position.y),Is.False);
                }
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            for(int i=0;i<3;i++)
            {
                var b=vehicles[i].GetComponent<Rigidbody>();Debug.Log($"MOV009 TERRAIN {terrain} {kinds[i]} z={b.position.z:F2} maxY={peakY[i]:F2} maxVy={peakVy[i]:F2} maxTilt={peakTilt[i]:F2}");
                Assert.That(b.position.z,Is.GreaterThan(18),terrain+" "+kinds[i]+" did not traverse the obstacle.");
                Assert.That(peakY[i],Is.LessThan(2.5f));Assert.That(peakVy[i],Is.LessThan(5));Assert.That(peakTilt[i],Is.LessThan(45));
            }
            foreach(Transform child in fixtures.transform){child.gameObject.SetActive(false);UnityEngine.Object.Destroy(child.gameObject);}yield return null;
        }
    }
    [UnityTest] public IEnumerator FullSupportVehicleContactDoesNotCreateExplosiveRepulsion()
    {
        Floor(100);var moving=Build(Model(R3VehicleKind.Light),100,-6);var target=Build(Model(R3VehicleKind.Standard),100,0);
        yield return Seconds(1);var a=moving.GetComponent<Rigidbody>();var b=target.GetComponent<Rigidbody>();a.linearVelocity=Vector3.forward*9;
        float end=Time.time+2,peakSpeed=0,peakY=0,peakVy=0;
        while(Time.time<end)
        {
            peakSpeed=Mathf.Max(peakSpeed,a.linearVelocity.magnitude,b.linearVelocity.magnitude);peakY=Mathf.Max(peakY,a.position.y,b.position.y);peakVy=Mathf.Max(peakVy,Mathf.Abs(a.linearVelocity.y),Mathf.Abs(b.linearVelocity.y));yield return null;
        }
        Debug.Log($"MOV009 CONTACT speed={peakSpeed:F2} maxY={peakY:F2} maxVy={peakVy:F2} targetZ={b.position.z:F2}");
        Assert.That(peakSpeed,Is.LessThanOrEqualTo(9.5f));Assert.That(peakY,Is.LessThan(1.6f));Assert.That(peakVy,Is.LessThan(3));Assert.That(b.position.z,Is.GreaterThan(.1f));
    }
    [UnityTest] public IEnumerator HeavyTraversesRampPlatformSeam()
    {
        MOV009TerrainCourse.BuildLane(fixtures.transform,new Vector3(100,0,0),palette.Parts[0],"Slope",true);
        var vehicle=Build(Model(R3VehicleKind.Heavy),100);var body=vehicle.GetComponent<Rigidbody>();var contact=vehicle.gameObject.AddComponent<MOV009ContactProbe>();
        yield return Seconds(1);vehicle.Controller.SetGameplayInputEnabled(true);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
        for(int i=0;i<12;i++)
        {
            yield return Seconds(1);
            Debug.Log($"MOV009 SEAM t={i+1} p={body.position:F3} v={body.linearVelocity:F3} euler={body.rotation.eulerAngles:F2} probes="+
                string.Join(";",vehicle.GetComponentsInChildren<ActiveSuspensionModule>().Select(m=>$"{m.LastGroundContact}:{m.LastGroundDistanceMeters:F3}:{m.LastAppliedForceNewtons:F1}")));
            if(i==5)Debug.Log($"MOV009 SEAM CONTACTS wheels={vehicle.GetComponent<RoboRougelike.R6.R6VehicleDrive>().Wheels} force={vehicle.GetComponent<RoboRougelike.R6.R6VehicleDrive>().AvailableForce} input={vehicle.Controller.GameplayInputEnabled} W={Keyboard.current.wKey.isPressed} throttle={vehicle.Controller.GetType().GetField("moveInput",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(vehicle.Controller)} "+contact.Summary);
        }
        Assert.That(body.position.z,Is.GreaterThan(18));
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(oldSettings!=null)InputSystem.settings=oldSettings;if(settings!=null)UnityEngine.Object.Destroy(settings);
        if(fixtures!=null)UnityEngine.Object.Destroy(fixtures);
        if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}
    }
}

using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R6;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;

public sealed class Camera275ConstructionTests
{
    R1RunController run;GameObject wall;P12PipelineSafeCamera follow;Camera275RuntimeInput native;
    [UnitySetUp]public IEnumerator Setup()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity");yield return new EnterPlayMode();
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
        native=new GameObject("CAM275 construction input isolation").AddComponent<Camera275RuntimeInput>();
    }
    IEnumerator Prepare(R3VehicleKind kind,string extra="")
    {
        Assert.That(run.SelectVehicle(kind)&&run.OpenSandboxGarage(),Is.True);
        if(extra=="Tall")for(int y=2;y<=5;y++)Assert.That(run.GarageBuy(new Vector3Int(1,y,0),R3PartKind.Armor),Is.True,"Tall added cell "+y);
        if(extra=="Wide")foreach(int sign in new[]{-1,1})for(int x=3;x<=6;x++)Assert.That(run.GarageBuy(new Vector3Int(sign*x,1,0),R3PartKind.Armor),Is.True,"Wide added cell "+sign*x);
        Assert.That(run.EnterTestRange(),Is.True);while(run.Transitioning)yield return null;
        run.GetComponent<R6TestRange>().Layout.gameObject.SetActive(false);
        run.Player.GetComponent<Rigidbody>().isKinematic=true;
        follow=Object.FindFirstObjectByType<P12PipelineSafeCamera>();yield return new WaitForSecondsRealtime(.5f);
    }
    IEnumerator Construction(R3VehicleKind kind,string extra="")
    {
        yield return Prepare(kind,extra);follow.enabled=false;var rig=follow.ReceiverRig;
        // Deterministic final-pose safety sweep on a real assembly. Separate
        // Windows tests provide actual force-driven travel at each FPS target.
        foreach(int fps in new[]{30,60,120})
        {
            for(int frame=0;frame<fps*3;frame++)
            {
                float t=frame/(fps*3f);rig.SetOrbit(t*360,Mathf.Lerp(-5,58,t),Mathf.Lerp(3,45,t));rig.StepCamera(1f/fps);
                Assert.That(rig.ActualOwnHullIntersectsCrosshair,Is.False,$"{kind}/{extra} fps={fps} frame={frame}");
                Assert.That(rig.WorldOverlapAtCamera,Is.False,$"{kind}/{extra} fps={fps} frame={frame}");
                Assert.That(rig.LastFollowStepMeters,Is.LessThanOrEqualTo(rig.EffectiveContinuousSpeedLimit/fps+.001f));
                if(!rig.WorldClipped)
                {
                    var b=rig.LastHullBounds;
                    for(int i=0;i<8;i++)
                    {
                        var e=b.extents;var corner=b.center+new Vector3((i&1)==0?-e.x:e.x,(i&2)==0?-e.y:e.y,(i&4)==0?-e.z:e.z);
                        var v=Camera.main.WorldToViewportPoint(corner);
                        Assert.That(v.z,Is.GreaterThan(0));Assert.That(v.x,Is.InRange(0f,1f));Assert.That(v.y,Is.InRange(0f,1f));
                    }
                }
            }
            yield return null;
        }
        int resets=rig.SmoothingResetCount;
        var armor=run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.Armor).OrderByDescending(p=>p.Cell.y).First();
        armor.Damageable.ApplyDamage(1000,armor.transform.position,Vector3.right);yield return null;
        Assert.That(armor.Operational,Is.False);Assert.That(armor.transform.IsChildOf(run.Player.transform),Is.False);
        for(int frame=0;frame<120;frame++){rig.StepCamera(1f/60);Assert.That(rig.ActualOwnHullIntersectsCrosshair,Is.False);}
        Assert.That(rig.SmoothingResetCount,Is.EqualTo(resets),"Normal part detachment does not restart smoothing");
        wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="CAM275 receiver orbit wall";
        var direction=(Camera.main.transform.position-rig.LastSolution.AimWorld).normalized;
        wall.transform.position=rig.LastSolution.AimWorld+direction*12;wall.transform.rotation=Quaternion.LookRotation(direction);wall.transform.localScale=new Vector3(12,12,1);
        Physics.SyncTransforms();bool clipped=false;
        for(int frame=0;frame<180;frame++)
        {
            rig.SetOrbit(frame*2,18,45);rig.StepCamera(1f/60);clipped|=rig.WorldClipped;
            Assert.That(rig.WorldOverlapAtCamera,Is.False,"Receiver sweep must protect the final camera volume");
            Assert.That(rig.ActualOwnHullIntersectsCrosshair,Is.False);
        }
        Assert.That(clipped,Is.True,"Test must actually exercise a collision correction");
    }
    [UnityTest]public IEnumerator LightConstructionSafety()=>Construction(R3VehicleKind.Light);
    [UnityTest]public IEnumerator StandardConstructionSafety()=>Construction(R3VehicleKind.Standard);
    [UnityTest]public IEnumerator HeavyConstructionSafety()=>Construction(R3VehicleKind.Heavy);
    [UnityTest]public IEnumerator TallConstructionSafety()=>Construction(R3VehicleKind.Standard,"Tall");
    [UnityTest]public IEnumerator WideConstructionSafety()=>Construction(R3VehicleKind.Standard,"Wide");
    [UnityTest]public IEnumerator OverlappedMuzzleIsNotReportedClear()
    {
        yield return Prepare(R3VehicleKind.Standard);
        var assembly=run.Player.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.First(p=>p.Kind==R3PartKind.Gun);
        gun.GunPivot.rotation=Quaternion.identity;
        var point=gun.Muzzle.position+gun.Muzzle.forward*30;
        wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="CAM275 enclosing muzzle wall";
        wall.transform.position=gun.Muzzle.position;wall.transform.localScale=Vector3.one*.5f;Physics.SyncTransforms();
        var snapshot=RRLAimFeedback.Evaluate(assembly,run.Player.Controller,point,null);
        Assert.That(snapshot.InRange,Is.EqualTo(1));Assert.That(snapshot.Aligned,Is.EqualTo(1));
        Assert.That(snapshot.Clear,Is.Zero,"A ray starting inside an obstruction must not advertise a clear muzzle");
    }
    [UnityTest]public IEnumerator LosingLastWeaponClearsReticleState()
    {
        yield return Prepare(R3VehicleKind.Standard);yield return null;
        Assert.That(run.Player.Controller.AimFeedback.Guns,Is.GreaterThan(0));
        foreach(var gun in run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.Gun))
            gun.Damageable.ApplyDamage(1000,gun.transform.position,Vector3.forward);
        yield return null;yield return null;
        Assert.That(run.Player.Controller.AimFeedback.Guns,Is.Zero);
        StringAssert.Contains("Brak sprawnej broni",run.Player.Controller.AimFeedback.Caption);
    }
    [UnityTearDown]public IEnumerator Cleanup()
    {if(native!=null)Object.DestroyImmediate(native.gameObject);if(wall!=null)Object.Destroy(wall);if(Application.isPlaying){Time.timeScale=1;yield return new ExitPlayMode();}}
}

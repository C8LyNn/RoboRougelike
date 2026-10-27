#if UNITY_EDITOR || B01_UNIFIED_RC2_ACCEPTANCE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R6;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

// Explicit opt-in fixture, real camera mouse consumption, canonical gun aiming,
// native projectiles and P12 damage. Static targets and held chassis isolate aim;
// these results are not classified as R6 drive or human combat acceptance.
public sealed class Camera275AimQA : MonoBehaviour
{
    [Serializable]public sealed class ShotResult
    {
        public string weapon,status="Running",error,hitCollider,feedback;
        public float requestedDistance,muzzleDistance,damage,muzzleError,range,pitch;
        public bool expectedHit,cameraOnTarget;
        public Vector3 origin,direction,aimPoint;
    }
    [Serializable]public sealed class Report
    {
        public string status="Running",method="Native queued LMB and mouse aim; real projectiles / P12 HP, isolated stationary clear lane. Not human play or vehicle drive.";
        public List<ShotResult> shots=new List<ShotResult>();
        public List<string> errors=new List<string>();
    }
    public Report Results=new Report();
    public Camera275RuntimeInput Input;
    R1RunController run;GameObject target;string output;
    static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Install()
    {
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-camera275-aim-output");if(index<0||index+1>=args.Length)return;
        var root=new GameObject("CAM275 opt-in projectile QA");DontDestroyOnLoad(root);
        root.AddComponent<Camera275AimQA>().output=args[index+1];
    }
    IEnumerator Start()
    {
        if(output==null)yield break;
        Directory.CreateDirectory(output);Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
        Input=gameObject.AddComponent<Camera275RuntimeInput>();Application.logMessageReceived+=Log;
        while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
        foreach(var weapon in new[]{R4WeaponKind.Basic,R4WeaponKind.Automatic,R4WeaponKind.Heavy,R4WeaponKind.Shotgun,R4WeaponKind.Railgun,R4WeaponKind.BeamCutter,R4WeaponKind.RocketLauncher})
            foreach(float distance in new[]{10f,30f,60f,100f})
            {
                var shot=new ShotResult{weapon=weapon.ToString(),requestedDistance=distance};Results.shots.Add(shot);
                var routine=Execute(run,Input,weapon,distance,shot);
                while(true)
                {
                    bool more=false;try{more=routine.MoveNext();}catch(Exception e){shot.error=e.ToString();shot.status="Failed";}
                    if(!more)break;yield return routine.Current;
                }
                Save();
            }
        Results.status=Results.shots.All(s=>s.status=="Passed")&&Results.errors.Count==0?"Passed":"Failed";
        Input.Fire=false;Input.MouseRate=Vector2.zero;Save();Application.logMessageReceived-=Log;Destroy(Input);
        if(Results.status=="Passed")run.RequestQuit();else Application.Quit(1);
    }
    void Save()=>File.WriteAllText(Path.Combine(output,"aim-result.json"),JsonUtility.ToJson(Results,true));
    void Log(string text,string stack,LogType kind){if(kind==LogType.Exception||kind==LogType.Error||kind==LogType.Assert)Results.errors.Add(text);}
    static void Require(bool condition,string text){if(!condition)throw new InvalidOperationException(text);}
    public IEnumerator Execute(R1RunController owner,Camera275RuntimeInput native,R4WeaponKind weapon,float distance,ShotResult result)
    {
        if(owner.Progress.State==R1RunState.TestDrive){Require(owner.ReturnFromTestRange(),"Return to workshop");while(owner.Transitioning)yield return null;}
        if(owner.VehicleModel==null)Require(owner.OpenSandboxGarage(),"Open actual workshop");
        // The garage correctly forbids removing the last gun. Install its
        // replacement first, then remove the old gun through the normal rules.
        var oldGuns=owner.VehicleModel.Parts.Where(p=>p.Kind==R3PartKind.Gun).ToArray();
        var cell=oldGuns.Any(p=>p.Cell==new Vector3Int(1,2,0))?new Vector3Int(0,2,0):new Vector3Int(1,2,0);
        Require(owner.GarageBuyWeapon(cell,weapon),"Mount actual weapon "+weapon);
        foreach(var gun in oldGuns)Require(owner.GarageRemove(gun.Cell),"Remove previous gun");
        yield return null;Require(owner.EnterTestRange(),"Enter actual range");while(owner.Transitioning)yield return null;
        yield return new WaitForSecondsRealtime(.5f);
        var body=owner.Player.GetComponent<Rigidbody>();body.isKinematic=true;
        owner.GetComponent<R6TestRange>().Layout.gameObject.SetActive(false);
        var follow=FindFirstObjectByType<P12PipelineSafeCamera>();
        // Starting intent only. All convergence thereafter is supplied through mouse input.
        typeof(P12PipelineSafeCamera).GetField("yaw",flags).SetValue(follow,0f);
        typeof(P12PipelineSafeCamera).GetField("pitch",flags).SetValue(follow,18f);
        target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.name="CAM275 P12 target "+distance+"m";
        target.transform.position=body.position+Vector3.forward*distance+Vector3.up*1.5f;
        target.transform.localScale=new Vector3(3,3,1);
        target.GetComponent<Renderer>().sharedMaterial=owner.Player.GetComponentInChildren<Renderer>().sharedMaterial;
        var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(10000,false);Physics.SyncTransforms();
        var gunPart=owner.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Kind==R3PartKind.Gun);
        float deadline=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<deadline)
        {
            var viewport=Camera.main.WorldToViewportPoint(target.transform.position);
            Vector2 pixels=Vector2.Scale(new Vector2(viewport.x-.5f,viewport.y-.5f),new Vector2(Camera.main.fieldOfView*Camera.main.aspect,Camera.main.fieldOfView))/.12f;
            native.MouseRate=Vector2.ClampMagnitude(pixels,10)/Mathf.Max(.001f,Time.unscaledDeltaTime);
            yield return null;
            if((new Vector2(viewport.x-.5f,viewport.y-.5f)).magnitude<.0005f&&
                owner.Player.Controller.LastConsumedAimCollider==target.GetComponent<Collider>()&&
                Vector3.Angle(gunPart.Muzzle.forward,owner.Player.Controller.LastConsumedAimPoint-gunPart.Muzzle.position)<.1f)break;
        }
        native.MouseRate=Vector2.zero;yield return null;
        var controller=owner.Player.Controller;var muzzle=gunPart.Muzzle;
        result.cameraOnTarget=controller.LastConsumedAimCollider==target.GetComponent<Collider>();
        result.hitCollider=controller.LastConsumedAimCollider?.name;result.aimPoint=controller.LastConsumedAimPoint;
        result.origin=muzzle.position;result.direction=muzzle.forward;result.muzzleDistance=Vector3.Distance(muzzle.position,result.aimPoint);
        result.muzzleError=Vector3.Angle(muzzle.forward,result.aimPoint-muzzle.position);
        result.range=RoboRougelike.WeaponIntegration.RRLAimFeedback.Range(gunPart,controller);result.feedback=controller.AimFeedback.Caption;
        result.pitch=(float)typeof(P12PipelineSafeCamera).GetField("pitch",flags).GetValue(follow);
        result.expectedHit=weapon==R4WeaponKind.Shotgun?distance<=30:weapon==R4WeaponKind.BeamCutter?distance<=10:true;
        Require(result.cameraOnTarget,"Final camera ray did not hit intended target: "+result.hitCollider);
        Require(result.muzzleError<.15f,"Gun has not converged "+result.muzzleError);
        if(output!=null&&distance==100)ScreenCapture.CaptureScreenshot(Path.Combine(output,weapon+"-100m.png"));
        native.Fire=true;yield return new WaitForSecondsRealtime(.16f);native.Fire=false;
        float wait=weapon==R4WeaponKind.Shotgun||weapon==R4WeaponKind.BeamCutter?2.3f:distance/Mathf.Max(18,R4WeaponDefinition.Get(weapon).Speed)+.7f;
        yield return new WaitForSecondsRealtime(wait);
        result.damage=10000-hp.CurrentHp;
        result.status=(result.expectedHit?result.damage>0:result.damage==0)?"Passed":"Failed";
        if(!result.expectedHit&&controller.AimFeedback.InRange!=0){result.status="Failed";result.error="Reticle did not disclose limited weapon range";}
        Debug.Log($"[CAM275-SHOT] {weapon} {distance}m {result.status} HP loss={result.damage} muzzleError={result.muzzleError} pitch={result.pitch} feedback={result.feedback}");
        Destroy(target);target=null;yield return null;
    }
    void OnDestroy(){if(target!=null)Destroy(target);}
}
#endif

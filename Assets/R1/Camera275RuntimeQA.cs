#if UNITY_EDITOR || B01_UNIFIED_RC2_ACCEPTANCE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using B01.CAMERA001;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RogueMachine.P12PipelineSafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[DefaultExecutionOrder(-2000)]
public sealed class Camera275RuntimeInput : MonoBehaviour
{
    public Keyboard Keys {get;private set;}
    public Mouse Mouse {get;private set;}
    public float Throttle,Steering;
    public bool Fire;
    public Vector2 MouseRate,AppliedDelta,Scroll;
    InputSettings.BackgroundBehavior oldBackground;
    readonly List<InputDevice> muted=new List<InputDevice>();
#if UNITY_EDITOR
    InputSettings.EditorInputBehaviorInPlayMode oldRouting;
#endif
    void Awake(){oldBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        foreach(var device in InputSystem.devices)if(device.enabled&&(device is Keyboard||device is Mouse||device is Gamepad)){muted.Add(device);InputSystem.DisableDevice(device);}
        Keys=InputSystem.AddDevice<Keyboard>();Mouse=InputSystem.AddDevice<Mouse>();
#if UNITY_EDITOR
        oldRouting=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
    }
    void Update()
    {
        Keys.MakeCurrent();Mouse.MakeCurrent();
        var keys=new List<Key>();if(Throttle>.1f)keys.Add(Key.W);else if(Throttle<-.1f)keys.Add(Key.S);
        if(Steering>.1f)keys.Add(Key.D);else if(Steering<-.1f)keys.Add(Key.A);
        AppliedDelta=MouseRate*Time.unscaledDeltaTime;
        InputState.Change(Keys,new KeyboardState(keys.ToArray()));
        InputState.Change(Mouse,new MouseState{delta=AppliedDelta,scroll=Scroll,position=new Vector2(Screen.width/2f,Screen.height/2f)}.WithButton(MouseButton.Left,Fire));
        Scroll=Vector2.zero;
    }
    void OnDestroy(){if(Keys!=null&&Keys.added)InputSystem.RemoveDevice(Keys);if(Mouse!=null&&Mouse.added)InputSystem.RemoveDevice(Mouse);InputSystem.settings.backgroundBehavior=oldBackground;
        foreach(var device in muted)if(device.added)InputSystem.EnableDevice(device);
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode=oldRouting;
#endif
    }
}

// Opt-in only, same retail executable and canonical drive/input/aim path.
[DefaultExecutionOrder(2000)]
public sealed class Camera275RuntimeQA : MonoBehaviour
{
    [Serializable]public sealed class Result
    {
        public string status="Running",vehicle,startedUtc,finishedUtc,method="Queued native input, actual R3/R6 physics and final rendered pose. Not human play or subjective comfort acceptance.";
        public int fps,width,height,frames,groundedFrames,ownRayHits,worldOverlaps,resets;
        public float distance,peakAngularStep,peakLinearStep;
        public List<string> phases=new List<string>(),errors=new List<string>();
    }
    Result result=new Result();string output;int phase,targetIndex,fixedIndex;float entered,deadline;
    R1RunController run;P12PipelineSafeCamera follow;Rigidbody body;Camera275RuntimeInput input;StreamWriter csv;
    Vector3 previousPosition,previousCamera;Quaternion previousRotation;bool bound,finished;
    readonly Vector3[] goals={new Vector3(0,0,-18),new Vector3(12,0,-18),new Vector3(12,0,-8),new Vector3(12,0,1),new Vector3(12,0,9)};
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Install()
    {
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-camera275-output");if(index<0||index+1>=args.Length)return;
        string Value(string key,string fallback){int n=Array.IndexOf(args,key);return n>=0&&n+1<args.Length?args[n+1]:fallback;}
        var root=new GameObject("CAM275 explicit native drive QA");DontDestroyOnLoad(root);
        var fixture=root.AddComponent<Camera275RuntimeQA>();fixture.output=args[index+1];fixture.result.vehicle=Value("-camera275-vehicle","Standard");fixture.result.fps=int.Parse(Value("-camera275-fps","60"));
    }
    void Start()
    {
        Directory.CreateDirectory(output);result.startedUtc=DateTime.UtcNow.ToString("o");result.width=Screen.width;result.height=Screen.height;
        Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=result.fps;
        input=gameObject.AddComponent<Camera275RuntimeInput>();deadline=Time.realtimeSinceStartup+150;
        csv=new StreamWriter(Path.Combine(output,"camera-drive.csv"));csv.WriteLine("frame,time,dt,fixed,phase,bodyX,bodyY,bodyZ,rootX,rootY,rootZ,rootQx,rootQy,rootQz,rootQw,speed,camX,camY,camZ,camQx,camQy,camQz,camQw,aimX,aimY,aimZ,boundsX,boundsY,boundsZ,extentX,extentY,extentZ,fit,clipped,overlap,selfHit,step,angleStep,mouseX,mouseY,orbitYaw,orbitPitch,zoom,resets,grounded");
        Application.logMessageReceived+=OnLog;Go(0,"waiting");
    }
    void OnLog(string text,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&result.errors.Count<30)result.errors.Add(text);}
    void Save()=>File.WriteAllText(Path.Combine(output,"camera-result.json"),JsonUtility.ToJson(result,true));
    void Go(int value,string label){phase=value;entered=Time.realtimeSinceStartup;result.phases.Add(label);Save();}
    void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    void Update()
    {
        if(finished)return;
        try
        {
            if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Drive fixture phase "+phase+" target "+targetIndex);
            if(phase==0)
            {
                if(R1RunController.Instance?.Progress==null)return;run=R1RunController.Instance;
                var kind=result.vehicle=="Light"?R3VehicleKind.Light:result.vehicle=="Heavy"?R3VehicleKind.Heavy:R3VehicleKind.Standard;
                Check(run.SelectVehicle(kind)&&run.OpenSandboxGarage(),"Garage preparation");Go(1,"garage");return;
            }
            if(phase==1)
            {
                if(Time.realtimeSinceStartup-entered<.3f)return;
                if(result.vehicle=="Tall")for(int y=2;y<=5;y++)Check(run.GarageBuy(new Vector3Int(1,y,0),R3PartKind.Armor),"Tall mount "+y);
                if(result.vehicle=="Wide")foreach(int sign in new[]{-1,1})for(int x=3;x<=6;x++)Check(run.GarageBuy(new Vector3Int(sign*x,1,0),R3PartKind.Armor),"Wide mount "+sign*x);
                Check(run.EnterTestRange(),"Range entry");Go(2,"stationary");return;
            }
            if(run.Transitioning)return;
            if(!bound)
            {
                body=run.Player.GetComponent<Rigidbody>();follow=FindFirstObjectByType<P12PipelineSafeCamera>();
                Check(!body.isKinematic&&body.interpolation==RigidbodyInterpolation.Interpolate,"Actual interpolated dynamic R6 vehicle");
                previousPosition=body.position;previousCamera=follow.transform.position;previousRotation=follow.transform.rotation;bound=true;
            }
            if(phase==2){if(Time.realtimeSinceStartup-entered>2)Go(3,"flat-acceleration");return;}
            if(phase==3)
            {
                Pilot(goals[targetIndex]);
                if(Vector3.ProjectOnPlane(goals[targetIndex]-body.position,Vector3.up).magnitude<1.8f)
                {
                    input.Throttle=input.Steering=0;
                    if(targetIndex==0){Go(4,"braking");return;}
                    targetIndex++;if(targetIndex>=goals.Length){Go(5,"orbit-and-max-zoom");return;}
                    result.phases.Add(targetIndex==1?"sharp-turn":targetIndex==2?"low-steps":targetIndex==3?"uneven-and-ramp":targetIndex==4?"ramp-exit":"turn");Save();
                }
                return;
            }
            if(phase==4){input.Throttle=-.5f;if(Time.realtimeSinceStartup-entered>1){targetIndex=1;Go(3,"sharp-turn");}return;}
            if(phase==5)
            {
                input.Throttle=input.Steering=0;input.MouseRate=new Vector2(300,0);
                // Scroll is supplied as normal native state in a separate targeted input test.
                typeof(P12PipelineSafeCamera).GetField("distance",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(follow,45f);
                if(Time.realtimeSinceStartup-entered>10){input.MouseRate=Vector2.zero;ScreenCapture.CaptureScreenshot(Path.Combine(output,"final-drive.png"));Go(6,"settled");}return;
            }
            if(phase==6&&Time.realtimeSinceStartup-entered>.4f)Finish();
        }
        catch(Exception error){result.errors.Add(error.ToString());Finish(false);}
    }
    void Pilot(Vector3 goal)
    {
        var direction=Vector3.ProjectOnPlane(goal-body.position,Vector3.up);float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(body.transform.forward,Vector3.up),direction,Vector3.up);
        float steer=angle-body.angularVelocity.y*Mathf.Rad2Deg*.22f;
        input.Steering=Mathf.Abs(steer)>7?Mathf.Sign(steer):0;input.Throttle=Mathf.Abs(angle)<48?1:0;
    }
    void FixedUpdate(){fixedIndex++;}
    static string N(float n)=>n.ToString("R",CultureInfo.InvariantCulture);
    void LateUpdate()
    {
        if(!bound||finished||run.Transitioning)return;
        var rig=follow.ReceiverRig;var p=follow.transform.position;var q=follow.transform.rotation;var root=body.transform;var r=root.position;var rq=root.rotation;
        var b=rig.LastHullBounds;var a=rig.LastSolution.AimWorld;var orbit=rig.RequestedOrbit;int grounded=body.GetComponent<R6VehicleDrive>().GroundedWheels;
        float step=Vector3.Distance(p,previousCamera),angle=Quaternion.Angle(q,previousRotation);
        result.frames++;if(grounded>0)result.groundedFrames++;if(rig.ActualOwnHullIntersectsCrosshair)result.ownRayHits++;if(rig.WorldOverlapAtCamera)result.worldOverlaps++;
        result.distance+=Vector3.Distance(body.position,previousPosition);result.peakAngularStep=Mathf.Max(result.peakAngularStep,angle);result.peakLinearStep=Mathf.Max(result.peakLinearStep,step);result.resets=rig.SmoothingResetCount;
        var values=new[]{Time.realtimeSinceStartup,Time.deltaTime,fixedIndex,phase,body.position.x,body.position.y,body.position.z,r.x,r.y,r.z,rq.x,rq.y,rq.z,rq.w,body.linearVelocity.magnitude,p.x,p.y,p.z,q.x,q.y,q.z,q.w,a.x,a.y,a.z,b.center.x,b.center.y,b.center.z,b.extents.x,b.extents.y,b.extents.z,rig.ReceiverFitDistance,rig.WorldClipped?1:0,rig.WorldOverlapAtCamera?1:0,rig.ActualOwnHullIntersectsCrosshair?1:0,step,angle,input.AppliedDelta.x,input.AppliedDelta.y,orbit.x,orbit.y,orbit.z,rig.SmoothingResetCount,grounded};
        csv.WriteLine(Time.frameCount+","+string.Join(",",values.Select(N)));previousPosition=body.position;previousCamera=p;previousRotation=q;
        if(result.frames%120==0){csv.Flush();Save();}
    }
    void Finish(bool success=true)
    {
        if(finished)return;finished=true;input.Throttle=input.Steering=0;input.MouseRate=Vector2.zero;
        if(result.ownRayHits>0)result.errors.Add("Actual own-hull reticle intersections: "+result.ownRayHits);
        if(result.worldOverlaps>0)result.errors.Add("Actual camera/world overlaps: "+result.worldOverlaps);
        if(result.distance<30||result.groundedFrames<result.frames/2)result.errors.Add("Insufficient real drive/contact evidence");
        result.finishedUtc=DateTime.UtcNow.ToString("o");result.status=success&&result.errors.Count==0?"Passed":"Failed";Save();csv?.Dispose();Application.logMessageReceived-=OnLog;
        Destroy(input);if(result.status=="Passed"&&run!=null)run.RequestQuit();else Application.Quit(1);
    }
}
#endif

#if UNITY_EDITOR || B01_UNIFIED_RC2_ACCEPTANCE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using B01.CAMERA001;
using RoboRougelike.MapSystemR4;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RogueMachine.P12PipelineSafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Dormant in the single retail EXE. All activation requires explicit diagnostic arguments.
[DefaultExecutionOrder(2000)]
public sealed class FullMergeRuntimeQA : MonoBehaviour
{
    [Serializable] public sealed class Result {
        public string status="Running",mode,vehicle,method="API activity selection/settlement; actual force-driven R3/R6 traversals (NO teleport/velocity writes); combat outcomes explicitly API-controlled after live AI/projectiles. Camera orbit injected, actual consumed aim recorded. NOT human or unassisted gameplay.",graphics,startedUtc,finishedUtc;
        public int width,height,frames,contacts,maxContacts,selfRayHits,worldOverlaps,shots,completed,detachedParts,enemyShots,maxAliveEnemies,saturatedFrames,offscreenHullFrames,continuousSpeedViolations;
        public float distance,peakAimError,settledAimError,peakCameraStep,peakFrameMs,meanFrameMs,maxEnemyMovement;
        public Vector3 finalPosition;
        public bool settledAimObserved;
        public List<string> checks=new List<string>(),errors=new List<string>();
    }
    Result result=new Result();string output,mode,vehicle;R1RunController run,oldRun;Rigidbody body;R6VehicleDrive drive;
    R3VehicleAssembly assembly;P12PipelineSafeCamera follow;readonly List<Vector3> path=new List<Vector3>();int target,phase,activity;
    float entered,deadline,lastTrace,lastKill;Vector3 previous;StreamWriter trace,aim;
    readonly HashSet<int> shots=new HashSet<int>();bool finished;Camera001AimTimeline timeline=new Camera001AimTimeline(65536);
    readonly HashSet<int> hostileShots=new HashSet<int>();readonly Dictionary<int,Vector3> enemyStarts=new Dictionary<int,Vector3>();bool detached;
    Keyboard keyboard;Mouse mouse;
    static readonly BindingFlags fields=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Field(object obj,string name,object value)=>obj.GetType().GetField(name,fields).SetValue(obj,value);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Install(){
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-fm270-output");if(i<0||i+1>=args.Length)return;
        string Value(string flag,string fallback){int n=Array.IndexOf(args,flag);return n>=0&&n+1<args.Length?args[n+1]:fallback;}
        var root=new GameObject("Full270 explicit native QA");DontDestroyOnLoad(root);root.AddComponent<FullMergeRuntimeQA>().Initialize(args[i+1],Value("-fm270-mode","Race"),Value("-fm270-vehicle","Light"));
    }
    void Initialize(string folder,string selectedMode,string selectedVehicle){
        output=folder;mode=result.mode=selectedMode;vehicle=result.vehicle=selectedVehicle;Directory.CreateDirectory(folder);
        result.startedUtc=DateTime.UtcNow.ToString("o");result.graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;
        Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;deadline=Time.realtimeSinceStartup+600;
        trace=new StreamWriter(Path.Combine(folder,"drive.csv"));trace.WriteLine("frame,time,activity,x,y,z,speed,grounded,target,state");
        aim=new StreamWriter(Path.Combine(folder,"aim-provenance.csv"));aim.WriteLine("frame,dt,hitId,hitName,targetX,targetY,targetZ,chassisX,chassisY,chassisZ,chassisW,localGunX,localGunY,localGunZ,localGunW,gunError");
        Application.logMessageReceived+=Log;Save();
        if(mode=="Camera"){InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();}
    }
    void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){if(result.errors.Count<30)result.errors.Add(text);}}
    void Save()=>File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));
    void Require(bool value,string text){if(!value)throw new InvalidOperationException(text);result.checks.Add(text);Save();}
    void Go(int next){phase=next;entered=Time.realtimeSinceStartup;Save();}
    void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
    bool Live=>run!=null&&!run.Transitioning&&run.Player!=null&&(run.Progress.State==R1RunState.Combat||run.Progress.State==R1RunState.TestDrive);
    void Bind(){body=run.Player.GetComponent<Rigidbody>();drive=body.GetComponent<R6VehicleDrive>();assembly=body.GetComponent<R3VehicleAssembly>();previous=body.position;
        run.Player.Controller.enabled=mode=="Camera";follow=FindFirstObjectByType<P12PipelineSafeCamera>();Require(follow.ReceiverRig!=null&&!follow.ReceiverRig.enabled,"One delegated camera owner");
        Require(FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader!=null&&m.shader.isSupported&&!m.shader.name.Contains("InternalError"))),"Active scene uses supported actual materials");}
    void SelectActivity(){
        path.Clear();target=0;
        string selected=mode=="Loop"?new[]{"Race","TimeAttack","Escape","Survival","Reactor","Ordinary"}[activity]:mode;
        bool ok=selected=="Race"?run.ChooseCheckpointRace():selected=="Reactor"?run.ChooseReactorDefense():selected=="Ordinary"?run.StartNextEncounter()&&run.ChooseRoute(0):run.ChooseRoadChallenge((MapR4Mode)Enum.Parse(typeof(MapR4Mode),selected));
        Require(ok,"Garage selects "+selected+": "+run.Garage.Message);Go(2);
    }
    void BuildPath(){
        if(run.CheckpointRace!=null){path.AddRange(run.GeneratedWorld.Checkpoints);if(path.Count==0||Vector3.Distance(path.Last(),run.GeneratedWorld.Finish)>.1f)path.Add(run.GeneratedWorld.Finish);return;}
        if(run.RoadChallenge==null||run.RoadChallenge.Mode==MapR4Mode.Survival||run.RoadChallenge.Mode==MapR4Mode.Boss)return;
        var ids=run.RoadChallenge.Mode==MapR4Mode.TimeAttack?new[]{"start_junction","junction_upper","upper_bridge","bridge_exit"}:new[]{"start_junction","junction_lower","lower_exit"};
        foreach(var id in ids){var mesh=run.RoadWorld.Edges[id].GetComponent<MeshFilter>().sharedMesh;var v=mesh.vertices;int count=v.Length/4;
            for(int i=1;i<count;i+=2)path.Add((v[2*i]+v[2*i+1])*.5f);path.Add((v[2*(count-1)]+v[2*(count-1)+1])*.5f);}
    }
    void Update(){
        if(output==null||finished)return;
        try{
            if(phase==99){if(Time.realtimeSinceStartup-entered>.6f){finished=true;result.finishedUtc=DateTime.UtcNow.ToString("o");Save();trace.Dispose();aim.Dispose();File.WriteAllText(Path.Combine(output,"aim-timeline.csv"),timeline.ToCsv());Application.Quit(result.status=="Passed"?0:1);}return;}
            if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Finite QA phase "+phase+" activity "+activity);
            if(phase==0){if(R1RunController.Instance?.Progress==null)return;run=R1RunController.Instance;result.width=Screen.width;result.height=Screen.height;
                Require(run.SelectVehicle(vehicle=="Heavy"?R3VehicleKind.Heavy:R3VehicleKind.Light),"Vehicle selected");
                run.RequestedSeed=7;Require(mode=="Camera"?run.OpenSandboxGarage():run.PrepareUnifiedRun(),"Actual preparation garage");Capture("00-garage");Go(1);}
            else if(phase==1){if(Time.realtimeSinceStartup-entered<.4f)return;
                if(mode=="Camera"){
                    if(vehicle=="Tall")for(int y=1;y<=5;y++)Require(run.GarageBuy(new Vector3Int(1,y,0),R3PartKind.Armor),"Tall real structural part "+y);
                    if(vehicle=="Wide")foreach(int side in new[]{-1,1})for(int x=1;x<=6;x++)Require(run.GarageBuy(new Vector3Int(side*x,1,0),R3PartKind.Armor),"Wide real structural part "+(side*x));
                    Require(run.EnterTestRange(),"Same garage enters actual test range");Go(2);
                }else SelectActivity();}
            else if(phase==2){if(run.Transitioning)return;Require(Live,"Activity entered canonical live state");Bind();BuildPath();lastKill=Time.time;Capture("01-activity-"+activity);Go(3);}
            else if(phase==3){
                if(run.Progress.State==R1RunState.Lost)throw new InvalidOperationException("Actual activity lost: "+run.Progress.DefeatReason);
                if(mode=="Camera"){
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Time.realtimeSinceStartup-entered<3?new[]{Key.W}:Array.Empty<Key>()));
                    InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));
                    if(!detached&&Time.realtimeSinceStartup-entered>8){
                        var part=assembly.Parts.Where(p=>p!=null&&p.Operational&&p.Kind==R3PartKind.Armor).OrderByDescending(p=>Mathf.Abs(p.Cell.x)+p.Cell.y).First();
                        part.Damageable.ApplyDamage(100000,part.transform.position,Vector3.zero);detached=true;result.detachedParts++;Capture("02-explicit-part-detachment");
                    }
                    if(Time.realtimeSinceStartup-entered>16.3f){Require(result.selfRayHits==0,"Zero actual own-hull reticle intersections");Require(result.shots>0,"Real owned physical shots observed");Require(result.distance>2,"Actual R6 movement while aiming");Require(result.continuousSpeedViolations==0,"Continuous camera motion respects its declared limit");Require(result.settledAimObserved&&result.settledAimError<=.05f,"Settled muzzle aligns after orbit stops; all transient peaks retained");Capture("03-camera-final");Finish();}return;}
                if(run.Progress.State==R1RunState.Salvage){Require(run.VehicleModel==assembly.Model,"One canonical model through result");result.completed++;Capture("02-reward-"+activity);Go(4);return;}
                if(run.Encounter.AliveCount>0&&Time.time-lastKill>3){
                    // Explicit controlled settlement, distinct from genuine force driving and native physical-gun test.
                    foreach(var enemy in run.Encounter.Enemies.Where(e=>e!=null&&!e.IsDefeated).ToArray())enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.zero);
                    lastKill=Time.time;
                }
            }
            else if(phase==4){
                var ids=run.Salvage.Items.Where(p=>p.Selectable).Take(3).Select(p=>p.Id).ToArray();Require(run.ConfirmRewards(ids,new[]{0}),"Canonical bonus and up-to-three salvage once");
                if(run.Progress.State==R1RunState.Won){Require(mode=="Loop"&&run.Progress.CompletedEncounters==6,"Full six-stage finale");oldRun=run;run.RestartRun();Go(6);return;}
                Require(run.Progress.State==R1RunState.Garage,"Result returns to same garage");run.GarageRepairAffordable();if(!run.VehicleModel.CanDeploy)run.GarageEmergencyRepair();
                Require(run.VehicleModel.CanDeploy,"Actual repair preserves deployment");Capture("03-garage-"+activity);
                if(mode=="Loop"&&activity<5){activity++;SelectActivity();}else {oldRun=run;run.RestartRun();Go(6);}
            }
            else if(phase==6){if(R1RunController.Instance==oldRun||R1RunController.Instance?.Progress==null)return;run=R1RunController.Instance;
                Require(run.Progress.State==R1RunState.Ready&&run.CheckpointRace==null&&run.RoadWorld==null&&run.GeneratedWorld==null,"Restart removes all activity objects");Capture("04-restart");Finish();}
        }catch(Exception e){result.errors.Add(e.ToString());result.status="Failed";Capture("FAIL");Go(99);}
    }
    void Finish(){result.status=result.errors.Count==0?"Passed":"Failed";Go(99);}
    void FixedUpdate(){
        if(phase!=3||!Live||body==null)return;
        Vector3 goal=path.Count>0?path[Mathf.Min(target,path.Count-1)]:body.position+body.transform.forward*10;
        if(path.Count>0){while(target<path.Count-1&&Vector2.Distance(new Vector2(body.position.x,body.position.z),new Vector2(path[target].x,path[target].z))<2.2f)target++;goal=path[target];}
        var to=goal-body.position;to.y=0;float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(body.transform.forward,Vector3.up),to,Vector3.up);
        float throttle=path.Count>0?(to.magnitude>1.8f&&Mathf.Abs(angle)<65?1:0):mode=="Camera"?(Time.realtimeSinceStartup-entered<3?1:0):0;
        float forwardLimit=run.Player.Controller.EffectiveForwardSpeed;
        if(run.RoadWorld!=null&&path.Count>0){
            // The initial pilot accelerated Heavy through a sharp curve and drove
            // OFF the road. Follow source centres slowly, braking with actual R6
            // input rather than widening roads or moving the body into place.
            forwardLimit=3;
            if(drive.ForwardSpeed>3.2f)throttle=-1;
            if(Mathf.Abs(angle)>35&&drive.ForwardSpeed>1)throttle=-1;
        }
        if(mode!="Camera")drive.Step(throttle,Mathf.Clamp(angle/35-body.angularVelocity.y*.2f,-1,1),forwardLimit,run.Player.Controller.EffectiveReverseSpeed,run.Player.Controller.EffectiveTurnSpeed);
        body.GetComponent<MapR6ProductionEvidenceBridge>()?.SetExternalDemand01(Mathf.Abs(throttle));
        result.distance+=Vector3.Distance(previous,body.position);previous=result.finalPosition=body.position;result.maxContacts=Mathf.Max(result.maxContacts,drive.GroundedWheels);if(drive.GroundedWheels>0)result.contacts++;
        if(body.position.y< -4){result.errors.Add("Real vehicle fell below road: "+body.position);result.status="Failed";Go(99);}
        if(Time.time-lastTrace>.1f){lastTrace=Time.time;trace.WriteLine(FormattableString.Invariant($"{Time.frameCount},{Time.time:F3},{activity},{body.position.x:F3},{body.position.y:F3},{body.position.z:F3},{body.linearVelocity.magnitude:F3},{drive.GroundedWheels},{target},{run.Progress.State}"));trace.Flush();}
    }
    void LateUpdate(){
        if(phase!=3||!Live||body==null)return;
        try{
            var rig=follow.ReceiverRig;result.frames++;float ms=Time.unscaledDeltaTime*1000;result.meanFrameMs+=(ms-result.meanFrameMs)/result.frames;result.peakFrameMs=Mathf.Max(result.peakFrameMs,ms);
            result.peakCameraStep=Mathf.Max(result.peakCameraStep,rig.LastCameraStepMeters);if(rig.ActualOwnHullIntersectsCrosshair)result.selfRayHits++;if(rig.WorldOverlapAtCamera)result.worldOverlaps++;
            if(rig.ReceiverFramingSaturated)result.saturatedFrames++;
            if(rig.LastCameraStepMeters>rig.EffectiveContinuousSpeedLimit*Time.deltaTime+.001f)result.continuousSpeedViolations++;
            var hull=Camera001Rig.GetHullBounds(body.transform);bool cropped=false;
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1}){
                var p=Camera.main.WorldToViewportPoint(hull.center+Vector3.Scale(hull.extents,new Vector3(x,y,z)));
                if(p.z<=0||p.x<0||p.x>1||p.y<0||p.y>1)cropped=true;
            }
            if(cropped)result.offscreenHullFrames++;
            if(mode=="Camera"){
                // Orbit inputs are injected into the real facade; source rig still has one automatic writer.
                float t=Mathf.Min(15,Time.realtimeSinceStartup-entered);Field(follow,"yaw",t*24);Field(follow,"pitch",18+20*(1+Mathf.Sin(t*.6f)));Field(follow,"distance",Mathf.Lerp(3,18,(1+Mathf.Sin(t*.5f))*.5f));
            }
            foreach(var shot in FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None))if(shot.IsActiveProjectile&&shot.OwnerRoot==body.transform&&shots.Add(shot.GetInstanceID()))result.shots++;
            foreach(var shot in FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None))if(shot.IsActiveProjectile&&shot.OwnerRoot!=body.transform&&hostileShots.Add(shot.GetInstanceID()))result.enemyShots++;
            result.maxAliveEnemies=Mathf.Max(result.maxAliveEnemies,run.Encounter.AliveCount);
            foreach(var enemy in run.Encounter.Enemies.Where(e=>e!=null&&!e.IsDefeated)){
                int id=enemy.GetInstanceID();if(!enemyStarts.TryGetValue(id,out var start))enemyStarts[id]=start=enemy.transform.position;
                result.maxEnemyMovement=Mathf.Max(result.maxEnemyMovement,Vector3.Distance(start,enemy.transform.position));
            }
            var gun=assembly.Parts.FirstOrDefault(p=>p!=null&&p.Operational&&p.Muzzle!=null);var controller=run.Player.Controller;
            if(gun!=null&&controller.LastConsumedAimFrame==Time.frameCount&&Time.deltaTime>0){
                float error=Vector3.Angle(gun.Muzzle.forward,controller.LastConsumedAimPoint-gun.Muzzle.position);result.peakAimError=Mathf.Max(result.peakAimError,error);
                if(mode=="Camera"&&Time.realtimeSinceStartup-entered>=16.2f){result.settledAimError=error;result.settledAimObserved=true;}
                var hit=controller.LastConsumedAimCollider;var q=body.rotation;var g=gun.GunPivot.localRotation;var p=controller.LastConsumedAimPoint;
                aim.WriteLine(FormattableString.Invariant($"{Time.frameCount},{Time.deltaTime:R},{(hit!=null?hit.GetInstanceID():0)},{(hit!=null?hit.name.Replace(',','_'):"no_hit")},{p.x:R},{p.y:R},{p.z:R},{q.x:R},{q.y:R},{q.z:R},{q.w:R},{g.x:R},{g.y:R},{g.z:R},{g.w:R},{error:R}"));
                timeline.Capture(new Camera001AimTimeline.Frame{FrameIndex=Time.frameCount,HitColliderInstanceId=hit!=null?hit.GetInstanceID():0,HitKind=hit!=null?hit.name:"no_hit",Sample=new Camera001AimTransitionDiagnostics.Sample{CurrentTargetWorld=p,CurrentMuzzleWorld=gun.Muzzle.position,CurrentMuzzleForwardWorld=gun.Muzzle.forward,DeltaTimeSeconds=Time.deltaTime,TurretSlewDegreesPerSecond=170,CameraFinalAimErrorDegrees=rig.LastAimErrorDegrees,CameraOwnHullRayHit=rig.ActualOwnHullIntersectsCrosshair,CameraWorldOverlap=rig.WorldOverlapAtCamera}});
            }
            if(Time.frameCount%120==0){Save();aim.Flush();if(mode=="Camera")Capture("camera-"+Time.frameCount);}
        }catch(Exception e){result.errors.Add(e.ToString());result.status="Failed";Go(99);}
    }
    void OnDestroy(){Application.logMessageReceived-=Log;trace?.Dispose();aim?.Dispose();if(output!=null)File.WriteAllText(Path.Combine(output,"aim-timeline.csv"),timeline.ToCsv());}
}
#endif

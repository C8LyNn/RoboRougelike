using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using B01.MAPGEN001;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;

// Dedicated QA scene only. No RuntimeInitialize hook, production scene or source generator changes.
public sealed class MapgenR341WheelsQA : MonoBehaviour
{
    public Material VehicleTemplate;
    [Serializable] public sealed class Trial
    {
        public string id,group,recipe,vehicle,status="Running",detail="",geometryHash,sourceShader,destinationKind;
        public int seed,segments,density,startingWheels,remainingWheels,partsBefore,partsAfter,goal,totalGoals;
        public int samples,groundedSamples,minimumContacts=999,maximumContacts,seamTransitions,nonFloorCollisionEvents,rampCollisionEvents,recoveryAttempts;
        public float difficulty,laneWidth,mass,distance,netTravel,seconds,maxSpeed,maxTilt,longestNoContact,longestStall;
        public Vector3 centerOfMass,colliderEnvelope,spawn,settledSpawn,lastPosition;
        public float nominalWidth,nominalLength,usedWidth,usedLength,spawnDrop;
        public int width,height;public bool reachedFinish,probeEndReached,fallen,spawnBlocked,flipped,renderOverride;
        public Vector3 destination;public float selectedRampWidth,selectedRampPitch;
        public List<string> collisions=new List<string>();public List<string> screenshots=new List<string>();
    }
    [Serializable] public sealed class Report
    {
        public string source="5be31039411f89f17654b1c332f8692883fa03cc",vehicleSource="4c9236a4cdc27814ea2e7679872ec4a524150cc8";
        public string method="Windows PhysX real R3 catalog/assembly/R6WheelContacts/R6VehicleDrive.Step; bounded API steering, NOT human or keyboard play. No travel teleports or replacement physics.";
        public string status="Running",startedUtc,finishedUtc,graphics,renderPipeline;public bool diagnosticMaterialOverride,rampProbe;
        public List<Trial> trials=new List<Trial>();public List<string> errors=new List<string>();
    }
    readonly Report report=new Report();readonly Queue<Trial> pending=new Queue<Trial>();
    readonly List<Vector3> goals=new List<Vector3>();
    MapGenBuilder builder;R3VisualPalette palette;R1VehicleState actor;R3VehicleAssembly assembly;Rigidbody body;R6VehicleDrive drive;Camera view;
    Trial row;MapResult map;string output;StreamWriter trace;float readyAt,startedAt,lastSample,stillSince,noContactSince=-1,maxProgressAt;
    float bestGoalDistance=float.PositiveInfinity;Vector3 previous,initial;string lastFloor;bool finishing,ending,rampProbe,renderOverride;float cleanupAt;int limit=99;
    static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
    void Start()
    {
        var args=Environment.GetCommandLineArgs();int k=Array.IndexOf(args,"-mapgen-wheel-output");
        if(k<0||k+1>=args.Length){enabled=false;return;}output=args[k+1];Directory.CreateDirectory(output);
        k=Array.IndexOf(args,"-mapgen-case-limit");if(k>=0&&k+1<args.Length)int.TryParse(args[k+1],out limit);
        rampProbe=Array.IndexOf(args,"-mapgen-ramp-probe")>=0;renderOverride=Array.IndexOf(args,"-mapgen-material-override")>=0;
        report.rampProbe=rampProbe;report.diagnosticMaterialOverride=renderOverride;
        report.renderPipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.GetType().FullName??"builtin";
        if(rampProbe)report.method="Supplemental real R3/R6 optional ramp probe, API controlled. Destination is RAMP EXIT, not complete course finish. Test-only URP material override does not change colliders.";
        Application.runInBackground=true;Application.targetFrameRate=60;Time.timeScale=1;
        report.startedUtc=DateTime.UtcNow.ToString("o");report.graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;
        Application.logMessageReceived+=OnLog;
        builder=new GameObject("QA generated map owner").AddComponent<MapGenBuilder>();
        palette=new GameObject("QA original R3 palette").AddComponent<R3VisualPalette>();palette.Initialize(VehicleTemplate);
        view=Camera.main;
        int[] seeds=Array.IndexOf(args,"-mapgen-remaining-only")>=0?new[]{31,83}:new[]{7,31,83};
        foreach(int seed in seeds)foreach(var recipe in new[]{MapRecipe.CombatArena,MapRecipe.ObstacleCourse})
        foreach(var kind in new[]{R3VehicleKind.Light,R3VehicleKind.Heavy})pending.Enqueue(New("BASE",recipe,seed,kind,.5f));
        if(rampProbe){pending.Clear();foreach(var kind in new[]{R3VehicleKind.Light,R3VehicleKind.Heavy})pending.Enqueue(New("RAMP_PROBE",MapRecipe.ObstacleCourse,7,kind,.5f));}
        trace=new StreamWriter(Path.Combine(output,"wheel-trace.csv"));
        trace.WriteLine("case,seconds,x,y,z,speed,contacts,wheels,tilt,goal");Save();readyAt=Time.realtimeSinceStartup+.5f;
    }
    static Trial New(string group,MapRecipe recipe,int seed,R3VehicleKind kind,float difficulty)=>new Trial{
        id=group+"-"+recipe+"-"+kind+"-"+seed,group=group,recipe=recipe.ToString(),seed=seed,vehicle=kind.ToString(),difficulty=difficulty,segments=4,density=50,laneWidth=16};
    void OnLog(string text,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&report.errors.Count<30){report.errors.Add(text);Save();}}
    void Save(){if(output!=null)File.WriteAllText(Path.Combine(output,"wheel-trials.json"),JsonUtility.ToJson(report,true));trace?.Flush();}
    void Begin()
    {
        if(pending.Count==0||report.trials.Count>=limit){Complete();return;}
        row=pending.Dequeue();report.trials.Add(row);startedAt=Time.time;
        try
        {
            var kind=(R3VehicleKind)Enum.Parse(typeof(R3VehicleKind),row.vehicle);
            var recipe=(MapRecipe)Enum.Parse(typeof(MapRecipe),row.recipe);
            var cfg=MapGenConfig.Default;cfg.Difficulty=row.difficulty;
            var profile=kind==R3VehicleKind.Heavy?MapVehicleProfile.Heavy:MapVehicleProfile.Light;
            var nominal=MapGenPlanner.Plan(recipe,row.seed,cfg,profile);
            row.nominalWidth=profile.Width;row.nominalLength=profile.Length;
            var ramp=nominal.Elements.FirstOrDefault(e=>e.Kind==MapElementKind.Ramp);
            Vector3 spawn=nominal.Start;Quaternion pose=Quaternion.identity;
            if(rampProbe){spawn=new Vector3(ramp.Position.x,nominal.Start.y,ramp.Position.z+ramp.Size.z*.5f+profile.Length*.5f+1);pose=Quaternion.Euler(0,180,0);}
            actor=R3VehicleAssembly.Build(new R3VehicleModel(R3VehicleCatalog.Create(kind)),new R1RunStats(),palette,spawn,pose);
            actor.name="REAL_R3_R6_"+row.id;
            actor.Controller.SetGameplayInputEnabled(true);actor.Controller.enabled=false;
            assembly=actor.GetComponent<R3VehicleAssembly>();body=actor.GetComponent<Rigidbody>();drive=actor.GetComponent<R6VehicleDrive>();
            // R6 Awake owns the real gravity/contact/material settings; do not flatten or constrain the body.
            Physics.SyncTransforms();drive.RefreshParts();
            var cols=actor.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&c.attachedRigidbody==body).ToArray();
            var bounds=cols[0].bounds;foreach(var c in cols)bounds.Encapsulate(c.bounds);
            row.colliderEnvelope=bounds.size;profile.Width=Mathf.Max(profile.Width,bounds.size.x);profile.Length=Mathf.Max(profile.Length,bounds.size.z);
            row.usedWidth=profile.Width;row.usedLength=profile.Length;
            map=builder.Generate(recipe,row.seed,cfg,profile);row.geometryHash=map.GeometryHash;
            var renderers=map.GeneratedRoot.GetComponentsInChildren<Renderer>();row.sourceShader=renderers.FirstOrDefault()?.sharedMaterial?.shader?.name??"none";
            row.renderOverride=renderOverride;if(renderOverride)foreach(var r in renderers)r.sharedMaterial=VehicleTemplate;
            row.spawn=body.position;row.mass=body.mass;row.centerOfMass=body.centerOfMass;
            row.partsBefore=assembly.Parts.Count;row.startingWheels=drive.Wheels;
            actor.gameObject.AddComponent<MapgenR341CollisionProbe>().Owner=this;
            goals.Clear();
            if(rampProbe)
            {
                row.selectedRampWidth=ramp.Size.x;row.selectedRampPitch=ramp.Rotation.eulerAngles.x;
                goals.Add(new Vector3(ramp.Position.x,map.Start.y,ramp.Position.z-ramp.Size.z*.5f-profile.Length*.5f-1));
                // Test query only: do not move/teleport colliders to hide an invalid ramp-side approach.
                foreach(var own in cols)foreach(var world in map.GeneratedRoot.GetComponentsInChildren<Collider>())
                    if(world.name.StartsWith("Boundary")&&Physics.ComputePenetration(own,own.transform.position,own.transform.rotation,world,world.transform.position,world.transform.rotation,out _,out float depth)&&depth>.01f)row.spawnBlocked=true;
            }
            else{goals.AddRange(map.CheckpointDescriptors);goals.Add(map.Finish);}
            row.totalGoals=goals.Count;row.destination=goals.Last();row.destinationKind=rampProbe?"RampExit":"GeneratedFinish";
            previous=initial=body.position;row.lastPosition=body.position;startedAt=Time.time;lastSample=Time.time;stillSince=Time.time;maxProgressAt=Time.time;
            bestGoalDistance=float.PositiveInfinity;lastFloor=null;noContactSince=-1;
            Screen.SetResolution(kind==R3VehicleKind.Heavy?1920:1280,kind==R3VehicleKind.Heavy?1080:720,false);
            readyAt=Time.realtimeSinceStartup+1.2f;Save();
        }
        catch(Exception ex){End("FAIL_SETUP",ex.GetType().Name+": "+ex.Message);}
    }
    void Update()
    {
        if(output==null||finishing)return;
        if(ending){if(Time.realtimeSinceStartup>=cleanupAt)CleanupTrial();return;}
        if(row==null){if(Time.realtimeSinceStartup>=readyAt)Begin();return;}
        if(actor==null||body==null)return;
        // One dedicated view camera; neither production P12 follow nor a second driver is installed.
        var size=row.colliderEnvelope;var focus=body.position+Vector3.up*Mathf.Max(1,size.y*.5f);
        view.transform.position=body.position+new Vector3(0,Mathf.Max(7,size.y+5),-10);
        view.transform.rotation=Quaternion.LookRotation(focus-view.transform.position);
        if(Time.realtimeSinceStartup<readyAt)return;
        if(row.settledSpawn==Vector3.zero)
        {
            row.settledSpawn=body.position;row.spawnDrop=row.spawn.y-body.position.y;row.width=Screen.width;row.height=Screen.height;
            Capture("spawn");
        }
    }
    void FixedUpdate()
    {
        if(row==null||actor==null||body==null||finishing||ending||Time.realtimeSinceStartup<readyAt)return;
        try
        {
            row.seconds=Time.time-startedAt;
            if(row.spawnBlocked){End("FAIL_PROBE_SPAWN","Real collider overlap with side boundary at optional ramp approach");return;}
            if(body.position.y< -3){row.fallen=true;End("FAIL_FELL","Body fell below generated floor");return;}
            if(actor.DefeatReason!=null){End("FAIL_VEHICLE",actor.DefeatReason);return;}
            if(row.seconds>65){End("FAIL_TIMEOUT","65s simulation deadline");return;}
            var to=goals[row.goal]-body.position;to.y=0;
            if(to.magnitude<2.4f)
            {
                row.goal++;bestGoalDistance=float.PositiveInfinity;maxProgressAt=Time.time;
                if(row.goal==goals.Count){row.reachedFinish=!rampProbe;row.probeEndReached=rampProbe;End("PASS",rampProbe?"Optional ramp exit reached using R6 force drive":"Finish reached using R6 contact-limited force drive");return;}
                to=goals[row.goal]-body.position;to.y=0;
            }
            if(to.magnitude<bestGoalDistance-.15f){bestGoalDistance=to.magnitude;maxProgressAt=Time.time;}
            float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(body.transform.forward,Vector3.up),to,Vector3.up);
            float steer=Mathf.Clamp(angle/35f-body.angularVelocity.y*.2f,-1,1);
            float throttle=Mathf.Abs(angle)>72?0:1;
            float limitSpeed=Mathf.Min(actor.Controller.EffectiveForwardSpeed,Mathf.Max(1.6f,to.magnitude*.8f));
            drive.Step(throttle,steer,limitSpeed,3,90);
            var position=body.position;float speed=body.linearVelocity.magnitude;
            row.distance+=Vector3.Distance(previous,position);previous=position;row.lastPosition=position;
            if(row.distance>10&&row.screenshots.Count==1)Capture("driving");
            row.maxSpeed=Mathf.Max(row.maxSpeed,speed);row.netTravel=Vector3.Distance(initial,position);
            float tilt=Vector3.Angle(body.transform.up,Vector3.up);row.maxTilt=Mathf.Max(row.maxTilt,tilt);row.flipped|=tilt>90;
            row.samples++;if(drive.GroundedWheels>0)row.groundedSamples++;
            row.minimumContacts=Mathf.Min(row.minimumContacts,drive.GroundedWheels);row.maximumContacts=Mathf.Max(row.maximumContacts,drive.GroundedWheels);
            if(drive.GroundedWheels==0){if(noContactSince<0)noContactSince=0;noContactSince+=Time.fixedDeltaTime;row.longestNoContact=Mathf.Max(row.longestNoContact,noContactSince);}else noContactSince=-1;
            if(speed>.15f||Mathf.Abs(body.angularVelocity.y)>.15f)stillSince=Time.time;
            row.longestStall=Mathf.Max(row.longestStall,Time.time-stillSince);
            if(Time.time-maxProgressAt>12&&Time.time-stillSince>5){End("FAIL_STALLED","No waypoint progress for12s and static for5s");return;}
            if(Time.time-lastSample>=.1f)
            {
                lastSample=Time.time;
                string floor=GroundName();if(lastFloor!=null&&floor!=null&&floor!=lastFloor)row.seamTransitions++;if(floor!=null)lastFloor=floor;
                trace.WriteLine(string.Join(",",row.id,row.seconds.ToString("F3",Inv),position.x.ToString("F3",Inv),position.y.ToString("F3",Inv),position.z.ToString("F3",Inv),speed.ToString("F3",Inv),drive.GroundedWheels,drive.Wheels,tilt.ToString("F2",Inv),row.goal));
            }
        }
        catch(Exception ex){End("FAIL_EXCEPTION",ex.ToString());}
    }
    string GroundName()
    {
        foreach(var hit in Physics.RaycastAll(body.position+Vector3.up*3,Vector3.down,8).OrderBy(h=>h.distance))
            if(hit.collider.transform.IsChildOf(map.GeneratedRoot.transform)&&(hit.collider.name.StartsWith("Floor")||hit.collider.name.StartsWith("Ramp")))return hit.collider.name;
        return null;
    }
    public void Collision(Collision collision)
    {
        if(row==null||map?.GeneratedRoot==null||!collision.collider.transform.IsChildOf(map.GeneratedRoot.transform))return;
        string name=collision.collider.name;if(name.StartsWith("Floor"))return;
        row.nonFloorCollisionEvents++;if(name.StartsWith("Ramp"))row.rampCollisionEvents++;
        if(!row.collisions.Contains(name))row.collisions.Add(name);
    }
    void Capture(string stage)
    {
        if(row==null)return;string name=row.id+"-"+stage+".png";ScreenCapture.CaptureScreenshot(Path.Combine(output,name));row.screenshots.Add(name);
    }
    void End(string status,string detail)
    {
        if(row==null||ending)return;
        if(actor!=null)
        {
            row.remainingWheels=drive.Wheels;row.partsAfter=assembly.Parts.Count(p=>p!=null&&p.Operational&&p.transform.IsChildOf(actor.transform));
            row.recoveryAttempts=actor.GetComponent<R6CoreRecovery>()?.AttemptsStarted??0;
            row.lastPosition=body.position;row.seconds=Time.time-startedAt;
            if(status=="PASS"&&(row.partsAfter<row.partsBefore||row.flipped))status="FINISH_WITH_DAMAGE_OR_FLIP";
            Capture("end");
        }
        row.status=status;row.detail=detail;Save();ending=true;cleanupAt=Time.realtimeSinceStartup+.2f;
    }
    void CleanupTrial()
    {
        if(actor!=null){actor.gameObject.SetActive(false);Destroy(actor.gameObject);}
        row=null;actor=null;body=null;builder.Clear();map=null;ending=false;readyAt=Time.realtimeSinceStartup+.5f;
    }
    void Complete()
    {
        report.finishedUtc=DateTime.UtcNow.ToString("o");report.status=report.trials.Count>0&&report.errors.Count==0&&report.trials.All(t=>t.status=="PASS")?"PASS":"FAIL";Save();trace?.Dispose();trace=null;
        finishing=true;Invoke(nameof(Quit),.5f);
    }
    void Quit()=>Application.Quit(report.status=="PASS"?0:2);
    void OnGUI()
    {
        if(output==null)return;
        GUI.Box(new Rect(12,12,810,110),"MAPGEN R3.4.1 • REAL R3/R6 PHYSX • QA ONLY / API DRIVER");
        if(renderOverride)GUI.Label(new Rect(24,90,780,25),"TEST-ONLY URP MATERIAL OVERRIDE • ORIGINAL RENDERING IS NOT CERTIFIED");
        GUI.Label(new Rect(24,40,780,25),row?.id??("Preparing / "+report.trials.Count+" rows"));
        if(row!=null)GUI.Label(new Rect(24,65,780,45),$"Goal {row.goal}/{row.totalGoals} • {row.seconds:F1}s • {row.distance:F1}m • contacts {drive?.GroundedWheels}/{drive?.Wheels} • tilt {row.maxTilt:F1}°");
    }
    void OnDestroy(){Application.logMessageReceived-=OnLog;trace?.Dispose();}
}
public sealed class MapgenR341CollisionProbe : MonoBehaviour
{
    public MapgenR341WheelsQA Owner;
    void OnCollisionEnter(Collision collision)=>Owner?.Collision(collision);
}

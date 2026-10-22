#if MOVSERIES_VISUAL_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R4
{
    // QA executable only. Combat pilot uses ordinary input/physical rounds and active AI.
    // Menus/rewards/garage require the operator. No damage, reward or currency API shortcuts.
    [DefaultExecutionOrder(2000)]
    public sealed class RRLW2RuntimeQA : MonoBehaviour
    {
        [Serializable] public sealed class Observation
        {
            public float realtime;public string state;public int completed,alive,scrap,inventory,recovered,bonuses;
            public float damage,hull,core,playerHp,playerMaxHp;public string inventoryConditions;
        }
        [Serializable] public sealed class DebrisObservation
        {
            public int id;public float mass,firstTime,maxDistance;public bool initialNonblocking,restoredColliders,restoredGravity;
            public Vector3 origin;
        }
        [Serializable] public sealed class Result
        {
            public string runId,startedUtc,finishedUtc,status="Running",method="Native UI operator + combat input pilot; no direct damage/reward/currency APIs",graphics;
            public int width,height,physicalRounds,peakShells,peakDebris;public bool won;public float combatSeconds;public int combatFrames;
            public List<Observation> observations=new List<Observation>();
            public List<DebrisObservation> debris=new List<DebrisObservation>();
            public List<string> screenshots=new List<string>();public List<string> errors=new List<string>();
        }
        Result result=new Result();string output,lastKey;float captureAt=-1;string captureName;
        readonly HashSet<int> seenShells=new HashSet<int>();
        int debrisSeries;
        Mouse mouse;Keyboard keyboard;P12PipelineSafeCamera follow;P12PipelineSafeEnemyTurret target;Vector3 previous,velocity;
        readonly Dictionary<int,DebrisObservation> traces=new Dictionary<int,DebrisObservation>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rrl-w2-qa-output");
            if(i<0||i+1>=args.Length)return;
            var root=new GameObject("RRL-W2 operator QA observer");DontDestroyOnLoad(root);
            root.AddComponent<RRLW2RuntimeQA>().Initialize(args[i+1]);
        }
        void Initialize(string directory)
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rrl-w2-run-id");
            if(i<0||i+1>=args.Length)throw new ArgumentException("QA run ID required");
            output=directory;Directory.CreateDirectory(output);result.runId=args[i+1];result.startedUtc=DateTime.UtcNow.ToString("o");
            result.graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;result.width=Screen.width;result.height=Screen.height;
            Application.runInBackground=true;Application.targetFrameRate=60;
            Application.logMessageReceived+=OnLog;Save();
        }
        void OnLog(string message,string stack,LogType kind)
        {if((kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)&&result.errors.Count<40){result.errors.Add(message);Save();}}
        void Save(){if(output!=null)File.WriteAllText(Path.Combine(output,"operator-qa-result.json"),JsonUtility.ToJson(result,true));}
        void Observe(R1RunController run)
        {
            if(run.Transitioning)return;
            string inv=run.VehicleModel==null?"":string.Join(";",run.VehicleModel.Inventory.Select(p=>p.Kind+":"+p.Health.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture)));
            string key=run.Progress.State+"/"+run.Progress.CompletedEncounters+"/"+(run.Encounter?.AliveCount??0)+"/"+run.VehicleModel?.Scrap+"/"+run.VehicleModel?.Inventory.Count+"/"+run.Progress.Acquired.Count+"/"+inv;
            if(key==lastKey)return;lastKey=key;
            var o=new Observation{realtime=Time.unscaledTime,state=run.Progress.State.ToString(),completed=run.Progress.CompletedEncounters,
                alive=run.Encounter?.AliveCount??0,scrap=run.VehicleModel?.Scrap??0,inventory=run.VehicleModel?.Inventory.Count??0,recovered=run.RecoveredModules,
                bonuses=run.Progress.Acquired.Count,damage=run.Progress.Stats.Damage,hull=run.Progress.Stats.Hull,core=run.Progress.Stats.Core,inventoryConditions=inv,
                playerHp=run.Player!=null&&run.Player.Core!=null?run.Player.Core.CurrentHp:0,playerMaxHp=run.Player!=null&&run.Player.Core!=null?run.Player.Core.MaxHp:0};
            result.observations.Add(o);result.won|=run.Progress.State==R1RunState.Won;
            captureName=result.observations.Count.ToString("D3")+"-"+o.state+"-stage"+o.completed;
            captureAt=Time.unscaledTime+.35f;Save();
        }
        void StartPilot()
        {
            if(mouse!=null)return;
            mouse=InputSystem.AddDevice<Mouse>();keyboard=InputSystem.AddDevice<Keyboard>();
            follow=FindFirstObjectByType<P12PipelineSafeCamera>();if(follow!=null)follow.enabled=false;
            target=null;velocity=Vector3.zero;
        }
        void StopPilot(R1RunController run)
        {
            if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            mouse=null;keyboard=null;target=null;
            // Garage owns its camera; never re-enable follow over its framed viewport.
            if(follow!=null&&run.Progress.State!=R1RunState.Garage)follow.enabled=true;
        }
        void Pilot(R1RunController run)
        {
            StartPilot();
            if(target==null||target.IsDefeated)
            {
                target=run.Encounter.Enemies.Where(e=>e!=null&&!e.IsDefeated).OrderBy(e=>Vector3.Distance(e.transform.position,run.Player.transform.position)).FirstOrDefault();
                if(target!=null)previous=target.transform.position;velocity=Vector3.zero;
            }
            if(target==null)return;
            var position=target.transform.position;
            if(Time.deltaTime>0)velocity=Vector3.Lerp(velocity,(position-previous)/Time.deltaTime,.25f);previous=position;
            P12PipelineSafeDamageable aimPart=null;
            foreach(var gun in target.VariantWeapons)if(gun.Online(target.transform)){aimPart=gun.Block;break;}
            if(aimPart==null&&target.WeaponOnline&&target.WeaponBlock!=null&&!target.WeaponBlock.IsDead)aimPart=target.WeaponBlock;
            if(aimPart==null)aimPart=target.Core;
            var muzzle=run.Player.transform.Find("Turret_Yaw/Cannon_Pitch/Muzzle");
            if(muzzle==null||aimPart==null)return;
            Vector3 aim=aimPart.transform.position+velocity*(Vector3.Distance(muzzle.position,aimPart.transform.position)/34f);
            var camera=Camera.main;
            camera.transform.position=run.Player.transform.position+new Vector3(0,3,-6);
            camera.transform.rotation=Quaternion.LookRotation(aim-camera.transform.position);
            Vector3 away=Vector3.ProjectOnPlane(run.Player.transform.position-position,Vector3.up);float distance=away.magnitude;
            Vector3 waypoint=distance<16?position+away.normalized*11+Vector3.Cross(Vector3.up,away.normalized)*7:position;
            Vector3 direction=Vector3.ProjectOnPlane(waypoint-run.Player.transform.position,Vector3.up);
            float angle=Vector3.SignedAngle(run.Player.transform.forward,direction,Vector3.up);
            var keys=new List<Key>();if(angle>4)keys.Add(Key.D);else if(angle< -4)keys.Add(Key.A);
            if(distance<6)keys.Add(Key.S);else if(Mathf.Abs(angle)<65)keys.Add(Key.W);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));
            InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));
            result.combatFrames++;result.combatSeconds+=Time.unscaledDeltaTime;
        }
        void TrackPhysics()
        {
            var shells=FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None);result.peakShells=Math.Max(result.peakShells,shells.Length);
            foreach(var shell in shells)if(seenShells.Add(shell.GetInstanceID()))result.physicalRounds++;
            var pieces=FindObjectsByType<R3Debris>(FindObjectsSortMode.None);result.peakDebris=Math.Max(result.peakDebris,pieces.Length);
            foreach(var p in pieces)
            {
                var body=p.GetComponent<Rigidbody>();if(body==null)continue;int id=body.GetInstanceID();
                if(!traces.TryGetValue(id,out var trace))
                {
                    if(traces.Count>=120)continue;
                    trace=new DebrisObservation{id=id,mass=body.mass,firstTime=Time.time,origin=body.position,
                        initialNonblocking=p.GetComponentsInChildren<Collider>().All(c=>!c.enabled)};
                    traces[id]=trace;result.debris.Add(trace);
                    if(debrisSeries<3&&R1RunController.Instance.Progress.State==R1RunState.Combat)
                        StartCoroutine(CaptureDebris(++debrisSeries));
                }
                trace.maxDistance=Mathf.Max(trace.maxDistance,Vector3.Distance(trace.origin,body.position));
                if(Time.time-trace.firstTime>.5f){trace.restoredColliders|=p.GetComponentsInChildren<Collider>().Any(c=>c.enabled);trace.restoredGravity|=body.useGravity;}
            }
        }
        IEnumerator CaptureDebris(int series)
        {
            for(int frame=0;frame<4;frame++)
            {
                yield return new WaitForSecondsRealtime(.12f);
                if(R1RunController.Instance?.Progress?.State!=R1RunState.Combat)yield break;
                var name="debris-"+series+"-"+frame+".png";
                ScreenCapture.CaptureScreenshot(Path.Combine(output,name));result.screenshots.Add(name);
            }
            Save();
        }
        void Update()
        {
            var run=R1RunController.Instance;if(run?.Progress==null||output==null)return;
            Observe(run);TrackPhysics();
            if(run.Progress.State==R1RunState.Combat&&!run.Transitioning&&!run.Paused)Pilot(run);else if(mouse!=null)StopPilot(run);
            if(captureAt>=0&&Time.unscaledTime>=captureAt)
            {var name=captureName+".png";ScreenCapture.CaptureScreenshot(Path.Combine(output,name));result.screenshots.Add(name);captureAt=-1;Save();}
        }
        void OnApplicationQuit()
        {
            result.finishedUtc=DateTime.UtcNow.ToString("o");result.status=result.won?"ClosedAfterVictory":"ClosedBeforeFinalVictory";Save();
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
#endif

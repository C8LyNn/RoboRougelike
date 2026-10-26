#if UNITY_EDITOR || B01_UNIFIED_RC2_ACCEPTANCE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.R3;
using RoboRougelike.R6;

namespace RoboRougelike.R1
{
    // Explicit diagnostic in the SAME application. Never installed on ordinary launch.
    [DefaultExecutionOrder(2000)]
    public sealed class B01RC2InputAcceptance : MonoBehaviour
    {
        [Serializable] public sealed class Check {public string name,detail;public bool passed;}
        [Serializable] public sealed class Result
        {
            public string status="Running",method="Native queued InputSystem keyboard/mouse through unchanged player input/drive/fire; canonical API activity selection; not OS/human control",startedUtc,finishedUtc;
            public int width,height,physicalShots,rmbNewShots,maxGroundedWheels;public float movedMeters,turnedDegrees;
            public List<Check> checks=new List<Check>();public List<string> errors=new List<string>();
        }
        readonly Result result=new Result();readonly HashSet<int> seen=new HashSet<int>();
        R1RunController run;R3VehicleModel model;Rigidbody body;Keyboard keyboard;Mouse mouse;
        string output;int phase;float entered,deadline;Vector3 start,pausedPosition;Quaternion turnStart;int rightStart;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rc2-input-output");if(i<0||i+1>=args.Length)return;
            var root=new GameObject("RC2 explicit native input acceptance");DontDestroyOnLoad(root);root.AddComponent<B01RC2InputAcceptance>().Initialize(args[i+1]);
        }
        void Initialize(string path)
        {
            output=path;Directory.CreateDirectory(path);result.startedUtc=DateTime.UtcNow.ToString("o");
            Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();
            deadline=Time.realtimeSinceStartup+120;Application.logMessageReceived+=OnLog;Save();
        }
        void Save()=>File.WriteAllText(Path.Combine(output,"rc2-input-result.json"),JsonUtility.ToJson(result,true));
        void CheckThat(bool ok,string name,string detail="")
        {result.checks.Add(new Check{passed=ok,name=name,detail=detail});Save();if(!ok)throw new InvalidOperationException(name+": "+detail);}
        void Queue(bool forward=false,bool turn=false,bool left=false,bool right=false,bool enter=false,bool escape=false)
        {
            var keys=new List<Key>();if(forward)keys.Add(Key.W);if(turn)keys.Add(Key.D);if(enter)keys.Add(Key.Enter);if(escape)keys.Add(Key.Escape);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys.ToArray()));
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width*.5f,Screen.height*.5f)}.WithButton(MouseButton.Left,left).WithButton(MouseButton.Right,right));
        }
        void Go(int value){phase=value;entered=Time.realtimeSinceStartup;Save();}
        void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
        void Observe()
        {
            if(body==null)return;
            result.maxGroundedWheels=Mathf.Max(result.maxGroundedWheels,run.Player.GetComponent<R6VehicleDrive>().GroundedWheels);
            foreach(var shot in FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None))
                if(shot.IsActiveProjectile&&shot.OwnerRoot==run.Player.transform&&seen.Add(shot.GetInstanceID()))
                {
                    var origin=shot.GetComponent<RoboRougelike.R4.R4ProjectileOrigin>();
                    CheckThat(origin!=null&&model.Parts.Any(p=>p.Id==origin.PartId),"Actual player shot has canonical equipped part provenance");result.physicalShots++;
                }
        }
        void Update()
        {
            if(output==null)return;
            try
            {
                if(phase==99){if(Time.realtimeSinceStartup-entered<.3f)return;Finish();return;}
                if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Input fixture phase "+phase);
                if(phase==0)
                {
                    if(R1RunController.Instance?.Progress==null)return;run=R1RunController.Instance;
                    result.width=Screen.width;result.height=Screen.height;CheckThat(run.Progress.State==R1RunState.Ready,"Original game menu");Capture("00-menu");
                    Queue(enter:true);Go(1);
                }
                else if(phase==1)
                {
                    if(run.Progress.State!=R1RunState.Garage)return;Queue();model=run.VehicleModel;
                    CheckThat(run.Player!=null&&model!=null&&run.Player.Controller.enabled,"Native Enter opened the canonical preparation garage");Capture("01-garage");Go(2);
                }
                else if(phase==2)
                {
                    if(Time.realtimeSinceStartup-entered<.35f)return;
                    CheckThat(run.StartNextEncounter()&&run.ChooseRoute(0),"Canonical API selects ordinary activity from the same garage");Go(3);
                }
                else if(phase==3)
                {
                    if(run.Transitioning)return;
                    CheckThat(run.Progress.State==R1RunState.Combat&&run.VehicleModel==model,"Same robot/model in actual combat");
                    run.Encounter.SetCombatActive(false);body=run.Player.GetComponent<Rigidbody>();start=body.position;Queue(forward:true,left:true);Go(4);
                }
                else if(phase==4)
                {
                    Observe();Queue(forward:true,left:true);
                    if(Time.realtimeSinceStartup-entered<3)return;
                    result.movedMeters=Vector3.Distance(start,body.position);
                    CheckThat(result.movedMeters>2&&result.maxGroundedWheels>0&&body.constraints==RigidbodyConstraints.None,"W input drives original R6 through real wheel contacts",result.movedMeters.ToString("F3"));
                    CheckThat(result.physicalShots>0,"LMB input fires real owned physical projectiles");Capture("02-w-lmb");Queue();Go(5);
                }
                else if(phase==5)
                {
                    Observe();if(Time.realtimeSinceStartup-entered<.3f)return;
                    rightStart=result.physicalShots;turnStart=body.rotation;Queue(turn:true,right:true);Go(6);
                }
                else if(phase==6)
                {
                    Observe();Queue(turn:true,right:true);if(Time.realtimeSinceStartup-entered<1)return;
                    result.turnedDegrees=Quaternion.Angle(turnStart,body.rotation);result.rmbNewShots=result.physicalShots-rightStart;
                    CheckThat(result.turnedDegrees>3,"D input turns actual rigidbody",result.turnedDegrees.ToString("F3"));
                    CheckThat(result.rmbNewShots==0,"RMB preserves existing base behavior without firing LMB group");Capture("03-d-rmb");Queue();Go(7);
                }
                else if(phase==7)
                {
                    if(Time.realtimeSinceStartup-entered<.2f)return;Queue(escape:true);Go(8);
                }
                else if(phase==8)
                {
                    if(!run.Paused)return;Queue();pausedPosition=body.position;CheckThat(Time.timeScale==0,"Escape input pauses actual game");Capture("04-pause");Go(9);
                }
                else if(phase==9)
                {
                    if(Time.realtimeSinceStartup-entered<.3f)return;
                    CheckThat(Vector3.Distance(body.position,pausedPosition)<.01f,"Paused actual rigidbody does not advance");Queue(escape:true);Go(10);
                }
                else if(phase==10)
                {
                    if(run.Paused)return;Queue();CheckThat(Time.timeScale>0,"Escape input resumes actual game");
                    CheckThat(FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader!=null&&m.shader.isSupported&&!m.shader.name.Contains("InternalError"))),"Active scene materials have supported shaders");
                    result.status=result.errors.Count==0?"Passed":"Failed";Capture("05-resumed");Go(99);
                }
            }
            catch(Exception ex){result.errors.Add(ex.ToString());result.status="Failed";Go(99);}
        }
        void OnLog(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){result.errors.Add(text);Save();}}
        void Finish()
        {
            result.finishedUtc=DateTime.UtcNow.ToString("o");Save();Application.logMessageReceived-=OnLog;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
            output=null;Application.Quit(result.status=="Passed"?0:1);
        }
    }
}
#endif

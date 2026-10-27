#if MOVSERIES_VISUAL_QA
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.MOV007;
using RoboRougelike.Mechanics.MOV009;

namespace RoboRougelike.MOVSeries
{
    // Separate QA build: engine API assertions and virtual InputSystem events, never OS input.
    public sealed class MOVSeriesVisualQA : MonoBehaviour
    {
        int stage,checks,captures;float next,originX,originZ;
        string output,originalSnapshot;R3VehicleModel original;Keyboard keyboard;Key[] held=Array.Empty<Key>();
        readonly Result result=new Result();
        Action afterCapture;float afterCaptureDelay;
        RogueMachine.P12PipelineSafe.P12PipelineSafeCamera qaFollowCamera;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-movseries-qa-output");
            if(at<0||at+1>=args.Length)return;
            Application.runInBackground=true;var root=new GameObject("MOV series standalone QA");DontDestroyOnLoad(root);
            root.AddComponent<MOVSeriesVisualQA>().Initialize(args[at+1]);
        }
        void Initialize(string path)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-movseries-qa-run-id");
            result.runId=at>=0&&at+1<args.Length?args[at+1]:null;
            output=path;Directory.CreateDirectory(output);next=Time.unscaledTime+2;
            var settings=Instantiate(InputSystem.settings);InputSystem.settings=settings;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard=InputSystem.AddDevice<Keyboard>();
        }
        void Check(bool ok,string message){checks++;if(!ok)throw new InvalidOperationException(message);}
        void Capture(string name)
        {
            if(!Environment.GetCommandLineArgs().Contains("-movseries-no-capture"))
            {ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));captures++;}
            Debug.Log("[MOV SERIES QA] "+name);next=Time.unscaledTime+1;
        }
        void SetKeys(params Key[] keys){held=keys;}
        void AfterCapture(Action action,float delay)
        {
            // CaptureScreenshot reads the end of this frame. Keep its state intact until next Update.
            afterCapture=action;afterCaptureDelay=delay;next=Time.unscaledTime;
        }
        void Catalog(R1RunController run,int page,int tab,R3PartKind kind)
        {
            var hud=FindFirstObjectByType<R1RunHUD>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(R1RunHUD).GetField("partsPage",flags).SetValue(hud,page);typeof(R1RunHUD).GetField("settingsTab",flags).SetValue(hud,tab);
            run.Garage.SelectedPart=kind;
        }
        void FullConstruction(R1RunController run)
        {
            Action<Vector3Int,R3PartKind> buy=(cell,kind)=>Check(run.GarageBuy(cell,kind),"Buy "+kind+" "+cell);
            foreach(int x in new[]{-2,2})foreach(int z in new[]{-2,2})buy(new Vector3Int(x,0,z),R3PartKind.HoverEngine);
            foreach(int x in new[]{-2,2})buy(new Vector3Int(x,0,0),R3PartKind.CrawlerTrack);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-3,3})buy(new Vector3Int(x,0,z),R3PartKind.Suspension);
            buy(new Vector3Int(-1,1,0),R3PartKind.Booster);buy(new Vector3Int(1,1,0),R3PartKind.JumpDrive);
            buy(new Vector3Int(0,1,1),R3PartKind.Blink);buy(new Vector3Int(0,1,-1),R3PartKind.Gyroscope);
            buy(new Vector3Int(-1,1,2),R3PartKind.MagneticGrapple);buy(new Vector3Int(0,0,3),R3PartKind.OmniDrive);
            Check(run.VehicleModel.Scrap==15,"585Z actual purchases");run.Garage.FrameVehicle();
        }
        static string Snapshot(R3VehicleModel model)=>model.Scrap+"/"+model.Inventory.Count+"/"+model.SelectedDrive+"/"+model.GyroEnabled+"/"+
            model.BoosterThrustNewtons+"/"+model.BoosterDurationSeconds+"/"+model.HoverHeightMeters+"/"+model.SuspensionEnabled+"/"+model.SuspensionHeightOffset+"/"+
            string.Join(";",model.Parts.OrderBy(p=>p.Id).Select(p=>$"{p.Id}:{p.Kind}:{p.Cell}:{p.Health}:{p.DeviceReadyAt}"));
        void Mode(R1RunController run,R3DriveKind mode)
        {run.TogglePause();Check(run.ConfigureDrive(mode),"Select "+mode);run.TogglePause();}
        void Save(bool passed,string error=null)
        {
            result.assertionsPassed=passed;result.error=error;result.stage=stage;result.assertions=checks;result.requestedCaptures=captures;
            result.graphics=SystemInfo.graphicsDeviceType.ToString();
            File.WriteAllText(Path.Combine(output,"standalone-qa-result.json"),JsonUtility.ToJson(result,true));
        }
        void Update()
        {
            if(keyboard!=null){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(held));}
            var run=R1RunController.Instance;if(run?.Progress==null||run.Transitioning||Time.unscaledTime<next)return;
            var a=run.Player?.GetComponent<MOVSeriesVehicleAdapter>();var body=run.Player?.GetComponent<Rigidbody>();
            try
            {
                if(afterCapture!=null)
                {
                    var action=afterCapture;afterCapture=null;action();next=Time.unscaledTime+afterCaptureDelay;return;
                }
                switch(stage)
                {
                    case 0:Capture("qa-menu");break;
                    case 1:
                        Check(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),"Garage entry");FullConstruction(run);
                        Check(run.ConfigureDrive(R3DriveKind.Hover),"Select Hover in garage");
                        Check(run.ConfigureSuspension(true,ActiveSuspensionSupportMode.FullSupport,.3f),"Retain suspension demand");
                        Catalog(run,0,2,R3PartKind.Suspension);Capture("qa-garage-suspension");break;
                    case 2:Catalog(run,1,1,R3PartKind.Booster);Capture("qa-garage-booster");break;
                    case 3:Catalog(run,2,0,R3PartKind.HoverEngine);Capture("qa-garage-drive");break;
                    case 4:
                        original=run.VehicleModel;originalSnapshot=Snapshot(original);Check(run.EnterTestRange(),"Enter trial");next=Time.unscaledTime+2;break;
                    case 5:
                        result.hoverHeight=body.position.y;Check(body.position.y>.85f&&body.position.y<1.25f,"Actual Hover lift");
                        Check(run.Player.GetComponent<HoverVehicleController>().LastSupportedPodCount==4,"Four real pods");
                        Check(run.Player.GetComponent<ActiveSuspensionController>().LastTotalLiftNewtons==0,"No double suspension support");
                        Capture("qa-hover");originX=body.position.x;SetKeys(Key.A);next=Time.unscaledTime+.7f;break;
                    case 6:
                        Check(body.position.x<originX-.15f,"Hover virtual A strafe");Capture("qa-hover-strafe");SetKeys();
                        AfterCapture(()=>{Mode(run,R3DriveKind.Omni);originX=body.position.x;SetKeys(Key.A);},1);break;
                    case 7:
                        Check(body.position.x<originX-.1f,"Omni virtual A strafe");Capture("qa-omni");SetKeys();
                        AfterCapture(()=>{
                            run.TogglePause();Check(run.ConfigureSuspension(true,ActiveSuspensionSupportMode.FullSupport,0),"Low support for Tracks");
                            Check(run.ConfigureDrive(R3DriveKind.Tracks),"Select Tracks");run.TogglePause();},1);break;
                    case 8:originZ=body.position.z;SetKeys(Key.W);next=Time.unscaledTime+1;break;
                    case 9:
                        Check(body.position.z>originZ+.3f,"Tracks virtual W propulsion");result.tracksSpeed=body.linearVelocity.magnitude;Capture("qa-tracks");SetKeys();
                        AfterCapture(()=>{Mode(run,R3DriveKind.Wheels);SetKeys(Key.W,Key.LeftShift);},.45f);break;
                    case 10:
                        Check(a.Boosting&&body.linearVelocity.magnitude>.5f,"Booster virtual Shift activation/force");result.boostSpeed=body.linearVelocity.magnitude;Capture("qa-booster");SetKeys();
                        AfterCapture(()=>Check(run.UseJump(),"Jump actual grounded vehicle"),.25f);break;
                    case 11:
                        Check(body.position.y>.9f,"Jump physical height");Check(Vector3.Angle(body.transform.up,Vector3.up)<30,"Gyro keeps jump stable");Capture("qa-jump");next=Time.unscaledTime+1.5f;break;
                    case 12:
                        float before=body.position.z;Check(run.UseBlink(),"Safe Blink in shared range");Check(Mathf.Abs(body.position.z-before-8)<.02f,"Whole vehicle Blink distance");Capture("qa-blink");break;
                    case 13:
                        // Aim through the same player API at the real range anchor, then frame both endpoints.
                        qaFollowCamera=FindFirstObjectByType<RogueMachine.P12PipelineSafe.P12PipelineSafeCamera>();qaFollowCamera.enabled=false;
                        Vector3 anchor=new Vector3(-25,3,7),middle=(body.position+anchor)*.5f;
                        Camera.main.transform.position=middle+new Vector3(12,18,-22);Camera.main.transform.LookAt(anchor);
                        Check(run.UseGrapple(),"Grapple camera aim at real range anchor");
                        Camera.main.transform.LookAt(middle);next=Time.unscaledTime+.4f;break;
                    case 14:
                        Check(a.GrappleAttached,"Physical attached rope");Capture("qa-grapple");
                        AfterCapture(()=>{
                            Check(run.UseGrapple()&&!a.GrappleAttached,"Release rope");qaFollowCamera.enabled=true;Mode(run,R3DriveKind.Hover);
                            var pod=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.HoverEngine);
                            pod.Damageable.ApplyDamage(10000,pod.transform.position,Vector3.zero);},1);break;
                    case 15:
                        Check(a.OperationalCount(R3PartKind.HoverEngine)==3,"Real dead pod disabled");Check(run.Player.GetComponent<ActiveSuspensionController>().LastTotalLiftNewtons==0,"No fallback lift after pod loss");
                        Capture("qa-pod-loss");Check(Snapshot(original)==originalSnapshot,"Trial does not mutate garage");
                        AfterCapture(()=>Check(run.ResetTestRange(),"Reset trial"),2);break;
                    case 16:
                        Check(a.OperationalCount(R3PartKind.HoverEngine)==4,"Reset restores actual pods");run.TogglePause();Check(!run.UseBooster(),"Pause rejects abilities");Capture("qa-pause-controls");break;
                    case 17:Check(run.ReturnFromTestRange(),"Return garage");next=Time.unscaledTime+1;break;
                    case 18:
                        Check(ReferenceEquals(run.VehicleModel,original)&&Snapshot(original)==originalSnapshot,"Original IDs/HP/settings/scrap preserved");
                        Capture("qa-garage-return");result.parts=original.Parts.Count;result.scrap=original.Scrap;break;
                    case 19:Save(true);enabled=false;Application.Quit(0);return;
                }
                stage++;
            }
            catch(Exception error){Debug.LogException(error);Save(false,error.ToString());enabled=false;Application.Quit(2);}
        }
        [Serializable] sealed class Result
        {
            public bool assertionsPassed;public int stage,assertions,requestedCaptures,parts,scrap;
            public string runId,graphics,error,method="standalone engine APIs and virtual InputSystem events; no OS/human input";
            public float hoverHeight,tracksSpeed,boostSpeed;
        }
    }
}
#endif

#if MOVSERIES_VISUAL_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.PackageIntegration;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.Mechanics.IndependentSteering;

namespace RoboRougelike.QA
{
    // Controlled native API/physics fixtures, excluded entirely from retail.
    public sealed class RRLW8RuntimeQA : MonoBehaviour
    {
        [Serializable] public sealed class Check {public string id,name,detail;public bool passed;}
        [Serializable] public sealed class Result
        {
            public string method="Windows native API/physics fixture; garage APIs, controlled damage/EMP and R6 force commands; NOT manual gameplay",status="Running",startedUtc,finishedUtc,graphics;
            public int width,height;public List<Check> checks=new List<Check>();public List<string> errors=new List<string>();
        }
        readonly Result result=new Result();string output,current;R1RunController run;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-rrl-w8-native-api-output");if(at<0||at+1>=args.Length)return;
            var root=new GameObject("Explicit W8 native API fixture");DontDestroyOnLoad(root);root.AddComponent<RRLW8RuntimeQA>().Initialize(args[at+1]);
        }
        void Initialize(string path)
        {
            output=path;Directory.CreateDirectory(path);result.startedUtc=DateTime.UtcNow.ToString("o");result.graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;
            Application.runInBackground=true;Application.targetFrameRate=60;Application.logMessageReceived+=OnLog;Save();
        }
        void OnLog(string message,string stack,LogType type){if((type==LogType.Error||type==LogType.Assert||type==LogType.Exception)&&result.errors.Count<30){result.errors.Add(message);Save();}}
        void Save(){if(output!=null)File.WriteAllText(Path.Combine(output,"native-api-result.json"),JsonUtility.ToJson(result,true));}
        void Require(bool ok,string name,string detail=""){result.checks.Add(new Check{id=current,name=name,passed=ok,detail=detail});Save();if(!ok)throw new InvalidOperationException(current+": "+name+" "+detail);}
        void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
        IEnumerator Start()
        {
            var routine=Run();while(true){object step=null;bool more=false;try{more=routine.MoveNext();if(more)step=routine.Current;}catch(Exception error){result.errors.Add(error.ToString());Save();break;}if(!more)break;yield return step;}
            result.status=result.errors.Count==0&&result.checks.Count>0&&result.checks.All(c=>c.passed)?"Passed":"Failed";result.finishedUtc=DateTime.UtcNow.ToString("o");Save();yield return null;Application.Quit(result.status=="Passed"?0:1);
        }
        IEnumerator Run()
        {
            float deadline=Time.realtimeSinceStartup+30;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
            current="W8";run=R1RunController.Instance;Require(run?.Progress!=null,"Real gameplay scene initialized");result.width=Screen.width;result.height=Screen.height;
            Require(run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),"Real Light workshop entry");
            Require(run.GarageBuy(new Vector3Int(-1,1,-2),R3PartKind.StructuralScanner),"COM purchased and mounted through actual garage");
            Require(run.GarageBuy(new Vector3Int(-1,1,1),R3PartKind.ScannerPulse),"SEN purchased and mounted through actual garage");
            Require(run.GarageBuy(new Vector3Int(1,1,-2),R3PartKind.IndependentSteering),"MOV011 purchased and mounted through actual garage");
            Require(run.VehicleModel.Scrap==485,"Three modules spend original workshop funds exactly once",run.VehicleModel.Scrap.ToString());
            Require(run.EnterTestRange(),"Real range transition");while(run.Transitioning)yield return null;
            var assembly=run.Player.GetComponent<R3VehicleAssembly>();var com=run.Player.GetComponentInChildren<RRLStructuralScannerBinding>();var sen=run.Player.GetComponentInChildren<RRLPulseScannerBinding>();var steering=run.Player.GetComponentInChildren<RRLIndependentSteeringBinding>();
            Require(com!=null&&sen!=null&&steering!=null&&steering.GeometrySupported,"Three owned devices bind existing player/body");
            var body=run.Player.GetComponent<Rigidbody>();Require(run.Player.GetComponentsInChildren<Rigidbody>().Length==1&&
                !run.Player.GetComponentsInChildren<MonoBehaviour>().Any(c=>c.GetType().Name=="SteeringDemoDrive"),"One vehicle body and no standalone demo drive");
            var devices=new[]{com.Device,sen.Device,steering.Device};
            Require(devices.All(d=>d.GetComponents<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Length==1&&d.HardwareAvailable),"Each module uses one existing P12 HP/availability boundary");
            var enemy=run.GetComponent<R6TestRange>().TrainingTarget;float hp=enemy.Core.CurrentHp;
            current="COM-001 / SEN-001";
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Native scanner LOS fixture";wall.transform.position=new Vector3(0,3,-23.5f);wall.transform.localScale=new Vector3(50,10,1);Physics.SyncTransforms();
            Require(!com.TryActivate()&&com.CooldownLeft==0&&com.Scanner.Contacts.Count==0,"Solid wall blocks structural scan without spending cooldown");
            Require(sen.TryActivate()&&sen.Pulse.Revealed.Any(tag=>tag.transform.IsChildOf(enemy.transform))&&!sen.Pulse.LastPulseTruncated,"Original pulse reveals actual enemy through wall");
            Require(enemy.Core.CurrentHp==hp,"Scanning does not damage actual target");Capture("01-pulse-through-wall");yield return new WaitForSeconds(.15f);
            Destroy(wall);yield return null;Physics.SyncTransforms();Require(com.TryActivate()&&com.Scanner.Contacts.Count>0,"Visible actual enemy yields original structural HP contacts");
            Require(!com.TryActivate()&&!sen.TryActivate(),"Cooldown rejects repeated input on both source clocks");
            float comReady=com.Device.State.DeviceReadyAt,senReady=sen.Device.State.DeviceReadyAt,comLeft=com.CooldownLeft,senLeft=sen.CooldownLeft;
            run.TogglePause();yield return new WaitForSecondsRealtime(.2f);
            Require(!com.TryActivate()&&!sen.TryActivate()&&Mathf.Abs(com.CooldownLeft-comLeft)<.002f&&Mathf.Abs(sen.CooldownLeft-senLeft)<.002f,"Pause rejects activation and freezes both source clocks");
            run.TogglePause();Require(com.Scanner.Contacts.Count>0&&sen.Pulse.Revealed.Count>0,"Pause preserves valid information");Capture("02-distinct-scanner-results");yield return new WaitForSeconds(.15f);
            Require(steering.SetMode(SteeringMode.Crab),"Owned MOV011 accepts actual player mode selection");
            steering.TrySolve(1,Time.fixedDeltaTime,out var initial);
            var velocity=body.linearVelocity;var spin=body.angularVelocity;
            foreach(var device in devices)device.GetComponent<EmpReceiver>().ApplyEmp(.6f);
            Require(com.Scanner.Contacts.Count==0&&sen.Pulse.Revealed.Count==0&&steering.Controller.CurrentSolution.curvatureInverseMeters==0,"EMP clears scanner information and steering angles");
            Require(body.linearVelocity==velocity&&body.angularVelocity==spin,"EMP availability changes preserve existing momentum");
            Require(!com.TryActivate()&&!sen.TryActivate()&&!steering.SetMode(SteeringMode.FrontOnly),"EMP blocks all three device actions");
            deadline=Time.time+.7f;while(Time.time<deadline)yield return null;
            Require(devices.All(d=>d.HardwareAvailable)&&com.Device.State.DeviceReadyAt==comReady&&sen.Device.State.DeviceReadyAt==senReady,"Automatic EMP recovery preserves committed cooldowns");
            com.gameObject.SetActive(false);yield return new WaitForSeconds(.2f);com.gameObject.SetActive(true);yield return null;
            Require(Mathf.Abs(com.CooldownLeft-Mathf.Max(0,comReady-Time.time))<.05f&&!com.TryActivate(),"Retained COM view resumes canonical cooldown without free activation");
            current="MOV-011";run.Player.Controller.SetGameplayInputEnabled(true);run.Player.Controller.enabled=false;
            Require(steering.SetMode(SteeringMode.Crab),"Crab mode after EMP");Vector3 start=body.position;float yaw=body.rotation.eulerAngles.y;
            Require(body.useGravity&&body.constraints==RigidbodyConstraints.None,"B01 grounded R6 uses gravity without suspension parts");
            deadline=Time.time+.8f;while(Time.time<deadline){run.Player.GetComponent<R6VehicleDrive>().Step(1,1,10,5,105);yield return new WaitForFixedUpdate();}
            Require(body.position.x-start.x>.12f&&body.position.z-start.z>.2f&&Mathf.Abs(Mathf.DeltaAngle(yaw,body.rotation.eulerAngles.y))<8,"Actual grounded R6 crab translation",$"delta={body.position-start}; yaw={Mathf.DeltaAngle(yaw,body.rotation.eulerAngles.y)}");
            Capture("03-actual-r6-crab");yield return new WaitForSeconds(.15f);
            Require(steering.SetMode(SteeringMode.CounterPhase),"Counter-phase mode");deadline=Time.time+.5f;
            while(Time.time<deadline){run.Player.GetComponent<R6VehicleDrive>().Step(1,1,10,5,105);yield return new WaitForFixedUpdate();}
            Require(steering.Controller.CurrentSolution.frontCenterDegrees>0&&steering.Controller.CurrentSolution.rearCenterDegrees<0&&body.angularVelocity.y>.1f,"Actual R6 counter-phase yaw");
            Require(steering.SetMode(SteeringMode.Crab),"Persistent mode selected before loss");run.Player.Controller.enabled=true;
            current="W8 physical repair / ownership";
            var states=devices.Select(d=>d.State).ToArray();
            foreach(var device in devices)device.Damageable.ApplyDamage(100000,device.transform.position,Vector3.up);
            assembly.Capture();Require(states.All(s=>s.Health==0),"Actual physical device destruction captured in canonical owned state");
            var copy=run.VehicleModel.DeepCopy();int funds=copy.Scrap;
            Require(states.All(s=>copy.Repair(s.Cell))&&copy.Scrap==funds-115,"All three destroyed devices repaired at actual full purchase cost");
            foreach(var state in states)
            {
                Require(copy.Remove(state.Cell)&&copy.Inventory.Any(p=>p.Id==state.Id)&&copy.Mount(state.Id,state.Cell),"Actual remove/inventory/remount preserves owned identity",state.Kind.ToString());
            }
            var restored=R3VehicleAssembly.Build(copy,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);
            Require(restored.GetComponentInChildren<RRLStructuralScannerBinding>().Device.State.DeviceReadyAt==comReady&&restored.GetComponentInChildren<RRLPulseScannerBinding>().Device.State.DeviceReadyAt==senReady&&restored.GetComponentInChildren<RRLIndependentSteeringBinding>().Controller.Mode==SteeringMode.Crab,"Paid repair/rebuild retains source readiness and selected mode");
            Destroy(restored.gameObject);Require(run.ReturnFromTestRange(),"Actual return to workshop");while(run.Transitioning)yield return null;
            Require(run.Progress.State==R1RunState.Garage&&run.VehicleModel.Scrap==485&&run.VehicleModel.Parts.Where(p=>states.Any(s=>s.Id==p.Id)).All(p=>p.Health==1),"Disposable range damage does not leak into workshop");
            Capture("04-restored-workshop");yield return new WaitForSecondsRealtime(.2f);
            current="MOV-011 + MOV-009";
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-3,3})Require(run.GarageBuy(new Vector3Int(x,0,z),R3PartKind.Suspension),"Actual MOV009 support purchased",$"{x},0,{z}");
            Require(run.EnterTestRange(),"Range with original four-point active suspension");while(run.Transitioning)yield return null;
            body=run.Player.GetComponent<Rigidbody>();steering=run.Player.GetComponentInChildren<RRLIndependentSteeringBinding>();
            var suspension=run.Player.GetComponent<RoboRougelike.Mechanics.MOV009.ActiveSuspensionController>();
            Require(suspension!=null&&suspension.RegisteredModuleCount==4&&body.useGravity&&body.constraints==RigidbodyConstraints.None&&run.Player.GetComponent<R6VehicleDrive>().SuspensionPhysics,"Actual MOV009 four supports, gravity and unconstrained body");
            yield return new WaitForSeconds(.8f);run.Player.Controller.SetGameplayInputEnabled(true);run.Player.Controller.enabled=false;
            Require(steering.SetMode(SteeringMode.Crab),"Crab with active suspension");start=body.position;yaw=body.rotation.eulerAngles.y;
            deadline=Time.time+.8f;while(Time.time<deadline){run.Player.GetComponent<R6VehicleDrive>().Step(1,1,10,5,105);yield return new WaitForFixedUpdate();}
            Require(body.position.x-start.x>.1f&&body.position.z-start.z>.2f&&Mathf.Abs(Mathf.DeltaAngle(yaw,body.rotation.eulerAngles.y))<8&&suspension.LastTotalLiftNewtons>0&&Vector3.Angle(run.Player.transform.up,Vector3.up)<15,"Actual MOV009-supported R6 crab translation without competing solver",$"delta={body.position-start}; yaw={Mathf.DeltaAngle(yaw,body.rotation.eulerAngles.y)}; lift={suspension.LastTotalLiftNewtons}");
            Capture("05-mov009-supported-crab");yield return new WaitForSeconds(.15f);
            Require(run.ReturnFromTestRange(),"Supported rig returns to workshop");while(run.Transitioning)yield return null;
            run.RestartRun();yield return null;while(R1RunController.Instance?.Progress==null)yield return null;
            Require(UnityEngine.Object.FindObjectsByType<R1RunController>(FindObjectsSortMode.None).Length==1&&UnityEngine.Object.FindObjectsByType<RRLScannerControls>(FindObjectsSortMode.None).Length==0,"Restart removes old owned sensor/input views");
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
#endif

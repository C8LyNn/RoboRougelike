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
using RoboRougelike.Mechanics.EMP;

namespace RoboRougelike.QA
{
    // Explicit controlled native physics fixtures, absent from retail. Not manual gameplay.
    public sealed class B01PhysicsRuntimeQA : MonoBehaviour
    {
        [Serializable] public sealed class Check {public string name,detail;public bool passed;}
        [Serializable] public sealed class Sample {public string rig;public float time;public Vector3 position,up,velocity,spin;public int attempts,contacts;public bool recovering;}
        [Serializable] public sealed class Result
        {
            public string method="Native controlled physics API fixture; real garage/body/collider/force/Core; fixture poses/damage and fixed inspection camera, NOT manual gameplay",status="Running",startedUtc,finishedUtc,graphics;
            public int width,height;public List<Check> checks=new List<Check>();public List<Sample> samples=new List<Sample>();public List<string> errors=new List<string>();
        }
        readonly Result result=new Result();string output,stage;R1RunController run;Rigidbody body;float cameraDistance;
        MonoBehaviour[] cameraBehaviours;bool inspectCamera;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-b01-phy-native-output");if(at<0||at+1>=args.Length)return;
            var root=new GameObject("B01 explicit native physics fixture");DontDestroyOnLoad(root);root.AddComponent<B01PhysicsRuntimeQA>().Initialize(args[at+1]);
        }
        void Initialize(string path)
        {
            output=path;Directory.CreateDirectory(path);result.startedUtc=DateTime.UtcNow.ToString("o");result.graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;
            Application.runInBackground=true;Application.targetFrameRate=60;Application.logMessageReceived+=OnLog;Save();
        }
        void Save(){if(output!=null)File.WriteAllText(Path.Combine(output,"physics-native-result.json"),JsonUtility.ToJson(result,true));}
        void OnLog(string text,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&result.errors.Count<30){result.errors.Add(text);Save();}}
        void Require(bool ok,string text,string detail=""){result.checks.Add(new Check{name=stage+" / "+text,detail=detail,passed=ok});Save();if(!ok)throw new InvalidOperationException(text+" "+detail);}
        void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
        void Pose(Vector3 p,Quaternion q)
        {
            // Fixture placement only. Product recovery never performs these writes.
            body.interpolation=RigidbodyInterpolation.None;body.transform.SetPositionAndRotation(p,q);body.position=p;body.rotation=q;
            body.linearVelocity=body.angularVelocity=Vector3.zero;Physics.SyncTransforms();body.WakeUp();
        }
        void LateUpdate()
        {
            if(!inspectCamera||body==null||Camera.main==null)return;
            var target=body.position+Vector3.up*.4f;
            Camera.main.transform.SetPositionAndRotation(target+new Vector3(cameraDistance,cameraDistance*.55f,-cameraDistance),Quaternion.LookRotation(new Vector3(-cameraDistance,-cameraDistance*.55f,cameraDistance),Vector3.up));
        }
        void RestoreCamera(){inspectCamera=false;if(cameraBehaviours!=null)foreach(var b in cameraBehaviours)if(b!=null)b.enabled=true;}
        IEnumerator Start()
        {
            var routine=Run();while(true){object step=null;bool more=false;try{more=routine.MoveNext();if(more)step=routine.Current;}catch(Exception error){result.errors.Add(error.ToString());Save();break;}if(!more)break;yield return step;}
            RestoreCamera();result.finishedUtc=DateTime.UtcNow.ToString("o");result.status=result.errors.Count==0&&result.checks.Count>0&&result.checks.All(c=>c.passed)?"Passed":"Failed";Save();yield return null;Application.Quit(result.status=="Passed"?0:1);
        }
        IEnumerator Run()
        {
            float end=Time.realtimeSinceStartup+30;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<end)yield return null;
            run=R1RunController.Instance;stage="Boot";Require(run?.Progress!=null,"Actual game initializes");result.width=Screen.width;result.height=Screen.height;
            foreach(var kind in new[]{R3VehicleKind.Light,R3VehicleKind.Heavy})
            {
                stage=kind.ToString();Require(run.SelectVehicle(kind)&&run.OpenSandboxGarage(),"Actual garage entry");int bottom=kind==R3VehicleKind.Light?1:2;
                int bought=0;for(int y=bottom;y<=bottom+1;y++)for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                {
                    var cell=new Vector3Int(x,y,z);if(run.VehicleModel.Parts.Any(p=>p.Cell==cell))continue;
                    Require(run.GarageBuy(cell,R3PartKind.Armor),"Broad roof armor installed through actual garage",cell.ToString());bought++;
                }
                Require(bought==17&&run.VehicleModel.Scrap==515,"Seventeen existing armor parts spend85 scrap");
                Require(run.EnterTestRange(),"Actual range entry");while(run.Transitioning)yield return null;
                var vehicle=run.Player;var assembly=vehicle.GetComponent<R3VehicleAssembly>();body=vehicle.GetComponent<Rigidbody>();
                cameraBehaviours=UnityEngine.Object.FindObjectsByType<RogueMachine.P12PipelineSafe.P12PipelineSafeCamera>(FindObjectsSortMode.None).Where(b=>b.enabled).Cast<MonoBehaviour>().ToArray();foreach(var b in cameraBehaviours)b.enabled=false;inspectCamera=true;
                var drive=vehicle.GetComponent<R6VehicleDrive>();var recovery=vehicle.GetComponent<R6CoreRecovery>();var core=vehicle.Core;
                cameraDistance=kind==R3VehicleKind.Light?5:8;
                Require(recovery!=null&&body.useGravity&&body.constraints==RigidbodyConstraints.None,"One real gravity body and owned Core capability");
                Require(vehicle.GetComponentsInChildren<Rigidbody>().Length==1,"No extra solver body");
                vehicle.Controller.SetGameplayInputEnabled(true);vehicle.Controller.enabled=false;yield return new WaitForSeconds(.6f);
                var spawn=body.position;spawn.y=.7f;Pose(spawn,Quaternion.identity);var start=body.position;
                end=Time.time+.5f;while(Time.time<end){drive.Step(1,0,10,5,100);yield return new WaitForFixedUpdate();}
                Require(body.position.z-start.z>.15f&&drive.GroundedWheels>0,"Real floor contacts create forward movement",$"delta={body.position-start}, contacts={drive.GroundedWheels}");Capture(stage+"-01-flat");yield return new WaitForSeconds(.1f);
                Pose(spawn+Vector3.up*5,Quaternion.identity);body.linearVelocity=new Vector3(1,2,3);
                end=Time.time+.2f;while(Time.time<end){drive.Step(1,1,10,5,100);drive.StepWorld(Vector3.forward,10);yield return new WaitForFixedUpdate();}
                Require(drive.GroundedWheels==0&&Mathf.Abs(body.linearVelocity.x-1)<.05f&&Mathf.Abs(body.linearVelocity.z-3)<.05f,"Airborne player and AI wheel commands preserve horizontal inertia",body.linearVelocity.ToString());
                Require(body.linearVelocity.y<2&&body.linearVelocity.y>-.5f,"Gravity remains active during loss of contact");
                var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.name="B01 explicit native10degree slope";ramp.transform.position=new Vector3(spawn.x+15,1,spawn.z);ramp.transform.localScale=new Vector3(10,1,20);ramp.transform.rotation=Quaternion.Euler(-10,0,0);
                var slopePose=ramp.transform.TransformPoint(new Vector3(0,.5f,0))+ramp.transform.up*.65f;Pose(slopePose,ramp.transform.rotation);start=body.position;
                end=Time.time+.4f;while(Time.time<end){drive.Step(1,0,10,5,100);yield return new WaitForFixedUpdate();}
                Require(drive.GroundedWheels>0&&Vector3.Dot(body.position-start,ramp.transform.forward)>.03f&&Vector3.Dot(body.rotation*Vector3.up,Vector3.up)>.7f,"Actual10degree slope remains drivable and supported",$"delta={body.position-start}, contacts={drive.GroundedWheels}");Capture(stage+"-01b-slope");yield return new WaitForSeconds(.1f);Destroy(ramp);yield return null;
                recovery.enabled=false;var roof=spawn;roof.y=kind==R3VehicleKind.Light?1.55f:2.1f;Pose(roof,Quaternion.Euler(0,0,180));
                end=Time.time+.35f;while(Time.time<end){drive.Step(1,1,10,5,100);yield return new WaitForFixedUpdate();}
                Require(drive.GroundedWheels==0&&new Vector2(body.linearVelocity.x,body.linearVelocity.z).magnitude<.15f,"Roof-facing wheels cannot create traction",body.linearVelocity.ToString());
                recovery.enabled=true;body.WakeUp();float health=core.CurrentHp;int count=assembly.Parts.Count;Capture(stage+"-02-roof");
                bool intermediate=false;float nextSample=0;end=Time.time+7;
                while(Time.time<end)
                {
                    if(Time.time>=nextSample)
                    {
                        result.samples.Add(new Sample{rig=stage,time=Time.time,position=body.position,up=body.rotation*Vector3.up,velocity=body.linearVelocity,spin=body.angularVelocity,attempts=recovery.AttemptsStarted,contacts=drive.GroundedWheels,recovering=recovery.IsRecovering});nextSample=Time.time+.1f;
                    }
                    float dot=Vector3.Dot(body.rotation*Vector3.up,Vector3.up);
                    if(!intermediate&&dot>-.6f&&dot<.6f&&recovery.AttemptsStarted>0){intermediate=true;Capture(stage+"-03-physical-roll");}
                    yield return new WaitForFixedUpdate();
                }
                drive.RefreshMass();
                Require(recovery.AttemptsStarted==1&&Vector3.Dot(body.rotation*Vector3.up,Vector3.up)>.75f&&drive.GroundedWheels>0,"Actual body recovers to wheel-supported orientation",$"attempts={recovery.AttemptsStarted}, up={body.rotation*Vector3.up}, contacts={drive.GroundedWheels}");
                Require(intermediate,"Intermediate simulated orientation captured; no teleport");
                Require(core.CurrentHp==health&&assembly.Parts.Count==count,"Recovery restores no HP or parts");
                Require(recovery.LastLiftImpulse<=1800.1f&&recovery.LastPeakTorque<=30000.1f,"Actual lift and torque obey configured default caps",$"mass={body.mass}, lift={recovery.LastLiftImpulse}, torque={recovery.LastPeakTorque}");Capture(stage+"-04-recovered");yield return new WaitForSeconds(.15f);
                var wheel=assembly.Parts.First(p=>p.Kind==R3PartKind.Wheel);wheel.Damageable.ApplyDamage(100000,wheel.transform.position,Vector3.zero);vehicle.Structure.EvaluateStructure();drive.RefreshMass();
                Require(drive.Wheels==(kind==R3VehicleKind.Light?3:11),"Actual destroyed wheel reduces available running gear");
                Pose(roof,Quaternion.Euler(0,0,180));core.GetComponent<EmpReceiver>().ApplyEmp(8);int attempts=recovery.AttemptsStarted;
                yield return new WaitForSeconds(recovery.CooldownLeft+.6f);
                Require(recovery.CooldownLeft==0&&recovery.AttemptsStarted==attempts,"EMP blocks roof recovery even after cooldown naturally expires");
                core.ApplyDamage(100000,core.transform.position,Vector3.zero);vehicle.Structure.EvaluateStructure();yield return new WaitForSeconds(2);
                Require(recovery.AttemptsStarted==attempts&&!recovery.IsRecovering,"Destroyed Core cannot initiate another attempt");
                vehicle.Controller.enabled=true;Require(run.ReturnFromTestRange(),"Actual return to garage");while(run.Transitioning)yield return null;
                Require(run.VehicleModel.Scrap==515&&run.VehicleModel.Parts.All(p=>p.Health==1),"Disposable range loss does not leak into garage");
                RestoreCamera();run.RestartRun();yield return null;while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;Require(run.Progress.State==R1RunState.Ready,"Actual main menu return");
            }
        }
        void OnDestroy(){RestoreCamera();Application.logMessageReceived-=OnLog;}
    }
}
#endif

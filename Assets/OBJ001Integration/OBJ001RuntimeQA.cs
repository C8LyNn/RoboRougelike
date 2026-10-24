#if MOVSERIES_VISUAL_QA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.OBJ001Integration
{
    // Explicit API/physics acceptance fixture, not human gameplay; completely excluded from retail.
    [DefaultExecutionOrder(2000)]
    public sealed class OBJ001RuntimeQA : MonoBehaviour
    {
        [Serializable] public sealed class Check {public string name,detail;public bool passed;}
        [Serializable] public sealed class Result
        {
            public string method="Native controlled API/physics: real AI fire, canonical HP, explicit core-damage wave completion, actual reward/garage/restart; NOT human gameplay",status="Running",startedUtc,finishedUtc,graphics;
            public int width,height;public List<Check> checks=new List<Check>();public List<string> errors=new List<string>();
        }
        readonly Result result=new Result();string output;R1RunController run,previous;int phase;float readyAt,deadline,playerHealth;
        bool quitting;int expectedWave;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-obj001-native-output");if(i<0||i+1>=args.Length)return;
            var root=new GameObject("OBJ001 explicit native acceptance");DontDestroyOnLoad(root);root.AddComponent<OBJ001RuntimeQA>().Initialize(args[i+1]);
        }
        void Initialize(string path)
        {
            output=path;Directory.CreateDirectory(output);result.startedUtc=DateTime.UtcNow.ToString("o");result.graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;
            Application.runInBackground=true;Application.targetFrameRate=60;deadline=Time.realtimeSinceStartup+180;Application.logMessageReceived+=OnLog;Save();
        }
        void OnLog(string text,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&result.errors.Count<25){result.errors.Add(text);Save();}}
        void Save(){if(output!=null)File.WriteAllText(Path.Combine(output,"obj001-native-result.json"),JsonUtility.ToJson(result,true));}
        void Require(bool ok,string name,string detail=""){result.checks.Add(new Check{name=name,detail=detail,passed=ok});Save();if(!ok)throw new InvalidOperationException(name+" "+detail);}
        void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
        void Go(int next,float delay=.15f){phase=next;readyAt=Time.realtimeSinceStartup+delay;}
        void Finish(bool success)
        {
            result.status=success&&result.errors.Count==0?"Passed":"Failed";result.finishedUtc=DateTime.UtcNow.ToString("o");Save();Go(99,.3f);
        }
        static float Health(R1RunController r)=>r.Player.GetComponent<R3VehicleAssembly>().Parts.Sum(p=>p.Damageable!=null?p.Damageable.CurrentHp:0);
        void ClearWave(){foreach(var e in run.Encounter.Enemies.ToArray())e.Core.ApplyDamage(100000,e.Core.transform.position,Vector3.zero);}
        static void Place(P12PipelineSafeEnemyTurret enemy,Vector3 p,Vector3 aim)
        {
            var b=enemy.GetComponent<Rigidbody>();b.interpolation=RigidbodyInterpolation.None;enemy.transform.SetPositionAndRotation(p,Quaternion.identity);b.position=p;b.rotation=Quaternion.identity;
            b.linearVelocity=b.angularVelocity=Vector3.zero;enemy.ConfigureMovement(0,12,0);
            enemy.GetComponentsInChildren<Transform>().Single(t=>t.name=="Enemy_Yaw").rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(aim-p,Vector3.up));
        }
        void Update()
        {
            if(output==null||quitting||Time.realtimeSinceStartup<readyAt)return;
            try
            {
                if(phase==99){quitting=true;Application.Quit(result.status=="Passed"?0:1);return;}
                if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Native OBJ001 acceptance timed out at phase"+phase);
                if(phase==0)
                {
                    if(R1RunController.Instance?.Progress==null)return;run=R1RunController.Instance;result.width=Screen.width;result.height=Screen.height;
                    Require(run.Progress.State==R1RunState.Ready,"Actual game boot");Capture("00-menu");Go(1);
                }
                else if(phase==1){Require(run.SelectVehicle(R3VehicleKind.Heavy)&&run.PrepareReactorRun(),"Actual preparation garage");Go(2);}
                else if(phase==2)
                {
                    Require(run.Progress.State==R1RunState.Garage&&!run.SandboxWorkshop&&run.VehicleModel.Scrap==0,"No sandbox budget in defense");Capture("01-garage");
                    Require(run.ChooseReactorDefense(),"Player-reachable defense entry");Go(3);
                }
                else if(phase==3)
                {
                    if(run.Transitioning)return;Require(run.ReactorActive&&run.ReactorHp==600&&run.DefenseWave==1&&run.Encounter.AliveCount==2,"Actual600HP reactor and first of3 waves");Capture("02-first-wave");Go(4,.4f);
                }
                else if(phase==4)
                {
                    run.Player.MoveTo(new Vector3(15,.7f,-17),Quaternion.identity);run.Player.Controller.SetGameplayInputEnabled(false);
                    Place(run.Encounter.Enemies[0],new Vector3(-16,0,-13),run.ReactorActor.transform.position);Place(run.Encounter.Enemies[1],new Vector3(15,0,-5),run.Player.transform.position);
                    Physics.SyncTransforms();playerHealth=Health(run);run.Encounter.SetCombatActive(true);Go(5,5);
                }
                else if(phase==5)
                {
                    Require(run.ReactorHp<600&&Health(run)<playerHealth,"Real enemy projectiles damage both targets",$"reactor={run.ReactorHp}, player={playerHealth}->{Health(run)}");
                    Require(run.Encounter.Enemies[0].EncounterTarget==run.ReactorActor.transform&&run.Encounter.Enemies[1].EncounterTarget==run.Player.transform,"Actual AI target policy");
                    Capture("03-real-ai-damage");run.Encounter.SetCombatActive(false);ClearWave();expectedWave=2;Go(6);
                }
                else if(phase==6)
                {
                    Require(run.Progress.State==R1RunState.Combat&&run.VehicleModel.Scrap==0,"No intermediate-wave reward");
                    if(run.DefenseWave!=expectedWave)return;
                    Require(run.Encounter.AliveCount==(expectedWave==3?3:2),"Next real wave"+expectedWave);Capture("04-wave"+expectedWave);
                    run.Encounter.SetCombatActive(false);ClearWave();if(expectedWave==2){expectedWave=3;Go(6);}else Go(7);
                }
                else if(phase==7)
                {
                    if(run.Progress.State!=R1RunState.Salvage)return;
                    Require(run.DefeatedEnemies==7&&run.Progress.CompletedEncounters==1&&run.VehicleModel.Scrap==run.CurrentEncounter.ScrapReward,"Three-wave win settles authored reward once");
                    Require(!run.ReactorActive&&run.LastActivityWasReactor,"Winning objective cleans up");Capture("05-defense-won");Go(8);
                }
                else if(phase==8)
                {
                    var loot=run.Salvage.Items.First(p=>p.Selectable);Require(run.ConfirmRewards(new[]{loot.Id},new[]{0}),"Existing1bonus/loot settlement");Go(9);
                }
                else if(phase==9)
                {
                    Require(run.Progress.State==R1RunState.Garage&&run.VehicleModel.Inventory.Count==2,"Rewards reach actual garage");Capture("06-reward-garage");
                    Require(run.StartNextEncounter()&&run.ChooseRoute(0),"Return to standard route");Go(10);
                }
                else if(phase==10)
                {
                    if(run.Transitioning)return;Require(!run.ReactorActive&&!run.LastActivityWasReactor&&FindObjectsByType<OBJ001ReactorActor>(FindObjectsSortMode.None).Length==0,"Ordinary fight has no stale objective");
                    Capture("07-standard-fight");previous=run;run.RestartRun();Go(11);
                }
                else if(phase==11)
                {
                    if(R1RunController.Instance==previous||R1RunController.Instance?.Progress==null)return;run=R1RunController.Instance;
                    Require(run.Progress.State==R1RunState.Ready&&FindObjectsByType<OBJ001ReactorActor>(FindObjectsSortMode.None).Length==0,"Scene restart clears activity");
                    Require(run.PrepareReactorRun()&&run.ChooseReactorDefense(),"Fresh defense after restart");Go(12);
                }
                else if(phase==12)
                {
                    if(run.Transitioning)return;Require(run.ReactorHp==600&&run.DefenseWave==1,"New attempt restores only new objective state");
                    run.ReactorActor.Damageable.ApplyDamage(100000,run.ReactorActor.transform.position,Vector3.zero);Go(13);
                }
                else if(phase==13)
                {
                    Require(run.Progress.State==R1RunState.Lost&&run.Progress.DefeatReason.Contains("Reaktor")&&run.VehicleModel.Scrap==0,"Canonical reactor death loses without reward");Capture("08-reactor-lost");
                    previous=run;run.RestartRun();Go(14);
                }
                else if(phase==14)
                {
                    if(R1RunController.Instance==previous||R1RunController.Instance?.Progress==null)return;run=R1RunController.Instance;
                    Require(run.Progress.State==R1RunState.Ready&&!run.ReactorActive&&FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None).Length==0,"Loss restart removes objective and shots");Capture("09-restart-menu");Finish(true);
                }
            }
            catch(Exception error){result.errors.Add(error.ToString());Finish(false);}
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
#endif

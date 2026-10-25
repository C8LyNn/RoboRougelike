#if B01_RC1_QA
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using B01.MAPGENWORLD001;
using RoboRougelike.R3;
using RoboRougelike.R6;
using UnityEngine;

namespace RoboRougelike.R1
{
    // Separate finite API/PhysX acceptance runner, completely excluded from retail.
    [DefaultExecutionOrder(2000)]
    public sealed class B01RC1RuntimeQA : MonoBehaviour
    {
        [Serializable] public sealed class Check {public string name,detail;public bool passed;}
        [Serializable] public sealed class Result
        {
            public string status="Running",method="API-controlled real R3/R6 forces and real AI/player projectiles; explicit core-damage win/loss settlement; NOT human gameplay or an unassisted combat victory",startedUtc,finishedUtc,graphics;
            public int width,height,contactSamples,supportedSamples,maxContacts;public float distance,playerHpLost,enemyHpLost;
            public Vector3 start,last,measuredEnvelope;public string firstHash,secondHash;
            public List<Check> checks=new List<Check>();public List<string> errors=new List<string>();
        }
        readonly Result result=new Result();string output;StreamWriter trace;R1RunController run,oldRun;
        Rigidbody body;R6VehicleDrive drive;R3VehicleAssembly assembly;GameObject legacyFloor;
        MapWorldSnapshot first;int phase;float readyAt,deadline,phaseStarted,initialPlayerHp,initialEnemyHp,lastTrace;
        Vector3 previous,goal;bool quitting;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rc1-native-output");if(i<0||i+1>=args.Length)return;
            var root=new GameObject("B01 RC1 explicit API QA");DontDestroyOnLoad(root);root.AddComponent<B01RC1RuntimeQA>().Initialize(args[i+1]);
        }
        void Initialize(string path)
        {
            output=path;Directory.CreateDirectory(path);result.startedUtc=DateTime.UtcNow.ToString("o");
            result.graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;
            Application.runInBackground=true;Application.targetFrameRate=60;deadline=Time.realtimeSinceStartup+180;
            trace=new StreamWriter(Path.Combine(path,"drive.csv"));trace.WriteLine("phase,time,x,y,z,speed,contacts,wheels,aliveEnemies");
            Application.logMessageReceived+=OnLog;Save();
        }
        void OnLog(string text,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&result.errors.Count<25){result.errors.Add(text);Save();}}
        void Save(){if(output!=null)File.WriteAllText(Path.Combine(output,"rc1-native-result.json"),JsonUtility.ToJson(result,true));}
        void Require(bool ok,string name,string detail=""){result.checks.Add(new Check{passed=ok,name=name,detail=detail});Save();if(!ok)throw new InvalidOperationException(name+": "+detail);}
        void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
        void Go(int next,float delay=.25f){phase=next;phaseStarted=Time.realtimeSinceStartup;readyAt=phaseStarted+delay;Save();}
        static float PlayerHp(R1RunController r)=>r.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p!=null&&p.Damageable!=null).Sum(p=>p.Damageable.CurrentHp);
        float EnemyHp()=>run.Encounter.Enemies.Where(e=>e!=null).Sum(e=>e.GetComponentsInChildren<RogueMachine.P12PipelineSafe.P12PipelineSafeDamageable>().Sum(h=>h.CurrentHp));
        void BindDrive()
        {
            run.Player.Controller.SetGameplayInputEnabled(true);run.Player.Controller.enabled=false;
            body=run.Player.GetComponent<Rigidbody>();drive=run.Player.GetComponent<R6VehicleDrive>();assembly=run.Player.GetComponent<R3VehicleAssembly>();
            previous=body.position;goal=run.GeneratedWorld.Finish;
        }
        void CheckRendering()
        {
            var renderers=run.GeneratedWorld.GeneratedRoot.GetComponentsInChildren<Renderer>();
            Require(renderers.Length>0&&renderers.All(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.isSupported&&r.sharedMaterial.shader.name=="B01/MAPGEN001/BlocksURP"),"Actual source Resources shader renders generated arena");
        }
        void Update()
        {
            if(output==null||quitting||Time.realtimeSinceStartup<readyAt)return;
            try
            {
                if(phase==99){quitting=true;trace?.Dispose();Application.Quit(result.status=="Passed"?0:1);return;}
                if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("RC1 API QA deadline at phase "+phase);
                if(phase==0)
                {
                    if(R1RunController.Instance?.Progress==null)return;run=R1RunController.Instance;
                    result.width=Screen.width;result.height=Screen.height;legacyFloor=GameObject.Find("P12_Ground");
                    Require(run.Progress.State==R1RunState.Ready,"Real game menu");Capture("00-menu");Go(1);
                }
                else if(phase==1){run.SelectVehicle(R3VehicleKind.Heavy);run.RequestedSeed=7;Require(run.PrepareMapgenRun(),"Player-accessible MAPGEN preparation");Go(2);}
                else if(phase==2){Require(run.Progress.State==R1RunState.Garage&&run.VehicleModel.Scrap==0&&!run.SandboxWorkshop,"Ordinary preparation economy");Capture("01-garage");Go(3);}
                else if(phase==3){Require(run.ChooseGeneratedArena(),"Generated activity selected through actual run API");Go(4);}
                else if(phase==4)
                {
                    if(run.Transitioning)return;first=run.GeneratedWorld;result.firstHash=first.GeometryHash;result.measuredEnvelope=run.MapgenMeasuredEnvelope;
                    Require(first.Seed==7&&first.GeneratedRoot.activeInHierarchy&&!legacyFloor.activeSelf,"Seed7 map with legacy floor disabled");
                    Require(run.Encounter.AliveCount==2&&!run.ReactorActive,"Canonical enemies on source map spawns");CheckRendering();BindDrive();
                    result.start=body.position;initialPlayerHp=PlayerHp(run);initialEnemyHp=EnemyHp();Capture("02-generated-start");Go(5,.8f);
                }
                else if(phase==5)
                {
                    RequireLiveCombat();
                    var target=run.Encounter.Enemies.Where(e=>e!=null&&!e.IsDefeated).OrderBy(e=>(e.transform.position-body.position).sqrMagnitude).FirstOrDefault();
                    if(target!=null){assembly.AimGuns(target.Core.transform.position);assembly.FireGuns();}
                    result.playerHpLost=initialPlayerHp-PlayerHp(run);result.enemyHpLost=initialEnemyHp-EnemyHp();
                    if(result.distance>8&&result.playerHpLost>0&&result.enemyHpLost>0)
                    {
                        Require(result.maxContacts>0&&body.constraints==RigidbodyConstraints.None,"Real R6 grounded force drive",$"distance={result.distance:F3}, contacts={result.maxContacts}");
                        Require(true,"Real hostile and player projectile damage",$"playerLost={result.playerHpLost:F1}, enemyLost={result.enemyHpLost:F1}");
                        Capture("03-real-drive-projectile-damage");Go(6);
                    }
                    else if(Time.realtimeSinceStartup-phaseStarted>40)throw new TimeoutException("Real movement/projectile criteria were not met in40s");
                }
                else if(phase==6)
                {
                    run.Encounter.SetCombatActive(false);
                    foreach(var e in run.Encounter.Enemies.ToArray())e.Core.ApplyDamage(100000,e.Core.transform.position,Vector3.zero);
                    Go(7);
                }
                else if(phase==7)
                {
                    if(run.Progress.State!=R1RunState.Salvage)return;
                    Require(run.GeneratedWorld==null&&legacyFloor.activeSelf&&(first.GeneratedRoot==null||!first.GeneratedRoot.activeSelf),"Controlled win uses existing salvage and clears generated world");
                    Capture("04-rewards");Go(8);
                }
                else if(phase==8)
                {
                    var loot=run.Salvage.Items.First(p=>p.Selectable);Require(run.ConfirmRewards(new[]{loot.Id},new[]{0}),"One bonus plus real enemy loot claimed");Go(9);
                }
                else if(phase==9)
                {
                    Require(run.Progress.State==R1RunState.Garage&&run.VehicleModel.Inventory.Count==2,"Rewards return to actual garage");
                    // A separate explicitly induced small part injury makes the repair check deterministic.
                    var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor&&p.Operational&&p.Damageable.CurrentHp>3);
                    part.Damageable.ApplyDamage(2,part.transform.position,Vector3.zero);run.Player.GetComponent<R3VehicleAssembly>().Capture();
                    Require(run.GarageRepair(part.Cell),"Canonical repair after explicit2HP fixture injury");
                    var owned=run.VehicleModel.Inventory.First(p=>p.Health>0);bool mounted=false;
                    for(int y=2;y<=5&&!mounted;y++)for(int z=-3;z<=3&&!mounted;z++)for(int x=-2;x<=2&&!mounted;x++)
                    {
                        var cell=new Vector3Int(x,y,z);
                        if(run.VehicleModel.PlacementError(cell,owned.Kind,true,owned.Weapon,owned.RotationQuarterTurns,owned.ContactMask)==null)
                            mounted=run.GarageMount(owned.Id,cell,owned.RotationQuarterTurns);
                    }
                    Require(mounted,"Recovered module mounted through canonical garage");Go(10);
                }
                else if(phase==10){Capture("05-repaired-mounted-garage");Go(11);}
                else if(phase==11){run.RequestedSeed=31;Require(run.ChooseGeneratedArena(),"Second generated activity after rewards");Go(12);}
                else if(phase==12)
                {
                    if(run.Transitioning)return;result.secondHash=run.GeneratedWorld.GeometryHash;
                    Require(run.GeneratedWorld.Seed==31&&result.secondHash!=result.firstHash&&first.GeneratedRoot==null,"New seed regenerates map without old root");CheckRendering();Capture("06-regenerated31");Go(13);
                }
                else if(phase==13){run.Player.Core.ApplyDamage(100000,run.Player.Core.transform.position,Vector3.zero);Go(14);}
                else if(phase==14)
                {
                    if(run.Progress.State!=R1RunState.Lost)return;Require(run.GeneratedWorld==null&&legacyFloor.activeSelf,"Controlled player loss cleans map and restores normal terrain");
                    Capture("07-defeat");oldRun=run;Go(15);
                }
                else if(phase==15){run.RestartRun();Go(16);}
                else if(phase==16)
                {
                    if(R1RunController.Instance==oldRun||R1RunController.Instance?.Progress==null)return;run=R1RunController.Instance;
                    Require(run.Progress.State==R1RunState.Ready&&run.GeneratedWorld==null,"Scene restart returns to menu without stale map");
                    Require(Resources.FindObjectsOfTypeAll<Material>().Count(m=>m.name=="MAPGEN001_Generated_URP_Blocks_Runtime")==0,"Restart releases owned generated material");
                    Capture("08-restarted-menu");Finish(true);
                }
            }
            catch(Exception ex){result.errors.Add(ex.ToString());Finish(false);}
        }
        void RequireLiveCombat(){if(run.Progress.State!=R1RunState.Combat||run.Player.DefeatReason!=null)throw new InvalidOperationException("Combat ended before explicit settlement: "+run.Progress.State);}
        void FixedUpdate()
        {
            if(phase!=5||Time.realtimeSinceStartup<readyAt||body==null||run.Progress.State!=R1RunState.Combat)return;
            var to=goal-body.position;to.y=0;
            float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(body.transform.forward,Vector3.up),to,Vector3.up);
            drive.Step(to.magnitude>2.4f&&Mathf.Abs(angle)<72?1:0,Mathf.Clamp(angle/35-body.angularVelocity.y*.2f,-1,1),run.Player.Controller.EffectiveForwardSpeed,3,90);
            result.distance+=Vector3.Distance(previous,body.position);previous=result.last=body.position;
            result.contactSamples++;if(drive.GroundedWheels>0)result.supportedSamples++;result.maxContacts=Mathf.Max(result.maxContacts,drive.GroundedWheels);
            if(body.position.y< -3){result.errors.Add("Vehicle fell below source floor");Finish(false);}
            if(Time.time-lastTrace>.1f){lastTrace=Time.time;trace.WriteLine(string.Join(",",phase,Time.time.ToString("F3",CultureInfo.InvariantCulture),body.position.x.ToString("F3",CultureInfo.InvariantCulture),body.position.y.ToString("F3",CultureInfo.InvariantCulture),body.position.z.ToString("F3",CultureInfo.InvariantCulture),body.linearVelocity.magnitude.ToString("F3",CultureInfo.InvariantCulture),drive.GroundedWheels,drive.Wheels,run.Encounter.AliveCount));trace.Flush();}
        }
        void Finish(bool ok){result.status=ok&&result.errors.Count==0?"Passed":"Failed";result.finishedUtc=DateTime.UtcNow.ToString("o");Go(99,.4f);}
        void OnDestroy(){Application.logMessageReceived-=OnLog;trace?.Dispose();}
    }
}
#endif

#if MOVSERIES_VISUAL_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R6;
using RoboRougelike.Mechanics.COV001;
using RoboRougelike.Mechanics.HAZ001;
using RoboRougelike.Mechanics.Rockets;
using RoboRougelike.VFX001;
using RoboRougelike.VFX001Integration;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.QA
{
    // Explicit API/physics fixture, excluded from retail. Does not count as player gameplay.
    public sealed class RRLW7RuntimeQA : MonoBehaviour
    {
        [Serializable] public sealed class Check {public string id,name,detail;public bool passed;}
        [Serializable] public sealed class Result
        {
            public string method="Windows native API/physics fixture; controlled placement, damage APIs and actual projectile flight; NOT manual gameplay",status="Running",startedUtc,finishedUtc,graphics;
            public int width,height;public List<Check> checks=new List<Check>();public List<string> errors=new List<string>();
        }
        readonly Result result=new Result();string output,current;R1RunController run;VfxPoolManager pool;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-rrl-w7-native-api-output");if(at<0||at+1>=args.Length)return;
            var root=new GameObject("Explicit W7 native API fixture");DontDestroyOnLoad(root);root.AddComponent<RRLW7RuntimeQA>().Initialize(args[at+1]);
        }
        void Initialize(string path)
        {
            output=path;Directory.CreateDirectory(path);result.startedUtc=DateTime.UtcNow.ToString("o");result.graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;
            Application.runInBackground=true;Application.targetFrameRate=60;Application.logMessageReceived+=OnLog;Save();
        }
        void OnLog(string message,string stack,LogType type){if((type==LogType.Error||type==LogType.Assert||type==LogType.Exception)&&result.errors.Count<30){result.errors.Add(message);Save();}}
        void Save(){if(output!=null)File.WriteAllText(Path.Combine(output,"native-api-result.json"),JsonUtility.ToJson(result,true));}
        void Require(bool ok,string name,string detail=""){result.checks.Add(new Check{id=current,name=name,passed=ok,detail=detail});Save();if(!ok)throw new InvalidOperationException(current+": "+name+" "+detail);}
        void Capture(string name){ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));}
        bool Wave(float green)
        {
            var properties=new MaterialPropertyBlock();foreach(var ring in pool.GetComponentsInChildren<VfxShockwaveRing>())
            {if(!ring.IsRunning)continue;ring.GetComponent<Renderer>().GetPropertyBlock(properties);if(Mathf.Abs(properties.GetColor("_Tint").g-green)<.001f)return true;}return false;
        }
        IEnumerator Start()
        {
            var routine=Run();while(true){object step=null;bool more=false;try{more=routine.MoveNext();if(more)step=routine.Current;}catch(Exception error){result.errors.Add(error.ToString());Save();break;}if(!more)break;yield return step;}
            result.status=result.errors.Count==0&&result.checks.Count>0&&result.checks.All(c=>c.passed)?"Passed":"Failed";result.finishedUtc=DateTime.UtcNow.ToString("o");Save();yield return null;Application.Quit(result.status=="Passed"?0:1);
        }
        IEnumerator Run()
        {
            float deadline=Time.realtimeSinceStartup+30;while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
            current="W7";run=R1RunController.Instance;Require(run?.Progress!=null,"Real gameplay scene initialized");result.width=Screen.width;result.height=Screen.height;
            Require(run.StartRun()&&run.ChooseRoute(0),"Actual expedition start");while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);R1EncounterManager.ClearTransientObjects();
            pool=run.GetComponentInChildren<VfxPoolManager>();Require(pool!=null,"Original pool installed in real run");
            current="COV-001";
            var covers=run.Arena.Layout.GetComponentsInChildren<DestructibleArenaCover>();Require(covers.Length==6,"Actual salvage yard covers bound",covers.Length.ToString());
            var cover=covers.OrderBy(c=>Vector3.Distance(run.Player.transform.position,c.transform.position)).First();var collider=cover.GetComponent<Collider>();
            var gun=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Gun);gun.GunPivot.rotation=Quaternion.LookRotation(collider.bounds.center-gun.Muzzle.position);Physics.SyncTransforms();
            run.Player.Controller.FireFrom(gun.Muzzle,gun.Weapon,gun.Id);deadline=Time.time+2;while(cover.Durability==50&&Time.time<deadline)yield return null;
            Require(cover.Durability<50,"Actual P17 projectile damages contacted cover",cover.Durability.ToString());Capture("01-cover-hit");
            Require(RRLHitRouter.Apply(collider,1000,collider.bounds.center,Vector3.forward,run.Player.transform)&&cover.Broken&&!collider.enabled&&!cover.GetComponent<Renderer>().enabled,"Shared hit breaks real blocker and mesh");
            cover.ResetCover();Require(collider.enabled&&cover.GetComponent<Renderer>().enabled&&cover.Durability==50,"Reset restores original durability and collision");
            current="HAZ-001";
            var zone=run.Arena.Layout.GetComponentsInChildren<TelegraphedHazardZone>()[0];run.Player.MoveTo(new Vector3(zone.transform.position.x,run.Player.transform.position.y,zone.transform.position.z),Quaternion.identity);Physics.SyncTransforms();zone.ResetCycle();Require(zone.Begin(),"Original hazard cycle begins");
            var parts=run.Player.GetComponentsInChildren<P12PipelineSafeDamageable>();float before=parts.Sum(p=>p.CurrentHp);yield return new WaitForSeconds(.25f);
            Require(zone.Phase==HazardPhase.Telegraph&&zone.GetComponentInChildren<LineRenderer>().startColor==Color.yellow&&parts.Sum(p=>p.CurrentHp)==before,"Automatic warning is visible and harmless before activation");Capture("02-hazard-warning");
            deadline=Time.time+2;while(parts.Sum(p=>p.CurrentHp)==before&&Time.time<deadline)yield return null;
            Require(Mathf.Abs(before-parts.Sum(p=>p.CurrentHp)-5)<.01f&&zone.Phase==HazardPhase.Active&&!zone.LastScanTruncated,"Automatic active phase damages exactly one living part",(before-parts.Sum(p=>p.CurrentHp)).ToString());Capture("03-hazard-active");
            run.TogglePause();before=parts.Sum(p=>p.CurrentHp);zone.ScanAtTime(Time.time+10);yield return new WaitForSecondsRealtime(.6f);Require(parts.Sum(p=>p.CurrentHp)==before,"Pause blocks source clock and explicit damage requests");run.TogglePause();run.Encounter.SetCombatActive(false);
            foreach(var item in run.Arena.Layout.GetComponentsInChildren<TelegraphedHazardZone>())item.enabled=false;
            current="VFX-001-v5";yield return null;pool.ClearAll();var armor=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor).Damageable;
            var body=run.Player.GetComponent<Rigidbody>();float mass=body.mass;var position=body.position;var velocity=body.linearVelocity;before=armor.CurrentHp;armor.ApplyDamage(10,armor.transform.position,Vector3.up);
            Require(pool.ActiveCount==1&&armor.CurrentHp==before-10&&body.mass==mass&&body.position==position&&body.linearVelocity==velocity,"Settled hit emits once without changing gameplay physics");
            Require(pool.GetComponentsInChildren<Collider>(true).Length==0&&pool.GetComponentsInChildren<Rigidbody>(true).Length==0,"Cosmetic pool contains no physics bodies or colliders");
            yield return null;pool.ClearAll();R6CombatFeedback.Muzzle(gun.Muzzle,gun.Weapon);yield return null;yield return null;Require(pool.ActiveCount==0,"Muzzle flash is not a hit");
            var core=run.Encounter.Enemies[0].Core;core.ApplyDamage(100000,core.transform.position,Vector3.up);Require(Wave(.38f),"Real designated core uses original core shockwave");Capture("04-core-destruction");
            yield return null;pool.ClearAll();var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=R4WeaponKind.RocketLauncher;
            var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100,4,100),Quaternion.identity);var rocketGun=vehicle.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==state.Id);
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.position=rocketGun.Muzzle.position+rocketGun.Muzzle.forward*6;target.transform.localScale=new Vector3(2,2,.5f);var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);Physics.SyncTransforms();
            Require(rocketGun.GetComponent<RocketLauncher>().TryFire()==RocketFireResult.Fired,"Actual original rocket launch");deadline=Time.time+.6f;while(hp.CurrentHp==500&&Time.time<deadline)yield return null;
            Require(hp.CurrentHp>=440&&hp.CurrentHp<=450&&!hp.IsDead&&Wave(.72f),"Nonlethal native rocket impact produces explosion shockwave",hp.CurrentHp.ToString());Destroy(vehicle.gameObject);Destroy(target);
            var bridge=run.GetComponent<Vfx001R6LiveBridge>();bridge.SetVisualEffectsEnabled(false);Require(pool.ActiveCount==0,"Disabling effects clears all active bursts");armor.ApplyDamage(1,armor.transform.position,Vector3.up);yield return null;bridge.SetVisualEffectsEnabled(true);yield return null;Require(pool.ActiveCount==0,"Re-enabling does not replay old impacts or fragments");
            foreach(var enemy in run.Encounter.Enemies)if(!enemy.IsDefeated)enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
            while(run.Progress.State==R1RunState.Combat)yield return null;yield return null;
            Require(pool.ActiveCount==0&&!zone.enabled&&zone.Phase==HazardPhase.Idle,"Reward transition clears VFX and disables hazard");
            run.RestartRun();yield return null;while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;Require(run.GetComponents<Vfx001R6LiveBridge>().Length==1,"Scene restart installs exactly one new bridge");
            Require(run.StartRun()&&run.ChooseRoute(0),"Restarted expedition enters combat");while(run.Transitioning)yield return null;run.Encounter.SetCombatActive(false);pool=run.GetComponentInChildren<VfxPoolManager>();armor=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor).Damageable;armor.ApplyDamage(1,armor.transform.position,Vector3.up);Require(pool.ActiveCount==1,"Fresh run owns working hit subscription");yield return null;
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
#endif

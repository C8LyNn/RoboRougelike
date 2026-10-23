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
using RoboRougelike.WeaponIntegration;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.Mechanics.Rockets;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.QA
{
    // Explicit native API fixture, entirely excluded from retail. Never a campaign win.
    public sealed class RRLW6RuntimeQA : MonoBehaviour
    {
        [Serializable] public sealed class Check {public string id,name,detail;public bool passed;}
        [Serializable] public sealed class Result
        {
            public string method="Windows native API/physics fixture; temporary actors and damage APIs; NOT manual gameplay",status="Running",startedUtc,finishedUtc,graphics;
            public List<Check> checks=new List<Check>();public List<string> errors=new List<string>();
        }
        sealed class Rig {public R3VehicleModel model;public R1VehicleState vehicle;public R3VehicleAssembly assembly;public R3MountedPart gun,device;}
        readonly Result result=new Result();readonly List<GameObject> owned=new List<GameObject>();
        string output,current;int sequence;R1RunController run;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-rrl-w6-native-api-output");
            if(at<0||at+1>=args.Length)return;
            var root=new GameObject("Explicit W6 native API fixture");DontDestroyOnLoad(root);
            root.AddComponent<RRLW6RuntimeQA>().Initialize(args[at+1]);
        }
        void Initialize(string path)
        {
            output=path;Directory.CreateDirectory(path);result.startedUtc=DateTime.UtcNow.ToString("o");
            result.graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;
            Application.runInBackground=true;Application.targetFrameRate=60;Application.logMessageReceived+=OnLog;Save();
        }
        void OnLog(string message,string stack,LogType type)
        {if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&result.errors.Count<30){result.errors.Add(message);Save();}}
        void Save(){if(output!=null)File.WriteAllText(Path.Combine(output,"native-api-result.json"),JsonUtility.ToJson(result,true));}
        void Require(bool ok,string name,string detail="")
        {
            result.checks.Add(new Check{id=current,name=name,passed=ok,detail=detail});Save();
            if(!ok)throw new InvalidOperationException(current+": "+name+" "+detail);
        }
        IEnumerator Start()
        {
            while(R1RunController.Instance?.Progress==null)yield return null;run=R1RunController.Instance;
            Time.timeScale=1;
            var jobs=new List<(string id,Func<IEnumerator> start)>{
                ("WPN-001",()=>Weapon(R4WeaponKind.Shotgun)),("WPN-002",()=>Weapon(R4WeaponKind.Railgun)),
                ("WPN-003",()=>Weapon(R4WeaponKind.BeamCutter)),("WPN-004",()=>Weapon(R4WeaponKind.RocketLauncher)),
                ("WPN-005",()=>Weapon(R4WeaponKind.RotarySaw)),("WPN-006",()=>Weapon(R4WeaponKind.EmpEmitter)),
                ("DEF-001",Shield),("DEF-002",PointDefense)};
            foreach(var job in jobs)
            {
                current=job.id;var routine=job.start();
                while(true)
                {
                    object step=null;bool more=false;
                    try{more=routine.MoveNext();if(more)step=routine.Current;}
                    catch(Exception error){result.errors.Add(current+": "+error);Save();break;}
                    if(!more)break;yield return step;
                }
                foreach(var obj in owned)if(obj!=null){obj.SetActive(false);Destroy(obj);}owned.Clear();
                R1EncounterManager.ClearTransientObjects();yield return null;
            }
            result.status=result.errors.Count==0&&result.checks.All(c=>c.passed)?"Passed":"Failed";
            result.finishedUtc=DateTime.UtcNow.ToString("o");Save();Application.Quit(result.status=="Passed"?0:1);
        }
        Rig Build(R4WeaponKind weapon,R3PartKind? defense=null,R3VehicleModel restored=null)
        {
            var model=restored??new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));
            if(restored==null)
            {
                model.Award(1000);model.Parts.First(p=>p.Kind==R3PartKind.Gun).Weapon=weapon;
                if(defense.HasValue)Require(model.Buy(new Vector3Int(0,1,-2),defense.Value),"Physical garage admission");
            }
            var vehicle=R3VehicleAssembly.Build(model,new R1RunStats(),run.GetComponent<R3VisualPalette>(),new Vector3(100+sequence++*30,4,100),Quaternion.identity);
            vehicle.Controller.SetGameplayInputEnabled(false);owned.Add(vehicle.gameObject);
            var assembly=vehicle.GetComponent<R3VehicleAssembly>();
            return new Rig{model=model,vehicle=vehicle,assembly=assembly,gun=assembly.Parts.Single(p=>p.Kind==R3PartKind.Gun),
                device=defense.HasValue?assembly.Parts.Single(p=>p.Kind==defense.Value):null};
        }
        P12PipelineSafeDamageable Target(Vector3 position,bool saw)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);owned.Add(obj);obj.name="Temporary native API damage target";
            obj.transform.position=position;obj.transform.localScale=saw?Vector3.one*.2f:new Vector3(2,2,.5f);
            var hp=obj.AddComponent<P12PipelineSafeDamageable>();hp.Configure(500,false);return hp;
        }
        IEnumerator Weapon(R4WeaponKind kind)
        {
            var rig=Build(kind);yield return null;yield return null;
            var part=rig.gun;var target=Target(part.Muzzle.position+part.Muzzle.forward*(kind==R4WeaponKind.RotarySaw?.45f:4),kind==R4WeaponKind.RotarySaw);
            EmpReceiver receiver=null;
            if(kind==R4WeaponKind.EmpEmitter){receiver=target.gameObject.AddComponent<EmpReceiver>();receiver.ConfigureOwnership(target.transform,9000+sequence,2);}
            Physics.SyncTransforms();yield return new WaitForFixedUpdate();
            var body=rig.vehicle.GetComponent<Rigidbody>();var velocity=body.linearVelocity;bool fired=true;
            if(kind==R4WeaponKind.Shotgun)fired=part.GetComponent<RRLShotgunBinding>().Fire();
            else if(kind==R4WeaponKind.Railgun)fired=part.GetComponent<RRLRailgunBinding>().Fire();
            else if(kind==R4WeaponKind.RocketLauncher)fired=part.GetComponent<RRLRocketBinding>().Fire();
            else if(kind==R4WeaponKind.EmpEmitter)fired=part.GetComponent<RRLEmpBinding>().Fire();
            else if(kind==R4WeaponKind.BeamCutter)part.GetComponent<RRLBeamBinding>().SetTriggerHeld(true);
            else part.GetComponent<RRLSawBinding>().SetMotorRequested(true);
            Require(fired,"Actual production binding activation");
            if(receiver!=null)
                Require(receiver.IsDisrupted&&target.CurrentHp==500&&body.linearVelocity==velocity,"Original EMP changes status without HP/momentum",receiver.RemainingSeconds.ToString("0.000"));
            yield return new WaitForSeconds(1);
            if(kind==R4WeaponKind.BeamCutter)part.GetComponent<RRLBeamBinding>().SetTriggerHeld(false);
            if(kind==R4WeaponKind.RotarySaw)part.GetComponent<RRLSawBinding>().SetMotorRequested(false);
            if(receiver==null)Require(target.CurrentHp<500,"Real native physics/ray/contact damage",(500-target.CurrentHp).ToString("0.000"));
            else{yield return new WaitForSeconds(3);Require(!receiver.IsDisrupted,"Original automatic EMP recovery");}
            int expected=kind==R4WeaponKind.RocketLauncher?9:11;
            if(kind==R4WeaponKind.Shotgun||kind==R4WeaponKind.Railgun||kind==R4WeaponKind.RocketLauncher)
                Require(rig.model.Parts.Single(p=>p.Id==part.Id).Ammo==expected,"Exactly one ammunition debit");
            part.Damageable.ApplyDamage(100000,part.transform.position,Vector3.up);rig.assembly.Capture();
            var state=rig.model.Parts.Single(p=>p.Id==part.Id);int ammo=state.Ammo;
            Require(state.Health==0,"Physical destruction captured");Require(rig.model.Repair(part.Cell),"Paid repair of original module");
            var rebuilt=Build(kind,null,rig.model.DeepCopy());yield return null;yield return null;
            var restored=rebuilt.model.Parts.Single(p=>p.Id==part.Id);
            Require(restored.Health==1&&restored.Weapon==kind&&restored.Ammo==ammo,"Identity and resources survive repair/rebuild");
        }
        IEnumerator Shield()
        {
            var rig=Build(R4WeaponKind.Basic,R3PartKind.DirectionalShield);yield return null;yield return null;
            var shield=rig.device.GetComponent<RRLShieldBinding>();var hp=rig.gun.Damageable;float before=hp.CurrentHp;
            hp.ApplyDamage(40,hp.transform.position+Vector3.forward*2,Vector3.back);
            Require(hp.CurrentHp==before&&shield.Generator.Charge<=60.01f,"Directional shield intercepts front hit before shared HP");
            hp.ApplyDamage(10,hp.transform.position-Vector3.forward*2,Vector3.forward);
            Require(hp.CurrentHp==before-10,"Rear hit reaches shared HP");
            rig.device.Damageable.ApplyDamage(100000,rig.device.transform.position-Vector3.forward*2,Vector3.forward);
            Require(!shield.Generator.Operational,"Destroyed physical shield stops protection");rig.assembly.Capture();
            float charge=rig.model.Parts.Single(p=>p.Id==rig.device.Id).DefenseCharge;
            Require(rig.model.Repair(rig.device.Cell),"Shield paid repair");var rebuilt=Build(R4WeaponKind.Basic,R3PartKind.DirectionalShield,rig.model.DeepCopy());
            yield return null;yield return null;
            Require(Mathf.Abs(rebuilt.device.GetComponent<RRLShieldBinding>().Generator.Charge-charge)<.01f,"Shield repair preserves charge");
        }
        IEnumerator PointDefense()
        {
            var rig=Build(R4WeaponKind.Basic,R3PartKind.PointDefense);yield return null;yield return null;
            var binding=rig.device.GetComponent<RRLPointDefenseBinding>();
            var template=(P12PipelineSafeEnemyTurret)typeof(R1RunController).GetField("enemyTemplate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(run);
            var enemy=Instantiate(template,rig.vehicle.transform.position+new Vector3(12,0,18),Quaternion.identity);owned.Add(enemy.gameObject);
            enemy.ConfigureArchetype(R3EnemyArchetype.Gunner);enemy.ConfigureSalvage(null,"native-api");enemy.gameObject.SetActive(true);
            enemy.ConfigureEncounter(rig.vehicle.transform,1,.35f,1000,1);enemy.ConfigureMovement(0,12,0);enemy.SetCombatActive(true);
            Require(enemy.EquipRocket(),"Actual enemy rocket equipment");enemy.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
            var bank=enemy.GetComponentInChildren<RRLRocketBinding>();
            var muzzle=new GameObject("Native API incoming rocket muzzle").transform;muzzle.SetParent(enemy.transform,false);
            muzzle.position=rig.device.transform.position+Vector3.up*1.2f+Vector3.forward*18;
            muzzle.rotation=Quaternion.LookRotation(rig.vehicle.transform.position+Vector3.up-muzzle.position);
            bank.Module.Bind(enemy.GetComponent<Rigidbody>(),enemy.transform,muzzle);Physics.SyncTransforms();Require(bank.Fire(),"Original incoming rocket fired");
            var rocket=FindObjectsByType<RocketProjectile>(FindObjectsSortMode.None).Single(p=>p.IsInitialized&&!p.IsResolved);
            float hp=rig.device.Damageable.CurrentHp;yield return new WaitForSeconds(1.2f);
            Require(rocket.HitPoints<=0&&binding.Controller.Ammunition<40,"Real interceptors deplete original rocket HP",rocket.HitPoints+" / ammo "+binding.Controller.Ammunition);
            Require(rig.device.Damageable.CurrentHp==hp,"Intercepted rocket does not damage defense");
            rig.device.Damageable.ApplyDamage(100000,rig.device.transform.position,Vector3.up);rig.assembly.Capture();
            int ammo=rig.model.Parts.Single(p=>p.Id==rig.device.Id).Ammo;
            Require(!binding.Controller.IsOperational&&rig.model.Repair(rig.device.Cell),"Destroyed PDS stops and can be repaired");
            var rebuilt=Build(R4WeaponKind.Basic,R3PartKind.PointDefense,rig.model.DeepCopy());yield return null;yield return null;
            Require(rebuilt.device.GetComponent<RRLPointDefenseBinding>().Controller.Ammunition==ammo,"PDS paid repair preserves finite ammunition");
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
#endif

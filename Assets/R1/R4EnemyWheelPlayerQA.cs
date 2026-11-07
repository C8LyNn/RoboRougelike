using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.R4_1;
using RoboRougelike.R5;
using RoboRougelike.R6;
using RoboRougelike.Mechanics.EMP;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.R1
{
    // Explicit batch-only verification; it does not read or write a run save.
    public sealed class R4EnemyWheelPlayerQA : MonoBehaviour
    {
        [Serializable] sealed class Result
        {
            public string status="RUNNING",error="",method="Windows Player, API-driven; not human gameplay";
            public int enemies,wheels;
            public bool enteredCombat,modelsPresent,originalColliders,detachedVisual;
            public bool eliteArt,eliteVisibleMuzzle,eliteHit,automaticFallback;
            public bool primaryArt,primaryVisibleMuzzle,primaryHit,primarySwap;
            public bool shotgunHit;
            public bool railgunArt,railgunMuzzle,railgunHit;
            public bool rocketArt,rocketHit,beamArt,beamHit;
            public bool sawArt,sawHit,empArt,empPulse;
        }
        string directory;
        readonly Result result=new Result();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-r4-enemy-wheel-qa");
            if(i<0||i+1>=args.Length||!Application.isBatchMode)return;
            var go=new GameObject("Explicit R4 enemy wheel Player QA");DontDestroyOnLoad(go);
            go.AddComponent<R4EnemyWheelPlayerQA>().directory=args[i+1];
        }
        void Start(){Directory.CreateDirectory(directory);Application.runInBackground=true;StartCoroutine(Run());}
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        IEnumerator Run()
        {
            var flow=Exercise();bool ok=true;
            while(true)
            {
                bool next=false;object current=null;
                try{next=flow.MoveNext();if(next)current=flow.Current;}
                catch(Exception error){ok=false;result.error=error.ToString();}
                if(!next||!ok)break;
                yield return current;
            }
            result.status=ok?"PASS":"FAIL";
            File.WriteAllText(Path.Combine(directory,"r4-enemy-wheel-player.json"),JsonUtility.ToJson(result,true));
            Application.Quit(ok?0:1);
        }
        IEnumerator Exercise()
        {
            float deadline=Time.realtimeSinceStartup+45;
            while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
            var run=R1RunController.Instance;Require(run!=null&&run.Progress!=null,"Game bootstrap");
            Require(run.PrepareUnifiedRun(),"Main menu to garage");
            Require(run.StartNextEncounter()&&run.ChooseRoute(0),"Start activity");
            deadline=Time.realtimeSinceStartup+45;
            while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            result.enteredCombat=!run.Transitioning&&run.Progress.State==R1RunState.Combat;
            Require(result.enteredCombat,"Entered combat");run.Encounter.SetCombatActive(false);
            var enemies=run.Encounter.Enemies.Where(e=>e!=null).ToArray();result.enemies=enemies.Length;
            var wheels=enemies.SelectMany(e=>e.GetComponentsInChildren<P12PipelineSafeDamageable>())
                .Where(R6VehicleDrive.IsWheel).ToArray();result.wheels=wheels.Length;
            result.modelsPresent=wheels.Length>0&&wheels.All(w=>w.GetComponent<R4EnemyWheelArt>()!=null&&
                w.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4M_Wheel_Rotor"));
            result.originalColliders=wheels.Length>0&&wheels.All(w=>w.GetComponent<Collider>()!=null&&w.GetComponent<Collider>().enabled);
            Require(result.modelsPresent&&result.originalColliders,"Enemy wheel visuals and original colliders");
            var wheel=wheels[0];var collider=wheel.GetComponent<Collider>();
            var rotor=wheel.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R4M_Wheel_Rotor");
            wheel.ApplyDamage(100000,wheel.transform.position,Vector3.forward);yield return null;
            result.detachedVisual=wheel!=null&&wheel.IsDead&&wheel.transform.parent==null&&
                rotor!=null&&rotor.IsChildOf(wheel.transform)&&wheel.GetComponent<Collider>()==collider;
            Require(result.detachedVisual,"Enemy wheel visual follows physical debris");

            var template=(P12PipelineSafeEnemyTurret)typeof(R1RunController)
                .GetField("enemyTemplate",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
            var primaryDef=new R1EncounterDefinition{EnemyCount=2,Roster=new[]{R3EnemyArchetype.Flanker,R3EnemyArchetype.Bruiser},Variants=new[]{R5EnemyVariant.Normal,R5EnemyVariant.Normal}};
            run.Encounter.Spawn(template,run.Player.transform,primaryDef,run.Salvage,"r4-enemy-primary-player");
            run.Encounter.SetCombatActive(false);yield return null;
            var basic=run.Encounter.Enemies.First(e=>e.WeaponKind==R4WeaponKind.Basic);
            var primary=run.Encounter.Enemies.First(e=>e.WeaponKind==R4WeaponKind.Heavy);
            var primaryMuzzleField=typeof(P12PipelineSafeEnemyTurret).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic);
            var basicArt=basic.WeaponBlock.GetComponentInChildren<R4WeaponArt>(true);
            var primaryVisual=primary.WeaponBlock.GetComponentInChildren<R4WeaponArt>(true);
            result.primaryArt=basicArt!=null&&primaryVisual!=null;
            result.primaryVisibleMuzzle=result.primaryArt&&primaryMuzzleField.GetValue(basic)==basicArt.Muzzle&&primaryMuzzleField.GetValue(primary)==primaryVisual.Muzzle;
            Require(result.primaryArt&&result.primaryVisibleMuzzle,"Standard Basic and Heavy art and muzzle");
            foreach(var other in run.Encounter.Enemies.Where(e=>e!=primary))other.gameObject.SetActive(false);
            R1EncounterManager.ClearTransientObjects();
            var yaw=primary.transform.Find("Enemy_Yaw");yaw.rotation=Quaternion.identity;primaryVisual.Sync();
            var primaryTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);primaryTarget.transform.SetParent(run.Player.transform,true);
            primaryTarget.transform.position=primaryVisual.Muzzle.position+Vector3.forward*12;primaryTarget.transform.localScale=Vector3.one*2;
            var primaryHp=primaryTarget.AddComponent<P12PipelineSafeDamageable>();primaryHp.Configure(1000,false);
            run.Player.GetComponent<Rigidbody>().isKinematic=true;Physics.SyncTransforms();
            var enemyType=typeof(P12PipelineSafeEnemyTurret);var primaryCollider=primaryTarget.GetComponent<Collider>();
            enemyType.GetField("currentTarget",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(primary,primaryCollider);
            enemyType.GetField("nextTargetTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(primary,Time.time+30);
            enemyType.GetField("nextFireTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(primary,0f);
            primary.SetCombatActive(true);enemyType.GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(primary,null);
            var primaryShell=FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).SingleOrDefault(p=>p.PartId==primary.EquipmentInstanceId("gun"));
            Require(primaryShell!=null,"Standard Heavy fired a physical shell");
            var diameter=(float)enemyType.GetField("projectileDiameter",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(primary);
            var primaryExpected=primaryVisual.Muzzle.position+(primaryCollider.bounds.center-primaryVisual.Muzzle.position).normalized*Mathf.Max(.14f,diameter*.75f);
            Require(Vector3.Distance(primaryShell.transform.position,primaryExpected)<.001f,"Standard shell starts at visible muzzle");
            deadline=Time.realtimeSinceStartup+5;
            while(primaryHp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            result.primaryHit=primaryHp.CurrentHp<1000;Require(result.primaryHit,"Standard Heavy hits target");
            if(primaryShell!=null)Destroy(primaryShell.gameObject);Destroy(primaryTarget);primary.SetCombatActive(false);
            basic.gameObject.SetActive(true);
            var legacyMuzzle=basic.transform.Find("Enemy_Yaw/Enemy_Muzzle");
            bool swapped=basic.EquipShotgun();
            var shotgun=basic.WeaponBlock.GetComponent<RoboRougelike.WeaponIntegration.RRLShotgunBinding>();
            var shotgunArt=basic.WeaponBlock.GetComponentsInChildren<R4WeaponArt>(false).SingleOrDefault();
            result.primarySwap=swapped&&legacyMuzzle!=null&&shotgun!=null&&shotgunArt!=null&&
                shotgunArt.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_Shotgun_ROOT")&&
                typeof(RoboRougelike.WeaponIntegration.RRLShotgunBinding).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(shotgun)==shotgunArt.Muzzle;
            Require(result.primarySwap,"Switching to shotgun binds its visible muzzle");
            var enemyBody=basic.GetComponent<Rigidbody>();enemyBody.isKinematic=true;
            enemyBody.position=new Vector3(100,4,100);basic.transform.rotation=Quaternion.identity;shotgunArt.Sync();
            var shotgunTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);
            shotgunTarget.transform.position=shotgunArt.Muzzle.position+shotgunArt.Muzzle.forward*6;
            shotgunTarget.transform.localScale=new Vector3(4,4,1);
            var shotgunHp=shotgunTarget.AddComponent<P12PipelineSafeDamageable>();shotgunHp.Configure(500,false);
            basic.GetComponent<VehicleEnergyBank>()?.Recharge(1000);Physics.SyncTransforms();basic.SetCombatActive(true);
            Require(shotgun.Fire(),"Shotgun fires from its visible muzzle");
            deadline=Time.realtimeSinceStartup+5;
            while(shotgunHp.CurrentHp>=500&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            result.shotgunHit=shotgunHp.CurrentHp<500;Require(result.shotgunHit,"Physical shotgun pellets hit target");
            basic.SetCombatActive(false);Destroy(shotgunTarget);

            var railDef=new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Scout},Variants=new[]{R5EnemyVariant.Normal}};
            run.Encounter.Spawn(template,run.Player.transform,railDef,run.Salvage,"r4-enemy-rail-player");
            run.Encounter.SetCombatActive(false);yield return null;
            var railEnemy=run.Encounter.Enemies.First();Require(railEnemy.EquipRailgun(),"Equip enemy railgun");
            var rail=railEnemy.WeaponBlock.GetComponent<RoboRougelike.WeaponIntegration.RRLRailgunBinding>();
            var railArt=railEnemy.WeaponBlock.GetComponentInChildren<R4WeaponArt>(true);
            result.railgunArt=railArt!=null&&railArt.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_Railgun_ROOT");
            result.railgunMuzzle=rail!=null&&railArt!=null&&
                typeof(RoboRougelike.WeaponIntegration.RRLRailgunBinding).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(rail)==railArt.Muzzle;
            Require(result.railgunArt&&result.railgunMuzzle,"Railgun model and visible muzzle");
            var railBody=railEnemy.GetComponent<Rigidbody>();railBody.isKinematic=true;
            railBody.position=new Vector3(100,4,100);railEnemy.transform.rotation=Quaternion.identity;railArt.Sync();
            var railTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);railTarget.transform.position=railArt.Muzzle.position+railArt.Muzzle.forward*6;
            railTarget.transform.localScale=new Vector3(4,4,1);var railHp=railTarget.AddComponent<P12PipelineSafeDamageable>();railHp.Configure(500,false);
            railEnemy.GetComponent<VehicleEnergyBank>()?.Recharge(1000);Physics.SyncTransforms();railEnemy.SetCombatActive(true);
            Require(rail.Fire(),"Railgun fires from its visible muzzle");
            deadline=Time.realtimeSinceStartup+5;
            while(railHp.CurrentHp>=500&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            result.railgunHit=railHp.CurrentHp<500;Require(result.railgunHit,"Physical railgun projectile hits target");
            railEnemy.SetCombatActive(false);Destroy(railTarget);

            run.Encounter.Spawn(template,run.Player.transform,railDef,run.Salvage,"r4-enemy-rocket-player");
            run.Encounter.SetCombatActive(false);yield return null;
            var rocketEnemy=run.Encounter.Enemies.First();Require(rocketEnemy.EquipRocket(),"Equip enemy rocket launcher");
            var rocket=rocketEnemy.WeaponBlock.GetComponent<RoboRougelike.WeaponIntegration.RRLRocketBinding>();
            var rocketVisual=rocketEnemy.WeaponBlock.GetComponentInChildren<R4WeaponArt>(true);
            result.rocketArt=rocketVisual!=null&&rocketVisual.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_RocketLauncher_ROOT")&&
                typeof(RoboRougelike.WeaponIntegration.RRLRocketBinding).GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(rocket)==rocketVisual.Muzzle;
            Require(result.rocketArt,"Rocket model and visible muzzle");
            var rocketBody=rocketEnemy.GetComponent<Rigidbody>();rocketBody.isKinematic=true;
            rocketBody.position=new Vector3(100,4,100);rocketEnemy.transform.rotation=Quaternion.identity;rocketVisual.Sync();
            var rocketTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);rocketTarget.transform.position=rocketVisual.Muzzle.position+rocketVisual.Muzzle.forward*6;
            rocketTarget.transform.localScale=new Vector3(3,3,1);var rocketHp=rocketTarget.AddComponent<P12PipelineSafeDamageable>();rocketHp.Configure(500,false);
            rocketEnemy.GetComponent<VehicleEnergyBank>()?.Recharge(1000);Physics.SyncTransforms();rocketEnemy.SetCombatActive(true);
            Require(rocket.Fire(),"Rocket fires from visible muzzle");deadline=Time.realtimeSinceStartup+6;
            while(rocketHp.CurrentHp>=500&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            result.rocketHit=rocketHp.CurrentHp<500;Require(result.rocketHit,"Physical rocket damages target");
            rocketEnemy.SetCombatActive(false);Destroy(rocketTarget);

            run.Encounter.Spawn(template,run.Player.transform,railDef,run.Salvage,"r4-enemy-beam-player");
            run.Encounter.SetCombatActive(false);yield return null;
            var beamEnemy=run.Encounter.Enemies.First();Require(beamEnemy.EquipBeam(),"Equip enemy beam");
            var beam=beamEnemy.WeaponBlock.GetComponent<RoboRougelike.WeaponIntegration.RRLBeamBinding>();
            var beamVisual=beamEnemy.WeaponBlock.GetComponentInChildren<R4WeaponArt>(true);
            result.beamArt=beamVisual!=null&&beamVisual.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_BeamCutter_ROOT")&&
                beam.Module.GetType().GetField("muzzle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(beam.Module)==beamVisual.Muzzle;
            Require(result.beamArt,"Beam model and visible muzzle");
            var beamBody=beamEnemy.GetComponent<Rigidbody>();beamBody.isKinematic=true;
            beamBody.position=new Vector3(100,4,100);beamEnemy.transform.rotation=Quaternion.identity;beamVisual.Sync();
            var beamTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);beamTarget.transform.position=beamVisual.Muzzle.position+beamVisual.Muzzle.forward*6;
            beamTarget.transform.localScale=new Vector3(3,3,1);var beamHp=beamTarget.AddComponent<P12PipelineSafeDamageable>();beamHp.Configure(500,false);
            beamEnemy.GetComponent<VehicleEnergyBank>()?.Recharge(1000);Physics.SyncTransforms();beamEnemy.SetCombatActive(true);
            var beamStep=typeof(RoboRougelike.WeaponIntegration.RRLBeamBinding).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
            for(int i=0;i<30&&beamHp.CurrentHp>=500;i++){beam.SetTriggerHeld(true);beamStep.Invoke(beam,null);yield return null;}
            result.beamHit=beamHp.CurrentHp<500;Require(result.beamHit,"Actual beam damages target");
            beamEnemy.SetCombatActive(false);Destroy(beamTarget);

            run.Encounter.Spawn(template,run.Player.transform,primaryDef,run.Salvage,"r4-enemy-saw-player");
            run.Encounter.SetCombatActive(false);yield return null;
            var sawEnemy=run.Encounter.Enemies.First(e=>e.WeaponKind==R4WeaponKind.Basic);
            var sawCenter=sawEnemy.transform.Find("Enemy_Yaw/Enemy_Muzzle");Require(sawEnemy.EquipSaw(),"Equip enemy saw");
            var saw=sawEnemy.WeaponBlock.GetComponent<RoboRougelike.WeaponIntegration.RRLSawBinding>();
            var sawVisual=sawEnemy.WeaponBlock.GetComponentsInChildren<R4WeaponArt>(false).SingleOrDefault();
            result.sawArt=sawVisual!=null&&sawVisual.SawRotor!=null&&
                typeof(RoboRougelike.WeaponIntegration.RRLSawBinding).GetField("rotor",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(saw)==sawVisual.SawRotor&&
                typeof(RoboRougelike.WeaponIntegration.RRLSawBinding).GetField("blade",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(saw)==sawCenter;
            Require(result.sawArt,"Saw switch binds active visual and original physical center");
            sawEnemy.GetComponent<Rigidbody>().isKinematic=true;sawEnemy.transform.SetPositionAndRotation(new Vector3(100,4,100),Quaternion.identity);Physics.SyncTransforms();sawVisual.Sync();
            var sawTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);sawTarget.transform.position=sawCenter.position+sawCenter.forward*.45f;
            sawTarget.transform.localScale=Vector3.one*.2f;var sawHp=sawTarget.AddComponent<P12PipelineSafeDamageable>();sawHp.Configure(500,false);
            sawEnemy.GetComponent<VehicleEnergyBank>()?.Recharge(1000);Physics.SyncTransforms();sawEnemy.SetCombatActive(true);
            var sawStep=typeof(RoboRougelike.WeaponIntegration.RRLSawBinding).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
            for(int i=0;i<80&&sawHp.CurrentHp>=500;i++){saw.SetMotorRequested(true);sawStep.Invoke(saw,null);yield return null;}
            result.sawHit=sawHp.CurrentHp<500;Require(result.sawHit,"Actual saw damages target");
            sawEnemy.SetCombatActive(false);Destroy(sawTarget);

            run.Encounter.Spawn(template,run.Player.transform,railDef,run.Salvage,"r4-enemy-emp-player");
            run.Encounter.SetCombatActive(false);yield return null;
            var empEnemy=run.Encounter.Enemies.First();var empLegacy=empEnemy.transform.Find("Enemy_Yaw/Enemy_Muzzle");Require(empEnemy.EquipEmp(),"Equip enemy EMP");
            var emp=empEnemy.WeaponBlock.GetComponent<RoboRougelike.WeaponIntegration.RRLEmpBinding>();
            var empVisual=empEnemy.WeaponBlock.GetComponentInChildren<R4WeaponArt>();
            result.empArt=empVisual!=null&&empVisual.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_EmpEmitter_ROOT")&&
                primaryMuzzleField.GetValue(empEnemy)==empLegacy&&emp.Module.GetType().GetField("pulseOrigin",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(emp.Module)==empEnemy.WeaponBlock.transform;
            Require(result.empArt,"EMP art preserves pulse origin");
            empEnemy.GetComponent<Rigidbody>().isKinematic=true;empEnemy.transform.position=new Vector3(100,4,100);Physics.SyncTransforms();
            var empTarget=GameObject.CreatePrimitive(PrimitiveType.Cube);empTarget.transform.position=empEnemy.WeaponBlock.transform.position+Vector3.up*4;
            var receiver=empTarget.AddComponent<EmpReceiver>();receiver.ConfigureOwnership(empTarget.transform,1234,1);
            empEnemy.GetComponent<VehicleEnergyBank>()?.Recharge(1000);Physics.SyncTransforms();empEnemy.SetCombatActive(true);
            result.empPulse=emp.Fire()&&receiver.IsDisrupted&&!emp.Fire();Require(result.empPulse,"EMP disrupts target and respects cooldown");
            empEnemy.SetCombatActive(false);Destroy(empTarget);

            var def=new R1EncounterDefinition{EnemyCount=1,Roster=new[]{R3EnemyArchetype.Gunner},Variants=new[]{R5EnemyVariant.EliteGunner}};
            run.Encounter.Spawn(template,run.Player.transform,def,run.Salvage,"r4-enemy-art-player");
            run.Encounter.SetCombatActive(false);yield return null;
            var elite=run.Encounter.Enemies.First(e=>e.Variant==R5EnemyVariant.EliteGunner);
            foreach(var other in run.Encounter.Enemies.Where(e=>e!=elite))other.gameObject.SetActive(false);
            var heavy=elite.VariantWeapons.Single(g=>g.Kind==R4WeaponKind.Heavy);
            var automatic=elite.VariantWeapons.Single(g=>g.Kind==R4WeaponKind.Automatic);
            var art=heavy.Block.GetComponent<R4WeaponArt>();
            result.eliteArt=art!=null&&heavy.Block.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="R4W_Heavy_ROOT");
            result.eliteVisibleMuzzle=art!=null&&heavy.Muzzle==art.Muzzle;
            result.automaticFallback=automatic.Block.GetComponent<R4WeaponArt>()==null&&automatic.Pivot.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled);
            Require(result.eliteArt&&result.eliteVisibleMuzzle&&result.automaticFallback,"Elite heavy art and automatic fallback");
            R1EncounterManager.ClearTransientObjects();
            var direction=new Vector3(.4f,.15f,1).normalized;
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.transform.SetParent(run.Player.transform,true);
            target.transform.position=heavy.Pivot.position+direction*12;target.transform.localScale=Vector3.one*2;
            var hp=target.AddComponent<P12PipelineSafeDamageable>();hp.Configure(1000,false);
            run.Player.GetComponent<Rigidbody>().isKinematic=true;
            var targetCollider=target.GetComponent<Collider>();Physics.SyncTransforms();
            var type=typeof(P12PipelineSafeEnemyTurret);
            type.GetField("currentTarget",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(elite,targetCollider);
            type.GetField("nextTargetTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(elite,Time.time+30);
            heavy.Pivot.rotation=Quaternion.LookRotation(-direction);art.Sync();
            heavy.Pivot.rotation=Quaternion.LookRotation(direction);heavy.NextShot=0;
            type.GetMethod("UpdateVariantWeapons",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(elite,new object[]{new List<P12PipelineSafeEnemyTurret.VariantGun>{heavy},"gun/"});
            var shell=FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None)
                .SingleOrDefault(p=>p.PartId==elite.EquipmentInstanceId("gun/0"));
            Require(shell!=null,"Elite heavy fired a physical shell");
            var expected=heavy.Muzzle.position+(targetCollider.bounds.center-heavy.Muzzle.position).normalized*.25f;
            Require(Vector3.Distance(shell.transform.position,expected)<.001f,"Shell starts at current visible muzzle");
            deadline=Time.realtimeSinceStartup+5;
            while(hp.CurrentHp>=1000&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
            result.eliteHit=hp.CurrentHp<1000;Require(result.eliteHit,"Elite physical shell hits target");
        }
    }
}

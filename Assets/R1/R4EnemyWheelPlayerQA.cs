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

using System;
using System.Collections;
using System.IO;
using System.Linq;
using RoboRougelike.R4_1;
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
        }
    }
}

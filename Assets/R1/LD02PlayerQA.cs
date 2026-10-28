#if UNITY_EDITOR || B01_UNIFIED_RC2_ACCEPTANCE
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R1
{
    // Explicit batch Player acceptance, dormant in ordinary gameplay.
    public sealed class LD02PlayerQA : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string status="Running",error="",mode="",method="API-controlled victory/defeat; enemy physical projectile; not human gameplay";
            public bool batchMode,garage,enemyShot,loss,partIdsPreserved;
            public int battles,wins,rewards;
        }
        readonly Report report=new Report();string directory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-ld02-player-qa");
            if(i<0||i+2>=args.Length||!Application.isBatchMode)return;
            var host=new GameObject("LD02 batch Player QA");DontDestroyOnLoad(host);
            var qa=host.AddComponent<LD02PlayerQA>();qa.directory=args[i+1];qa.report.mode=args[i+2];
        }
        void Start(){Directory.CreateDirectory(directory);Application.runInBackground=true;Application.targetFrameRate=60;report.batchMode=true;StartCoroutine(Run());}
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        IEnumerator Run()
        {
            var flow=Exercise();bool ok=true;
            while(true)
            {
                bool next=false;object current=null;
                try{next=flow.MoveNext();if(next)current=flow.Current;}
                catch(Exception e){report.error=e.ToString();ok=false;}
                if(!next||!ok)break;yield return current;
            }
            report.status=ok?"PASS":"FAIL";
            File.WriteAllText(Path.Combine(directory,"ld02-player-qa.json"),JsonUtility.ToJson(report,true));
            Application.Quit(ok?0:1);
        }
        IEnumerator Exercise()
        {
            float deadline=Time.realtimeSinceStartup+45;
            while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
            var run=R1RunController.Instance;Require(run?.Progress!=null,"game bootstrap");
            Require(run.SelectVehicle(report.mode=="loss"?R3VehicleKind.Heavy:R3VehicleKind.Light),"vehicle selection");
            Require(run.PrepareUnifiedRun(),"garage entry");report.garage=true;
            var ids=run.VehicleModel.Parts.Select(p=>p.Id).ToArray();
            int rounds=report.mode=="loss"?1:2;
            for(int round=0;round<rounds;round++)
            {
                Require(run.ChooseLD02Arena(),"select LD02 from garage");deadline=Time.realtimeSinceStartup+45;
                while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
                Require(!run.Transitioning&&run.Progress.State==R1RunState.Combat&&run.Encounter.AliveCount>0,"LD02 combat starts");report.battles++;
                if(report.mode=="loss")
                {
                    run.Encounter.SetCombatActive(false);run.Player.Core.ApplyDamage(100000,run.Player.Core.transform.position,Vector3.zero);
                    deadline=Time.realtimeSinceStartup+20;
                    while(run.Progress.State!=R1RunState.Lost&&Time.realtimeSinceStartup<deadline)yield return null;
                    report.loss=run.Progress.State==R1RunState.Lost;Require(report.loss,"actual defeat transition");yield break;
                }
                if(round==0)
                {
                    deadline=Time.realtimeSinceStartup+20;
                    while(!report.enemyShot&&run.Progress.State==R1RunState.Combat&&Time.realtimeSinceStartup<deadline)
                    {
                        report.enemyShot=FindObjectsByType<P17PhysicalShell>(FindObjectsSortMode.None)
                            .Any(s=>run.Encounter.Enemies.Any(e=>e!=null&&s.OwnerRoot==e.transform));
                        yield return null;
                    }
                    Require(report.enemyShot,"enemy creates physical projectile");
                }
                run.Encounter.SetCombatActive(false);int before=run.VehicleModel.Scrap;
                foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
                deadline=Time.realtimeSinceStartup+20;
                while(run.Progress.State!=R1RunState.Salvage&&Time.realtimeSinceStartup<deadline)yield return null;
                Require(run.Progress.State==R1RunState.Salvage,"victory and reward state");
                int paid=run.VehicleModel.Scrap;Require(paid==before+run.LastScrapReward&&run.LastScrapReward>0,"one reward");
                yield return null;yield return null;Require(run.VehicleModel.Scrap==paid,"reward is not repeated");
                report.wins++;report.rewards++;
                if(round==0)
                {
                    var loot=run.Salvage.Items.First(p=>p.Selectable);
                    Require(run.ConfirmRewards(new[]{loot.Id},new[]{0}),"return to garage with loot");
                    report.partIdsPreserved=run.VehicleModel.Parts.Select(p=>p.Id).OrderBy(p=>p).SequenceEqual(ids.OrderBy(p=>p));
                    Require(report.partIdsPreserved&&run.Progress.State==R1RunState.Garage,"owned IDs preserved");yield return null;
                }
            }
            Require(run.Progress.CompletedEncounters==2,"two completed encounters");
        }
    }
}
#endif

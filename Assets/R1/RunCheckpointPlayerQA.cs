#if B01_UNIFIED_RC2_ACCEPTANCE
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using RoboRougelike.R3;

namespace RoboRougelike.R1
{
    // Batch-only QA; requires an explicit separate save slot.
    public sealed class RunCheckpointPlayerQA : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string status="Running",error="",method="API-controlled victory, save, restart and resume; not human play";
            public bool separateSlot,saved,restarted,loaded,ids,balance,damage,repeatedLoadRejected,nextFight;
        }
        readonly Report report=new Report();string directory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rrl-checkpoint-player-qa");
            if(i<0||i+1>=args.Length||!Application.isBatchMode)return;
            var host=new GameObject("Checkpoint batch Player QA");DontDestroyOnLoad(host);
            host.AddComponent<RunCheckpointPlayerQA>().directory=args[i+1];
        }
        void Start(){Directory.CreateDirectory(directory);Application.runInBackground=true;Application.targetFrameRate=60;StartCoroutine(Run());}
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
            File.WriteAllText(Path.Combine(directory,"checkpoint-player-qa.json"),JsonUtility.ToJson(report,true));
            Application.Quit(ok?0:1);
        }
        IEnumerator Exercise()
        {
            string expected=Path.GetFullPath(Path.Combine(directory,"test-checkpoint.json"));
            report.separateSlot=string.Equals(Path.GetFullPath(R1RunCheckpointStorage.PathName),expected,StringComparison.OrdinalIgnoreCase);
            Require(report.separateSlot,"refuse to touch the real save slot");
            float deadline=Time.realtimeSinceStartup+45;
            while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
            var run=R1RunController.Instance;Require(run?.Progress!=null,"bootstrap");
            Require(run.SelectVehicle(R3VehicleKind.Light)&&run.PrepareUnifiedRun(),"garage");
            Require(run.ChooseLD02Arena(),"LD02");deadline=Time.realtimeSinceStartup+45;
            while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(run.Progress.State==R1RunState.Combat,"combat starts");
            run.Encounter.SetCombatActive(false);
            foreach(var enemy in run.Encounter.Enemies.ToArray())enemy.Core.ApplyDamage(100000,enemy.transform.position,Vector3.zero);
            deadline=Time.realtimeSinceStartup+20;
            while(run.Progress.State!=R1RunState.Salvage&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(run.Progress.State==R1RunState.Salvage,"victory");
            var loot=run.Salvage.Items.First(p=>p.Selectable);Require(run.ConfirmRewards(new[]{loot.Id},new[]{0}),"rewards");
            yield return null;
            var armor=run.Player.GetComponent<R3VehicleAssembly>().Parts.First(p=>p.Kind==R3PartKind.Armor);
            string armorId=armor.Id;armor.Damageable.ApplyDamage(10,armor.transform.position,Vector3.zero);
            int balance=run.VehicleModel.Scrap;
            var ids=run.VehicleModel.Parts.Select(p=>p.Id).Concat(run.VehicleModel.Inventory.Select(p=>p.Id)).OrderBy(x=>x).ToArray();
            report.saved=run.SaveRunCheckpoint();Require(report.saved&&File.Exists(expected),"save checkpoint");
            var old=run;run.RestartRun();deadline=Time.realtimeSinceStartup+45;
            while((R1RunController.Instance==old||R1RunController.Instance?.Progress==null)&&Time.realtimeSinceStartup<deadline)yield return null;
            run=R1RunController.Instance;report.restarted=run!=old&&run.Progress.State==R1RunState.Ready;Require(report.restarted,"restart");
            report.loaded=run.LoadRunCheckpoint();Require(report.loaded&&run.Progress.CompletedEncounters==1,"load checkpoint");
            report.ids=run.VehicleModel.Parts.Select(p=>p.Id).Concat(run.VehicleModel.Inventory.Select(p=>p.Id)).OrderBy(x=>x).SequenceEqual(ids);
            report.balance=run.VehicleModel.Scrap==balance;
            report.damage=run.VehicleModel.Parts.Single(p=>p.Id==armorId).Health<1;
            report.repeatedLoadRejected=!run.LoadRunCheckpoint();
            Require(report.ids&&report.balance&&report.damage&&report.repeatedLoadRejected,"preserved state and duplicate-load safety");
            report.nextFight=run.ChooseLD02Arena();Require(report.nextFight,"start next fight after loading");
        }
    }
}
#endif

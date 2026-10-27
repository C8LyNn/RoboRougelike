using System;
using System.Collections;
using System.IO;
using System.Linq;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

namespace RoboRougelike.R1
{
    // Explicit batch-only Player evidence; not active during normal gameplay.
    public sealed class Garage307PlayerQA : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string status="Running",error="";
            public bool batchMode,garage,removedGun,removedAllWheels,blockedStart,protectedCore,noPhantomShot;
            public bool restoredSameParts,unchangedScrap,validDeployment,enteredCombat;
            public int removedWheels;
        }
        string directory;readonly Report report=new Report();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rrl-garage307-qa");
            if(i<0||i+1>=args.Length||!Application.isBatchMode)return;
            var obj=new GameObject("Explicit garage 307 Player QA");DontDestroyOnLoad(obj);
            obj.AddComponent<Garage307PlayerQA>().directory=args[i+1];
        }
        void Start(){Directory.CreateDirectory(directory);Application.runInBackground=true;Application.targetFrameRate=60;report.batchMode=Application.isBatchMode;StartCoroutine(Run());}
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        IEnumerator Run()
        {
            var flow=Exercise();bool ok=true;
            while(true)
            {
                bool next=false;object current=null;
                try{next=flow.MoveNext();if(next)current=flow.Current;}
                catch(Exception e){report.error=e.ToString();ok=false;}
                if(!next||!ok)break;
                yield return current;
            }
            report.status=ok?"PASS":"FAIL";
            File.WriteAllText(Path.Combine(directory,"garage307-player-qa.json"),JsonUtility.ToJson(report,true));
            Application.Quit(ok?0:1);
        }
        IEnumerator Exercise()
        {
            float deadline=Time.realtimeSinceStartup+45;
            while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
            var run=R1RunController.Instance;Require(run!=null&&run.Progress!=null,"Game bootstrap");
            Require(run.PrepareUnifiedRun(),"Main menu to actual garage");report.garage=true;
            var model=run.VehicleModel;int scrap=model.Scrap;
            var gun=model.Parts.First(p=>p.Kind==R3PartKind.Gun);
            var wheels=model.Parts.Where(p=>p.Kind==R3PartKind.Wheel).ToArray();
            Require(run.GarageRemove(gun.Cell),"Remove last gun and rebuild garage");report.removedGun=true;
            foreach(var wheel in wheels){Require(run.GarageRemove(wheel.Cell),"Remove wheel "+wheel.Cell);report.removedWheels++;}
            report.removedAllWheels=!model.Parts.Any(p=>p.Kind==R3PartKind.Wheel);
            report.blockedStart=!model.CanDeploy&&!run.StartNextEncounter()&&!run.EnterTestRange();
            report.protectedCore=!run.GarageRemove(Vector3Int.zero);
            R1EncounterManager.ClearTransientObjects();run.Player.GetComponent<R3VehicleAssembly>().FireGuns();
            report.noPhantomShot=FindObjectsByType<R4ProjectileOrigin>(FindObjectsSortMode.None).Length==0;
            Require(report.removedAllWheels&&report.blockedStart&&report.protectedCore&&report.noPhantomShot,"Incomplete build protections");
            Require(run.GarageMount(gun.Id,gun.Cell,gun.RotationQuarterTurns),"Remount the owned gun");
            foreach(var wheel in wheels)Require(run.GarageMount(wheel.Id,wheel.Cell,wheel.RotationQuarterTurns),"Remount owned wheel "+wheel.Cell);
            report.restoredSameParts=model.Parts.Any(p=>p.Id==gun.Id)&&wheels.All(w=>model.Parts.Any(p=>p.Id==w.Id));
            report.unchangedScrap=model.Scrap==scrap;report.validDeployment=model.CanDeploy;
            Require(report.restoredSameParts&&report.unchangedScrap&&report.validDeployment,"Restore vehicle without purchases");
            Require(run.StartNextEncounter()&&run.ChooseRoute(0),"Start activity after restore");
            deadline=Time.realtimeSinceStartup+45;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            report.enteredCombat=!run.Transitioning&&run.Progress.State==R1RunState.Combat&&run.Player!=null;
            Require(report.enteredCombat,"Restored vehicle entered actual combat");
        }
    }
}

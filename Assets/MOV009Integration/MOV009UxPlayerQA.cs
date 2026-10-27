using System;
using System.Collections;
using System.IO;
using System.Linq;
using RoboRougelike.Mechanics.MOV009;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.MOV009Integration
{
    // Explicit batch-only Player acceptance; never runs in a normal game session.
    public sealed class MOV009UxPlayerQA : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string status="Running",error="",missingStatus="",readyStatus="",activeStatus="",hoverStatus="",damagedStatus="";
            public bool batchMode,missing,garage,off,assist,full,height,actualForce,paused,hover,damaged;
            public float force;
        }
        string directory;readonly Report report=new Report();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rrl-suspension-ux-qa");
            if(i<0||i+1>=args.Length||!Application.isBatchMode)return;
            var obj=new GameObject("Explicit suspension UX Player QA");DontDestroyOnLoad(obj);
            obj.AddComponent<MOV009UxPlayerQA>().directory=args[i+1];
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
            File.WriteAllText(Path.Combine(directory,"suspension-ux-player-qa.json"),JsonUtility.ToJson(report,true));
            Application.Quit(ok?0:1);
        }
        IEnumerator Exercise()
        {
            float deadline=Time.realtimeSinceStartup+40;
            while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
            var run=R1RunController.Instance;Require(run!=null&&run.SelectVehicle(R3VehicleKind.Light)&&run.OpenSandboxGarage(),"Actual garage");
            var state=MOV009Presentation.Capture(run);report.missingStatus=state.Status;
            report.missing=state.MountedCount==0&&state.OperationalCount==0&&!state.Applying&&state.Status.Contains("Brak");
            Require(report.missing,"Missing module state");
            foreach(var cell in new[]{new Vector3Int(-1,0,-3),new Vector3Int(1,0,-3),new Vector3Int(-1,0,3)})
                Require(run.GarageBuy(cell,R3PartKind.Suspension),"Mount real suspension "+cell);
            Require(run.GarageBuy(new Vector3Int(1,0,3),R3PartKind.HoverEngine),"Mount real Hover");
            state=MOV009Presentation.Capture(run);report.readyStatus=state.Status;
            report.garage=state.OperationalCount==3&&!state.Applying&&state.Status.Contains("Gotowe");
            Require(run.ConfigureSuspension(false,ActiveSuspensionSupportMode.FullSupport,0),"Configure OFF");
            state=MOV009Presentation.Capture(run);report.off=state.Mode=="OFF"&&!state.Applying;
            Require(run.ConfigureSuspension(true,ActiveSuspensionSupportMode.AssistOnly,0),"Configure Assist");
            state=MOV009Presentation.Capture(run);report.assist=state.Mode=="Assist"&&!state.Applying;
            Require(run.ConfigureSuspension(true,ActiveSuspensionSupportMode.FullSupport,.2f),"Configure Full and height");
            state=MOV009Presentation.Capture(run);report.full=state.Mode=="Full";report.height=Mathf.Abs(state.Offset-.2f)<.001f;
            Require(report.garage&&report.off&&report.assist&&report.full&&report.height,"Garage settings and readiness");
            Require(run.EnterTestRange(),"Real R6 test range");deadline=Time.realtimeSinceStartup+40;
            while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(!run.Transitioning,"Range transition");deadline=Time.realtimeSinceStartup+12;
            while(!MOV009Presentation.Capture(run).Applying&&Time.realtimeSinceStartup<deadline)yield return null;
            state=MOV009Presentation.Capture(run);report.activeStatus=state.Status;report.force=state.ForceNewtons;
            report.actualForce=state.Applying&&state.ForceNewtons>0&&state.Status.Contains("Pracuje");
            Require(report.actualForce,"Actual suspension force");
            run.TogglePause();state=MOV009Presentation.Capture(run);
            report.paused=run.Paused&&!state.Applying&&state.ForceNewtons==0&&state.CanConfigure;
            Require(report.paused,"Pause does not display stale force");
            Require(run.ConfigureDrive(R3DriveKind.Hover),"Select actual Hover drive");
            state=MOV009Presentation.Capture(run);report.hoverStatus=state.Status;report.hover=!state.Applying&&state.Status.Contains("Hover");
            Require(report.hover&&run.ConfigureDrive(R3DriveKind.Wheels),"Hover inhibits suspension");
            foreach(var part in run.Player.GetComponent<R3VehicleAssembly>().Parts.Where(p=>p.Kind==R3PartKind.Suspension).ToArray())
                part.Damageable.ApplyDamage(10000,part.transform.position,Vector3.up);
            state=MOV009Presentation.Capture(run);report.damagedStatus=state.Status;
            report.damaged=state.OperationalCount==0&&!state.Applying&&state.Status.Contains("sprawnych");
            Require(report.damaged,"Destroyed modules are unavailable");
        }
    }
}

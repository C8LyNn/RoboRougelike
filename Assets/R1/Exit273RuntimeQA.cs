#if UNITY_EDITOR || B01_UNIFIED_RC2_ACCEPTANCE
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace RoboRougelike.R1
{
    // Explicit opt-in acceptance fixture; never installed on an ordinary launch.
    public sealed class Exit273RuntimeQA : MonoBehaviour
    {
        [Serializable] public sealed class Result
        {
            public string method="API-controlled session/shutdown acceptance in real Windows Player; not human GUI or native Alt+F4/X";
            public string status="Running",mode,startedUtc,finishedUtc,stateAtQuit;
            public bool applicationQuitReached,quitRequested,transitioning,paused,menuReturned;
            public int width,height;public string error;
        }
        string output; readonly Result result=new Result();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-exit273-output");
            if(i<0||i+1>=args.Length)return;
            var root=new GameObject("EXIT273 explicit Player acceptance");DontDestroyOnLoad(root);
            var fixture=root.AddComponent<Exit273RuntimeQA>();fixture.output=args[i+1];
            i=Array.IndexOf(args,"-exit273-mode");fixture.result.mode=i>=0&&i+1<args.Length?args[i+1]:"Menu";
        }
        void Save()=>File.WriteAllText(Path.Combine(output,"exit-result.json"),JsonUtility.ToJson(result,true));
        void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);result.startedUtc=DateTime.UtcNow.ToString("o");
            Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
            result.width=Screen.width;result.height=Screen.height;Save();
            yield return RunSafely();
        }
        IEnumerator RunSafely()
        {
            var steps=Run();
            while(true)
            {
                object next;
                try { if(!steps.MoveNext())break;next=steps.Current; }
                catch(Exception error)
                {result.status="Failed";result.error=error.ToString();Save();Debug.LogException(error);Application.Quit(1);yield break;}
                yield return next;
            }
        }
        IEnumerator Run()
        {
            float deadline=Time.realtimeSinceStartup+20;
            while(R1RunController.Instance?.Progress==null)
            {if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Initial menu");yield return null;}
            var run=R1RunController.Instance;Capture("00-menu");yield return new WaitForSecondsRealtime(.3f);
            if(result.mode!="Menu")
            {
                if(!run.PrepareUnifiedRun())throw new InvalidOperationException("Garage unavailable");
                yield return null;yield return null;Capture("01-garage");
                if(result.mode!="Garage")
                {
                    bool started=result.mode=="RangeLong"?run.EnterTestRange():run.ChooseGeneratedArena();
                    if(!started)throw new InvalidOperationException("Activity unavailable");
                    if(result.mode!="Transition")
                    {
                        deadline=Time.realtimeSinceStartup+20;
                        while(run.Transitioning){if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Activity transition");yield return null;}
                        yield return new WaitForSecondsRealtime(result.mode=="RangeLong"?185:2);
                        run.TogglePause();Capture("02-paused-activity");yield return new WaitForSecondsRealtime(.3f);
                        if(result.mode=="Return")
                        {
                            var old=run;run.RestartRun();deadline=Time.realtimeSinceStartup+20;
                            while(R1RunController.Instance==old||R1RunController.Instance?.Progress==null)
                            {if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Return to menu");yield return null;}
                            run=R1RunController.Instance;result.menuReturned=run.Progress.State==R1RunState.Ready;
                            if(!result.menuReturned||run.GeneratedWorld!=null)throw new InvalidOperationException("Activity not cleaned");
                            Capture("03-returned-menu");yield return new WaitForSecondsRealtime(.3f);
                        }
                    }
                }
                else {run.TogglePause();Capture("02-paused-garage");yield return new WaitForSecondsRealtime(.3f);}
            }
            result.stateAtQuit=run.Progress.State.ToString();result.transitioning=run.Transitioning;result.paused=run.Paused;
            result.status="Passed";result.quitRequested=true;Save();run.RequestQuit();
        }
        void OnApplicationQuit(){result.applicationQuitReached=true;result.finishedUtc=DateTime.UtcNow.ToString("o");if(output!=null)Save();}
    }
}
#endif

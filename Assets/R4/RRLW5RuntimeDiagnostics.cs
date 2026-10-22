#if MOVSERIES_VISUAL_QA
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R4
{
    // Explicitly installed diagnostic fixture. Entirely absent from retail assemblies.
    public sealed class RRLW5RuntimeDiagnostics : MonoBehaviour
    {
        [Serializable] public sealed class CombatSample
        {
            public int stage,frames,peakEnemies,peakParts,peakDebris;
            public string encounter;
            public float seconds,medianMs,p95Ms,p99Ms,maxMs;
            public long peakAllocatedBytes;
        }
        [Serializable] public sealed class Result
        {
            public string method="QA-only frame-time sampling; optional controlled Application.Quit and empty-scene isolation",startedUtc,finishedUtc,graphics,cpu;
            public int width,height,targetFrameRate,completedEncounters,bonusAcquisitions;
            public bool emptyScene,quitRequested,applicationQuitReached,sawVictory,restartVerified;
            public int liveGameControllers,liveCameras;
            public float maxTransitionSeconds;
            public List<CombatSample> combat=new List<CombatSample>();
            public List<string> errors=new List<string>();
        }
        readonly Result result=new Result();
        readonly Dictionary<int,List<float>> frames=new Dictionary<int,List<float>>();
        string output,lastState;float started,quitAfter=-1,nextSample,transitionStarted=-1;
        bool emptyRequested;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-rrl-w5-diagnostics-output");
            if(at<0||at+1>=args.Length)return;
            var root=new GameObject("W5 explicit QA diagnostics");DontDestroyOnLoad(root);
            root.AddComponent<RRLW5RuntimeDiagnostics>().Initialize(args,args[at+1]);
        }
        void Initialize(string[] args,string path)
        {
            output=path;Directory.CreateDirectory(output);started=Time.realtimeSinceStartup;
            result.startedUtc=DateTime.UtcNow.ToString("o");result.graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;
            result.cpu=SystemInfo.processorType;result.emptyScene=args.Contains("-rrl-w5-empty-scene");
            int at=Array.IndexOf(args,"-rrl-w5-quit-after");
            if(at>=0&&at+1<args.Length&&float.TryParse(args[at+1],NumberStyles.Float,CultureInfo.InvariantCulture,out float seconds)&&!float.IsNaN(seconds)&&!float.IsInfinity(seconds))
                quitAfter=Mathf.Clamp(seconds,3,300);
            Application.runInBackground=true;Application.logMessageReceived+=OnLog;Save();
        }
        void OnLog(string message,string stack,LogType kind)
        {
            if((kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)&&result.errors.Count<20)
            {result.errors.Add(message);Save();}
        }
        void IsolateEmptyScene()
        {
            emptyRequested=true;
            var old=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            var scene=SceneManager.CreateScene("W5 empty shutdown probe");SceneManager.SetActiveScene(scene);
            foreach(var item in old)SceneManager.UnloadSceneAsync(item);
        }
        void Update()
        {
            if(output==null)return;
            if(result.emptyScene&&!emptyRequested&&Time.realtimeSinceStartup-started>2)IsolateEmptyScene();
            var run=R1RunController.Instance;
            if(!result.emptyScene&&run?.Progress!=null)
            {
                result.completedEncounters=Math.Max(result.completedEncounters,run.Progress.CompletedEncounters);
                result.bonusAcquisitions=Math.Max(result.bonusAcquisitions,run.Progress.Acquired.Count);
                result.sawVictory|=run.Progress.State==R1RunState.Won;
                result.restartVerified|=result.sawVictory&&run.Progress.State==R1RunState.Ready&&run.Progress.CompletedEncounters==0&&run.Progress.Acquired.Count==0&&
                    (run.VehicleModel==null||(run.VehicleModel.Scrap==0&&run.VehicleModel.Inventory.Count==0));
                if(run.Transitioning&&transitionStarted<0)transitionStarted=Time.realtimeSinceStartup;
                if(!run.Transitioning&&transitionStarted>=0)
                {result.maxTransitionSeconds=Mathf.Max(result.maxTransitionSeconds,Time.realtimeSinceStartup-transitionStarted);transitionStarted=-1;}
                if(run.Progress.State==R1RunState.Combat&&!run.Paused&&!run.Transitioning)RecordCombat(run);
                string state=run.Progress.State+"/"+run.Progress.CompletedEncounters+"/"+(run.Encounter?.AliveCount??0);
                if(state!=lastState){lastState=state;Save();}
            }
            if(quitAfter>=0&&!result.quitRequested&&Time.realtimeSinceStartup-started>=quitAfter)
            {
                result.quitRequested=true;Save();Debug.Log("[W5 SHUTDOWN PROBE] Application.Quit(0), empty="+result.emptyScene);
                Application.Quit(0);
            }
        }
        void RecordCombat(R1RunController run)
        {
            int stage=run.Progress.EncounterIndex+1;
            if(!frames.TryGetValue(stage,out var times))
            {
                times=new List<float>();frames.Add(stage,times);
                result.combat.Add(new CombatSample{stage=stage,encounter=run.CurrentEncounter.Title});
            }
            float ms=Time.unscaledDeltaTime*1000;if(ms>0)times.Add(ms);
            if(Time.realtimeSinceStartup<nextSample)return;nextSample=Time.realtimeSinceStartup+1;
            var sample=result.combat.First(s=>s.stage==stage);
            sample.peakEnemies=Math.Max(sample.peakEnemies,run.Encounter.AliveCount);
            sample.peakParts=Math.Max(sample.peakParts,FindObjectsByType<P12PipelineSafeDamageable>(FindObjectsSortMode.None).Length);
            sample.peakDebris=Math.Max(sample.peakDebris,FindObjectsByType<R3Debris>(FindObjectsSortMode.None).Length);
            sample.peakAllocatedBytes=Math.Max(sample.peakAllocatedBytes,Profiler.GetTotalAllocatedMemoryLong());
        }
        void Save()
        {
            if(output==null)return;
            result.width=Screen.width;result.height=Screen.height;result.targetFrameRate=Application.targetFrameRate;
            result.liveGameControllers=FindObjectsByType<R1RunController>(FindObjectsSortMode.None).Length;
            result.liveCameras=FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
            foreach(var sample in result.combat)
            {
                var sorted=frames[sample.stage].OrderBy(t=>t).ToArray();if(sorted.Length==0)continue;
                sample.frames=sorted.Length;sample.seconds=sorted.Sum()/1000;
                sample.medianMs=sorted[(sorted.Length-1)/2];sample.p95Ms=sorted[Mathf.CeilToInt(sorted.Length*.95f)-1];
                sample.p99Ms=sorted[Mathf.CeilToInt(sorted.Length*.99f)-1];sample.maxMs=sorted[sorted.Length-1];
            }
            File.WriteAllText(Path.Combine(output,"w5-diagnostics.json"),JsonUtility.ToJson(result,true));
        }
        void OnApplicationQuit(){result.applicationQuitReached=true;result.finishedUtc=DateTime.UtcNow.ToString("o");Save();}
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
#endif

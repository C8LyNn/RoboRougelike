// B01-50 PERFSCALING-001 / opt-in true live scene frame observer.
// Time.unscaledDeltaTime != PresentMon display intervals. NOT a FPS/CPU/GPU acceptance.
// Dormant without compile define B0150_LIVE_PROFILING AND command-line activation.
#if B0150_LIVE_PROFILING
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace RoboRougelike.B0150.PerformanceQA
{
    [DefaultExecutionOrder(32760)]
    public sealed class B0150LiveFrameCapture : MonoBehaviour
    {
        private string path,gameSha,buildSha,scenario;
        private readonly StringBuilder frames=new StringBuilder("frame_index,elapsed_ms,delta_ms\n");
        private readonly StringBuilder objects=new StringBuilder("elapsed_ms,scene,active_parts,active_projectiles,active_enemies,rigidbodies,scan_ms\n");
        private static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
        private double started,captureStart,lastScan;
        private int frameCount,scanCount;
        private bool running,finished;
        [Serializable] private sealed class Receipt
        {
            public string classification="GAME_FRAME_TIME_REVIEW_REQUIRED_NOT_DISPLAY_PRESENTMON_OR_NATIVE_PERF_PASS";
            public string game_sha,build_zip_sha256,scenario_id,unity_version,os,cpu,gpu,scene;
            public string frame_time_metric="Time.unscaledDeltaTime, NOT presented-frame duration";
            public string profiler_main_render_gpu_gc="NOT_MEASURED";
            public string active_projectile_types="P17PhysicalShell, RocketProjectile, PointDefenseInterceptor; incomplete";
            public int frame_rows,object_scans;
            public float warmup_seconds=30,requested_capture_seconds=120,observed_capture_seconds;
            public bool capture_complete;
        }
        private static string Flag(string[] args,string key)
        {
            int index=Array.IndexOf(args,key);
            return index>=0&&index+1<args.Length?args[index+1]:null;
        }
        private static bool Hex(string text,int length)
        {
            if(text==null||text.Length!=length)return false;
            foreach(char c in text)
                if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')||(c>='A'&&c<='F')))return false;
            return true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            string[] args=Environment.GetCommandLineArgs();
            string output=Flag(args,"-b0150-live-output");
            if(output==null)return; // no side effects without an operator flag.
            string game=Flag(args,"-b0150-live-game-sha");
            string build=Flag(args,"-b0150-live-build-sha");
            string label=Flag(args,"-b0150-live-scenario");
            if(!Hex(game,40)||!Hex(build,64)||string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("B0150 capture requires true source40, build SHA256, scenario");
            output=Path.GetFullPath(output);
            if(Directory.Exists(output))throw new IOException("Evidence path already exists: "+output);
            Directory.CreateDirectory(output);
            var go=new GameObject("B0150 EXPLICIT TRUE SCENE FRAME COUNTER");
            DontDestroyOnLoad(go);
            go.AddComponent<B0150LiveFrameCapture>().StartCapture(output,game,build,label);
        }
        private void StartCapture(string output,string game,string build,string label)
        {
            path=output;gameSha=game.ToLowerInvariant();buildSha=build.ToLowerInvariant();scenario=label;
            started=Time.realtimeSinceStartupAsDouble;
            Application.runInBackground=true;
        }
        private void LateUpdate()
        {
            if(path==null||finished)return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(!running)
            {
                if(now-started<30)return;
                running=true;captureStart=now;lastScan=-1;
            }
            double age=now-captureStart;
            if(age>=120){Finish(true,age);return;}
            frames.Append(frameCount).Append(',').Append((age*1000).ToString("R",Invariant))
                .Append(',').Append((Time.unscaledDeltaTime*1000).ToString("R",Invariant)).Append('\n');
            frameCount++;
            if(age-lastScan>=1)
            {
                var clock=Stopwatch.StartNew();
                int parts=0,shots=0,enemies=0;
                // Count actual loaded game MonoBehaviours without referencing canonical
                // source assemblies or modifying any R3/R4/P12 runtime state.
                foreach(var script in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if(script==null)continue;
                    string type=script.GetType().Name;
                    if(type=="R3MountedPart")parts++;
                    else if(type=="P12PipelineSafeEnemyTurret")enemies++;
                    else if(type=="P17PhysicalShell"||type=="RocketProjectile"||
                        type=="PointDefenseInterceptor")shots++;
                }
                int bodies=FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length;
                clock.Stop();lastScan=age;scanCount++;
                objects.Append((age*1000).ToString("R",Invariant)).Append(',')
                    .Append(SceneManager.GetActiveScene().name.Replace(',', '_')).Append(',')
                    .Append(parts).Append(',').Append(shots).Append(',').Append(enemies)
                    .Append(',').Append(bodies).Append(',')
                    .Append(clock.Elapsed.TotalMilliseconds.ToString("R",Invariant)).Append('\n');
            }
        }
        private void Finish(bool complete,double age)
        {
            if(finished||path==null)return;
            finished=true;
            var receipt=new Receipt{
                game_sha=gameSha,build_zip_sha256=buildSha,scenario_id=scenario,
                unity_version=Application.unityVersion,os=SystemInfo.operatingSystem,
                cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,
                scene=SceneManager.GetActiveScene().name,frame_rows=frameCount,
                object_scans=scanCount,observed_capture_seconds=(float)age,
                capture_complete=complete
            };
            File.WriteAllText(Path.Combine(path,"frames.csv"),frames.ToString());
            File.WriteAllText(Path.Combine(path,"objects.csv"),objects.ToString());
            File.WriteAllText(Path.Combine(path,"receipt.json"),JsonUtility.ToJson(receipt,true));
            UnityEngine.Debug.Log("B0150 TRUE SCENE FRAME CAPTURE: RAW_REVIEW_REQUIRED "+path);
            // Never quits or manipulates gameplay; operator exits normally.
        }
        private void OnApplicationQuit()
        {
            if(running&&!finished)Finish(false,Time.realtimeSinceStartupAsDouble-captureStart);
        }
    }
}
#endif

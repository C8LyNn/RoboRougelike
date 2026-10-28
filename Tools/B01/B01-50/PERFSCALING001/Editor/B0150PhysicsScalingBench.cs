#if UNITY_EDITOR
// PERFSCALING-001 R1 — B01-50 only. Isolated Unity physics proxies, never game FPS.
// Receiver must stage this file in a DISPOSABLE project's Assets/B0150PerfSandbox/Editor.
// Never edit Codex working tree or the default PhysicsScene.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoboRougelike.B0150.PerformanceQA
{
    public static class B0150PhysicsScalingBench
    {
        private const int Warmup = 30, Steps = 200, Repeats = 3;
        private const float Dt = .02f;
        private static readonly CultureInfo C = CultureInfo.InvariantCulture;

        private sealed class Load
        {
            internal readonly string Name;
            internal readonly int Parts, Projectiles, Enemies;
            internal Load(string name, int parts, int projectiles, int enemies)
            { Name = name; Parts = parts; Projectiles = projectiles; Enemies = enemies; }
        }
        private static readonly Load[] Cases = {
            new Load("baseline_24_0_0",24,0,0),
            new Load("parts_72_0_0",72,0,0),
            new Load("parts_144_0_0",144,0,0),
            new Load("projectiles_24_32_0",24,32,0),
            new Load("projectiles_24_128_0",24,128,0),
            new Load("enemies_24_0_8",24,0,8),
            new Load("enemies_24_0_32",24,0,32),
            new Load("combined_144_128_32",144,128,32),
        };

        [Serializable]
        private sealed class Receipt
        {
            public string classification = "NATIVE_PHYSICS_PROXY_CAPTURE_REVIEW_REQUIRED_NOT_GAME_FPS";
            public string source_sha, fixture_sha, unity_version, os, cpu, graphics;
            public string started_utc, finished_utc;
            public string native_player_fps = "NOT_MEASURED";
            public string real_enemy_ai = "NOT_MEASURED";
            public string real_r4_projectiles = "NOT_MEASURED";
            public string gpu_main_render = "NOT_MEASURED";
            public int cases=8, repetitions=3, measured_steps=200, warmup_steps=30;
            public float fixed_delta_s=.02f;
        }
        private static string Param(string[] args, string key)
        {
            int i = Array.IndexOf(args,key);
            return i>=0 && i+1<args.Length ? args[i+1] : null;
        }
        private static bool Hex40(string s)
        {
            if(s==null || s.Length!=40) return false;
            foreach(char c in s)
                if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')||(c>='A'&&c<='F'))) return false;
            return true;
        }
        private static double Quantile(double[] sorted,double q)
        {
            double x=(sorted.Length-1)*q;
            int a=(int)Math.Floor(x),b=(int)Math.Ceiling(x);
            return sorted[a]+(sorted[b]-sorted[a])*(x-a);
        }
        private static GameObject In(Scene scene,string name,Vector3 position)
        {
            var go=new GameObject(name);
            SceneManager.MoveGameObjectToScene(go,scene);
            go.transform.position=position;
            return go;
        }
        private static void Populate(Scene scene,Load load,out Rigidbody vehicle,out int colliders,out int bodies)
        {
            colliders=1; bodies=1;
            var floor=In(scene,"floor",new Vector3(0,-.5f,0));
            floor.AddComponent<BoxCollider>().size=new Vector3(200,1,200);
            var root=In(scene,"robot_compound_proxy",new Vector3(-12,1.3f,-4));
            vehicle=root.AddComponent<Rigidbody>();
            vehicle.mass=80f+load.Parts*3f;
            for(int i=0;i<load.Parts;i++)
            {
                var part=new GameObject("armor_proxy_"+i);
                part.transform.SetParent(root.transform,false);
                part.transform.localPosition=new Vector3((i%12)*.18f,((i/12)%3)*.18f,(i/36)*.18f);
                part.AddComponent<BoxCollider>().size=Vector3.one*.16f;
                colliders++;
            }
            for(int i=0;i<load.Enemies;i++)
            {
                var enemy=In(scene,"enemy_physics_proxy_"+i,
                    new Vector3(-5+(i%8)*2.5f,1.3f,3+(i/8)*2.5f));
                enemy.AddComponent<Rigidbody>().mass=70f;
                bodies++;
                for(int j=0;j<6;j++)
                {
                    var armor=new GameObject("enemy_armor_proxy_"+j);
                    armor.transform.SetParent(enemy.transform,false);
                    armor.transform.localPosition=new Vector3((j%3)*.3f,0,(j/3)*.3f);
                    armor.AddComponent<BoxCollider>().size=Vector3.one*.29f;
                    colliders++;
                }
            }
            for(int i=0;i<load.Projectiles;i++)
            {
                var shot=In(scene,"projectile_physics_proxy_"+i,
                    new Vector3(-24+(i%16)*.55f,1+(i/16)*.2f,3));
                var body=shot.AddComponent<Rigidbody>();
                body.mass=.2f;
                body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                body.linearVelocity=new Vector3(32,0,0);
                shot.AddComponent<SphereCollider>().radius=.08f;
                colliders++; bodies++;
            }
        }

        // Explicit opt-in only: Unity -batchmode -nographics -quit -executeMethod
        // RoboRougelike.B0150.PerformanceQA.B0150PhysicsScalingBench.RunBatch
        public static void RunBatch()
        {
            string[] args=Environment.GetCommandLineArgs();
            string output=Param(args,"-b0150-output");
            string gameSha=Param(args,"-b0150-source-sha");
            string fixtureSha=Param(args,"-b0150-fixture-sha");
            if(string.IsNullOrWhiteSpace(output)||!Hex40(gameSha)||!Hex40(fixtureSha))
                throw new ArgumentException("Missing output or 40-hex Git SHA inputs");
            output=Path.GetFullPath(output);
            if(Directory.Exists(output))
                throw new IOException("Refuse overwrite of previous performance evidence: "+output);
            Directory.CreateDirectory(output);
            try { Execute(output,gameSha.ToLowerInvariant(),fixtureSha.ToLowerInvariant()); }
            catch(Exception ex)
            {
                File.WriteAllText(Path.Combine(output,"FAILED.txt"),ex.ToString());
                UnityEngine.Debug.LogException(ex);
                throw;
            }
        }
        private static void Execute(string dir,string gameSha,string fixtureSha)
        {
            var data=new StringBuilder("case_id,repetition,step,simulate_ms\n");
            var totals=new StringBuilder("case_id,repetition,parts,projectiles,enemy_proxies,compound_colliders,rigidbodies,p50_ms,p95_ms,p99_ms,mean_ms,managed_alloc_bytes,gc0_collections\n");
            var meta=new Receipt {
                source_sha=gameSha,fixture_sha=fixtureSha,
                started_utc=DateTime.UtcNow.ToString("o"),
                unity_version=Application.unityVersion,os=SystemInfo.operatingSystem,
                cpu=SystemInfo.processorType,graphics=SystemInfo.graphicsDeviceName
            };
            foreach(var load in Cases)
            for(int repeat=1;repeat<=Repeats;repeat++)
            {
                Scene scene=default;
                try
                {
                    scene=EditorSceneManager.NewPreviewScene();
                    PhysicsScene physics=scene.GetPhysicsScene();
                    // Unity Editor may map a preview scene to DEFAULT physics.
                    // Such a case is UNSAFE: abort, do not measure the active game.
                    if(!physics.IsValid()||physics.Equals(Physics.defaultPhysicsScene))
                        throw new InvalidOperationException("No private PhysicsScene; refuse default");
                    Populate(scene,load,out Rigidbody vehicle,out int colliders,out int bodies);
                    for(int i=0;i<Warmup;i++) physics.Simulate(Dt);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    long before=GC.GetAllocatedBytesForCurrentThread();
                    int gc0=GC.CollectionCount(0);
                    double[] samples=new double[Steps];
                    var clock=new Stopwatch();
                    for(int step=0;step<Steps;step++)
                    {
                        clock.Restart();
                        physics.Simulate(Dt);
                        clock.Stop();
                        double ms=clock.ElapsedTicks*1000.0/Stopwatch.Frequency;
                        if(double.IsNaN(ms)||double.IsInfinity(ms)||ms<0)
                            throw new InvalidOperationException("Invalid timing");
                        samples[step]=ms;
                        data.Append(load.Name).Append(',').Append(repeat).Append(',')
                            .Append(step).Append(',').Append(ms.ToString("R",C)).Append('\n');
                    }
                    long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
                    int collections=GC.CollectionCount(0)-gc0;
                    if(vehicle==null||float.IsNaN(vehicle.position.y)||
                        float.IsInfinity(vehicle.position.y)||vehicle.position.y>2)
                        throw new InvalidOperationException("PhysX did not advance robot body");
                    Array.Sort(samples);
                    totals.Append(load.Name).Append(',').Append(repeat).Append(',')
                        .Append(load.Parts).Append(',').Append(load.Projectiles).Append(',')
                        .Append(load.Enemies).Append(',').Append(colliders).Append(',')
                        .Append(bodies).Append(',')
                        .Append(Quantile(samples,.50).ToString("R",C)).Append(',')
                        .Append(Quantile(samples,.95).ToString("R",C)).Append(',')
                        .Append(Quantile(samples,.99).ToString("R",C)).Append(',')
                        .Append(samples.Average().ToString("R",C)).Append(',')
                        .Append(bytes).Append(',').Append(collections).Append('\n');
                }
                finally
                {
                    if(scene.IsValid()&&scene.isLoaded&&!EditorSceneManager.ClosePreviewScene(scene))
                        throw new InvalidOperationException("Preview PhysicsScene cleanup failed");
                }
            }
            meta.finished_utc=DateTime.UtcNow.ToString("o");
            File.WriteAllText(Path.Combine(dir,"simulations.csv"),data.ToString());
            File.WriteAllText(Path.Combine(dir,"summary.csv"),totals.ToString());
            File.WriteAllText(Path.Combine(dir,"receipt.json"),JsonUtility.ToJson(meta,true));
            UnityEngine.Debug.Log("PERFSCALING001 / 4800 physics proxy steps / REVIEW_REQUIRED");
        }
    }
}
#endif

// B01-50 / PASS17 W27: authorized scratch ONLY, never auto-run in the game.
// Captures REAL Unity ProfilerRecorder frame samples. Does not fabricate measurements.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using B01.Performance.DebrisSleep001.Unity;
using Unity.Profiling;
using UnityEngine;

namespace B01.Performance.W27DebrisLab
{
    [DisallowMultipleComponent]
    public sealed class B0150W27DebrisPhysicsLab : MonoBehaviour
    {
        // A real scene starts idle; the operator must invoke Run Benchmark while playing.
        [SerializeField] private bool autoRunOnPlay = false;
        [SerializeField, Range(30, 300)] private int warmupFixedSteps = 120;
        [SerializeField, Range(120, 500)] private int measuredFrames = 180;
        [SerializeField] private bool permitScratchCsvWrite = true;
        private const string DependencyR3Sha = "b38216d94fb0f87af0c1cbc47090c7cb4bf7177e";
        private static readonly int[] Counts = { 10, 100, 500 };
        private static readonly string[] Scenarios = { "STATIC_REST", "DYNAMIC_CONTACT" };
        private static readonly bool[] AbbaCandidate = { false, true, true, false };
        private static readonly FieldInfo Consent = typeof(B0150OptionalDebrisSleepGovernor)
            .GetField("allowCosmeticSleep", BindingFlags.NonPublic | BindingFlags.Instance);
        private bool running;
        private int safetyFailures;
        private int inconclusive;
        public bool IsRunning { get { return running; } }
        public string LastEvidencePath { get; private set; }
        public string LastRunStatus { get; private set; }
        private GameObject trialRoot;
        private readonly List<Rigidbody> debris = new List<Rigidbody>(500);
        private readonly List<B0150W27ContactWitness> witnesses = new List<B0150W27ContactWitness>(500);
        private readonly List<B0150OptionalDebrisSleepGovernor> governors = new List<B0150OptionalDebrisSleepGovernor>(500);
        private Rigidbody activeControl;
        private B0150OptionalDebrisSleepGovernor activeGovernor;
        private StringBuilder csv;

        private void Start()
        {
            // This property defaults to FALSE in source and provided .unity scene.
            if (autoRunOnPlay) Debug.LogWarning("[B0150-W27] autoRunOnPlay is deliberately unsupported; invoke Run Benchmark manually during PlayMode.");
        }

        [ContextMenu("Run B01-50 W27 physics benchmark (scratch, PlayMode ONLY)")]
        public void RunBenchmark()
        {
            if (!Application.isPlaying || running || Consent == null ||
                warmupFixedSteps < 30 || warmupFixedSteps > 300 ||
                measuredFrames < 120 || measuredFrames > 500)
            {
                Debug.LogError("[B0150-W27] BLOCKED: requires explicit PlayMode, valid R3 source and stable settings.");
                return;
            }
            running = true;
            LastEvidencePath = null;
            LastRunStatus = "RUNNING";
            safetyFailures = 0;
            inconclusive = 0;
            StartCoroutine(RunSuite());
        }

        private static GameObject Primitive(PrimitiveType type, Transform parent, string name,
            Vector3 position, Vector3 scale)
        {
            GameObject o = GameObject.CreatePrimitive(type);
            o.name = name;
            o.transform.SetParent(parent, false);
            o.transform.localPosition = position;
            o.transform.localScale = scale;
            return o;
        }

        private static B0150OptionalDebrisSleepGovernor OptInOnlyOnInactive(GameObject objectToOptIn)
        {
            if (objectToOptIn.activeSelf) throw new InvalidOperationException("Benchmark must configure original R3 BEFORE Awake.");
            B0150OptionalDebrisSleepGovernor governor = objectToOptIn.AddComponent<B0150OptionalDebrisSleepGovernor>();
            Consent.SetValue(governor, true); // test-fixture reflection ONLY, no production API mutation
            return governor;
        }

        private void SetupTrial(int n, bool candidate, string scenario)
        {
            debris.Clear(); witnesses.Clear(); governors.Clear();
            activeGovernor = null;
            trialRoot = new GameObject("B0150_W27_PRIVATE_TRIAL_" + n + "_" + scenario);
            Transform parent = trialRoot.transform;
            Camera cam = null;
            if (Camera.main == null)
            {
                GameObject cameraObject = new GameObject("PRIVATE_BENCH_CAMERA");
                cameraObject.transform.SetParent(parent, false);
                cameraObject.transform.position = new Vector3(0, 34, -36);
                cameraObject.transform.LookAt(new Vector3(0, 0, 0));
                cam = cameraObject.AddComponent<Camera>();
                cam.farClipPlane = 150f;
            }
            Primitive(PrimitiveType.Cube, parent, "STATIC_FLOOR_NO_RIGIDBODY",
                new Vector3(0, -0.4f, 0), new Vector3(56, 0.8f, 56));

            int columns = Mathf.CeilToInt(Mathf.Sqrt(n));
            for (int i = 0; i < n; ++i)
            {
                float x = ((i % columns) - (columns - 1) / 2f) * 0.92f;
                float z = ((i / columns) - (columns - 1) / 2f) * 0.92f;
                GameObject o = Primitive(PrimitiveType.Cube, parent, "COSMETIC_DISPOSABLE_" + i,
                    new Vector3(x, 1.25f + (i % 4) * 0.11f, z), Vector3.one * 0.58f);
                o.SetActive(false);
                Rigidbody b = o.AddComponent<Rigidbody>();
                b.mass = 1f; b.useGravity = true; b.detectCollisions = true;
                b.interpolation = RigidbodyInterpolation.None;
                debris.Add(b);
                witnesses.Add(o.AddComponent<B0150W27ContactWitness>());
                if (candidate) governors.Add(OptInOnlyOnInactive(o));
                o.SetActive(true);
            }

            // One moving BODY collision lane; both baseline and candidate have the same load.
            if (scenario == "DYNAMIC_CONTACT")
            {
                int movers = Math.Max(1, n / 25);
                for (int i = 0; i < movers; ++i)
                {
                    float z = ((i * 5) % columns - (columns - 1) / 2f) * 0.92f;
                    GameObject m = Primitive(PrimitiveType.Cube, parent, "BODY_CONTACT_SWEEPER_" + i,
                        new Vector3(0, 0.55f, z), new Vector3(2.8f, 0.8f, 0.6f));
                    Rigidbody rb = m.AddComponent<Rigidbody>();
                    rb.useGravity = false; rb.isKinematic = true;
                    B0150W27Sweeper sweeper = m.AddComponent<B0150W27Sweeper>();
                    sweeper.Configure(rb, m.transform.position, 2.5f, 0.8f + i * .03f);
                }
            }

            // Deliberately opted-in HIGH-VELOCITY non-gameplay negative control:
            // must NEVER be explicitly slept while it is driven. Separate from N.
            GameObject active = Primitive(PrimitiveType.Sphere, parent, "ACTIVE_MOVING_NEGATIVE_CONTROL",
                new Vector3(-8, 3, 36), Vector3.one * .65f);
            active.SetActive(false);
            activeControl = active.AddComponent<Rigidbody>();
            activeControl.useGravity = false;
            activeControl.collisionDetectionMode = CollisionDetectionMode.Discrete;
            active.AddComponent<B0150W27ActiveControl>().Configure(activeControl);
            if (candidate) activeGovernor = OptInOnlyOnInactive(active);
            active.SetActive(true);
        }

        private static ProfilerRecorder StartRecorder(ProfilerCategory category, string marker)
        {
            try { return ProfilerRecorder.StartNew(category, marker, 256); }
            catch (Exception e)
            {
                Debug.LogWarning("[B0150-W27] Profiler marker unavailable: " + marker + " (" + e.GetType().Name + ")");
                return default;
            }
        }

        private static long ReadOrMissing(ProfilerRecorder r)
        {
            return r.Valid && r.Count > 0 ? r.LastValue : -1L;
        }

        private static string Cell(long value)
        {
            return value < 0 ? "NOT_MEASURED" : value.ToString(CultureInfo.InvariantCulture);
        }

        private IEnumerator RunOne(int n, string scenario, int order, bool candidate)
        {
            SetupTrial(n, candidate, scenario);
            for (int i = 0; i < warmupFixedSteps; ++i) yield return new WaitForFixedUpdate();

            // Recorder sampling is after a rendered frame; values are actual Unity markers.
            // Missing or unsupported counters remain NOT_MEASURED, never simulated/zero.
            ProfilerRecorder main = StartRecorder(ProfilerCategory.Internal, "Main Thread");
            ProfilerRecorder physics = StartRecorder(ProfilerCategory.Physics, "Physics.Simulate");
            ProfilerRecorder gc = StartRecorder(ProfilerCategory.Memory, "GC.Alloc");
            long[] mainNs = new long[measuredFrames];
            long[] physicsNs = new long[measuredFrames];
            long[] gcBytes = new long[measuredFrames];
            for (int frame = 0; frame < measuredFrames; ++frame)
            {
                yield return new WaitForEndOfFrame();
                mainNs[frame] = ReadOrMissing(main);
                physicsNs[frame] = ReadOrMissing(physics);
                gcBytes[frame] = ReadOrMissing(gc);
            }
            if (main.Valid) main.Dispose();
            if (physics.Valid) physics.Dispose();
            if (gc.Valid) gc.Dispose();

            int staticContacts = 0, bodyContacts = 0, sleeping = 0, sleepRequests = 0;
            for (int i = 0; i < n; ++i)
            {
                staticContacts += witnesses[i].staticStays;
                bodyContacts += witnesses[i].bodyStays;
                if (debris[i].IsSleeping()) ++sleeping;
                if (candidate) sleepRequests += governors[i].SleepRequests;
            }
            int activeRequests = activeGovernor != null ? activeGovernor.SleepRequests : 0;
            // No witnessed physical contacts => INCONCLUSIVE, never false PASS.
            bool contactObserved = scenario == "DYNAMIC_CONTACT" ? bodyContacts > 0 : staticContacts > 0;
            string safety = activeRequests > 0 ? "FAIL" : (contactObserved ? "PASS" : "INCONCLUSIVE");
            if (safety == "FAIL") ++safetyFailures;
            if (safety == "INCONCLUSIVE") ++inconclusive;
            string mode = candidate ? "R3_CANDIDATE" : "UNITY_AUTO_BASELINE";
            string unity = Application.unityVersion;
            string build = Application.platform.ToString();
            for (int frame = 0; frame < measuredFrames; ++frame)
            {
                csv.Append("FRAME,").Append(unity).Append(',').Append(build).Append(',')
                    .Append(DependencyR3Sha).Append(',').Append(scenario).Append(',')
                    .Append(n).Append(',').Append(order).Append(',').Append(mode).Append(',')
                    .Append(frame).Append(',').Append(Cell(mainNs[frame])).Append(',')
                    .Append(Cell(physicsNs[frame])).Append(',').Append(Cell(gcBytes[frame]))
                    .Append(",,,,,,\n");
            }
            csv.Append("SUMMARY,").Append(unity).Append(',').Append(build).Append(',')
                .Append(DependencyR3Sha).Append(',').Append(scenario).Append(',')
                .Append(n).Append(',').Append(order).Append(',').Append(mode)
                .Append(",NOT_APPLICABLE,NOT_APPLICABLE,NOT_APPLICABLE,NOT_APPLICABLE,")
                .Append(sleeping).Append(',').Append(sleepRequests).Append(',')
                .Append(activeRequests).Append(',').Append(staticContacts).Append(',')
                .Append(bodyContacts).Append(',').Append(safety).Append('\n');

            Debug.Log("[B0150-W27] " + scenario + " n=" + n + " mode=" + mode +
                " sleeping=" + sleeping + " requests=" + sleepRequests +
                " active-control-requests=" + activeRequests + " safety=" + safety +
                " static-stays=" + staticContacts + " body-stays=" + bodyContacts +
                " physics-marker=" + (physicsNs[measuredFrames-1] < 0 ? "NOT_MEASURED" : "RECORDED"));
            UnityEngine.Object.Destroy(trialRoot);
            trialRoot = null;
            // Allow Destroy to retire all physics objects before a new trial is constructed.
            yield return null;
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator RunSuite()
        {
            csv = new StringBuilder(1024 * 96);
            csv.Append("record_type,unity_version,platform,r3_dependency_sha,scenario,parts,order,variant,frame,main_thread_ns,physics_simulate_ns,gc_alloc_bytes,sleeping_count,sleep_request_count,active_control_sleep_requests,static_stays,body_stays,safety\n");
            try
            {
                foreach (int n in Counts)
                    foreach (string scenario in Scenarios)
                        for (int order = 0; order < AbbaCandidate.Length; ++order)
                            yield return RunOne(n, scenario, order, AbbaCandidate[order]);

                if (permitScratchCsvWrite)
                {
                    string file = Path.Combine(Application.temporaryCachePath,
                        "B0150_W27_NATIVE_" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture) +
                        "_" + Guid.NewGuid().ToString("N") + ".csv");
                    File.WriteAllText(file, csv.ToString(), new UTF8Encoding(false));
                    LastEvidencePath = file;
                    Debug.Log("[B0150-W27] LOCAL_NATIVE_RAW_CSV: " + file);
                }
                else Debug.Log("[B0150-W27] CSV persistence disabled by operator. No evidence file produced.");
                LastRunStatus = safetyFailures > 0 ? "FAILED_SAFETY" :
                    (inconclusive > 0 ? "INCONCLUSIVE_CONTACTS" :
                    (LastEvidencePath == null ? "NO_EVIDENCE_FILE" : "SOURCE_CAPTURE_ONLY_AWAITING_G1_REVIEW"));
                Debug.Log("[B0150-W27] RUN_STATUS=" + LastRunStatus +
                    "; unsafe=" + safetyFailures + "; inconclusive=" + inconclusive);
            }
            finally
            {
                if (trialRoot != null) UnityEngine.Object.Destroy(trialRoot);
                trialRoot = null;
                running = false;
            }
        }

        private void OnDisable()
        {
            if (running) Debug.LogWarning("[B0150-W27] Interrupted; no complete result is valid.");
        }
    }
}

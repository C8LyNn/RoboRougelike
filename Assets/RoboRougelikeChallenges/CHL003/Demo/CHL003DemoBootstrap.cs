using UnityEngine;

namespace B01.CHL003
{
    /// <summary>
    /// Stand-alone greybox demo bootstrap. Loaded by CHL003_Demo.unity.
    /// Creates actual MeshRenderer + nontrigger geometry and 4 trigger gates.
    /// No production scene or real R3/W8 input owns this component.
    /// </summary>
    public sealed class CHL003DemoBootstrap : MonoBehaviour
    {
        private CHL003CourseController course;
        private Rigidbody vehicle;
        private CHL003VehicleMotor motor;
        private GameObject owned;
        private Transform spawn;
        private Camera view;
        private int rigIndex;
        private int retries;
        private float steer;
        private float drive;
        private float guiDrive, guiSteer;

        private void Start()
        {
            owned = new GameObject("CHL003_GENERATED_DEMO_ONLY");
            var sun = new GameObject("CHL003_GRAYBOX_KEY_LIGHT");
            sun.transform.SetParent(owned.transform);
            sun.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
            var lighting = sun.AddComponent<Light>();
            lighting.type = LightType.Directional;
            lighting.intensity = 1.35f;
            BuildWorld();
            SelectRig(0);
            var cameraObject = new GameObject("CHL003_DEMO_FOLLOW_CAMERA");
            cameraObject.transform.SetParent(owned.transform);
            view = cameraObject.AddComponent<Camera>();
            view.fieldOfView = 60f;
            view.nearClipPlane = .08f;
            cameraObject.AddComponent<AudioListener>();
            view.transform.position = new Vector3(0f, 7f, -12f);
            view.transform.rotation = Quaternion.LookRotation(
                Vector3.forward * 12f - view.transform.position);
        }

        private GameObject Box(string label, Vector3 center, Vector3 size,
            float slopeDegrees = 0f, bool trigger = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "CHL003_" + label;
            go.transform.SetParent(owned.transform);
            go.transform.position = center;
            go.transform.localScale = size;
            go.transform.rotation = Quaternion.Euler(slopeDegrees, 0f, 0f);
            go.GetComponent<BoxCollider>().isTrigger = trigger;
            return go;
        }

        private CHL003Gate Gate(string label, float z, CHL003Gate.GateType type)
        {
            var obj = Box(label, new Vector3(0f, 2.25f, z),
                new Vector3(3.5f, 4.5f, .16f), trigger: true);
            var renderer = obj.GetComponent<Renderer>();
            renderer.enabled = false; // invisible physics volume; side markers show gates
            for (int side = -1; side <= 1; side += 2)
            {
                var marker = Box(label + "_VISIBLE_POST_" + side,
                    new Vector3(side * 1.87f, 1.3f, z),
                    new Vector3(.12f, 2.6f, .12f));
                // Visual-only markers never obstruct the actual drive lane.
                marker.GetComponent<Collider>().enabled = false;
            }
            var result = obj.AddComponent<CHL003Gate>();
            result.Configure(type);
            return result;
        }

        private void BuildWorld()
        {
            Box("APPROACH_GROUND", new Vector3(0f, -.35f, 4f),
                new Vector3(5.4f, .7f, 21f));
            Box("REAL_LOW_BARRIER", new Vector3(0f, .13f, 8.7f),
                new Vector3(3.8f, .26f, .45f));
            // Ramp is a real tilted solid collider, not a visually-only plane.
            Box("SOLID_RAMP", new Vector3(0f, .54f, 16f),
                new Vector3(3.7f, .55f, 7f), -12f);
            Box("NARROW_SOLID_BRIDGE", new Vector3(0f, 1.38f, 24.5f),
                new Vector3(3.0f, .60f, 10f));
            Box("FINAL_PLATFORM", new Vector3(0f, 1.38f, 34.5f),
                new Vector3(4.2f, .60f, 11f));
            spawn = new GameObject("CHL003_PHYSICAL_SPAWN").transform;
            spawn.SetParent(owned.transform);
            spawn.position = new Vector3(0f, 1.02f, 0f);
            spawn.rotation = Quaternion.identity;
            var ownerGo = new GameObject("CHL003_COURSE_CONTROLLER");
            ownerGo.transform.SetParent(owned.transform);
            course = ownerGo.AddComponent<CHL003CourseController>();
            var gates = new[] {
                Gate("CP0", 5f, CHL003Gate.GateType.Checkpoint),
                Gate("CP1", 11.5f, CHL003Gate.GateType.Checkpoint),
                Gate("CP2", 21f, CHL003Gate.GateType.Checkpoint),
                Gate("CP3", 29f, CHL003Gate.GateType.Checkpoint)
            };
            var finish = Gate("FINISH", 38f, CHL003Gate.GateType.Finish);
            // Note: neither gate OnTriggerEnter nor transform.Reset is invoked
            // directly; this activity requires physical collider crossings.
            gatesLocal = gates;
            finishLocal = finish;
        }
        private CHL003Gate[] gatesLocal;
        private CHL003Gate finishLocal;

        private void SelectRig(int next)
        {
            rigIndex = next;
            if (vehicle != null) Destroy(vehicle.gameObject);
            bool heavy = rigIndex == 1;
            var go = new GameObject(heavy ? "CHL003_HEAVY_RIG" : "CHL003_LIGHT_RIG");
            go.transform.SetParent(owned.transform);
            go.transform.position = spawn.position;
            var chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "CHASSIS_SOLID_HULL";
            chassis.transform.SetParent(go.transform, false);
            chassis.transform.localScale = heavy ?
                new Vector3(2.5f, .65f, 3.4f) :
                new Vector3(1.45f, .52f, 2.3f);
            chassis.transform.localPosition = new Vector3(0f, -.12f, 0f);
            float width = heavy ? 1.0f : .59f;
            for (int side = -1; side <= 1; side += 2)
            for (int end = -1; end <= 1; end += 2)
            {
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                wheel.name = "PHYSICAL_COLLIDER_WHEEL";
                wheel.transform.SetParent(go.transform, false);
                wheel.transform.localPosition = new Vector3(side * width,
                    -.52f, end * (heavy ? 1.15f : .75f));
                wheel.transform.localScale = new Vector3(.82f, .82f, .82f);
            }
            vehicle = go.AddComponent<Rigidbody>();
            vehicle.mass = heavy ? 1150f : 270f;
            vehicle.interpolation = RigidbodyInterpolation.Interpolate;
            vehicle.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            vehicle.constraints = RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;
            motor = go.AddComponent<CHL003VehicleMotor>();
            // Distinct mass/footprint and force-to-mass response.
            motor.Configure(course, heavy ? 7000f : 2800f, heavy ? 1550f : 390f);
            course.Configure(vehicle, gatesLocal, finishLocal, spawn,
                failureY: -3f);
            course.Begin();
            Physics.SyncTransforms();
            retries = 0;
        }

        private void Update()
        {
            // UI hold controls are a complete fallback when the game uses
            // the new-only Input System without old axes.
            drive = guiDrive;
            steer = guiSteer;
            bool retry = false, pause = false, switchRig = false;
#if ENABLE_LEGACY_INPUT_MANAGER
            float keysDrive = Input.GetAxisRaw("Vertical");
            float keysSteer = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(keysDrive) > .01f) drive = keysDrive;
            if (Mathf.Abs(keysSteer) > .01f) steer = keysSteer;
            retry = Input.GetKeyDown(KeyCode.R);
            pause = Input.GetKeyDown(KeyCode.P);
            switchRig = Input.GetKeyDown(KeyCode.Tab);
#endif
            if (retry) { course.Retry(); course.Begin(); retries++; }
            if (pause) course.SetPaused(!course.IsPaused);
            if (switchRig) SelectRig(1 - rigIndex);
            if (motor != null) motor.SetAxes(drive, steer);
        }

        private void LateUpdate()
        {
            if (view == null || vehicle == null) return;
            var target = vehicle.position + Vector3.up * 1.3f;
            var desired = target - vehicle.transform.forward * 9f + Vector3.up * 5f;
            view.transform.position = Vector3.Lerp(view.transform.position, desired,
                Mathf.Clamp01(Time.deltaTime * 6f));
            view.transform.rotation = Quaternion.LookRotation(
                (target - view.transform.position).normalized, Vector3.up);
        }

        private void OnGUI()
        {
            if (course == null || course.Core == null) return;
            var c = course.Core;
            GUILayout.BeginArea(new Rect(16, 16, 545, 240), GUI.skin.box);
            GUILayout.Label("CHL-003 ISOLATED PHYSICS OBSTACLE COURSE | " +
                (rigIndex == 0 ? "LIGHT" : "HEAVY"));
            GUILayout.Label("Gate " + c.CheckpointIndex + "/" + c.CheckpointCount +
                "  Time " + c.ElapsedSeconds.ToString("F2") +
                "  State " + c.State + "  Fail " + c.LastFailure);
            GUILayout.Label("W/S drive, A/D steer (legacy input), or GUI hold; R retry, P pause, TAB switch rig");
            GUILayout.BeginHorizontal();
            float uiForward = GUILayout.RepeatButton("HOLD DRIVE") ? 1f : 0f;
            float uiBackward = GUILayout.RepeatButton("HOLD REVERSE") ? -1f : 0f;
            float uiLeft = GUILayout.RepeatButton("HOLD LEFT") ? -1f : 0f;
            float uiRight = GUILayout.RepeatButton("HOLD RIGHT") ? 1f : 0f;
            GUILayout.EndHorizontal();
            guiDrive = uiForward + uiBackward;
            guiSteer = uiLeft + uiRight;
            GUILayout.Label("Goal: cross 4 gates in order, over low barrier + ramp + narrow bridge, reach finish.");
            if (GUILayout.Button("Retry / Reset course")) { course.Retry(); course.Begin(); retries++; }
            if (GUILayout.Button("Pause / Resume")) course.SetPaused(!course.IsPaused);
            if (GUILayout.Button("Swap Light / Heavy")) SelectRig(1 - rigIndex);
            GUILayout.Label("Actual Rigidbody mass " + vehicle.mass + " kg | retries " + retries);
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (owned != null) Destroy(owned);
        }
    }
}

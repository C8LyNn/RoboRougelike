using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace B01.CAMERA001
{
    /// <summary>Manual, opt-in standalone demo. No R1/R3/R6 or production scene references.</summary>
    public sealed class Camera001Demo : MonoBehaviour
    {
        private Camera001Rig rig;
        private Transform vehicle;
        private GameObject vehicleObject;
        private int modelIndex; // Light, Heavy, Tall, Wide
        private GameObject corridorLeft, corridorRight, detachablePart;
        private bool corridorEnabled;
        private static readonly string[] Models = { "Light", "Heavy", "Tall", "Wide" };
        private float yaw;
        private float pitch = 18f;
        private float zoom = 8f;

        private void Start()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "CAMERA001_DEMO_FLOOR";
            floor.transform.position = new Vector3(0, -.5f, 0);
            floor.transform.localScale = new Vector3(45, 1, 45);
            for (int i = -1; i <= 1; i++)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "CAMERA001_WALL_" + i;
                wall.transform.position = new Vector3(8 + i * 4, 2, 0);
                wall.transform.localScale = new Vector3(.7f, 4, 4);
            }
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "CAMERA001_VISIBLE_TARGET";
            target.transform.position = new Vector3(0, 1.6f, 14);
            target.transform.localScale = Vector3.one * 1.4f;

            var cameraObject = new GameObject("CAMERA001_STANDALONE_CAMERA");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 62f;
            rig = cameraObject.AddComponent<Camera001Rig>();
            var lightObject = new GameObject("CAMERA001_DEMO_LIGHT");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(45, -35, 0);
            RebuildVehicle();
        }

        private void RebuildVehicle()
        {
            if (vehicleObject != null) Destroy(vehicleObject);
            string model = Models[modelIndex];
            vehicleObject = new GameObject("CAMERA001_" + model.ToUpperInvariant());
            vehicle = vehicleObject.transform;
            Vector3 dimensions = model == "Light" ? new Vector3(2.98f, 1.56f, 2.72f) :
                model == "Heavy" ? new Vector3(5.18f, 2.12f, 4.92f) :
                model == "Tall" ? new Vector3(3.8f, 9.16f, 4.92f) :
                new Vector3(10.42f, 2.12f, 4.92f);
            AddPart("Hull", new Vector3(0, dimensions.y * .5f, 0), dimensions);
            AddPart("FrontWeapon", new Vector3(0, dimensions.y + .45f, dimensions.z * .3f),
                new Vector3(.7f, .9f, .8f));
            var detachable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            detachable.name = "CAMERA001_DETACHABLE_TOP";
            detachable.transform.SetParent(vehicle, false);
            detachable.transform.localPosition = new Vector3(0f, dimensions.y + 1f, 0f);
            detachable.transform.localScale = new Vector3(.75f, 1f, .75f);
            detachablePart = detachable;
            if (corridorLeft != null) Destroy(corridorLeft);
            if (corridorRight != null) Destroy(corridorRight);
            corridorLeft = null; corridorRight = null;
            corridorEnabled = false;
            if (rig != null) rig.Configure(vehicle, rig.GetComponent<Camera>());
        }

        private void AddPart(string partName, Vector3 localPosition, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = partName;
            go.transform.SetParent(vehicle, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
        }

        private void Update()
        {
            float dx = 0, dy = 0, dz = 0;
            bool swap = false;
            bool toggleCorridor = false, detach = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed) dx -= 95f * Time.deltaTime;
                if (Keyboard.current.dKey.isPressed) dx += 95f * Time.deltaTime;
                if (Keyboard.current.wKey.isPressed) dy += 65f * Time.deltaTime;
                if (Keyboard.current.sKey.isPressed) dy -= 65f * Time.deltaTime;
                if (Keyboard.current.qKey.isPressed) dz -= 7f * Time.deltaTime;
                if (Keyboard.current.eKey.isPressed) dz += 7f * Time.deltaTime;
                swap = Keyboard.current.tabKey.wasPressedThisFrame;
                toggleCorridor = Keyboard.current.cKey.wasPressedThisFrame;
                detach = Keyboard.current.xKey.wasPressedThisFrame;
            }
#else
            if (Input.GetKey(KeyCode.A)) dx -= 95f * Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) dx += 95f * Time.deltaTime;
            if (Input.GetKey(KeyCode.W)) dy += 65f * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) dy -= 65f * Time.deltaTime;
            if (Input.GetKey(KeyCode.Q)) dz -= 7f * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) dz += 7f * Time.deltaTime;
            swap = Input.GetKeyDown(KeyCode.Tab);
            toggleCorridor = Input.GetKeyDown(KeyCode.C);
            detach = Input.GetKeyDown(KeyCode.X);
#endif
            if (swap) { modelIndex = (modelIndex + 1) % Models.Length; RebuildVehicle(); }
            if (toggleCorridor) ToggleCorridor();
            if (detach && detachablePart != null) detachablePart.SetActive(false);
            yaw = Mathf.Repeat(yaw + dx, 360f);
            pitch = Mathf.Clamp(pitch + dy, -8f, 70f);
            zoom = Mathf.Clamp(zoom + dz, 3f, 18f);
            if (rig != null) rig.SetOrbit(yaw, pitch, zoom);
        }

        private void ToggleCorridor()
        {
            corridorEnabled = !corridorEnabled;
            if (corridorLeft != null) Destroy(corridorLeft);
            if (corridorRight != null) Destroy(corridorRight);
            corridorLeft = null;
            corridorRight = null;
            if (!corridorEnabled || vehicle == null) return;
            Bounds b = Camera001Rig.GetHullBounds(vehicle);
            float width = b.extents.x + .7f;
            float height = b.size.y + 5f;
            corridorLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corridorRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corridorLeft.name = "CAMERA001_CORRIDOR_LEFT";
            corridorRight.name = "CAMERA001_CORRIDOR_RIGHT";
            corridorLeft.transform.position = new Vector3(-width, height * .5f, 0f);
            corridorRight.transform.position = new Vector3(width, height * .5f, 0f);
            corridorLeft.transform.localScale = new Vector3(.4f, height, 64f);
            corridorRight.transform.localScale = new Vector3(.4f, height, 64f);
            Physics.SyncTransforms();
        }

        private void OnGUI()
        {
            if (rig == null) return;
            GUILayout.BeginArea(new Rect(12, 12, 820, 145), GUI.skin.box);
            GUILayout.Label("CAMERA-001 R1.2: A/D orbit | W/S pitch | Q/E zoom | TAB four rigs | C corridor | X detach");
            GUILayout.Label("Rig=" + Models[modelIndex] + " yaw=" + yaw.ToString("F0") +
                " pitch=" + pitch.ToString("F0") + " zoom=" + zoom.ToString("F1") +
                " corridor=" + corridorEnabled);
            GUILayout.Label("ACTUAL OWN RAY BLOCKED=" + rig.ActualOwnHullIntersectsCrosshair +
                " | WORLD CLIPPED=" + rig.WorldClipped + " | PIVOT OVERLAP=" + rig.WorldOverlapAtAim +
                " | FINAL WORLD OVERLAP=" + rig.WorldOverlapAtCamera);
            GUILayout.Label("Step=" + rig.LastCameraStepMeters.ToString("F3") +
                "m | actual aim error=" + rig.LastAimErrorDegrees.ToString("F4") + "deg");
            GUILayout.EndArea();
        }
    }
}

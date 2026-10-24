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
        private bool heavy;
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
            vehicleObject = new GameObject(heavy ? "CAMERA001_HEAVY" : "CAMERA001_LIGHT");
            vehicle = vehicleObject.transform;
            float width = heavy ? 4.5f : 2.1f;
            float length = heavy ? 5.8f : 3.2f;
            AddPart("Hull", new Vector3(0, .9f, 0), new Vector3(width, 1.4f, length));
            AddPart("FrontWeapon", new Vector3(0, heavy ? 4.6f : 3.2f, 0),
                new Vector3(heavy ? 1.8f : .7f, heavy ? 5f : 3.2f, 1.2f));
            AddPart("SideModule", new Vector3(width *.5f, 2.4f, .5f),
                new Vector3(.7f, 2.4f, 1.6f));
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
            }
#else
            if (Input.GetKey(KeyCode.A)) dx -= 95f * Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) dx += 95f * Time.deltaTime;
            if (Input.GetKey(KeyCode.W)) dy += 65f * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) dy -= 65f * Time.deltaTime;
            if (Input.GetKey(KeyCode.Q)) dz -= 7f * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) dz += 7f * Time.deltaTime;
            swap = Input.GetKeyDown(KeyCode.Tab);
#endif
            if (swap) { heavy = !heavy; RebuildVehicle(); }
            yaw = Mathf.Repeat(yaw + dx, 360f);
            pitch = Mathf.Clamp(pitch + dy, -8f, 70f);
            zoom = Mathf.Clamp(zoom + dz, 3f, 18f);
            if (rig != null) rig.SetOrbit(yaw, pitch, zoom);
        }

        private void OnGUI()
        {
            if (rig == null) return;
            GUILayout.BeginArea(new Rect(12, 12, 720, 120), GUI.skin.box);
            GUILayout.Label("CAMERA-001 isolated demo: A/D orbit 360 | W/S pitch | Q/E zoom | TAB Light/Heavy");
            GUILayout.Label("Rig=" + (heavy ? "Heavy" : "Light") + " yaw=" + yaw.ToString("F0") +
                " pitch=" + pitch.ToString("F0") + " zoom=" + zoom.ToString("F1"));
            GUILayout.Label("OWN HULL IN RETICLE=" + rig.LastSolution.OwnHullIntersectsCrosshair +
                " | WORLD CLIPPED=" + rig.WorldClipped + " | PIVOT OVERLAP=" + rig.WorldOverlapAtAim);
            GUILayout.EndArea();
        }
    }
}

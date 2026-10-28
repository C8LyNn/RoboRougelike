using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.B0132P17
{
    /// <summary>
    /// Scratch-scene operator for four distinct wake modes. Never drives real
    /// gameplay traction, actual wheels, collision, damage or save state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class B0132WheelWakeDemo : MonoBehaviour
    {
        [SerializeField] private WheelWakeMode mode = WheelWakeMode.Cruise;
        [SerializeField, Range(0f, 20f)] private float speedMetersPerSecond = 9f;
        [SerializeField, Range(0f, 1f)] private float normalizedSlip = .45f;
        [SerializeField] private bool grounded = true;
        [SerializeField] private bool vfxEnabled = true;

        private B0132WheelWakeEmitter emitter;
        private readonly List<Material> ownedMaterials = new List<Material>();

        private void Awake()
        {
            emitter = gameObject.AddComponent<B0132WheelWakeEmitter>();
            CreateShowcase();
        }

        private Material MakeMaterial(Color tint)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            ownedMaterials.Add(material);
            return material;
        }

        private void Box(string title, Vector3 localPosition, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = title;
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = size;
            var c = obj.GetComponent<Collider>();
            if (c != null) { c.enabled = false; Destroy(c); }
            var r = obj.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = material;
        }

        private void CreateShowcase()
        {
            var deck = MakeMaterial(new Color(.12f, .17f, .21f));
            var chassis = MakeMaterial(new Color(.28f, .49f, .56f));
            var wheels = MakeMaterial(new Color(.07f, .10f, .13f));
            Box("ground test lane", new Vector3(0f, -.9f, -1.4f), new Vector3(7f, .12f, 10f), deck);
            Box("vehicle shell (dummy)", new Vector3(0f, .24f, .20f), new Vector3(2.1f, .72f, 2.65f), chassis);
            Box("left wheel dummy", new Vector3(-1.04f, -.26f, -.62f), new Vector3(.34f, .82f, .92f), wheels);
            Box("right wheel dummy", new Vector3(1.04f, -.26f, -.62f), new Vector3(.34f, .82f, .92f), wheels);
            if (Camera.main == null)
            {
                var obj = new GameObject("B01-32 Wake Demo Camera");
                obj.transform.position = new Vector3(4.4f, 3.8f, -7f);
                obj.transform.LookAt(new Vector3(0f, -.2f, -1.0f));
                var cam = obj.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(.025f, .05f, .075f);
                cam.fieldOfView = 51f;
                obj.transform.SetParent(transform, true);
            }
            var light = new GameObject("B01-32 Wake Demo Light");
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            var lamp = light.AddComponent<Light>();
            lamp.type = LightType.Directional;
            lamp.intensity = 1.3f;
            light.transform.SetParent(transform, true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) mode = WheelWakeMode.Off;
            if (Input.GetKeyDown(KeyCode.Alpha2)) mode = WheelWakeMode.Cruise;
            if (Input.GetKeyDown(KeyCode.Alpha3)) mode = WheelWakeMode.Drift;
            if (Input.GetKeyDown(KeyCode.Alpha4)) mode = WheelWakeMode.Brake;
            if (Input.GetKeyDown(KeyCode.X)) vfxEnabled = !vfxEnabled;
            emitter.SetVisualInput(mode, speedMetersPerSecond, normalizedSlip,
                grounded, vfxEnabled);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12f, 12f, 440f, 260f), "B01-32 / PASS 17 / WHEEL WAKE");
            if (GUI.Button(new Rect(26f, 45f, 95f, 32f), "1 OFF")) mode = WheelWakeMode.Off;
            if (GUI.Button(new Rect(126f, 45f, 95f, 32f), "2 CRUISE")) mode = WheelWakeMode.Cruise;
            if (GUI.Button(new Rect(226f, 45f, 95f, 32f), "3 DRIFT")) mode = WheelWakeMode.Drift;
            if (GUI.Button(new Rect(326f, 45f, 95f, 32f), "4 BRAKE")) mode = WheelWakeMode.Brake;
            GUI.Label(new Rect(26f, 84f, 400f, 25f), "Speed (visual input): " + speedMetersPerSecond.ToString("F1") + " m/s");
            speedMetersPerSecond = GUI.HorizontalSlider(new Rect(26f, 114f, 396f, 18f), speedMetersPerSecond, 0f, 20f);
            GUI.Label(new Rect(26f, 142f, 400f, 25f), "Slip (visual input): " + normalizedSlip.ToString("F2"));
            normalizedSlip = GUI.HorizontalSlider(new Rect(26f, 171f, 396f, 18f), normalizedSlip, 0f, 1f);
            grounded = GUI.Toggle(new Rect(26f, 197f, 175f, 25f), grounded, "Contact enabled");
            vfxEnabled = GUI.Toggle(new Rect(215f, 197f, 180f, 25f), vfxEnabled, "VFX enabled (X)");
            GUI.Label(new Rect(26f, 234f, 405f, 23f),
                "Mode: " + mode + " / dust: " + (emitter != null && emitter.IsEmitting));
        }

        private void OnDestroy()
        {
            foreach (var material in ownedMaterials)
                if (material != null) Destroy(material);
            ownedMaterials.Clear();
        }
    }
}

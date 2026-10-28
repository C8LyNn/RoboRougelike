using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.B0132P16
{
    /// <summary>Independent visual sandbox: hit / damage-detach / warning.
    /// No R1/R3/R4/VFX001 or player-HP dependencies.</summary>
    [DisallowMultipleComponent]
    public sealed class B0132Showcase : MonoBehaviour
    {
        [SerializeField] private bool autoCycle = true;
        [SerializeField, Range(1f, 8f)] private float cycleSeconds = 3.2f;
        private readonly List<B0132Effect> active = new List<B0132Effect>();
        private readonly List<Material> materials = new List<Material>();
        private GameObject stage;
        private float timer;

        private void Awake()
        {
            CreateCamera();
            CreateStage();
        }

        private void CreateCamera()
        {
            if (Camera.main != null) return;
            var cameraObject = new GameObject("B01-32 Demo Camera");
            cameraObject.transform.SetParent(transform);
            cameraObject.transform.position = new Vector3(0f, 6.5f, -16f);
            cameraObject.transform.LookAt(new Vector3(0f, 1.1f, 0f));
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 47f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .047f, .072f);
            var lightObject = new GameObject("B01-32 Demo Light");
            lightObject.transform.SetParent(transform);
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
        }

        private Material Material(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            materials.Add(material);
            return material;
        }

        private GameObject Cube(string title, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = title;
            obj.transform.SetParent(stage.transform, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            var r = obj.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = material;
            return obj;
        }

        private void CreateStage()
        {
            stage = new GameObject("B01-32 P16 Preview Stage");
            stage.transform.SetParent(transform, false);
            Material deck = Material(new Color(.11f, .17f, .22f));
            Material steel = Material(new Color(.24f, .33f, .37f));
            Material highlight = Material(new Color(.13f, .56f, .66f));
            Cube("deck", new Vector3(0f, -.27f, 0f), new Vector3(13f, .35f, 5f), deck);
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 4.2f;
                Cube("display pedestal " + i, new Vector3(x, .05f, 0f),
                    new Vector3(3.3f, .45f, 3.3f), steel);
                Cube("mock robot chassis " + i, new Vector3(x, .75f, 0f),
                    new Vector3(1.35f, .95f, 1.4f), steel);
                Cube("front module detail " + i, new Vector3(x, 1.2f, -.78f),
                    new Vector3(.48f, .26f, .28f), highlight);
                Cube("pedestal trim " + i, new Vector3(x, .32f, -1.6f),
                    new Vector3(3f, .08f, .08f), highlight);
            }
        }

        private Vector3 Position(int type)
        {
            return new Vector3((type - 1) * 4.2f, 1.5f, -.92f);
        }

        public B0132Effect Spawn(int type)
        {
            type = Mathf.Clamp(type, 0, 2);
            var obj = new GameObject("B01-32 " + (B0132Effect.EffectKind)type);
            obj.transform.SetParent(transform);
            var fx = obj.AddComponent<B0132Effect>();
            fx.Play((B0132Effect.EffectKind)type, Position(type),
                new Vector3(.2f, .65f, -1f));
            active.Add(fx);
            return fx;
        }

        public void Clear()
        {
            foreach (var effect in active)
            {
                if (effect == null) continue;
                effect.StopVisuals();
                Destroy(effect.gameObject);
            }
            active.Clear();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Spawn(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Spawn(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Spawn(2);
            if (Input.GetKeyDown(KeyCode.X)) Clear();
            if (Input.GetKeyDown(KeyCode.Space)) SpawnAll();
            for (int i = active.Count - 1; i >= 0; i--)
                if (active[i] == null || !active[i].IsPlaying)
                    active.RemoveAt(i);
            if (!autoCycle) return;
            timer += Time.deltaTime;
            if (timer >= cycleSeconds)
            {
                timer = 0f;
                SpawnAll();
            }
        }

        private void SpawnAll()
        {
            for (int i = 0; i < 3; i++) Spawn(i);
        }

        private void OnGUI()
        {
            var rect = new Rect(18f, 16f, 370f, 210f);
            GUI.Box(rect, "B01-32 / PASS 16 / VFX VISUAL SANDBOX");
            GUI.Label(new Rect(34f, 52f, 340f, 28f), "1: Projectile impact     2: Part separation");
            GUI.Label(new Rect(34f, 78f, 340f, 28f), "3: Critical warning       X: Clear");
            if (GUI.Button(new Rect(34f, 109f, 110f, 36f), "1 · HIT")) Spawn(0);
            if (GUI.Button(new Rect(150f, 109f, 110f, 36f), "2 · DETACH")) Spawn(1);
            if (GUI.Button(new Rect(266f, 109f, 110f, 36f), "3 · WARN")) Spawn(2);
            if (GUI.Button(new Rect(34f, 152f, 165f, 34f), "Trigger all · SPACE")) SpawnAll();
            if (GUI.Button(new Rect(205f, 152f, 165f, 34f), "Clear all · X")) Clear();
            GUI.Label(new Rect(18f, 233f, 800f, 30f),
                "Standalone cosmetics only: HP, physics, damage and weapon behavior unchanged.");
        }

        private void OnDestroy()
        {
            Clear();
            foreach (var material in materials)
                if (material != null) Destroy(material);
            materials.Clear();
        }
    }
}

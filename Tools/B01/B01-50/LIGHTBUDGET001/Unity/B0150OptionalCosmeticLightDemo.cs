using System;
using UnityEngine;

namespace B01.Performance.LightBudget001.Unity
{
    /// <summary>
    /// Standalone PlayMode reproducer; attach only in an empty disposable scene.
    /// Creates no physics or persistence and does not modify the existing game.
    /// </summary>
    public sealed class B0150OptionalCosmeticLightDemo : MonoBehaviour
    {
        [SerializeField, Range(4, 24)] private int lightCount = 12;
        [SerializeField, Range(0, 12)] private int maximumEnabled = 4;

        private GameObject generatedRoot;
        private Camera generatedCamera;
        private bool ownsCamera;

        private void Start()
        {
            generatedRoot = new GameObject("B01-50 LIGHTBUDGET demo (runtime only)");
            generatedRoot.SetActive(false);
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("B01-50 demo camera");
                camera = cameraObject.AddComponent<Camera>();
                generatedCamera = camera;
                ownsCamera = true;
            }

            var slots = new B0150OptionalCosmeticLightBudget.LightSlot[lightCount];
            for (int i = 0; i < lightCount; i++)
            {
                float a = i * Mathf.PI * 2f / lightCount;
                Vector3 pos = new Vector3(Mathf.Sin(a) * 8f, 0f, Mathf.Cos(a) * 8f);
                GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cube);
                column.name = "Decorative sample column " + i;
                column.transform.SetParent(generatedRoot.transform, false);
                column.transform.position = pos;
                column.transform.localScale = new Vector3(1.25f, 2f, 1.25f);
                Collider collider = column.GetComponent<Collider>();
                if (collider != null) Destroy(collider); // visual sample, no physics

                GameObject lamp = new GameObject("Optional point light " + i);
                lamp.transform.SetParent(generatedRoot.transform, false);
                lamp.transform.position = pos + Vector3.up * 2f;
                Light point = lamp.AddComponent<Light>();
                point.type = LightType.Point;
                point.range = 9f;
                point.intensity = 3f;
                point.shadows = LightShadows.None;
                point.enabled = true;
                slots[i] = new B0150OptionalCosmeticLightBudget.LightSlot
                {
                    Light = point,
                    Importance = 50
                };
            }

            GameObject owner = new GameObject("Optional cosmetic-light budget manager");
            owner.transform.SetParent(generatedRoot.transform, false);
            // Root remains inactive: Configure happens before manager.Awake.
            var manager = owner.AddComponent<B0150OptionalCosmeticLightBudget>();
            if (!manager.Configure(camera, slots, Mathf.Min(maximumEnabled, lightCount), 2.5f, 4))
            {
                Debug.LogError("B01-50 LIGHTBUDGET demo could not configure.", this);
                Destroy(generatedRoot);
                return;
            }
            generatedRoot.SetActive(true);
        }

        private void LateUpdate()
        {
            if (!ownsCamera || generatedCamera == null) return;
            float angle = Time.unscaledTime * 0.35f;
            generatedCamera.transform.position =
                new Vector3(Mathf.Sin(angle) * 11f, 6f, Mathf.Cos(angle) * 11f);
            generatedCamera.transform.LookAt(Vector3.zero);
        }

        private void OnDestroy()
        {
            if (generatedRoot != null) Destroy(generatedRoot);
            if (ownsCamera && generatedCamera != null) Destroy(generatedCamera.gameObject);
        }
    }
}

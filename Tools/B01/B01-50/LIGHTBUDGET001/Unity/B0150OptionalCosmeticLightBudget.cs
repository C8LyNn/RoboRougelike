using System;
using System.Collections.Generic;
using B01.Performance.LightBudget001;
using UnityEngine;

namespace B01.Performance.LightBudget001.Unity
{
    /// <summary>
    /// Optional cosmetic lights ONLY. Do not register combat-critical, navigational,
    /// warning, puzzle, main-key or any other gameplay-significant lights.
    /// Exclusive owner of opt-in Light.enabled while active.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class B0150OptionalCosmeticLightBudget : MonoBehaviour
    {
        [Serializable]
        public sealed class LightSlot
        {
            public Light Light;
            [Range(0, 100)] public int Importance = 50;
        }

        [SerializeField] private Camera observer;
        [SerializeField] private LightSlot[] cosmeticLights = Array.Empty<LightSlot>();
        [SerializeField, Min(0)] private int maximumEnabled = 8;
        [SerializeField, Range(0f, 50f)] private float distanceHysteresisMeters = 3f;
        [SerializeField, Range(1, 30)] private int updateEveryNFrames = 6;

        private CosmeticLightBudget policy;
        private CosmeticLightCandidate[] samples;
        private bool[] desired;
        private bool[] originallyEnabled;
        private bool configured;
        private int frameIndex;

        /// <summary>
        /// Used by the optional standalone demo, before an inactive GameObject
        /// is enabled and Awake initializes the selector. No reflection.
        /// </summary>
        public bool Configure(Camera camera, LightSlot[] lights, int limit, float hysteresis, int cadence)
        {
            if (configured) return false;
            observer = camera;
            cosmeticLights = lights;
            maximumEnabled = limit;
            distanceHysteresisMeters = hysteresis;
            updateEveryNFrames = cadence;
            return true;
        }

        private void Awake()
        {
            if (cosmeticLights == null || cosmeticLights.Length == 0 ||
                cosmeticLights.Length > CosmeticLightBudget.MaximumSlots ||
                maximumEnabled < 0 || maximumEnabled > cosmeticLights.Length ||
                updateEveryNFrames < 1 || updateEveryNFrames > 30)
            {
                Debug.LogError("B01-50 LIGHTBUDGET: invalid opt-in configuration.", this);
                enabled = false;
                return;
            }

            // Never mutate a light before the entire configuration has passed validation.
            var seen = new HashSet<Light>();
            for (int i = 0; i < cosmeticLights.Length; i++)
            {
                LightSlot slot = cosmeticLights[i];
                if (slot == null || slot.Light == null || !seen.Add(slot.Light) ||
                    slot.Importance < 0 || slot.Importance > 100)
                {
                    Debug.LogError("B01-50 LIGHTBUDGET: null/duplicate light or invalid importance.", this);
                    enabled = false;
                    return;
                }
            }

            samples = new CosmeticLightCandidate[cosmeticLights.Length];
            desired = new bool[cosmeticLights.Length];
            originallyEnabled = new bool[cosmeticLights.Length];
            for (int i = 0; i < cosmeticLights.Length; i++)
                originallyEnabled[i] = cosmeticLights[i].Light.enabled;
            policy = new CosmeticLightBudget(cosmeticLights.Length);
            configured = true;
        }

        private void LateUpdate()
        {
            if (!configured || ++frameIndex % updateEveryNFrames != 0) return;
            Camera activeCamera = observer != null ? observer : Camera.main;
            if (activeCamera == null) return; // Fail unchanged rather than guessing a viewpoint.

            Vector3 point = activeCamera.transform.position;
            for (int i = 0; i < cosmeticLights.Length; i++)
            {
                Light l = cosmeticLights[i].Light;
                bool available = l != null && originallyEnabled[i] && l.gameObject.activeInHierarchy;
                float distanceSquared = available ? (l.transform.position - point).sqrMagnitude : 0f;
                samples[i] = new CosmeticLightCandidate(i, cosmeticLights[i].Importance,
                    distanceSquared, available, available && l.enabled);
            }

            if (!policy.TrySelect(samples, maximumEnabled, distanceHysteresisMeters, desired, out string error))
            {
                Debug.LogWarning("B01-50 LIGHTBUDGET policy rejected update: " + error, this);
                return; // No partial commit to any Light.enabled.
            }

            for (int i = 0; i < cosmeticLights.Length; i++)
            {
                Light l = cosmeticLights[i].Light;
                if (l != null && l.enabled != desired[i])
                    l.enabled = desired[i];
            }
        }

        private void OnDisable()
        {
            RestoreOwnedLights();
        }

        private void OnDestroy()
        {
            RestoreOwnedLights();
        }

        private void RestoreOwnedLights()
        {
            if (!configured) return;
            for (int i = 0; i < cosmeticLights.Length; i++)
            {
                Light l = cosmeticLights[i].Light;
                if (l != null && l.enabled != originallyEnabled[i])
                    l.enabled = originallyEnabled[i];
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R6;
using RoboRougelike.VFX001;

namespace RoboRougelike.VFX001Integration
{
    /// <summary>
    /// Read-only, live Gauntlet adapter. Observes R6's existing transient hit markers
    /// and detached debris; it never writes HP, owns damage, or manipulates physics.
    /// No changes to R6CombatFeedback, P12Damageable, scenes or prefabs are needed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Vfx001R6LiveBridge : MonoBehaviour
    {
        [SerializeField] private bool visualEffectsEnabled = true;
        [SerializeField, Range(0.1f, 2f)] private float debrisRefreshSeconds = 0.25f;

        private R6CombatFeedback legacyFeedback;
        private VfxPoolManager pool;
        private VfxFeedbackOrchestrator orchestrator;
        private GameObject visualRoot;
        private float nextDebrisRefresh;
        private readonly HashSet<int> seenFlashIds = new HashSet<int>();
        private readonly HashSet<int> currentFlashIds = new HashSet<int>();
        private readonly HashSet<int> seenDebrisIds = new HashSet<int>();
        private readonly HashSet<int> currentDebrisIds = new HashSet<int>();

        /// <summary>Run only when the real gameplay scene already has an R1RunController.
        /// PlayMode unit test scenes remain untouched unless they contain that controller.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForGameplayScene()
        {
            var run = Object.FindFirstObjectByType<R1RunController>();
            if (run != null && run.GetComponent<Vfx001R6LiveBridge>() == null)
                run.gameObject.AddComponent<Vfx001R6LiveBridge>();
        }

        private void Awake()
        {
            visualRoot = new GameObject("VFX001 Live Pool (cosmetics only)");
            visualRoot.transform.SetParent(transform, false);
            pool = visualRoot.AddComponent<VfxPoolManager>();
            orchestrator = visualRoot.AddComponent<VfxFeedbackOrchestrator>();
            orchestrator.Bind(pool);
        }

        private void LateUpdate()
        {
            if (!visualEffectsEnabled || orchestrator == null) return;
            if (legacyFeedback == null) legacyFeedback = R6CombatFeedback.Ensure();
            if (legacyFeedback != null) ScanLegacyHitMarkers();

            var run = R1RunController.Instance;
            if (run == null || run.Progress == null ||
                (run.Progress.State != R1RunState.Combat && run.Progress.State != R1RunState.TestDrive))
                return;
            if (Time.unscaledTime >= nextDebrisRefresh)
            {
                nextDebrisRefresh = Time.unscaledTime + debrisRefreshSeconds;
                ScanDetachedDebris();
            }
        }

        private void ScanLegacyHitMarkers()
        {
            currentFlashIds.Clear();
            foreach (Transform child in legacyFeedback.transform)
            {
                if (child == null || child.name != "Błysk trafienia") continue;
                int id = child.GetInstanceID();
                currentFlashIds.Add(id);
                if (!seenFlashIds.Add(id)) continue;

                // R6 currently encodes stronger hits in the visual marker size.
                // This is a compatibility adapter, not an authoritative damage event.
                float size = Mathf.Max(0f, child.localScale.x);
                bool explosion = size >= .34f;
                float intensity = Mathf.Clamp01(Mathf.Max(.1f, size * 2.25f));
                var evt = new VfxEvent(explosion ? VfxEventKind.Explosion : VfxEventKind.Hit,
                    child.position, Vector3.up, ImpactMaterialType.Metal, intensity,
                    child.GetInstanceID());
                orchestrator.Emit(evt);
            }
            seenFlashIds.RemoveWhere(id => !currentFlashIds.Contains(id));
        }

        private void ScanDetachedDebris()
        {
            currentDebrisIds.Clear();
            foreach (var debris in Object.FindObjectsByType<R3Debris>(FindObjectsSortMode.None))
            {
                if (debris == null || !debris.isActiveAndEnabled) continue;
                int id = debris.GetInstanceID();
                currentDebrisIds.Add(id);
                if (!seenDebrisIds.Add(id)) continue;
                orchestrator.Emit(new VfxEvent(VfxEventKind.Detached,
                    debris.transform.position, Vector3.up, ImpactMaterialType.Metal,
                    .5f, id));
            }
            seenDebrisIds.RemoveWhere(id => !currentDebrisIds.Contains(id));
        }

        public void SetVisualEffectsEnabled(bool enabled)
        {
            visualEffectsEnabled = enabled;
            if (!enabled && pool != null) pool.ClearAll();
            // When re-enabled, only effects generated after the change are visualized.
            if (!enabled) { seenFlashIds.Clear(); seenDebrisIds.Clear(); }
        }

        private void OnDestroy()
        {
            if (visualRoot != null) Destroy(visualRoot);
        }
    }
}

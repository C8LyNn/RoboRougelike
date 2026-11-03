using System.Collections.Generic;
using RoboRougelike.R3;
using RoboRougelike.VFX001;
using UnityEngine;

namespace RoboRougelike.B01Performance
{
    /// <summary>
    /// Optional visual-only budget for actual R3Debris.Separated events.
    /// R2: defer creating emitters until selected; cache Rigidbody per actual
    /// detachment; do not rewrite inactive emitters on every refresh.
    /// No changes to Rigidbody, collider, module IDs or gameplay economics.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("B01/Performance/Detached cosmetic spark budget")]
    public sealed class B0150DetachedDebrisSparkBudget : MonoBehaviour
    {
        [SerializeField, Range(0, 70)] private int maximumEmitters = 16;
        [SerializeField, Range(0.1f, 10f)] private float minimumSpeed = 2.2f;
        [SerializeField, Range(.05f, 1f)] private float refreshInterval = .25f;
        [SerializeField] private bool createMissingCosmeticEmitters = true;

        private static B0150DetachedDebrisSparkBudget activeOwner;
        private readonly List<R3Debris> tracked = new List<R3Debris>(70);
        private readonly List<Rigidbody> bodies = new List<Rigidbody>(70);
        private readonly List<DetachedModuleVfxController> fx =
            new List<DetachedModuleVfxController>(70);
        private readonly List<bool> wasEmitting = new List<bool>(70);
        private readonly float[] speeds = new float[70];
        private readonly bool[] eligible = new bool[70];
        private readonly bool[] selected = new bool[70];
        private float nextRefresh;
        private int visibleCount;

        public int TrackedCount { get { return tracked.Count; } }
        public int ActiveEmitters { get { return visibleCount; } }
        public int MaximumEmitters { get { return maximumEmitters; } }

        private void OnEnable()
        {
            if (activeOwner != null && activeOwner != this)
            {
                Debug.LogWarning("B0150: duplicate debris spark budget disabled", this);
                enabled = false;
                return;
            }
            activeOwner = this;
            R3Debris.Separated += Observe;
            nextRefresh = 0f;
        }

        private void OnDisable()
        {
            R3Debris.Separated -= Observe;
            if (activeOwner == this) activeOwner = null;
            // Stop only visuals we actually activated. Never change bodies.
            for (int i = 0; i < fx.Count; ++i)
                if (wasEmitting[i] && fx[i] != null)
                    fx[i].SetDetachedMotion(false, 0f);
            tracked.Clear();
            bodies.Clear();
            fx.Clear();
            wasEmitting.Clear();
            visibleCount = 0;
        }

        private void Observe(R3Debris item)
        {
            if (item == null || !item.gameObject.activeInHierarchy ||
                tracked.Contains(item)) return;
            // The original game bounds R3Debris lifetime at 70. Release dead
            // entries before declining additional active debris observations.
            if (tracked.Count >= 70) PurgeDead();
            if (tracked.Count >= 70) return;
            Rigidbody body = item.GetComponent<Rigidbody>();
            if (body == null || body.isKinematic) return;

            // Existing VFX components may already exist on an original prefab.
            // Do not allocate a NEW component until actually selected.
            DetachedModuleVfxController existing =
                item.GetComponentInChildren<DetachedModuleVfxController>(true);
            if (existing == null && !createMissingCosmeticEmitters) return;
            tracked.Add(item);
            bodies.Add(body);
            fx.Add(existing);
            wasEmitting.Add(false);
            if (existing != null) existing.SetDetachedMotion(false, 0f);
        }

        private void PurgeDead()
        {
            for (int i = tracked.Count - 1; i >= 0; --i)
            {
                if (tracked[i] != null && tracked[i].gameObject.activeInHierarchy)
                    continue;
                if (wasEmitting[i] && fx[i] != null)
                    fx[i].SetDetachedMotion(false, 0f);
                tracked.RemoveAt(i);
                bodies.RemoveAt(i);
                fx.RemoveAt(i);
                wasEmitting.RemoveAt(i);
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + Mathf.Clamp(refreshInterval, .05f, 1f);
            RefreshNow();
        }

        // Same game entrypoint as R1. Does not change Rigidbody or collision state.
        public void RefreshNow()
        {
            PurgeDead();
            int count = tracked.Count;
            if (count > 70) throw new System.InvalidOperationException("R3 debris budget exceeded");
            for (int i = 0; i < count; ++i)
            {
                Rigidbody body = bodies[i];
                DetachedModuleVfxController emitter = fx[i];
                bool valid = body != null && !body.isKinematic &&
                    tracked[i].enabled && tracked[i].gameObject.activeInHierarchy &&
                    (emitter != null ? emitter.isActiveAndEnabled :
                        createMissingCosmeticEmitters);
                eligible[i] = valid;
                speeds[i] = valid ? body.linearVelocity.sqrMagnitude : 0f;
            }

            // A selected item is eligible for a VFX lease even if its emitter
            // has not yet been created. ChooseFastest itself allocates nothing.
            DebrisSparkBudgetCore.ChooseFastest(
                speeds, eligible, selected, count,
                Mathf.Clamp(maximumEmitters, 0, 70), minimumSpeed * minimumSpeed);
            visibleCount = 0;
            for (int i = 0; i < count; ++i)
            {
                DetachedModuleVfxController emitter = fx[i];
                if (selected[i])
                {
                    if (emitter == null && createMissingCosmeticEmitters)
                    {
                        emitter = tracked[i].gameObject.AddComponent<DetachedModuleVfxController>();
                        fx[i] = emitter;
                    }
                    if (emitter != null && emitter.isActiveAndEnabled)
                    {
                        float speed = Mathf.Sqrt(speeds[i]);
                        emitter.SetDetachedMotion(true, speed);
                        // VFX001 may have a stricter per-emitter minimum speed
                        // than this controller's selection threshold. Never count
                        // an invisible lease as an active emitting particle effect.
                        // Clear IsDetached immediately on an ineffective request.
                        if (emitter.EmissionRate > 0f)
                        {
                            wasEmitting[i] = true;
                            ++visibleCount;
                            continue;
                        }
                        emitter.SetDetachedMotion(false, 0f);
                        wasEmitting[i] = false;
                        continue;
                    }
                }

                // No redundant material/emission write for inactive or never
                // allocated visuals. Only revoke a previously granted lease.
                if (wasEmitting[i] && emitter != null)
                    emitter.SetDetachedMotion(false, 0f);
                wasEmitting[i] = false;
            }
        }

        public void Configure(int max, float minSpeed, float cadence)
        {
            if (max < 0 || max > 70 ||
                float.IsNaN(minSpeed) || float.IsInfinity(minSpeed) ||
                minSpeed <= 0f || float.IsNaN(cadence) || float.IsInfinity(cadence) ||
                cadence < .05f || cadence > 1f)
                throw new System.ArgumentOutOfRangeException("Invalid VFX budget");
            maximumEmitters = max;
            minimumSpeed = minSpeed;
            refreshInterval = cadence;
        }
    }
}

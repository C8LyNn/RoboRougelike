using System.Collections.Generic;
using RoboRougelike.R3;
using RoboRougelike.VFX001;
using UnityEngine;

namespace RoboRougelike.B01Performance
{
    /// <summary>
    /// Optional visual-only budget for actual R3Debris.Separated events.
    /// No Rigidbody, collider, gameplay damage, ownership or Save writes.
    /// Place ONE controller in the scene manually after independent acceptance.
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
        private readonly List<DetachedModuleVfxController> fx =
            new List<DetachedModuleVfxController>(70);
        private float[] speeds = new float[70];
        private bool[] eligible = new bool[70];
        private bool[] selected = new bool[70];
        private float nextRefresh;
        private int visibleCount;
        public int TrackedCount { get { return tracked.Count; } }
        public int ActiveEmitters { get { return visibleCount; } }
        public int MaximumEmitters { get { return maximumEmitters; } }

        private void OnEnable()
        {
            // Only one authoritative visual budget per loaded scene/session.
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
            // Restore visual policy to off. Never disable, sleep or destroy debris itself.
            for (int i = 0; i < fx.Count; ++i)
                if (fx[i] != null) fx[i].SetDetachedMotion(false, 0f);
            tracked.Clear();
            fx.Clear();
            visibleCount = 0;
        }

        private void Observe(R3Debris item)
        {
            if (item == null || !item.gameObject.activeInHierarchy ||
                tracked.Contains(item)) return;
            // Bounded by actual R3Debris's 70-live-object design. Do not allocate
            // an emitter for an already doomed or gameplay-owned object.
            if (tracked.Count >= 70) PurgeDead();
            if (tracked.Count >= 70) return;
            var body = item.GetComponent<Rigidbody>();
            if (body == null || body.isKinematic) return;
            var emitter = item.GetComponentInChildren<DetachedModuleVfxController>(true);
            if (emitter == null && createMissingCosmeticEmitters)
                emitter = item.gameObject.AddComponent<DetachedModuleVfxController>();
            if (emitter == null) return;
            tracked.Add(item);
            fx.Add(emitter);
            emitter.SetDetachedMotion(false, 0f); // no unbudgeted impulse flash
        }

        private void PurgeDead()
        {
            for (int i = tracked.Count - 1; i >= 0; --i)
            {
                if (tracked[i] != null && tracked[i].gameObject.activeInHierarchy)
                    continue;
                if (fx[i] != null) fx[i].SetDetachedMotion(false, 0f);
                tracked.RemoveAt(i);
                fx.RemoveAt(i);
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + Mathf.Clamp(refreshInterval, .05f, 1f);
            RefreshNow();
        }

        // Callable for actual Unity PlayMode tests; never changes collider or Rigidbody.
        public void RefreshNow()
        {
            PurgeDead();
            int count = tracked.Count;
            if (count > 70) throw new System.InvalidOperationException("R3 debris budget exceeded");
            for (int i = 0; i < count; ++i)
            {
                var body = tracked[i].GetComponent<Rigidbody>();
                bool valid = body != null && !body.isKinematic &&
                    tracked[i].enabled && tracked[i].gameObject.activeInHierarchy &&
                    fx[i] != null && fx[i].isActiveAndEnabled;
                eligible[i] = valid;
                speeds[i] = valid ? body.linearVelocity.sqrMagnitude : 0f;
            }
            visibleCount = DebrisSparkBudgetCore.ChooseFastest(
                speeds, eligible, selected, count, Mathf.Clamp(maximumEmitters, 0, 70),
                minimumSpeed * minimumSpeed);
            for (int i = 0; i < count; ++i)
            {
                if (fx[i] == null) continue;
                // A selected visual receives speed, unselected gets zero particles.
                // VFX001 uses world-space bounded maxParticles=16 per source emitter.
                float speed = selected[i] ? Mathf.Sqrt(speeds[i]) : 0f;
                fx[i].SetDetachedMotion(selected[i], speed);
            }
        }

        // Operator-only override: fail closed for invalid configuration.
        public void Configure(int max, float minSpeed, float cadence)
        {
            if (max < 0 || max > 70 || float.IsNaN(minSpeed) || float.IsInfinity(minSpeed) ||
                minSpeed <= 0f || float.IsNaN(cadence) || float.IsInfinity(cadence) ||
                cadence < .05f || cadence > 1f)
                throw new System.ArgumentOutOfRangeException("Invalid VFX budget");
            maximumEmitters = max;
            minimumSpeed = minSpeed;
            refreshInterval = cadence;
        }
    }
}

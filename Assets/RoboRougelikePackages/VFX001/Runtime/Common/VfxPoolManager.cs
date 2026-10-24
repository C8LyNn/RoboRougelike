using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Cosmetic-only pooled VFX dispatcher. No dependency on vehicle/weapon assemblies.</summary>
    public sealed class VfxPoolManager : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxActiveEffects = 48;
        [SerializeField, Min(1)] private int maxSpawnsPerFrame = 12;
        [SerializeField, Min(0)] private int prewarm = 8;

        private readonly Stack<VfxBurstActor> _free = new Stack<VfxBurstActor>();
        private readonly List<VfxBurstActor> _active = new List<VfxBurstActor>();
        private readonly HashSet<VfxBurstActor> _all = new HashSet<VfxBurstActor>();
        private VfxBudgetPolicy _budget;
        public int ActiveCount => _active.Count;
        public int PooledCount => _free.Count;
        public int CreatedCount => _all.Count;

        private void Awake()
        {
            _budget = new VfxBudgetPolicy(maxActiveEffects, maxSpawnsPerFrame);
            for (int i = 0; i < Mathf.Min(prewarm, maxActiveEffects); i++) _free.Push(CreateOne());
        }

        private VfxBurstActor CreateOne()
        {
            var go = new GameObject("VFX001 Pooled Burst");
            go.transform.SetParent(transform, false);
            var actor = go.AddComponent<VfxBurstActor>();
            actor.Initialize(this);
            actor.StopAndReset();
            _all.Add(actor);
            return actor;
        }

        public VfxBurstActor Emit(VfxEvent evt)
        {
            if (_budget == null) _budget = new VfxBudgetPolicy(maxActiveEffects, maxSpawnsPerFrame);
            if (!_budget.TryAdmit(Time.frameCount, _active.Count, evt.Kind))
            {
                if (evt.Kind != VfxEventKind.CoreDestroyed) return null;
                // Prefer the core's visual feedback over a lower-priority effect.
                if (_active.Count == 0) return null;
                Return(_active[0]);
                if (!_budget.TryAdmit(Time.frameCount, _active.Count, evt.Kind)) return null;
            }
            var actor = _free.Count > 0 ? _free.Pop() : CreateOne();
            _active.Add(actor);
            actor.Play(evt);
            return actor;
        }

        internal void Return(VfxBurstActor actor)
        {
            if (actor == null || !_active.Remove(actor)) return;
            actor.StopAndReset();
            _free.Push(actor);
        }

        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--) Return(_active[i]);
        }

        private void OnDisable() { ClearAll(); }
    }
}

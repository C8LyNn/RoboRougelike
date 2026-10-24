using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Optional grouping of same-cause detach events before spawning purely visual FX.</summary>
    public sealed class VfxEventBatcher : MonoBehaviour
    {
        [SerializeField] private VfxPoolManager pool;
        [SerializeField, Min(.01f)] private float mergeRadius = 1.5f;
        [SerializeField, Min(1)] private int maxGroups = 64;
        private readonly List<Group> _groups = new List<Group>();
        public int PendingCount => _groups.Count;

        private struct Group
        {
            public VfxEvent Event;
            public int Count;
        }

        public bool Report(VfxEvent e)
        {
            if (e.Kind == VfxEventKind.Detached && e.SequenceId != 0 && e.SourceId != 0)
            {
                for (int i = 0; i < _groups.Count; i++)
                {
                    var group = _groups[i];
                    if (group.Event.Kind != e.Kind || group.Event.SourceId != e.SourceId ||
                        group.Event.SequenceId != e.SequenceId || group.Event.Material != e.Material ||
                        (group.Event.Position - e.Position).sqrMagnitude > mergeRadius * mergeRadius) continue;
                    group.Event.Position = (group.Event.Position * group.Count + e.Position) / (group.Count + 1);
                    group.Event.Normal = (group.Event.Normal * group.Count + e.Normal).normalized;
                    if (group.Event.Normal.sqrMagnitude < .00001f) group.Event.Normal = Vector3.up;
                    group.Event.Intensity = Mathf.Clamp01(Mathf.Max(group.Event.Intensity, e.Intensity) + .04f);
                    group.Count++;
                    _groups[i] = group;
                    return true;
                }
            }
            if (_groups.Count >= maxGroups) return false;
            _groups.Add(new Group { Event = e, Count = 1 });
            return true;
        }

        private void LateUpdate() { Flush(); }
        public void Flush()
        {
            if (pool == null) pool = GetComponentInParent<VfxPoolManager>();
            if (pool != null) foreach (var group in _groups) pool.Emit(group.Event);
            _groups.Clear();
        }
        private void OnDisable() { _groups.Clear(); }
    }
}

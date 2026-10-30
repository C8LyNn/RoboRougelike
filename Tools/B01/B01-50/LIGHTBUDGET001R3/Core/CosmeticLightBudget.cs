using System;
using System.Collections.Generic;

namespace B01.Performance.LightBudget001
{
    /// <summary>Opt-in cosmetic-light ranking input. Ids must be unique within each update.</summary>
    public readonly struct CosmeticLightCandidate
    {
        public readonly int Id;
        public readonly int Priority;
        public readonly float DistanceSquared;
        public readonly bool Eligible;
        public readonly bool WasLit;

        public CosmeticLightCandidate(int id, int priority, float distanceSquared, bool eligible, bool wasLit)
        {
            Id = id;
            Priority = priority;
            DistanceSquared = distanceSquared;
            Eligible = eligible;
            WasLit = wasLit;
        }
    }

    /// <summary>
    /// Allocation-bounded selection for opt-in lights, NOT shadows or all scene lighting.
    /// No Unity types: source testing uses actual production logic.
    /// Failed evaluation leaves the caller's decision array unchanged.
    /// </summary>
    public sealed class CosmeticLightBudget
    {
        public const int MaximumSlots = 256;

        private readonly Rank[] ranks;
        private readonly bool[] scratch;
        private readonly HashSet<int> seen;

        private struct Rank
        {
            public int Index;
            public int Id;
            public int Priority;
            public double Distance;
        }

        private sealed class RankComparer : IComparer<Rank>
        {
            public int Compare(Rank a, Rank b)
            {
                int p = b.Priority.CompareTo(a.Priority);
                if (p != 0) return p;
                int d = a.Distance.CompareTo(b.Distance);
                return d != 0 ? d : a.Id.CompareTo(b.Id);
            }
        }

        private static readonly IComparer<Rank> Comparer = new RankComparer();

        public CosmeticLightBudget(int maximumSlots)
        {
            if (maximumSlots < 1 || maximumSlots > MaximumSlots)
                throw new ArgumentOutOfRangeException(nameof(maximumSlots));
            ranks = new Rank[maximumSlots];
            scratch = new bool[maximumSlots];
            seen = new HashSet<int>();
        }

        public int Capacity => ranks.Length;

        public bool TrySelect(
            IReadOnlyList<CosmeticLightCandidate> candidates,
            int maximumLit,
            float hysteresisMeters,
            bool[] desired,
            out string error)
        {
            error = string.Empty;
            if (candidates == null || desired == null)
            {
                error = "Null candidates or output"; return false;
            }

            int count = candidates.Count;
            if (count > Capacity || desired.Length < count || maximumLit < 0 || maximumLit > count)
            {
                error = "Invalid count, output buffer or maximumLit"; return false;
            }
            if (float.IsNaN(hysteresisMeters) || float.IsInfinity(hysteresisMeters)
                || hysteresisMeters < 0f || hysteresisMeters > 50f)
            {
                error = "Invalid hysteresis distance"; return false;
            }

            seen.Clear();
            int eligibleCount = 0;
            for (int i = 0; i < count; i++)
            {
                CosmeticLightCandidate c = candidates[i];
                if (c.Id < 0 || !seen.Add(c.Id) ||
                    c.Priority < 0 || c.Priority > 100 ||
                    float.IsNaN(c.DistanceSquared) || float.IsInfinity(c.DistanceSquared) ||
                    c.DistanceSquared < 0f || c.DistanceSquared > 100000000f)
                {
                    error = "Invalid candidate id, duplicate, priority or distance"; return false;
                }
                if (!c.Eligible) continue;
                // Previously lit entries receive only a *distance* advantage.
                // Importance priority always dominates distance and hysteresis.
                double d = Math.Sqrt(c.DistanceSquared) - (c.WasLit ? hysteresisMeters : 0.0);
                ranks[eligibleCount++] = new Rank
                {
                    Id = c.Id,
                    Index = i,
                    Priority = c.Priority,
                    Distance = Math.Max(0.0, d)
                };
            }

            Array.Sort(ranks, 0, eligibleCount, Comparer);
            Array.Clear(scratch, 0, count);
            int enabledCount = Math.Min(maximumLit, eligibleCount);
            for (int i = 0; i < enabledCount; i++)
                scratch[ranks[i].Index] = true;
            Array.Copy(scratch, 0, desired, 0, count);
            return true;
        }
    }
}

// B01-50 / SHADOWLOD-001 R1: standalone Unity-compatible C#9 shadow-only culling.
// Call only for OPTIONAL cosmetic shadow casters. Never disable mesh or colliders.
using System;

namespace B01.Performance.ShadowBudget
{
    public sealed class ShadowDistanceBudget<T> where T : class
    {
        private struct Entry
        {
            public T Item;
            public bool Casting;
            public Entry(T item) { Item = item; Casting = true; }
        }

        private readonly Entry[] entries;
        private readonly double enableBelowMeters;
        private readonly double disableAboveMeters;
        private readonly int maxChecksPerFrame;
        private int count;
        private int cursor;

        public int Count => count;
        public int MaxChecksPerFrame => maxChecksPerFrame;
        public long Checks { get; private set; }
        public long Disabled { get; private set; }
        public long Reenabled { get; private set; }
        public long Restored { get; private set; }

        public ShadowDistanceBudget(int maxDecorativeRenderers, int maxChecksPerFrame,
            double enableBelowMeters, double disableAboveMeters)
        {
            if (maxDecorativeRenderers < 1 || maxDecorativeRenderers > 4096)
                throw new ArgumentOutOfRangeException(nameof(maxDecorativeRenderers));
            if (maxChecksPerFrame < 1 || maxChecksPerFrame > 128)
                throw new ArgumentOutOfRangeException(nameof(maxChecksPerFrame));
            if (!Finite(enableBelowMeters) || enableBelowMeters < 0d || enableBelowMeters > 10000d)
                throw new ArgumentOutOfRangeException(nameof(enableBelowMeters));
            if (!Finite(disableAboveMeters) || disableAboveMeters <= enableBelowMeters ||
                disableAboveMeters > 10000d)
                throw new ArgumentOutOfRangeException(nameof(disableAboveMeters),
                    "Disable threshold must be strictly greater than enable threshold.");
            entries = new Entry[maxDecorativeRenderers];
            this.maxChecksPerFrame = maxChecksPerFrame;
            this.enableBelowMeters = enableBelowMeters;
            this.disableAboveMeters = disableAboveMeters;
        }

        private static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);

        // All registered casters must start with shadows enabled. Non-shadow renderers
        // are deliberately excluded by the Unity adapter, not modified by this core.
        public void Register(T decorativeShadowCaster)
        {
            if (decorativeShadowCaster == null) throw new ArgumentNullException(nameof(decorativeShadowCaster));
            if (count >= entries.Length) throw new InvalidOperationException("Shadow caster budget full.");
            for (int i = 0; i < count; i++)
                if (ReferenceEquals(entries[i].Item, decorativeShadowCaster))
                    throw new InvalidOperationException("Duplicate shadow caster.");
            entries[count++] = new Entry(decorativeShadowCaster);
        }

        // One caller invocation = one Unity frame; maxChecksPerFrame guarantees bounded
        // source-side cost. Distances provided at run time (renderer.bounds.center).
        // Apply callback runs only on a change, *before* internal commit, for retry on fault.
        public int EvaluateFrame(Func<T, double> distanceMeters, Action<T, bool> applyCastShadows)
        {
            if (distanceMeters == null) throw new ArgumentNullException(nameof(distanceMeters));
            if (applyCastShadows == null) throw new ArgumentNullException(nameof(applyCastShadows));
            int performed = 0;
            int limit = Math.Min(count, maxChecksPerFrame);
            while (performed < limit)
            {
                int index = cursor;
                Entry entry = entries[index];
                double distance = distanceMeters(entry.Item);
                if (!Finite(distance) || distance < 0d)
                    throw new ArgumentOutOfRangeException(nameof(distanceMeters),
                        "Distance provider must return nonnegative finite metres.");
                // Hysteresis: turn off only past outer threshold, turn on only inside
                // inner threshold. Both exact thresholds retain their previous state.
                bool desired = entry.Casting
                    ? distance <= disableAboveMeters
                    : distance < enableBelowMeters;
                if (desired != entry.Casting)
                {
                    applyCastShadows(entry.Item, desired);
                    entry.Casting = desired;
                    entries[index] = entry;
                    if (desired) Reenabled++; else Disabled++;
                }
                cursor = (cursor + 1) % count;
                Checks++;
                performed++;
            }
            return performed;
        }

        public bool IsCasting(int index)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            return entries[index].Casting;
        }

        // The Unity adapter restores original per-renderer modes, not a hardcoded On.
        // This core restores its logical state; a throwing callback keeps the un-restored
        // item in the ring so caller can retry instead of reporting a false success.
        public void RestoreAll(Action<T, bool> restoreOriginalShadows)
        {
            if (restoreOriginalShadows == null) throw new ArgumentNullException(nameof(restoreOriginalShadows));
            for (int i = 0; i < count; i++)
            {
                Entry entry = entries[i];
                if (!entry.Casting)
                {
                    restoreOriginalShadows(entry.Item, true);
                    entry.Casting = true;
                    entries[i] = entry;
                    Restored++;
                }
            }
        }
    }
}

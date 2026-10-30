// B01-50 VISUALPOOL-001 R1: bounded lease-based reuse for dispensable visual instances.
// Pure C#9 source, no Unity mocks. Never use for projectile, physics or gameplay state.
using System;

namespace B01.Performance.VisualPool
{
    public readonly struct VisualLease
    {
        public readonly int Slot;
        public readonly long Token;
        public VisualLease(int slot, long token) { Slot = slot; Token = token; }
        public bool IsValid => Slot >= 0 && Token > 0;
    }

    public sealed class OptionalVisualLeasePool<T> where T : class
    {
        private struct SlotEntry
        {
            public T Visual;
            public bool InUse;
            public long Token;
            public double DeadlineMs;
        }

        private readonly SlotEntry[] slots;
        private int registered;
        private int active;
        private int cursor;
        private long nextToken = 1;
        private double lastTimeMs = -1;

        public int Capacity => slots.Length;
        public int Registered => registered;
        public int ActiveCount => active;
        public long Granted { get; private set; }
        public long RejectedCapacity { get; private set; }
        public long Expired { get; private set; }
        public long Released { get; private set; }
        public long Cleared { get; private set; }
        public long FailedRetirementCallbacks { get; private set; }

        public OptionalVisualLeasePool(int maxInstances)
        {
            if (maxInstances < 1 || maxInstances > 256)
                throw new ArgumentOutOfRangeException(nameof(maxInstances));
            slots = new SlotEntry[maxInstances];
        }

        private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        private void CheckClock(double nowMs)
        {
            if (!Finite(nowMs) || nowMs < 0d || nowMs < lastTimeMs)
                throw new ArgumentOutOfRangeException(nameof(nowMs), "Time must be finite, nonnegative and monotonic.");
            lastTimeMs = nowMs;
        }

        // Prewarm the visual instances ONCE; the caller owns Unity Instantiate.
        // Once registered, a visual never migrates between slots.
        public int Register(T visual)
        {
            if (visual == null) throw new ArgumentNullException(nameof(visual));
            if (registered >= slots.Length) throw new InvalidOperationException("Visual pool full.");
            for (int i = 0; i < registered; i++)
                if (ReferenceEquals(slots[i].Visual, visual))
                    throw new InvalidOperationException("Visual instance registered twice.");
            int index = registered++;
            slots[index] = new SlotEntry { Visual = visual };
            return index;
        }

        // Refuses exhaustion rather than stealing an active effect. This must not
        // be used for any visual essential to gameplay or accessibility.
        // Expired slots remain occupied until Tick/RetireExpired deactivates them.
        public bool TryAcquire(double nowMs, double lifetimeMs, out VisualLease lease)
        {
            if (!Finite(lifetimeMs) || lifetimeMs <= 0d || lifetimeMs > 60000d)
                throw new ArgumentOutOfRangeException(nameof(lifetimeMs));
            CheckClock(nowMs);
            lease = default;
            if (active >= registered)
            {
                RejectedCapacity++;
                return false;
            }
            if (nextToken == long.MaxValue)
                throw new InvalidOperationException("Lease token space exhausted.");
            for (int scan = 0; scan < registered; scan++)
            {
                int slot = (cursor + scan) % registered;
                if (slots[slot].InUse) continue;
                long token = nextToken++;
                SlotEntry entry = slots[slot];
                entry.InUse = true;
                entry.Token = token;
                entry.DeadlineMs = nowMs + lifetimeMs;
                slots[slot] = entry;
                active++;
                Granted++;
                cursor = (slot + 1) % registered;
                lease = new VisualLease(slot, token);
                return true;
            }
            throw new InvalidOperationException("Pool accounting mismatch.");
        }

        // Caller never sees a prewarmed object without a valid, matching lease.
        public T Get(VisualLease lease)
        {
            if (!Owns(lease)) throw new InvalidOperationException("Stale or invalid visual lease.");
            return slots[lease.Slot].Visual;
        }

        public bool Owns(VisualLease lease)
        {
            return lease.IsValid && lease.Slot < registered &&
                   slots[lease.Slot].InUse && slots[lease.Slot].Token == lease.Token;
        }

        // Release callbacks deactivate GameObject BEFORE freeing slot.
        // A throwing callback leaves the lease active/retryable; never permits
        // a new owner to reuse an object that might still be visible.
        public bool TryRelease(VisualLease lease, Action<T> deactivate)
        {
            if (deactivate == null) throw new ArgumentNullException(nameof(deactivate));
            if (!Owns(lease)) return false;
            SlotEntry entry = slots[lease.Slot];
            try { deactivate(entry.Visual); }
            catch { FailedRetirementCallbacks++; throw; }
            entry.InUse = false;
            entry.DeadlineMs = 0d;
            slots[lease.Slot] = entry;
            active--;
            Released++;
            return true;
        }

        public int RetireExpired(double nowMs, Action<T> deactivate)
        {
            if (deactivate == null) throw new ArgumentNullException(nameof(deactivate));
            CheckClock(nowMs);
            int retired = 0;
            for (int i = 0; i < registered; i++)
            {
                SlotEntry entry = slots[i];
                if (!entry.InUse || nowMs < entry.DeadlineMs) continue;
                try { deactivate(entry.Visual); }
                catch { FailedRetirementCallbacks++; throw; }
                entry.InUse = false;
                entry.DeadlineMs = 0d;
                slots[i] = entry;
                active--;
                retired++;
                Expired++;
            }
            return retired;
        }

        public int ReleaseAll(Action<T> deactivate)
        {
            if (deactivate == null) throw new ArgumentNullException(nameof(deactivate));
            int retired = 0;
            for (int i = 0; i < registered; i++)
            {
                SlotEntry entry = slots[i];
                if (!entry.InUse) continue;
                try { deactivate(entry.Visual); }
                catch { FailedRetirementCallbacks++; throw; }
                entry.InUse = false;
                entry.DeadlineMs = 0d;
                slots[i] = entry;
                active--;
                retired++;
                Cleared++;
            }
            return retired;
        }
    }
}

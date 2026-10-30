// B01-50 FXPACE-001 / Unity-compatible C# pure, deterministic cosmetic activation scheduler.
// IMPORTANT: cosmetic-only; do not delay actual projectile, damage, collider or AI events.
using System;
using System.Collections.Generic;

namespace B01.Performance.FxPacing
{
    public enum CosmeticPriority { Regular = 0, Important = 1 }

    public sealed class CosmeticActivationBudget<T> where T : class
    {
        private struct Item
        {
            public T Value;
            public double CreatedMs;
            public Item(T value, double createdMs) { Value = value; CreatedMs = createdMs; }
        }

        private readonly Queue<Item> regular = new Queue<Item>();
        private readonly Queue<Item> important = new Queue<Item>();
        private readonly int frameLimit;
        private readonly int queueLimit;
        private readonly double ttlMilliseconds;
        private int consecutiveImportant;
        private double lastMs = -1d;

        public int Pending { get { return regular.Count + important.Count; } }
        public int MaxActivationsPerFrame { get { return frameLimit; } }
        public int MaxQueued { get { return queueLimit; } }
        public long Activated { get; private set; }
        public long DroppedExpired { get; private set; }
        public long DroppedOverflow { get; private set; }
        public long DroppedActivationFailure { get; private set; }
        public long Cancelled { get; private set; }

        public CosmeticActivationBudget(int maxActivationsPerFrame, int maxQueued, double maxAgeMilliseconds)
        {
            if (maxActivationsPerFrame < 1 || maxActivationsPerFrame > 128)
                throw new ArgumentOutOfRangeException(nameof(maxActivationsPerFrame));
            if (maxQueued < 1 || maxQueued > 32768)
                throw new ArgumentOutOfRangeException(nameof(maxQueued));
            if (double.IsNaN(maxAgeMilliseconds) || double.IsInfinity(maxAgeMilliseconds)
                || maxAgeMilliseconds < 0d || maxAgeMilliseconds > 30000d)
                throw new ArgumentOutOfRangeException(nameof(maxAgeMilliseconds));
            frameLimit = maxActivationsPerFrame;
            queueLimit = maxQueued;
            ttlMilliseconds = maxAgeMilliseconds;
        }

        private void CheckTime(double nowMs)
        {
            if (double.IsNaN(nowMs) || double.IsInfinity(nowMs) || nowMs < 0d || nowMs < lastMs)
                throw new ArgumentOutOfRangeException(nameof(nowMs), "Time must be finite, nonnegative and monotonic.");
            lastMs = nowMs;
        }

        private void Expire(Queue<Item> queue, double nowMs)
        {
            while (queue.Count > 0 && nowMs - queue.Peek().CreatedMs > ttlMilliseconds)
            {
                queue.Dequeue();
                DroppedExpired++;
            }
        }

        private void ExpireAll(double nowMs)
        {
            Expire(regular, nowMs);
            Expire(important, nowMs);
        }

        // High-priority cosmetics may displace a queued REGULAR visual, never gameplay.
        public bool TrySchedule(T effect, double nowMs, CosmeticPriority priority = CosmeticPriority.Regular)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            if (priority != CosmeticPriority.Regular && priority != CosmeticPriority.Important)
                throw new ArgumentOutOfRangeException(nameof(priority));
            CheckTime(nowMs);
            ExpireAll(nowMs);
            if (Pending >= queueLimit)
            {
                if (priority == CosmeticPriority.Important && regular.Count > 0)
                {
                    regular.Dequeue();
                    DroppedOverflow++;
                }
                else
                {
                    DroppedOverflow++;
                    return false;
                }
            }
            var entry = new Item(effect, nowMs);
            if (priority == CosmeticPriority.Important) important.Enqueue(entry);
            else regular.Enqueue(entry);
            return true;
        }

        // IMPORTANT IMPORTANT REGULAR when both have work, ensuring normal visuals make progress.
        // Only one call per actual frame should be made by the host (Unity adapter does this).
        public int DrainFrame(double nowMs, Action<T> activate)
        {
            if (activate == null) throw new ArgumentNullException(nameof(activate));
            CheckTime(nowMs);
            ExpireAll(nowMs);
            int completed = 0;
            while (completed < frameLimit && Pending > 0)
            {
                bool chooseImportant = important.Count > 0 &&
                    (regular.Count == 0 || consecutiveImportant < 2);
                Item entry;
                if (chooseImportant)
                {
                    entry = important.Dequeue();
                    consecutiveImportant++;
                }
                else
                {
                    entry = regular.Dequeue();
                    consecutiveImportant = 0;
                }

                // Never retry an effect after a throwing callback: it may already
                // have instantiated a GameObject before throwing. Caller gets exception.
                try { activate(entry.Value); }
                catch
                {
                    DroppedActivationFailure++;
                    throw;
                }
                Activated++;
                completed++;
            }
            return completed;
        }

        public int CancelPending()
        {
            int removed = Pending;
            regular.Clear();
            important.Clear();
            consecutiveImportant = 0;
            Cancelled += removed;
            return removed;
        }
    }
}

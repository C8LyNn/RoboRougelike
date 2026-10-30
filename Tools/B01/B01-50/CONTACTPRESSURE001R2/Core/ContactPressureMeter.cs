using System;

namespace B01.Performance.ContactPressure001
{
    /// <summary>Only observed collision callback kinds, never physics commands.</summary>
    public enum ContactKind { Enter = 0, Stay = 1, Exit = 2 }

    /// <summary>Immutable, allocation-free report over observed fixed-tick buckets.</summary>
    public readonly struct ContactPressureSnapshot
    {
        public readonly int ObservedTicks;
        public readonly long EnterCallbacks, StayCallbacks, ExitCallbacks;
        public readonly long ContactPoints;
        public readonly int PeakCallbacksPerTick, P95CallbacksPerTick;
        public readonly long LatestTick;

        public long TotalCallbacks { get { return EnterCallbacks + StayCallbacks + ExitCallbacks; } }

        internal ContactPressureSnapshot(int observedTicks, long enter, long stay, long exit,
            long points, int peak, int p95, long latestTick)
        {
            ObservedTicks = observedTicks;
            EnterCallbacks = enter;
            StayCallbacks = stay;
            ExitCallbacks = exit;
            ContactPoints = points;
            PeakCallbacksPerTick = peak;
            P95CallbacksPerTick = p95;
            LatestTick = latestTick;
        }
    }

    /// <summary>
    /// Bounded fixed-tick rolling collision-event counter. Per-event recording never allocates
    /// and never changes physics, transforms, gameplay state or collider lifetime.
    /// </summary>
    public sealed class ContactPressureMeter
    {
        public const int MinHistoryTicks = 4;
        public const int MaxHistoryTicks = 512;
        public const int MaxPointsPerCallback = 4096;

        private struct Bucket
        {
            internal int Enter, Stay, Exit;
            internal long Points;
            internal long Callbacks
            {
                get { return (long)Enter + Stay + Exit; }
            }
        }

        private readonly Bucket[] _buckets;
        private readonly int[] _sortScratch;
        private int _index = -1;
        private int _observed;
        private long _latestTick = -1;

        public ContactPressureMeter(int historyTicks)
        {
            if (historyTicks < MinHistoryTicks || historyTicks > MaxHistoryTicks)
                throw new ArgumentOutOfRangeException(nameof(historyTicks));
            _buckets = new Bucket[historyTicks];
            _sortScratch = new int[historyTicks];
        }

        public int HistoryTicks { get { return _buckets.Length; } }
        public long LatestTick { get { return _latestTick; } }
        public int ObservedTicks { get { return _observed; } }

        public void Reset()
        {
            Array.Clear(_buckets, 0, _buckets.Length);
            _index = -1;
            _observed = 0;
            _latestTick = -1;
        }

        /// <summary>Failed backwards or duplicated tick is nonmutating. Gaps count as empty ticks.</summary>
        public bool BeginTick(long tick)
        {
            if (tick < 0 || (_latestTick >= 0 && tick <= _latestTick)) return false;
            if (_latestTick < 0)
            {
                _index = 0;
                _buckets[_index] = default(Bucket);
                _observed = 1;
            }
            else
            {
                long gap = tick - _latestTick;
                if (gap >= _buckets.Length)
                {
                    Array.Clear(_buckets, 0, _buckets.Length);
                    _index = (_index + (int)(gap % _buckets.Length)) % _buckets.Length;
                    _observed = _buckets.Length;
                }
                else
                {
                    for (int i = 0; i < (int)gap; ++i)
                    {
                        _index = (_index + 1) % _buckets.Length;
                        _buckets[_index] = default(Bucket);
                    }
                    _observed = Math.Min(_buckets.Length, _observed + (int)gap);
                }
            }
            _latestTick = tick;
            return true;
        }

        /// <summary>Invalid contact input is rejected without modifying the active bucket.</summary>
        public bool TryRecord(ContactKind kind, int contactPoints)
        {
            if (_index < 0 || (int)kind < 0 || (int)kind > 2 ||
                contactPoints < 0 || contactPoints > MaxPointsPerCallback) return false;

            Bucket b = _buckets[_index];
            // Fail closed at bucket counter saturation; no partial points/event writes.
            if (b.Callbacks >= int.MaxValue || b.Points > long.MaxValue - contactPoints) return false;
            if (kind == ContactKind.Enter && b.Enter == int.MaxValue) return false;
            if (kind == ContactKind.Stay && b.Stay == int.MaxValue) return false;
            if (kind == ContactKind.Exit && b.Exit == int.MaxValue) return false;
            switch (kind)
            {
                case ContactKind.Enter: ++b.Enter; break;
                case ContactKind.Stay: ++b.Stay; break;
                case ContactKind.Exit: ++b.Exit; break;
            }
            b.Points += contactPoints;
            _buckets[_index] = b;
            return true;
        }

        /// <summary>Nearest-rank p95 of callbacks per observed tick, including gap-zero ticks.</summary>
        public ContactPressureSnapshot Snapshot()
        {
            if (_observed == 0)
                return new ContactPressureSnapshot(0, 0, 0, 0, 0, 0, 0, -1);

            long enter = 0, stay = 0, exit = 0, points = 0;
            int peak = 0, filled = 0;
            // For a partially full ring, the first _observed slots were filled by BeginTick.
            // For a full ring, all slots represent the most recent history interval.
            for (int i = 0; i < _observed; ++i)
            {
                Bucket b = _buckets[i];
                enter += b.Enter;
                stay += b.Stay;
                exit += b.Exit;
                points += b.Points;
                int count = (int)b.Callbacks;
                if (count > peak) peak = count;
                _sortScratch[filled++] = count;
            }
            Array.Sort(_sortScratch, 0, filled);
            int p95Index = (int)Math.Ceiling(filled * 0.95) - 1;
            return new ContactPressureSnapshot(filled, enter, stay, exit, points, peak,
                _sortScratch[p95Index], _latestTick);
        }
    }
}

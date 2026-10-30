// B01-50 PARTICLELOD-001 R1 — stand-alone C#9 optional emission-level pacing.
// Only for manually opted-in dispensable ParticleSystems. No game-state changes.
using System;

namespace B01.Performance.ParticleEmission
{
    public enum EmissionBand { Near, Medium, Far }

    public sealed class OptionalParticleEmissionBudget<T> where T : class
    {
        private struct Entry
        {
            public T Item;
            public double Original;
            public double Applied;
            public EmissionBand Band;
        }

        private readonly Entry[] entries;
        private readonly int maxChecksPerFrame;
        private readonly double near;
        private readonly double far;
        private readonly double hysteresis;
        private readonly double mediumScale;
        private readonly double farScale;
        private int count;
        private int cursor;
        public int Count => count;
        public long Checks { get; private set; }
        public long Transitions { get; private set; }
        public long Applications { get; private set; }
        public long Restorations { get; private set; }
        public int MaxChecksPerFrame => maxChecksPerFrame;

        private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        public OptionalParticleEmissionBudget(int capacity, int checksPerFrame,
            double nearDistanceMeters, double farDistanceMeters, double hysteresisMeters,
            double mediumEmissionScale = 0.35, double farEmissionScale = 0.0)
        {
            if (capacity < 1 || capacity > 4096) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (checksPerFrame < 1 || checksPerFrame > 128) throw new ArgumentOutOfRangeException(nameof(checksPerFrame));
            if (!Finite(nearDistanceMeters) || nearDistanceMeters <= 0 || nearDistanceMeters > 10000)
                throw new ArgumentOutOfRangeException(nameof(nearDistanceMeters));
            if (!Finite(farDistanceMeters) || farDistanceMeters > 10000 || farDistanceMeters <= nearDistanceMeters)
                throw new ArgumentOutOfRangeException(nameof(farDistanceMeters));
            if (!Finite(hysteresisMeters) || hysteresisMeters < 0 || hysteresisMeters >= nearDistanceMeters ||
                farDistanceMeters - nearDistanceMeters <= 2 * hysteresisMeters)
                throw new ArgumentOutOfRangeException(nameof(hysteresisMeters));
            if (!Finite(mediumEmissionScale) || mediumEmissionScale < 0 || mediumEmissionScale > 1)
                throw new ArgumentOutOfRangeException(nameof(mediumEmissionScale));
            if (!Finite(farEmissionScale) || farEmissionScale < 0 || farEmissionScale > mediumEmissionScale)
                throw new ArgumentOutOfRangeException(nameof(farEmissionScale));
            entries = new Entry[capacity];
            maxChecksPerFrame = checksPerFrame;
            near = nearDistanceMeters;
            far = farDistanceMeters;
            hysteresis = hysteresisMeters;
            mediumScale = mediumEmissionScale;
            farScale = farEmissionScale;
        }

        // The host captures the exact original emission rateOverTimeMultiplier; this
        // core never assumes a linear constant ParticleSystem.MinMaxCurve.
        public void Register(T emitter, double originalRateMultiplier)
        {
            if (emitter == null) throw new ArgumentNullException(nameof(emitter));
            if (!Finite(originalRateMultiplier) || originalRateMultiplier < 0 || originalRateMultiplier > 10000)
                throw new ArgumentOutOfRangeException(nameof(originalRateMultiplier));
            if (count == entries.Length) throw new InvalidOperationException("Emitter budget full.");
            for (int i = 0; i < count; i++)
                if (ReferenceEquals(entries[i].Item, emitter))
                    throw new InvalidOperationException("Duplicate emitter registration.");
            entries[count++] = new Entry {
                Item = emitter, Original = originalRateMultiplier,
                Applied = originalRateMultiplier, Band = EmissionBand.Near
            };
        }

        private EmissionBand SelectBand(EmissionBand current, double distance)
        {
            // Far arrivals can jump directly to Far without intermediate 35% frame.
            if (current == EmissionBand.Near)
                return distance > far + hysteresis ? EmissionBand.Far
                     : distance > near + hysteresis ? EmissionBand.Medium : EmissionBand.Near;
            if (current == EmissionBand.Medium)
                return distance > far + hysteresis ? EmissionBand.Far
                     : distance < near - hysteresis ? EmissionBand.Near : EmissionBand.Medium;
            return distance < near - hysteresis ? EmissionBand.Near
                 : distance < far - hysteresis ? EmissionBand.Medium : EmissionBand.Far;
        }

        // Called precisely once by the Unity adapter's Update. The callback sets
        // emission.rateOverTimeMultiplier; it must never touch ParticleSystem.Play,
        // Stop, Clear, collision or trigger modules.
        public int EvaluateFrame(Func<T, double> distanceMeters, Action<T, double> applyMultiplier)
        {
            if (distanceMeters == null) throw new ArgumentNullException(nameof(distanceMeters));
            if (applyMultiplier == null) throw new ArgumentNullException(nameof(applyMultiplier));
            int work = Math.Min(count, maxChecksPerFrame);
            int done = 0;
            while (done < work)
            {
                int index = cursor;
                Entry entry = entries[index];
                double d = distanceMeters(entry.Item);
                if (!Finite(d) || d < 0d)
                    throw new ArgumentOutOfRangeException(nameof(distanceMeters), "Distance must be finite and nonnegative.");
                EmissionBand desiredBand = SelectBand(entry.Band, d);
                double scale = desiredBand == EmissionBand.Near ? 1d
                             : desiredBand == EmissionBand.Medium ? mediumScale : farScale;
                double desired = entry.Original * scale;

                // Compare by exact scalar value: do not call Unity's emission setter
                // on stable frames. A throwing callback leaves this caster retryable.
                if (desired != entry.Applied)
                {
                    applyMultiplier(entry.Item, desired);
                    entry.Applied = desired;
                    Applications++;
                }
                if (entry.Band != desiredBand)
                {
                    entry.Band = desiredBand;
                    Transitions++;
                }
                entries[index] = entry;
                cursor = (cursor + 1) % count;
                Checks++;
                done++;
            }
            return done;
        }

        public EmissionBand BandAt(int index)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            return entries[index].Band;
        }

        public double AppliedAt(int index)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            return entries[index].Applied;
        }

        // Restores ORIGINAL multiplier, not 1, and retries if Unity callback fails.
        public void RestoreAll(Action<T, double> applyMultiplier)
        {
            if (applyMultiplier == null) throw new ArgumentNullException(nameof(applyMultiplier));
            for (int i = 0; i < count; i++)
            {
                Entry entry = entries[i];
                if (entry.Applied != entry.Original)
                {
                    applyMultiplier(entry.Item, entry.Original);
                    entry.Applied = entry.Original;
                    Restorations++;
                }
                entry.Band = EmissionBand.Near;
                entries[i] = entry;
            }
        }
    }
}

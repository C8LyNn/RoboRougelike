// B01-50 / SFXVOICE-001 R1. C# 9, standalone, deterministic optional-SFX voice budget.
// This does NOT touch gameplay-critical warning/attack/hit/damage audio or any game state.
using System;

namespace B01.Performance.OptionalAudio
{
    public enum OptionalSfxTier { Ambient = 0, Accented = 1 }
    public enum VoiceOutcome { Started, Replaced, RejectedDistance, RejectedCooldown, RejectedCapacity }

    public readonly struct VoiceAllocation
    {
        public readonly VoiceOutcome Outcome;
        public readonly int Slot;
        public readonly long Token;
        public readonly long ReplacedToken;

        public bool Granted => Outcome == VoiceOutcome.Started || Outcome == VoiceOutcome.Replaced;
        public VoiceAllocation(VoiceOutcome outcome, int slot, long token, long replacedToken)
        {
            Outcome = outcome; Slot = slot; Token = token; ReplacedToken = replacedToken;
        }
    }

    // One instance per audio world. Caller plays/halts AudioSource only when Granted.
    // No dynamic allocations during requests; bounded arrays are allocated once.
    public sealed class OptionalSfxVoiceBudget
    {
        private struct Voice
        {
            public bool Active;
            public string Key;
            public double UntilMs;
            public double Distance;
            public OptionalSfxTier Tier;
            public long Token;
        }

        private struct Stamp
        {
            public string Key;
            public double WhenMs;
        }

        private readonly Voice[] voices;
        private readonly Stamp[] recentKeys;
        private readonly double maxDistance;
        private readonly double sameKeyCooldownMs;
        private int nextStamp;
        private double lastTime = -1;
        private long nextToken = 1;

        public int Capacity => voices.Length;
        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var voice in voices) if (voice.Active) count++;
                return count;
            }
        }
        public long Started { get; private set; }
        public long Replaced { get; private set; }
        public long RejectedDistance { get; private set; }
        public long RejectedCooldown { get; private set; }
        public long RejectedCapacity { get; private set; }
        public long Expired { get; private set; }
        public long Stopped { get; private set; }
        public long Cleared { get; private set; }

        public OptionalSfxVoiceBudget(int maxSimultaneousVoices, double maxDistanceMeters, double sameKeyCooldownMilliseconds)
        {
            if (maxSimultaneousVoices < 1 || maxSimultaneousVoices > 32)
                throw new ArgumentOutOfRangeException(nameof(maxSimultaneousVoices));
            if (!IsFinite(maxDistanceMeters) || maxDistanceMeters <= 0 || maxDistanceMeters > 1000)
                throw new ArgumentOutOfRangeException(nameof(maxDistanceMeters));
            if (!IsFinite(sameKeyCooldownMilliseconds) ||
                sameKeyCooldownMilliseconds < 0 || sameKeyCooldownMilliseconds > 10000)
                throw new ArgumentOutOfRangeException(nameof(sameKeyCooldownMilliseconds));
            voices = new Voice[maxSimultaneousVoices];
            recentKeys = new Stamp[Math.Max(4, Math.Min(128, maxSimultaneousVoices * 4))];
            maxDistance = maxDistanceMeters;
            sameKeyCooldownMs = sameKeyCooldownMilliseconds;
        }

        private static bool IsFinite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
        private void CheckClock(double nowMs)
        {
            if (!IsFinite(nowMs) || nowMs < 0 || nowMs < lastTime)
                throw new ArgumentOutOfRangeException(nameof(nowMs), "Require monotonic finite milliseconds.");
            lastTime = nowMs;
        }

        private int Expire(double nowMs)
        {
            int count = 0;
            for (int i = 0; i < voices.Length; ++i)
            {
                if (!voices[i].Active || nowMs < voices[i].UntilMs) continue;
                voices[i] = default;
                Expired++;
                count++;
            }
            return count;
        }

        public int Advance(double nowMs)
        {
            CheckClock(nowMs);
            return Expire(nowMs);
        }

        public VoiceAllocation Request(string key, OptionalSfxTier tier, double distanceMeters,
            double nowMs, double durationMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 80)
                throw new ArgumentException("Require a nonempty short visual/audio category key.", nameof(key));
            if (tier != OptionalSfxTier.Ambient && tier != OptionalSfxTier.Accented)
                throw new ArgumentOutOfRangeException(nameof(tier));
            if (!IsFinite(distanceMeters) || distanceMeters < 0)
                throw new ArgumentOutOfRangeException(nameof(distanceMeters));
            if (!IsFinite(durationMilliseconds) || durationMilliseconds <= 0 || durationMilliseconds > 60000)
                throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
            CheckClock(nowMs);
            Expire(nowMs);

            if (distanceMeters > maxDistance)
            {
                RejectedDistance++;
                return new VoiceAllocation(VoiceOutcome.RejectedDistance, -1, 0, 0);
            }

            if (sameKeyCooldownMs > 0)
            {
                foreach (var stamp in recentKeys)
                {
                    if (stamp.Key != null && string.Equals(stamp.Key, key, StringComparison.Ordinal) &&
                        nowMs - stamp.WhenMs < sameKeyCooldownMs)
                    {
                        RejectedCooldown++;
                        return new VoiceAllocation(VoiceOutcome.RejectedCooldown, -1, 0, 0);
                    }
                }
            }

            int chosen = -1;
            for (int i = 0; i < voices.Length; ++i)
                if (!voices[i].Active) { chosen = i; break; }

            VoiceOutcome outcome = VoiceOutcome.Started;
            long replacedToken = 0;

            // Higher-tier optional sound may replace the farthest active lower-tier one.
            // Same-tier requests NEVER steal, avoiding rapid source restarts/clicks.
            if (chosen < 0 && tier == OptionalSfxTier.Accented)
            {
                for (int i = 0; i < voices.Length; ++i)
                {
                    if (voices[i].Tier != OptionalSfxTier.Ambient) continue;
                    if (chosen < 0 || voices[i].Distance > voices[chosen].Distance)
                        chosen = i; // ties keep the earlier slot
                }
                if (chosen >= 0)
                {
                    replacedToken = voices[chosen].Token;
                    outcome = VoiceOutcome.Replaced;
                }
            }
            if (chosen < 0)
            {
                RejectedCapacity++;
                return new VoiceAllocation(VoiceOutcome.RejectedCapacity, -1, 0, 0);
            }
            if (nextToken == long.MaxValue)
                throw new InvalidOperationException("Voice token space exhausted; allocate a new budget.");
            long token = nextToken++;
            voices[chosen] = new Voice {
                Active = true, Key = key, UntilMs = nowMs + durationMilliseconds,
                Distance = distanceMeters, Tier = tier, Token = token
            };
            recentKeys[nextStamp] = new Stamp { Key = key, WhenMs = nowMs };
            nextStamp = (nextStamp + 1) % recentKeys.Length;
            if (outcome == VoiceOutcome.Started) Started++; else Replaced++;
            return new VoiceAllocation(outcome, chosen, token, replacedToken);
        }

        // Token prevents a stale AudioSource completion callback from stopping a replacement.
        public bool Stop(int slot, long token)
        {
            if (slot < 0 || slot >= voices.Length || token <= 0 ||
                !voices[slot].Active || voices[slot].Token != token) return false;
            voices[slot] = default;
            Stopped++;
            return true;
        }

        public long TokenAt(int slot)
        {
            if (slot < 0 || slot >= voices.Length) throw new ArgumentOutOfRangeException(nameof(slot));
            return voices[slot].Active ? voices[slot].Token : 0;
        }

        public int Clear()
        {
            int count = ActiveCount;
            Array.Clear(voices, 0, voices.Length);
            Array.Clear(recentKeys, 0, recentKeys.Length);
            nextStamp = 0;
            Cleared += count;
            return count;
        }
    }
}

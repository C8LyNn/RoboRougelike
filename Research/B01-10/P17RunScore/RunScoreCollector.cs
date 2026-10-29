using System;

namespace RoboRougelike.B0110.RunScore
{
    /// <summary>
    /// Original, portable R5 post-encounter observation collector.
    /// Exactly ONE live combat stage per instance. Do not use for alternate
    /// non-combat objectives (race / road / reactor) unless Codex authorizes
    /// a separate scoring policy. No UnityEngine or game code dependency.
    ///
    /// R1RunController increments DefeatedEnemies/EarnedScrap at encounter end,
    /// but RecoveredModules increments LATER, at ConfirmRewards. Therefore
    /// capture a battle snapshot, then finalize after a successful rewards
    /// commit, before any garage transactions or the next stage.
    /// </summary>
    public sealed class RunScoreCollector
    {
        private readonly int stageNumber;
        private readonly string encounterId;
        private readonly float threat, initialRatio;
        private readonly int spawnCount, initialKills, initialScrap, initialRecovery;
        private double activeSeconds;
        private bool combatEnded, closed, victory;
        private int combatKills;
        private float finalRatio;

        public bool AwaitingRewards => combatEnded && victory && !closed;
        public bool Finished => closed;
        public double ActiveSeconds => activeSeconds;

        private static void Ensure(bool condition, string message)
        {
            if (!condition) throw new ArgumentException(message);
        }

        private static bool Finite(float v, float min, float max)
        {
            return !float.IsNaN(v) && !float.IsInfinity(v) && v >= min && v <= max;
        }

        public RunScoreCollector(int number, string id, float encounterThreat,
            int enemiesSpawned, float startHullRatio,
            int cumulativeDefeated, int cumulativeScrap, int cumulativeRecovered)
        {
            RunScorer.ParSeconds(number, encounterThreat); // original source range gate
            Ensure(!string.IsNullOrWhiteSpace(id) && id.Length <= 128,
                "EncounterId must identify a real R5 encounter");
            Ensure(enemiesSpawned >= 1 && enemiesSpawned <= 4,
                "Combat collector needs actual 1..4 spawned enemies, including escort");
            Ensure(Finite(startHullRatio, 0, 1), "Initial core HP must be within 0..1");
            Ensure(cumulativeDefeated >= 0 && cumulativeScrap >= 0 && cumulativeRecovered >= 0,
                "Cumulative baseline counters cannot be negative");
            stageNumber = number; encounterId = id; threat = encounterThreat;
            spawnCount = enemiesSpawned; initialRatio = startHullRatio;
            initialKills = cumulativeDefeated;
            initialScrap = cumulativeScrap;
            initialRecovery = cumulativeRecovered;
        }

        /// <summary>
        /// Host calls only while the R1 combat state is live. Elapsed time is
        /// provided by its authoritative unscaled clock; pause is excluded.
        /// There is intentionally NO automatic system clock or Unity reference.
        /// </summary>
        public void Advance(float deltaUnscaledSeconds, bool paused)
        {
            Ensure(!combatEnded && !closed, "Time can change only during combat");
            Ensure(Finite(deltaUnscaledSeconds, 0, 36000f),
                "Delta time must be finite, positive-or-zero and within 10h");
            if (paused) return;
            double next = activeSeconds + deltaUnscaledSeconds;
            Ensure(next <= 36000d, "Combat cannot exceed 10h");
            activeSeconds = next;
        }

        private StageScoreInput Snapshot(bool won, int kills, float hull,
            int totalScrap, int totalRecovered)
        {
            var scrapDelta = (long)totalScrap - initialScrap;
            var recoveryDelta = (long)totalRecovered - initialRecovery;
            Ensure(scrapDelta >= 0 && scrapDelta <= 100000,
                "Scrap delta must be 0..100000 (not total wallet or purchase cost)");
            Ensure(recoveryDelta >= 0 && recoveryDelta <= 3,
                "Only 0..3 actually recovered parts per encounter");
            return new StageScoreInput
            {
                StageNumber = stageNumber,
                EncounterId = encounterId,
                Victory = won,
                Threat = threat,
                ExpectedEnemies = spawnCount,
                DefeatedEnemies = kills,
                HullRatioStart = initialRatio,
                HullRatioEnd = hull,
                DurationSeconds = (float)activeSeconds,
                ScrapEarned = (int)scrapDelta,
                RecoveredModules = (int)recoveryDelta
            };
        }

        /// <summary>
        /// Call AFTER R1 combat win/loss transition has incremented its
        /// cumulative DefeatedEnemies counter, AFTER the real victory wallet
        /// award, and BEFORE a reward-selection/garage state transition.
        /// Returns a final score observation for DEFEAT, null for victory
        /// because its recovery count is not committed yet.
        /// </summary>
        public StageScoreInput FinishCombat(bool won, int cumulativeDefeated,
            float remainingHullRatio, int cumulativeEarnedScrap)
        {
            Ensure(!combatEnded && !closed, "Combat outcome already captured");
            long delta = (long)cumulativeDefeated - initialKills;
            Ensure(delta >= 0 && delta <= spawnCount,
                "Combat kill count must be a real stage delta, including Scout escort");
            Ensure(Finite(remainingHullRatio, 0, 1), "End-of-combat core HP must be 0..1");
            var snapshot = Snapshot(won, (int)delta, remainingHullRatio,
                cumulativeEarnedScrap, initialRecovery);
            // Validate BEFORE mutating collector state: failed capture can retry.
            RunScorer.ScoreStage(snapshot);
            combatEnded = true; victory = won; combatKills = (int)delta;
            finalRatio = remainingHullRatio;
            if (!won) closed = true;
            return won ? null : snapshot;
        }

        /// <summary>
        /// Call ONCE only after successful R1 ConfirmRewards (not on opening
        /// the reward window, not on a failed selection, not in the garage).
        /// This is the point at which RecoveredModules changes in the source.
        /// </summary>
        public StageScoreInput FinishRewards(int cumulativeRecovered, int cumulativeEarnedScrap)
        {
            Ensure(combatEnded && victory && !closed,
                "Rewards can only finalize one validated victory exactly once");
            var snapshot = Snapshot(true, combatKills, finalRatio,
                cumulativeEarnedScrap, cumulativeRecovered);
            // Atomic: an invalid counter never closes the collector.
            RunScorer.ScoreStage(snapshot);
            closed = true;
            return snapshot;
        }
    }
}

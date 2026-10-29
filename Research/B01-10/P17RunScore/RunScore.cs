using System;
using System.Collections.Generic;
using System.Globalization;

namespace RoboRougelike.B0110.RunScore
{
    // B01-10 independent production code. No UnityEngine dependency.
    // Codex is the exclusive owner of any gameplay integration.
    public enum RunMedal { None = 0, Bronze = 1, Silver = 2, Gold = 3 }

    // An authoritative observation from ONE completed encounter, not a save file.
    // Gather BEFORE proceeding to the next arena. Inputs must come from game runtime.
    public sealed class StageScoreInput
    {
        public int StageNumber { get; set; }
        public string EncounterId { get; set; }
        public bool Victory { get; set; }
        public float Threat { get; set; }
        public int ExpectedEnemies { get; set; }
        public int DefeatedEnemies { get; set; }
        public float HullRatioStart { get; set; }
        public float HullRatioEnd { get; set; }
        public float DurationSeconds { get; set; }
        // Display-only, not scored: prevents double incentives for farmed Scrap.
        public int ScrapEarned { get; set; }
        public int RecoveredModules { get; set; }
    }

    public sealed class StageScoreResult
    {
        public int StageNumber { get; set; }
        public string EncounterId { get; set; }
        public bool Victory { get; set; }
        public int BasePoints { get; set; }
        public int TimeBonus { get; set; }
        public int HullBonus { get; set; }
        public int SalvageBonus { get; set; }
        public int Total { get; set; }
        public RunMedal Medal { get; set; }
        public float ParSeconds { get; set; }
        public int ScrapEarned { get; set; }
        public int RecoveredModules { get; set; }
        public string LabelPl { get; set; }
        public string LabelEn { get; set; }
    }

    public sealed class RunScoreResult
    {
        public StageScoreResult[] Stages { get; set; }
        public int Total { get; set; }
        public int CompletedStages { get; set; }
        public bool RunWon { get; set; }
        public bool RunLost { get; set; }
        public RunMedal Medal { get; set; }
        public string[] Achievements { get; set; }
        public string StatusPl { get; set; }
        public string StatusEn { get; set; }
    }

    public static class RunScorer
    {
        private static readonly string[] Pl = { "Brak", "Brąz", "Srebro", "Złoto" };
        private static readonly string[] En = { "None", "Bronze", "Silver", "Gold" };

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new ArgumentException(message);
        }
        private static bool Range(float v, float min, float max)
        {
            return !float.IsNaN(v) && !float.IsInfinity(v) && v >= min && v <= max;
        }

        public static float ParSeconds(int stage, float threat)
        {
            Require(stage >= 1 && stage <= 6, "StageNumber must be 1..6");
            Require(Range(threat, 0.01f, 100f), "Threat must be finite within (0,100]");
            return 50f + 10f * stage + 4f * threat;
        }

        public static StageScoreResult ScoreStage(StageScoreInput s)
        {
            Require(s != null, "Stage result is required");
            Require(s.StageNumber >= 1 && s.StageNumber <= 6, "StageNumber must be 1..6");
            Require(!string.IsNullOrWhiteSpace(s.EncounterId) && s.EncounterId.Length <= 128,
                "EncounterId is required and limited to 128 characters");
            Require(Range(s.Threat, 0.01f, 100f), "Threat is invalid");
            Require(s.ExpectedEnemies >= 1 && s.ExpectedEnemies <= 4, "ExpectedEnemies must be 1..4");
            Require(s.DefeatedEnemies >= 0 && s.DefeatedEnemies <= s.ExpectedEnemies,
                "DefeatedEnemies cannot exceed the spawned roster");
            Require(Range(s.HullRatioStart, 0, 1) && Range(s.HullRatioEnd, 0, 1),
                "Hull ratios must be 0..1");
            Require(Range(s.DurationSeconds, 0.001f, 36000f),
                "Duration must be a real positive number within 10 hours");
            Require(s.ScrapEarned >= 0 && s.ScrapEarned <= 100000,
                "ScrapEarned is display-only but must be a valid reward delta");
            Require(s.RecoveredModules >= 0 && s.RecoveredModules <= 3,
                "RecoveredModules must be 0..3 per encounter");
            Require(!s.Victory || (s.DefeatedEnemies == s.ExpectedEnemies && s.HullRatioEnd > 0),
                "Victory requires all actual enemies destroyed and a live vehicle");

            var result = new StageScoreResult
            {
                StageNumber = s.StageNumber,
                EncounterId = s.EncounterId,
                Victory = s.Victory,
                ParSeconds = ParSeconds(s.StageNumber, s.Threat),
                ScrapEarned = s.ScrapEarned,
                RecoveredModules = s.RecoveredModules
            };
            if (s.Victory)
            {
                result.BasePoints = 200 + s.StageNumber * 20
                    + (int)Math.Round(s.Threat * 20f, MidpointRounding.AwayFromZero);
                // Full speed credit at <=par, linearly fades to zero at 2x par.
                var timeFraction = Math.Max(0d, Math.Min(1d,
                    2d - s.DurationSeconds / (double)result.ParSeconds));
                result.TimeBonus = (int)Math.Floor(60d * timeFraction + 0.000001d);
                // End-state ratio, not delta: repair does not award infinite extra points.
                result.HullBonus = (int)Math.Floor(80d * s.HullRatioEnd + 0.000001d);
                result.SalvageBonus = s.RecoveredModules * 15;
                result.Total = result.BasePoints + result.TimeBonus
                    + result.HullBonus + result.SalvageBonus;
                int variable = result.TimeBonus + result.HullBonus + result.SalvageBonus;
                result.Medal = variable >= 130 ? RunMedal.Gold
                    : variable >= 75 ? RunMedal.Silver : RunMedal.Bronze;
            }
            else
            {
                // Defeat NEVER awards progression score and cannot be followed by later stages.
                result.Medal = RunMedal.None;
            }
            result.LabelPl = Pl[(int)result.Medal];
            result.LabelEn = En[(int)result.Medal];
            return result;
        }

        public static RunScoreResult ScoreRun(IReadOnlyList<StageScoreInput> observations)
        {
            Require(observations != null && observations.Count >= 1 && observations.Count <= 6,
                "Run needs 1..6 observed sequential stages");
            var stages = new StageScoreResult[observations.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            int total = 0, wins = 0, gold = 0, silver = 0, salvaged = 0;
            bool fast = true, unbroken = true, lost = false;
            for (int i = 0; i < observations.Count; i++)
            {
                var s = observations[i];
                Require(s != null && s.StageNumber == i + 1,
                    "Stage results must be strictly sequential from 1");
                Require(!lost, "No stages allowed after defeat");
                Require(ids.Add(s.EncounterId ?? ""), "Duplicate encounter ID in the same run");
                var result = ScoreStage(s);
                stages[i] = result;
                total += result.Total;
                salvaged += s.RecoveredModules;
                if (s.Victory)
                {
                    wins++;
                    if (result.Medal == RunMedal.Gold) gold++;
                    if (result.Medal >= RunMedal.Silver) silver++;
                    if (s.DurationSeconds > result.ParSeconds) fast = false;
                    if (s.HullRatioEnd < .65f) unbroken = false;
                }
                else lost = true;
            }
            var runWon = wins == 6 && !lost;
            var achievements = new List<string>();
            if (runWon)
            {
                if (fast) achievements.Add("PAR_MASTER");
                if (unbroken) achievements.Add("HULL_KEEPER");
                if (salvaged >= 8) achievements.Add("SALVAGE_EXPERT");
            }
            // No medal for an unfinished/failed run, irrespective of prior points.
            var medal = !runWon ? RunMedal.None :
                gold >= 4 && silver >= 6 ? RunMedal.Gold :
                silver >= 4 ? RunMedal.Silver : RunMedal.Bronze;
            return new RunScoreResult
            {
                Stages = stages, Total = total, CompletedStages = wins,
                RunWon = runWon, RunLost = lost, Medal = medal,
                Achievements = achievements.ToArray(),
                StatusPl = lost ? "Przegrana" : runWon ? "Ukończona wyprawa" : "Wyprawa trwa",
                StatusEn = lost ? "Defeat" : runWon ? "Run completed" : "Run in progress"
            };
        }
    }
}

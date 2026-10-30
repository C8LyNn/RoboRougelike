using System;

namespace Swarm.B01.Playtesting
{
    /// <summary>
    /// Standalone, engine-independent protocol for TECHNICAL facilitator trials.
    /// Marks are subjective operator observations, never authoritative gameplay events.
    /// No participant data, file/network I/O or game-state changes.
    /// </summary>
    public sealed class Ld02TechnicalTrial
    {
        public const int StageCount = 4;

        public enum Variant { Neutral, Assisted }
        public enum Outcome { Unmarked, Passed, Failed, Skipped }
        public enum Stage { Fight, Garage, Repair, Checkpoint }

        private readonly Outcome[] _results = new Outcome[StageCount];
        private int _current;

        public int TrialNumber { get; private set; }
        public int CurrentStageIndex { get { return _current; } }
        public bool IsFinished { get { return _current == StageCount; } }
        public int MarkedCount { get { return _current; } }

        public Ld02TechnicalTrial(int trialNumber)
        {
            Reset(trialNumber);
        }

        public void Reset(int trialNumber)
        {
            if (trialNumber < 0) throw new ArgumentOutOfRangeException("trialNumber");
            TrialNumber = trialNumber;
            _current = 0;
            for (int i = 0; i < StageCount; i++)
                _results[i] = Outcome.Unmarked;
        }

        public Variant GetVariant(int stageIndex)
        {
            CheckIndex(stageIndex);
            // Consecutive technical trials counterbalance each individual stage 1:1.
            return ((TrialNumber & 1) == (stageIndex & 1)) ? Variant.Neutral : Variant.Assisted;
        }

        public Outcome GetOutcome(int stageIndex)
        {
            CheckIndex(stageIndex);
            return _results[stageIndex];
        }

        public Stage GetStage(int stageIndex)
        {
            CheckIndex(stageIndex);
            return (Stage)stageIndex;
        }

        public string GetObjective(int stageIndex)
        {
            CheckIndex(stageIndex);
            switch (stageIndex)
            {
                case 0: return "Zlokalizuj przeciwnika i spróbuj zakończyć walkę.";
                case 1: return "Znajdź drogę do garażu lub ekranu konfiguracji.";
                case 2: return "Odszukaj sposób naprawienia uszkodzonej części.";
                default: return "Sprawdź, jak wznowić próbę z punktu kontrolnego.";
            }
        }

        public string GetAssistedHint(int stageIndex)
        {
            CheckIndex(stageIndex);
            switch (stageIndex)
            {
                case 0: return "Podpowiedź testowa: sprawdź celowanie i dostępne grupy broni.";
                case 1: return "Podpowiedź testowa: poszukaj wejścia do konfiguracji po walce.";
                case 2: return "Podpowiedź testowa: sprawdź stan części i dostępne naprawy.";
                default: return "Podpowiedź testowa: znajdź opcję kontynuacji lub ponownej próby.";
            }
        }

        public void MarkCurrent(Outcome outcome)
        {
            if (IsFinished) throw new InvalidOperationException("Trial already ended.");
            if (outcome != Outcome.Passed && outcome != Outcome.Failed && outcome != Outcome.Skipped)
                throw new ArgumentOutOfRangeException("outcome");
            if (_results[_current] != Outcome.Unmarked)
                throw new InvalidOperationException("Stage already marked.");
            _results[_current] = outcome;
            ++_current;
        }

        public int Count(Outcome outcome)
        {
            if (outcome == Outcome.Unmarked) throw new ArgumentOutOfRangeException("outcome");
            int count = 0;
            for (int i = 0; i < StageCount; ++i)
                if (_results[i] == outcome) ++count;
            return count;
        }

        private static void CheckIndex(int index)
        {
            if (index < 0 || index >= StageCount)
                throw new ArgumentOutOfRangeException("index");
        }
    }
}

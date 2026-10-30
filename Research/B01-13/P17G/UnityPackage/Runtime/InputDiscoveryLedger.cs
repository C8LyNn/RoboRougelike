using System;

namespace Swarm.B01.Playtesting
{
    /// <summary>
    /// In-memory, passive first-press observer for isolated technical playtests.
    /// Records raw physical inputs, NOT player intentions, combat or gameplay success.
    /// No disk I/O, network, identities, screenshots, or modifications of game state.
    /// </summary>
    public sealed class InputDiscoveryLedger
    {
        public enum Control
        {
            KeyboardW = 0,
            KeyboardA = 1,
            KeyboardS = 2,
            KeyboardD = 3,
            KeyboardSpace = 4,
            MouseLeft = 5,
            MouseRight = 6
        }

        public const int ControlCount = 7;
        private const double MaximumTrialSeconds = 6 * 60 * 60;
        private readonly long?[] firstPressMs = new long?[ControlCount];
        private readonly int[] repeatCounts = new int[ControlCount];
        private double startedAtSeconds;
        private double lastObservedAtSeconds;

        public int SeenCount { get; private set; }
        public bool IsStarted { get; private set; }

        public void Begin(double nowSeconds)
        {
            CheckTime(nowSeconds);
            startedAtSeconds = nowSeconds;
            lastObservedAtSeconds = nowSeconds;
            SeenCount = 0;
            IsStarted = true;
            for (int i = 0; i < ControlCount; i++)
            {
                firstPressMs[i] = null;
                repeatCounts[i] = 0;
            }
        }

        public bool Observe(Control input, double nowSeconds)
        {
            int index = Index(input);
            if (!IsStarted) throw new InvalidOperationException("The technical trial has not started.");
            CheckTime(nowSeconds);
            if (nowSeconds < lastObservedAtSeconds || nowSeconds < startedAtSeconds)
                throw new ArgumentOutOfRangeException("nowSeconds", "Clock moved backwards.");
            double duration = nowSeconds - startedAtSeconds;
            if (duration > MaximumTrialSeconds)
                throw new ArgumentOutOfRangeException("nowSeconds", "Trial exceeded six hours.");
            lastObservedAtSeconds = nowSeconds;

            if (firstPressMs[index].HasValue)
            {
                if (repeatCounts[index] < 1000000) repeatCounts[index]++;
                return false;
            }
            firstPressMs[index] = (long)Math.Floor(duration * 1000.0);
            SeenCount++;
            return true;
        }

        public long? FirstPressMilliseconds(Control input)
        {
            return firstPressMs[Index(input)];
        }

        public int RepeatCount(Control input)
        {
            return repeatCounts[Index(input)];
        }

        public int UnseenCount
        {
            get { return ControlCount - SeenCount; }
        }

        public static string DisplayName(Control input)
        {
            switch (input)
            {
                case Control.KeyboardW: return "W";
                case Control.KeyboardA: return "A";
                case Control.KeyboardS: return "S";
                case Control.KeyboardD: return "D";
                case Control.KeyboardSpace: return "SPACE";
                case Control.MouseLeft: return "LMB";
                case Control.MouseRight: return "RMB";
                default: throw new ArgumentOutOfRangeException("input");
            }
        }

        private static int Index(Control input)
        {
            int index = (int)input;
            if (index < 0 || index >= ControlCount)
                throw new ArgumentOutOfRangeException("input");
            return index;
        }

        private static void CheckTime(double timeSeconds)
        {
            if (double.IsNaN(timeSeconds) || double.IsInfinity(timeSeconds) || timeSeconds < 0)
                throw new ArgumentOutOfRangeException("timeSeconds");
        }
    }
}

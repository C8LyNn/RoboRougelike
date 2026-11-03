#nullable disable
using UnityEngine;
using UnityEngine.UI;

namespace B01.RecoveryHud
{
    // Read-only view. The scene owner must pass already verified snapshots from
    // ONE canonical session. This component performs NO wallet, combat, Save, or
    // loaner writes and intentionally has no gameplay-command buttons.
    public sealed class RecoveryNoticePanel : MonoBehaviour
    {
        [SerializeField] private Text heading = null;
        [SerializeField] private Text explanation = null;
        [SerializeField] private Text detail = null;
        [SerializeField] private Text severityLabel = null;

        public RecoveryNoticeKind CurrentKind { get; private set; } = RecoveryNoticeKind.Unknown;

        public RecoveryNoticeKind Present(RecoveryRead trustedRead, string expectedRun, string expectedSlot, long expectedEpoch)
        {
            // Presentation validation only. The host MUST independently verify
            // disk+world+economy authorization under its physical session lock.
            RecoveryNotice result = RecoveryNoticeBuilder.Build(trustedRead, expectedRun, expectedSlot, expectedEpoch);
            CurrentKind = result.Kind;
            if (heading != null) heading.text = result.Heading;
            if (explanation != null) explanation.text = result.Explanation;
            if (detail != null) detail.text = result.Detail;
            if (severityLabel != null) severityLabel.text = SeverityText(result.Kind);
            return CurrentKind;
        }

        private static string SeverityText(RecoveryNoticeKind kind)
        {
            switch (kind)
            {
                case RecoveryNoticeKind.Ready: return "GOTOWY";
                case RecoveryNoticeKind.LoanerAvailable: return "AWARYJNY WYBÓR";
                case RecoveryNoticeKind.SaveUncertain: return "NIEPEWNY ZAPIS";
                case RecoveryNoticeKind.Unknown: return "NIEZWERYFIKOWANE";
                case RecoveryNoticeKind.CoreDestroyed: return "CORE ZNISZCZONY";
                case RecoveryNoticeKind.LoanerSpent: return "ROBOT WYKORZYSTANY";
                case RecoveryNoticeKind.ProgressionAtRisk: return "RYZYKO SOFTLOCKA";
                case RecoveryNoticeKind.AwaitingNextVictory: return "OCZEKUJE";
                default: return "BLOKADA KONSTRUKCJI";
            }
        }

        // The canonical host MUST invoke this synchronously BEFORE starting a
        // Save/Load transition, run/slot switch, owner lease handoff, or replay
        // reconstruction. A previously rendered READY/LOANER notice is no
        // longer trustworthy while there is no current authenticated snapshot.
        // This invalidates DISPLAY ONLY: no wallet/Save/gameplay mutations.
        public void Invalidate()
        {
            CurrentKind = RecoveryNoticeKind.Unknown;
            if (heading != null) heading.text = "Oczekiwanie na zweryfikowany stan";
            if (explanation != null) explanation.text = "";
            if (detail != null) detail.text = "";
            if (severityLabel != null) severityLabel.text = "NIEZWERYFIKOWANE";
        }

        private void OnDisable()
        {
            // Scene changes and garage UI reuse must not retain an old GREEN.
            Invalidate();
        }

        private void OnApplicationPause(bool paused)
        {
            // Fail closed on application suspension, without assuming the
            // world/session lease is still current after a resume.
            if (paused) Invalidate();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) Invalidate();
        }
    }
}

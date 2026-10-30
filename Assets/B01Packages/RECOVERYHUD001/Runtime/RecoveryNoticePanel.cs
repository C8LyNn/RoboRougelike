using UnityEngine;
using UnityEngine.UI;

namespace B01.RecoveryHud
{
    // Read-only view. The scene owner must pass already verified snapshots from
    // ONE canonical session. This component performs NO wallet, combat, Save, or
    // loaner writes and intentionally has no gameplay-command buttons.
    public sealed class RecoveryNoticePanel : MonoBehaviour
    {
        [SerializeField] private Text heading;
        [SerializeField] private Text explanation;
        [SerializeField] private Text detail;
        [SerializeField] private Text severityLabel;

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
                case RecoveryNoticeKind.AwaitingNextVictory: return "OCZEKUJE";
                default: return "BLOKADA KONSTRUKCJI";
            }
        }

        private void OnDisable()
        {
            // Prevent retaining a stale GREEN notice when the UI is reused for
            // another run/slot or when a scene changes. This does NOT release
            // any actual world, physical lease, or persistent Save.
            CurrentKind = RecoveryNoticeKind.Unknown;
            if (heading != null) heading.text = "Oczekiwanie na zweryfikowany stan";
            if (explanation != null) explanation.text = "";
            if (detail != null) detail.text = "";
            if (severityLabel != null) severityLabel.text = "NIEZWERYFIKOWANE";
        }
    }
}

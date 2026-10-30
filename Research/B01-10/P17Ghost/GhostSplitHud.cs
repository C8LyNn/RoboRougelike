// B01-10 GHOST-001 W09: optional native Unity 6 training-split overlay.
// A visual UI adapter ONLY: all gate acceptance and physics belong to B01-20.
#if UNITY_5_3_OR_NEWER
using System;
using RoboRougelike.B0110.TrainingGhost;
using UnityEngine;

namespace RoboRougelike.B0110.TrainingGhost
{
    [DisallowMultipleComponent]
    public sealed class GhostSplitHud : MonoBehaviour
    {
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private Vector2 screenAnchor = new Vector2(14f, 14f);

        private GhostRun reference;
        private GhostSplitCoach coach;
        // A verified coach result is snapshotted before it is returned to
        // the host; host DTO edits cannot rewrite the displayed result.
        private readonly GhostPracticePresentation presentation =
            new GhostPracticePresentation();

        public bool IsBound => coach != null;
        public int AcceptedGates => coach == null ? 0 : coach.GatesReached;
        public GhostPracticeLoop RecommendedLoop => coach == null ? null
            : coach.RecommendedLoop;

        public void BindCompletedReference(GhostRun completed)
        {
            // Source is always deep-copied; caller edits cannot forge time splits.
            var validated = GhostMath.Clone(completed);
            GhostMath.Check(validated.Completed,
                "Cannot display a failed run as a training reference");
            var next = new GhostSplitCoach(validated);
            reference = validated;
            coach = next;
            presentation.Clear();
        }

        /// <summary>
        /// HOST MUST FIRST accept the exact CHL-003 actor's physical gate crossing.
        /// This overlay never receives trigger colliders and never calls CHL controllers.
        /// </summary>
        public GhostSplitResult OnHostValidatedGate(int zeroBasedGate,
                                                     float authoritativeElapsed)
        {
            if (coach == null)
                throw new InvalidOperationException("Ghost coaching is not bound");
            var next = coach.AcceptValidatedGate(zeroBasedGate, authoritativeElapsed);
            presentation.CaptureGate(next);
            return next;
        }

        public GhostPracticeSummary OnHostValidatedEnd(bool actuallyCompleted,
                                                        float authoritativeElapsed)
        {
            if (coach == null)
                throw new InvalidOperationException("Ghost coaching is not bound");
            var result = coach.EndTrial(actuallyCompleted, authoritativeElapsed);
            presentation.CaptureEnd(result);
            return result;
        }

        public void Retry()
        {
            if (reference == null)
                throw new InvalidOperationException("No validated practice benchmark");
            coach = new GhostSplitCoach(reference);
            presentation.Clear();
        }

        public void Hide() => showOverlay = false;
        public void Show() => showOverlay = true;

        private void OnGUI()
        {
            if (!showOverlay || coach == null) return;
            var last = presentation.LastGate;
            var summary = presentation.FinalSummary;
            float width = Mathf.Min(390f, Mathf.Max(220f, Screen.width - screenAnchor.x - 14f));
            float height = summary == null ? 92f : 177f;
            var bounds = new Rect(screenAnchor.x, screenAnchor.y, width, height);
            GUI.Box(bounds, "CHL-003 · TRAINING SPLITS (visual only)");
            GUI.Label(new Rect(bounds.x + 12, bounds.y + 27, width - 24, 24),
                "Accepted gates: " + coach.GatesReached + "/" + coach.RequiredGates);
            if (last != null)
            {
                string sign = last.CumulativeDeltaSeconds > 0f ? "+" : "";
                GUI.Label(new Rect(bounds.x + 12, bounds.y + 49, width - 24, 24),
                    "Segment " + last.GateNumber + ": " + last.Pace
                    + " | total " + sign + last.CumulativeDeltaSeconds.ToString("0.00") + "s");
            }
            if (summary != null)
            {
                GUI.Label(new Rect(bounds.x + 12, bounds.y + 73, width - 24, 24),
                    summary.Completed ? "Finish measured (no rewards)" : "Attempt ended / retry");
                GUI.Label(new Rect(bounds.x + 12, bounds.y + 94, width - 24, 24),
                    summary.HasFullCourseComparison
                        ? "Full course: " + (summary.TotalDeltaSeconds > 0f ? "+" : "")
                          + summary.TotalDeltaSeconds.ToString("0.00") + "s vs reference"
                        : "No full-course comparison (attempt incomplete)");
                GUI.Label(new Rect(bounds.x + 12, bounds.y + 116, width - 24, 24),
                    GhostPracticeFocus.Text(summary, coach.RequiredGates));
                if (coach.RecommendedLoop != null)
                {
                    var segment = coach.RecommendedLoop;
                    GUI.Label(new Rect(bounds.x + 12, bounds.y + 138, width - 24, 24),
                        "Visual drill: " + segment.Label + " ["
                        + segment.StartSeconds.ToString("0.00") + "-"
                        + segment.EndSeconds.ToString("0.00") + "s]");
                }
            }
        }
    }
}
#endif

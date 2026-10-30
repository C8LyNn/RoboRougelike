using UnityEngine;

namespace Swarm.B01.Playtesting
{
    /// <summary>
    /// Optional self-contained technical playtest overlay.
    /// Facilitator buttons manually mark observations; NEVER manipulates the game.
    /// Enabled only on an explicitly isolated scene/prefab. No persistent storage.
    /// </summary>
    public sealed class Ld02TechnicalOverlay : MonoBehaviour
    {
        [SerializeField] private int technicalTrialNumber = 0;
        [SerializeField] private bool visibleAtStart = true;
        [SerializeField] private float width = 420f;
        [SerializeField] private float top = 12f;

        private Ld02TechnicalTrial trial;
        private bool visible;
        private bool showHint;

        private void Awake()
        {
            trial = new Ld02TechnicalTrial(technicalTrialNumber < 0 ? 0 : technicalTrialNumber);
            visible = visibleAtStart;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
                visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || trial == null) return;
            float actualWidth = Mathf.Clamp(width, 280f, 500f);
            GUILayout.BeginArea(new Rect(12f, top < 0f ? 0f : top, actualWidth, 330f), GUI.skin.box);
            GUILayout.Label("LD02 — TEST TECHNICZNY (F9: ukryj)");
            GUILayout.Label("Wyłącznie operator — oznaczenia nie dowodzą wyniku gry.");
            GUILayout.Label("Próba nr " + trial.TrialNumber + " | zakończone etapy: " + trial.MarkedCount + "/4");

            if (trial.IsFinished)
            {
                GUILayout.Label("Etapy zakończone. PASS=" + trial.Count(Ld02TechnicalTrial.Outcome.Passed)
                    + " FAIL=" + trial.Count(Ld02TechnicalTrial.Outcome.Failed)
                    + " SKIP=" + trial.Count(Ld02TechnicalTrial.Outcome.Skipped));
                if (GUILayout.Button("Resetuj tę próbę"))
                {
                    trial.Reset(trial.TrialNumber);
                    showHint = false;
                }
            }
            else
            {
                int stage = trial.CurrentStageIndex;
                Ld02TechnicalTrial.Variant variant = trial.GetVariant(stage);
                GUILayout.Label("Zadanie " + (stage + 1) + "/4 — " + trial.GetStage(stage));
                GUILayout.Label("Wariant: " + (variant == Ld02TechnicalTrial.Variant.Neutral ? "neutralny" : "ze wskazówką"));
                GUILayout.Label(trial.GetObjective(stage));
                if (variant == Ld02TechnicalTrial.Variant.Assisted)
                {
                    if (GUILayout.Button(showHint ? "Ukryj wskazówkę" : "Pokaż wskazówkę"))
                        showHint = !showHint;
                    if (showHint) GUILayout.Label(trial.GetAssistedHint(stage));
                }
                if (GUILayout.Button("Operator: wykonano")) Mark(Ld02TechnicalTrial.Outcome.Passed);
                if (GUILayout.Button("Operator: nie wykonano")) Mark(Ld02TechnicalTrial.Outcome.Failed);
                if (GUILayout.Button("Operator: pomiń")) Mark(Ld02TechnicalTrial.Outcome.Skipped);
            }
            GUILayout.EndArea();
        }

        private void Mark(Ld02TechnicalTrial.Outcome outcome)
        {
            trial.MarkCurrent(outcome);
            showHint = false;
        }
    }
}

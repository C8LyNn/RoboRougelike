using UnityEngine;

namespace RoboRougelike.Mechanics.OBJ001
{
    /// <summary>Optional isolated demo UI; does not change production input or scenes.</summary>
    [RequireComponent(typeof(ReactorDefenseObjective))]
    public sealed class ReactorDefenseDemoUI : MonoBehaviour
    {
        private ReactorDefenseObjective objective;
        private void Awake() => objective = GetComponent<ReactorDefenseObjective>();
        private void OnGUI()
        {
            if (objective == null) return;
            GUILayout.BeginArea(new Rect(20f, 20f, 330f, 230f), GUI.skin.box);
            GUILayout.Label("OBJ-001 — Reactor Defense (isolated demo)");
            GUILayout.Label("State: " + objective.State);
            GUILayout.Label("Integrity: " + objective.Integrity.ToString("F1") +
                            " / " + objective.MaximumIntegrity.ToString("F1"));
            GUILayout.Label("Time remaining: " + objective.RemainingSeconds.ToString("F1") + " s");
            bool oldEnabled = GUI.enabled;
            GUI.enabled = objective.State == DefenseState.Idle;
            if (GUILayout.Button("Start defense")) objective.Begin();
            GUI.enabled = objective.State == DefenseState.Defending;
            if (GUILayout.Button("Simulate attack (-20)")) objective.ApplyDamage(20f);
            if (GUILayout.Button("Repair (+10)")) objective.Repair(10f);
            GUI.enabled = oldEnabled;
            if (GUILayout.Button("Reset")) objective.ResetObjective();
            GUILayout.EndArea();
        }
    }
}

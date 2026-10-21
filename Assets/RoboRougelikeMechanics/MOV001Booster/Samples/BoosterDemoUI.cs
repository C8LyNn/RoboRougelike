using UnityEngine;

namespace RoboRougelike.Mechanics.Booster
{
    /// <summary>
    /// OPTIONAL on-screen test button. No dependency on legacy or new Input System.
    /// Remove this script for production integration.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoosterDemoUI : MonoBehaviour
    {
        [SerializeField] private VehicleBooster booster;

        private void Awake()
        {
            if (booster == null) booster = GetComponent<VehicleBooster>();
        }

        private void OnGUI()
        {
            if (booster == null) return;
            GUILayout.BeginArea(new Rect(14, 14, 340, 125), GUI.skin.box);
            GUILayout.Label($"MOV-001 Booster: {(booster.IsBoosting ? "BOOSTING" : booster.IsReady ? "READY" : "COOLDOWN")}");
            GUILayout.Label($"Boost: {booster.RemainingBoostSeconds:0.00}s  Cooldown: {booster.RemainingCooldownSeconds:0.00}s");
            if (GUILayout.Button("Activate booster"))
            {
                if (!booster.TryActivate())
                    Debug.Log($"MOV-001 activation: {booster.LastActivationFailure}", booster);
            }
            GUILayout.EndArea();
        }
    }
}

using UnityEngine;

namespace RoboRougelike.Mechanics.HAZ001
{
    /// <summary>Demo-only repeat trigger and logging. No game damage integration.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TelegraphedHazardZone))]
    public sealed class HazardDemoHarness : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float restartDelay = 1f;
        private TelegraphedHazardZone zone;
        private float nextStart;

        private void OnEnable()
        {
            zone = GetComponent<TelegraphedHazardZone>();
            zone.DamageRequested += LogDamageRequest;
            zone.PhaseChanged += OnPhaseChanged;
            nextStart = Time.time;
        }

        private void OnDisable()
        {
            if (zone == null) return;
            zone.DamageRequested -= LogDamageRequest;
            zone.PhaseChanged -= OnPhaseChanged;
        }

        private void Update()
        {
            if (zone != null && zone.Phase == HazardPhase.Idle && Time.time >= nextStart)
                zone.Begin();
        }

        private void OnPhaseChanged(HazardPhase phase)
        {
            if (phase == HazardPhase.Idle) nextStart = Time.time + restartDelay;
        }

        private static void LogDamageRequest(GameObject target, float amount)
        {
            Debug.Log($"HAZ-001 demo: damage REQUEST for {target.name}: {amount} (no HP modified)");
        }
    }
}

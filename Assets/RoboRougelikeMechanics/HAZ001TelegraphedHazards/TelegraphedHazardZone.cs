using UnityEngine;
using UnityEngine.Events;

namespace RoboRougelike.Mechanics.HAZ001
{
    public enum HazardPhase { Idle, Telegraph, Active, Cooldown }

    /// <summary>Opt-in arena hazard timeline. Does not modify combat or vehicle HP.</summary>
    [DisallowMultipleComponent]
    public sealed class TelegraphedHazardZone : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float warningSeconds = 1.5f;
        [SerializeField, Min(0.05f)] private float activeSeconds = 2f;
        [SerializeField, Min(0.05f)] private float cooldownSeconds = 3f;
        [SerializeField] private UnityEvent onWarning = new UnityEvent();
        [SerializeField] private UnityEvent onActive = new UnityEvent();
        [SerializeField] private UnityEvent onCooldown = new UnityEvent();
        [SerializeField] private UnityEvent onReady = new UnityEvent();

        public HazardPhase Phase { get; private set; } = HazardPhase.Idle;
        public float PhaseElapsed { get; private set; }
        public bool IsDangerous => Phase == HazardPhase.Active;

        public bool Begin()
        {
            if (Phase != HazardPhase.Idle || !isActiveAndEnabled) return false;
            Transition(HazardPhase.Telegraph);
            return true;
        }

        public void ResetCycle()
        {
            Phase = HazardPhase.Idle;
            PhaseElapsed = 0f;
        }

        private void OnDisable() => ResetCycle();

        private void Update()
        {
            if (Phase == HazardPhase.Idle) return;
            PhaseElapsed += Time.deltaTime;
            float duration = Phase == HazardPhase.Telegraph ? warningSeconds :
                Phase == HazardPhase.Active ? activeSeconds : cooldownSeconds;
            if (PhaseElapsed < Mathf.Max(0.05f, duration)) return;
            switch (Phase)
            {
                case HazardPhase.Telegraph: Transition(HazardPhase.Active); break;
                case HazardPhase.Active: Transition(HazardPhase.Cooldown); break;
                default: Transition(HazardPhase.Idle); break;
            }
        }

        private void Transition(HazardPhase next)
        {
            Phase = next;
            PhaseElapsed = 0f;
            switch (next)
            {
                case HazardPhase.Telegraph: onWarning.Invoke(); break;
                case HazardPhase.Active: onActive.Invoke(); break;
                case HazardPhase.Cooldown: onCooldown.Invoke(); break;
                case HazardPhase.Idle: onReady.Invoke(); break;
            }
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.Events;

namespace RoboRougelike.Mechanics.OBJ001
{
    public enum DefenseState { Idle, Defending, Won, Lost }

    /// <summary>Isolated protect-object objective; external systems supply all damage/repairs.</summary>
    [DisallowMultipleComponent]
    public sealed class ReactorDefenseObjective : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxIntegrity = 100f;
        [SerializeField, Min(0.01f)] private float defendSeconds = 60f;
        [SerializeField] private bool automaticClock = true;
        [SerializeField] private UnityEvent onStarted = new UnityEvent();
        [SerializeField] private UnityEvent onDamage = new UnityEvent();
        [SerializeField] private UnityEvent onRepair = new UnityEvent();
        [SerializeField] private UnityEvent onWon = new UnityEvent();
        [SerializeField] private UnityEvent onLost = new UnityEvent();
        [SerializeField] private UnityEvent onReset = new UnityEvent();

        public DefenseState State { get; private set; } = DefenseState.Idle;
        public float Integrity { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public float MaximumIntegrity => maxIntegrity;
        public float DefendSeconds => defendSeconds;
        public float RemainingSeconds => Mathf.Max(0f, defendSeconds - ElapsedSeconds);
        public float Progress01 => Mathf.Clamp01(ElapsedSeconds / defendSeconds);

        public event Action<float> DamageTaken;
        public event Action<float> RepairApplied;
        public event Action<DefenseState> StateChanged;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void Awake() { Sanitize(); ResetObjective(); }
        private void OnValidate() => Sanitize();
        private void Sanitize()
        {
            if (!Finite(maxIntegrity) || maxIntegrity < 1f) maxIntegrity = 100f;
            if (!Finite(defendSeconds) || defendSeconds <= 0f) defendSeconds = 60f;
        }

        /// <summary>Reconfiguration is only allowed outside active defense; resets progress.</summary>
        public bool Configure(float integrity, float seconds)
        {
            if (State == DefenseState.Defending || !Finite(integrity) || integrity < 1f ||
                !Finite(seconds) || seconds <= 0f) return false;
            maxIntegrity = integrity;
            defendSeconds = seconds;
            ResetObjective();
            return true;
        }

        public void SetAutomaticClock(bool enabled) => automaticClock = enabled;

        public void ResetObjective()
        {
            Sanitize();
            Integrity = maxIntegrity;
            ElapsedSeconds = 0f;
            State = DefenseState.Idle;
            onReset?.Invoke();
            StateChanged?.Invoke(State);
        }

        public bool Begin()
        {
            if (!isActiveAndEnabled || State != DefenseState.Idle) return false;
            ChangeState(DefenseState.Defending);
            onStarted?.Invoke();
            return true;
        }

        public bool ApplyDamage(float amount)
        {
            if (!isActiveAndEnabled || State != DefenseState.Defending || !Finite(amount) || amount <= 0f)
                return false;
            float before = Integrity;
            Integrity = Mathf.Max(0f, before - amount);
            float applied = before - Integrity;
            if (applied <= 0f) return false;
            DamageTaken?.Invoke(applied);
            onDamage?.Invoke();
            if (Integrity <= 0f) { ChangeState(DefenseState.Lost); onLost?.Invoke(); }
            return true;
        }

        public bool Repair(float amount)
        {
            if (!isActiveAndEnabled || State != DefenseState.Defending || !Finite(amount) || amount <= 0f)
                return false;
            float before = Integrity;
            Integrity = Mathf.Min(maxIntegrity, before + amount);
            float applied = Integrity - before;
            if (applied <= 0f) return false;
            RepairApplied?.Invoke(applied);
            onRepair?.Invoke();
            return true;
        }

        /// <summary>First terminal action wins: fatal damage before deadline loses; deadline first wins.</summary>
        public bool Tick(float deltaSeconds)
        {
            if (!isActiveAndEnabled || State != DefenseState.Defending ||
                !Finite(deltaSeconds) || deltaSeconds <= 0f) return false;
            ElapsedSeconds = Mathf.Min(defendSeconds, ElapsedSeconds + deltaSeconds);
            if (ElapsedSeconds >= defendSeconds) { ChangeState(DefenseState.Won); onWon?.Invoke(); }
            return true;
        }

        private void Update() { if (automaticClock) Tick(Time.deltaTime); }

        private void ChangeState(DefenseState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}

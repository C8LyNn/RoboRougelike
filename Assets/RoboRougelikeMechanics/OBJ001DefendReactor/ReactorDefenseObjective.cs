using UnityEngine;
using UnityEngine.Events;

namespace RoboRougelike.Mechanics.OBJ001
{
    public enum DefenseState { Idle, Defending, Won, Lost }

    /// <summary>Stand-alone protect-object objective. Damage source is explicitly external.</summary>
    [DisallowMultipleComponent]
    public sealed class ReactorDefenseObjective : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxIntegrity = 100f;
        [SerializeField, Min(1f)] private float defendSeconds = 60f;
        [SerializeField] private UnityEvent onDamage = new UnityEvent();
        [SerializeField] private UnityEvent onRepair = new UnityEvent();
        [SerializeField] private UnityEvent onWon = new UnityEvent();
        [SerializeField] private UnityEvent onLost = new UnityEvent();

        public DefenseState State { get; private set; } = DefenseState.Idle;
        public float Integrity { get; private set; }
        public float ElapsedSeconds { get; private set; }

        private void Awake() => ResetObjective();

        public void ResetObjective()
        {
            State = DefenseState.Idle;
            Integrity = Mathf.Max(1f, maxIntegrity);
            ElapsedSeconds = 0f;
        }

        public bool Begin()
        {
            if (!isActiveAndEnabled || State != DefenseState.Idle) return false;
            State = DefenseState.Defending;
            return true;
        }

        public bool ApplyDamage(float amount)
        {
            if (State != DefenseState.Defending || float.IsNaN(amount) || amount <= 0f) return false;
            Integrity = Mathf.Max(0f, Integrity - amount);
            onDamage.Invoke();
            if (Integrity <= 0f)
            {
                State = DefenseState.Lost;
                onLost.Invoke();
            }
            return true;
        }

        public bool Repair(float amount)
        {
            if (State != DefenseState.Defending || float.IsNaN(amount) || amount <= 0f) return false;
            Integrity = Mathf.Min(Mathf.Max(1f, maxIntegrity), Integrity + amount);
            onRepair.Invoke();
            return true;
        }

        private void Update()
        {
            if (State != DefenseState.Defending) return;
            ElapsedSeconds += Time.deltaTime;
            if (ElapsedSeconds < defendSeconds) return;
            State = DefenseState.Won;
            onWon.Invoke();
        }
    }
}

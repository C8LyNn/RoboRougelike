using System;
using UnityEngine;

namespace RoboRougelike.Defenses.DirectionalShield
{
    /// <summary>Defensywny moduł montowany na kadłubie. Nie tworzy colliderów ani własnego systemu HP.</summary>
    public sealed class DirectionalShieldGenerator : MonoBehaviour
    {
        [Header("Owner and direction")]
        [SerializeField] private Rigidbody ownerBody;
        [SerializeField] private Transform mountForward;
        [SerializeField] private MonoBehaviour energySourceComponent;
        [SerializeField] private bool operational = true;
        [SerializeField] private int priority;

        [Header("Directional coverage")]
        [SerializeField, Range(0f, 180f)] private float halfAngleDegrees = 60f;
        [SerializeField, Min(0.01f)] private float coverageRadius = 9f;
        [SerializeField] private bool horizontalArc = true;
        [SerializeField] private bool requireHitPointInFront = true;

        [Header("Shield capacity")]
        [SerializeField, Min(0f)] private float maximumCharge = 100f;
        [SerializeField, Min(0f)] private float maximumAbsorptionPerHit = 100f;
        [SerializeField, Min(0f)] private float regenerationPerSecond = 12f;
        [SerializeField, Min(0f)] private float regenerationDelay = 3f;
        [SerializeField, Min(0f)] private float energyPerAbsorbed = 0f;
        [SerializeField, Min(0f)] private float energyPerRegenerated = 0f;

        private ShieldState _state;
        private IShieldEnergySource _energy;
        private bool automaticSimulation=true;

        public event Action<float> OnAbsorbed;
        public event Action OnDepleted;
        public Rigidbody OwnerBody => ownerBody;
        public int Priority => priority;
        public float Charge => State.Charge;
        public float Capacity => State.Maximum;
        public float SecondsSinceAbsorption => State.SecondsSinceAbsorption;
        public bool Operational => operational && isActiveAndEnabled && ownerBody != null;
        public Vector3 MountPosition => (mountForward != null ? mountForward : transform).position;
        private ShieldState State => _state ?? (_state = new ShieldState(maximumCharge));

        private void Awake()
        {
            if (ownerBody == null) ownerBody = GetComponentInParent<Rigidbody>();
            if (mountForward == null) mountForward = transform;
            _state = new ShieldState(maximumCharge);
            _energy = energySourceComponent as IShieldEnergySource;
        }

        private void Update() {if(automaticSimulation)Advance(Time.deltaTime);}
        public void SetAutomaticSimulation(bool enabled) => automaticSimulation=enabled;
        public void RestoreState(float charge,float secondsSinceAbsorption) => State.RestoreState(charge,secondsSinceAbsorption);

        public void Advance(float deltaSeconds)
        {
            if (!Operational) return;
            State.Advance(deltaSeconds, regenerationPerSecond, regenerationDelay,
                energyPerRegenerated, Energy());
        }

        private IShieldEnergySource Energy()
        {
            // W Unity zniszczony komponent może nadal być referencją interfejsową C#.
            if (_energy is UnityEngine.Object unityObject && unityObject == null) return null;
            return _energy;
        }

        public void Bind(Rigidbody body, Transform orientation)
        {
            ownerBody = body;
            mountForward = orientation != null ? orientation : transform;
        }

        public void SetEnergySource(IShieldEnergySource source) => _energy = source;
        public void SetOperational(bool value) => operational = value;
        public void RepairAndRecharge() { operational = true; State.ResetFull(); }

        /// <summary>Parametry izolowanego testu lub adaptera garażu. Wywołanie resetuje pojemność.</summary>
        public void Configure(float capacity, float perHit, float regeneration, float delay,
            float angle, float radius, bool planar = true, bool requireFront = true,
            float energyPerPoint = 0f, float energyPerRegenPoint = 0f, int newPriority = 0)
        {
            maximumCharge = SafeNonNegative(capacity);
            maximumAbsorptionPerHit = SafeNonNegative(perHit);
            regenerationPerSecond = SafeNonNegative(regeneration);
            regenerationDelay = SafeNonNegative(delay);
            halfAngleDegrees = Mathf.Clamp(SafeNonNegative(angle), 0f, 180f);
            coverageRadius = SafeNonNegative(radius);
            horizontalArc = planar;
            requireHitPointInFront = requireFront;
            energyPerAbsorbed = SafeNonNegative(energyPerPoint);
            energyPerRegenerated = SafeNonNegative(energyPerRegenPoint);
            priority = newPriority;
            _state = new ShieldState(maximumCharge);
        }

        private static float SafeNonNegative(float f) => ShieldMath.Finite(f) ? Mathf.Max(0f, f) : 0f;

        public float Absorb(float incomingDamage, Vector3 hitPoint, Vector3 projectileTravelDirection)
        {
            if (!Operational || !ShieldMath.Finite(incomingDamage) || incomingDamage <= 0f)
                return 0f;

            Transform basis = mountForward != null ? mountForward : transform;
            if (!ShieldMath.Covers(basis.position, basis.forward, hitPoint,
                    projectileTravelDirection, coverageRadius, halfAngleDegrees,
                    horizontalArc, requireHitPointInFront)) return 0f;

            float previously = State.Charge;
            float amount = State.Absorb(incomingDamage, maximumAbsorptionPerHit, energyPerAbsorbed, Energy());
            if (amount > 0f)
            {
                OnAbsorbed?.Invoke(amount);
                if (previously > 0f && State.Charge <= 0f) OnDepleted?.Invoke();
            }
            return amount;
        }
    }
}

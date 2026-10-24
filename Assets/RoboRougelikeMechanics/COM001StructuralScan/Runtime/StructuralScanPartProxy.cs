using UnityEngine;

namespace RoboRougelike.Mechanics.StructuralScan
{
    // DEMO ONLY. Production: adapt existing R6 Part/Block objects instead of copying HP.
    public sealed class StructuralScanPartProxy : MonoBehaviour, IStructuralScanPart
    {
        [SerializeField] private StructuralScanRegistry registry;
        [SerializeField] private int ownerKey = 2;
        [SerializeField] private int teamKey = 2;
        [SerializeField] private int partKey = 1;
        [SerializeField] private StructuralPartType partType = StructuralPartType.Armor;
        [SerializeField, Range(0f, 1f)] private float healthFraction = 1f;
        [SerializeField, Range(0f, 1f)] private float structuralImportance;
        [SerializeField] private bool functional = true;
        [SerializeField] private bool attached = true;
        [SerializeField] private Collider[] colliders;
        private bool registered;

        public int OwnerKey { get { return ownerKey; } }
        public int TeamKey { get { return teamKey; } }
        public int PartKey { get { return partKey; } }
        public bool IsAttached { get { return attached && isActiveAndEnabled; } }
        public bool IsFunctional { get { return functional; } }
        public float HealthFraction { get { return healthFraction; } }
        public float StructuralImportance { get { return structuralImportance; } }
        public StructuralPartType PartType { get { return partType; } }
        public Transform ScanPoint { get { return transform; } }

        private void OnEnable() { TryRegister(); }
        private void OnDisable() { Unregister(); }
        private void OnDestroy() { Unregister(); }

        public void Configure(StructuralScanRegistry targetRegistry, int owner, int team, int part, StructuralPartType type,
            float health = 1f, float importance = 0f)
        {
            Unregister();
            registry = targetRegistry;
            ownerKey = owner; teamKey = team; partKey = part; partType = type;
            healthFraction = Mathf.Clamp01(health);
            structuralImportance = Mathf.Clamp01(importance);
            colliders = GetComponentsInChildren<Collider>(true);
            if (isActiveAndEnabled) TryRegister();
        }

        public void SetAttached(bool value)
        {
            attached = value;
            if (!value) Unregister();
            else if (isActiveAndEnabled) TryRegister();
        }

        public void SetHealth(float value) { healthFraction = Mathf.Clamp01(value); }
        public void SetFunctional(bool value) { functional = value; }
        public bool OwnsCollider(Collider collider)
        {
            if (collider == null) return false;
            if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) if (colliders[i] == collider) return true;
            return false;
        }
        private void TryRegister() { registered = registry != null && attached && registry.Register(this); }
        private void Unregister() { if (registered && registry != null) registry.Unregister(this); registered = false; }
    }
}

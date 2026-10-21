using UnityEngine;
using RogueMachine.P12PipelineSafe;
using RoboRougelike.Mechanics.StructuralScan;

namespace RoboRougelike.R6
{
    // Strictly read-only. Uses existing P12 hit points and collider identity.
    // Never adds a second Damageable, modifies HP or copies an authoritative health value.
    public sealed class COM001LiveScanPartAdapter : MonoBehaviour, IStructuralScanPart
    {
        private Transform ownerRoot;
        private P12PipelineSafeDamageable damageable;
        private int ownerKey;
        private int teamKey;
        private int partKey;
        private StructuralPartType type;

        public void Bind(Transform root, P12PipelineSafeDamageable part, int owner, int team,
            StructuralPartType partType)
        {
            ownerRoot = root;
            damageable = part;
            ownerKey = owner;
            teamKey = team;
            partKey = part != null ? part.GetInstanceID() : 0;
            type = partType;
        }

        public int OwnerKey => ownerKey;
        public int TeamKey => teamKey;
        public int PartKey => partKey;
        public bool IsAttached => damageable != null && !damageable.IsDead &&
            ownerRoot != null && transform.IsChildOf(ownerRoot) && gameObject.activeInHierarchy;
        public bool IsFunctional => IsAttached;
        public float HealthFraction => damageable == null || damageable.IsDead ? 0f :
            Mathf.Clamp01(damageable.CurrentHp / Mathf.Max(0.001f, damageable.MaxHp));
        public float StructuralImportance => 0f; // Real graph importance is not computed.
        public StructuralPartType PartType => type;
        public Transform ScanPoint => transform;

        public bool OwnsCollider(Collider collider)
        {
            return collider != null &&
                (collider.transform == transform || collider.transform.IsChildOf(transform));
        }
    }
}

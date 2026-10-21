using UnityEngine;

namespace RoboRougelike.Mechanics.StructuralScan
{
    // Immutable-ish snapshot: no references to mutable R6 parts / no ownership of game HP.
    public struct StructuralScanContact
    {
        public readonly int OwnerKey;
        public readonly int PartKey;
        public readonly StructuralPartType Type;
        public readonly Vector3 Position;
        public readonly float HealthFraction;
        public readonly float Priority;
        public readonly bool Functional;

        public StructuralScanContact(IStructuralScanPart part)
        {
            OwnerKey = part.OwnerKey;
            PartKey = part.PartKey;
            Type = part.PartType;
            Position = part.ScanPoint.position;
            float observedHp = part.HealthFraction;
            HealthFraction = (float.IsNaN(observedHp) || float.IsInfinity(observedHp))
                ? 1f : Mathf.Clamp01(observedHp);
            Functional = part.IsFunctional;
            Priority = StructuralScanMath.Priority(part.PartType, part.HealthFraction, part.StructuralImportance, part.IsFunctional);
        }
    }
}

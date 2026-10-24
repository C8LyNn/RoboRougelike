using UnityEngine;

namespace RoboRougelike.Mechanics.StructuralScan
{
    // Adapter R6 should implement this for EXISTING parts, not duplicate HP or ownership.
    public interface IStructuralScanPart
    {
        int OwnerKey { get; }                 // Unique vehicle / detached group's current owner
        int TeamKey { get; }                  // Faction/team, for excluding friendly parts
        int PartKey { get; }                  // Stable part ID unique within OwnerKey
        bool IsAttached { get; }              // Still connected to a live vehicle
        bool IsFunctional { get; }            // Read-only functional status from game
        float HealthFraction { get; }         // HP/maxHP in [0,1]
        float StructuralImportance { get; }   // Optional 0..1, from REAL R6 graph analysis
        StructuralPartType PartType { get; }
        Transform ScanPoint { get; }          // Aimpoint on/inside part
        bool OwnsCollider(Collider collider); // True for ALL colliders belonging to this part
    }

    // Optional filter for own colliders that do not share a Transform hierarchy.
    public interface IStructuralScanSelfFilter
    {
        bool IsOwnCollider(Collider collider);
    }

    // Optional shared game energy. Spend only after at least one contact is confirmed.
    public interface IStructuralScanEnergy
    {
        bool TrySpend(float units);
    }
}

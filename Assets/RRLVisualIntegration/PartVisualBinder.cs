using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;

namespace RoboRougelike.VisualIntegration
{
    /// <summary>Graphics-only replacement. No changes to original chassis physics or HP.</summary>
    public static class PartVisualBinder
    {
        const string CatalogResource = "RRLPartVisualCatalog";

        public static bool TryApply(Transform blockRoot, Vector3Int cell, R3PartKind kind,
                                    R4WeaponKind weapon, Transform logicalPivot,
                                    PartVisualCatalog explicitCatalog = null)
        {
            if (blockRoot == null) return false;
            if (blockRoot.GetComponentInChildren<PartVisualMarker>(true) != null) return true;
            var catalog = explicitCatalog != null
                ? explicitCatalog : Resources.Load<PartVisualCatalog>(CatalogResource);
            if (catalog == null) return false;
            var entry = catalog.Resolve(kind, weapon, cell);
            if (entry == null || entry.prefab == null) return false;

            // Reject any asset capable of introducing a second rigid body, joint or
            // game-side functional behavior. GLB import prefabs should be presentation-only.
            if (entry.prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
                entry.prefab.GetComponentsInChildren<Joint>(true).Length != 0)
            {
                Debug.LogWarning("[RRL Visual] Rigidbody/Joint in visual prefab: " + entry.prefab.name);
                return false;
            }

            // The anchor tracks the logical pose, while the actual GLB stays a CHILD.
            // This is crucial: an animated/spinning wheel would otherwise have its
            // localRotation overwritten every LateUpdate by the pose follower.
            var anchor = new GameObject("VisualOverride_" + kind);
            anchor.transform.SetParent(blockRoot, false);
            anchor.AddComponent<PartVisualMarker>();

            var visual = Object.Instantiate(entry.prefab, anchor.transform, false);
            visual.name = "Model";
            visual.transform.localScale = entry.localScale;

            foreach (var importedCollider in visual.GetComponentsInChildren<Collider>(true))
                importedCollider.enabled = false;

            if (entry.followLogicalPivot && logicalPivot != null && logicalPivot.parent == blockRoot)
                anchor.AddComponent<PartVisualFollowPose>()
                    .Configure(logicalPivot, entry.localOffset, entry.localEuler);
            else
                anchor.transform.SetLocalPositionAndRotation(
                    entry.localOffset, Quaternion.Euler(entry.localEuler));

            // Wheel animation uses CHASSIS forward velocity; it never applies torque.
            if (kind == R3PartKind.Wheel && logicalPivot != null)
            {
                var spin = anchor.AddComponent<PartVisualWheelSpin>();
                spin.Configure(visual.transform, blockRoot.GetComponent<R3MountedPart>(), 0.39f);
            }

            if (kind == R3PartKind.HoverEngine)
                anchor.AddComponent<PartVisualHoverRotor>()
                    .Configure(visual.transform, blockRoot.GetComponent<R3MountedPart>());

            // Preserve root block's visible renderer, HP, colliders, module bindings,
            // muzzle and detached-part behavior. Only hide the old child mesh objects.
            foreach (var renderer in blockRoot.GetComponentsInChildren<Renderer>(true))
                if (renderer.transform != blockRoot &&
                    !renderer.transform.IsChildOf(anchor.transform))
                    renderer.enabled = false;
            return true;
        }
    }
}

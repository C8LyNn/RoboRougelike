using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;

namespace RoboRougelike.VisualIntegration
{
    /// <summary>
    /// Opt-in, graphics-only substitution. NEVER changes physics bodies, colliders of the
    /// logical part, HP, muzzle transforms, module bindings or inventory IDs.
    /// Missing catalog/prefab: original primitive visuals remain intact.
    /// </summary>
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

            // Reject assets with gameplay physics. The existing part owns ALL physics.
            if (entry.prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
                entry.prefab.GetComponentsInChildren<Joint>(true).Length != 0)
            {
                Debug.LogWarning("[RRL Visual] Rejected prefab with Rigidbody/Joint: " + entry.prefab.name);
                return false;
            }

            var instance = Object.Instantiate(entry.prefab, blockRoot, false);
            instance.name = "VisualOverride_" + kind;
            instance.AddComponent<PartVisualMarker>();
            instance.transform.localScale = entry.localScale;

            // Imported mesh colliders should never change the existing chassis physics.
            foreach (var importedCollider in instance.GetComponentsInChildren<Collider>(true))
                importedCollider.enabled = false;

            if (entry.followLogicalPivot && logicalPivot != null && logicalPivot.parent == blockRoot)
                instance.AddComponent<PartVisualFollowPose>()
                    .Configure(logicalPivot, entry.localOffset, entry.localEuler);
            else
                instance.transform.SetLocalPositionAndRotation(
                    entry.localOffset, Quaternion.Euler(entry.localEuler));

            // Hide ONLY legacy child geometry. Keep the root HP-block renderer and all
            // original colliders/logic intact. If the prefab is absent, none of this runs.
            foreach (var renderer in blockRoot.GetComponentsInChildren<Renderer>(true))
                if (renderer.transform != blockRoot &&
                    !renderer.transform.IsChildOf(instance.transform))
                    renderer.enabled = false;
            return true;
        }
    }
}

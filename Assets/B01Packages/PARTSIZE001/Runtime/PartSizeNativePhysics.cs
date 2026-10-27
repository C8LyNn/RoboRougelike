using System;
using System.Collections.Generic;
using UnityEngine;

namespace B01.PARTSIZE001
{
    /// <summary>
    /// Native Unity ONLY: actual registered Collider bounds and PhysX narrow
    /// phase. Caller MUST supply a complete nearby collider inventory;
    /// preview cell occupancy alone never authorizes real installation.
    /// Standalone producer demo, NOT a replacement for Codex R3 mounting.
    /// </summary>
    public static class PartSizeNativePhysics
    {
        public const float MaxPenetrationMeters=.005f;
        private const float WorldBoundsEpsilon=.0005f;

        private static bool Supported(Collider c) =>
            c is BoxCollider || c is SphereCollider || c is CapsuleCollider ||
            (c is MeshCollider mesh && mesh.convex);

        private static bool Inside(Bounds allowed,Bounds actual) =>
            actual.min.x>=allowed.min.x-MaxPenetrationMeters-WorldBoundsEpsilon &&
            actual.min.y>=allowed.min.y-MaxPenetrationMeters-WorldBoundsEpsilon &&
            actual.min.z>=allowed.min.z-MaxPenetrationMeters-WorldBoundsEpsilon &&
            actual.max.x<=allowed.max.x+MaxPenetrationMeters+WorldBoundsEpsilon &&
            actual.max.y<=allowed.max.y+MaxPenetrationMeters+WorldBoundsEpsilon &&
            actual.max.z<=allowed.max.z+MaxPenetrationMeters+WorldBoundsEpsilon;

        public static Bounds GridBounds(PartSizeDefinition definition,Cell3 anchor,Cell3 turns)
        {
            Box3 grid=PartSizeGrid.LogicalBounds(definition,anchor,turns);
            var min=new Vector3((float)grid.Min.X,(float)grid.Min.Y,(float)grid.Min.Z);
            var max=new Vector3((float)grid.Max.X,(float)grid.Max.Y,(float)grid.Max.Z);
            var result=new Bounds();
            result.SetMinMax(min,max);
            return result;
        }

        public static string Validate(PartSizeDefinition definition,
            Cell3 anchor,Cell3 quarterTurns,Transform owner,
            IReadOnlyList<Collider> allOwnedColliders,
            IReadOnlyList<Collider> fullExternalBroadphase,
            bool completeExternalBroadphase)
        {
            var shapeError=PartSizeGrid.EnvelopeError(definition,anchor,quarterTurns);
            if(shapeError!=null)return shapeError;
            if(owner==null||allOwnedColliders==null||allOwnedColliders.Count==0||
               fullExternalBroadphase==null||!completeExternalBroadphase)
                return "PHYSICS_UNVERIFIED";
            // Only global world-aligned 90-degree mounting cells supported.
            // Arbitrary parent rotation/scale is not yet certified.
            if((owner.position-new Vector3((float)anchor.X*.55f,
                (float)anchor.Y*.55f,(float)anchor.Z*.55f)).sqrMagnitude>1e-8f ||
                Quaternion.Angle(owner.rotation,Quaternion.Euler(
                    quarterTurns.X*90f,quarterTurns.Y*90f,quarterTurns.Z*90f))>.1f ||
                (owner.lossyScale-Vector3.one).sqrMagnitude>1e-8f)
                return "NONCANONICAL_OWNER_TRANSFORM";

            var seen=new HashSet<Collider>();
            Bounds envelope=GridBounds(definition,anchor,quarterTurns);
            foreach(Collider c in allOwnedColliders)
            {
                if(c==null||!seen.Add(c)||!c.enabled||c.isTrigger||
                   !c.gameObject.activeInHierarchy||!Supported(c)||
                   (c.transform!=owner&&!c.transform.IsChildOf(owner)))
                    return "UNREGISTERED_OR_UNSUPPORTED_COLLIDER";
                if(!Inside(envelope,c.bounds))return "PHYSICAL_ENVELOPE_OUTSIDE_OWNED_CELLS";
            }

            // Do not trust a caller that enumerates only the convenient
            // colliders. Every enabled, non-trigger physical Collider under
            // this owner must appear EXACTLY once in the authoritative list.
            foreach(var actual in owner.GetComponentsInChildren<Collider>(true))
                if(actual!=null&&actual.enabled&&!actual.isTrigger&&
                   actual.gameObject.activeInHierarchy&&!seen.Contains(actual))
                    return "UNREGISTERED_PHYSICAL_COLLIDER";

            var otherSeen=new HashSet<Collider>();
            Physics.SyncTransforms();
            foreach(var outside in fullExternalBroadphase)
            {
                if(outside==null||!otherSeen.Add(outside)||seen.Contains(outside)||
                   !outside.enabled||outside.isTrigger||!outside.gameObject.activeInHierarchy)
                    return "INVALID_EXTERNAL_BROADPHASE";
                if(!Supported(outside))return "UNSUPPORTED_EXTERNAL_SHAPE";
                foreach(var inside in allOwnedColliders)
                {
                    if(Physics.ComputePenetration(inside,inside.transform.position,
                        inside.transform.rotation,outside,outside.transform.position,
                        outside.transform.rotation,out _,out float depth) &&
                        depth>MaxPenetrationMeters)
                        return "PHYSICAL_PENETRATION";
                }
            }
            return null;
        }
    }

    /// <summary>Standalone in-scene measurement helper. Not a save adapter.</summary>
    public static class PartSizeMeasuredBounds
    {
        public static bool TryCollect(Transform physicalPart,
            out Bounds worldBounds,out int physicalCount)
        {
            worldBounds=new Bounds();physicalCount=0;
            if(physicalPart==null)return false;
            foreach(var c in physicalPart.GetComponentsInChildren<Collider>(true))
            {
                if(c==null||!c.enabled||c.isTrigger||!c.gameObject.activeInHierarchy)
                    continue;
                if(physicalCount==0)worldBounds=c.bounds;
                else worldBounds.Encapsulate(c.bounds);
                physicalCount++;
            }
            return physicalCount>0;
        }
    }
}

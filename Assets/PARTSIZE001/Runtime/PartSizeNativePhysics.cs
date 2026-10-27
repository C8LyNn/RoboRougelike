using System;
using System.Collections.Generic;
using UnityEngine;

namespace B01.PARTSIZE001
{
    /// <summary>
    /// Native Unity ONLY: independently query all nearby world Colliders, then
    /// test actual PhysX narrow phase. The caller's broadphase completeness
    /// flag is NEVER proof of its own inventory: omissions fail closed.
    /// Preview cell occupancy does not authorize installation or typed mounts.
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

            // Update the world broadphase BEFORE reading world-space collider
            // AABBs. Otherwise recent owner movements may be absent from queries.
            Physics.SyncTransforms();
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
            foreach(var outside in fullExternalBroadphase)
            {
                if(outside==null||!otherSeen.Add(outside)||seen.Contains(outside)||
                   !outside.enabled||outside.isTrigger||!outside.gameObject.activeInHierarchy)
                    return "INVALID_EXTERNAL_BROADPHASE";
                if(!Supported(outside))return "UNSUPPORTED_EXTERNAL_SHAPE";
            }

            // P0 W17: discover *independently* through Unity's actual scene
            // physics. A caller may omit a colliding neighbour even when it
            // asserts completeExternalBroadphase=true. Each owned physical
            // collider is queried with a padded world-space AABB; Physics
            // returns the full allocated result array (no truncating fixed
            // NonAlloc capacity). Layer mask ~0 also includes IgnoreRaycast.
            // We do NOT guess that an exposed logical grid face is a socket.
            foreach(var inside in allOwnedColliders)
            {
                Bounds b=inside.bounds;
                if(b.size.sqrMagnitude<=0f)
                    return "PHYSICS_QUERY_UNVERIFIED";
                bool selfObserved=false;
                Collider[] nearby=Physics.OverlapBox(b.center,
                    b.extents+Vector3.one*(MaxPenetrationMeters+WorldBoundsEpsilon),
                    Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
                if(nearby==null||nearby.Length==0)
                    return "PHYSICS_QUERY_UNVERIFIED";
                foreach(var discovered in nearby)
                {
                    if(discovered==null)
                        return "PHYSICS_QUERY_UNVERIFIED";
                    if(discovered==inside)selfObserved=true;
                    if(seen.Contains(discovered))continue;
                    if(!discovered.enabled||discovered.isTrigger||
                       !discovered.gameObject.activeInHierarchy)
                        return "PHYSICS_QUERY_UNVERIFIED";
                    if(!otherSeen.Contains(discovered))
                        return "INCOMPLETE_EXTERNAL_BROADPHASE";
                }
                // If this physical collider wasn't observed by an actual Unity
                // world query, the physics scene may not be the one we queried.
                // Never accept an empty/default-physics-scene false-green.
                if(!selfObserved)return "PHYSICS_QUERY_UNVERIFIED";
            }

            foreach(var outside in fullExternalBroadphase)
            foreach(var inside in allOwnedColliders)
            {
                if(Physics.ComputePenetration(inside,inside.transform.position,
                    inside.transform.rotation,outside,outside.transform.position,
                    outside.transform.rotation,out _,out float depth) &&
                    depth>MaxPenetrationMeters)
                    return "PHYSICAL_PENETRATION";
            }
            return null;
        }

        /// <summary>
        /// PARTSIZE knows only occupied cells, not signed/typed trusted Build
        /// sockets, support graph or actual normal-specific mount area.
        /// A logically exposed face is NEVER placement authorization.
        /// The Codex-owned host must supply its own authoritative socket gate.
        /// </summary>
        public static string ValidateMountSocketAuthority(PartSizeDefinition definition,
            Cell3 localCell,Cell3 face)
        {
            if(definition==null)return "INVALID_LOGICAL_FOOTPRINT";
            if(!PartSizeGrid.IsExposedFace(definition,localCell,face))
                return "NOT_EXPOSED_LOGICAL_FACE";
            return "TYPED_SOCKET_UNVERIFIED";
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

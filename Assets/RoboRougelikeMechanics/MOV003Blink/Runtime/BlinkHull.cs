using UnityEngine;

namespace RoboRougelike.Mechanics.MOV003Blink
{
    // A deliberately conservative, single oriented bounding box around the *entire* rigidbody assembly.
    // Unlike core-only bounds, attached wheels/guns/armor all influence clearance.
    public static class BlinkHull
    {
        public static bool TryCalculate(Rigidbody body, out Vector3 localCenter, out Vector3 halfExtents)
        {
            localCenter = Vector3.zero;
            halfExtents = Vector3.zero;
            if (body == null) return false;

            Vector3 scale = body.transform.lossyScale;
            if (!BlinkMath.IsFinite(scale) || Vector3.Distance(scale, Vector3.one) > 0.01f)
                return false; // A non-unit root scale changes OBB physics query semantics.

            Collider[] colliders = body.GetComponentsInChildren<Collider>(false);
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            bool any = false;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || !col.enabled || col.isTrigger || col.attachedRigidbody != body)
                    continue;
                if (col is BoxCollider box)
                {
                    Vector3 he = box.size * 0.5f;
                    for (int c = 0; c < 8; c++)
                    {
                        Vector3 corner = box.center + new Vector3((c & 1) == 0 ? -he.x : he.x,
                            (c & 2) == 0 ? -he.y : he.y, (c & 4) == 0 ? -he.z : he.z);
                        Vector3 localPoint = body.transform.InverseTransformPoint(box.transform.TransformPoint(corner));
                        min = Vector3.Min(min, localPoint);
                        max = Vector3.Max(max, localPoint);
                    }
                }
                else
                {
                    // Conservative AABB fallback for spheres, capsules and mesh colliders.
                    Bounds bounds = col.bounds;
                    for (int c = 0; c < 8; c++)
                    {
                        Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                            (c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f));
                        Vector3 localPoint = body.transform.InverseTransformPoint(point);
                        min = Vector3.Min(min, localPoint);
                        max = Vector3.Max(max, localPoint);
                    }
                }
                any = true;
            }
            if (!any || !BlinkMath.IsFinite(min) || !BlinkMath.IsFinite(max)) return false;
            localCenter = (min + max) * 0.5f;
            halfExtents = (max - min) * 0.5f;
            return halfExtents.x > 0f && halfExtents.y > 0f && halfExtents.z > 0f;
        }
    }
}

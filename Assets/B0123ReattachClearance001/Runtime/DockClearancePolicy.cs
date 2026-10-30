using System;
using System.Collections.Generic;

namespace B01.ReattachClearance001
{
    // Engine-independent source logic; the Unity adapter supplies actual PhysX observations.
    public enum DockVerdict
    {
        Allowed = 0,
        NotDetached = 1,
        InvalidInput = 2,
        OutOfReach = 3,
        QuerySaturated = 4,
        Blocked = 5
    }

    public readonly struct ContactObservation
    {
        public readonly bool OwnDetachedPart;
        public readonly bool Trigger;
        public readonly bool SupportedShape;
        public readonly double PenetrationMetres;

        public ContactObservation(bool ownDetachedPart, bool trigger, bool supportedShape, double penetrationMetres)
        {
            OwnDetachedPart = ownDetachedPart;
            Trigger = trigger;
            SupportedShape = supportedShape;
            PenetrationMetres = penetrationMetres;
        }
    }

    public static class DockClearancePolicy
    {
        /// <summary>
        /// Fail-closed approval of a physical reattachment. No state mutation here.
        /// The caller must use actual Collider/Physics observations at the target pose.
        /// </summary>
        public static DockVerdict Evaluate(
            bool detached,
            bool partIsDynamic,
            double travelDistanceMetres,
            double maxReachMetres,
            int returnedHitCount,
            int queryCapacity,
            double penetrationToleranceMetres,
            IReadOnlyList<ContactObservation> hits)
        {
            if (!detached)
                return DockVerdict.NotDetached;

            if (!partIsDynamic ||
                !Finite(travelDistanceMetres) || travelDistanceMetres < 0 ||
                !Finite(maxReachMetres) || maxReachMetres <= 0 || maxReachMetres > 50 ||
                !Finite(penetrationToleranceMetres) ||
                penetrationToleranceMetres < 0 || penetrationToleranceMetres > 0.05 ||
                queryCapacity < 4 || queryCapacity > 512 ||
                returnedHitCount < 0 || returnedHitCount > queryCapacity ||
                hits == null || hits.Count != returnedHitCount)
                return DockVerdict.InvalidInput;

            // Saturation cannot distinguish "there are exactly N" from "some were omitted".
            if (returnedHitCount >= queryCapacity)
                return DockVerdict.QuerySaturated;
            if (travelDistanceMetres > maxReachMetres + 1e-7)
                return DockVerdict.OutOfReach;

            for (int i = 0; i < hits.Count; i++)
            {
                ContactObservation hit = hits[i];
                if (hit.OwnDetachedPart || hit.Trigger)
                    continue;
                if (!hit.SupportedShape ||
                    !Finite(hit.PenetrationMetres) ||
                    hit.PenetrationMetres < 0)
                    return DockVerdict.Blocked;
                if (hit.PenetrationMetres > penetrationToleranceMetres)
                    return DockVerdict.Blocked;
            }

            return DockVerdict.Allowed;
        }

        private static bool Finite(double number) =>
            !double.IsNaN(number) && !double.IsInfinity(number);
    }
}

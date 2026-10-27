using System;

namespace B01.CHL003
{
    /// <summary>
    /// Deterministic narrow checkpoint acceptance policy, with dimensions
    /// measured by Unity from the real actor collider Bounds and gate box.
    /// The trigger is merely a contact candidate: a genuine forward crossing
    /// of the actor CENTER and its whole collision envelope must follow.
    /// No Unity API and no game-host reward code.
    /// </summary>
    public static class CHL003GateCrossing
    {
        public static bool IsForwardFullBodyCrossing(
            float previousDepth, float currentDepth, float localMinX,
            float localMaxX, float localMinY, float localMaxY,
            float gateMinX, float gateMaxX, float gateMinY, float gateMaxY,
            float facingDot, float minimumSideClearance = .25f)
        {
            if (!Finite(previousDepth) || !Finite(currentDepth) ||
                !Finite(localMinX) || !Finite(localMaxX) ||
                !Finite(localMinY) || !Finite(localMaxY) ||
                !Finite(gateMinX) || !Finite(gateMaxX) ||
                !Finite(gateMinY) || !Finite(gateMaxY) ||
                !Finite(facingDot) || !Finite(minimumSideClearance))
                return false;
            if (minimumSideClearance < 0f ||
                gateMaxX <= gateMinX || gateMaxY <= gateMinY ||
                localMaxX < localMinX || localMaxY < localMinY)
                return false;
            // Actor center must pass from BEFORE to AFTER the gate plane
            // in the current physics step. Touching by only a front wheel,
            // sliding laterally or driving in reverse must never score.
            if (!(previousDepth <= 0f && currentDepth > 0f &&
                  currentDepth - previousDepth >= .005f && facingDot >= .5f))
                return false;
            const float numericalVerticalTolerance = .06f;
            return localMinX >= gateMinX + minimumSideClearance &&
                   localMaxX <= gateMaxX - minimumSideClearance &&
                   localMinY >= gateMinY - numericalVerticalTolerance &&
                   localMaxY <= gateMaxY + numericalVerticalTolerance;
        }

        private static bool Finite(float n) =>
            !float.IsNaN(n) && !float.IsInfinity(n);
    }
}

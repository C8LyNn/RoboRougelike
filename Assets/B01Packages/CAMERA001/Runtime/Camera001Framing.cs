using System;
using UnityEngine;

namespace B01.CAMERA001
{
    /// <summary>Deterministic, physics-free camera pose proposal. World collision belongs to Camera001Rig.</summary>
    public static class Camera001Framing
    {
        public struct Request
        {
            public Bounds HullBounds;
            public float YawDegrees;
            public float PitchDegrees;
            public float RequestedDistance;
            public float MinimumDistance;
            public float MaximumDistance;
            public float VerticalFovDegrees;
            public float Aspect;
            public float SafetyHeadroom;
            public float AimLead;
        }

        public struct Result
        {
            public Quaternion Rotation;
            public Vector3 AimWorld;
            public Vector3 DesiredCameraWorld;
            public float Distance;
            public float FitDistance;
            public bool FitSaturated;
            public bool OwnHullIntersectsCrosshair;
        }

        public static Result Solve(Request r)
        {
            if (!Finite(r.RequestedDistance) || !Finite(r.MinimumDistance) ||
                !Finite(r.MaximumDistance) || r.MinimumDistance <= 0f ||
                r.MaximumDistance < r.MinimumDistance ||
                !Finite(r.YawDegrees) || !Finite(r.PitchDegrees) ||
                !Finite(r.VerticalFovDegrees) || r.VerticalFovDegrees < 20f ||
                r.VerticalFovDegrees > 110f || !Finite(r.Aspect) || r.Aspect < .25f ||
                !Finite(r.AimLead) || r.AimLead < 0f ||
                !Finite(r.SafetyHeadroom) || r.SafetyHeadroom < 0f ||
                !FiniteBounds(r.HullBounds))
                throw new ArgumentException("CAMERA001_INVALID_REQUEST");

            // Azimuth-independent bounding support along the current camera-facing horizontal vector.
            Quaternion yawOnly = Quaternion.Euler(0f, r.YawDegrees, 0f);
            Vector3 forward = yawOnly * Vector3.forward;
            Vector3 e = r.HullBounds.extents;
            float forwardRadius = Mathf.Abs(forward.x) * e.x + Mathf.Abs(forward.z) * e.z;
            float horizontalRadius = Mathf.Abs(forward.z) * e.x + Mathf.Abs(forward.x) * e.z;

            float fovY = r.VerticalFovDegrees * Mathf.Deg2Rad;
            float fitVertical = (e.y + r.SafetyHeadroom) /
                                Mathf.Tan(fovY * .5f * .78f);
            float fitHorizontal = (horizontalRadius + r.SafetyHeadroom) /
                                  (Mathf.Tan(fovY * .5f * .78f) * r.Aspect);
            float fit = Mathf.Max(r.MinimumDistance, fitVertical, fitHorizontal);
            float distance = Mathf.Clamp(Mathf.Max(r.RequestedDistance, fit),
                                         r.MinimumDistance, r.MaximumDistance);
            Quaternion rotation = Quaternion.Euler(r.PitchDegrees, r.YawDegrees, 0f);
            Vector3 behind = rotation * Vector3.back;

            // Reticle ray points AHEAD of the complete vehicle envelope, not into
            // the hull/core. A positive pitch places camera even farther above it.
            // A negative pitch is compensated to keep the camera outside the roof.
            float negativePitchDrop = Mathf.Max(0f, -behind.y * distance);
            float topY = r.HullBounds.max.y + r.SafetyHeadroom + negativePitchDrop;
            Vector3 aim = new Vector3(r.HullBounds.center.x, topY, r.HullBounds.center.z) +
                          forward * (forwardRadius + r.AimLead + .05f);
            Vector3 cameraPosition = aim + behind * distance;
            // Crosshair ray must not enter the conservative axis-aligned hull.
            bool blocked = r.HullBounds.IntersectRay(
                new Ray(cameraPosition, rotation * Vector3.forward), out float entry) && entry >= 0f;

            return new Result {
                Rotation = rotation,
                AimWorld = aim,
                DesiredCameraWorld = cameraPosition,
                Distance = distance,
                FitDistance = fit,
                FitSaturated = fit > r.MaximumDistance,
                OwnHullIntersectsCrosshair = blocked
            };
        }

        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        private static bool FiniteBounds(Bounds b) =>
            Finite(b.center.x) && Finite(b.center.y) && Finite(b.center.z) &&
            Finite(b.size.x) && Finite(b.size.y) && Finite(b.size.z) &&
            b.size.x > 0f && b.size.y > 0f && b.size.z > 0f;
    }
}

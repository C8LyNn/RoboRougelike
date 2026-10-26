using System;
using UnityEngine;

namespace B01.CAMERA001
{
    /// <summary>
    /// Read-only CAMERA / turret-aim differential telemetry. No weapon ownership,
    /// target selection, mouse input, transform writes or camera pose changes.
    /// Angles share the CURRENT muzzle origin to remove origin differences from
    /// the per-frame target jump. Not a substitute for matched native BASE tests.
    /// </summary>
    public static class Camera001AimTransitionDiagnostics
    {
        public const float CameraPoseToleranceDegrees = .05f;
        public const float AlignmentToleranceDegrees = .05f;
        public const float ReferenceTurretSlewDegreesPerSecond = 170f;

        public struct Sample
        {
            public Vector3 PreviousTargetWorld;
            public Vector3 CurrentTargetWorld;
            public Vector3 CurrentMuzzleWorld;
            public Vector3 PreviousMuzzleForwardWorld;
            public Vector3 CurrentMuzzleForwardWorld;
            public float DeltaTimeSeconds;
            public float TurretSlewDegreesPerSecond;
            public float CameraFinalAimErrorDegrees;
            public bool CameraOwnHullRayHit;
            public bool CameraWorldOverlap;
        }

        public struct Observation
        {
            public float TargetDirectionChangeDegrees;
            public float AvailableTurretSlewDegrees;
            public float ObservedMuzzleDirectionChangeDegrees;
            public float CurrentMuzzleToTargetErrorDegrees;
            public float LowerBoundCatchUpSeconds;
            public bool TargetChangeExceedsOneFrameTurretBudget;
            public bool CameraFinalPoseUnsafe;
            public bool MuzzleWorldStepExceedsTurretBudget;
            public string Classification;
        }

        public static Observation Measure(Sample sample)
        {
            if (!Finite(sample.PreviousTargetWorld) || !Finite(sample.CurrentTargetWorld) ||
                !Finite(sample.CurrentMuzzleWorld) || !Finite(sample.PreviousMuzzleForwardWorld) ||
                !Finite(sample.CurrentMuzzleForwardWorld) ||
                !Finite(sample.DeltaTimeSeconds) || sample.DeltaTimeSeconds <= 0f ||
                !Finite(sample.TurretSlewDegreesPerSecond) || sample.TurretSlewDegreesPerSecond <= 0f ||
                !Finite(sample.CameraFinalAimErrorDegrees) || sample.CameraFinalAimErrorDegrees < 0f)
                throw new ArgumentException("CAMERA001_AIM_DIAG_INVALID_SAMPLE");

            Vector3 previousTargetRay = sample.PreviousTargetWorld - sample.CurrentMuzzleWorld;
            Vector3 currentTargetRay = sample.CurrentTargetWorld - sample.CurrentMuzzleWorld;
            if (previousTargetRay.sqrMagnitude <= 1e-8f ||
                currentTargetRay.sqrMagnitude <= 1e-8f ||
                sample.PreviousMuzzleForwardWorld.sqrMagnitude <= 1e-8f ||
                sample.CurrentMuzzleForwardWorld.sqrMagnitude <= 1e-8f)
                throw new ArgumentException("CAMERA001_AIM_DIAG_ZERO_DIRECTION");

            float targetStep = Vector3.Angle(previousTargetRay, currentTargetRay);
            float muzzleStep = Vector3.Angle(sample.PreviousMuzzleForwardWorld,
                                             sample.CurrentMuzzleForwardWorld);
            float error = Vector3.Angle(sample.CurrentMuzzleForwardWorld, currentTargetRay);
            float budget = sample.TurretSlewDegreesPerSecond * sample.DeltaTimeSeconds;
            if (!Finite(budget))
                throw new ArgumentException("CAMERA001_AIM_DIAG_INVALID_BUDGET");

            bool poseUnsafe = sample.CameraOwnHullRayHit || sample.CameraWorldOverlap ||
                              sample.CameraFinalAimErrorDegrees > CameraPoseToleranceDegrees;
            bool rateLimited = targetStep > budget + AlignmentToleranceDegrees;
            // Lower bound assumes a fixed target and no vehicle/hull rotation.
            // It does NOT prove that a specific gun algorithm used this time.
            float catchUp = Mathf.Max(0f, error - AlignmentToleranceDegrees) /
                            sample.TurretSlewDegreesPerSecond;
            string classification = poseUnsafe ? "CAMERA_FINAL_POSE_UNSAFE" :
                error <= AlignmentToleranceDegrees ? "ALIGNED" :
                rateLimited ? "TARGET_STEP_EXCEEDS_TURRET_BUDGET_PLAUSIBLE" :
                "UNRESOLVED_TRACKING_GAP_REQUIRES_PAIRED_NATIVE_QA";

            return new Observation {
                TargetDirectionChangeDegrees = targetStep,
                AvailableTurretSlewDegrees = budget,
                ObservedMuzzleDirectionChangeDegrees = muzzleStep,
                CurrentMuzzleToTargetErrorDegrees = error,
                LowerBoundCatchUpSeconds = catchUp,
                TargetChangeExceedsOneFrameTurretBudget = rateLimited,
                CameraFinalPoseUnsafe = poseUnsafe,
                // World-space muzzle step includes rigid-body motion. It is
                // diagnostic, NOT proof that a local joint exceeded its limit.
                MuzzleWorldStepExceedsTurretBudget =
                    muzzleStep > budget + AlignmentToleranceDegrees,
                Classification = classification
            };
        }

        private static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        private static bool Finite(Vector3 v) =>
            Finite(v.x) && Finite(v.y) && Finite(v.z);
    }
}

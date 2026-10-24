using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.IndependentSteering
{
    /// <summary>All directions are expressed in the vehicle's LOCAL X/Z plane. +Z forward, +X right.</summary>
    public enum SteeringMode { FrontOnly = 0, RearOnly = 1, CounterPhase = 2, Crab = 3 }
    public enum SteeringWheelPosition { FrontLeft = 0, FrontRight = 1, RearLeft = 2, RearRight = 3 }

    [Serializable]
    public struct SteeringGeometry
    {
        [Min(0.1f)] public float wheelbaseMeters;
        [Min(0.1f)] public float trackMeters;

        public static SteeringGeometry Default => new SteeringGeometry { wheelbaseMeters = 2.8f, trackMeters = 2.0f };
        public SteeringGeometry Sanitized() => new SteeringGeometry
        {
            wheelbaseMeters = SteeringMath.SafeClamp(wheelbaseMeters, 0.1f, 100f, 2.8f),
            trackMeters = SteeringMath.SafeClamp(trackMeters, 0.1f, 100f, 2.0f)
        };
    }

    [Serializable]
    public struct SteeringProfile
    {
        [Range(1, 50)] public float maxCenterAngleDegrees;
        [Range(0, 1)] public float rearSteerRatio;
        [Min(0.1f)] public float steeringRateDegreesPerSecond;
        [Min(0.1f)] public float halfAngleAtSpeedMetersPerSecond;
        [Range(0.1f, 1f)] public float minimumSpeedFactor;

        public static SteeringProfile Default => new SteeringProfile
        {
            maxCenterAngleDegrees = 30f,
            rearSteerRatio = 0.70f,
            steeringRateDegreesPerSecond = 135f,
            halfAngleAtSpeedMetersPerSecond = 12f,
            minimumSpeedFactor = 0.35f
        };

        public SteeringProfile Sanitized() => new SteeringProfile
        {
            maxCenterAngleDegrees = SteeringMath.SafeClamp(maxCenterAngleDegrees, 1f, 50f, 30f),
            rearSteerRatio = SteeringMath.SafeClamp(rearSteerRatio, 0f, 1f, 0.7f),
            steeringRateDegreesPerSecond = SteeringMath.SafeClamp(steeringRateDegreesPerSecond, 0.1f, 2000f, 135f),
            halfAngleAtSpeedMetersPerSecond = SteeringMath.SafeClamp(halfAngleAtSpeedMetersPerSecond, 0.1f, 10000f, 12f),
            minimumSpeedFactor = SteeringMath.SafeClamp(minimumSpeedFactor, 0.1f, 1f, 0.35f)
        };
    }

    public struct SteeringSolution
    {
        public readonly float frontLeftDegrees, frontRightDegrees, rearLeftDegrees, rearRightDegrees;
        public readonly float frontCenterDegrees, rearCenterDegrees;
        /// <summary>Yaw in radians per meter of LOCAL forward movement. Negative when turning left.</summary>
        public readonly float curvatureInverseMeters;
        /// <summary>Local mid-point travel direction relative to vehicle +Z.</summary>
        public readonly float midpointSlipDegrees;

        public SteeringSolution(float fl, float fr, float rl, float rr, float f, float r, float curvature, float slip)
        {
            frontLeftDegrees=fl; frontRightDegrees=fr; rearLeftDegrees=rl; rearRightDegrees=rr;
            frontCenterDegrees=f; rearCenterDegrees=r; curvatureInverseMeters=curvature; midpointSlipDegrees=slip;
        }
        public float GetAngle(SteeringWheelPosition position)
        {
            switch(position)
            {
                case SteeringWheelPosition.FrontLeft: return frontLeftDegrees;
                case SteeringWheelPosition.FrontRight: return frontRightDegrees;
                case SteeringWheelPosition.RearLeft: return rearLeftDegrees;
                case SteeringWheelPosition.RearRight: return rearRightDegrees;
                default: return 0f;
            }
        }
    }
}

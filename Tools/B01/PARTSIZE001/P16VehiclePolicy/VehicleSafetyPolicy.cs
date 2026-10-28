// B01-23 / PASS16 -- PURE VEHICLE POLICY PROTOTYPE, NOT A UNITY COMPONENT.
// No dependency on UnityEngine, Physics, Codex R3, weapon-group input, or enemy AI.
// Vehicle controller integration requires a separate explicit Codex review.
using System;
using System.Collections.Generic;
using System.Linq;

namespace B01.PARTSIZE001.VehiclePolicy
{
    public enum PartFunction { Core, Weapon, Wheel, CrawlerTrack, OmniDrive, HoverEngine }
    public enum DriveSelection { Wheels, Tracks, Omni, Hover }
    public enum SafetyMode { InvalidSnapshot, CoreLost, CoastingNoDrive, DriveReady }
    public enum SteeringOverride { None, ObstacleLeft, ObstacleRight, StopNoClearance, AllyAhead }

    /// <summary>Snapshot of ONE physical owned part (never of an occupied voxel cell).</summary>
    public readonly record struct MountedPart(
        string Id, PartFunction Function, bool IsOperational, int SignedSide);

    /// <summary>
    /// Physical metres, relative to a vehicle-facing sensor at the current instant.
    /// +Infinity is "nothing detected". FrontObstacle excludes the intended hostile target.
    /// The caller must supply actual independently classified ally/obstacle observations.
    /// No raycasts or target classification are performed by this prototype.
    /// </summary>
    public readonly record struct SensorSnapshot(
        double FrontObstacleMetres, double LeftClearanceMetres,
        double RightClearanceMetres, double AllyAheadMetres,
        double HostileTargetAheadMetres, bool TargetConfirmedHostile,
        double ForwardSpeedMetresPerSecond);

    public readonly record struct DriverRequest(
        DriveSelection SelectedDrive, double Throttle, double Steering, bool RamRequested);

    public readonly record struct VehicleDecision(
        SafetyMode Mode, SteeringOverride Override, double Throttle, double Steering,
        bool HasFunctionalWeapon, bool RamAuthorized, string Reason);

    /// <summary>
    /// Fail-closed response policy after physical part loss.
    /// It only emits a *suggested normalized control intent*, not Rigidbody forces,
    /// weapon trigger commands, vehicle acceleration, damage, targeting, or collision.
    /// The canonical game remains authoritative for actual constraints.
    /// </summary>
    public static class VehicleSafetyPolicy
    {
        public const double AllyStopMetres = 5.0;
        public const double ObstacleAvoidMetres = 3.0;
        public const double SideClearanceRequiredMetres = 1.5;
        public const double ChargeMinimumSpeedMps = 2.0;
        public const double ChargeMinimumTargetMetres = 2.0;
        public const double ChargeMaximumTargetMetres = 12.0;

        private static VehicleDecision Safe(SafetyMode mode, string cause) =>
            new(mode, SteeringOverride.None, 0, 0, false, false, cause);

        private static bool ValidDistance(double x) =>
            !double.IsNaN(x) && x >= 0 && !double.IsNegativeInfinity(x);

        private static bool ValidSigned(double x) =>
            !double.IsNaN(x) && !double.IsInfinity(x) && x >= -1 && x <= 1;

        public static VehicleDecision Evaluate(
            IReadOnlyList<MountedPart>? source, DriverRequest driver, SensorSnapshot sensor)
        {
            if (source is null || source.Count == 0 ||
                !Enum.IsDefined(driver.SelectedDrive) ||
                !ValidSigned(driver.Throttle) || !ValidSigned(driver.Steering) ||
                !ValidDistance(sensor.FrontObstacleMetres) ||
                !ValidDistance(sensor.LeftClearanceMetres) ||
                !ValidDistance(sensor.RightClearanceMetres) ||
                !ValidDistance(sensor.AllyAheadMetres) ||
                !ValidDistance(sensor.HostileTargetAheadMetres) ||
                !double.IsFinite(sensor.ForwardSpeedMetresPerSecond) ||
                sensor.ForwardSpeedMetresPerSecond < 0)
                return Safe(SafetyMode.InvalidSnapshot, "INVALID_OR_UNTRUSTED_INPUT");

            var identifiers = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in source)
            {
                if (string.IsNullOrWhiteSpace(part.Id) ||
                    !identifiers.Add(part.Id) ||
                    !Enum.IsDefined(part.Function) ||
                    part.SignedSide is < -1 or > 1)
                    return Safe(SafetyMode.InvalidSnapshot, "INVALID_PART_ID_OR_METADATA");
            }

            bool hasCore = source.Any(p => p.IsOperational && p.Function == PartFunction.Core);
            if (!hasCore)
                return Safe(SafetyMode.CoreLost, "NO_FUNCTIONAL_CORE");

            bool hasWeapon = source.Any(p => p.IsOperational && p.Function == PartFunction.Weapon);
            bool leftWheel = source.Any(p => p.IsOperational && p.Function == PartFunction.Wheel && p.SignedSide == -1);
            bool rightWheel = source.Any(p => p.IsOperational && p.Function == PartFunction.Wheel && p.SignedSide == 1);
            bool leftTrack = source.Any(p => p.IsOperational && p.Function == PartFunction.CrawlerTrack && p.SignedSide == -1);
            bool rightTrack = source.Any(p => p.IsOperational && p.Function == PartFunction.CrawlerTrack && p.SignedSide == 1);
            bool hasDrive = driver.SelectedDrive switch
            {
                DriveSelection.Wheels => leftWheel && rightWheel,
                DriveSelection.Tracks => leftTrack && rightTrack,
                DriveSelection.Omni => source.Any(p => p.IsOperational && p.Function == PartFunction.OmniDrive),
                DriveSelection.Hover => source.Any(p => p.IsOperational && p.Function == PartFunction.HoverEngine),
                _ => false
            };

            // Never synthesize acceleration, steering, fallback-drive activation or
            // "charge" in a vehicle whose selected locomotion is unavailable.
            // Existing Rigidbody momentum (coasting) is NOT set to zero here.
            if (!hasDrive)
                return new VehicleDecision(SafetyMode.CoastingNoDrive,
                    SteeringOverride.None, 0, 0, hasWeapon, false, "NO_SELECTED_FUNCTIONAL_DRIVE");

            double throttle = driver.Throttle;
            double steer = driver.Steering;
            SteeringOverride safety = SteeringOverride.None;
            string reason = "NORMAL_DRIVE";

            // Ally and collision avoidance take priority over ram intent.
            if (throttle > 0 && sensor.AllyAheadMetres < AllyStopMetres)
            {
                throttle = 0;
                safety = SteeringOverride.AllyAhead;
                reason = "ALLY_IN_FORWARD_CORRIDOR";
            }
            else if (throttle > 0 && sensor.FrontObstacleMetres < ObstacleAvoidMetres)
            {
                throttle = 0;
                if (sensor.LeftClearanceMetres >= SideClearanceRequiredMetres &&
                    sensor.LeftClearanceMetres > sensor.RightClearanceMetres)
                {
                    steer = -1;
                    safety = SteeringOverride.ObstacleLeft;
                    reason = "OBSTACLE_STEER_LEFT";
                }
                else if (sensor.RightClearanceMetres >= SideClearanceRequiredMetres &&
                         sensor.RightClearanceMetres > sensor.LeftClearanceMetres)
                {
                    steer = 1;
                    safety = SteeringOverride.ObstacleRight;
                    reason = "OBSTACLE_STEER_RIGHT";
                }
                else
                {
                    steer = 0;
                    safety = SteeringOverride.StopNoClearance;
                    reason = "OBSTACLE_NO_SAFE_LATERAL_GAP";
                }
            }

            bool ram = driver.RamRequested && !hasWeapon &&
                throttle > 0.25 && safety == SteeringOverride.None &&
                sensor.TargetConfirmedHostile &&
                sensor.HostileTargetAheadMetres >= ChargeMinimumTargetMetres &&
                sensor.HostileTargetAheadMetres <= ChargeMaximumTargetMetres &&
                sensor.ForwardSpeedMetresPerSecond >= ChargeMinimumSpeedMps &&
                sensor.AllyAheadMetres >= AllyStopMetres &&
                sensor.FrontObstacleMetres >= ObstacleAvoidMetres;
            return new VehicleDecision(SafetyMode.DriveReady, safety, throttle,
                steer, hasWeapon, ram, ram ? "UNARMED_RAM_ELIGIBLE" : reason);
        }
    }
}

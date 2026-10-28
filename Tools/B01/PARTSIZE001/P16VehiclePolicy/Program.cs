// B01-23 P16 executable ORIGINAL C# policy contract, no UnityEngine shims.
// Each assertion evaluates VehicleSafetyPolicy itself, not a copied algorithm.
using B01.PARTSIZE001.VehiclePolicy;
using System;
using System.Collections.Generic;
using System.Linq;

internal static class Program
{
    private static readonly MountedPart Core = new("core", PartFunction.Core, true, 0);
    private static readonly MountedPart WL = new("left", PartFunction.Wheel, true, -1);
    private static readonly MountedPart WR = new("right", PartFunction.Wheel, true, 1);
    private static readonly MountedPart Gun = new("gun", PartFunction.Weapon, true, 0);
    private static readonly MountedPart TL = new("trackL", PartFunction.CrawlerTrack, true, -1);
    private static readonly MountedPart TR = new("trackR", PartFunction.CrawlerTrack, true, 1);
    private static readonly MountedPart Omni = new("omni", PartFunction.OmniDrive, true, 0);
    private static readonly MountedPart Hover = new("hover", PartFunction.HoverEngine, true, 0);

    private static readonly SensorSnapshot Open =
        new(double.PositiveInfinity, 8, 8, double.PositiveInfinity, 8, true, 4);
    private static readonly DriverRequest Forward =
        new(DriveSelection.Wheels, .8, .2, false);
    private static readonly DriverRequest Ram =
        new(DriveSelection.Wheels, .8, .2, true);
    private static readonly IReadOnlyList<MountedPart> Armed = new[] { Core, WL, WR, Gun };
    private static readonly IReadOnlyList<MountedPart> Unarmed = new[] { Core, WL, WR };

    private static VehicleDecision Run(IReadOnlyList<MountedPart>? parts, DriverRequest input, SensorSnapshot? sensors = null) =>
        VehicleSafetyPolicy.Evaluate(parts, input, sensors ?? Open);

    private static int passed;
    private static void Check(string name, Func<bool> assertion)
    {
        if (!assertion()) throw new InvalidOperationException("P16_VEHICLE_POLICY_FAIL:" + name);
        passed++;
        Console.WriteLine("PASS " + name);
    }
    private static void Eq<T>(string name, T actual, T expected) =>
        Check(name, () => EqualityComparer<T>.Default.Equals(actual, expected));

    public static int Main(string[] args)
    {
        if (args.Length > 1 || args.Any(a => a != "--selftest"))
        {
            Console.Error.WriteLine("Use --selftest or no arguments; this is NOT a Unity executable.");
            return 2;
        }
        try
        {
            var moving = Run(Armed, Forward);
            Eq("01_armed_drive_ready", moving.Mode, SafetyMode.DriveReady);
            Check("02_armed_keeps_pilot_control", () => moving.Throttle == .8 && moving.Steering == .2);
            Check("03_fire_availability_read_only", () => moving.HasFunctionalWeapon && !moving.RamAuthorized);
            Eq("04_unarmed_drive_ready", Run(Unarmed, Forward).Mode, SafetyMode.DriveReady);
            Check("05_unarmed_ram_forward_only", () => Run(Unarmed, Ram).RamAuthorized);
            Check("06_armed_ram_denied", () => !Run(Armed, Ram).RamAuthorized);
            Check("07_broken_gun_is_unarmed", () => Run(new[]{Core,WL,WR,Gun with { IsOperational=false }},Ram).RamAuthorized);
            Eq("08_one_wheel_lost_coasts", Run(new[]{Core,WL},Ram).Mode, SafetyMode.CoastingNoDrive);
            Check("09_broken_right_wheel_zero_command", () => {
                var d=Run(new[]{Core,WL,WR with { IsOperational=false }},Ram);
                return d.Throttle==0 && d.Steering==0 && !d.RamAuthorized;
            });
            Eq("10_no_core_is_disabled", Run(new[]{WL,WR},Ram).Mode, SafetyMode.CoreLost);
            Eq("11_broken_core_is_disabled", Run(new[]{Core with { IsOperational=false },WL,WR},Ram).Mode, SafetyMode.CoreLost);
            Check("12_no_selected_drive_fallback_disabled", () => {
                var p=new[]{Core,WL,Omni};
                return Run(p,Ram).Mode==SafetyMode.CoastingNoDrive;
            });
            Check("13_tracks_need_both_sides", () => {
                var d=Ram with {SelectedDrive=DriveSelection.Tracks};
                return Run(new[]{Core,TL,TR},d).RamAuthorized &&
                       Run(new[]{Core,TL},d).Mode==SafetyMode.CoastingNoDrive;
            });
            Check("14_omni_selected_operates", () => Run(new[]{Core,Omni},Ram with {SelectedDrive=DriveSelection.Omni}).RamAuthorized);
            Check("15_hover_selected_operates", () => Run(new[]{Core,Hover},Ram with {SelectedDrive=DriveSelection.Hover}).RamAuthorized);
            Eq("16_missing_parts_invalid",Run(null,Ram).Mode,SafetyMode.InvalidSnapshot);
            Eq("17_empty_parts_invalid",Run(Array.Empty<MountedPart>(),Ram).Mode,SafetyMode.InvalidSnapshot);
            Eq("18_duplicate_physical_ids_invalid",Run(new[]{Core,WL,WR,WR},Ram).Mode,SafetyMode.InvalidSnapshot);
            Eq("19_null_part_id_invalid",Run(new[]{Core,WL,WR with {Id=null!}},Ram).Mode,SafetyMode.InvalidSnapshot);
            Eq("20_unknown_enum_invalid",Run(new[]{Core,WL,WR with {Function=(PartFunction)666}},Ram).Mode,SafetyMode.InvalidSnapshot);
            Eq("21_bad_side_invalid",Run(new[]{Core,WL with {SignedSide=4},WR},Ram).Mode,SafetyMode.InvalidSnapshot);
            Eq("22_bad_drive_enum_invalid",Run(Unarmed,Ram with {SelectedDrive=(DriveSelection)444}).Mode,SafetyMode.InvalidSnapshot);
            Eq("23_nan_pilot_invalid",Run(Unarmed,Ram with {Throttle=double.NaN}).Mode,SafetyMode.InvalidSnapshot);
            Eq("24_outside_pilot_range_invalid",Run(Unarmed,Ram with {Steering=1.01}).Mode,SafetyMode.InvalidSnapshot);
            Eq("25_negative_sensor_invalid",Run(Unarmed,Ram,Open with {AllyAheadMetres=-1}).Mode,SafetyMode.InvalidSnapshot);
            Eq("26_nan_sensor_invalid",Run(Unarmed,Ram,Open with {FrontObstacleMetres=double.NaN}).Mode,SafetyMode.InvalidSnapshot);
            Eq("27_infinite_speed_invalid",Run(Unarmed,Ram,Open with {ForwardSpeedMetresPerSecond=double.PositiveInfinity}).Mode,SafetyMode.InvalidSnapshot);
            Check("28_friendly_corridor_bypass_denied", () => {
                var d=Run(Unarmed,Ram,Open with {AllyAheadMetres=4.99});
                return d.Override==SteeringOverride.AllyAhead && d.Throttle==0 && !d.RamAuthorized;
            });
            Check("29_friendly_just_at_threshold_not_blocked", () =>
                Run(Unarmed,Ram,Open with {AllyAheadMetres=5}).RamAuthorized);
            Check("30_front_obstacle_choose_left", () => {
                var d=Run(Unarmed,Ram,Open with {FrontObstacleMetres=2,LeftClearanceMetres=8,RightClearanceMetres=.3});
                return d.Override==SteeringOverride.ObstacleLeft && d.Steering==-1 && d.Throttle==0 && !d.RamAuthorized;
            });
            Check("31_front_obstacle_choose_right", () => {
                var d=Run(Unarmed,Ram,Open with {FrontObstacleMetres=2,LeftClearanceMetres=.4,RightClearanceMetres=8});
                return d.Override==SteeringOverride.ObstacleRight && d.Steering==1 && d.Throttle==0;
            });
            Check("32_no_safe_side_brake_only", () => {
                var d=Run(Unarmed,Ram,Open with {FrontObstacleMetres=1,LeftClearanceMetres=1,RightClearanceMetres=1});
                return d.Override==SteeringOverride.StopNoClearance && d.Steering==0 && d.Throttle==0;
            });
            Check("33_tied_clearance_never_autoselects_side", () =>
                Run(Unarmed,Ram,Open with {FrontObstacleMetres=1,LeftClearanceMetres=8,RightClearanceMetres=8})
                .Override==SteeringOverride.StopNoClearance);
            Check("34_obstacle_boundary_not_premature", () =>
                Run(Unarmed,Ram,Open with {FrontObstacleMetres=3}).RamAuthorized);
            Check("35_reverse_preserves_reverse", () => {
                var d=Run(Unarmed,Ram with {Throttle=-.5},Open with {FrontObstacleMetres=0,AllyAheadMetres=0});
                return d.Throttle==-.5 && d.Steering==.2 && d.Override==SteeringOverride.None && !d.RamAuthorized;
            });
            Check("36_unconfirmed_target_no_ram", () =>
                !Run(Unarmed,Ram,Open with {TargetConfirmedHostile=false}).RamAuthorized);
            Check("37_target_too_near_no_ram", () =>
                !Run(Unarmed,Ram,Open with {HostileTargetAheadMetres=1.99}).RamAuthorized);
            Check("38_target_too_far_no_ram", () =>
                !Run(Unarmed,Ram,Open with {HostileTargetAheadMetres=12.01}).RamAuthorized);
            Check("39_speed_too_low_no_ram", () =>
                !Run(Unarmed,Ram,Open with {ForwardSpeedMetresPerSecond=1.99}).RamAuthorized);
            Check("40_no_request_no_ram", () => !Run(Unarmed,Forward).RamAuthorized);
            Check("41_forward_input_too_low_no_ram", () => !Run(Unarmed,Ram with {Throttle=.25}).RamAuthorized);
            Check("42_charge_exact_boundaries", () =>
                Run(Unarmed,Ram,Open with {ForwardSpeedMetresPerSecond=2,HostileTargetAheadMetres=12}).RamAuthorized &&
                Run(Unarmed,Ram,Open with {HostileTargetAheadMetres=2}).RamAuthorized);
            Check("43_zero_speed_vehicle_coasts_without_charge", () =>
                !Run(Unarmed,Ram,Open with {ForwardSpeedMetresPerSecond=0}).RamAuthorized);
            Check("44_part_loss_then_repair_restores_same_id_only", () => {
                var before=Run(new[]{Core,WL,WR,Gun with {IsOperational=false}},Ram);
                var after=Run(Armed,Ram);
                return before.RamAuthorized && !after.RamAuthorized && after.HasFunctionalWeapon;
            });
            Check("45_ally_priority_over_obstacle", () => {
                var d=Run(Unarmed,Ram,Open with {AllyAheadMetres=1,FrontObstacleMetres=1});
                return d.Override==SteeringOverride.AllyAhead && d.Throttle==0 && !d.RamAuthorized;
            });
            Check("46_nondeterminism_absent", () => {
                var reference=Run(Unarmed,Ram,Open with {FrontObstacleMetres=2,LeftClearanceMetres=1,RightClearanceMetres=4});
                for(int i=0;i<1000;i++)
                    if(Run(Unarmed,Ram,Open with {FrontObstacleMetres=2,LeftClearanceMetres=1,RightClearanceMetres=4})!=reference)return false;
                return true;
            });
            Console.WriteLine("B01_23_P16_ORIGINAL_VEHICLE_POLICY_PASS " + passed +
                "/46; PURE_CSHARP_NET8; UNITY_PHYSX_NOT_RUN; GAME_NOT_INTEGRATED");
            return passed == 46 ? 0 : 1;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine(ex);
            Console.Error.WriteLine("B01_23_P16_ORIGINAL_VEHICLE_POLICY_FAIL; UNITY_NOT_RUN");
            return 1;
        }
    }
}

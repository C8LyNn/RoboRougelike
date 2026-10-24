using System;
using NUnit.Framework;
using UnityEngine;

namespace B01.MAPGEN001.Tests
{
    public sealed class MapGenRouteValidationTests
    {
        [TestCase(MapRecipe.CombatArena)]
        [TestCase(MapRecipe.ObstacleCourse)]
        public void HundredSeedsPassReferenceVehicleSweepsAndConnectivity(MapRecipe recipe)
        {
            var cfg = MapGenConfig.Default;
            for (int seed = 0; seed < 100; seed++)
            {
                var light = MapGenPlanner.Plan(recipe, seed, cfg, MapVehicleProfile.Light);
                var heavy = MapGenPlanner.Plan(recipe, seed, cfg, MapVehicleProfile.Heavy);
                var checkLight = MapGenRouteValidator.Validate(light, MapVehicleProfile.Light);
                var checkHeavy = MapGenRouteValidator.Validate(heavy, MapVehicleProfile.Heavy);
                Assert.IsTrue(checkLight.Passed, "Light seed=" + seed + ": " + checkLight);
                Assert.IsTrue(checkHeavy.Passed, "Heavy seed=" + seed + ": " + checkHeavy);
                Assert.Greater(checkHeavy.ExploredCells, 0);
                Assert.IsTrue(heavy.Diagnostics.Contains("BFS_SPINE_CLEARANCE_OK"));
            }
        }

        [Test] public void AnArenaWallThroughTheWholeRouteFailsClosed()
        {
            var p = MapGenPlanner.Plan(MapRecipe.CombatArena, 7, MapGenConfig.Default, MapVehicleProfile.Heavy);
            p.Elements.Add(new MapElement(MapElementKind.Obstacle,
                new Vector3(0, 1, 0), new Vector3(63, 2, 4), Quaternion.identity));
            var r = MapGenRouteValidator.Validate(p, MapVehicleProfile.Heavy);
            Assert.IsFalse(r.Passed, r.ToString());
            Assert.AreEqual("SPINE_FOOTPRINT_BLOCKED", r.Code);
        }

        [Test] public void HeavyRoverCannotStartWithRearHangingPastFloor()
        {
            var p = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 11, MapGenConfig.Default, MapVehicleProfile.Heavy);
            p.Start = new Vector3(p.Start.x, p.Start.y, 1f);
            var r = MapGenRouteValidator.Validate(p, MapVehicleProfile.Heavy);
            Assert.IsFalse(r.Passed, r.ToString());
            Assert.AreEqual("SPINE_FOOTPRINT_BLOCKED", r.Code);
        }

        [Test] public void RampBeyondVehicleSlopeCapabilityIsRejected()
        {
            var p = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 2, MapGenConfig.Default, MapVehicleProfile.Heavy);
            int i = p.Elements.FindIndex(e => e.Kind == MapElementKind.Ramp);
            Assert.GreaterOrEqual(i, 0);
            var e = p.Elements[i];
            e.Rotation = Quaternion.Euler(25f, 0, 0);
            p.Elements[i] = e;
            var r = MapGenRouteValidator.Validate(p, MapVehicleProfile.Heavy);
            Assert.IsFalse(r.Passed);
            Assert.AreEqual("RAMP_EXCEEDS_PROFILE_SLOPE", r.Code);
        }

        [Test] public void ArtificiallySharpWaypointFailsSweptRouteOrTurnRadius()
        {
            var p = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 2, MapGenConfig.Default, MapVehicleProfile.Heavy);
            var point = p.Checkpoints[1];
            p.Checkpoints[1] = new Vector3(point.x + 18f, point.y, point.z);
            var r = MapGenRouteValidator.Validate(p, MapVehicleProfile.Heavy);
            Assert.IsFalse(r.Passed);
            Assert.IsTrue(r.Code == "TURN_RADIUS_TOO_SMALL" || r.Code == "SPINE_FOOTPRINT_BLOCKED",
                r.ToString());
        }

        [Test] public void PlannerRejectsNonFiniteSlopeConfiguration()
        {
            var p = MapVehicleProfile.Light;
            p.MaxSlopeDegrees = float.NaN;
            // NaN cannot bypass slope validation through the default-value rule.
            var plan = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 9, MapGenConfig.Default, p);
            var result = MapGenRouteValidator.Validate(plan, p);
            Assert.IsFalse(result.Passed);
            Assert.AreEqual("INVALID_SLOPE_LIMIT", result.Code);
        }
    }
}

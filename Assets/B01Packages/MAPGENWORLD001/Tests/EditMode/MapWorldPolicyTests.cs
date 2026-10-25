using B01.MAPGEN001;
using NUnit.Framework;

namespace B01.MAPGENWORLD001.Tests
{
    public sealed class MapWorldPolicyTests
    {
        static MapWorldRequest Request(MapRecipe recipe, float width, int seed = 7)
        {
            var p = width > 4f ? MapVehicleProfile.Heavy : MapVehicleProfile.Light;
            p.Width = width;
            return new MapWorldRequest {
                Recipe = recipe, Seed = seed, Config = MapGenConfig.Default, Vehicle = p,
                EnvelopeMeasured = true, ClearancePerSideMeters = .25f
            };
        }
        [Test] public void ArenaLight7PassesWithMeasuredEnvelope()
        {
            Assert.IsTrue(MapWorldPolicy.TryPlan(Request(MapRecipe.CombatArena, 2.98f),
                out var map, out var status), status.ToString());
            Assert.IsNotEmpty(map.GeometryHash);
            Assert.AreEqual(MapWorldFailure.None, status);
        }
        [Test] public void CourseHeavy31PassesWithMeasuredEnvelope()
        {
            Assert.IsTrue(MapWorldPolicy.TryPlan(Request(MapRecipe.ObstacleCourse, 5.18f, 31),
                out var map, out var status), status.ToString());
            Assert.Greater(map.Checkpoints.Count, 0);
        }
        [Test] public void MissingMeasurementRejectedBeforeBuilder()
        {
            var r = Request(MapRecipe.CombatArena, 2.98f); r.EnvelopeMeasured = false;
            Assert.IsFalse(MapWorldPolicy.TryPlan(r, out _, out var status));
            Assert.AreEqual(MapWorldFailure.UnmeasuredEnvelope, status);
        }
        [Test] public void NarrowCourseWithRealLightRejects()
        {
            var r = Request(MapRecipe.ObstacleCourse, 2.98f);
            r.Config = MapGenConfig.Default;
            r.Config = new MapGenConfig {Segments=3, LaneWidth=10f, Difficulty=1f, ObstacleDensity=100};
            Assert.IsFalse(MapWorldPolicy.TryPlan(r, out _, out var status));
            Assert.AreEqual(MapWorldFailure.RejectedGeneratorConfig, status);
        }
        [Test] public void MinimumClearanceCannotBeWeakened()
        {
            var r = Request(MapRecipe.CombatArena, 2.98f); r.ClearancePerSideMeters = 0f;
            Assert.IsFalse(MapWorldPolicy.TryPlan(r, out _, out var status));
            Assert.AreEqual(MapWorldFailure.InvalidClearance, status);
        }
        [Test] public void NullAndInvalidRigFailClosed()
        {
            Assert.IsFalse(MapWorldPolicy.TryPlan(null, out _, out var n));
            Assert.AreEqual(MapWorldFailure.MissingRequest, n);
            var r = Request(MapRecipe.CombatArena, 2.98f); r.Vehicle.Width = float.NaN;
            Assert.IsFalse(MapWorldPolicy.TryPlan(r, out _, out var status));
            Assert.AreEqual(MapWorldFailure.RejectedGeneratorConfig, status);
        }
    }
}

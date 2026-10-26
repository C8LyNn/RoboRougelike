using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.MapSystemR4.Tests
{
    public sealed class MapR6JunctionTests
    {
        private static MapR4MapData TestMap(float padWidth)
        {
            return new MapR4MapData
            {
                mapId = "r6_turn_test",
                version = 6,
                nodes = new[]
                {
                    new MapR4NodeData { id = "west", position = new Vector3(-60,0,0) },
                    new MapR4NodeData { id = "center", position = Vector3.zero, padWidth = padWidth },
                    new MapR4NodeData { id = "north", position = new Vector3(0,0,60) }
                },
                edges = new[]
                {
                    new MapR4EdgeData { id = "west_to_center", fromNode="west", toNode="center", width=12, clearHeight=8, roadSegments=24,roadThickness=.35f },
                    new MapR4EdgeData { id = "center_to_north", fromNode="center", toNode="north", width=12,clearHeight=8,roadSegments=24,roadThickness=.35f }
                }
            };
        }
        private static MapR4VehicleProfile Vehicle(float minRadius) => new MapR4VehicleProfile
        {
            id="reference", width=2.6f, height=2f, length=3.5f, minTurnRadius=minRadius,
            maxSlopeDeg=15f, sideMargin=.3f, verticalMargin=.2f
        };

        [Test]
        public void WideJunction_ProducesContinuousCandidate()
        {
            var plan = MapR6JunctionPlanner.Plan(TestMap(36f), "west_to_center", "center_to_north", "center", Vehicle(.1f));
            Assert.IsTrue(plan.Valid, plan.Reason);
            Assert.Greater(plan.LengthMeters, 0f);
            Assert.Less(Vector3.Distance(plan.Evaluate(0f),plan.Start),.0001f);
            Assert.Less(Vector3.Distance(plan.Evaluate(1f),plan.End),.0001f);
        }

        [Test]
        public void SameJunction_RejectsOversizedTurnRadius()
        {
            var plan = MapR6JunctionPlanner.Plan(TestMap(36f), "west_to_center", "center_to_north", "center", Vehicle(100f));
            Assert.IsFalse(plan.Valid);
            StringAssert.Contains("radius", plan.Reason);
        }

        [Test]
        public void WrongConnection_IsRejected()
        {
            var plan = MapR6JunctionPlanner.Plan(TestMap(36f), "west_to_center", "center_to_north", "west", Vehicle(.1f));
            Assert.IsFalse(plan.Valid);
        }

        [Test]
        public void Junction_RejectsTooWideVehicle()
        {
            var vehicle = Vehicle(.1f);
            vehicle.width = 24f;
            var plan = MapR6JunctionPlanner.Plan(TestMap(36f), "west_to_center", "center_to_north", "center", vehicle);
            Assert.IsFalse(plan.Valid);
            StringAssert.Contains("envelope", plan.Reason);
        }
    }
}

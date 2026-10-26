using NUnit.Framework;
using UnityEngine;
using RoboRougelike.MapSystemR4;

namespace RoboRougelike.MapSystemR4.Tests
{
    public class MapR4GraphTests
    {
        private MapR4MapData map;
        private MapR4VehicleProfile standard;

        [SetUp]
        public void Setup()
        {
            var asset = Resources.Load<TextAsset>("MAP_R4_quarry_03");
            Assert.IsNotNull(asset, "Missing sample map Resources asset");
            map = MapR4MapData.Parse(asset.text);
            standard = new MapR4VehicleProfile
            {
                id = "standard", width = 2.6f, height = 2f, minTurnRadius = 4.5f,
                maxSlopeDeg = 16f, capabilities = new[] {"ground"}
            };
        }

        [Test] public void SampleMap_G0NoStructuralErrors() => Assert.IsEmpty(MapR4GraphValidator.Validate(map));
        [Test] public void SampleMap_ContainsExpectedTopology() { Assert.AreEqual(7, map.nodes.Length); Assert.AreEqual(8, map.edges.Length); }
        [Test] public void NormalRoute_Reachable() => Assert.IsTrue(MapR4GraphValidator.CanReach(map, "intact", "start", "exit", standard));
        [Test] public void CollapsedBridge_EscapeReachableViaLowerRoad() => Assert.IsTrue(MapR4GraphValidator.CanReach(map, "bridge_collapsed", "start", "exit", standard));
        [Test] public void AllEastRoutesBlocked_EscapeRejected() => Assert.IsFalse(MapR4GraphValidator.CanReach(map, "all_eastern_routes_closed", "start", "exit", standard));
        [Test] public void TooWideVehicle_Rejected()
        {
            standard.width = 8f;
            Assert.IsFalse(MapR4GraphValidator.CanReach(map, "intact", "start", "exit", standard));
        }
        [Test] public void PhysicallySteepLink_IsRejectedEvenIfSlopeMetadataIsLow()
        {
            foreach (var node in map.nodes)
                if (node.id == "junction") node.position = new Vector3(node.position.x, 100f, node.position.z);
            Assert.IsFalse(MapR4GraphValidator.CanReach(map, "intact", "start", "exit", standard));
        }
        [Test] public void UnknownStateRejected() => Assert.IsFalse(MapR4GraphValidator.CanReach(map, "missing", "start", "exit", standard));
        [Test] public void DanglingEdgeDetected()
        {
            map.edges[0].toNode = "missing";
            Assert.IsNotEmpty(MapR4GraphValidator.Validate(map));
        }
        [Test] public void NoCapabilityMeansNoSpecialRoute()
        {
            map.edges[0].requiredCapabilities = new[] {"hover"};
            Assert.IsFalse(MapR4GraphValidator.CanReach(map, "intact", "start", "exit", standard));
        }
    }
}

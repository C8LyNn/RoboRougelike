using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.MapSystemR4.Tests
{
    public sealed class MapR6RouteValidatorTests
    {
        private MapR4MapData map;
        private MapR4VehicleProfile vehicle;
        [SetUp]
        public void Setup()
        {
            var text = Resources.Load<TextAsset>("MAP_R4_quarry_03");
            Assert.IsNotNull(text);
            map = MapR4MapData.Parse(text.text);
            vehicle = new MapR4VehicleProfile
            {
                id="standard",width=2.6f,height=2f,length=3.5f,
                minTurnRadius=4.5f,maxSlopeDeg=16f,
                sideMargin=.35f,verticalMargin=.25f,capabilities=new[]{"ground"}
            };
        }
        [Test]
        public void CollapsedBridge_LowerDetourIsValidAfterJunctionRedesign()
        {
            var route = new[] { "start_junction", "junction_lower", "lower_exit" };
            bool result = MapR6RouteValidator.ValidateExplicitRoute(map,"bridge_collapsed","start",route,vehicle,out var reason);
            Assert.IsTrue(result,reason);
        }
        [Test]
        public void PhysicallyOversizedTurnVehicle_IsNotGivenFalseRoute()
        {
            vehicle.minTurnRadius=1000f;
            Assert.IsFalse(MapR6RouteValidator.CanReachWithTurns(map,"bridge_collapsed","start","exit",vehicle,out var reason),reason);
        }
        [Test]
        public void AllEasternRoutesClosed_RemainsUnreachable()
        {
            Assert.IsFalse(MapR6RouteValidator.CanReachWithTurns(map,"all_eastern_routes_closed","start","exit",vehicle,out var reason),reason);
        }
        [Test]
        public void NoUnknownStateAccepted()
        {
            Assert.IsFalse(MapR6RouteValidator.CanReachWithTurns(map,"missing","start","exit",vehicle,out var reason));
        }
        [Test]
        public void KnownState_CanReachExitViaTurnAwareGraph()
        {
            Assert.IsTrue(MapR6RouteValidator.CanReachWithTurns(map,"bridge_collapsed","start","exit",vehicle,out var reason),reason);
        }
    }
}

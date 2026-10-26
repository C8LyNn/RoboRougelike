using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.MapSystemR4.Tests
{
    public class MapR5TraversalProbePlayModeTests
    {
        private static MapR4VehicleProfile Vehicle() => new MapR4VehicleProfile
        {
            id = "test", width = 2.6f, height = 2f, length = 3.5f,
            minTurnRadius = 4f, maxSlopeDeg = 16f
        };

        private static MapR4EdgeData Straight() => new MapR4EdgeData
        {
            id = "test_straight", width = 8f, clearHeight = 5f,
            availableTurnRadius = 10f, roadSegments = 16, roadThickness = 0.35f
        };

        [UnityTest]
        public IEnumerator ObstacleOnSelectedLayer_IsRejected()
        {
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.layer = 8;
            obstacle.transform.position = new Vector3(5f, 1f, 0f);
            obstacle.transform.localScale = new Vector3(2f, 2f, 2f);
            yield return new WaitForFixedUpdate();
            var result = MapR5TraversalProbe.CheckEdge(Vector3.zero, new Vector3(10f, 0f, 0f),
                Straight(), Vehicle(), 1 << 8);
            Assert.AreEqual(MapR5ProbeStatus.Blocked, result.Status, result.Reason);
            Object.Destroy(obstacle);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClearSelectedObstacleLayer_PassesPartialProbe()
        {
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.layer = 8;
            obstacle.transform.position = new Vector3(70f, 1f, 70f);
            yield return new WaitForFixedUpdate();
            var result = MapR5TraversalProbe.CheckEdge(Vector3.zero, new Vector3(10f, 0f, 0f),
                Straight(), Vehicle(), 1 << 8);
            Assert.AreEqual(MapR5ProbeStatus.Clear, result.Status, result.Reason);
            Object.Destroy(obstacle);
            yield return null;
        }

        [Test]
        public void MissingObstacleMask_IsNotTreatedAsPass()
        {
            var result = MapR5TraversalProbe.CheckEdge(Vector3.zero, new Vector3(10f, 0f, 0f),
                Straight(), Vehicle(), (LayerMask)0);
            Assert.AreEqual(MapR5ProbeStatus.InvalidConfiguration, result.Status);
        }
    }
}

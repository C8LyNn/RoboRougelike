using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.MapSystemR4;

namespace RoboRougelike.MapSystemR4.Tests
{
    public class MapR4GeometryPlayModeTests
    {
        [UnityTest]
        public IEnumerator Builder_CreatesEightRoadsWithColliders()
        {
            var map = MapR4MapData.Parse(Resources.Load<TextAsset>("MAP_R4_quarry_03").text);
            var build = MapR4GeometryBuilder.Build(map, null);
            Assert.AreEqual(8, build.Edges.Count);
            foreach (var pair in build.Edges)
            {
                var meshFilter = pair.Value.GetComponent<MeshFilter>();
                Assert.IsNotNull(meshFilter);
                Assert.IsNotNull(pair.Value.GetComponent<MeshCollider>());
                Assert.Greater(meshFilter.sharedMesh.normals[0].y, 0f, "Road surface should face upward: " + pair.Key);
            }
            Object.Destroy(build.Root);
            MapR4GeometryBuilder.DisposeMeshes(build);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClearanceProbe_RejectsObstacleOnMarkedLayer()
        {
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.layer = 8; // Isolated sandbox sample layer; NOT a production layer assignment.
            obstacle.transform.position = new Vector3(5f, 1f, 0f);
            obstacle.transform.localScale = new Vector3(2f, 2f, 2f);
            Physics.SyncTransforms();
            Vector3 blocked;
            Assert.IsFalse(MapR4ClearanceProbe.CheckStraightCorridor(
                new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f),
                2f, 2f, 1 << 8, out blocked));
            Object.Destroy(obstacle);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClearanceProbe_ClearEmptyCorridorWhenObstaclesExistElsewhere()
        {
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.layer = 8;
            obstacle.transform.position = new Vector3(50f, 1f, 50f);
            Physics.SyncTransforms();
            Vector3 blocked;
            Assert.IsTrue(MapR4ClearanceProbe.CheckStraightCorridor(
                new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f),
                2f, 2f, 1 << 8, out blocked));
            Object.Destroy(obstacle);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BridgeState_ChangesActualEdgeActiveStatus()
        {
            var map = MapR4MapData.Parse(Resources.Load<TextAsset>("MAP_R4_quarry_03").text);
            var build = MapR4GeometryBuilder.Build(map, null);
            var state = new MapR4StateController(map, build);
            Assert.IsTrue(state.TrySetState("intact"));
            Assert.IsTrue(build.Edges["bridge_exit"].activeSelf);
            Assert.IsTrue(state.TrySetState("bridge_collapsed"));
            Assert.IsFalse(build.Edges["bridge_exit"].activeSelf);
            Assert.IsTrue(build.Edges["lower_exit"].activeSelf);
            Assert.IsFalse(state.TrySetState("unknown"));
            Object.Destroy(build.Root);
            MapR4GeometryBuilder.DisposeMeshes(build);
            yield return null;
        }
    }
}

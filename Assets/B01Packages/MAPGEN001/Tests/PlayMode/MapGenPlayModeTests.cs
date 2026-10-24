using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.MAPGEN001.Tests
{
    public sealed class MapGenPlayModeTests
    {
        [UnityTest] public IEnumerator GenerateAndClearRealPhysicsColliders()
        {
            var host = new GameObject("MapGenTestHost");
            var gen = host.AddComponent<MapGenBuilder>();
            try
            {
                var a = gen.Generate(MapRecipe.CombatArena, 101, MapGenConfig.Default, MapVehicleProfile.Light);
                Assert.NotNull(a.GeneratedRoot);
                Assert.Greater(a.GeneratedRoot.GetComponentsInChildren<Collider>().Length, 5);
                var old = a.GeneratedRoot;
                var b = gen.Generate(MapRecipe.ObstacleCourse, 202, MapGenConfig.Default, MapVehicleProfile.Heavy);
                Assert.IsFalse(old.activeSelf, "Old map colliders must be disabled synchronously");
                Assert.AreEqual(4, b.CheckpointDescriptors.Length);
                Assert.AreNotEqual(a.GeometryHash, b.GeometryHash);
                yield return null; // flush Unity Destroy
                Assert.IsTrue(old == null);
                gen.Clear();
                Assert.IsNull(gen.Current);
                yield return null;
                Assert.IsTrue(b.GeneratedRoot == null);
            }
            finally { if (host != null) Object.Destroy(host); }
        }

        [UnityTest] public IEnumerator RigidbodyActuallyFallsAndLandsOnGeneratedFloor()
        {
            var host = new GameObject("MapGenPhysicsTestHost");
            var gen = host.AddComponent<MapGenBuilder>();
            var layout = gen.Generate(MapRecipe.CombatArena, 45, MapGenConfig.Default, MapVehicleProfile.Light);
            var rover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var body = rover.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.position = layout.Start + Vector3.up * 2.5f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Physics.SyncTransforms();
            try
            {
                for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();
                Assert.Greater(body.position.y, .2f, "Fell through generated ground collider");
                Assert.Less(body.position.y, 1.6f, "Did not settle on generated floor");
                Assert.IsTrue(rover.activeInHierarchy);
            }
            finally { Object.Destroy(rover); Object.Destroy(host); }
        }

        [UnityTest] public IEnumerator RejectedInputPreservesPreviousLiveLevel()
        {
            var host = new GameObject("MapGenAtomicityTest");
            var gen = host.AddComponent<MapGenBuilder>();
            try
            {
                var good = gen.Generate(MapRecipe.ObstacleCourse, 5, MapGenConfig.Default, MapVehicleProfile.Light);
                var bad = MapGenConfig.Default; bad.LaneWidth = 10f;
                var heavy = new MapVehicleProfile { Width = 9f, Length = 8f, MinTurnRadius = 4f };
                Assert.Throws<System.ArgumentException>(() => gen.Generate(MapRecipe.ObstacleCourse, 5, bad, heavy));
                Assert.AreSame(good, gen.Current);
                Assert.IsTrue(good.GeneratedRoot.activeSelf);
                yield return null;
            }
            finally { Object.Destroy(host); }
        }
    }
}

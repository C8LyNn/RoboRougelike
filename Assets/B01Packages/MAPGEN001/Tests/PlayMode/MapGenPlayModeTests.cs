using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.MAPGEN001.Tests
{
    public sealed class MapGenPlayModeTests
    {
        [UnityTest] public IEnumerator GeneratedBlocksUseUrpShaderInsteadOfErrorMaterial()
        {
            // Codex W10 actual Unity receiver QA: generated cubes appeared
            // magenta with Hidden/InternalErrorShader in URP. Detect that at
            // runtime, not just in a source-only headless planner test.
            var host = new GameObject("MAPGEN001_URP_Material_Test");
            var builder = host.AddComponent<MapGenBuilder>();
            try
            {
                // Resources path must resolve in Player too; mere Editor
                // Shader.Find success is insufficient for a stripped build.
                var retained = Resources.Load<Shader>("MAPGEN001_BlocksURP");
                Assert.NotNull(retained, "Package-owned URP shader absent from Resources");
                Assert.AreEqual("B01/MAPGEN001/BlocksURP", retained.name);
                Assert.IsTrue(retained.isSupported);
                var layout = builder.Generate(MapRecipe.CombatArena, 7,
                    MapGenConfig.Default, MapVehicleProfile.Light);
                var renderers = layout.GeneratedRoot.GetComponentsInChildren<Renderer>();
                Assert.Greater(renderers.Length, 5);
                foreach (var renderer in renderers)
                {
                    Assert.NotNull(renderer.sharedMaterial);
                    Assert.NotNull(renderer.sharedMaterial.shader);
                    string shader = renderer.sharedMaterial.shader.name;
                    Assert.AreEqual("B01/MAPGEN001/BlocksURP", shader,
                        "Unexpected non-retained MAPGEN shader");
                    Assert.AreEqual("UniversalPipeline",
                        renderer.sharedMaterial.shader.GetTag("RenderPipeline", false),
                        "Shader lacks URP pipeline tag");
                    Assert.AreNotEqual("Hidden/InternalErrorShader", shader);
                }
                yield return null; // ensure render assignment survives a frame
            }
            finally { Object.Destroy(host); }
        }

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

        [UnityTest] public IEnumerator OffsetAndRotatedHostReturnsWorldSpaceMetadata()
        {
            var host = new GameObject("MapGenOffsetHost");
            host.transform.position = new Vector3(21f, 3f, -17f);
            host.transform.rotation = Quaternion.Euler(0f, 37f, 0f);
            var gen = host.AddComponent<MapGenBuilder>();
            try
            {
                var blueprint = MapGenPlanner.Plan(MapRecipe.ObstacleCourse, 92, MapGenConfig.Default, MapVehicleProfile.Light);
                var result = gen.Generate(MapRecipe.ObstacleCourse, 92, MapGenConfig.Default, MapVehicleProfile.Light);
                Assert.Less(Vector3.Distance(result.Start, host.transform.TransformPoint(blueprint.Start)), .001f);
                Assert.Less(Vector3.Distance(result.Finish, host.transform.TransformPoint(blueprint.Finish)), .001f);
                Assert.AreEqual(blueprint.Checkpoints.Count, result.CheckpointDescriptors.Length);
                Assert.Less(Vector3.Distance(result.CheckpointDescriptors[0],
                    host.transform.TransformPoint(blueprint.Checkpoints[0])), .001f);
                Assert.IsTrue(result.Bounds.Contains(result.Start));
                yield return null;
            }
            finally { Object.Destroy(host); }
        }


        [UnityTest] public IEnumerator RegenerateCyclesRemoveOldColliderRoots()
        {
            var host = new GameObject("MAPGEN001_RegenCycleHost");
            var generator = host.AddComponent<MapGenBuilder>();
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    var recipe = i % 2 == 0 ? MapRecipe.CombatArena : MapRecipe.ObstacleCourse;
                    var original = generator.Regenerate(recipe, 100 + i, MapGenConfig.Default, MapVehicleProfile.Heavy);
                    Assert.Greater(original.GeneratedRoot.GetComponentsInChildren<Collider>().Length, 0);
                    var replaced = generator.Regenerate(recipe, 200 + i, MapGenConfig.Default, MapVehicleProfile.Heavy);
                    Assert.AreNotSame(original.GeneratedRoot, replaced.GeneratedRoot);
                    Assert.IsFalse(original.GeneratedRoot.activeSelf, "Prior root must disable synchronously");
                    yield return null;
                    Assert.IsTrue(original.GeneratedRoot == null, "Old collider root not destroyed");
                    Assert.Greater(replaced.GeneratedRoot.GetComponentsInChildren<Collider>().Length, 0);
                    generator.Clear();
                    yield return null;
                    Assert.IsTrue(replaced.GeneratedRoot == null, "Clear left colliders alive");
                }
            }
            finally { Object.Destroy(host); }
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

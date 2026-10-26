using System;
using System.Collections;
using B01.MAPGEN001;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.MAPGENWORLD001.Tests
{
    /// <summary>
    /// W14 native Unity acceptance fixtures for the already-frozen adapter v1.1.
    /// This checks actual Unity colliders via Physics.ComputePenetration and
    /// metadata/lifecycle replay; it does NOT drive R6 or prove wheel traversal.
    /// Execute these in licensed Unity 6000.3.14f1, NOT in a C# math surrogate.
    /// </summary>
    public sealed class MapWorldW14NativeAcceptanceTests
    {
        private const float LightWidth = 2.980f;
        private const float HeavyWidth = 5.180f;
        private const float PenetrationTolerance = .005f;
        private const float ProbeHeight = 1.0f;
        private const int RouteStepsPerEdge = 8;

        private static MapWorldRequest Request(MapRecipe recipe, int seed, bool heavy)
        {
            var rig = heavy ? MapVehicleProfile.Heavy : MapVehicleProfile.Light;
            rig.Width = heavy ? HeavyWidth : LightWidth;
            return new MapWorldRequest {
                Recipe = recipe, Seed = seed, Vehicle = rig,
                Config = MapGenConfig.Default, EnvelopeMeasured = true,
                ClearancePerSideMeters = .25f
            };
        }

        [UnityTest] public IEnumerator W14_Arena07_Light_RealWidth() =>
            InspectPhysicalEnvelope(MapRecipe.CombatArena, 7, false);
        [UnityTest] public IEnumerator W14_Arena07_Heavy_RealWidth() =>
            InspectPhysicalEnvelope(MapRecipe.CombatArena, 7, true);
        [UnityTest] public IEnumerator W14_Arena31_Light_RealWidth() =>
            InspectPhysicalEnvelope(MapRecipe.CombatArena, 31, false);
        [UnityTest] public IEnumerator W14_Arena31_Heavy_RealWidth() =>
            InspectPhysicalEnvelope(MapRecipe.CombatArena, 31, true);
        [UnityTest] public IEnumerator W14_Arena83_Light_RealWidth() =>
            InspectPhysicalEnvelope(MapRecipe.CombatArena, 83, false);
        [UnityTest] public IEnumerator W14_Arena83_Heavy_RealWidth() =>
            InspectPhysicalEnvelope(MapRecipe.CombatArena, 83, true);
        [UnityTest] public IEnumerator W14_Course07_Light_RealWidth() =>
            InspectPhysicalEnvelope(MapRecipe.ObstacleCourse, 7, false);
        [UnityTest] public IEnumerator W14_Course07_Heavy_RealWidth() =>
            InspectPhysicalEnvelope(MapRecipe.ObstacleCourse, 7, true);
        [UnityTest] public IEnumerator W14_Course31_Light_RealWidth() =>
            InspectPhysicalEnvelope(MapRecipe.ObstacleCourse, 31, false);
        [UnityTest] public IEnumerator W14_Course31_Heavy_RealWidth() =>
            InspectPhysicalEnvelope(MapRecipe.ObstacleCourse, 31, true);
        [UnityTest] public IEnumerator W14_Course83_Light_RealWidth() =>
            InspectPhysicalEnvelope(MapRecipe.ObstacleCourse, 83, false);
        [UnityTest] public IEnumerator W14_Course83_Heavy_RealWidth() =>
            InspectPhysicalEnvelope(MapRecipe.ObstacleCourse, 83, true);

        private static IEnumerator InspectPhysicalEnvelope(MapRecipe recipe, int seed, bool heavy)
        {
            var host = new GameObject("B01_21_W14_NATIVE_CLEARANCE_" + recipe + "_" + seed);
            var adapter = host.AddComponent<MapGenWorldAdapter>();
            var builder = host.GetComponent<MapGenBuilder>();
            var probe = new GameObject("W14_Unpowered_EnvelopeProbe");
            var probeCollider = probe.AddComponent<BoxCollider>();
            var request = Request(recipe, seed, heavy);
            var profile = request.Vehicle;
            probeCollider.size = new Vector3(profile.Width, ProbeHeight, profile.Length);
            var snapshots = 0;
            var cleared = 0;
            adapter.WorldReady += _ => snapshots++;
            adapter.WorldCleared += _ => cleared++;
            try
            {
                Assert.IsTrue(adapter.TryGenerate(request, out var initial, out var reason),
                    "NATIVE_BUILD_FAIL recipe=" + recipe + " seed=" + seed +
                    " heavy=" + heavy + " error=" + reason);
                Assert.NotNull(builder.Current);
                Assert.AreEqual(initial.GeometryHash, builder.Current.GeometryHash);
                Assert.AreEqual(1, snapshots);
                yield return new WaitForFixedUpdate();
                Physics.SyncTransforms();

                var geometry = initial.GeneratedRoot.GetComponentsInChildren<Collider>();
                Assert.Greater(geometry.Length, 0);
                var path = initial.Checkpoints;
                var current = initial.Start;
                int samples = 0;
                float worstDepth = 0f;
                for (int i = 0; i <= path.Length; i++)
                {
                    var destination = i == path.Length ? initial.Finish : path[i];
                    for (int k = 0; k <= RouteStepsPerEdge; k++)
                    {
                        var point = Vector3.Lerp(current, destination, k / (float)RouteStepsPerEdge);
                        var orientation = host.transform.rotation;
                        // No rotation/steering shortcuts: constant hull orientation tests
                        // a conservative straight main corridor, not the optional side ramp.
                        foreach (var terrain in geometry)
                        {
                            if (!terrain.enabled || !terrain.gameObject.activeInHierarchy) continue;
                            if (!Physics.ComputePenetration(probeCollider, point, orientation,
                                terrain, terrain.transform.position, terrain.transform.rotation,
                                out var direction, out float depth)) continue;
                            worstDepth = Mathf.Max(worstDepth, depth);
                            Assert.LessOrEqual(depth, PenetrationTolerance,
                                "NATIVE_CONTACT_OVERLAP recipe=" + recipe +
                                " seed=" + seed + " heavy=" + heavy +
                                " sample=" + i + "/" + k +
                                " point=" + point + " depth=" + depth +
                                " collider=" + terrain.name);
                        }
                        samples++;
                    }
                    current = destination;
                }

                var receipt = initial.ReplayReceipt;
                Assert.IsNotEmpty(receipt);
                var originalRoot = initial.GeneratedRoot;
                var oldReadyCount = snapshots;
                var oldClearCount = cleared;
                var badReceipt = receipt.Substring(0, receipt.Length - 1) +
                    (receipt.EndsWith("0", StringComparison.Ordinal) ? "1" : "0");
                Assert.IsFalse(adapter.TryReplay(badReceipt, out var rejected, out reason));
                Assert.AreEqual(MapWorldFailure.InvalidReplayReceipt, reason);
                Assert.IsNull(rejected);
                Assert.AreSame(initial, adapter.Current);
                Assert.AreSame(originalRoot, builder.Current.GeneratedRoot);
                Assert.IsTrue(originalRoot.activeInHierarchy);
                Assert.AreEqual(oldReadyCount, snapshots);
                Assert.AreEqual(oldClearCount, cleared);

                Assert.IsTrue(adapter.TryReplay(receipt, out var replay, out reason),
                    "NATIVE_REPLAY_FAIL recipe=" + recipe + " seed=" + seed +
                    " heavy=" + heavy + " error=" + reason);
                Assert.AreEqual(initial.GeometryHash, replay.GeometryHash);
                Assert.AreEqual(receipt, replay.ReplayReceipt);
                Assert.AreEqual(2, snapshots);
                Assert.AreEqual(1, cleared);
                Assert.IsFalse(originalRoot.activeInHierarchy);
                Debug.Log("W14_NATIVE_MAP_REPLAY_AND_ENVELOPE_CASE_PASS recipe=" +
                    recipe + " seed=" + seed + " heavy=" + heavy +
                    " width=" + profile.Width + " length=" + profile.Length +
                    " samples=" + samples + " worstOverlapM=" + worstDepth +
                    " oldRootCleared=true nativePhysxUnpowered=true actualR6Drive=NOT_RUN");
            }
            finally
            {
                if (adapter != null) adapter.ClearWorld();
                UnityEngine.Object.Destroy(probe);
                UnityEngine.Object.Destroy(host);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator W14_RealLight10mImpossibleCourseRejectsWithoutTouchingArena()
        {
            var host = new GameObject("B01_21_W14_NATIVE_BAD_COURSE");
            var adapter = host.AddComponent<MapGenWorldAdapter>();
            var builder = host.GetComponent<MapGenBuilder>();
            int ready = 0, cleared = 0;
            adapter.WorldReady += _ => ready++;
            adapter.WorldCleared += _ => cleared++;
            try
            {
                Assert.IsTrue(adapter.TryGenerate(Request(MapRecipe.CombatArena, 7, false),
                    out var arena, out var failure), failure.ToString());
                foreach (var seed in new[] { 2, 12, 14, 22, 28 })
                {
                    var invalid = Request(MapRecipe.ObstacleCourse, seed, false);
                    invalid.Config = new MapGenConfig {
                        Segments = 3, LaneWidth = 10f, Difficulty = 1f, ObstacleDensity = 100
                    };
                    Assert.IsFalse(adapter.TryRegenerate(invalid, out var next, out failure),
                        "10m real-Light fail-closed seed=" + seed);
                    Assert.IsNull(next);
                    Assert.AreEqual(MapWorldFailure.RejectedGeneratorConfig, failure);
                    Assert.AreSame(arena, adapter.Current);
                    Assert.AreSame(arena.GeneratedRoot, builder.Current.GeneratedRoot);
                    Assert.IsTrue(arena.GeneratedRoot.activeInHierarchy);
                }
                Assert.AreEqual(1, ready);
                Assert.AreEqual(0, cleared);
                yield return null;
            }
            finally
            {
                if (adapter != null) adapter.ClearWorld();
                UnityEngine.Object.Destroy(host);
            }
        }
    }
}

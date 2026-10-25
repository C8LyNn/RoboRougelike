using System.Collections;
using B01.MAPGEN001;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.MAPGENWORLD001.Tests
{
    public sealed class MapWorldLifecycleTests
    {
        static MapWorldRequest Request(int seed=7, bool heavy=false)
        {
            var p = heavy ? MapVehicleProfile.Heavy : MapVehicleProfile.Light;
            p.Width = heavy ? 5.18f : 2.98f;
            return new MapWorldRequest { Recipe=MapRecipe.CombatArena, Seed=seed,
                Config=MapGenConfig.Default, Vehicle=p,
                EnvelopeMeasured=true, ClearancePerSideMeters=.25f };
        }

        [UnityTest] public IEnumerator GenerateRegenerateClearAndNotifyOnce()
        {
            var host = new GameObject("WORLD001_Lifecycle_Test");
            var adapter = host.AddComponent<MapGenWorldAdapter>();
            int ready=0, cleared=0;
            adapter.WorldReady += _ => ready++;
            adapter.WorldCleared += _ => cleared++;
            try
            {
                Assert.IsTrue(adapter.TryGenerate(Request(), out var first, out var status), status.ToString());
                Assert.AreEqual(1, ready);
                Assert.IsTrue(adapter.TryRegenerate(Request(31), out var next, out status), status.ToString());
                Assert.AreEqual(2, ready); Assert.AreEqual(1, cleared);
                Assert.Greater(next.Revision, first.Revision);
                Assert.AreNotEqual(first.GeometryHash, next.GeometryHash);
                adapter.ClearWorld(); Assert.AreEqual(2, cleared);
                Assert.IsNull(adapter.Current);
                adapter.ClearWorld(); Assert.AreEqual(2, cleared);
                yield return null;
                Assert.IsTrue(first.GeneratedRoot == null && next.GeneratedRoot == null);
            }
            finally { Object.Destroy(host); }
        }

        [UnityTest] public IEnumerator InvalidInputPreservesOldSceneAndCallbacks()
        {
            var host = new GameObject("WORLD001_AtomicReject_Test");
            var adapter = host.AddComponent<MapGenWorldAdapter>();
            int ready=0, cleared=0;
            adapter.WorldReady += _ => ready++;
            adapter.WorldCleared += _ => cleared++;
            try
            {
                Assert.IsTrue(adapter.TryGenerate(Request(), out var before, out _));
                var bad=Request(12); bad.EnvelopeMeasured=false;
                Assert.IsFalse(adapter.TryRegenerate(bad, out var unused, out var status));
                Assert.IsNull(unused);
                Assert.AreEqual(MapWorldFailure.UnmeasuredEnvelope, status);
                Assert.AreSame(before, adapter.Current);
                Assert.IsTrue(before.GeneratedRoot.activeSelf);
                Assert.AreEqual(1,ready); Assert.AreEqual(0,cleared);
                yield return null;
            }
            finally { Object.Destroy(host); }
        }

        [UnityTest] public IEnumerator MissingMeasuredProfileAndNarrowRealLightDoNotSpawn()
        {
            var host = new GameObject("WORLD001_Negative_Test");
            var adapter = host.AddComponent<MapGenWorldAdapter>();
            try
            {
                var r=Request(); r.EnvelopeMeasured=false;
                Assert.IsFalse(adapter.TryGenerate(r,out _,out var failure));
                Assert.AreEqual(MapWorldFailure.UnmeasuredEnvelope,failure);
                r=Request(); r.Recipe=MapRecipe.ObstacleCourse;
                r.Config=new MapGenConfig {Segments=3,LaneWidth=10f,Difficulty=1f,ObstacleDensity=100};
                Assert.IsFalse(adapter.TryGenerate(r,out _,out failure));
                Assert.AreEqual(MapWorldFailure.RejectedGeneratorConfig,failure);
                Assert.IsNull(adapter.Current);
                yield return null;
            }
            finally { Object.Destroy(host); }
        }
    }
}

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

        [UnityTest] public IEnumerator RotatedAndOffsetHostAcceptsValidatedWorldMetadata()
        {
            // Native-only: source math surrogate cannot establish transformed Collider bounds.
            var host=new GameObject("WORLD001_OffsetMetadata_Test");
            host.transform.position=new Vector3(17f,2.5f,-13f);
            host.transform.rotation=Quaternion.Euler(0f,41f,0f);
            var adapter=host.AddComponent<MapGenWorldAdapter>();
            try
            {
                var req=Request(31);req.Recipe=MapRecipe.ObstacleCourse;
                var plan=MapGenPlanner.Plan(req.Recipe,req.Seed,req.Config,req.Vehicle);
                Assert.IsTrue(adapter.TryGenerate(req,out var world,out var error),error.ToString());
                Assert.Less(Vector3.Distance(host.transform.TransformPoint(plan.Start),world.Start),.001f);
                Assert.Less(Vector3.Distance(host.transform.TransformPoint(plan.Finish),world.Finish),.001f);
                Assert.AreEqual(plan.Checkpoints.Count,world.Checkpoints.Length);
                Assert.Greater(Vector3.Distance(world.Start,plan.Start),1f,
                    "Offset host points must be WORLD coordinates");
                adapter.ClearWorld();
                yield return null;
            }
            finally { Object.Destroy(host); }
        }

        [UnityTest] public IEnumerator ReplayReceiptRegeneratesExactlyOrFailsClosed()
        {
            var host=new GameObject("WORLD001_ReplayNative");
            var adapter=host.AddComponent<MapGenWorldAdapter>();
            try
            {
                var request=Request(31);request.Recipe=MapRecipe.ObstacleCourse;
                Assert.IsTrue(adapter.TryGenerate(request,out var first,out var failure),failure.ToString());
                Assert.IsNotEmpty(first.ReplayReceipt);
                Assert.IsTrue(adapter.TryReplay(first.ReplayReceipt,out var second,out failure),failure.ToString());
                Assert.AreEqual(first.GeometryHash,second.GeometryHash);
                Assert.AreEqual(first.ReplayReceipt,second.ReplayReceipt);
                var original=adapter.Current;
                Assert.IsFalse(adapter.TryReplay(first.ReplayReceipt+"0",out var rejected,out failure));
                Assert.AreEqual(MapWorldFailure.InvalidReplayReceipt,failure);
                Assert.IsNull(rejected);
                Assert.AreSame(original,adapter.Current);
                adapter.ClearWorld();
                yield return null;
            }
            finally {Object.Destroy(host);}
        }

        [UnityTest] public IEnumerator W15SignedSeedGeometryContradictionKeepsLiveMap()
        {
            // Native Unity regression. A well-formed, rechecksummed save refers
            // to a real but DIFFERENT layout. Check real builder root identity.
            var host = new GameObject("WORLD001_W15_SignedSeedDrift");
            var adapter = host.AddComponent<MapGenWorldAdapter>();
            var builder = host.GetComponent<MapGenBuilder>();
            int ready=0, cleared=0;
            adapter.WorldReady += _ => ready++;
            adapter.WorldCleared += _ => cleared++;
            try
            {
                var request=Request(7);
                request.Recipe=MapRecipe.ObstacleCourse;
                Assert.IsTrue(adapter.TryGenerate(request,out var first,out var reason),
                    "W15 baseline "+reason);
                var alternative=Request(31);
                alternative.Recipe=MapRecipe.ObstacleCourse;
                var plan=MapGenPlanner.Plan(alternative.Recipe,alternative.Seed,
                    alternative.Config,alternative.Vehicle);
                Assert.AreNotEqual(first.GeometryHash,plan.GeometryHash,
                    "W15 test requires two genuinely different MAPGEN layouts");

                var parts=first.ReplayReceipt.Split('|');
                parts[2]="31"; // tamper seed, retain old original geometry hash
                var body=string.Join("|",parts,0,parts.Length-1);
                string digest;
                using(var sha=System.Security.Cryptography.SHA256.Create())
                    digest=System.BitConverter.ToString(
                        sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(body)))
                        .Replace("-","").ToLowerInvariant();
                var alternate=body+"|"+digest; // fully valid new checksum, not bit-flip
                var existing=builder.Current;
                var root=first.GeneratedRoot;
                Assert.IsFalse(adapter.TryReplay(alternate,out var rejected,out reason));
                Assert.AreEqual(MapWorldFailure.ReplayGeometryMismatch,reason);
                Assert.IsNull(rejected);
                Assert.AreSame(first,adapter.Current);
                Assert.AreSame(existing,builder.Current);
                Assert.AreSame(root,builder.Current.GeneratedRoot);
                Assert.IsTrue(root.activeInHierarchy);
                Assert.AreEqual(1,ready);
                Assert.AreEqual(0,cleared);
                yield return null;
                Assert.IsTrue(root.activeInHierarchy);
            }
            finally
            {
                if (adapter != null) adapter.ClearWorld();
                Object.Destroy(host);
            }
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

using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.UnitySaveBridge001;

namespace B01.CI41.NativeSaveQA
{
    // SOURCE-AUTHORED, NATIVE NOT_RUN until real Unity 6000.3.14f1 PlayMode.
    // Only an isolated #604 host demo. NO canonical frozen #510 Rig[] hydration.
    public sealed class SaveBridgeFailureNativeTests
    {
        private static string Slot() => "CI41W17_" + Guid.NewGuid().ToString("N");
        private static string FencePath(string slot) =>
            Path.Combine(Application.persistentDataPath,
                "B0123UnitySaveBridge001", "W16CheckpointFences", slot + ".fence");

        [UnityTest]
        public IEnumerator CorruptPersistentFence_QuarantinesFreshHostBeforePhysicalPublication()
        {
            string slot=Slot();
            string fence=FencePath(slot);
            var oldGo=new GameObject("CI41 W17 corrupt fence original");
            var freshGo=new GameObject("CI41 W17 corrupt fence fresh");
            try
            {
                var old=oldGo.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(old.UsePersistentCheckpointSlot(slot));
                byte[] original=IsolatedTwinRigSaveHost.MakeFixture(861,9,true,false);
                Assert.IsTrue(old.TryRestoreBytes(original,861,9));
                Assert.IsTrue(old.IsConsumed(0,2310001));
                Assert.IsTrue(File.Exists(fence));
                byte[] physical=File.ReadAllBytes(fence);
                Assert.AreEqual(88,physical.Length);
                physical[26]^=0x5A; // real disk watermark checksum invalid, not snapshot data
                File.WriteAllBytes(fence,physical);
                var fresh=freshGo.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(fresh.UsePersistentCheckpointSlot(slot));
                Assert.IsFalse(fresh.TryRestoreBytes(original,861,9));
                Assert.AreEqual(WorldSwapVerdict.RollbackQuarantine,fresh.LastRestoreVerdict);
                Assert.IsTrue(fresh.Quarantined);
                Assert.IsFalse(fresh.HasWorld);
                Assert.AreEqual(0,freshGo.GetComponentsInChildren<Rigidbody>(true).Length);
                Assert.IsFalse(fresh.TryRestoreBytes(original,861,9));
                Assert.IsTrue(old.HasWorld);
                Assert.IsTrue(old.IsConsumed(0,2310001));
                yield return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(oldGo);
                UnityEngine.Object.Destroy(freshGo);
                if(File.Exists(fence))File.Delete(fence); // own unique CI41 slot only
            }
        }

        [UnityTest]
        public IEnumerator StaleGenerationOnFreshHost_DeniedWithoutPublishingNewRigidbodies()
        {
            string slot=Slot();
            string fence=FencePath(slot);
            var firstGo=new GameObject("CI41 W17 original generation");
            var freshGo=new GameObject("CI41 W17 fresh generation");
            try
            {
                var first=firstGo.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(first.UsePersistentCheckpointSlot(slot));
                byte[] newer=IsolatedTwinRigSaveHost.MakeFixture(862,12,true,false);
                Assert.IsTrue(first.TryRestoreBytes(newer,862,12));
                var fresh=freshGo.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(fresh.UsePersistentCheckpointSlot(slot));
                Assert.IsFalse(fresh.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(862,11,false,true),862,11));
                Assert.AreEqual(WorldSwapVerdict.StaleGeneration,fresh.LastRestoreVerdict);
                Assert.IsFalse(fresh.HasWorld);
                Assert.IsFalse(fresh.Quarantined);
                Assert.AreEqual(0,freshGo.GetComponentsInChildren<Rigidbody>(true).Length);
                // Genuine separate Unity host object, same process: exact cold replay
                // of the original immutable bytes is allowed after stale refusal.
                Assert.IsTrue(fresh.TryRestoreBytes(newer,862,12));
                Assert.AreEqual(12,fresh.Generation);
                Assert.IsTrue(fresh.IsConsumed(0,2310001));
                Assert.IsFalse(fresh.IsConsumed(1,2310002));
                yield return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(firstGo);
                UnityEngine.Object.Destroy(freshGo);
                if(File.Exists(fence))File.Delete(fence);
            }
        }

        [UnityTest]
        public IEnumerator ForeignRunSameFence_DeniedWithPriorWorldAndConsumedIdsIntact()
        {
            string slot=Slot();
            string fence=FencePath(slot);
            var firstGo=new GameObject("CI41 W17 original run");
            var foreignGo=new GameObject("CI41 W17 foreign run");
            try
            {
                var first=firstGo.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(first.UsePersistentCheckpointSlot(slot));
                Assert.IsTrue(first.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(863,8,false,true),863,8));
                var foreign=foreignGo.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsTrue(foreign.UsePersistentCheckpointSlot(slot));
                Assert.IsFalse(foreign.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(864,8,true,false),864,8));
                Assert.AreEqual(WorldSwapVerdict.StaleGeneration,foreign.LastRestoreVerdict);
                Assert.IsFalse(foreign.HasWorld);
                Assert.AreEqual(0,foreignGo.GetComponentsInChildren<Rigidbody>(true).Length);
                Assert.IsTrue(first.HasWorld);
                Assert.IsFalse(first.IsConsumed(0,2310001));
                Assert.IsTrue(first.IsConsumed(1,2310002));
                Assert.AreEqual(8,first.Generation);
                yield return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(firstGo);
                UnityEngine.Object.Destroy(foreignGo);
                if(File.Exists(fence))File.Delete(fence);
            }
        }

        [UnityTest]
        public IEnumerator InvalidSlotName_RejectsTraversalAndNeverEnablesPersistentFence()
        {
            string slot=Slot();
            string fence=FencePath(slot);
            var go=new GameObject("CI41 W17 slot validation");
            try
            {
                var host=go.AddComponent<IsolatedTwinRigSaveHost>();
                Assert.IsFalse(host.UsePersistentCheckpointSlot("../foreign"));
                Assert.IsFalse(host.UsePersistentCheckpointSlot("a/b"));
                Assert.IsFalse(host.UsePersistentCheckpointSlot("a\\b"));
                Assert.IsFalse(host.UsePersistentCheckpointSlot("a..b"));
                Assert.IsFalse(host.UsePersistentCheckpointSlot(new string('a',33)));
                Assert.IsFalse(host.UsePersistentCheckpointSlot(""));
                Assert.IsFalse(host.PersistentCheckpointEnabled);
                Assert.IsFalse(host.HasWorld);
                Assert.IsTrue(host.UsePersistentCheckpointSlot(slot));
                Assert.IsTrue(host.PersistentCheckpointEnabled);
                Assert.IsTrue(host.TryRestoreBytes(
                    IsolatedTwinRigSaveHost.MakeFixture(865,1,true,true),865,1));
                Assert.IsTrue(File.Exists(fence));
                Assert.IsTrue(host.IsConsumed(0,2310001));
                Assert.IsTrue(host.IsConsumed(1,2310002));
                yield return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(go);
                if(File.Exists(fence))File.Delete(fence);
            }
        }
    }
}

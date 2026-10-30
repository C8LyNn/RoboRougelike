using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.DetachedSaveContract001;
using B01.SecureSaveHost001;
using System.Collections;

namespace B01.SecureSaveHost001.Tests
{
    public sealed class IsolatedAtomicHostPlayModeTests
    {
        [UnityTest]
        public IEnumerator DetachedLightHeavy_SaveColdLoad_PhysicalState_OriginalW06Ids()
        {
            var first = new GameObject("B01-23 real Editor/PhysX host A");
            var host=first.AddComponent<IsolatedSecureV2SaveHost>();
            Assert.AreEqual(RestoreVerdict.Accepted,host.InitializeDemo(true,true));
            Assert.AreEqual(RestoreVerdict.Accepted,host.TryCapture(out byte[] bytes));
            Assert.IsTrue(ColdRestore.TryStage(bytes,out ColdRestore plan));
            Assert.IsTrue(plan.ContainsConsumedId(0,2310001));
            Assert.IsTrue(plan.ContainsConsumedId(1,2310002));
            UnityEngine.Object.Destroy(first);
            yield return null; // actually destroy previous scene host
            var second=new GameObject("B01-23 real Editor/PhysX host B");
            var restored=second.AddComponent<IsolatedSecureV2SaveHost>();
            Assert.AreEqual(RestoreVerdict.Accepted,restored.TryRestore(bytes,1));
            Assert.AreEqual(RestoreVerdict.Accepted,restored.TryCapture(out byte[] bytes2));
            Assert.IsTrue(ColdRestore.TryStage(bytes2,out ColdRestore plan2));
            Assert.IsTrue(plan2.ContainsConsumedId(0,2310001));
            Assert.IsTrue(plan2.ContainsConsumedId(1,2310002));
            UnityEngine.Object.Destroy(second);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InvalidChecksum_RetainsAttachedWorldAndGeneration()
        {
            var obj=new GameObject("B01-23 real source invalid physical state");
            var host=obj.AddComponent<IsolatedSecureV2SaveHost>();
            Assert.AreEqual(RestoreVerdict.Accepted,host.InitializeDemo(false,false));
            Assert.AreEqual(RestoreVerdict.Accepted,host.TryCapture(out byte[] bytes));
            bytes[99]^=1;
            Assert.AreEqual(RestoreVerdict.InvalidSnapshot,host.TryRestore(bytes,1));
            Assert.AreEqual(1,host.Generation);
            Assert.AreEqual(RestoreVerdict.Accepted,host.TryCapture(out byte[] old));
            Assert.IsTrue(ColdRestore.TryStage(old,out ColdRestore plan));
            Assert.IsFalse(plan.ContainsConsumedId(0,2310001));
            Assert.IsFalse(plan.ContainsConsumedId(1,2310002));
            UnityEngine.Object.Destroy(obj);
            yield return null;
        }
    }
}

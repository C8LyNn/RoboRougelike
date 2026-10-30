using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.B0132P17.DualRecoil.Tests
{
    public sealed class DualRecoilPlayModeTests
    {
        private GameObject Create()
        {
            var prefab = Resources.Load<GameObject>("B0132_DualRecoil");
            Assert.IsNotNull(prefab, "Original source import prefab was not found");
            var actual = Object.Instantiate(prefab);
            Assert.IsNotNull(actual.GetComponent<B0132DualRecoilVisual>());
            return actual;
        }

        [UnityTest]
        public IEnumerator PrefabCreatesTwoMeshBarrelsWithoutPhysicalColliders()
        {
            var actual = Create();
            yield return null;
            Assert.AreEqual(2, actual.GetComponentsInChildren<MeshFilter>().Length);
            Assert.AreEqual(2, actual.GetComponentsInChildren<MeshRenderer>().Length);
            Assert.AreEqual(0, actual.GetComponentsInChildren<Collider>().Length,
                "Cosmetic animation cannot change vehicle physics");
            Assert.AreEqual(0, actual.GetComponentsInChildren<Rigidbody>().Length);
            Object.Destroy(actual);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OneLaneKickDoesNotFireOtherLaneAndOffResets()
        {
            var actual = Create();
            var actor = actual.GetComponent<B0132DualRecoilVisual>();
            Assert.IsTrue(actor.PlayLeftShot());
            Assert.IsFalse(actor.PlayRightShot(float.NaN));
            yield return null;
            var pose = actor.CurrentPose;
            Assert.Greater(pose.LeftTravel, 0f);
            Assert.AreEqual(0f, pose.RightTravel, 0.00001f);
            actor.SetVisualsEnabled(false);
            Assert.IsFalse(actor.PlayRightShot());
            Assert.AreEqual(0f, actor.CurrentPose.LeftTravel, 0.00001f);
            actor.SetVisualsEnabled(true);
            Assert.IsTrue(actor.PlayRightShot());
            yield return null;
            Assert.Greater(actor.CurrentPose.RightTravel, 0f);
            Object.Destroy(actual);
            yield return null;
        }
    }
}

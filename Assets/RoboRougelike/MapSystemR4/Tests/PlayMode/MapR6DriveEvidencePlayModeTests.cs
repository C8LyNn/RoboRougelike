using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.MapSystemR4.Tests
{
    public sealed class MapR6DriveEvidencePlayModeTests
    {
        [UnityTest]
        public IEnumerator RecorderTracksSyntheticRigidbodyMotion_NotRealDriving()
        {
            var go = new GameObject("R6_TEST_BODY");
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var record = go.AddComponent<MapR6DriveEvidenceRecorder>();
            record.SetDriverDemand01(1f);
            yield return new WaitForFixedUpdate();
            rb.MovePosition(new Vector3(2f,0,0));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(record.Snapshot().hadMotionSamples);
            Assert.Greater(record.Snapshot().travelledMeters,0f);
            StringAssert.Contains("RECORDED_ONLY",record.Snapshot().verdict);
            Object.Destroy(go);
            yield return null;
        }
    }
}

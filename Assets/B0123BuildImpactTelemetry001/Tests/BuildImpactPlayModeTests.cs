using System.Collections;
using B01.BuildImpactTelemetry001;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.BuildImpactTelemetry001.Tests
{
    public sealed class BuildImpactPlayModeTests
    {
        [UnityTest]
        public IEnumerator FallingBodyProducesActualPhysXImpactWithoutChangingBodyState()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(20f, 1f, 20f);
            part.transform.position = new Vector3(0f, 4f, 0f);
            var body = part.AddComponent<Rigidbody>();
            body.mass = 3f;
            var telemetry = part.AddComponent<BuildImpactTelemetry>();
            telemetry.Configure(0.1f);
            int callbacks = 0;
            telemetry.ImpactRecorded += _ => callbacks++;
            try
            {
                for (int i = 0; i < 180 && callbacks == 0; i++)
                    yield return new WaitForFixedUpdate();
                Assert.Greater(callbacks, 0, "No actual Rigidbody/Collider contact occurred");
                Assert.Greater(telemetry.LastSample.ImpulseNewtonSeconds, 0.1f);
                Assert.Greater(telemetry.LastSample.RelativeSpeedMetersPerSecond, 0f);
                Assert.AreEqual(3f, body.mass, 0.001f);
                Assert.IsFalse(body.isKinematic);
                Assert.AreEqual(callbacks, telemetry.TotalRecorded);
            }
            finally
            {
                Object.Destroy(part);
                Object.Destroy(floor);
            }
        }

        [Test]
        public void InvalidThresholdRejected()
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var telemetry = part.AddComponent<BuildImpactTelemetry>();
                Assert.Throws<System.ArgumentOutOfRangeException>(() => telemetry.Configure(-1f));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => telemetry.Configure(float.NaN));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => telemetry.Configure(float.PositiveInfinity));
            }
            finally { Object.DestroyImmediate(part); }
        }
    }
}

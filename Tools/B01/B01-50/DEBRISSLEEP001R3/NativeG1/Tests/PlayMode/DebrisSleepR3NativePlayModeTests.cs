// NATIVE UNITY TEST SOURCE: NOT RUN in GitHub .NET surrogate CI.
// Only import into an authorized fresh Unity 6000.3.14f1 scratch with the PlayMode test framework.
using System;
using System.Collections;
using System.Reflection;
using B01.Performance.DebrisSleep001.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.Performance.DebrisSleep001.NativeTests
{
    public sealed class DebrisSleepR3NativePlayModeTests
    {
        private const int QuietSteps = 8;

        private static void ConfigureInactive(B0150OptionalDebrisSleepGovernor governor)
        {
            // Private configuration belongs ONLY to this isolated B01-50 source.
            FieldInfo consent = typeof(B0150OptionalDebrisSleepGovernor).GetField(
                "allowCosmeticSleep", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo window = typeof(B0150OptionalDebrisSleepGovernor).GetField(
                "requiredQuietFixedSteps", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(consent);
            Assert.NotNull(window);
            consent.SetValue(governor, true);
            window.SetValue(governor, QuietSteps);
        }

        private static GameObject MakeDebris(bool optIn, out Rigidbody body,
            out B0150OptionalDebrisSleepGovernor governor)
        {
            var go = new GameObject("B0150_W25_disposable_debris");
            go.SetActive(false); // configure BEFORE actual native MonoBehaviour.Awake
            go.transform.position = new Vector3(0f, 1.5f, 0f);
            go.AddComponent<SphereCollider>();
            body = go.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.useGravity = false;
            body.sleepThreshold = 0f; // disable native auto-sleep in request-count control
            governor = go.AddComponent<B0150OptionalDebrisSleepGovernor>();
            if (optIn) ConfigureInactive(governor);
            go.SetActive(true);
            return go;
        }

        [UnityTest]
        public IEnumerator DefaultOffCannotSleepCosmeticBody()
        {
            Rigidbody b;
            B0150OptionalDebrisSleepGovernor g;
            GameObject go = MakeDebris(false, out b, out g);
            try
            {
                for (int i = 0; i < 20; ++i) yield return new WaitForFixedUpdate();
                Assert.IsFalse(g.enabled, "No explicit consent must leave governor disabled");
                Assert.AreEqual(0, g.SleepRequests);
            }
            finally { UnityEngine.Object.Destroy(go); }
        }

        [UnityTest]
        public IEnumerator UnconstrainedOptedInFreeBodyMayRequestAfterEightFixedTicks()
        {
            Rigidbody b;
            B0150OptionalDebrisSleepGovernor g;
            GameObject go = MakeDebris(true, out b, out g);
            try
            {
                for (int i = 0; i < QuietSteps - 2; ++i)
                    yield return new WaitForFixedUpdate();
                Assert.AreEqual(0, g.SleepRequests, "Premature native Sleep requested");
                for (int i = 0; i < 8; ++i) yield return new WaitForFixedUpdate();
                // If native auto-sleep / scheduling invalidates this assertion, G1 fails:
                // source-only test success MUST NOT override native evidence.
                Assert.AreEqual(1, g.SleepRequests,
                    "Opted-in free body failed to request exactly one Sleep");
            }
            finally { UnityEngine.Object.Destroy(go); }
        }

        [UnityTest]
        public IEnumerator OngoingPhysicalContactStayPreventsForcedSleepRequest()
        {
            Rigidbody body;
            B0150OptionalDebrisSleepGovernor governor;
            GameObject debris = MakeDebris(true, out body, out governor);
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                ground.name = "B0150_W25_contact_stay_ground";
                ground.transform.position = new Vector3(0f, -0.5f, 0f);
                ground.transform.localScale = new Vector3(10f, 1f, 10f);
                // A stationary KINEMATIC body remains a body: R3 must guard it.
                var platform = ground.AddComponent<Rigidbody>();
                platform.isKinematic = true;
                debris.transform.position = new Vector3(0f, 1.5f, 0f);
                body.useGravity = true;
                body.constraints = RigidbodyConstraints.FreezePositionX |
                    RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
                var witness = debris.AddComponent<DebrisStayWitness>();
                for (int i = 0; i < 150; ++i)
                    yield return new WaitForFixedUpdate();
                Assert.GreaterOrEqual(witness.dynamicStays, QuietSteps,
                    "INCONCLUSIVE: fewer than eight REAL body-on-body OnCollisionStay callbacks observed");
                Assert.AreEqual(0, governor.SleepRequests,
                    "Native sustained contact allowed a forced Sleep request");
            }
            finally
            {
                UnityEngine.Object.Destroy(debris);
                UnityEngine.Object.Destroy(ground);
            }
        }

        [UnityTest]
        public IEnumerator RestingOnStaticColliderDoesNotPermanentlyBlockSleep()
        {
            Rigidbody body;
            B0150OptionalDebrisSleepGovernor governor;
            GameObject debris = MakeDebris(true, out body, out governor);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                floor.name = "B0150_W25_static_floor_no_Rigidbody";
                floor.transform.position = new Vector3(0f, -0.5f, 0f);
                floor.transform.localScale = new Vector3(10f, 1f, 10f);
                Assert.IsNull(floor.GetComponent<Rigidbody>());
                debris.transform.position = new Vector3(0f, 1.5f, 0f);
                body.useGravity = true;
                body.constraints = RigidbodyConstraints.FreezePositionX |
                    RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
                var witness = debris.AddComponent<DebrisStayWitness>();
                for (int i = 0; i < 150; ++i) yield return new WaitForFixedUpdate();
                Assert.GreaterOrEqual(witness.staticStays, 4,
                    "INCONCLUSIVE: no sustained native static collision was observed");
                Assert.GreaterOrEqual(governor.SleepRequests, 1,
                    "R3 never requested sleep on physically settled STATIC ground");
            }
            finally
            {
                UnityEngine.Object.Destroy(debris);
                UnityEngine.Object.Destroy(floor);
            }
        }

        [UnityTest]
        public IEnumerator JointedDebrisIsNeverEligible()
        {
            var go = new GameObject("B0150_W25_jointed_negative");
            go.SetActive(false);
            try
            {
                go.AddComponent<SphereCollider>();
                var rb = go.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.sleepThreshold = 0f;
                go.AddComponent<FixedJoint>();
                var g = go.AddComponent<B0150OptionalDebrisSleepGovernor>();
                ConfigureInactive(g);
                go.SetActive(true);
                for (int i = 0; i < 25; ++i) yield return new WaitForFixedUpdate();
                Assert.IsFalse(g.enabled, "Jointed Rigidbody must fail closed");
                Assert.AreEqual(0, g.SleepRequests);
            }
            finally { UnityEngine.Object.Destroy(go); }
        }
    }
}

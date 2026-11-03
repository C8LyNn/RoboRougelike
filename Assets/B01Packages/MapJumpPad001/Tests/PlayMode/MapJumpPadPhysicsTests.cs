using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace B01.MapJumpPad001.Tests
{
    public sealed class MapJumpPadPhysicsTests
    {
        private GameObject pad, robot;
        [TearDown] public void Cleanup()
        {
            if (robot) Object.DestroyImmediate(robot);
            if (pad) Object.DestroyImmediate(pad);
        }
        private MapJumpPad Create(float cooldown)
        {
            pad = new GameObject("Grid1x1_JumpPad");
            BoxCollider trigger = pad.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1f, 1f, 1f);
            var jump = pad.AddComponent<MapJumpPad>();
            jump.Configure(10f, 0f, cooldown);
            robot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            robot.name = "PhysicsRobot";
            robot.transform.position = new Vector3(0f, 1.4f, 0f);
            Rigidbody body = robot.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.mass = 10f;
            return jump;
        }
        [UnityTest]
        public IEnumerator FallingRobotGetsRealUpwardVelocity()
        {
            Create(1f);
            Rigidbody body = robot.GetComponent<Rigidbody>();
            float best = float.NegativeInfinity;
            for (int i = 0; i < 120; i++)
            {
                yield return new WaitForFixedUpdate();
                best = Mathf.Max(best, body.linearVelocity.y);
            }
            Assert.Greater(best, 5f, "Actual trigger must launch heavy Rigidbody");
        }
        [UnityTest]
        public IEnumerator DisabledPadNeverLaunches()
        {
            MapJumpPad jump = Create(1f);
            jump.SetActive(false);
            Rigidbody body = robot.GetComponent<Rigidbody>();
            float best = float.NegativeInfinity;
            for (int i = 0; i < 90; i++)
            {
                yield return new WaitForFixedUpdate();
                best = Mathf.Max(best, body.linearVelocity.y);
            }
            Assert.LessOrEqual(best, 0.1f);
        }
        [UnityTest]
        public IEnumerator MissingExitForDisabledPartColliderDoesNotBlockNextRealBodyContact()
        {
            // Deliberately simulate the lost OnTriggerExit of a destructible vehicle part.
            // This is callback-level fault injection, NOT the full R3 vehicle acceptance.
            var jump = Create(0f);
            Rigidbody body = robot.GetComponent<Rigidbody>();
            body.useGravity = false;
            Collider oldWheel = robot.GetComponent<Collider>();
            Assert.AreSame(body, oldWheel.attachedRigidbody);

            jump.gameObject.SendMessage("OnTriggerEnter", oldWheel,
                SendMessageOptions.RequireReceiver);
            yield return new WaitForFixedUpdate();
            Assert.Greater(body.linearVelocity.y, 5f, "Original contact must launch.");

            body.linearVelocity = Vector3.zero;
            oldWheel.enabled = false; // no matching OnTriggerExit is sent
            var replacement = new GameObject("ReplacementWheelCollider");
            replacement.transform.SetParent(robot.transform, false);
            BoxCollider newWheel = replacement.AddComponent<BoxCollider>();
            Assert.AreSame(body, newWheel.attachedRigidbody);

            jump.gameObject.SendMessage("OnTriggerEnter", newWheel,
                SendMessageOptions.RequireReceiver);
            yield return new WaitForFixedUpdate();
            Assert.Greater(body.linearVelocity.y, 5f,
                "Stale disabled collider must not suppress the replacement entry.");
        }

        [UnityTest]
        public IEnumerator DestroyedRigidbodyNoLongerOccupiesBothCachesWhenAnotherBodyEnters()
        {
            // Real Unity object lifetime, while the missing-Exit edge itself
            // is exercised separately by W33's callback fault injection test.
            var jump = Create(0f);
            var firstBody = robot.GetComponent<Rigidbody>();
            firstBody.useGravity = false;
            jump.gameObject.SendMessage("OnTriggerEnter", robot.GetComponent<Collider>(),
                SendMessageOptions.RequireReceiver);
            yield return new WaitForFixedUpdate();
            Assert.Greater(firstBody.linearVelocity.y, 5f);

            Object.Destroy(robot);
            robot = null;
            yield return null; // allow Unity to complete Destroy and fake-null transition
            Assert.That(firstBody == null, Is.True, "Original Rigidbody must be destroyed.");

            robot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            robot.transform.position = new Vector3(0f, 1.4f, 0f);
            var secondBody = robot.AddComponent<Rigidbody>();
            secondBody.useGravity = false;
            jump.gameObject.SendMessage("OnTriggerEnter", robot.GetComponent<Collider>(),
                SendMessageOptions.RequireReceiver);
            yield return new WaitForFixedUpdate();
            Assert.Greater(secondBody.linearVelocity.y, 5f);

            var flags = System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic;
            var contacts = (System.Collections.IDictionary)typeof(MapJumpPad)
                .GetField("contacts", flags).GetValue(jump);
            var launches = (System.Collections.IDictionary)typeof(MapJumpPad)
                .GetField("lastLaunch", flags).GetValue(jump);
            Assert.That(contacts.Count, Is.EqualTo(1), "Destroyed body retained in contacts.");
            Assert.That(launches.Count, Is.EqualTo(1), "Destroyed body retained in lastLaunch.");
            Assert.That(contacts.Contains(secondBody), Is.True);
            Assert.That(launches.Contains(secondBody), Is.True);
        }

        [UnityTest]
        public IEnumerator MultiColliderRobotDoesNotReceiveDoubleLaunch()
        {
            Create(5f);
            var second = new GameObject("SecondWheelCollider");
            second.transform.SetParent(robot.transform, false);
            second.transform.localPosition = new Vector3(0.25f, 0f, 0f);
            second.AddComponent<BoxCollider>();
            Rigidbody body = robot.GetComponent<Rigidbody>();
            float best = float.NegativeInfinity;
            for (int i = 0; i < 100; i++)
            {
                yield return new WaitForFixedUpdate();
                best = Mathf.Max(best, body.linearVelocity.y);
            }
            Assert.Greater(best, 5f);
            Assert.Less(best, 16f, "Repeated wheel trigger entries must not stack impulses");
        }
    }
}

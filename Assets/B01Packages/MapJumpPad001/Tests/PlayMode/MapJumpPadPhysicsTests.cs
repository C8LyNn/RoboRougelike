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

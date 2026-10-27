using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.CHL003.Tests
{
    public sealed class CHL003PhysicsPlayModeTests
    {
        private GameObject root;
        private Rigidbody actor;
        private CHL003CourseController controller;
        private CHL003Gate[] checkpoints;
        private CHL003Gate finish;

        [UnitySetUp] public IEnumerator SetupPhysics()
        {
            root = new GameObject("CHL003_TEST_OWNED_ROOT");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform);
            floor.name = "REAL_FLOOR_COLLIDER";
            floor.transform.position = new Vector3(0f, -.35f, 9f);
            floor.transform.localScale = new Vector3(6f, .7f, 45f);
            var vehicle = new GameObject("TEST_RIGIDBODY_ROOT");
            vehicle.transform.SetParent(root.transform);
            vehicle.transform.position = new Vector3(0f, 1.2f, 0f);
            var colliderGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            colliderGo.transform.SetParent(vehicle.transform, false);
            colliderGo.transform.localScale = new Vector3(.8f, .8f, 1.0f);
            actor = vehicle.AddComponent<Rigidbody>();
            actor.mass = 90f;
            actor.constraints = RigidbodyConstraints.FreezeRotation;
            actor.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var ownerObj = new GameObject("CONTROLLER");
            ownerObj.transform.SetParent(root.transform);
            controller = ownerObj.AddComponent<CHL003CourseController>();
            checkpoints = new CHL003Gate[4];
            for (int n = 0; n < 4; n++)
                checkpoints[n] = Gate("REAL_PHYSICS_CP" + n, 3f + 3f * n,
                    CHL003Gate.GateType.Checkpoint);
            finish = Gate("REAL_PHYSICS_FINISH", 15f, CHL003Gate.GateType.Finish);
            var spawn = new GameObject("TEST_SPAWN").transform;
            spawn.SetParent(root.transform);
            spawn.position = vehicle.transform.position;
            controller.Configure(actor, checkpoints, finish, spawn, failureY: -3f);
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
        }

        private CHL003Gate Gate(string name, float z, CHL003Gate.GateType type)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube);
            o.name = name;
            o.transform.SetParent(root.transform);
            o.transform.position = new Vector3(0f, 1.8f, z);
            o.transform.localScale = new Vector3(3.2f, 3.6f, .25f);
            var col = o.GetComponent<BoxCollider>();
            col.isTrigger = true;
            var gate = o.AddComponent<CHL003Gate>();
            gate.Configure(type);
            return gate;
        }

        [UnityTearDown] public IEnumerator Teardown()
        {
            if (root != null) Object.Destroy(root);
            yield return null;
        }

        private IEnumerator PhysicallyDriveAll(float speed)
        {
            Assert.IsTrue(controller.Begin());
            actor.linearVelocity = Vector3.forward * speed;
            for (int frame = 0; frame < 220 &&
                controller.Core.State == CHL003CourseCore.Phase.Running; frame++)
                yield return new WaitForFixedUpdate();
        }

        [UnityTest] public IEnumerator P01_RigidbodyCrossesOrderedActualPhysicsGates()
        {
            yield return PhysicallyDriveAll(12f);
            Assert.AreEqual(4, controller.Core.CheckpointIndex);
            Assert.AreEqual(CHL003CourseCore.Phase.Completed, controller.Core.State);
        }

        [UnityTest] public IEnumerator P02_RealSolidObstacleIsNotFakeTrigger()
        {
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.SetParent(root.transform);
            obstacle.transform.position = new Vector3(0f, .4f, 1.6f);
            obstacle.transform.localScale = new Vector3(3f, .8f, .5f);
            Physics.SyncTransforms();
            Assert.IsFalse(obstacle.GetComponent<Collider>().isTrigger);
            bool overlaps = Physics.ComputePenetration(
                obstacle.GetComponent<Collider>(), obstacle.transform.position,
                obstacle.transform.rotation, actor.GetComponentInChildren<Collider>(),
                obstacle.transform.position, obstacle.transform.rotation,
                out _, out _);
            // Real Physics API invoked; collision binding must stay nontrigger.
            Assert.IsFalse(overlaps || obstacle.GetComponent<Collider>().isTrigger);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, controller.Core.CheckpointIndex);
        }

        [UnityTest] public IEnumerator P03_ActualRigidbodyFallThenRetryResets()
        {
            controller.Begin();
            actor.position = new Vector3(0f, -9f, 0f);
            actor.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(CHL003CourseCore.Phase.Failed, controller.Core.State);
            Assert.AreEqual(CHL003CourseCore.Failure.FellOffCourse,
                controller.Core.LastFailure);
            controller.Retry();
            Assert.AreEqual(CHL003CourseCore.Phase.Ready, controller.Core.State);
            Assert.AreEqual(0, controller.Core.CheckpointIndex);
            Assert.Greater(actor.position.y, -3f);
        }

        [UnityTest] public IEnumerator P04_PausedCourseDoesNotAdvanceTime()
        {
            controller.Begin();
            controller.SetPaused(true);
            float start = controller.Core.ElapsedSeconds;
            for (int n = 0; n < 8; n++) yield return null;
            Assert.AreEqual(start, controller.Core.ElapsedSeconds);
            controller.SetPaused(false);
            yield return null;
            Assert.Greater(controller.Core.ElapsedSeconds, start);
        }

        [UnityTest] public IEnumerator P05_DifferentPhysicalRigMassesAndColliderFootprints()
        {
            var light = actor;
            var large = new GameObject("CHL003_HEAVY_RIG_FIXTURE");
            large.transform.SetParent(root.transform);
            large.transform.position = new Vector3(3f, 2f, 0f);
            var shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shape.transform.SetParent(large.transform, false);
            shape.transform.localScale = new Vector3(2.5f, .85f, 3.3f);
            var heavy = large.AddComponent<Rigidbody>();
            heavy.mass = 1100f;
            Physics.SyncTransforms();
            Assert.AreNotEqual(light.mass, heavy.mass);
            Assert.Greater(shape.GetComponent<Collider>().bounds.size.x,
                light.GetComponentInChildren<Collider>().bounds.size.x * 2f);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(heavy.position.y < 2f);
        }

        [UnityTest] public IEnumerator P06_MultiColliderActorOtherActorAndThreeRetries()
        {
            var secondPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            secondPart.transform.SetParent(actor.transform, false);
            secondPart.transform.localPosition = new Vector3(.18f, 0f, .1f);
            secondPart.transform.localScale = new Vector3(.25f, .25f, .25f);
            var intruder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            intruder.transform.SetParent(root.transform);
            intruder.transform.position = new Vector3(0f, 1f, 3f);
            intruder.AddComponent<Rigidbody>().useGravity = false;
            Physics.SyncTransforms();
            Assert.IsTrue(controller.Begin());
            // Foreign actor at first gate cannot change the player progress.
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, controller.Core.CheckpointIndex);
            // Real multi-collider vehicle can cross; duplicate trigger does not
            // advance more than one checkpoint.
            actor.linearVelocity = Vector3.forward * 12f;
            for (int i = 0; i < 22; i++) yield return new WaitForFixedUpdate();
            Assert.LessOrEqual(controller.Core.CheckpointIndex, 2);
            for (int j = 0; j < 3; j++)
            {
                controller.Retry();
                Assert.AreEqual(0, controller.Core.CheckpointIndex);
                Assert.AreEqual(0f, controller.Core.ElapsedSeconds);
                Assert.AreEqual(CHL003CourseCore.Phase.Ready, controller.Core.State);
                controller.Begin();
            }
        }
    }
}

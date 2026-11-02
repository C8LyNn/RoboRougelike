using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.EnemyCharge001.Tests
{
    public sealed class EnemyChargePhysicsTests
    {
        GameObject enemy, target, wall, ground;
        EnemyChargeMotor Make(bool obstacle)
        {
            enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy.name = "ChargeActor";
            var body = enemy.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var motor = enemy.AddComponent<EnemyChargeMotor>();
            motor.Configure(.08f, 1f, 1.8f, 65f, 12f, 20f, 14f);

            target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "AssignedTarget";
            target.transform.position = new Vector3(4f, 0f, 0f);
            target.AddComponent<ChargeDemoHealth>();
            motor.AssignTarget(target.transform);

            if (obstacle)
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "SolidBlocker";
                wall.transform.position = new Vector3(2f, 0f, 0f);
                wall.transform.localScale = new Vector3(.5f, 3f, 3f);
            }
            return motor;
        }

        [TearDown]
        public void Cleanup()
        {
            if (enemy) Object.DestroyImmediate(enemy);
            if (target) Object.DestroyImmediate(target);
            if (wall) Object.DestroyImmediate(wall);
            if (ground) Object.DestroyImmediate(ground);
        }

        [UnityTest]
        public IEnumerator FreeLane_OneRealPhysicsHit()
        {
            var motor = Make(false);
            int ticks = 0;
            while (ticks++ < 150 && target.GetComponent<ChargeDemoHealth>().Hits == 0)
                yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, target.GetComponent<ChargeDemoHealth>().Hits);
            Assert.IsTrue(motor.HasDealtDamage);
            Assert.AreNotEqual(EnemyChargeMotor.Phase.Charging, motor.CurrentPhase);
        }

        [UnityTest]
        public IEnumerator SolidBlockerWithHealth_IsNeverDamagedOrPassedThrough()
        {
            Make(true);
            var blockerHealth = wall.AddComponent<ChargeDemoHealth>();
            for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, blockerHealth.Hits, "An unassigned obstacle must never receive charge damage");
            Assert.AreEqual(0, target.GetComponent<ChargeDemoHealth>().Hits);
            Assert.Less(enemy.transform.position.x, target.transform.position.x);
        }

        [UnityTest]
        public IEnumerator CancelTelegraph_NoDamage()
        {
            var motor = Make(false);
            yield return new WaitForFixedUpdate();
            motor.SetArmed(false);
            for (int i = 0; i < 90; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, target.GetComponent<ChargeDemoHealth>().Hits);
        }

        [UnityTest]
        public IEnumerator FriendlyAssignedTarget_CannotBeAcquiredOrDamaged()
        {
            var motor = Make(false);
            target.GetComponent<ChargeDemoHealth>().SetTeamId(motor.TeamId);
            for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, target.GetComponent<ChargeDemoHealth>().Hits);
            Assert.AreEqual(EnemyChargeMotor.Phase.Seeking, motor.CurrentPhase);
        }

        [UnityTest]
        public IEnumerator MovingKinematicBlocker_StopsChargeWithoutDamagingProp()
        {
            var motor = Make(false);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "MovingBlocker";
            wall.transform.position = new Vector3(2f, 0f, 3f);
            wall.transform.localScale = new Vector3(.65f, 2f, 3f);
            var body = wall.AddComponent<Rigidbody>();
            body.isKinematic = true;
            var propHealth = wall.AddComponent<ChargeDemoHealth>();

            int ticks = 0;
            while (ticks++ < 40 && motor.CurrentPhase != EnemyChargeMotor.Phase.Charging)
                yield return new WaitForFixedUpdate();
            Assert.AreEqual(EnemyChargeMotor.Phase.Charging, motor.CurrentPhase);
            body.MovePosition(new Vector3(2f, 0f, 0f));
            for (int i = 0; i < 85; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, propHealth.Hits, "A moving obstacle is not the assigned target");
            Assert.AreEqual(0, target.GetComponent<ChargeDemoHealth>().Hits);
        }

        [UnityTest]
        public IEnumerator FloorSupportContact_DoesNotAbortCharge()
        {
            Make(false);
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "SolidTestFloor";
            ground.transform.position = new Vector3(0f, -.65f, 0f);
            ground.transform.localScale = new Vector3(14f, .3f, 8f);
            enemy.GetComponent<Rigidbody>().useGravity = true;

            int ticks = 0;
            while (ticks++ < 150 && target.GetComponent<ChargeDemoHealth>().Hits == 0)
                yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, target.GetComponent<ChargeDemoHealth>().Hits,
                "Support contact must not cancel a horizontal physics charge");
        }

        [UnityTest]
        public IEnumerator SecondAlliedRobot_InterruptsWithoutFriendlyFire()
        {
            var motor = Make(false);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "AlliedChargeActor";
            wall.transform.position = new Vector3(2f, 0f, 0f);
            var body = wall.AddComponent<Rigidbody>();
            body.isKinematic = true;
            var ally = wall.AddComponent<EnemyChargeMotor>();
            ally.SetArmed(false);
            var allyHealth = wall.AddComponent<ChargeDemoHealth>();
            allyHealth.SetTeamId(motor.TeamId);

            for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, allyHealth.Hits);
            Assert.AreEqual(0, target.GetComponent<ChargeDemoHealth>().Hits);
        }
    }
}

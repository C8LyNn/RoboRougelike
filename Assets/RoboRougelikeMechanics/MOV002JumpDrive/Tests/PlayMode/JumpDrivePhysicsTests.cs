using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Mechanics.MOV002JumpDrive.Tests
{
    public sealed class JumpDrivePhysicsTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        private GameObject CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "MOV002_TestGround";
            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(100f, 0.5f, 100f);
            spawned.Add(ground);
            Physics.SyncTransforms();
            return ground;
        }

        private JumpDrive CreateVehicle(float mass = 100f, float y = 0.65f, float x = 0f)
        {
            var root = new GameObject("MOV002_TestVehicle");
            root.transform.position = new Vector3(x, y, 0f);
            root.AddComponent<BoxCollider>();
            var body = root.AddComponent<Rigidbody>();
            body.mass = mass;
            body.useGravity = true;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            var module = new GameObject("JumpDriveModule");
            module.transform.SetParent(root.transform, false);
            var drive = module.AddComponent<JumpDrive>();
            drive.BindToVehicle(body);
            drive.Configure(new JumpDriveSettings
            {
                TargetJumpHeightMeters = 2f,
                SupportedMassKg = 100f,
                MaxUpwardSpeedMps = 8f,
                CooldownSeconds = 5f,
                EnergyCost = 0f,
                GroundProbeDistanceMeters = 2.5f,
                MinimumGroundNormalDot = 0.5f,
                GroundLayers = ~0,
                RequireGroundContact = true,
                RequireModuleUnderVehicleTransform = true
            });
            spawned.Add(root);
            Physics.SyncTransforms();
            return drive;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (GameObject item in spawned)
                if (item != null) Object.Destroy(item);
            spawned.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator GroundedJumpAddsUpwardVelocityWithoutChangingLateralVelocity()
        {
            CreateGround();
            var drive = CreateVehicle();
            drive.VehicleBody.linearVelocity = new Vector3(3f, 0f, 2f);
            Assert.AreEqual(JumpDriveResult.Activated, drive.TryActivate());
            Vector3 velocity = drive.VehicleBody.linearVelocity;
            Assert.Greater(velocity.y, 5f);
            Assert.AreEqual(3f, velocity.x, 0.0001f);
            Assert.AreEqual(2f, velocity.z, 0.0001f);
            yield return new WaitForFixedUpdate();
            Assert.Greater(drive.VehicleBody.transform.position.y, 0.65f);
        }

        [UnityTest]
        public IEnumerator HeavierVehicleReceivesSmallerJump()
        {
            CreateGround();
            var light = CreateVehicle(100f, 0.65f, -10f);
            var heavy = CreateVehicle(200f, 0.65f, 10f);
            Assert.AreEqual(JumpDriveResult.Activated, light.TryActivate());
            Assert.AreEqual(JumpDriveResult.Activated, heavy.TryActivate());
            Assert.AreEqual(light.VehicleBody.linearVelocity.y * 0.5f,
                heavy.VehicleBody.linearVelocity.y, 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AirborneVehicleCannotJumpAgain()
        {
            CreateGround();
            var drive = CreateVehicle(100f, 10f);
            Assert.AreEqual(JumpDriveResult.MissingGroundSupport, drive.TryActivate());
            Assert.AreEqual(0f, drive.CooldownRemaining);
            yield return null;
        }

        [UnityTest]
        public IEnumerator KinematicBodyIsRejected()
        {
            CreateGround();
            var drive = CreateVehicle();
            drive.VehicleBody.isKinematic = true;
            Assert.AreEqual(JumpDriveResult.InvalidRigidbody, drive.TryActivate());
            yield return null;
        }

        [UnityTest]
        public IEnumerator DetachedModuleCannotTriggerOriginalVehicle()
        {
            CreateGround();
            var drive = CreateVehicle();
            drive.transform.SetParent(null);
            spawned.Add(drive.gameObject);
            Assert.AreEqual(JumpDriveResult.DetachedFromVehicle, drive.TryActivate());
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyedOrDisabledModuleDoesNotJump()
        {
            CreateGround();
            var drive = CreateVehicle();
            drive.SetOperational(false);
            Assert.AreEqual(JumpDriveResult.NotOperational, drive.TryActivate());
            drive.SetOperational(true);
            drive.enabled = false;
            Assert.AreEqual(JumpDriveResult.NotOperational, drive.TryActivate());
            yield return null;
        }

        [UnityTest]
        public IEnumerator FailedEnergyCheckDoesNotSpendOrStartCooldown()
        {
            CreateGround();
            var drive = CreateVehicle();
            drive.Settings.EnergyCost = 20f;
            var pool = new JumpDriveEnergyPool(100f, 10f);
            drive.SetEnergySource(pool);
            Assert.AreEqual(JumpDriveResult.InsufficientEnergy, drive.TryActivate());
            Assert.AreEqual(10f, pool.Current);
            Assert.AreEqual(0f, drive.CooldownRemaining);
            pool.Regenerate(20f);
            Assert.AreEqual(JumpDriveResult.Activated, drive.TryActivate());
            Assert.AreEqual(10f, pool.Current);
            Assert.AreEqual(JumpDriveResult.CoolingDown, drive.TryActivate());
            Assert.AreEqual(10f, pool.Current);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NoEnergySourceFailsOnlyWhenCostPositive()
        {
            CreateGround();
            var drive = CreateVehicle();
            drive.Settings.EnergyCost = 10f;
            Assert.AreEqual(JumpDriveResult.MissingEnergySource, drive.TryActivate());
            drive.Settings.EnergyCost = 0f;
            Assert.AreEqual(JumpDriveResult.Activated, drive.TryActivate());
            yield return null;
        }

        [UnityTest]
        public IEnumerator RotatedModuleStillJumpsAgainstGravity()
        {
            CreateGround();
            var drive = CreateVehicle();
            drive.transform.localRotation = Quaternion.Euler(90f, 45f, 180f);
            Assert.AreEqual(JumpDriveResult.Activated, drive.TryActivate());
            Assert.Greater(drive.VehicleBody.linearVelocity.y, 0f);
            Assert.AreEqual(0f, drive.VehicleBody.linearVelocity.x, 0.0001f);
            Assert.AreEqual(0f, drive.VehicleBody.linearVelocity.z, 0.0001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TwoDrivesInSameFrameRespectSharedUpwardVelocityCap()
        {
            CreateGround();
            var first = CreateVehicle();
            first.Settings.MaxUpwardSpeedMps = 7f;
            var otherModule = new GameObject("SecondDrive");
            otherModule.transform.SetParent(first.VehicleBody.transform, false);
            var second = otherModule.AddComponent<JumpDrive>();
            second.BindToVehicle(first.VehicleBody);
            second.Configure(new JumpDriveSettings
            {
                TargetJumpHeightMeters = 2f,
                SupportedMassKg = 100f,
                MaxUpwardSpeedMps = 7f,
                RequireGroundContact = true,
                GroundProbeDistanceMeters = 2.5f
            });
            Assert.AreEqual(JumpDriveResult.Activated, first.TryActivate());
            Assert.AreEqual(JumpDriveResult.Activated, second.TryActivate());
            Assert.AreEqual(7f, first.VehicleBody.linearVelocity.y, 0.0001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AirborneFailureDoesNotConsumeEnergy()
        {
            CreateGround();
            var drive = CreateVehicle(100f, 10f);
            drive.Settings.EnergyCost = 25f;
            var pool = new JumpDriveEnergyPool(100f, 40f);
            drive.SetEnergySource(pool);
            Assert.AreEqual(JumpDriveResult.MissingGroundSupport, drive.TryActivate());
            Assert.AreEqual(40f, pool.Current);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RejectedInvalidJumpHeightDoesNotConsumeEnergy()
        {
            CreateGround();
            var drive = CreateVehicle();
            drive.Settings.EnergyCost = 25f;
            drive.Settings.TargetJumpHeightMeters = 0f;
            var pool = new JumpDriveEnergyPool(100f, 40f);
            drive.SetEnergySource(pool);
            Assert.AreEqual(JumpDriveResult.InvalidGravityOrConfiguration, drive.TryActivate());
            Assert.AreEqual(40f, pool.Current);
            yield return null;
        }
    }
}

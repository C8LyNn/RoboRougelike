using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Mechanics.MOV003Blink.Tests
{
    public sealed class BlinkPhysicsTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        private GameObject Ground(float size = 100f)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "MOV003_Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(size, 1f, size);
            spawned.Add(ground);
            Physics.SyncTransforms();
            return ground;
        }

        private GameObject Obstacle(Vector3 position, Vector3 scale)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "MOV003_Obstacle";
            obstacle.transform.position = position;
            obstacle.transform.localScale = scale;
            spawned.Add(obstacle);
            Physics.SyncTransforms();
            return obstacle;
        }

        private BlinkDrive Vehicle(float x = 0f, float z = 0f, float mass = 100f)
        {
            GameObject root = new GameObject("MOV003_Vehicle");
            root.transform.position = new Vector3(x, 0.5f, z);
            root.AddComponent<BoxCollider>();
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = mass;
            body.useGravity = true;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            GameObject mount = new GameObject("MOV003_Module");
            mount.transform.SetParent(root.transform, false);
            BlinkDrive blink = mount.AddComponent<BlinkDrive>();
            blink.BindToVehicle(body);
            blink.Configure(new BlinkSettings
            {
                DistanceMeters = 8f,
                CooldownSeconds = 20f,
                EnergyCost = 0f,
                RequireClearPath = true,
                RequireGroundSupport = true,
                BlockingLayers = ~0,
                SupportLayers = ~0,
                MaximumDropMeters = 0.65f,
                AllowedRiseMeters = 0.08f,
                HullPaddingMeters = 0.02f
            });
            spawned.Add(root);
            Physics.SyncTransforms();
            return blink;
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
        public IEnumerator TeleportsWholeRigidbodyAndPreservesVelocity()
        {
            Ground();
            BlinkDrive blink = Vehicle();
            blink.VehicleBody.linearVelocity = new Vector3(1.5f, 0f, 3f);
            blink.VehicleBody.angularVelocity = new Vector3(0f, 0.8f, 0f);
            Assert.AreEqual(BlinkResult.Activated, blink.TryActivate());
            Assert.AreEqual(8f, blink.VehicleBody.position.z, 0.001f);
            Assert.AreEqual(8f, blink.transform.position.z, 0.001f);
            Assert.AreEqual(3f, blink.VehicleBody.linearVelocity.z, 0.001f);
            Assert.AreEqual(0.8f, blink.VehicleBody.angularVelocity.y, 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WallOnPathBlocksWithoutConsumingEnergyOrCooldown()
        {
            Ground();
            Obstacle(new Vector3(0f, 0.5f, 4f), new Vector3(2f, 2f, 1f));
            BlinkDrive blink = Vehicle();
            blink.Settings.EnergyCost = 20f;
            var pool = new BlinkEnergyPool(30f, 30f);
            blink.SetEnergySource(pool);
            Assert.AreEqual(BlinkResult.PathBlocked, blink.TryActivate());
            Assert.AreEqual(30f, pool.Current);
            Assert.AreEqual(0f, blink.CooldownRemaining);
            Assert.AreEqual(0f, blink.VehicleBody.position.z, 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestinationObstacleRejected()
        {
            Ground();
            Obstacle(new Vector3(0f, 0.5f, 8f), Vector3.one);
            BlinkDrive blink = Vehicle();
            Assert.AreEqual(BlinkResult.DestinationBlocked, blink.TryActivate());
            Assert.AreEqual(0f, blink.VehicleBody.position.z, 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HullIncludesChildColliderFarFromCore()
        {
            Ground();
            Obstacle(new Vector3(2f, 0.5f, 8f), Vector3.one * 0.5f);
            BlinkDrive blink = Vehicle();
            var extra = new GameObject("WideGunCollider");
            extra.transform.SetParent(blink.VehicleBody.transform, false);
            extra.transform.localPosition = new Vector3(2f, 0f, 0f);
            extra.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            Assert.AreEqual(BlinkResult.DestinationBlocked, blink.TryActivate());
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissingFloorAtDestinationRejected()
        {
            Ground(8f);
            BlinkDrive blink = Vehicle();
            Assert.AreEqual(BlinkResult.UnsupportedDestination, blink.TryActivate());
            yield return null;
        }

        [UnityTest]
        public IEnumerator DetachedBlinkCannotTeleportOriginalBody()
        {
            Ground();
            BlinkDrive blink = Vehicle();
            blink.transform.SetParent(null);
            spawned.Add(blink.gameObject);
            Physics.SyncTransforms();
            Assert.AreEqual(BlinkResult.DetachedFromVehicle, blink.TryActivate());
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledOrDestroyedModuleIsRejected()
        {
            Ground();
            BlinkDrive blink = Vehicle();
            blink.SetOperational(false);
            Assert.AreEqual(BlinkResult.NotOperational, blink.TryActivate());
            blink.SetOperational(true);
            blink.enabled = false;
            Assert.AreEqual(BlinkResult.NotOperational, blink.TryActivate());
            yield return null;
        }

        [UnityTest]
        public IEnumerator NotEnoughEnergyDoesNotStartCooldown()
        {
            Ground();
            BlinkDrive blink = Vehicle();
            blink.Settings.EnergyCost = 12f;
            var pool = new BlinkEnergyPool(100f, 5f);
            blink.SetEnergySource(pool);
            Assert.AreEqual(BlinkResult.InsufficientEnergy, blink.TryActivate());
            Assert.AreEqual(5f, pool.Current);
            Assert.AreEqual(0f, blink.CooldownRemaining);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SuccessfulEnergySpendAndCooldown()
        {
            Ground();
            BlinkDrive blink = Vehicle();
            blink.Settings.EnergyCost = 10f;
            var pool = new BlinkEnergyPool(50f, 50f);
            blink.SetEnergySource(pool);
            Assert.AreEqual(BlinkResult.Activated, blink.TryActivate());
            Assert.AreEqual(40f, pool.Current);
            Assert.AreEqual(BlinkResult.CoolingDown, blink.TryActivate());
            Assert.AreEqual(40f, pool.Current);
            Assert.Greater(blink.CooldownRemaining, 0f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NoEnergyBankRejectedOnlyWhenCostPositive()
        {
            Ground();
            BlinkDrive blink = Vehicle();
            blink.Settings.EnergyCost = 5f;
            Assert.AreEqual(BlinkResult.MissingEnergySource, blink.TryActivate());
            blink.Settings.EnergyCost = 0f;
            Assert.AreEqual(BlinkResult.Activated, blink.TryActivate());
            yield return null;
        }

        [UnityTest]
        public IEnumerator RotatedVehicleUsesItsActualForwardDirection()
        {
            Ground();
            BlinkDrive blink = Vehicle();
            blink.VehicleBody.rotation = Quaternion.Euler(0f, 90f, 0f);
            Physics.SyncTransforms();
            Assert.AreEqual(BlinkResult.Activated, blink.TryActivate());
            Assert.AreEqual(8f, blink.VehicleBody.position.x, 0.005f);
            Assert.AreEqual(0f, blink.VehicleBody.position.z, 0.005f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WallPhasingIsExplicitOptIn()
        {
            Ground();
            Obstacle(new Vector3(0f, 0.5f, 4f), new Vector3(2f, 2f, 1f));
            BlinkDrive blink = Vehicle();
            blink.Settings.RequireClearPath = false;
            Assert.AreEqual(BlinkResult.Activated, blink.TryActivate());
            Assert.AreEqual(8f, blink.VehicleBody.position.z, 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VelocityCanBeResetOnArrival()
        {
            Ground();
            BlinkDrive blink = Vehicle();
            blink.Settings.PreserveVelocity = false;
            blink.VehicleBody.linearVelocity = new Vector3(0f, 0f, 6f);
            Assert.AreEqual(BlinkResult.Activated, blink.TryActivate());
            Assert.AreEqual(0f, blink.VehicleBody.linearVelocity.sqrMagnitude, 0.0001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnotherVehicleAtDestinationIsBlocking()
        {
            Ground();
            BlinkDrive blink = Vehicle();
            Vehicle(0f, 8f);
            Assert.AreEqual(BlinkResult.DestinationBlocked, blink.TryActivate());
            yield return null;
        }

        [UnityTest]
        public IEnumerator KinematicVehicleIsRejected()
        {
            Ground();
            BlinkDrive blink = Vehicle();
            blink.VehicleBody.isKinematic = true;
            Assert.AreEqual(BlinkResult.InvalidRigidbody, blink.TryActivate());
            yield return null;
        }
    
        [UnityTest]
        public IEnumerator ConnectedChildRigidbodyIsRejectedRatherThanLeftBehind()
        {
            Ground();
            BlinkDrive blink = Vehicle();
            GameObject child = new GameObject("ArticulatedSection");
            child.transform.SetParent(blink.VehicleBody.transform, false);
            child.AddComponent<BoxCollider>();
            child.AddComponent<Rigidbody>();
            Physics.SyncTransforms();
            Assert.AreEqual(BlinkResult.MultiRigidbodyAssembly, blink.TryActivate());
            Assert.AreEqual(0f, blink.VehicleBody.position.z, 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InvalidDirectionRejectedWithoutCharging()
        {
            Ground();
            BlinkDrive blink = Vehicle();
            blink.Settings.EnergyCost = 5f;
            var pool = new BlinkEnergyPool(30f, 30f);
            blink.SetEnergySource(pool);
            Assert.AreEqual(BlinkResult.InvalidSettings, blink.TryActivate(Vector3.up));
            Assert.AreEqual(30f, pool.Current);
            Assert.AreEqual(0f, blink.CooldownRemaining);
            yield return null;
        }
    }
}

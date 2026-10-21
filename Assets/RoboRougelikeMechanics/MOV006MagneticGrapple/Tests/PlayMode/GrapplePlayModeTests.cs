using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Mechanics.MOV006MagneticGrapple.Tests
{
    public sealed class GrapplePlayModeTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private GameObject vehicle;
        private Rigidbody body;
        private MagneticGrappleEmitter emitter;
        private MagneticGrappleController grapple;

        [SetUp]
        public void Setup()
        {
            vehicle = NewCube("MOV006_vehicle", Vector3.zero, Vector3.one);
            body = vehicle.AddComponent<Rigidbody>();
            body.mass = 100f;
            body.useGravity = false;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            GameObject part = new GameObject("GrappleModule");
            objects.Add(part);
            part.transform.SetParent(vehicle.transform, false);
            part.transform.localPosition = new Vector3(0f, 0f, 0.8f);
            emitter = part.AddComponent<MagneticGrappleEmitter>();
            grapple = vehicle.AddComponent<MagneticGrappleController>();
            grapple.Settings.maximumAttachmentSeconds = 0f;
            grapple.Settings.maxRange = 25f;
            grapple.Settings.cooldownSeconds = 0.2f;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; --i)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        private GameObject NewCube(string name, Vector3 p, Vector3 scale)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objects.Add(result);
            result.name = name;
            result.transform.position = p;
            result.transform.localScale = scale;
            return result;
        }
        private GameObject Wall(float z = 12f)
        {
            return NewCube("target_wall", new Vector3(0f, 0f, z), new Vector3(3f, 3f, 1f));
        }
        private static IEnumerator Fixed(int count)
        {
            for (int i = 0; i < count; ++i) yield return new WaitForFixedUpdate();
        }
        private bool Fire()
        {
            Physics.SyncTransforms();
            return grapple.TryFire(emitter, Vector3.forward);
        }

        [UnityTest] public IEnumerator StaticWallAcceptsAnchor()
        {
            Wall();
            Assert.IsTrue(Fire());
            Assert.IsTrue(grapple.IsAttached);
            Assert.IsNull(grapple.AttachedRigidbody);
            yield return Fixed(2);
            Assert.IsTrue(grapple.IsAttached);
        }
        [UnityTest] public IEnumerator ReelLengthShrinksWhileAttached()
        {
            Wall();
            Assert.IsTrue(Fire());
            float initial = grapple.CurrentRopeLength;
            yield return Fixed(5);
            Assert.Less(grapple.CurrentRopeLength, initial);
        }
        [UnityTest] public IEnumerator StaticWinchPullsVehicleTowardWall()
        {
            Wall();
            Assert.IsTrue(Fire());
            yield return Fixed(28);
            Assert.Greater(body.position.z, 0.04f);
            Assert.Greater(body.linearVelocity.z, 0.04f);
        }
        [UnityTest] public IEnumerator DynamicWinchPullsBothBodies()
        {
            GameObject target = Wall();
            Rigidbody targetBody = target.AddComponent<Rigidbody>();
            targetBody.mass = 100f;
            targetBody.useGravity = false;
            targetBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Assert.IsTrue(Fire());
            Assert.AreSame(targetBody, grapple.AttachedRigidbody);
            yield return Fixed(28);
            Assert.Greater(body.position.z, 0.01f);
            Assert.Less(targetBody.position.z, 11.99f);
        }
        [UnityTest] public IEnumerator SelfCollidersDoNotBlockFire()
        {
            GameObject ownPiece = NewCube("own_block", new Vector3(0f, 0f, 2f), Vector3.one);
            ownPiece.transform.SetParent(vehicle.transform, true);
            Wall();
            Assert.IsTrue(Fire());
            yield return Fixed(1);
            Assert.IsTrue(grapple.IsAttached);
        }
        [UnityTest] public IEnumerator NearestUnmarkedObstacleBlocksTaggedTarget()
        {
            Wall(6f);
            GameObject far = Wall(12f);
            far.AddComponent<MagneticGrappleSurface>();
            grapple.Settings.requireMagneticSurface = true;
            Assert.IsFalse(Fire());
            Assert.IsFalse(grapple.IsAttached);
            yield return null;
        }
        [UnityTest] public IEnumerator NoColliderMeansNoCostOrCooldown()
        {
            var energy = vehicle.AddComponent<PrototypeGrappleEnergyTank>();
            energy.SetEnergy(50f);
            grapple.SetEnergyProvider(energy);
            grapple.Settings.useEnergy = true;
            Assert.IsFalse(Fire());
            Assert.AreEqual(50f, energy.CurrentEnergy, 0.001f);
            Assert.AreEqual(0f, grapple.CooldownRemaining);
            yield return null;
        }
        [UnityTest] public IEnumerator EnergyOnlySpentAfterValidLatch()
        {
            Wall();
            var energy = vehicle.AddComponent<PrototypeGrappleEnergyTank>();
            energy.regenPerSecond = 0f;
            energy.SetEnergy(10f);
            grapple.SetEnergyProvider(energy);
            grapple.Settings.useEnergy = true;
            Assert.IsFalse(Fire());
            Assert.AreEqual(10f, energy.CurrentEnergy);
            energy.SetEnergy(40f);
            Assert.IsTrue(Fire());
            Assert.AreEqual(20f, energy.CurrentEnergy, 0.001f);
            yield return null;
        }
        [UnityTest] public IEnumerator ReleasingStartsCooldown()
        {
            Wall();
            Assert.IsTrue(Fire());
            grapple.Release();
            Assert.IsFalse(grapple.IsAttached);
            Assert.IsFalse(Fire());
            yield return Fixed(16);
            Assert.IsTrue(Fire());
        }
        [UnityTest] public IEnumerator BreakingEmitterDropsRope()
        {
            Wall();
            Assert.IsTrue(Fire());
            emitter.SetOperational(false);
            yield return Fixed(2);
            Assert.IsFalse(grapple.IsAttached);
        }
        [UnityTest] public IEnumerator DestroyedTargetDropsRope()
        {
            GameObject wall = Wall();
            Assert.IsTrue(Fire());
            Object.Destroy(wall);
            yield return null;
            yield return Fixed(2);
            Assert.IsFalse(grapple.IsAttached);
            Assert.Greater(grapple.CooldownRemaining, 0f);
        }
        [UnityTest] public IEnumerator NewlyInsertedWallBreaksVisibleRope()
        {
            Wall(12f);
            Assert.IsTrue(Fire());
            Wall(6f);
            Physics.SyncTransforms();
            yield return Fixed(2);
            Assert.IsFalse(grapple.IsAttached);
        }
        [UnityTest] public IEnumerator KinematicVehicleCannotFire()
        {
            Wall();
            body.isKinematic = true;
            Assert.IsFalse(Fire());
            yield return null;
        }
        [UnityTest] public IEnumerator ForeignEmitterCannotBeUsed()
        {
            Wall();
            GameObject foreign = new GameObject("foreign_emitter");
            objects.Add(foreign);
            var other = foreign.AddComponent<MagneticGrappleEmitter>();
            Assert.IsFalse(grapple.TryFire(other, Vector3.forward));
            yield return null;
        }
        [UnityTest] public IEnumerator TeamFilterCanPreventGrapple()
        {
            Wall();
            grapple.TargetFilter = c => false;
            Assert.IsFalse(Fire());
            Assert.IsFalse(grapple.IsAttached);
            yield return null;
        }
        [UnityTest] public IEnumerator CannotAttachBeyondRange()
        {
            Wall(40f);
            Assert.IsFalse(Fire());
            yield return null;
        }
        [UnityTest] public IEnumerator DetachedEmitterCannotReattach()
        {
            Wall();
            Assert.IsTrue(Fire());
            emitter.transform.SetParent(null, true);
            yield return Fixed(2);
            Assert.IsFalse(grapple.IsAttached);
            Assert.IsFalse(Fire());
        }
    }
}

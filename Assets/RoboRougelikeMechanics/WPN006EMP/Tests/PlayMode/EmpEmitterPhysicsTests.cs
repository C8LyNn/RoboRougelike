using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.Mechanics.EMP;

namespace RoboRougelike.Mechanics.EMP.Tests
{
    public sealed class EmpEmitterPhysicsTests
    {
        // Actual MonoBehaviour OnDisable dispatch requires PlayMode.
        [Test] public void Receiver_DisableDoesNotLeaveStatusLatched()
        {
            var receiver=Create("receiver lifecycle",Vector3.zero).AddComponent<EmpReceiver>();
            receiver.ApplyEmp(3f);receiver.enabled=false;Assert.False(receiver.IsDisrupted);
        }
        private const int TargetLayer = 8;
        private const int ObstacleLayer = 9;
        private readonly List<GameObject> objects = new List<GameObject>();
        private EmpEmitter emitter;
        private GameObject player;
        private EmpReceiver enemy;

        private GameObject Create(string name, Vector3 position, int layer = 0)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.position = position;
            objects.Add(go);
            return go;
        }

        [SetUp]
        public void Setup()
        {
            player = Create("own-vehicle", Vector3.zero);
            emitter = player.AddComponent<EmpEmitter>();
            emitter.BindVehicle(player.transform, 101, 1);
            emitter.Configure(9f, 3f, 12f);
            emitter.ConfigurePhysics(1 << TargetLayer, 1 << ObstacleLayer, 64, 32);
            enemy = CreateReceiver("enemy-module", 3f);
            Physics.SyncTransforms();
        }

        private EmpReceiver CreateReceiver(string name, float x, int team = 2, int id = 202)
        {
            GameObject obj = Create(name, new Vector3(x, 0f, 0f), TargetLayer);
            obj.AddComponent<BoxCollider>();
            EmpReceiver receiver = obj.AddComponent<EmpReceiver>();
            receiver.ConfigureOwnership(obj.transform, id, team);
            return receiver;
        }

        private void AddObstacle(float x)
        {
            GameObject obstacle = Create("occluding-wall", new Vector3(x, 0f, 0f), ObstacleLayer);
            obstacle.AddComponent<BoxCollider>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject obj in objects)
                if (obj != null) Object.DestroyImmediate(obj);
            objects.Clear();
        }

        [Test]
        public void Pulse_DisruptsOneEnemy()
        {
            Assert.True(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(1, result.ReceiversAffected);
            Assert.True(enemy.IsDisrupted);
            Assert.That(enemy.RemainingSeconds, Is.GreaterThan(0f));
        }

        [Test]
        public void Pulse_ConsumesCooldownAfterSuccess()
        {
            Assert.True(emitter.TryPulse(out _));
            Assert.That(emitter.CooldownRemaining, Is.EqualTo(12f));
            Assert.False(emitter.TryPulse(out EmpPulseReport second));
            Assert.AreEqual(EmpPulseFailure.CoolingDown, second.Failure);
        }

        [Test]
        public void Pulse_StopsBehindWall()
        {
            AddObstacle(1.5f);
            Physics.SyncTransforms();
            Assert.True(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(0, result.ReceiversAffected);
            Assert.AreEqual(1, result.ReceiversOccluded);
            Assert.False(enemy.IsDisrupted);
        }

        [Test]
        public void Pulse_DoesNotHitBeyondRange()
        {
            enemy.transform.position = new Vector3(11f, 0f, 0f);
            Physics.SyncTransforms();
            Assert.True(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(0, result.ReceiversAffected);
        }

        [Test]
        public void Pulse_OnlyOneHitWithTwoCollidersOnModule()
        {
            GameObject child = Create("extra-module-collider", enemy.transform.position + Vector3.up * .1f,
                TargetLayer);
            child.transform.SetParent(enemy.transform, true);
            child.AddComponent<SphereCollider>();
            Physics.SyncTransforms();
            Assert.True(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(1, result.ReceiversAffected);
            Assert.AreEqual(1, result.ReceiversDetected);
        }

        [Test]
        public void Pulse_ExplicitColliderMappingResolvesDisjointHierarchy()
        {
            Object.DestroyImmediate(enemy.GetComponent<BoxCollider>());
            GameObject separate = Create("R6-disjoint-block-collider", new Vector3(3f, 0f, 0f), TargetLayer);
            separate.AddComponent<BoxCollider>();
            separate.AddComponent<EmpTargetCollider>().Bind(enemy);
            Physics.SyncTransforms();
            Assert.True(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(1, result.ReceiversAffected);
            Assert.True(enemy.IsDisrupted);
        }

        [Test]
        public void Pulse_IgnoresOwnVehicleByStableId()
        {
            enemy.ConfigureOwnership(enemy.transform, 101, 2);
            Assert.True(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(0, result.ReceiversAffected);
        }

        [Test]
        public void Pulse_IgnoresAlliesByTeamId()
        {
            enemy.ConfigureOwnership(enemy.transform, 202, 1);
            Assert.True(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(0, result.ReceiversAffected);
        }

        [Test]
        public void Pulse_RespectsFullResistance()
        {
            enemy.ConfigureSusceptibility(1f);
            Assert.True(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(0, result.ReceiversAffected);
        }

        [Test]
        public void Pulse_RespectsPartialResistance()
        {
            enemy.ConfigureSusceptibility(.5f);
            Assert.True(emitter.TryPulse(out _));
            float expected = EmpPulseMath.CalculateDuration(3f, 2.5f, 9f, .5f, .35f);
            Assert.That(enemy.RemainingSeconds, Is.EqualTo(expected).Within(.02f));
        }

        [Test]
        public void Pulse_InsufficientEnergyDoesNotSpendCooldownOrDisrupt()
        {
            var bank = player.AddComponent<EmpEnergyReservoir>();
            bank.Configure(100f, 3f, 0f);
            emitter.Configure(9f, 3f, 12f, 10f);
            emitter.SetEnergySource(bank);
            Assert.False(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(EmpPulseFailure.InsufficientEnergy, result.Failure);
            Assert.AreEqual(0f, emitter.CooldownRemaining);
            Assert.That(bank.Current, Is.EqualTo(3f));
            Assert.False(enemy.IsDisrupted);
        }

        [Test]
        public void Pulse_MissingEnergySourceFailsClosed()
        {
            emitter.Configure(9f, 3f, 12f, 10f);
            Assert.False(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(EmpPulseFailure.MissingEnergySource, result.Failure);
            Assert.False(enemy.IsDisrupted);
        }

        [Test]
        public void Pulse_SpendsEnergyOnceForMultipleTargets()
        {
            var bank = player.AddComponent<EmpEnergyReservoir>();
            bank.Configure(100f, 20f, 0f);
            emitter.Configure(9f, 3f, 12f, 7f);
            emitter.SetEnergySource(bank);
            CreateReceiver("another-module", 4f, id: 203);
            Physics.SyncTransforms();
            Assert.True(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(2, result.ReceiversAffected);
            Assert.That(bank.Current, Is.EqualTo(13f));
        }

        [Test]
        public void Pulse_DisabledEmitterRefuses()
        {
            emitter.SetOperational(false);
            Assert.False(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(EmpPulseFailure.NotOperational, result.Failure);
            Assert.False(enemy.IsDisrupted);
        }

        [Test]
        public void Pulse_DisabledReceiverIsIgnored()
        {
            enemy.enabled = false;
            Assert.True(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(0, result.ReceiversAffected);
        }

        [Test]
        public void Pulse_OverlapBudgetOverflowRefusesBeforeSpending()
        {
            emitter.ConfigurePhysics(1 << TargetLayer, 1 << ObstacleLayer, 16, 32);
            for (int i = 0; i < 20; i++) CreateReceiver("crowded-" + i, 4f + i * .04f, id: 300 + i);
            Physics.SyncTransforms();
            Assert.False(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(EmpPulseFailure.ColliderQueryOverflow, result.Failure);
            Assert.That(emitter.CooldownRemaining, Is.Zero);
            Assert.False(enemy.IsDisrupted);
        }

        [Test]
        public void Pulse_SelfColliderDoesNotBlockVisibility()
        {
            GameObject ownChild = Create("self-occluder", Vector3.right * 1.5f, ObstacleLayer);
            ownChild.transform.SetParent(player.transform, true);
            ownChild.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            Assert.True(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(1, result.ReceiversAffected);
        }

        [Test]
        public void Pulse_UnknownOcclusionQueryOverflowBlocksHit()
        {
            emitter.ConfigurePhysics(1 << TargetLayer, 1 << ObstacleLayer, 64, 8);
            for (int i = 0; i < 10; i++) AddObstacle(.8f + i * .11f);
            Physics.SyncTransforms();
            Assert.True(emitter.TryPulse(out EmpPulseReport result));
            Assert.AreEqual(0, result.ReceiversAffected);
            Assert.False(enemy.IsDisrupted);
        }

        [Test]
        public void Pulse_RecordsSuccessEventOnlyForFiredPulse()
        {
            int called = 0;
            emitter.Pulsed += _ => called++;
            Assert.True(emitter.TryPulse(out _));
            Assert.False(emitter.TryPulse(out _));
            Assert.AreEqual(1, called);
        }

        [UnityTest]
        public IEnumerator Receiver_RecoversAutomaticallyAfterDuration()
        {
            emitter.Configure(9f, .03f, .1f);
            Assert.True(emitter.TryPulse(out _));
            Assert.True(enemy.IsDisrupted);
            yield return new WaitForSeconds(.08f);
            Assert.False(enemy.IsDisrupted);
        }
    }
}

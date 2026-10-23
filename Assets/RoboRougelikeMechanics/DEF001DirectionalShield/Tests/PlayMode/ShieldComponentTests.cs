using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Defenses.DirectionalShield.Tests.PlayMode
{
    public class ShieldComponentTests
    {
        private GameObject _vehicle;
        private Rigidbody _body;
        private DirectionalShieldReceiver _receiver;
        private DirectionalShieldGenerator _shield;
        private const float Epsilon = 0.0001f;

        private class MockEnergy : IShieldEnergySource
        {
            public float Remaining;
            public MockEnergy(float remaining) { Remaining = remaining; }
            public bool TryConsume(float amount)
            {
                if (amount < 0 || Remaining < amount) return false;
                Remaining -= amount;
                return true;
            }
        }

        [SetUp]
        public void Setup()
        {
            _vehicle = new GameObject("DEF001-test-vehicle");
            _body = _vehicle.AddComponent<Rigidbody>();
            _body.isKinematic = true;
            _receiver = _vehicle.AddComponent<DirectionalShieldReceiver>();
            _receiver.Bind(_body);
            var mount = new GameObject("front-shield");
            mount.transform.SetParent(_vehicle.transform, false);
            _shield = mount.AddComponent<DirectionalShieldGenerator>();
            _shield.Bind(_body, mount.transform);
            _shield.Configure(100, 100, 0, 3, 60, 9);
            _receiver.Register(_shield);
        }

        [TearDown]
        public void TearDown()
        {
            if (_vehicle != null) Object.DestroyImmediate(_vehicle);
        }

        private float FrontHit(float damage, out float absorbed)
            => _receiver.Mitigate(damage, Vector3.forward * 2f, Vector3.back, out absorbed);

        [Test] public void FrontAttackIsAbsorbed()
        {
            float rem = FrontHit(20, out var absorbed);
            Assert.AreEqual(0, rem, Epsilon); Assert.AreEqual(20, absorbed,Epsilon);
            Assert.AreEqual(80, _shield.Charge,Epsilon);
        }
        [Test] public void RearAttackPassesThrough()
        {
            float remain = _receiver.Mitigate(20, Vector3.back * 2f, Vector3.forward, out var absorbed);
            Assert.AreEqual(20, remain,Epsilon); Assert.AreEqual(0, absorbed,Epsilon);
        }
        [Test] public void SideAttackPassesThrough()
        {
            float remain = _receiver.Mitigate(20, Vector3.right * 2f, Vector3.left, out var absorbed);
            Assert.AreEqual(20, remain,Epsilon); Assert.AreEqual(0, absorbed,Epsilon);
        }
        [Test] public void AbsorptionCannotExceedHitLimit()
        {
            _shield.Configure(100, 11, 0, 0, 60, 9);
            float remain = FrontHit(20,out var absorbed);
            Assert.AreEqual(11, absorbed,Epsilon); Assert.AreEqual(9, remain,Epsilon);
        }
        [Test] public void ExcessDamageReachesRealHealthPipeline()
        {
            _shield.Configure(10, 50, 0, 0, 60, 9);
            float remain = FrontHit(30,out var absorbed);
            Assert.AreEqual(10, absorbed,Epsilon); Assert.AreEqual(20, remain,Epsilon);
        }
        [Test] public void DisabledModuleCannotProtect()
        {
            _shield.SetOperational(false);
            Assert.AreEqual(20, FrontHit(20,out _),Epsilon);
        }
        [Test] public void DestroyedComponentCannotProtect()
        {
            _shield.gameObject.SetActive(false);
            Assert.AreEqual(20, FrontHit(20,out _),Epsilon);
        }
        [Test] public void DetachedModuleCannotProtectOldOwner()
        {
            var foreign = new GameObject("foreign");
            try
            {
                var newOwner = foreign.AddComponent<Rigidbody>();
                _shield.Bind(newOwner, _shield.transform);
                Assert.AreEqual(20, FrontHit(20,out _),Epsilon);
            }
            finally { Object.DestroyImmediate(foreign); }
        }
        [Test] public void RemovedModuleCannotProtect()
        {
            _receiver.Unregister(_shield);
            Assert.AreEqual(20, FrontHit(20,out _),Epsilon);
        }
        [Test] public void ChargeRestoredAfterRepair()
        {
            FrontHit(40,out _);
            _shield.SetOperational(false);
            _shield.RepairAndRecharge();
            Assert.AreEqual(100, _shield.Charge,Epsilon);
        }
        [Test] public void GeneratorOnAnotherVehicleIsIgnored()
        {
            var foreign = new GameObject("foreign");
            try
            {
                var body = foreign.AddComponent<Rigidbody>();
                var other = foreign.AddComponent<DirectionalShieldGenerator>();
                other.Bind(body, foreign.transform);
                other.Configure(100,100,0,0,60,9);
                _receiver.Register(other);
                float rem = FrontHit(120,out var absorbed);
                Assert.AreEqual(100,absorbed,Epsilon); Assert.AreEqual(20,rem,Epsilon);
            }
            finally { Object.DestroyImmediate(foreign); }
        }
        [Test] public void TwoShieldModulesShareDamageWithoutDoubleApplying()
        {
            var otherObj = new GameObject("second-shield");
            otherObj.transform.SetParent(_vehicle.transform,false);
            var other = otherObj.AddComponent<DirectionalShieldGenerator>();
            other.Bind(_body,otherObj.transform);
            other.Configure(20,20,0,0,60,9,newPriority:2);
            _receiver.Register(other);
            float rem = FrontHit(130,out var absorbed);
            Assert.AreEqual(10,rem,Epsilon); Assert.AreEqual(120,absorbed,Epsilon);
            Assert.AreEqual(0,other.Charge,Epsilon);
        }
        [Test] public void EnergyInsufficientMeansNoShieldChargeLost()
        {
            var energy = new MockEnergy(2f);
            _shield.Configure(100,100,0,0,60,9,energyPerPoint:2);
            _shield.SetEnergySource(energy);
            Assert.AreEqual(20,FrontHit(20,out var absorbed),Epsilon);
            Assert.AreEqual(0,absorbed,Epsilon); Assert.AreEqual(100,_shield.Charge,Epsilon);
        }
        [Test] public void SuccessfulHitRaisesEventOnce()
        {
            int count = 0; _shield.OnAbsorbed += value => count++;
            FrontHit(10,out _); Assert.AreEqual(1,count);
        }
        [Test] public void DepletionEventRaisedExactlyOnce()
        {
            _shield.Configure(10,10,0,0,60,9);
            int count = 0; _shield.OnDepleted += () => count++;
            FrontHit(10,out _); FrontHit(10,out _); Assert.AreEqual(1,count);
        }
        [Test] public void RegenerationAfterDelayThroughComponent()
        {
            _shield.Configure(100,100,10,2,60,9);
            FrontHit(20,out _);
            _shield.Advance(1f); Assert.AreEqual(80,_shield.Charge,Epsilon);
            _shield.Advance(2f); Assert.AreEqual(90,_shield.Charge,Epsilon);
        }
        [Test] public void RegenerationDoesNotRunWhenOffline()
        {
            _shield.Configure(100,100,10,0,60,9);
            FrontHit(20,out _);
            _shield.SetOperational(false);
            _shield.Advance(1f); Assert.AreEqual(80,_shield.Charge,Epsilon);
        }
        [Test] public void ZeroDirectionFailsClosed()
        {
            Assert.AreEqual(20,_receiver.Mitigate(20,Vector3.forward,Vector3.zero,out _),Epsilon);
        }
        [Test] public void NonFiniteInputFailsClosed()
        {
            Assert.AreEqual(20,_receiver.Mitigate(20,new Vector3(float.NaN,0,1),Vector3.back,out _),Epsilon);
        }

        [UnityTest]
        public IEnumerator RigidbodyBulletCollidesAndIsAbsorbed()
        {
            // Prawdziwe zdarzenie kolizji. Produkcyjne uszkodzenie pozostaje odpowiedzialnością R6.
            var targetCollider = _vehicle.AddComponent<BoxCollider>();
            targetCollider.size = Vector3.one;
            var bullet = new GameObject("physics-bullet");
            try
            {
                bullet.transform.position = new Vector3(0f,0f,3f);
                var sphere = bullet.AddComponent<SphereCollider>(); sphere.radius = 0.12f;
                var body = bullet.AddComponent<Rigidbody>();
                body.useGravity = false; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                var probe = bullet.AddComponent<ShieldProjectileTestProbe>(); probe.Receiver = _receiver; probe.TravelDirection = Vector3.back;
                body.linearVelocity = Vector3.back * 8f;
                for (int i=0; i<40 && probe.Remaining < 0f; i++) yield return new WaitForFixedUpdate();
                Assert.AreEqual(0, probe.Remaining, Epsilon);
                Assert.AreEqual(30, probe.Absorbed, Epsilon);
            }
            finally { Object.DestroyImmediate(bullet); }
        }

        [UnityTest]
        public IEnumerator RigidbodyBulletFromRearReachesBlockDamage()
        {
            var targetCollider = _vehicle.AddComponent<BoxCollider>();
            targetCollider.size = Vector3.one;
            // Kontakt sprawdzamy rzeczywistą fizyką, a w adapterze przekazujemy kierunek lotu do R6.
            var bullet = new GameObject("rear-physics-bullet");
            try
            {
                bullet.transform.position = new Vector3(0f,0f,-3f);
                var sphere = bullet.AddComponent<SphereCollider>(); sphere.radius = 0.12f;
                var body = bullet.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.linearVelocity = Vector3.forward * 8f;
                var probe = bullet.AddComponent<ShieldProjectileTestProbe>();
                probe.Receiver = _receiver;
                probe.TravelDirection = Vector3.forward;
                for (int i=0; i<40 && probe.Remaining < 0f; i++) yield return new WaitForFixedUpdate();
                Assert.AreEqual(30,probe.Remaining,Epsilon);
                Assert.AreEqual(0,probe.Absorbed,Epsilon);
            }
            finally { Object.DestroyImmediate(bullet); }
        }
    }
}

namespace RoboRougelike.Defenses.DirectionalShield.Tests.PlayMode
{
    // Top-level komponent: Unity może go utworzyć przez AddComponent w scenie testowej.
    public sealed class ShieldProjectileTestProbe : MonoBehaviour
    {
        public DirectionalShieldReceiver Receiver;
        public Vector3 TravelDirection = Vector3.back;
        public float RawDamage = 30f;
        public float Remaining = -1f;
        public float Absorbed;
        private void OnCollisionEnter(Collision contact)
        {
            if (Remaining >= 0f || Receiver == null || contact.collider.gameObject != Receiver.gameObject) return;
            Remaining = Receiver.Mitigate(RawDamage, contact.GetContact(0).point,
                TravelDirection, out Absorbed);
        }
    }
}

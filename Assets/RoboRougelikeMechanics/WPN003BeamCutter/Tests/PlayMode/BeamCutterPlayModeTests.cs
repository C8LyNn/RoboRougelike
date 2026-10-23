using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.Mechanics.BeamCutter;

namespace RoboRougelike.Mechanics.BeamCutter.Tests
{
    public sealed class RecordingBeamSink : MonoBehaviour, IBeamDamageSink
    {
        public int Hits;
        public float TotalDamage;
        public Collider LastTarget;
        public void ApplyBeamHit(in BeamHitRequest hit)
        {
            Hits++;
            TotalDamage += hit.Damage;
            LastTarget = hit.Collider;
        }
    }

    public sealed class RecordingBeamEnergy : MonoBehaviour, IBeamEnergySource
    {
        public float Available = 100f;
        public float Spent;
        public bool TryConsume(float amount)
        {
            if (amount < 0f || Available < amount) return false;
            Available -= amount;
            Spent += amount;
            return true;
        }
    }

    public sealed class BeamCutterPlayModeTests
    {
        private GameObject _vehicle;
        private Rigidbody _body;
        private GameObject _muzzle;
        private BeamCutterModule _beam;
        private RecordingBeamSink _sink;
        private GameObject _target;
        private BeamCutterSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _vehicle = new GameObject("Vehicle");
            _body = _vehicle.AddComponent<Rigidbody>();
            _body.useGravity = false;
            _body.constraints = RigidbodyConstraints.FreezeAll;
            _muzzle = new GameObject("Muzzle");
            _muzzle.transform.SetParent(_vehicle.transform, false);
            _beam = _vehicle.AddComponent<BeamCutterModule>();
            _sink = _vehicle.AddComponent<RecordingBeamSink>();
            _beam.SetAutomaticSimulation(false);
            _beam.Bind(_body, _muzzle.transform, _vehicle.transform, _sink);
            _settings = new BeamCutterSettings { damagePerSecond = 20f, heatPerSecond = 0f };
            _beam.Configure(_settings);
            _target = MakeCube("Target", 5f);
            Physics.SyncTransforms();
        }

        private static GameObject MakeCube(string name, float z)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.position = new Vector3(0f, 0f, z);
            return obj;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_target);
            Object.DestroyImmediate(_vehicle);
        }

        [Test]
        public void FiresIntoNearestCollider()
        {
            _beam.SetTriggerHeld(true);
            BeamStepResult result = _beam.ManualFixedStep(0.1f);
            Assert.AreEqual(BeamStepStatus.Firing, result.Status);
            Assert.AreEqual(_target.GetComponent<Collider>(), result.FirstHit);
            Assert.AreEqual(1, _sink.Hits);
        }

        [Test]
        public void DamageUsesActiveDuration_NotFrameCount()
        {
            _beam.SetTriggerHeld(true);
            _beam.ManualFixedStep(0.05f);
            _beam.ManualFixedStep(0.15f);
            Assert.AreEqual(4f, _sink.TotalDamage, 0.0001f);
        }

        [Test]
        public void NoTrigger_NoDamage()
        {
            Assert.AreEqual(BeamStepStatus.Idle, _beam.ManualFixedStep(0.1f).Status);
            Assert.AreEqual(0, _sink.Hits);
        }

        [Test]
        public void OwnColliderAhead_IsIgnored()
        {
            GameObject own = MakeCube("Own armor", 2f);
            own.transform.SetParent(_vehicle.transform, true);
            Physics.SyncTransforms();
            _beam.SetTriggerHeld(true);
            Assert.AreEqual(_target.GetComponent<Collider>(), _beam.ManualFixedStep(0.1f).FirstHit);
        }

        [Test]
        public void WallWithoutDamageReceiver_StillBlocks()
        {
            GameObject wall = MakeCube("Wall", 2f);
            try
            {
                Physics.SyncTransforms();
                _beam.SetTriggerHeld(true);
                BeamStepResult result = _beam.ManualFixedStep(0.1f);
                Assert.AreEqual(wall.GetComponent<Collider>(), result.FirstHit);
                Assert.AreEqual(wall.GetComponent<Collider>(), _sink.LastTarget);
                // The sink is a generic adapter. It decides whether to apply to walls.
            }
            finally { Object.DestroyImmediate(wall); }
        }

        [Test]
        public void ACloserTargetBlocksTargetBehind()
        {
            GameObject nearer = MakeCube("Closer", 2f);
            try
            {
                Physics.SyncTransforms();
                _beam.SetTriggerHeld(true);
                Assert.AreEqual(nearer.GetComponent<Collider>(), _beam.ManualFixedStep(0.1f).FirstHit);
                Assert.AreEqual(1, _sink.Hits);
            }
            finally { Object.DestroyImmediate(nearer); }
        }

        [Test]
        public void TriggerColliders_AreNotDamageableOrOccluding()
        {
            GameObject trigger = MakeCube("Trigger", 2f);
            trigger.GetComponent<Collider>().isTrigger = true;
            try
            {
                Physics.SyncTransforms();
                _beam.SetTriggerHeld(true);
                Assert.AreEqual(_target.GetComponent<Collider>(), _beam.ManualFixedStep(0.1f).FirstHit);
            }
            finally { Object.DestroyImmediate(trigger); }
        }

        [Test]
        public void LayerMaskCanExcludeTarget()
        {
            _settings.hitLayers = 1 << 9;
            _beam.Configure(_settings);
            _beam.SetTriggerHeld(true);
            Assert.IsNull(_beam.ManualFixedStep(0.1f).FirstHit);
            Assert.AreEqual(0, _sink.Hits);
        }

        [Test]
        public void MuzzleInsideForeignWall_FailsClosedWithoutDamage()
        {
            GameObject overlapping = MakeCube("Overlapping wall", 0f);
            try
            {
                overlapping.transform.localScale = new Vector3(2f, 2f, 2f);
                Physics.SyncTransforms();
                _beam.SetTriggerHeld(true);
                Assert.AreEqual(BeamStepStatus.ObstructedAtMuzzle, _beam.ManualFixedStep(0.1f).Status);
                Assert.AreEqual(0, _sink.Hits);
            }
            finally { Object.DestroyImmediate(overlapping); }
        }

        [Test]
        public void NoEnergyDoesNotDamageOrHeat()
        {
            RecordingBeamEnergy energy = _vehicle.AddComponent<RecordingBeamEnergy>();
            energy.Available = 0f;
            _settings.energyPerSecond = 10f;
            _beam.Configure(_settings);
            _beam.SetEnergySource(energy);
            _beam.SetTriggerHeld(true);
            Assert.AreEqual(BeamStepStatus.NoEnergy, _beam.ManualFixedStep(0.1f).Status);
            Assert.AreEqual(0, _sink.Hits);
            Assert.AreEqual(0f, _beam.HeatFraction);
        }

        [Test]
        public void EnergyExactlySpentForActualSeconds()
        {
            RecordingBeamEnergy energy = _vehicle.AddComponent<RecordingBeamEnergy>();
            _settings.energyPerSecond = 10f;
            _beam.Configure(_settings);
            _beam.SetEnergySource(energy);
            _beam.SetTriggerHeld(true);
            _beam.ManualFixedStep(0.25f);
            Assert.AreEqual(2.5f, energy.Spent, 0.0001f);
        }

        [Test]
        public void DestroyedOrDetachedEmitterCannotFire()
        {
            _beam.SetTriggerHeld(true);
            _beam.SetAttached(false);
            Assert.AreEqual(BeamStepStatus.Inoperable, _beam.ManualFixedStep(0.1f).Status);
            _beam.SetAttached(true);
            _beam.SetTriggerHeld(true);
            _beam.SetOperational(false);
            Assert.AreEqual(BeamStepStatus.Inoperable, _beam.ManualFixedStep(0.1f).Status);
        }

        [Test]
        public void MissingMuzzleFailsClosed()
        {
            _beam.Bind(_body, null, _vehicle.transform, _sink);
            _beam.SetTriggerHeld(true);
            Assert.AreEqual(BeamStepStatus.MissingBindings, _beam.ManualFixedStep(0.1f).Status);
            Assert.AreEqual(0, _sink.Hits);
        }

        [Test]
        public void OverheatLimitsDamageWithinFixedStep()
        {
            _settings.heatCapacity = 1f;
            _settings.heatPerSecond = 10f;
            _settings.coolPerSecond = 0f;
            _beam.Configure(_settings);
            _beam.SetTriggerHeld(true);
            BeamStepResult first = _beam.ManualFixedStep(1f);
            Assert.AreEqual(0.1f, first.ActiveSeconds, 0.0001f);
            Assert.AreEqual(2f, _sink.TotalDamage, 0.0001f);
            Assert.AreEqual(BeamStepStatus.Overheated, _beam.ManualFixedStep(0.1f).Status);
        }

        [Test]
        public void OwnGraphFilterCanIgnorePartOutsideHierarchy()
        {
            GameObject remoteOwnPart = MakeCube("Graph part", 2f);
            try
            {
                _beam.SetOwnColliderFilter(c => c == remoteOwnPart.GetComponent<Collider>());
                Physics.SyncTransforms();
                _beam.SetTriggerHeld(true);
                Assert.AreEqual(_target.GetComponent<Collider>(), _beam.ManualFixedStep(0.1f).FirstHit);
            }
            finally { Object.DestroyImmediate(remoteOwnPart); }
        }

        [UnityTest]
        public IEnumerator RecoilChangesActualRigidbodyVelocity()
        {
            _body.constraints = RigidbodyConstraints.None;
            _settings.recoilNewtons = 100f;
            _beam.Configure(_settings);
            _beam.SetTriggerHeld(true);
            _beam.ManualFixedStep(Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
            Assert.Less(_body.linearVelocity.z, 0f);
        }

        [UnityTest]
        public IEnumerator AutoFixedUpdateFiresOnPhysicsStep()
        {
            _beam.SetAutomaticSimulation(true);
            _beam.SetTriggerHeld(true);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.Greater(_sink.Hits, 0);
        }
    }
}

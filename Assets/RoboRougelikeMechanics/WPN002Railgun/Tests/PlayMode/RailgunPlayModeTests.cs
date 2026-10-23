using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.WPN002Railgun.Demo;

namespace RoboRougelike.WPN002Railgun.Tests.PlayMode
{
    public class RailgunPlayModeTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private GameObject Make(string name, Vector3 position, bool cube = false)
        {
            var o = cube ? GameObject.CreatePrimitive(PrimitiveType.Cube) : new GameObject(name);
            o.name = name; o.transform.position = position; objects.Add(o); return o;
        }
        private RailgunDemoBlock MakeTarget(string name, float z, float hp = 100f,
            float resistance = 20f, bool penetrable = true)
        {
            var go = Make(name, new Vector3(0, 0, z), true);
            var block = go.AddComponent<RailgunDemoBlock>(); block.Configure(hp, resistance, penetrable); return block;
        }
        private RailgunShot Shot(float speed = 100f, float range = 30f, int maxParts = 6,
            float energy = 100f, GameObject owner = null)
        {
            var cfg = new RailgunConfig { projectileSpeed = speed, maximumRange = range,
                maximumDistinctParts = maxParts, initialPenetrationEnergy = energy,
                energyLossPerMeter = 0f, damagePerEnergy = 1f, maximumDamagePerPart = 200f,
                minimumEnergyToContinue = 1f };
            return new RailgunShot(0u, Vector3.zero, Vector3.forward,
                Vector3.forward * speed, cfg.Snapshot(), null, owner);
        }
        private RailgunDemoProjectile CreateProjectile(RailgunShot shot)
        {
            var obj = Make("Projective", Vector3.zero);
            var projectile = obj.AddComponent<RailgunDemoProjectile>(); projectile.Initialize(shot);
            return projectile;
        }
        [TearDown] public void Cleanup()
        { for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]); objects.Clear(); }

        [Test] public void ProjectileTravelsInSpace()
        { var p = CreateProjectile(Shot()); p.SimulateStep(.05f); Assert.That(p.transform.position.z, Is.EqualTo(5f).Within(.01f)); }
        [Test] public void ProjectileCannotExceedRange()
        { var p = CreateProjectile(Shot(range: 4f)); p.SimulateStep(.2f); Assert.That(p.DistanceTravelled, Is.EqualTo(4f).Within(.01f)); Assert.That(p.IsTerminated, Is.True); }
        [Test] public void ProjectileHitsOneBlock()
        { var b = MakeTarget("B", 3f); Physics.SyncTransforms(); var p = CreateProjectile(Shot()); p.SimulateStep(.08f); Assert.That(b.HitCount, Is.EqualTo(1)); }
        [Test] public void ProjectilePenetratesMultipleBlocks()
        { var a = MakeTarget("A", 3); var b = MakeTarget("B", 6); Physics.SyncTransforms(); var p = CreateProjectile(Shot()); p.SimulateStep(.1f); Assert.That(a.HitCount, Is.EqualTo(1)); Assert.That(b.HitCount, Is.EqualTo(1)); }
        [Test] public void SecondBlockReceivesLessDamage()
        { var a = MakeTarget("A", 3, 200, 40); var b = MakeTarget("B", 6, 200, 40); Physics.SyncTransforms(); var p = CreateProjectile(Shot()); p.SimulateStep(.1f); Assert.That(200f - b.HitPoints, Is.LessThan(200f - a.HitPoints)); }
        [Test] public void NonPenetrableBlockStopsProjectile()
        { var a = MakeTarget("A", 3, 200, 10, false); var b = MakeTarget("B", 6); Physics.SyncTransforms(); var p = CreateProjectile(Shot()); p.SimulateStep(.1f); Assert.That(a.HitCount, Is.EqualTo(1)); Assert.That(b.HitCount, Is.Zero); Assert.That(p.IsTerminated, Is.True); }
        [Test] public void EnergyExhaustionStopsProjectile()
        { var a = MakeTarget("A", 3, 200, 150); var b = MakeTarget("B", 6); Physics.SyncTransforms(); var p = CreateProjectile(Shot()); p.SimulateStep(.1f); Assert.That(a.HitCount, Is.EqualTo(1)); Assert.That(b.HitCount, Is.Zero); }
        [Test] public void PenetrationPartCapStopsProjectile()
        { var a = MakeTarget("A", 3); var b = MakeTarget("B", 6); Physics.SyncTransforms(); var p = CreateProjectile(Shot(maxParts: 1)); p.SimulateStep(.1f); Assert.That(a.HitCount, Is.EqualTo(1)); Assert.That(b.HitCount, Is.Zero); }
        [Test] public void HardWallBlocksProjectile()
        { Make("Wall", new Vector3(0, 0, 2), true); var b = MakeTarget("B", 5); Physics.SyncTransforms(); var p = CreateProjectile(Shot()); p.SimulateStep(.1f); Assert.That(b.HitCount, Is.Zero); Assert.That(p.IsTerminated, Is.True); }
        [Test] public void ShooterColliderIsIgnored()
        { var owner = Make("Owner", new Vector3(0, 0, 1), true); var b = MakeTarget("B", 5); Physics.SyncTransforms(); var p = CreateProjectile(Shot(owner: owner)); p.SimulateStep(.1f); Assert.That(b.HitCount, Is.EqualTo(1)); }
        [Test] public void TwoCollidersSameBlockAreCountedOnce()
        {
            var root = Make("PartRoot", Vector3.zero); var part = root.AddComponent<RailgunDemoBlock>(); part.Configure(200, 20);
            var near = Make("ColliderNear", new Vector3(0, 0, 3), true); near.transform.SetParent(root.transform, true);
            var far = Make("ColliderFar", new Vector3(0, 0, 4), true); far.transform.SetParent(root.transform, true);
            Physics.SyncTransforms(); var p = CreateProjectile(Shot()); p.SimulateStep(.1f);
            Assert.That(part.HitCount, Is.EqualTo(1)); Assert.That(p.DistinctPartsHit, Is.EqualTo(1));
        }
        [Test] public void DestroyedDemoBlockDisablesItsCollider()
        { var b = MakeTarget("B", 3, 5); Physics.SyncTransforms(); var p = CreateProjectile(Shot()); p.SimulateStep(.1f); Assert.That(b.HitPoints, Is.Zero); Assert.That(b.GetComponent<Collider>().enabled, Is.False); }
        [Test] public void NoDamageBeyondMaxRange()
        { var b = MakeTarget("B", 7); Physics.SyncTransforms(); var p = CreateProjectile(Shot(range: 3)); p.SimulateStep(.2f); Assert.That(b.HitCount, Is.Zero); }
        [Test] public void SinkRejectsWithoutPrefab()
        { var sink = Make("Sink", Vector3.zero).AddComponent<RailgunDemoShotSink>(); Assert.That(sink.TrySpawnShot(Shot()), Is.False); }
        [Test] public void SinkCreatesConfiguredProjectile()
        {
            var prefabObject = Make("Prefab", Vector3.zero); var prefab = prefabObject.AddComponent<RailgunDemoProjectile>(); prefabObject.SetActive(false);
            var sink = Make("Sink", Vector3.zero).AddComponent<RailgunDemoShotSink>(); sink.SetPrefab(prefab);
            Assert.That(sink.TrySpawnShot(Shot()), Is.True);
            Assert.That(sink.LastSpawned, Is.Not.Null);
            Assert.That(sink.LastSpawned.gameObject.activeSelf, Is.True);
            Object.DestroyImmediate(sink.LastSpawned.gameObject);
        }
        [Test] public void WeaponFiresAndConsumesOneRound()
        {
            var owner = Make("Owner", Vector3.zero); var body = owner.AddComponent<Rigidbody>(); body.isKinematic = true;
            var muzzle = Make("Muzzle", Vector3.forward); muzzle.transform.SetParent(owner.transform, true);
            var gunObject = Make("Gun", Vector3.zero); gunObject.transform.SetParent(owner.transform, true);
            var gun = gunObject.AddComponent<RailgunModule>(); var bank = gunObject.AddComponent<RailgunSimpleAmmo>(); bank.SetRounds(3);
            var sink = gunObject.AddComponent<TestShotSink>(); sink.Accept = true;
            gun.Bind(body, muzzle.transform, owner); gun.BindSink(sink); gun.BindResourceBank(bank);
            Assert.That(gun.TryFireAtTime(0), Is.True); Assert.That(bank.Rounds, Is.EqualTo(2)); Assert.That(sink.Received, Is.EqualTo(1));
        }
        [Test] public void WeaponRefundsAmmoIfSinkRejects()
        {
            var owner = Make("Owner", Vector3.zero); var body = owner.AddComponent<Rigidbody>(); body.isKinematic = true;
            var muzzle = Make("Muzzle", Vector3.forward); muzzle.transform.SetParent(owner.transform, true);
            var gunObject = Make("Gun", Vector3.zero); gunObject.transform.SetParent(owner.transform, true);
            var gun = gunObject.AddComponent<RailgunModule>(); var bank = gunObject.AddComponent<RailgunSimpleAmmo>(); bank.SetRounds(3);
            var sink = gunObject.AddComponent<TestShotSink>(); sink.Accept = false;
            gun.Bind(body, muzzle.transform, owner); gun.BindSink(sink); gun.BindResourceBank(bank);
            Assert.That(gun.TryFireAtTime(0), Is.False); Assert.That(bank.Rounds, Is.EqualTo(3)); Assert.That(gun.SuccessfulShots, Is.Zero);
        }
        [Test] public void DetachedGunCannotFire()
        {
            var owner = Make("Owner", Vector3.zero); var body = owner.AddComponent<Rigidbody>(); body.isKinematic = true;
            var muzzle = Make("Muzzle", Vector3.forward); muzzle.transform.SetParent(owner.transform, true);
            var gunObject = Make("Gun", Vector3.zero); gunObject.transform.SetParent(owner.transform, true);
            var gun = gunObject.AddComponent<RailgunModule>(); var sink = gunObject.AddComponent<TestShotSink>(); sink.Accept = true;
            gun.Bind(body, muzzle.transform, owner); gun.BindSink(sink);
            gunObject.transform.SetParent(null, true);
            Assert.That(gun.TryFireAtTime(0), Is.False); Assert.That(gun.LastFailure, Is.EqualTo(RailgunFireFailure.Detached));
        }
        [Test] public void CooldownCannotConsumeASecondRound()
        {
            var owner = Make("Owner", Vector3.zero); var body = owner.AddComponent<Rigidbody>(); body.isKinematic = true;
            var muzzle = Make("Muzzle", Vector3.forward); muzzle.transform.SetParent(owner.transform, true);
            var gunObject = Make("Gun", Vector3.zero); gunObject.transform.SetParent(owner.transform, true);
            var gun = gunObject.AddComponent<RailgunModule>(); var bank = gunObject.AddComponent<RailgunSimpleAmmo>(); bank.SetRounds(2);
            var sink = gunObject.AddComponent<TestShotSink>(); sink.Accept = true;
            gun.Bind(body, muzzle.transform, owner); gun.BindSink(sink); gun.BindResourceBank(bank);
            Assert.That(gun.TryFireAtTime(0f), Is.True);
            Assert.That(gun.TryFireAtTime(.5f), Is.False);
            Assert.That(gun.LastFailure, Is.EqualTo(RailgunFireFailure.Cooldown));
            Assert.That(bank.Rounds, Is.EqualTo(1));
        }
        [Test] public void DisabledGunCannotConsumeAmmo()
        {
            var owner = Make("Owner", Vector3.zero); var body = owner.AddComponent<Rigidbody>(); body.isKinematic = true;
            var muzzle = Make("Muzzle", Vector3.forward); muzzle.transform.SetParent(owner.transform, true);
            var gunObject = Make("Gun", Vector3.zero); gunObject.transform.SetParent(owner.transform, true);
            var gun = gunObject.AddComponent<RailgunModule>(); var bank = gunObject.AddComponent<RailgunSimpleAmmo>(); bank.SetRounds(2);
            var sink = gunObject.AddComponent<TestShotSink>(); sink.Accept = true;
            gun.Bind(body, muzzle.transform, owner); gun.BindSink(sink); gun.BindResourceBank(bank);
            gun.SetOperational(false);
            Assert.That(gun.TryFireAtTime(0f), Is.False);
            Assert.That(bank.Rounds, Is.EqualTo(2));
        }
        [Test] public void InitialShotInheritsVehicleVelocity()
        {
            var owner = Make("Owner", Vector3.zero); var body = owner.AddComponent<Rigidbody>();
            body.useGravity = false; body.linearVelocity = Vector3.right * 4f;
            var muzzle = Make("Muzzle", Vector3.forward); muzzle.transform.SetParent(owner.transform, true);
            var gunObject = Make("Gun", Vector3.zero); gunObject.transform.SetParent(owner.transform, true);
            var gun = gunObject.AddComponent<RailgunModule>(); var sink = gunObject.AddComponent<TestShotSink>(); sink.Accept = true;
            gun.Bind(body, muzzle.transform, owner); gun.BindSink(sink);
            Assert.That(gun.TryFireAtTime(0f), Is.True);
            Assert.That(sink.LastShot.InitialVelocity.x, Is.EqualTo(4f).Within(.1f));
        }
        [Test] public void RecoilActsOppositeToMuzzleDirection()
        {
            var owner = Make("Owner", Vector3.zero); var body = owner.AddComponent<Rigidbody>();
            body.useGravity = false; body.mass = 100f;
            var muzzle = Make("Muzzle", Vector3.forward); muzzle.transform.SetParent(owner.transform, true);
            var gunObject = Make("Gun", Vector3.zero); gunObject.transform.SetParent(owner.transform, true);
            var gun = gunObject.AddComponent<RailgunModule>(); var sink = gunObject.AddComponent<TestShotSink>(); sink.Accept = true;
            gun.Bind(body, muzzle.transform, owner); gun.BindSink(sink);
            Assert.That(gun.TryFireAtTime(0f), Is.True);
            // Unity applies AddForce impulses during the next physics simulation step.
            // Observe that step explicitly, preserving the runner's simulation mode.
            var mode = Physics.simulationMode;
            try { Physics.simulationMode = SimulationMode.Script; Physics.Simulate(.02f); }
            finally { Physics.simulationMode = mode; }
            Assert.That(body.linearVelocity.z, Is.LessThan(0f));
        }
    }

    // Standalone MonoBehaviour type: Unity can attach it to dynamically created GameObjects.
    public sealed class TestShotSink : MonoBehaviour, IRailgunShotSink
    {
        public bool Accept;
        public int Received;
        public RailgunShot LastShot;
        public bool TrySpawnShot(RailgunShot shot) { Received++; LastShot = shot; return Accept; }
    }
}

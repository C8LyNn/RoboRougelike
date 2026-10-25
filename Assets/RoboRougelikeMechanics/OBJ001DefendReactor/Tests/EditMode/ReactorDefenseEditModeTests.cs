using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.OBJ001.Tests
{
    public sealed class ReactorDefenseEditModeTests
    {
        private GameObject root;
        private ReactorDefenseObjective objective;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("OBJ001 EditMode");
            objective = root.AddComponent<ReactorDefenseObjective>();
            Assert.IsTrue(objective.Configure(100f, 10f));
            objective.SetAutomaticClock(false);
        }

        [TearDown]
        public void TearDown() { if (root != null) Object.DestroyImmediate(root); }

        [Test]
        public void StartsIdleAndRejectsDamage()
        {
            Assert.AreEqual(DefenseState.Idle, objective.State);
            Assert.AreEqual(100f, objective.Integrity);
            Assert.IsFalse(objective.ApplyDamage(10f));
        }

        [Test]
        public void BeginRequiresEnabledAndCanOnlyRunOnce()
        {
            objective.enabled = false;
            Assert.IsFalse(objective.Begin());
            objective.enabled = true;
            Assert.IsTrue(objective.Begin());
            Assert.IsFalse(objective.Begin());
        }

        [Test]
        public void DamageReportsActualAmount()
        {
            float applied = 0f;
            objective.DamageTaken += value => applied += value;
            objective.Begin();
            Assert.IsTrue(objective.ApplyDamage(25f));
            Assert.AreEqual(75f, objective.Integrity);
            Assert.AreEqual(25f, applied);
        }

        [Test]
        public void RepairClampsAndRejectsFullIntegrity()
        {
            objective.Begin();
            Assert.IsFalse(objective.Repair(10f));
            objective.ApplyDamage(20f);
            float repaired = 0f;
            objective.RepairApplied += value => repaired += value;
            Assert.IsTrue(objective.Repair(999f));
            Assert.AreEqual(100f, objective.Integrity);
            Assert.AreEqual(20f, repaired);
            Assert.IsFalse(objective.Repair(1f));
        }

        [Test]
        public void FatalDamageLosesOnlyOnce()
        {
            int losses = 0;
            objective.StateChanged += state => { if (state == DefenseState.Lost) losses++; };
            objective.Begin();
            Assert.IsTrue(objective.ApplyDamage(150f));
            Assert.AreEqual(DefenseState.Lost, objective.State);
            Assert.AreEqual(0f, objective.Integrity);
            Assert.IsFalse(objective.ApplyDamage(1f));
            Assert.IsFalse(objective.Repair(10f));
            Assert.IsFalse(objective.Tick(10f));
            Assert.AreEqual(1, losses);
        }

        [Test]
        public void ClockWinsExactlyOnceAtDeadline()
        {
            int wins = 0;
            objective.StateChanged += state => { if (state == DefenseState.Won) wins++; };
            objective.Begin();
            Assert.IsTrue(objective.Tick(4f));
            Assert.AreEqual(DefenseState.Defending, objective.State);
            Assert.IsTrue(objective.Tick(6f));
            Assert.AreEqual(DefenseState.Won, objective.State);
            Assert.AreEqual(0f, objective.RemainingSeconds);
            Assert.IsFalse(objective.Tick(1f));
            Assert.IsFalse(objective.ApplyDamage(100f));
            Assert.AreEqual(1, wins);
        }

        [Test]
        public void InvalidNumbersDoNotMutateState()
        {
            objective.Begin();
            foreach (float bad in new[] { -1f, 0f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Assert.IsFalse(objective.ApplyDamage(bad));
                Assert.IsFalse(objective.Repair(bad));
                Assert.IsFalse(objective.Tick(bad));
            }
            Assert.AreEqual(100f, objective.Integrity);
            Assert.AreEqual(0f, objective.ElapsedSeconds);
        }

        [Test]
        public void ResetRestoresStateAndAllowsRestart()
        {
            objective.Begin();
            objective.ApplyDamage(40f);
            objective.Tick(5f);
            objective.ResetObjective();
            Assert.AreEqual(DefenseState.Idle, objective.State);
            Assert.AreEqual(100f, objective.Integrity);
            Assert.AreEqual(0f, objective.ElapsedSeconds);
            Assert.IsTrue(objective.Begin());
        }

        [Test]
        public void ActiveConfigurationIsRejected()
        {
            objective.Begin();
            Assert.IsFalse(objective.Configure(200f, 1f));
            Assert.AreEqual(100f, objective.MaximumIntegrity);
            Assert.AreEqual(10f, objective.DefendSeconds);
        }

        [Test]
        public void InvalidConfigurationIsRejected()
        {
            Assert.IsFalse(objective.Configure(float.NaN, 2f));
            Assert.IsFalse(objective.Configure(2f, float.PositiveInfinity));
            Assert.IsFalse(objective.Configure(0f, 2f));
            Assert.IsFalse(objective.Configure(2f, 0f));
            Assert.IsTrue(objective.Configure(120f, 2f));
            Assert.AreEqual(120f, objective.Integrity);
            Assert.AreEqual(2f, objective.RemainingSeconds);
        }

        [Test]
        public void FirstTerminalActionDeterminesOutcome()
        {
            objective.Begin();
            objective.ApplyDamage(100f);
            Assert.IsFalse(objective.Tick(10f));
            Assert.AreEqual(DefenseState.Lost, objective.State);
            objective.ResetObjective();
            objective.Begin();
            objective.Tick(10f);
            Assert.IsFalse(objective.ApplyDamage(100f));
            Assert.AreEqual(DefenseState.Won, objective.State);
        }
    }
}

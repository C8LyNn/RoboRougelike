using NUnit.Framework;
using UnityEngine;
using RoboRougelike.Mechanics.EMP;

namespace RoboRougelike.Mechanics.EMP.Tests
{
    public sealed class EmpReceiverAndEnergyTests
    {
        private GameObject go;
        [SetUp] public void Setup() => go = new GameObject("emp-test");
        [TearDown] public void Teardown() => Object.DestroyImmediate(go);

        [Test]
        public void Receiver_ReportsStartAndEndExactlyOnce()
        {
            var receiver = go.AddComponent<EmpReceiver>();
            int starts = 0, ends = 0;
            receiver.DisruptionChanged += (_, active) => { if (active) starts++; else ends++; };
            receiver.ApplyEmp(3f);
            receiver.ApplyEmp(1f);
            receiver.Advance(3f);
            receiver.Advance(2f);
            Assert.AreEqual(1, starts);
            Assert.AreEqual(1, ends);
        }

        [Test]
        public void Receiver_RefreshDoesNotDoubleStart()
        {
            var receiver = go.AddComponent<EmpReceiver>();
            int starts = 0;
            receiver.DisruptionChanged += (_, active) => { if (active) starts++; };
            receiver.ApplyEmp(1f);
            receiver.ApplyEmp(5f);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(receiver.RemainingSeconds, Is.EqualTo(5f));
        }

        [Test]
        public void Receiver_ImmuneRejectsPulse()
        {
            var receiver = go.AddComponent<EmpReceiver>();
            receiver.ConfigureSusceptibility(0f, false);
            receiver.ApplyEmp(3f);
            Assert.False(receiver.IsDisrupted);
        }

        [Test]
        public void Receiver_CanStoreOwnership()
        {
            var receiver = go.AddComponent<EmpReceiver>();
            receiver.ConfigureOwnership(go.transform, 27, 3);
            Assert.AreEqual(27, receiver.VehicleId);
            Assert.AreEqual(3, receiver.TeamId);
            Assert.AreSame(go.transform, receiver.VehicleRoot);
        }

        [Test]
        public void EnergyBank_SpendingAndRefundRespectCapacity()
        {
            var bank = go.AddComponent<EmpEnergyReservoir>();
            bank.Configure(50f, 20f, 0f);
            Assert.True(bank.TrySpend(15f));
            Assert.That(bank.Current, Is.EqualTo(5f));
            bank.Refund(100f);
            Assert.That(bank.Current, Is.EqualTo(50f));
        }

        [Test]
        public void EnergyBank_InsufficientEnergyIsAtomic()
        {
            var bank = go.AddComponent<EmpEnergyReservoir>();
            bank.Configure(50f, 4f, 0f);
            Assert.False(bank.TrySpend(5f));
            Assert.That(bank.Current, Is.EqualTo(4f));
        }

        [Test]
        public void EnergyBank_NegativeSpendRejected()
        {
            var bank = go.AddComponent<EmpEnergyReservoir>();
            bank.Configure(50f, 20f, 0f);
            Assert.False(bank.TrySpend(-2f));
            Assert.That(bank.Current, Is.EqualTo(20f));
        }
    }
}

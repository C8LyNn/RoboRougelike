using NUnit.Framework;
using UnityEngine;
using RoboRougelike.Mechanics.PointDefense;

namespace RoboRougelike.Mechanics.PointDefense.Tests
{
    public sealed class PointDefenseEnergyTests
    {
        private GameObject owner;
        private PointDefenseEnergyStore store;
        [SetUp] public void Setup()
        {
            owner = new GameObject("DEF002_EditModeEnergy");
            store = owner.AddComponent<PointDefenseEnergyStore>();
            store.Configure(10, 6, 0);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(owner); }
        [Test] public void ExactBalance_IsSpendable()
        { Assert.IsTrue(store.TrySpend(6)); Assert.AreEqual(0, store.Current); }
        [Test] public void InsufficientBalance_DoesNotSpend()
        { Assert.IsFalse(store.TrySpend(7)); Assert.AreEqual(6, store.Current); }
        [Test] public void ZeroCost_IsAllowed()
        { Assert.IsTrue(store.TrySpend(0)); Assert.AreEqual(6, store.Current); }
        [Test] public void NegativeCost_IsRejected()
        { Assert.IsFalse(store.TrySpend(-1)); Assert.AreEqual(6, store.Current); }
        [Test] public void NaNCost_IsRejected()
        { Assert.IsFalse(store.TrySpend(float.NaN)); Assert.AreEqual(6, store.Current); }
        [Test] public void InfiniteCost_IsRejected()
        { Assert.IsFalse(store.TrySpend(float.PositiveInfinity)); Assert.AreEqual(6, store.Current); }
        [Test] public void Configure_ClampsInitialEnergy()
        { store.Configure(5, 99, 0); Assert.AreEqual(5, store.Current); }
        [Test] public void MultipleShots_CannotOverdraw()
        { Assert.IsTrue(store.TrySpend(4)); Assert.IsFalse(store.TrySpend(4)); Assert.AreEqual(2, store.Current); }
    }
}

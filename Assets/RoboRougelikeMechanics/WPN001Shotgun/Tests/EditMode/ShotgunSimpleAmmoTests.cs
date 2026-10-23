using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.WPN001Shotgun.Tests.EditMode
{
    public sealed class ShotgunSimpleAmmoTests
    {
        private GameObject go;
        private ShotgunSimpleAmmo ammo;
        [SetUp] public void Setup()
        {
            go = new GameObject("test ammo"); ammo = go.AddComponent<ShotgunSimpleAmmo>();
        }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(go);
        [Test] public void ConsumeOneRound() { ammo.Configure(2, false); Assert.IsTrue(ammo.TryConsumeShot()); Assert.AreEqual(1, ammo.Rounds); }
        [Test] public void RefuseEmptyMagazine() { ammo.Configure(0, false); Assert.IsFalse(ammo.TryConsumeShot()); Assert.AreEqual(0, ammo.Rounds); }
        [Test] public void RefundOneRound() { ammo.Configure(1, false); ammo.TryConsumeShot(); ammo.RefundShot(); Assert.AreEqual(1, ammo.Rounds); }
        [Test] public void InfiniteAmmoDoesNotDecrement() { ammo.Configure(0, true); Assert.IsTrue(ammo.TryConsumeShot()); Assert.AreEqual(0, ammo.Rounds); }
        [Test] public void NegativeAmmoConfigurationClampsZero() { ammo.Configure(-3, false); Assert.AreEqual(0, ammo.Rounds); }
    }
}

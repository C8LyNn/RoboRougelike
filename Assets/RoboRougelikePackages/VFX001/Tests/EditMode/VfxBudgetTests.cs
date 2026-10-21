using NUnit.Framework;

namespace RoboRougelike.VFX001.Tests
{
    public class VfxBudgetTests
    {
        [Test]
        public void NormalEvents_StopAtPerFrameLimit()
        {
            var budget = new VfxBudgetPolicy(10, 2);
            Assert.IsTrue(budget.TryAdmit(1, 0, VfxEventKind.Hit));
            Assert.IsTrue(budget.TryAdmit(1, 1, VfxEventKind.Hit));
            Assert.IsFalse(budget.TryAdmit(1, 2, VfxEventKind.Hit));
        }
        [Test]
        public void NewFrame_ResetsPerFrameCounter()
        {
            var budget = new VfxBudgetPolicy(10, 1);
            Assert.IsTrue(budget.TryAdmit(1, 0, VfxEventKind.Hit));
            Assert.IsFalse(budget.TryAdmit(1, 1, VfxEventKind.Hit));
            Assert.IsTrue(budget.TryAdmit(2, 1, VfxEventKind.Hit));
        }
        [Test]
        public void ActiveCap_IsAlwaysRespected()
        {
            var budget = new VfxBudgetPolicy(1, 3);
            Assert.IsFalse(budget.TryAdmit(10, 1, VfxEventKind.CoreDestroyed));
        }
        [Test]
        public void CoreExplosion_BypassesPerFrameLimit()
        {
            var budget = new VfxBudgetPolicy(4, 1);
            Assert.IsTrue(budget.TryAdmit(1, 0, VfxEventKind.Hit));
            Assert.IsTrue(budget.TryAdmit(1, 1, VfxEventKind.CoreDestroyed));
        }
        [Test]
        public void InvalidConfig_IsClamped()
        {
            var budget = new VfxBudgetPolicy(0, -2);
            Assert.AreEqual(1, budget.MaxActive);
            Assert.AreEqual(1, budget.MaxPerFrame);
        }
    }
}

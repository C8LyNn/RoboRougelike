using NUnit.Framework;

namespace RoboRougelike.Mechanics.MOV003Blink.Tests
{
    public sealed class BlinkCooldownTests
    {
        [Test]
        public void NewCooldownIsReady() => Assert.IsTrue(new BlinkCooldown().IsReady(0f));
        [Test]
        public void BlocksActivationUntilReadyTime()
        {
            var cooldown = new BlinkCooldown();
            Assert.IsTrue(cooldown.TryStart(2f, 10f));
            Assert.IsFalse(cooldown.IsReady(11.999f));
            Assert.AreEqual(5f, cooldown.Remaining(7f), 0.0001f);
            Assert.IsTrue(cooldown.IsReady(12f));
        }
        [Test]
        public void FailedAttemptDoesNotResetCooldown()
        {
            var cooldown = new BlinkCooldown();
            Assert.IsTrue(cooldown.TryStart(1f, 8f));
            Assert.IsFalse(cooldown.TryStart(2f, 8f));
            Assert.AreEqual(9f, cooldown.ReadyAt);
        }
        [Test]
        public void ResetReturnsToReady()
        {
            var cooldown = new BlinkCooldown();
            cooldown.TryStart(1f, 80f);
            cooldown.Reset();
            Assert.IsTrue(cooldown.IsReady(2f));
        }
        [Test]
        public void RejectsInvalidNumbers()
        {
            var cooldown = new BlinkCooldown();
            Assert.IsFalse(cooldown.TryStart(float.NaN, 2f));
            Assert.IsFalse(cooldown.TryStart(2f, -1f));
            Assert.IsFalse(cooldown.TryStart(2f, float.PositiveInfinity));
            Assert.IsTrue(cooldown.IsReady(2f));
        }
    }
}

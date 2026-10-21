using NUnit.Framework;

namespace RoboRougelike.Mechanics.MOV002JumpDrive.Tests
{
    public sealed class JumpDriveCooldownTests
    {
        [Test]
        public void InitialStateReady() => Assert.IsTrue(new JumpDriveCooldown().IsReady(0f));
        [Test]
        public void RejectsEarlyReactivateAndPreservesReadyAt()
        {
            var cooldown = new JumpDriveCooldown();
            Assert.IsTrue(cooldown.TryStart(2f, 7f));
            Assert.IsFalse(cooldown.TryStart(3f, 7f));
            Assert.AreEqual(9f, cooldown.ReadyAt);
            Assert.AreEqual(6f, cooldown.Remaining(3f), 0.0001f);
        }
        [Test]
        public void AllowsActivationExactlyAtDeadline()
        {
            var cooldown = new JumpDriveCooldown();
            cooldown.TryStart(1f, 4f);
            Assert.IsTrue(cooldown.TryStart(5f, 4f));
            Assert.AreEqual(9f, cooldown.ReadyAt);
        }
        [Test]
        public void ResetMakesDriveReady()
        {
            var cooldown = new JumpDriveCooldown();
            cooldown.TryStart(0f, 20f);
            cooldown.Reset();
            Assert.IsTrue(cooldown.IsReady(1f));
        }
        [Test]
        public void RejectsClockNaNAndNegativeDuration()
        {
            var cooldown = new JumpDriveCooldown();
            Assert.IsFalse(cooldown.TryStart(float.NaN, 1f));
            Assert.IsFalse(cooldown.TryStart(1f, -1f));
            Assert.IsTrue(cooldown.IsReady(1f));
        }
        [Test]
        public void ZeroCooldownPermitsSameTimestampAgain()
        {
            var cooldown = new JumpDriveCooldown();
            Assert.IsTrue(cooldown.TryStart(1f, 0f));
            Assert.IsTrue(cooldown.TryStart(1f, 0f));
        }
    }
}

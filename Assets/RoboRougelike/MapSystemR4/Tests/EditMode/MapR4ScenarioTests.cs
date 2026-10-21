using NUnit.Framework;
using RoboRougelike.MapSystemR4;

namespace RoboRougelike.MapSystemR4.Tests
{
    public class MapR4ScenarioTests
    {
        [Test]
        public void TimeAttack_NeedsCheckpointFirst()
        {
            var runtime = new MapR4ChallengeRuntime(MapR4Mode.TimeAttack);
            runtime.Begin();
            runtime.NotifyAnchor("finish_east");
            Assert.AreEqual(MapR4Status.Running, runtime.Status);
            runtime.NotifyAnchor("checkpoint_upper");
            runtime.NotifyAnchor("finish_east");
            Assert.AreEqual(MapR4Status.Completed, runtime.Status);
        }

        [Test]
        public void Escape_CompletesAtExit()
        {
            var runtime = new MapR4ChallengeRuntime(MapR4Mode.Escape);
            runtime.Begin();
            runtime.NotifyAnchor("exit_east");
            Assert.AreEqual(MapR4Status.Completed, runtime.Status);
        }

        [Test]
        public void Survival_DurationSucceedsInSandbox()
        {
            var runtime = new MapR4ChallengeRuntime(MapR4Mode.Survival, survivalDuration: 2f);
            runtime.Begin();
            runtime.Tick(1.9f);
            Assert.AreEqual(MapR4Status.Running, runtime.Status);
            runtime.Tick(0.2f);
            Assert.AreEqual(MapR4Status.Completed, runtime.Status);
        }

        [Test]
        public void Boss_RequiresExplicitAdapterEvent()
        {
            var runtime = new MapR4ChallengeRuntime(MapR4Mode.Boss);
            runtime.Begin();
            runtime.NotifyAnchor("boss_middle");
            Assert.AreEqual(MapR4Status.Running, runtime.Status);
            runtime.NotifyBossDefeated();
            Assert.AreEqual(MapR4Status.Completed, runtime.Status);
        }

        [Test]
        public void PlayerDestroyed_FailsAndDoesNotLaterSucceed()
        {
            var runtime = new MapR4ChallengeRuntime(MapR4Mode.Escape);
            runtime.Begin();
            runtime.NotifyPlayerDestroyed();
            runtime.NotifyAnchor("exit_east");
            Assert.AreEqual(MapR4Status.Failed, runtime.Status);
        }

        [Test]
        public void Timeout_FailsAndCannotCompleteLater()
        {
            var runtime = new MapR4ChallengeRuntime(MapR4Mode.TimeAttack, timeLimitSeconds: 1f);
            runtime.Begin();
            runtime.Tick(1.1f);
            runtime.NotifyAnchor("checkpoint_upper");
            runtime.NotifyAnchor("finish_east");
            Assert.AreEqual(MapR4Status.Failed, runtime.Status);
        }
    }
}

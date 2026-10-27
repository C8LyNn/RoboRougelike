using NUnit.Framework;
using B01.CHL003;

namespace B01.CHL003.Tests
{
    public sealed class CHL003CoreEditModeTests
    {
        private CHL003CourseCore sut;
        [SetUp] public void SetUp()
        {
            sut = new CHL003CourseCore();
            sut.Configure(4, 321);
        }

        [Test] public void E01_OrderedGatesAndCompletionOnce()
        {
            int cp = 0, wins = 0;
            sut.CheckpointReached += _ => cp++;
            sut.Completed += _ => wins++;
            Assert.IsTrue(sut.Begin());
            for (int n = 0; n < 4; n++) Assert.IsTrue(sut.EnterCheckpoint(321, n));
            Assert.IsTrue(sut.EnterFinish(321));
            Assert.IsFalse(sut.EnterFinish(321));
            Assert.AreEqual(CHL003CourseCore.Phase.Completed, sut.State);
            Assert.AreEqual(4, cp);
            Assert.AreEqual(1, wins);
        }

        [Test] public void E02_EarlyFinishNeverCompletes()
        {
            int wins = 0; sut.Completed += _ => wins++;
            sut.Begin();
            Assert.IsFalse(sut.EnterFinish(321));
            sut.EnterCheckpoint(321, 0);
            Assert.IsFalse(sut.EnterFinish(321));
            Assert.AreEqual(0, wins);
        }

        [Test] public void E03_OutOfOrderAndSkippedCheckpointRejected()
        {
            sut.Begin();
            Assert.IsFalse(sut.EnterCheckpoint(321, 2));
            Assert.AreEqual(0, sut.CheckpointIndex);
            Assert.IsTrue(sut.EnterCheckpoint(321, 0));
            Assert.IsFalse(sut.EnterCheckpoint(321, 2));
            Assert.AreEqual(1, sut.CheckpointIndex);
        }

        [Test] public void E04_MultiColliderDuplicateNeverAdvances()
        {
            sut.Begin();
            Assert.IsTrue(sut.EnterCheckpoint(321, 0));
            Assert.IsFalse(sut.EnterCheckpoint(321, 0));
            Assert.AreEqual(1, sut.CheckpointIndex);
        }

        [Test] public void E05_WrongActorAndWrongFinishRejected()
        {
            sut.Begin();
            Assert.IsFalse(sut.EnterCheckpoint(999, 0));
            Assert.IsFalse(sut.EnterFinish(999));
            Assert.IsFalse(sut.Fail(999, CHL003CourseCore.Failure.FellOffCourse));
            Assert.AreEqual(0, sut.CheckpointIndex);
            Assert.AreEqual(CHL003CourseCore.Phase.Running, sut.State);
        }

        [Test] public void E06_ThreeRetriesNoStaleEventsOrElapsed()
        {
            int wins = 0;
            sut.Completed += _ => wins++;
            for (int round = 0; round < 3; round++)
            {
                sut.Retry();
                Assert.AreEqual(0f, sut.ElapsedSeconds);
                Assert.AreEqual(0, sut.CheckpointIndex);
                Assert.AreEqual(CHL003CourseCore.Phase.Ready, sut.State);
                sut.Begin();
                sut.AdvanceTime(2f);
                for (int n = 0; n < 4; n++) sut.EnterCheckpoint(321, n);
                Assert.IsTrue(sut.EnterFinish(321));
                Assert.IsFalse(sut.EnterFinish(321));
            }
            Assert.AreEqual(3, wins);
            sut.Cancel();
            Assert.AreEqual(0, sut.CheckpointIndex);
            Assert.AreEqual(0f, sut.ElapsedSeconds);
        }

        [Test] public void E07_PauseAndResumeFreezeClockAndGates()
        {
            sut.Begin();
            sut.AdvanceTime(1.25f);
            sut.SetPaused(true);
            sut.AdvanceTime(17f);
            Assert.IsFalse(sut.EnterCheckpoint(321, 0));
            Assert.AreEqual(1.25f, sut.ElapsedSeconds);
            sut.SetPaused(false);
            Assert.IsTrue(sut.EnterCheckpoint(321, 0));
            sut.AdvanceTime(.25f);
            Assert.AreEqual(1.5f, sut.ElapsedSeconds);
        }

        [Test] public void E08_FallFailsOnceAndRetryClearsFailure()
        {
            int failed = 0;
            sut.Failed += _ => failed++;
            sut.Begin();
            Assert.IsTrue(sut.Fail(321, CHL003CourseCore.Failure.FellOffCourse));
            Assert.IsFalse(sut.Fail(321, CHL003CourseCore.Failure.FellOffCourse));
            Assert.IsFalse(sut.EnterCheckpoint(321, 0));
            Assert.AreEqual(1, failed);
            sut.Retry();
            Assert.AreEqual(CHL003CourseCore.Failure.None, sut.LastFailure);
            Assert.AreEqual(CHL003CourseCore.Phase.Ready, sut.State);
        }

        [Test] public void E09_TimeoutStopsAtExactDeadline()
        {
            sut.Configure(3, 321, timeLimitSeconds: 2f);
            sut.Begin(); sut.AdvanceTime(1.5f); sut.AdvanceTime(1.0f);
            Assert.AreEqual(CHL003CourseCore.Phase.Failed, sut.State);
            Assert.AreEqual(CHL003CourseCore.Failure.TimeLimit, sut.LastFailure);
            Assert.AreEqual(2f, sut.ElapsedSeconds);
        }

        [Test] public void E10_InvalidConfigurationAndNegativeTimeFailClosed()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => sut.Configure(2, 321));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => sut.Configure(6, 321));
            Assert.Throws<System.ArgumentException>(() => sut.Configure(4, 0));
            sut.Begin();
            Assert.Throws<System.InvalidOperationException>(() => sut.Configure(4, 321));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => sut.AdvanceTime(-1));
            Assert.AreEqual(0f, sut.ElapsedSeconds);
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.RAC001.Tests
{
    public class CheckpointRaceStateTests
    {
        private static Vector3 At(float x, float z = 0f) => new Vector3(x, 0f, z);

        [Test]
        public void StartInsideCheckpointRequiresExitAndReentry()
        {
            var state = new CheckpointRaceState();
            Assert.IsTrue(state.Begin(At(5), new[] { At(5) }, 1f, 30f));
            Assert.AreEqual(RaceStepResult.None, state.Advance(At(5), 0.1f));
            Assert.AreEqual(RaceStepResult.None, state.Advance(At(8), 0.1f));
            Assert.AreEqual(RaceStepResult.Won, state.Advance(At(5), 0.1f));
        }

        [Test]
        public void OutOfOrderCheckpointDoesNotAdvance()
        {
            var state = new CheckpointRaceState();
            Assert.IsTrue(state.Begin(At(0), new[] { At(5), At(10) }, 1f, 30f));
            state.Advance(At(10, 10), 0.2f);
            state.Advance(At(10), 0.2f);
            Assert.AreEqual(0, state.NextCheckpoint);
            Assert.AreEqual(RaceStepResult.Checkpoint, state.Advance(At(5), 0.2f));
            Assert.AreEqual(RaceStepResult.Won, state.Advance(At(10), 0.2f));
        }

        [Test]
        public void SweptSegmentRegistersHighSpeedCrossing()
        {
            var state = new CheckpointRaceState();
            state.Begin(At(0), new[] { At(5) }, 1f, 30f);
            Assert.AreEqual(RaceStepResult.Won, state.Advance(At(10), 0.1f));
        }

        [Test]
        public void ImplausibleTeleportCannotScoreCheckpoint()
        {
            var state = new CheckpointRaceState();
            state.Begin(At(0), new[] { At(100) }, 1f, 30f, 20f);
            Assert.AreEqual(RaceStepResult.None, state.Advance(At(100), 0.1f));
            Assert.AreEqual(RaceStepResult.None, state.Advance(At(100), 0.1f));
            Assert.AreEqual(0, state.NextCheckpoint);
            Assert.AreEqual(RaceStepResult.None, state.Advance(At(103), 0.2f));
            Assert.AreEqual(RaceStepResult.Won, state.Advance(At(100), 0.2f));
        }

        [Test]
        public void TimeoutAtDeadlineBeatsCheckpoint()
        {
            var state = new CheckpointRaceState();
            state.Begin(At(0), new[] { At(5) }, 1f, 1f);
            Assert.AreEqual(RaceStepResult.TimedOut, state.Advance(At(5), 1f));
            Assert.AreEqual(RacePhase.TimedOut, state.Phase);
            Assert.AreEqual(RaceStepResult.None, state.Advance(At(5), 0.1f));
        }

        [Test]
        public void InvalidCourseAndNumbersCannotBegin()
        {
            var state = new CheckpointRaceState();
            Assert.IsFalse(state.Begin(At(0), new Vector3[0], 1f, 30f));
            Assert.IsFalse(state.Begin(At(0), new[] { At(5) }, 0f, 30f));
            Assert.IsFalse(state.Begin(At(0), new[] { At(5) }, 1f, float.NaN));
            Assert.IsFalse(state.Begin(At(0), new[] { At(5) }, 1f, 30f, 0f));
            Assert.AreEqual(RacePhase.Idle, state.Phase);
        }

        [Test]
        public void OverlappingCheckpointsRequireFreshExit()
        {
            var state = new CheckpointRaceState();
            state.Begin(At(0), new[] { At(5), At(5) }, 1f, 30f);
            Assert.AreEqual(RaceStepResult.Checkpoint, state.Advance(At(5), 0.1f));
            Assert.AreEqual(RaceStepResult.None, state.Advance(At(5), 0.1f));
            Assert.AreEqual(RaceStepResult.None, state.Advance(At(8), 0.1f));
            Assert.AreEqual(RaceStepResult.Won, state.Advance(At(5), 0.1f));
        }

        [Test]
        public void ResetClearsRaceAndPermitsNewAttempt()
        {
            var state = new CheckpointRaceState();
            state.Begin(At(0), new[] { At(5) }, 1f, 30f);
            Assert.AreEqual(RaceStepResult.Won, state.Advance(At(5), 0.1f));
            state.Reset();
            Assert.AreEqual(RacePhase.Idle, state.Phase);
            Assert.AreEqual(0, state.NextCheckpoint);
            Assert.AreEqual(0f, state.ElapsedSeconds);
            Assert.IsTrue(state.Begin(At(0), new[] { At(5) }, 1f, 30f));
        }

        [Test]
        public void InvalidInputInvalidatesOnce()
        {
            var state = new CheckpointRaceState();
            state.Begin(At(0), new[] { At(5) }, 1f, 30f);
            Assert.AreEqual(RaceStepResult.Invalidated, state.Advance(At(1), -0.1f));
            Assert.AreEqual(RaceStepResult.None, state.Advance(At(5), 0.1f));
        }
        [Test]
        public void ZeroTimeTicksCannotMoveOrScoreWithoutElapsedTime()
        {
            var state = new CheckpointRaceState();
            Assert.IsTrue(state.Begin(At(0), new[] { At(5) }, 0.2f, 30f, 1f));
            for (int i = 1; i <= 50; i++)
                Assert.AreEqual(RaceStepResult.None, state.Advance(At(i * 0.1f), 0f));
            Assert.AreEqual(0, state.NextCheckpoint);
            Assert.AreEqual(0f, state.ElapsedSeconds);
            Assert.AreEqual(RacePhase.Running, state.Phase);
            // A subsequent positive-time tick compares against the original start.
            Assert.AreEqual(RaceStepResult.None, state.Advance(At(5), 0.1f));
            Assert.AreEqual(0, state.NextCheckpoint);
        }

        [Test]
        public void TinyPositiveTicksCannotExploitPerStepDistanceAllowance()
        {
            var state = new CheckpointRaceState();
            Assert.IsTrue(state.Begin(At(0), new[] { At(0.4f) }, 0.05f, 30f, 1f));
            for (int i = 1; i <= 4; i++)
                Assert.AreEqual(RaceStepResult.None, state.Advance(At(i * 0.1f), 0.00001f));
            Assert.AreEqual(0, state.NextCheckpoint);
            Assert.AreEqual(RacePhase.Running, state.Phase);
        }

    }
}

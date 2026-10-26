using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Mechanics.RAC001.Tests
{
    public class CheckpointRacePlayModeTests
    {
        [UnityTest]
        public IEnumerator EventFiresExactlyOnceForWinningCheckpoint()
        {
            var host = new GameObject("RAC001 Test Host");
            var runner = new GameObject("RAC001 Test Runner");
            var point = new GameObject("RAC001 Test Checkpoint");
            try
            {
                runner.transform.position = Vector3.zero;
                point.transform.position = new Vector3(5f, 0f, 0f);
                var race = host.AddComponent<CheckpointRaceChallenge>();
                race.Configure(runner.transform, new[] { point.transform }, 1f, 30f);
                int checkpointEvents = 0, winEvents = 0;
                race.OnCheckpoint.AddListener(() => checkpointEvents++);
                race.OnWon.AddListener(() => winEvents++);
                Assert.IsTrue(race.Begin());
                runner.transform.position = point.transform.position;
                Assert.AreEqual(RaceStepResult.Won, race.Advance(0.1f));
                Assert.AreEqual(RaceStepResult.None, race.Advance(0.1f));
                yield return null;
                Assert.AreEqual(1, checkpointEvents);
                Assert.AreEqual(1, winEvents);
            }
            finally
            {
                Object.Destroy(host);
                Object.Destroy(runner);
                Object.Destroy(point);
            }
        }

        [UnityTest]
        public IEnumerator TimeoutEventAndResetAreSafe()
        {
            var host = new GameObject("RAC001 Test Host");
            var runner = new GameObject("RAC001 Test Runner");
            var point = new GameObject("RAC001 Test Checkpoint");
            try
            {
                point.transform.position = new Vector3(5f, 0f, 0f);
                var race = host.AddComponent<CheckpointRaceChallenge>();
                race.Configure(runner.transform, new[] { point.transform }, 1f, 1f);
                int timedOut = 0;
                race.OnTimedOut.AddListener(() => timedOut++);
                Assert.IsTrue(race.Begin());
                Assert.AreEqual(RaceStepResult.TimedOut, race.Advance(1f));
                Assert.AreEqual(RaceStepResult.None, race.Advance(1f));
                Assert.AreEqual(1, timedOut);
                race.ResetRace();
                Assert.AreEqual(RacePhase.Idle, race.Phase);
                yield return null;
            }
            finally
            {
                Object.Destroy(host);
                Object.Destroy(runner);
                Object.Destroy(point);
            }
        }

        [UnityTest]
        public IEnumerator MissingRunnerCannotStart()
        {
            var host = new GameObject("RAC001 Test Host");
            var point = new GameObject("RAC001 Test Checkpoint");
            try
            {
                var race = host.AddComponent<CheckpointRaceChallenge>();
                race.Configure(null, new[] { point.transform }, 1f, 30f);
                Assert.IsFalse(race.Begin());
                Assert.AreEqual(RacePhase.Idle, race.Phase);
                yield return null;
            }
            finally
            {
                Object.Destroy(host);
                Object.Destroy(point);
            }
        }
    }
}


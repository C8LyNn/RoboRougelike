using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.Mechanics.OBJ001.Tests
{
    public sealed class ReactorDefensePlayModeTests
    {
        [UnityTest]
        public IEnumerator AutomaticClockCompletesDefense()
        {
            var root = new GameObject("OBJ001 auto clock");
            var reactor = root.AddComponent<ReactorDefenseObjective>();
            Assert.IsTrue(reactor.Configure(100f, 0.05f));
            Assert.IsTrue(reactor.Begin());
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(DefenseState.Won, reactor.State);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator DisabledObjectPausesCountdown()
        {
            var root = new GameObject("OBJ001 disabled");
            var reactor = root.AddComponent<ReactorDefenseObjective>();
            Assert.IsTrue(reactor.Configure(100f, 0.1f));
            Assert.IsTrue(reactor.Begin());
            root.SetActive(false);
            float elapsed = reactor.ElapsedSeconds;
            yield return new WaitForSeconds(0.15f);
            Assert.AreEqual(elapsed, reactor.ElapsedSeconds, 0.001f);
            Assert.AreEqual(DefenseState.Defending, reactor.State);
            root.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(DefenseState.Won, reactor.State);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator MultipleReactorsHaveIndependentState()
        {
            var first = new GameObject("OBJ001 A");
            var second = new GameObject("OBJ001 B");
            var a = first.AddComponent<ReactorDefenseObjective>();
            var b = second.AddComponent<ReactorDefenseObjective>();
            a.Configure(100f, 20f);
            b.Configure(50f, 20f);
            a.SetAutomaticClock(false);
            b.SetAutomaticClock(false);
            a.Begin();
            b.Begin();
            Assert.IsTrue(a.ApplyDamage(100f));
            Assert.AreEqual(DefenseState.Lost, a.State);
            Assert.AreEqual(DefenseState.Defending, b.State);
            Assert.AreEqual(50f, b.Integrity);
            yield return null;
            Object.Destroy(first);
            Object.Destroy(second);
        }
    }
}

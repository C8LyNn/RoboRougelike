using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoboRougelike.B0132P17.RepairWeld.Tests
{
    public sealed class RepairWeldPlayModeTests
    {
        [UnityTest]
        public IEnumerator ActualPrefab_StartCancelOff_ClearsPooledLines()
        {
            var source = Resources.Load<GameObject>("B0132_RepairWeld");
            Assert.IsNotNull(source, "Original packaged prefab must import in Resources");
            var sceneInstance = Object.Instantiate(source);
            var fx = sceneInstance.GetComponent<B0132RepairWeldVfx>();
            Assert.IsNotNull(fx);
            Assert.AreEqual(9, sceneInstance.GetComponentsInChildren<LineRenderer>().Length);
            fx.BeginWeld(1337u);
            Assert.IsTrue(fx.IsVisualActive);
            fx.SetVisualProgress(0.5f);
            yield return null;
            Assert.AreEqual(WeldPhase.Welding, fx.Phase);
            fx.CancelWeld();
            Assert.IsFalse(fx.IsVisualActive);
            foreach (var line in sceneInstance.GetComponentsInChildren<LineRenderer>())
                Assert.IsFalse(line.enabled, "Cancelled weld must have no stale sparks");
            Object.Destroy(sceneInstance);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledVisuals_CancelCurrentAndRequireExplicitRestart()
        {
            var source = Resources.Load<GameObject>("B0132_RepairWeld");
            Assert.IsNotNull(source);
            var go = Object.Instantiate(source);
            var fx = go.GetComponent<B0132RepairWeldVfx>();
            fx.BeginWeld(444u);
            fx.SetVisualsEnabled(false);
            Assert.AreEqual(WeldPhase.Idle, fx.Phase);
            Assert.IsFalse(fx.IsVisualActive);
            foreach (var line in go.GetComponentsInChildren<LineRenderer>())
                Assert.IsFalse(line.enabled);
            fx.SetVisualsEnabled(true);
            Assert.IsFalse(fx.IsVisualActive, "ON alone must not resurrect an old repair");
            fx.BeginWeld(445u);
            Assert.IsTrue(fx.IsVisualActive);
            fx.FinishWeld();
            Assert.AreEqual(WeldPhase.Cooling, fx.Phase);
            Object.Destroy(go);
            yield return null;
        }
    }
}

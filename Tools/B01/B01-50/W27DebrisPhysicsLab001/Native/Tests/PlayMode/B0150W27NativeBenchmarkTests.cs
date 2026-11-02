// Authoritative Unity Test Framework code: NOT_RUN without local licensed Unity.
using System.Collections;
using System.IO;
using B01.Performance.W27DebrisLab;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.Performance.W27DebrisLab.NativeTests
{
    public sealed class B0150W27NativeBenchmarkTests
    {
        [UnityTest]
        public IEnumerator DefaultSceneComponentDoesNotStartBenchmarkWithoutOperatorAction()
        {
            var obj = new GameObject("B0150_W27_no_auto_execute");
            try
            {
                var lab = obj.AddComponent<B0150W27DebrisPhysicsLab>();
                yield return null;
                Assert.IsFalse(lab.IsRunning);
                Assert.IsNull(lab.LastEvidencePath);
            }
            finally { Object.Destroy(obj); }
        }

        [UnityTest]
        public IEnumerator RealPhysicsFullAbbaMatrixProducesReviewableEvidenceOrFailsExplicitly()
        {
            var obj = new GameObject("B0150_W27_native_full_matrix");
            try
            {
                var lab = obj.AddComponent<B0150W27DebrisPhysicsLab>();
                // Opt-in manual QA action: explicitly start in native PlayMode.
                lab.RunBenchmark();
                Assert.IsTrue(lab.IsRunning, "Native test never started");
                int elapsedFixedTicks = 0;
                // Hard timeout must FAIL, never silently claim an incomplete pass.
                while (lab.IsRunning && elapsedFixedTicks < 28000)
                {
                    ++elapsedFixedTicks;
                    yield return new WaitForFixedUpdate();
                }
                Assert.IsFalse(lab.IsRunning, "Native 10/100/500 ABBA trial timed out");
                Assert.AreEqual("SOURCE_CAPTURE_ONLY_AWAITING_G1_REVIEW",
                    lab.LastRunStatus, "Native witness/active-safety issue: G1 HOLD");
                Assert.IsNotNull(lab.LastEvidencePath, "No original native raw CSV");
                Assert.IsTrue(File.Exists(lab.LastEvidencePath),
                    "Original native evidence was not persisted to local temp");
                Assert.Greater(new FileInfo(lab.LastEvidencePath).Length, 1000L);
                // This native test's PASS is a candidate for independent B01-60 QA,
                // NOT a claim that Unity built-in sleep was outperformed.
            }
            finally { Object.Destroy(obj); }
        }
    }
}

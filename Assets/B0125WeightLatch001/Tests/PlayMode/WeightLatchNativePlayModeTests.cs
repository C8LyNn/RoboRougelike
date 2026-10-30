using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.LevelChallenges.WeightLatch;

namespace B0125.WeightLatch.Tests
{
    public sealed class WeightLatchNativePlayModeTests
    {
        [UnityTest]
        public IEnumerator ActualPhysicalDoorAndHeavyCapsuleExist()
        {
            var root=new GameObject("w20-real-gate");
            var demo=root.AddComponent<WeightLatchUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.GateBody.isKinematic);
            Assert.NotNull(demo.GateBody.GetComponent<BoxCollider>());
            Assert.IsTrue(demo.Vehicle.useGravity);
            Assert.AreEqual(1.5f,demo.Vehicle.mass,.001f);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator RealPressurePlateHeavyOpensGate()
        {
            var root=new GameObject("w20-real-heavy");
            var demo=root.AddComponent<WeightLatchUnityDemo>();
            demo.SetHeavy(true);
            yield return new WaitForFixedUpdate();
            demo.TeleportTo(new Vector3(0,1.1f,-3));
            for(int i=0;i<45;i++)yield return new WaitForFixedUpdate();
            Assert.Greater(demo.OpenFraction,0);
            Assert.Greater(demo.GateBody.position.y,1.3f);
            Assert.Greater(demo.ActivationCount,0);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator LightOnRealPlateCannotOpenHeavyLatch()
        {
            var root=new GameObject("w20-light-cannot-activate");
            var demo=root.AddComponent<WeightLatchUnityDemo>();
            demo.TeleportTo(new Vector3(0,1.1f,-3));
            for(int i=0;i<45;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(0,demo.OpenFraction,1e-7);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator PermanentPhysicalBypassNeverTriggersGate()
        {
            var root=new GameObject("w20-bypass");
            var demo=root.AddComponent<WeightLatchUnityDemo>();
            demo.SetHeavy(true);
            demo.TeleportTo(new Vector3(4,1.1f,-3));
            for(int i=0;i<30;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(0,demo.OpenFraction,1e-7);
            Assert.AreEqual(0,demo.ActivationCount);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator PauseAndRestartDoNotPreserveStaleOpenGate()
        {
            var root=new GameObject("w20-reset");
            var demo=root.AddComponent<WeightLatchUnityDemo>();
            demo.SetHeavy(true);
            demo.TeleportTo(new Vector3(0,1.1f,-3));
            for(int i=0;i<45;i++)yield return new WaitForFixedUpdate();
            demo.SetPaused(true);
            double before=demo.OpenFraction,time=demo.Clock;
            for(int i=0;i<8;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(before,demo.OpenFraction,1e-8);
            Assert.AreEqual(time,demo.Clock,1e-8);
            demo.Restart();
            Assert.AreEqual(0,demo.OpenFraction,1e-8);
            Assert.AreEqual(0,demo.ActivationCount);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator SourceInvalidTelemetryIsAtomicEvenWhenPaused()
        {
            var logic=new WeightLatchController(new GateConfig());
            logic.Advance(new GateInput(.02,0,-3,12,true));
            var before=logic.State;
            Assert.Throws<System.ArgumentException>(()=>
                logic.Advance(new GateInput(double.NaN,0,-3,12,true,true)));
            Assert.AreEqual(before.OpenFraction,logic.State.OpenFraction,1e-8);
            Assert.AreEqual(before.ElapsedSeconds,logic.State.ElapsedSeconds,1e-8);
            yield return null;
        }
    }
}

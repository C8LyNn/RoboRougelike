using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.LevelChallenges.CounterBridge;

namespace B0125.CounterBridge.Tests
{
    public sealed class CounterBridgeNativePlayModeTests
    {
        [UnityTest]
        public IEnumerator RealKinematicBridgeColliderAndGravityVehicleExist()
        {
            var root=new GameObject("R1 native physical");
            var demo=root.AddComponent<CounterBridgeUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.BridgeBody.isKinematic);
            Assert.IsTrue(demo.BridgeBody.GetComponent<BoxCollider>().enabled);
            Assert.IsTrue(demo.Vehicle.useGravity);
            Assert.AreEqual(1.5f,demo.Vehicle.mass,.001f);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator HeavyRigHasDistinctRealMassAndColliderEnvelope()
        {
            var root=new GameObject("R1 native mass");
            var demo=root.AddComponent<CounterBridgeUnityDemo>();
            yield return new WaitForFixedUpdate();
            float lightWidth=demo.Vehicle.GetComponent<Collider>().bounds.size.x;
            demo.SetHeavy(true);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(12f,demo.Vehicle.mass,.001f);
            Assert.Greater(demo.Vehicle.GetComponent<Collider>().bounds.size.x,lightWidth);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator RealColliderSupportTiltsActualKinematicBridge()
        {
            var root=new GameObject("R1 native contact");
            var demo=root.AddComponent<CounterBridgeUnityDemo>();
            yield return new WaitForFixedUpdate();
            demo.TeleportTo(new Vector3(0,1.1f,2));
            for(int i=0;i<30;i++)yield return new WaitForFixedUpdate();
            Assert.Greater(demo.AngleDegrees,0);
            Assert.Greater(demo.BridgeBody.rotation.eulerAngles.x,0);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator RealNorthernBypassNeverPressesPivot()
        {
            var root=new GameObject("R1 safe bypass");
            var demo=root.AddComponent<CounterBridgeUnityDemo>();
            yield return new WaitForFixedUpdate();
            demo.TeleportTo(new Vector3(4,1.1f,2));
            for(int i=0;i<15;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(0,demo.AngleDegrees,1e-7);
            Assert.AreEqual(0,demo.LastTorqueNm,1e-7);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator PauseFreezesClockAndRestartReturnsBridgeToLevel()
        {
            var root=new GameObject("R1 native pause reset");
            var demo=root.AddComponent<CounterBridgeUnityDemo>();
            yield return new WaitForFixedUpdate();
            demo.SetPaused(true);
            double old=demo.Clock;
            for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(old,demo.Clock,1e-7);
            demo.SetPaused(false);
            yield return new WaitForFixedUpdate();
            Assert.Greater(demo.Clock,old);
            demo.Restart();
            Assert.AreEqual(0,demo.Clock,1e-7);
            Assert.AreEqual(0,demo.AngleDegrees,1e-7);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator ControllerRejectsInvalidDataBeforeChangingState()
        {
            var c=new CounterBridgeController(new CounterBridgeConfig(1.5,3,250,45,90,18));
            c.Advance(new BridgeSample(.02,0,2,12,true));
            double angle=c.Last.AngleDeg,time=c.Clock;
            Assert.Throws<System.ArgumentException>(()=>
                c.Advance(new BridgeSample(double.NaN,0,2,12,true)));
            Assert.AreEqual(time,c.Clock,1e-8);
            Assert.AreEqual(angle,c.Last.AngleDeg,1e-8);
            yield return null;
        }
    }
}

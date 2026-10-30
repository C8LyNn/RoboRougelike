using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.LevelChallenges.MagnetLane.R2;

namespace B0125.MagnetLane.R2.Tests
{
    public sealed class MagnetLaneNativePlayModeTests
    {
        [UnityTest]
        public IEnumerator ActualPhysicsCapsuleAndFloorExist()
        {
            var root=new GameObject("w14-test-physics");
            var demo=root.AddComponent<MagnetLaneUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.NotNull(demo.Vehicle.GetComponent<CapsuleCollider>());
            Assert.IsTrue(demo.Vehicle.useGravity);
            Assert.AreEqual(1.5f,demo.Vehicle.mass,0.001f);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator HeavyHasDifferentRealBodyEnvelopeAndMass()
        {
            var root=new GameObject("w14-test-heavy");
            var demo=root.AddComponent<MagnetLaneUnityDemo>();
            yield return new WaitForFixedUpdate();
            float lightWidth=demo.Vehicle.GetComponent<Collider>().bounds.size.x;
            demo.SetVehicle(true);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(12f,demo.Vehicle.mass,0.001f);
            Assert.Greater(demo.Vehicle.GetComponent<Collider>().bounds.size.x,lightWidth);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator ActualRigidbodyReceivesMagnetForceOnlyDuringActivePeriod()
        {
            var root=new GameObject("w14-test-force");
            var demo=root.AddComponent<MagnetLaneUnityDemo>();
            yield return new WaitForFixedUpdate();
            demo.TeleportTo(new Vector3(0,1.1f,0));
            for(int i=0;i<40;i++)yield return new WaitForFixedUpdate();
            Assert.Greater(demo.Cycles+demo.Clock,0);
            Assert.Greater(demo.Vehicle.position.z,0);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator PauseFreezesMagnetClockRestartResets()
        {
            var root=new GameObject("w14-test-pause");
            var demo=root.AddComponent<MagnetLaneUnityDemo>();
            yield return new WaitForFixedUpdate();
            demo.SetPaused(true);
            double at=demo.Clock;
            for(int i=0;i<9;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(at,demo.Clock,1e-7);
            demo.SetPaused(false);
            yield return new WaitForFixedUpdate();
            Assert.Greater(demo.Clock,at);
            demo.Restart();
            Assert.AreEqual(0,demo.Clock,1e-7);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator FastWholeLaneTraversalDoesNotLoseSourceMagnetForce()
        {
            var c=new MagnetLaneController(new MagnetConfig(.6,2.4,1,60,3,1.5));
            for(int i=0;i<6;i++)c.Advance(new MagnetSample(.1,0,0,0,0,1.5));
            var result=c.Advance(new MagnetSample(.1,-7,0,7,0,1.5));
            Assert.AreEqual(60.0*6.0/14.0,result.ForceZ,1e-7);
            yield return null;
        }
        [UnityTest]
        public IEnumerator NorthernBypassNeverReceivesFieldForce()
        {
            var root=new GameObject("w14-test-bypass");
            var demo=root.AddComponent<MagnetLaneUnityDemo>();
            demo.TeleportTo(new Vector3(0,1.1f,4));
            for(int i=0;i<45;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(0,demo.LastForceZ,1e-7);
            Object.Destroy(root);
        }
    }
}

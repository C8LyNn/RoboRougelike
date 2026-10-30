using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.LevelChallenges.Airlock;

namespace B0125.Airlock.Tests
{
    public sealed class AirlockNativePlayModeTests
    {
        [UnityTest]
        public IEnumerator TwoRealMovingRigidbodyDoorsAndPhysicsVehicleExist()
        {
            var host=new GameObject("airlock native double gate");
            var d=host.AddComponent<AirlockUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(d.EntryGate.isKinematic);
            Assert.IsTrue(d.ExitGate.isKinematic);
            Assert.NotNull(d.EntryGate.GetComponent<BoxCollider>());
            Assert.NotNull(d.ExitGate.GetComponent<BoxCollider>());
            Assert.IsTrue(d.Vehicle.useGravity);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator LightAndHeavyHaveDifferentRealRigidbodyMass()
        {
            var host=new GameObject("airlock native mass");
            var d=host.AddComponent<AirlockUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(1.5f,d.Vehicle.mass,.001f);
            d.SetHeavy(true);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(12f,d.Vehicle.mass,.001f);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator ActualApproachSensorOpensEntryButNotExit()
        {
            var host=new GameObject("airlock native approach");
            var d=host.AddComponent<AirlockUnityDemo>();
            d.TeleportTo(new Vector3(0,1.1f,-4));
            for(int i=0;i<48;i++)yield return new WaitForFixedUpdate();
            Assert.Greater(d.Current.EntryOpen,.9);
            Assert.AreEqual(0,d.Current.ExitOpen,1e-6);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator TruePhysicalChamberEventuallyOpensOnlyExit()
        {
            var host=new GameObject("airlock chamber cycle");
            var d=host.AddComponent<AirlockUnityDemo>();
            d.TeleportTo(new Vector3(0,1.1f,-4));
            for(int i=0;i<48;i++)yield return new WaitForFixedUpdate();
            d.TeleportTo(new Vector3(0,1.1f,0));
            for(int i=0;i<155;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(0,d.Current.EntryOpen,1e-6);
            Assert.Greater(d.Current.ExitOpen,.9);
            Assert.IsFalse(d.Current.EntryOpen>0&&d.Current.ExitOpen>0);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator EmergencyCannotOpenBothPhysicalDoors()
        {
            var host=new GameObject("airlock emergency");
            var d=host.AddComponent<AirlockUnityDemo>();
            d.TeleportTo(new Vector3(0,1.1f,-4));
            for(int i=0;i<48;i++)yield return new WaitForFixedUpdate();
            d.TeleportTo(new Vector3(0,1.1f,0));
            for(int i=0;i<155;i++)yield return new WaitForFixedUpdate();
            d.RequestEmergency();
            for(int i=0;i<100;i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.IsFalse(d.Current.EntryOpen>0&&d.Current.ExitOpen>0);
            }
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator PausedAndResetPreserveNoStaleAuthority()
        {
            var host=new GameObject("airlock pause restart");
            var d=host.AddComponent<AirlockUnityDemo>();
            yield return new WaitForFixedUpdate();
            d.SetPaused(true);
            double old=d.Current.Clock;
            for(int i=0;i<12;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(old,d.Current.Clock,1e-7);
            d.Restart();
            Assert.AreEqual(0,d.Current.Clock,1e-7);
            Assert.AreEqual(AirlockPhase.Idle,d.Current.Phase);
            Object.Destroy(host);
        }
    }
}

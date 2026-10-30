using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.LevelChallenges.Conveyor;

namespace B0125.Conveyor.Tests
{
    public sealed class ConveyorNativePlayModeTests
    {
        [UnityTest]
        public IEnumerator RealBeltColliderAndGravityRigidbodyExist()
        {
            var root=new GameObject("Conveyor physical source");
            var demo=root.AddComponent<ConveyorUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.NotNull(demo.BeltCollider);
            Assert.IsTrue(demo.BeltCollider is BoxCollider);
            Assert.IsTrue(demo.Vehicle.useGravity);
            Assert.AreEqual(1.5f,demo.Vehicle.mass,.001f);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator ActualBeltContactAppliesPositiveForce()
        {
            var root=new GameObject("Conveyor contact test");
            var demo=root.AddComponent<ConveyorUnityDemo>();
            demo.TeleportTo(new Vector3(0,1.1f,0));
            for(int i=0;i<8;i++)yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.Supported);
            Assert.Greater(demo.ForceNewtons,0);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator PermanentSafeBypassNeverGrantsBeltForce()
        {
            var root=new GameObject("Conveyor bypass");
            var demo=root.AddComponent<ConveyorUnityDemo>();
            demo.TeleportTo(new Vector3(4,1.1f,0));
            for(int i=0;i<12;i++)yield return new WaitForFixedUpdate();
            Assert.IsFalse(demo.Supported);
            Assert.AreEqual(0,demo.ForceNewtons,1e-7);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator PhysicallyDifferentHeavyVehicleMass()
        {
            var root=new GameObject("Conveyor heavy mass");
            var demo=root.AddComponent<ConveyorUnityDemo>();
            yield return new WaitForFixedUpdate();
            float lightWidth=demo.Vehicle.GetComponent<Collider>().bounds.size.x;
            demo.SetHeavy(true);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(12f,demo.Vehicle.mass,.001f);
            Assert.Greater(demo.Vehicle.GetComponent<Collider>().bounds.size.x,lightWidth);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator RealBeltReversesAfterPhysicallyAdvancingClock()
        {
            var root=new GameObject("Conveyor reverse");
            var demo=root.AddComponent<ConveyorUnityDemo>();
            demo.TeleportTo(new Vector3(0,1.1f,0));
            for(int i=0;i<155;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(BeltPhase.Reverse,demo.Phase);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator PauseAndRestartCannotKeepStaleBeltClock()
        {
            var root=new GameObject("Conveyor pause");
            var demo=root.AddComponent<ConveyorUnityDemo>();
            yield return new WaitForFixedUpdate();
            demo.SetPaused(true);
            double before=demo.Clock;
            for(int i=0;i<15;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(before,demo.Clock,1e-7);
            demo.Restart();
            Assert.AreEqual(0,demo.Clock,1e-7);
            Assert.AreEqual(0,demo.ForceNewtons,1e-7);
            Object.Destroy(root);
        }
    }
}

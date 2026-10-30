using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B0125.SinkPlate.Tests
{
    public sealed class SinkPlateNativePlayModeTests
    {
        [UnityTest]
        public IEnumerator OriginalRealPhysicalFloorAndBody()
        {
            var root=new GameObject("sink-native-first");
            var demo=root.AddComponent<SinkPlateUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.NotNull(demo.Vehicle);
            Assert.IsNotNull(demo.Vehicle.GetComponent<CapsuleCollider>());
            Assert.IsTrue(demo.PlateCollider.enabled);
            Assert.AreEqual(1.5f,demo.Vehicle.mass,.01f);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator HeavyReallyHasLargerColliderAndFasterWarning()
        {
            var root=new GameObject("sink-native-heavy");
            var demo=root.AddComponent<SinkPlateUnityDemo>();
            yield return new WaitForFixedUpdate();
            float width=demo.Vehicle.GetComponent<Collider>().bounds.size.x;
            demo.SetVehicle(true);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(12f,demo.Vehicle.mass,.01f);
            Assert.Greater(demo.Vehicle.GetComponent<Collider>().bounds.size.x,width);
            demo.MoveVehicleTo(new Vector3(0,1.1f,0));
            for(int i=0;i<24;i++)yield return new WaitForFixedUpdate();
            Assert.GreaterOrEqual(demo.Collapses,1);
            Assert.IsFalse(demo.PlateCollider.enabled);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator PauseStopsRealCollapseClockAndResetRestoresFloor()
        {
            var root=new GameObject("sink-native-pause");
            var demo=root.AddComponent<SinkPlateUnityDemo>();
            demo.SetVehicle(true);
            yield return new WaitForFixedUpdate();
            demo.MoveVehicleTo(new Vector3(0,1.1f,0));
            yield return new WaitForFixedUpdate();
            demo.SetPaused(true);
            double before=demo.PhaseTime;
            for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(before,demo.PhaseTime,1e-7);
            demo.SetPaused(false);
            for(int i=0;i<25;i++)yield return new WaitForFixedUpdate();
            Assert.GreaterOrEqual(demo.Collapses,1);
            demo.Restart();
            Assert.AreEqual(0,demo.Collapses);
            Assert.IsTrue(demo.PlateCollider.enabled);
            Object.Destroy(root);
        }
        [UnityTest]
        public IEnumerator SafeNorthernBypassDoesNotCollapse()
        {
            var root=new GameObject("sink-native-bypass");
            var demo=root.AddComponent<SinkPlateUnityDemo>();
            yield return new WaitForFixedUpdate();
            demo.MoveVehicleTo(new Vector3(0,1.1f,4f));
            for(int i=0;i<50;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(0,demo.Collapses);
            Assert.IsTrue(demo.PlateCollider.enabled);
            Object.Destroy(root);
        }
    }
}

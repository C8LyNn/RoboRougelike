using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using B01.LevelChallenges.SafeRoute;

namespace B0125.SafeRoute.Tests
{
    public sealed class SafeRouteNativePlayModeTests
    {
        [UnityTest]
        public IEnumerator OriginalPhysicallyBlockedWallsAndThreeKinematicGatesExist()
        {
            var host=new GameObject("R1 physical world");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.Greater(demo.WallCount,0);
            Assert.AreEqual(3,demo.GateCount);
            Assert.AreEqual(2,demo.HazardPadCount);
            Assert.IsTrue(demo.Player.useGravity);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator LightClosedGateHasRealColliderClearDetour()
        {
            var host=new GameObject("R1 light actual route");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Assert.IsTrue(demo.PhysicallyClearCurrentPlan());
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator HeavyClosedGateHasConservativeRealPhysicalDetour()
        {
            var host=new GameObject("R1 heavy actual route");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            demo.SetHeavy(true);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(12f,demo.Player.mass,.001f);
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Assert.IsTrue(demo.PhysicallyClearCurrentPlan());
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator AllEightOriginalConditionsAreStaticallySafe()
        {
            var host=new GameObject("R1 eight states");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.Audit.AllReachable);
            Assert.AreEqual(8,demo.Audit.Cases.Length);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator RealGateOpeningNeverRemovesSafeDetour()
        {
            var host=new GameObject("R1 moving gate");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            demo.ToggleGate();
            for(int i=0;i<80;i++)yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.GatePhysicallyClear);
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Assert.IsTrue(demo.PhysicallyClearCurrentPlan());
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator TogglingPhysicalHazardKeepsLightAndHeavyRouteReachable()
        {
            var host=new GameObject("R1 hazard toggle");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            demo.ToggleHazard();
            Assert.IsFalse(demo.HazardActive);
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            demo.SetHeavy(true);
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            demo.ToggleHazard();
            Assert.IsTrue(demo.Audit.AllReachable);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W24ActualPlayerTeleportReplansFromPhysicalPosition()
        {
            var host=new GameObject("R2 actual moving origin");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            var first=demo.CurrentPlan;
            Assert.IsTrue(first.Reachable);
            var next=first.Cells[1];
            demo.TeleportTo(next);
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(next,demo.RouteOrigin);
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Assert.IsTrue(demo.CurrentRouteFromPlayer);
            Assert.LessOrEqual(demo.CurrentPlan.Cells.Length,first.Cells.Length);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W24TeleportOutsideMapNeverProvidesStaleRoute()
        {
            var host=new GameObject("R2 out of map must fail closed");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            // This is intentionally impossible to navigate; not a player spawn.
            demo.Player.position=new Vector3(-200,1.2f,-200);
            demo.Player.transform.position=demo.Player.position;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(demo.CurrentPlan.Reachable);
            Assert.IsFalse(demo.CurrentRouteFromPlayer);
            Object.Destroy(host);
        }
    }
}

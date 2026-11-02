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
        [UnityTest]
        public IEnumerator W25DynamicCrateReallyHasRigidbodyBoxColliderAndGridHit()
        {
            var host=new GameObject("Real R3 physical movable obstacle");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.PhysicalCrate.isKinematic);
            Assert.NotNull(demo.PhysicalCrate.GetComponent<BoxCollider>());
            Assert.AreEqual(new GridCell(3,8),demo.CrateCell);
            Assert.Greater(demo.PhysicalBlockedCells,0);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W25DangerousMoveNeverChangesPhysicsOrMintsFalseRoute()
        {
            var host=new GameObject("R3 reject heavy softlock");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            Vector3 prior=demo.PhysicalCrate.position;
            Assert.IsFalse(demo.TryMoveCrate(new GridCell(7,8)));
            Assert.AreEqual(prior,demo.PhysicalCrate.position);
            Assert.Greater(demo.RejectedCrateMoves,0);
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W25SafeMoveUpdatesRealWorldPhysicalOccupancy()
        {
            var host=new GameObject("R3 safe move");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.TryMoveCrate(new GridCell(4,4)));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(new GridCell(4,4),demo.CrateCell);
            Assert.Greater(demo.PhysicalBlockedCells,0);
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Assert.IsTrue(demo.PhysicallyClearCurrentPlan());
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W25ExternalUntrustedPhysicsMoveFailsClosedForHeavy()
        {
            var host=new GameObject("R3 unguarded external movement");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            demo.SetHeavy(true);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Vector3 prior=demo.PhysicalCrate.position;
            Vector3 danger=prior+new Vector3(8f,0,0);
            demo.PhysicalCrate.position=danger;
            demo.PhysicalCrate.transform.position=danger;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(new GridCell(7,8),demo.CrateCell);
            Assert.IsFalse(demo.CurrentPlan.Reachable);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W27TwoOriginalRealPhysicalCratesOccupyDistinctCells()
        {
            var host=new GameObject("R4 two distinct physical BoxCollider obstacles");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.AuxiliaryCrate.isKinematic);
            Assert.NotNull(demo.AuxiliaryCrate.GetComponent<BoxCollider>());
            Assert.AreEqual(new GridCell(11,8),demo.AuxiliaryCrateCell);
            Assert.GreaterOrEqual(demo.PhysicalBlockedCells,2);
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W27TwoSoloSafeCratesCombinedHeavySoftlockIsRejected()
        {
            var host=new GameObject("R4 no combined Heavy softlock");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            var before=demo.AuxiliaryCrate.position;
            Assert.IsFalse(demo.TryMoveAuxiliaryCrate(new GridCell(5,5)));
            Assert.AreEqual(before,demo.AuxiliaryCrate.position);
            Assert.Greater(demo.RejectedCrateMoves,0);
            demo.SetHeavy(true);
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W27SafeMoveOfSecondPhysicalCrateKeepsHeavyRoute()
        {
            var host=new GameObject("R4 safe composite crate relocation");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            demo.SetHeavy(true);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.TryMoveAuxiliaryCrate(new GridCell(4,4)));
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(new GridCell(4,4),demo.AuxiliaryCrateCell);
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Assert.IsTrue(demo.PhysicallyClearCurrentPlan());
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W27UnexpectedSecondCrateMoveFailsClosedWithoutFalseGuidance()
        {
            var host=new GameObject("R4 external physical obstacle movement");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            demo.SetHeavy(true);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            var old=demo.AuxiliaryCrate.position;
            // Cell (11,8) -> (5,5) in original 2m grid coordinates.
            var moved=old+new Vector3(-12f,0,-6f);
            demo.AuxiliaryCrate.position=moved;
            demo.AuxiliaryCrate.transform.position=moved;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(new GridCell(5,5),demo.AuxiliaryCrateCell);
            Assert.IsFalse(demo.CurrentPlan.Reachable);
            Assert.IsFalse(demo.CurrentRouteFromPlayer);
            Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W27SubcellObstacleMoveTriggersRealPhysicalReplan()
        {
            var host=new GameObject("R4 subcell pose tracking");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            int before=demo.ReplanRevision;
            Vector3 p=demo.AuxiliaryCrate.position+new Vector3(.14f,0,0);
            demo.AuxiliaryCrate.position=p;
            demo.AuxiliaryCrate.transform.position=p;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.Greater(demo.ReplanRevision,before);
            Assert.AreEqual(new GridCell(11,8),demo.AuxiliaryCrateCell);
            Object.Destroy(host);
        }
        // W28 real Unity world-part registration, native gate B01-60 NOT_RUN.
        [UnityTest]
        public IEnumerator W28RealDetachedModuleColliderBlocksHeavyWithoutAuthoritativeMutation()
        {
            var host=new GameObject("R5 external part world");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            demo.SetHeavy(true);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);
            var box=part.GetComponent<BoxCollider>();
            part.transform.localScale=new Vector3(1.25f,1.6f,1.25f);
            part.transform.position=demo.PhysicalCrate.position+new Vector3(8f,0,0);
            var body=part.AddComponent<Rigidbody>();
            body.useGravity=false;body.isKinematic=true;
            Physics.SyncTransforms();
            Vector3 immutable=body.position;
            Assert.IsTrue(demo.TryRegisterWorldBlocker(body));
            Assert.AreEqual(1,demo.RegisteredWorldBlockerCount);
            Assert.AreEqual(immutable,body.position);
            Assert.IsFalse(demo.CurrentPlan.Reachable);
            Assert.IsFalse(demo.CurrentRouteFromPlayer);
            Assert.NotNull(box);
            Assert.IsTrue(demo.UnregisterWorldBlocker(body));
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Object.Destroy(part);Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W28ExternallyMovingDebrisCausesRealPhysicsResample()
        {
            var host=new GameObject("R5 moving detached part");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            demo.SetHeavy(true);
            yield return new WaitForFixedUpdate();
            var debris=GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.transform.localScale=new Vector3(1.25f,1.6f,1.25f);
            debris.transform.position=demo.PhysicalCrate.position+
                new Vector3(8f,0,0);
            var body=debris.AddComponent<Rigidbody>();
            body.useGravity=false;body.isKinematic=true;
            Assert.IsTrue(demo.TryRegisterWorldBlocker(body));
            Assert.IsFalse(demo.CurrentPlan.Reachable);
            int rev=demo.ReplanRevision;
            body.position+=new Vector3(10f,0,0);
            body.transform.position=body.position;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.Greater(demo.ReplanRevision,rev);
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Object.Destroy(debris);Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W28DisabledRegisteredColliderFailsClosedUntilUnregistered()
        {
            var host=new GameObject("R5 disabled foreign physics part");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            var debris=GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.transform.position=demo.PhysicalCrate.position+
                new Vector3(12f,0,0);
            var body=debris.AddComponent<Rigidbody>();
            body.isKinematic=true;body.useGravity=false;
            Assert.IsTrue(demo.TryRegisterWorldBlocker(body));
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            debris.GetComponent<Collider>().enabled=false;
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(demo.OccupancyUncertain);
            Assert.IsFalse(demo.CurrentPlan.Reachable);
            Assert.IsTrue(demo.UnregisterWorldBlocker(body));
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Object.Destroy(debris);Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W28ForeignObstacleDoesNotAllowPlayerOrDuplicateRegistration()
        {
            var host=new GameObject("R5 registry rejects player");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(demo.TryRegisterWorldBlocker(demo.Player));
            Assert.IsFalse(demo.TryRegisterWorldBlocker(demo.PhysicalCrate));
            Assert.IsFalse(demo.TryRegisterWorldBlocker(demo.AuxiliaryCrate));
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.transform.position=demo.PhysicalCrate.position+
                new Vector3(12f,0,0);
            var body=part.AddComponent<Rigidbody>();
            body.isKinematic=true;body.useGravity=false;
            Assert.IsTrue(demo.TryRegisterWorldBlocker(body));
            Assert.IsFalse(demo.TryRegisterWorldBlocker(body));
            Assert.AreEqual(1,demo.RegisteredWorldBlockerCount);
            Object.Destroy(part);Object.Destroy(host);
        }
        [UnityTest]
        public IEnumerator W28CompoundDetachedPartRegistersBothPhysicalChildColliders()
        {
            var host=new GameObject("R5 compound external physics part");
            var demo=host.AddComponent<SafeRouteUnityDemo>();
            demo.SetHeavy(true);
            yield return new WaitForFixedUpdate();
            var root=new GameObject("Detached compound robot module");
            root.transform.position=demo.PhysicalCrate.position+
                new Vector3(20f,0,0);
            var body=root.AddComponent<Rigidbody>();
            body.isKinematic=true;body.useGravity=false;
            var a=GameObject.CreatePrimitive(PrimitiveType.Cube);
            a.transform.SetParent(root.transform,false);
            a.transform.localPosition=Vector3.zero;
            a.transform.localScale=new Vector3(1.25f,1.4f,1.25f);
            var b=GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.transform.SetParent(root.transform,false);
            b.transform.localPosition=new Vector3(-12f,0,0);
            b.transform.localScale=new Vector3(1.25f,1.4f,1.25f);
            Physics.SyncTransforms();
            Assert.IsTrue(demo.TryRegisterWorldBlocker(body));
            // Root body at SAFE (13,8), child B at (7,8); ONLY the
            // additional compound child physically creates the chokepoint.
            Assert.AreEqual(1,demo.RegisteredWorldBlockerCount);
            Assert.IsFalse(demo.CurrentPlan.Reachable);
            Assert.IsTrue(demo.UnregisterWorldBlocker(body));
            Assert.IsTrue(demo.CurrentPlan.Reachable);
            Object.Destroy(root);Object.Destroy(host);
        }
    }
}

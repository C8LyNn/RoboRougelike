using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.RUNSAFETY001.Tests
{
    public sealed class RunSafetyPlayModeTests
    {
        private static SafetyVehicleSnapshot Disabled()
        {
            return new SafetyVehicleSnapshot {
                RunActive=true, InGarage=true, NextChallengeAvailable=true,
                CoreAlive=true, StructureConnected=false, FunctionalWeapon=false,
                FunctionalDrive=false, CanonicalCanDeploy=false
            };
        }

        [UnityTest] public IEnumerator PhysicalTemporaryObjectIsNotOwnedAndDestroyedAfterVictory()
        {
            var state=new RunSafetyState(new[]{
                new SafetyOwnedPart("gun-owned",SafetyPartRole.Weapon,0,100,70,false),
                new SafetyOwnedPart("drive-owned",SafetyPartRole.Drive,0,100,70,false)
            },0);
            Assert.AreEqual(RebuildOrderResult.QueuedFree,state.TryQueueRebuild("gun-owned",out _));
            Assert.IsTrue(state.TryBeginChallenge("scene-1",Disabled(),out var lease,out _));
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name=lease.LeaseId;
            go.transform.position=new Vector3(0f,2f,0f);
            var body=go.AddComponent<Rigidbody>();
            body.mass=200f;
            body.useGravity=true;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position=new Vector3(0,-.5f,0);
            floor.transform.localScale=new Vector3(8f,1f,8f);
            try
            {
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                Assert.IsTrue(body!=null);
                Assert.AreEqual(2,state.PartCount);
                Assert.AreEqual(0,state.FindPart("gun-owned").HealthPercent);
                Assert.AreEqual(0,state.FindPart("drive-owned").HealthPercent);
                Assert.AreEqual(ChallengeOutcomeResult.VictoryAccepted,state.RecordOutcome("scene-1",true));
                Object.Destroy(go);
                go=null;
                yield return null;
                Assert.IsFalse(state.LoanerInUse);
                Assert.AreEqual(2,state.PartCount);
                Assert.AreEqual(100,state.FindPart("gun-owned").HealthPercent);
                Assert.AreEqual(0,state.FindPart("drive-owned").HealthPercent);
                Assert.IsFalse(state.FindPart("gun-owned").IsMounted);
            }
            finally
            {
                if(go!=null)Object.Destroy(go);
                Object.Destroy(floor);
            }
            yield return null;
        }

        [UnityTest] public IEnumerator DefeatAndGarageReentryDoNotProgressPendingOrDuplicateLease()
        {
            var state=new RunSafetyState(new[]{
                new SafetyOwnedPart("gun",SafetyPartRole.Weapon,0,10,7,false)},0);
            Assert.AreEqual(RebuildOrderResult.QueuedFree,state.TryQueueRebuild("gun",out _));
            for(int i=0;i<3;i++)yield return null; // frame advances not win counts
            Assert.IsTrue(state.TryBeginChallenge("lost-1",Disabled(),out var lease,out _));
            Assert.IsTrue(lease.UsesLoaner);
            Assert.IsFalse(state.TryBeginChallenge("lost-1",Disabled(),out _,out _));
            Assert.AreEqual(ChallengeOutcomeResult.DefeatRecorded,state.RecordOutcome("lost-1",false));
            Assert.AreEqual(0,state.VictorySerial);
            Assert.IsTrue(state.FindPart("gun").IsRebuilding);
            for(int i=0;i<5;i++)yield return null;
            Assert.AreEqual(0,state.FindPart("gun").HealthPercent);
            Assert.AreEqual(ChallengeOutcomeResult.DuplicateOrStaleEvent,
                state.RecordOutcome("lost-1",true));
            Assert.AreEqual(0,state.AcceptedVictories);
        }

        [UnityTest] public IEnumerator NormalHealthyRobotDoesNotReceiveEmergencyLease()
        {
            var state=new RunSafetyState(new[]{
                new SafetyOwnedPart("gun",SafetyPartRole.Weapon,100,10,7,true)},0);
            var healthy=Disabled();
            healthy.CanonicalCanDeploy=true;
            healthy.StructureConnected=true;
            healthy.FunctionalDrive=true;
            healthy.FunctionalWeapon=true;
            Assert.IsTrue(state.TryBeginChallenge("normal-1",healthy,out var lease,out _));
            Assert.IsFalse(lease.UsesLoaner);
            Assert.IsNull(lease.LeaseId);
            yield return null;
            Assert.AreEqual(ChallengeOutcomeResult.VictoryAccepted,state.RecordOutcome("normal-1",true));
            Assert.AreEqual(1,state.PartCount);
        }
    }
}

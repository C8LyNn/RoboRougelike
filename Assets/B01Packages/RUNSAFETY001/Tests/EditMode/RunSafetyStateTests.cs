using System;
using System.Linq;
using NUnit.Framework;

namespace B01.RUNSAFETY001.Tests
{
    public sealed class RunSafetyStateTests
    {
        static SafetyOwnedPart Part(string id,SafetyPartRole role,int hp=0,bool mounted=false)
            => new SafetyOwnedPart(id,role,hp,10,7,mounted);
        static RunSafetyState Machine(int scrap=0,params SafetyOwnedPart[] parts)
            => new RunSafetyState(parts,scrap);
        static SafetyVehicleSnapshot Blocked(bool gun=true,bool drive=true,bool structure=true)
            => new SafetyVehicleSnapshot{RunActive=true,InGarage=true,NextChallengeAvailable=true,
                CoreAlive=true,StructureConnected=structure,FunctionalWeapon=gun,
                FunctionalDrive=drive,CanonicalCanDeploy=false};
        static SafetyVehicleSnapshot Healthy()
        {
            var s=Blocked();s.CanonicalCanDeploy=true;return s;
        }
        static SafetyLease Begin(RunSafetyState m,string id,SafetyVehicleSnapshot snapshot)
        {
            Assert.IsTrue(m.TryBeginChallenge(id,snapshot,out var lease,out _));
            return lease;
        }

        [Test] public void ZeroHpWeaponAndDriveWithZeroScrapOfferLoanerNotRepair()
        {
            var m=Machine(0,Part("weapon",SafetyPartRole.Weapon),Part("leftdrive",SafetyPartRole.Drive));
            Assert.AreEqual(SafetyDecision.LoanerOffered,m.Evaluate(Blocked(false,false)));
            var l=Begin(m,"challenge-01",Blocked(false,false));
            Assert.IsTrue(l.UsesLoaner);
            Assert.AreEqual(0,m.Scrap);
            Assert.AreEqual(0,m.FindPart("weapon").HealthPercent);
            Assert.AreEqual(0,m.FindPart("leftdrive").HealthPercent);
            Assert.AreEqual(2,m.PartCount);
        }

        [Test] public void BrokenStructuralBridgeAlsoAllowsLoaner()
        {
            var m=Machine(0,Part("bridge",SafetyPartRole.Structure));
            Assert.IsTrue(Begin(m,"bridge-challenge",Blocked(true,true,false)).UsesLoaner);
        }

        [Test] public void OneFreeQueueAtZeroScrapButSecondUnpaidCannotBeQueued()
        {
            var m=Machine(0,Part("gun",SafetyPartRole.Weapon),Part("wheel",SafetyPartRole.Drive));
            Assert.AreEqual(RebuildOrderResult.QueuedFree,m.TryQueueRebuild("gun",out int price));
            Assert.AreEqual(0,price);
            Assert.IsTrue(m.FindPart("gun").IsRebuilding);
            Assert.IsFalse(m.FindPart("gun").TryMount());
            Assert.AreEqual(RebuildOrderResult.Unaffordable,
                m.TryQueueRebuild("wheel",out price));
            Assert.AreEqual(0,price);
            Assert.AreEqual(0,m.Scrap);
            Assert.AreEqual(0,m.FindPart("wheel").HealthPercent);
        }

        [Test] public void PaidOrderCheaperThanNewAndMoneyDeductedOnce()
        {
            var m=Machine(10,Part("gun",SafetyPartRole.Weapon),Part("wheel",SafetyPartRole.Drive));
            Assert.AreEqual(RebuildOrderResult.QueuedFree,m.TryQueueRebuild("gun",out _));
            Assert.AreEqual(RebuildOrderResult.QueuedPaid,m.TryQueueRebuild("wheel",out int charge));
            Assert.AreEqual(7,charge);
            Assert.Less(charge,m.FindPart("wheel").PurchasePrice);
            Assert.AreEqual(3,m.Scrap);
            Assert.AreEqual(RebuildOrderResult.AlreadyQueued,m.TryQueueRebuild("wheel",out int another));
            Assert.AreEqual(0,another);
            Assert.AreEqual(3,m.Scrap);
        }

        [Test] public void VictoryCompletesExactlyOneNextChallengeNotGarageReentry()
        {
            var m=Machine(0,Part("gun",SafetyPartRole.Weapon));
            Assert.AreEqual(RebuildOrderResult.QueuedFree,m.TryQueueRebuild("gun",out _));
            for(int i=0;i<12;i++)
            {
                Assert.AreEqual(SafetyDecision.LoanerOffered,m.Evaluate(Blocked(false,true)));
                Assert.IsTrue(m.FindPart("gun").IsRebuilding);
            }
            var lease=Begin(m,"battle-1",Blocked(false,true));
            Assert.IsTrue(lease.UsesLoaner);
            Assert.AreEqual(ChallengeOutcomeResult.VictoryAccepted,m.RecordOutcome("battle-1",true));
            Assert.IsFalse(m.FindPart("gun").IsRebuilding);
            Assert.AreEqual(100,m.FindPart("gun").HealthPercent);
            Assert.IsFalse(m.FindPart("gun").IsMounted); // must be mounted explicitly
            Assert.IsTrue(m.FindPart("gun").TryMount());
        }

        [Test] public void DefeatDoesNotAdvanceRebuildOrResetFreeSlot()
        {
            var m=Machine(0,Part("gun",SafetyPartRole.Weapon),Part("wheel",SafetyPartRole.Drive));
            Assert.AreEqual(RebuildOrderResult.QueuedFree,m.TryQueueRebuild("gun",out _));
            Begin(m,"lost-1",Blocked(false,false));
            Assert.AreEqual(ChallengeOutcomeResult.DefeatRecorded,m.RecordOutcome("lost-1",false));
            Assert.AreEqual(0,m.VictorySerial);
            Assert.IsTrue(m.FindPart("gun").IsRebuilding);
            Assert.IsTrue(m.FreeRebuildAlreadyClaimed);
            Assert.AreEqual(RebuildOrderResult.Unaffordable,m.TryQueueRebuild("wheel",out _));
            Assert.AreEqual(0,m.Scrap);
        }

        [Test] public void DefeatRunTerminationOwnedExternallyAndBlocksNewLease()
        {
            var m=Machine(0,Part("gun",SafetyPartRole.Weapon));
            Begin(m,"lost",Blocked(false,true));
            Assert.AreEqual(ChallengeOutcomeResult.DefeatRecorded,m.RecordOutcome("lost",false));
            m.MarkExternalRunEnded();
            Assert.AreEqual(SafetyDecision.RunAlreadyEnded,m.Evaluate(Blocked(false,true)));
            Assert.IsFalse(m.TryBeginChallenge("retry",Blocked(false,true),out _,out _));
        }

        [Test] public void DuplicateVictoryEventCannotProduceSecondFreeCycleOrDupPart()
        {
            var m=Machine(0,Part("gun",SafetyPartRole.Weapon));
            m.TryQueueRebuild("gun",out _);
            Begin(m,"battle-1",Blocked(false,true));
            Assert.AreEqual(ChallengeOutcomeResult.VictoryAccepted,m.RecordOutcome("battle-1",true));
            Assert.AreEqual(ChallengeOutcomeResult.DuplicateOrStaleEvent,m.RecordOutcome("battle-1",true));
            Assert.AreEqual(1,m.VictorySerial);
            Assert.AreEqual(1,m.AcceptedVictories);
            Assert.AreEqual(1,m.PartCount);
            Assert.AreEqual(100,m.FindPart("gun").HealthPercent);
        }

        [Test] public void FreeSlotDoesNotRegenerateByRepeatedGarageEntryOrRetryingLease()
        {
            var m=Machine(0,Part("a",SafetyPartRole.Weapon),Part("b",SafetyPartRole.Drive));
            m.TryQueueRebuild("a",out _);
            for(int i=0;i<20;i++)
            {
                Assert.AreEqual(RebuildOrderResult.Unaffordable,m.TryQueueRebuild("b",out _));
                Assert.IsTrue(m.FreeRebuildAlreadyClaimed);
                Assert.AreEqual(0,m.Scrap);
            }
            Begin(m,"battle",Blocked(false,false));
            Assert.IsFalse(m.TryBeginChallenge("battle",Blocked(false,false),out _,out _));
            Assert.IsFalse(m.TryBeginChallenge("another",Blocked(false,false),out _,out _));
        }

        [Test] public void FreeRebuildCanBeUsedAgainOnlyInNextWonCycle()
        {
            var m=Machine(0,Part("a",SafetyPartRole.Weapon),Part("b",SafetyPartRole.Drive));
            m.TryQueueRebuild("a",out _);
            Begin(m,"battle1",Blocked(false,false));
            m.RecordOutcome("battle1",true);
            Assert.AreEqual(RebuildOrderResult.QueuedFree,m.TryQueueRebuild("b",out int charged));
            Assert.AreEqual(0,charged);
            Assert.IsTrue(m.FindPart("b").IsRebuilding);
            Assert.IsFalse(m.FindPart("b").TryMount());
            Assert.AreEqual(0,m.FindPart("b").HealthPercent);
            Begin(m,"battle2",Blocked(true,false));
            m.RecordOutcome("battle2",true);
            Assert.AreEqual(2,m.VictorySerial);
            Assert.AreEqual(100,m.FindPart("b").HealthPercent);
            Assert.AreEqual(2,m.PartCount);
        }

        [Test] public void NoOtherStartGuardBypassNoCoreRevival()
        {
            var m=Machine(0,Part("gun",SafetyPartRole.Weapon));
            var s=Blocked(false,false);
            s.CoreAlive=false;
            Assert.AreEqual(SafetyDecision.MissingCore,m.Evaluate(s));
            Assert.IsFalse(m.TryBeginChallenge("x",s,out _,out _));
            s.CoreAlive=true;s.OtherStartBlocker=true;
            Assert.AreEqual(SafetyDecision.UnrelatedBlocker,m.Evaluate(s));
            Assert.IsFalse(m.TryBeginChallenge("x",s,out _,out _));
            s.OtherStartBlocker=false;s.NextChallengeAvailable=false;
            Assert.AreEqual(SafetyDecision.NoNextChallenge,m.Evaluate(s));
        }

        [Test] public void HealthyVehicleNotForcedIntoLoaner()
        {
            var m=Machine(0,Part("gun",SafetyPartRole.Weapon,100,true));
            Assert.AreEqual(SafetyDecision.NormalVehicle,m.Evaluate(Healthy()));
            Assert.IsFalse(Begin(m,"normal",Healthy()).UsesLoaner);
        }

        [Test] public void NoLoanerWhenGameCanDeployButDiagnosticContradicts()
        {
            var m=Machine(0,Part("gun",SafetyPartRole.Weapon));
            var s=Blocked(false,true);s.CanonicalCanDeploy=true;
            Assert.AreEqual(SafetyDecision.InvalidSnapshot,m.Evaluate(s));
        }

        [Test] public void UnrelatedBlockerNotMistakenForDestroyedPartSoftlock()
        {
            var m=Machine(0,Part("gun",SafetyPartRole.Weapon,100));
            var s=Blocked(true,true,true);
            Assert.AreEqual(SafetyDecision.UnrelatedBlocker,m.Evaluate(s));
        }

        [Test] public void DuplicatePartIdsAndBadPricingFailClosed()
        {
            Assert.Throws<ArgumentException>(()=>Machine(0,Part("same",SafetyPartRole.Weapon),
                Part("same",SafetyPartRole.Drive)));
            Assert.Throws<ArgumentException>(()=>new SafetyOwnedPart(
                "bad",SafetyPartRole.Weapon,0,10,10,false));
            Assert.Throws<ArgumentException>(()=>new SafetyOwnedPart(
                "bad",SafetyPartRole.Weapon,0,0,0,false));
            Assert.AreEqual(0,new SafetyOwnedPart(
                "cheap",SafetyPartRole.Weapon,0,1,0,false).RebuildQuote);
        }

        [Test] public void StaleEncounterCannotFinishAndLoanerIsTransient()
        {
            var m=Machine(0,Part("gun",SafetyPartRole.Weapon));
            var lease=Begin(m,"challenge1",Blocked(false,true));
            Assert.IsTrue(lease.UsesLoaner);
            Assert.IsTrue(m.LoanerInUse);
            Assert.AreEqual(ChallengeOutcomeResult.DuplicateOrStaleEvent,
                m.RecordOutcome("another",true));
            Assert.AreEqual(0,m.AcceptedVictories);
            m.RecordOutcome("challenge1",true);
            Assert.IsFalse(m.LoanerInUse);
            Assert.IsNull(m.ActiveEncounterId);
            Assert.AreEqual(1,m.PartCount);
            Assert.IsFalse(m.TryBeginChallenge("challenge1",Blocked(false,true),out _,out _));
        }
    }
}

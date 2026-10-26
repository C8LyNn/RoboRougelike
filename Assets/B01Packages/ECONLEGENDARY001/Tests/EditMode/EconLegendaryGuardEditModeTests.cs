// Authored Unity 6000.3.14f1 NUnit EditMode scenarios, NOT executed by portable CI.
using System;
using System.Collections.Generic;
using NUnit.Framework;
using RoboRougelike.B01.Legendary;
using RoboRougelike.B01.Economy.Rebuild;
using RoboRougelike.B01.Economy.LegendaryGuard;

namespace RoboRougelike.B01.Economy.LegendaryGuard.Tests
{
    public class EconLegendaryGuardEditModeTests
    {
        sealed class Host : IAuthoritativeR3Port, IAuthoritativeRebuildPort, IRebuildReservationProof, ISharedR3PhysicalOwnerIdentity
        {
            public string RobotId => "native-A";
            public readonly string Id;
            public Host(string id=null) {Id=id??Guid.NewGuid().ToString("N");}
            public bool HideOwnerProof;
            public object AuthoritativeR3Model => HideOwnerProof ? null : this;
            public float Hp;
            public bool Pending,Proof=true,KindMismatch;
            public int Repairs;
            public IReadOnlyList<PartObservation> Mounted => new PartObservation[0];
            public IReadOnlyList<PartObservation> Inventory =>
                new[]{new PartObservation(Id,"Gun","Railgun",Hp)};
            public IReadOnlyList<RebuildPartView> OwnedParts =>
                new[]{new RebuildPartView(Id,KindMismatch?"Armor":"Gun",Hp)};
            public int PurchasePrice(string id)=>100;
            public int PaidRebuildQuote(string id)=>25;
            public bool HasReservation(string id,string token)=>Pending;
            public bool TryIsPending(string id,out bool pending){pending=Pending;return Proof;}
            public bool TryReserveAndDebitAtomically(string id,int cost,string key,out string error)
            {error="UNSUPPORTED";return false;}
            public bool TryCompleteReservedBatchAtomically(string eventId,IReadOnlyList<RebuildTicket> tickets,out string error)
            {error="UNSUPPORTED";return false;}
            public bool HasCertifiedSocket(string kind,string token,LegendaryPlacement place)=>false;
            public bool CanMountExisting(string id,LegendaryPlacement place,out string error)
            {error="UNSUPPORTED";return false;}
            public bool MountExisting(string id,LegendaryPlacement place,out string error)
            {error="UNSUPPORTED";return false;}
            public bool CanUnmountSafely(string id,out string error)
            {error="UNSUPPORTED";return false;}
            public bool UnmountExisting(string id,out string error)
            {error="UNSUPPORTED";return false;}
            public bool RepairExisting(string id,out string error)
            {Repairs++;Hp=1;error="OK";return true;}
            public bool ReceiveSalvage(PartObservation item,out string error)
            {error="UNSUPPORTED";return false;}
            public bool TransferInventoryAtomically(IAuthoritativeR3Port target,string id,out string error)
            {error="UNSUPPORTED";return false;}
        }
        static GuardedLegendaryPort Guard(Host h)=>
            new GuardedLegendaryPort(h,h,new RebuildQueueService(h),h);
        [Test] public void IdenticalGuidAcrossDifferentR3StoresMustReject()
        {
            var same=Guid.NewGuid().ToString("N");
            var legendStore=new Host(same){Hp=0};
            var otherEconomyStore=new Host(same){Hp=0};
            var guard=new GuardedLegendaryPort(legendStore,otherEconomyStore,
                new RebuildQueueService(otherEconomyStore),otherEconomyStore);
            Assert.IsFalse(guard.CheckSharedOwnership(out var error));
            Assert.AreEqual("OWNER_IDENTITY_MISMATCH",error);
            Assert.IsFalse(guard.RepairExisting(same,out error));
            Assert.AreEqual(0,legendStore.Repairs);
        }
        [Test] public void MissingRealR3OwnerProofRejects()
        {
            var h=new Host{Hp=1,HideOwnerProof=true};
            var guard=Guard(h);
            Assert.IsFalse(guard.CheckSharedOwnership(out var error));
            Assert.AreEqual("OWNER_IDENTITY_UNAVAILABLE",error);
            Assert.IsFalse(guard.RepairExisting(h.Id,out error));
            Assert.AreEqual(0,h.Repairs);
        }
        [Test] public void DestroyedNeverInstantHeals()
        {
            var h=new Host{Hp=0};
            Assert.IsFalse(Guard(h).RepairExisting(h.Id,out var error));
            Assert.AreEqual("REBUILD_REQUIRED",error);
            Assert.AreEqual(0,h.Repairs);
        }
        [Test] public void PendingBlocksEvenBeforeQueuedMetadataLoaded()
        {
            var h=new Host{Hp=0,Pending=true};
            Assert.IsFalse(Guard(h).RepairExisting(h.Id,out var error));
            Assert.AreEqual("REBUILD_PENDING",error);
            Assert.AreEqual(0,h.Repairs);
        }
        [Test] public void UnavailableProofFailsClosed()
        {
            var h=new Host{Hp=1,Proof=false};
            Assert.IsFalse(Guard(h).RepairExisting(h.Id,out var error));
            Assert.AreEqual("RESERVATION_PROOF_UNAVAILABLE",error);
            Assert.AreEqual(0,h.Repairs);
        }
        [Test] public void PhysicalMismatchFailsClosed()
        {
            var h=new Host{Hp=1,KindMismatch=true};
            Assert.IsFalse(Guard(h).RepairExisting(h.Id,out var error));
            Assert.AreEqual("STALE_CROSS_PACKAGE_OWNERSHIP",error);
            Assert.AreEqual(0,h.Repairs);
        }
    }
}

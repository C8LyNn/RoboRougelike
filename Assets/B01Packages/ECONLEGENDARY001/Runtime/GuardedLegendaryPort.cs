// B01-22 W13 ECONLEGENDARY-GUARD-001: keep R3 authoritative; no additional ownership/economy ledger.
using System;
using System.Collections.Generic;
using System.Linq;
using RoboRougelike.B01.Legendary;
using RoboRougelike.B01.Economy.Rebuild;

namespace RoboRougelike.B01.Economy.LegendaryGuard
{
    // Live, authoritative host reservation status; returning false MUST fail closed.
    public interface IRebuildReservationProof
    {
        bool TryIsPending(string physicalId, out bool pending);
    }

    // Implement on BOTH concrete adapters, exposing the exact SAME authoritative
    // R3VehicleModel reference. Never use a GUID/string/DeepCopy as authority proof.
    // This detects two distinct R3 stores containing coincidentally identical physical IDs.
    public interface ISharedR3PhysicalOwnerIdentity
    {
        object AuthoritativeR3Model { get; }
    }

    // Receiver composition: pass THIS port, never the unguarded R3 port, to LegendaryUnityService.
    // All host mutations still run through the SAME existing R3VehicleModel and wallet.
    public sealed class GuardedLegendaryPort : IAuthoritativeR3Port
    {
        private readonly IAuthoritativeR3Port inner;
        private readonly IAuthoritativeRebuildPort economyHost;
        private readonly RebuildQueueService rebuild;
        private readonly IRebuildReservationProof authoritativeReservations;

        public GuardedLegendaryPort(IAuthoritativeR3Port actualR3Port,
            IAuthoritativeRebuildPort actualRebuildPort,
            RebuildQueueService actualQueue,
            IRebuildReservationProof trustedReservationProof)
        {
            inner=actualR3Port??throw new ArgumentNullException(nameof(actualR3Port));
            economyHost=actualRebuildPort??throw new ArgumentNullException(nameof(actualRebuildPort));
            rebuild=actualQueue??throw new ArgumentNullException(nameof(actualQueue));
            authoritativeReservations=trustedReservationProof??throw new ArgumentNullException(nameof(trustedReservationProof));
        }

        public string RobotId => inner.RobotId;
        public IReadOnlyList<PartObservation> Mounted => inner.Mounted;
        public IReadOnlyList<PartObservation> Inventory => inner.Inventory;

        public bool CheckSharedOwnership(out string reason)
        {
            if(rebuild.IsQuarantined){reason="REBUILD_QUARANTINED";return false;}
            var legendOwner=inner as ISharedR3PhysicalOwnerIdentity;
            var econOwner=economyHost as ISharedR3PhysicalOwnerIdentity;
            if(legendOwner==null||econOwner==null||
                legendOwner.AuthoritativeR3Model==null||econOwner.AuthoritativeR3Model==null)
            {reason="OWNER_IDENTITY_UNAVAILABLE";return false;}
            if(!object.ReferenceEquals(legendOwner.AuthoritativeR3Model,econOwner.AuthoritativeR3Model))
            {reason="OWNER_IDENTITY_MISMATCH";return false;}
            var physical=economyHost.OwnedParts;
            var mounted=inner.Mounted;
            var inventory=inner.Inventory;
            if(physical==null||mounted==null||inventory==null)
            {reason="MISSING_AUTHORITATIVE_OWNER";return false;}
            var items=mounted.Concat(inventory).ToArray();
            var economy=physical.ToArray();
            if(items.Any(p=>p==null)||economy.Any(p=>p==null)||
                items.Select(p=>p.InstanceId).Distinct(StringComparer.Ordinal).Count()!=items.Length||
                economy.Select(p=>p.InstanceId).Distinct(StringComparer.Ordinal).Count()!=economy.Length)
            {reason="DUPLICATE_HOST_OWNERSHIP";return false;}
            foreach(var part in items)
            {
                // ECON may cover more nonlegendary parts than the selected robot, never fewer.
                var match=economy.Where(p=>p.InstanceId==part.InstanceId).ToArray();
                if(match.Length!=1||match[0].PartKind!=part.BasePartKind||
                   match[0].Health!=part.Health)
                {reason="STALE_CROSS_PACKAGE_OWNERSHIP";return false;}
            }
            reason="OK";return true;
        }

        private bool CanMutate(string id, bool isRepair, out string reason)
        {
            if(!CheckSharedOwnership(out reason))return false;
            if(string.IsNullOrEmpty(id)){reason="INVALID_PHYSICAL_INSTANCE";return false;}
            if(!authoritativeReservations.TryIsPending(id,out var pending))
            {reason="RESERVATION_PROOF_UNAVAILABLE";return false;}
            if(pending||rebuild.IsRebuilding(id))
            {reason="REBUILD_PENDING";return false;}
            if(isRepair)
            {
                var p=Mounted.Concat(Inventory).FirstOrDefault(x=>x.InstanceId==id);
                if(p==null){reason="PART_NOT_OWNED";return false;}
                // Never bypass one victorious challenge by delegating R3.Repair at 0HP.
                if(p.Health<=0){reason="REBUILD_REQUIRED";return false;}
            }
            reason="OK";return true;
        }
        public bool HasCertifiedSocket(string socketKind,string socketToken,LegendaryPlacement placement) =>
            inner.HasCertifiedSocket(socketKind,socketToken,placement);

        public bool CanMountExisting(string id,LegendaryPlacement placement,out string error) =>
            CanMutate(id,false,out error) && inner.CanMountExisting(id,placement,out error);
        public bool MountExisting(string id,LegendaryPlacement placement,out string error) =>
            CanMutate(id,false,out error) && inner.MountExisting(id,placement,out error);
        public bool CanUnmountSafely(string id,out string error) =>
            CanMutate(id,false,out error) && inner.CanUnmountSafely(id,out error);
        public bool UnmountExisting(string id,out string error) =>
            CanMutate(id,false,out error) && inner.UnmountExisting(id,out error);
        public bool RepairExisting(string id,out string error) =>
            CanMutate(id,true,out error) && inner.RepairExisting(id,out error);
        public bool ReceiveSalvage(PartObservation reward,out string error) =>
            CanMutate(reward==null?null:reward.InstanceId,false,out error) &&
            inner.ReceiveSalvage(reward,out error);
        public bool TransferInventoryAtomically(IAuthoritativeR3Port target,string id,out string error)
        {
            if(!CanMutate(id,false,out error))return false;
            if(target is not GuardedLegendaryPort receiver)
            {error="UNGUARDED_TRANSFER_TARGET";return false;}
            if(!receiver.CanMutate(id,false,out error))return false;
            return inner.TransferInventoryAtomically(receiver.inner,id,out error);
        }
    }
}

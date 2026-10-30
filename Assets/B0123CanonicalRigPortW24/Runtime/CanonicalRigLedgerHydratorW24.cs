// NEW W24 minimal public #510 AtomicDockLedger / frozen #556 W06 hydrate port.
// Does not read or mutate the frozen SecureDebrisPlaygroundV2 private Rig[].
#nullable enable
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
namespace B01.CanonicalRigPortW24
{
    public static class CanonicalRigLedgerHydratorW24
    {
        private static readonly Vector3d Zero=new Vector3d(0,0,0);
        public static bool TryHydrate(PersistedPart part,int slot,ColdRestore plan,
            out AtomicDockLedger ledger)
        {
            ledger=new AtomicDockLedger();
            if(part==null||plan==null||slot<0||slot>=FrozenPartCatalog.Slots||
               !SnapshotRules.Valid(plan.Snapshot)||
               part.Id!=FrozenPartCatalog.Id(slot)||
               part.OwnerId!=FrozenPartCatalog.Owner(slot)||
               part.MassKg!=FrozenPartCatalog.Mass(slot)||
               part.AssetGuid!=FrozenPartCatalog.Asset(slot)||
               part.SocketGuid!=FrozenPartCatalog.Socket(slot)||
               plan.Snapshot.Parts[slot].Id!=part.Id||
               plan.Snapshot.Parts[slot].Consumed!=part.Consumed||
               plan.ContainsConsumedId(slot,part.Id)!=part.Consumed||
               plan.ConsumedCount(slot)!=(part.Consumed?1:0))
                return false;
            if(part.Consumed)
            {
                if(!ledger.TryDetach(part.Id,Zero,Zero,Zero,part.MassKg,0,0,
                    out Vector3d impulse)||impulse.MagnitudeSquared!=0)
                    return false;
            }
            return ledger.Contains(part.Id)==part.Consumed&&
                   ledger.Count==(part.Consumed?1:0)&&!ledger.Busy;
        }
    }
}

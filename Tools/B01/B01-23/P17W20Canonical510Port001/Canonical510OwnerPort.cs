using System;
using B01.DetachedSaveContract001;
using B01.UnitySaveBridge001;

namespace B01.Canonical510Port001
{
    // OWNER-APPROVED NEW VERSIONED SEAM, not an implementation of frozen #510.
    // Only the canonical game owner can implement access to its private Rig[],
    // Rigidbody and AtomicDockLedger. Never use reflection or mutation escapes.
    public interface ICanonical510WorldOwner
    {
        long RunId { get; }
        Guid PersistentSlotId { get; }
        bool MainThreadQuiescentAndOwnsLease();
        ICanonical510PreparedWorld PrepareFreshPrivateRigAndLedger(RunSnapshot frozen556);
    }
    public interface ICanonical510PreparedWorld : IPreparedWorld
    {
        // True only when fresh #510 private rigs and R2 ledger are safely staged
        // off-scene with ALL Rigidbody inactive and no shared old-world refs.
        bool IsOffstageInactive { get; }
        bool HasPrivateR2LedgerForSlot(int slot,long partId,bool consumed);
    }

    public sealed class Canonical510OwnerGateway
    {
        private readonly ICanonical510WorldOwner owner;
        private readonly AtomicWorldSwap transaction;
        public long Generation => transaction.Generation;
        public bool Quarantined => transaction.Quarantined;
        public bool HasWorld => transaction.HasWorld;

        public Canonical510OwnerGateway(ICanonical510WorldOwner owner,string trustedPersistentRoot)
        {
            this.owner=owner??throw new ArgumentNullException(nameof(owner));
            if(owner.RunId<=0||owner.PersistentSlotId==Guid.Empty)
                throw new ArgumentException("A positive run and stable private slot are required");
            // This path is solely derived from owner-authenticated identifiers.
            // Host lifecycle owns a separate lease; file CAS alone is not one.
            string path=CheckpointSlotPath.ForRun(trustedPersistentRoot,
                owner.RunId,owner.PersistentSlotId);
            transaction=new AtomicWorldSwap(new OwnerFactory(owner),
                new FileCheckpointStore(path));
        }
        private bool Gate()
        {
            try{return owner.MainThreadQuiescentAndOwnsLease();}
            catch{return false;}
        }
        public WorldSwapVerdict TryRestoreExact556(byte[] snapshot)
        {
            if(!Gate())return WorldSwapVerdict.HostBusy;
            // Original #556 binary is the ONLY accepted source. Decode and
            // validate BEFORE touching any existing original #510 world.
            if(snapshot==null||!SnapshotCodec.TryDecode(snapshot,out RunSnapshot? decoded)||
               decoded==null||!SnapshotRules.Valid(decoded))
                return WorldSwapVerdict.InvalidSnapshot;
            if(decoded.RunId!=owner.RunId)return WorldSwapVerdict.WrongRun;
            return transaction.TryLoad(snapshot,owner.RunId,1,Gate);
        }
        public bool TryCaptureExact556(out byte[] bytes)
            =>transaction.TrySave(owner.RunId,Gate,out bytes);
        public bool ContainsPrivateConsumedId(int slot,long id)
            =>transaction.IsConsumed(slot,id);

        private sealed class OwnerFactory : IWorldFactory
        {
            private readonly ICanonical510WorldOwner source;
            public OwnerFactory(ICanonical510WorldOwner owner){source=owner;}
            public IPreparedWorld Prepare(RunSnapshot snapshot)
            {
                if(!source.MainThreadQuiescentAndOwnsLease()||
                   !SnapshotRules.Valid(snapshot)||snapshot.RunId!=source.RunId)
                    throw new InvalidOperationException("Owner not quiescent or invalid #556");
                ICanonical510PreparedWorld? staged=null;
                try
                {
                    staged=source.PrepareFreshPrivateRigAndLedger(snapshot);
                    if(staged==null||!staged.IsOffstageInactive)
                        throw new InvalidOperationException("Fresh private #510 world is not inactive");
                    var guarded=new LedgerCheckedWorld(staged);
                    // Validate original #556 private R2 ledger coupling before activation.
                    var observed=guarded.Capture();
                    if(!AtomicWorldSwap.Consistent(observed,snapshot))
                        throw new InvalidOperationException("Original #510 staged identity mismatch");
                    return guarded;
                }
                catch
                {
                    try{staged?.Deactivate();}catch{}
                    try{staged?.Retire();}catch{}
                    throw; // transaction owns sticky quarantine if compensation fails
                }
            }
        }
        private sealed class LedgerCheckedWorld : IPreparedWorld
        {
            private readonly ICanonical510PreparedWorld actual;
            public LedgerCheckedWorld(ICanonical510PreparedWorld world){actual=world;}
            public RunSnapshot Capture()
            {
                RunSnapshot current=actual.Capture();
                if(!SnapshotRules.Valid(current))
                    throw new InvalidOperationException("Invalid runtime #510 capture");
                for(int slot=0;slot<FrozenPartCatalog.Slots;slot++)
                {
                    PersistedPart part=current.Parts[slot];
                    if(!actual.HasPrivateR2LedgerForSlot(slot,part.Id,part.Consumed))
                        throw new InvalidOperationException("Original private R2 ledger diverged");
                }
                return current;
            }
            public void Activate()=>actual.Activate();
            public void Deactivate()=>actual.Deactivate();
            public void Retire()=>actual.Retire();
        }
    }
}

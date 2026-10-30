// B01-62 P17/W15 – strict Unity HOST PORT, no reflection/private #510 access.
// Only schema: frozen selected #556 (itself tied to frozen #510).
// An authorized Unity host MUST implement atomic world-token publication and its
// own private per-Rigidbody AtomicDockLedger hydration. This kernel cannot do so.
using System;
using B01.DetachedSaveContract001;

namespace B01.B0162.AtomicHostBridge
{
    public enum BridgeResult
    {
        Accepted, InvalidSnapshot, WrongRun, StaleGeneration, Busy,
        Rejected, Quarantine
    }

    // Trusted host-owned stage; the ledger MUST be actual frozen original #510 R2
    // AtomicDockLedger, staged without touching current Rigidbody/GameObject state.
    public interface IPreparedWorld
    {
        bool IsReady { get; }
        bool ContainsConsumedId(int slot, long id);
        int ConsumedCount(int slot);
        // MUST only dispose *unpublished*, fresh offstage objects/ledgers.
        void Abort();
    }

    // This must be implemented by B01-23/Codex in an authorized new host version.
    // Implementing it with private #510 reflection or reusing a running W06 ledger
    // is specifically forbidden. Token swaps must be atomic to reader/physics.
    public interface IAtomicWorldHost
    {
        bool IsQuiescent();
        object CurrentWorldToken { get; } // reference identity, NOT a mutable snapshot
        RunSnapshot CaptureCurrent();
        IPreparedWorld PrepareFreshWorld(RunSnapshot snapshot);
        // success => stage published, failure => live world and token unchanged;
        // on THROW possibly partial change, RestorePrevious MUST restore entirely.
        bool TryPublish(object expectedPrevious, IPreparedWorld stage);
        bool RestorePrevious(object previous);
        void Quarantine(string reason);
    }

    public sealed class FrozenSaveHostBridge
    {
        private readonly IAtomicWorldHost host;
        private bool busy;
        private bool quarantined;

        public FrozenSaveHostBridge(IAtomicWorldHost host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
        }
        public bool Quarantined => quarantined;
        public bool Busy => busy;

        public BridgeResult TrySave(out byte[]? binary)
        {
            binary = null;
            if (quarantined) return BridgeResult.Quarantine;
            if (busy) return BridgeResult.Busy;
            busy = true;
            try
            {
                if (!host.IsQuiescent()) return BridgeResult.Busy;
                object before = host.CurrentWorldToken;
                if (before == null) return Fail("SAVE_MISSING_WORLD");
                RunSnapshot snap = host.CaptureCurrent();
                if (!SnapshotRules.Valid(snap)) return BridgeResult.InvalidSnapshot;
                byte[] candidate = SnapshotCodec.Encode(snap); // actual unchanged #556
                if (!host.IsQuiescent() || !ReferenceEquals(host.CurrentWorldToken, before))
                    return BridgeResult.Busy;
                binary = candidate; // fresh data; current world unchanged
                return BridgeResult.Accepted;
            }
            catch
            {
                return Fail("SAVE_CAPTURE_EXCEPTION");
            }
            finally { busy = false; }
        }

        // Two-stage cold load for ONLY #556; does NOT call #556 ColdSessionHandle.TrySwap
        // because that would publish a logical ledger before Unity world is ready.
        public BridgeResult TryLoad(byte[]? source, long expectedRunId, long minGeneration)
        {
            if (quarantined) return BridgeResult.Quarantine;
            if (busy) return BridgeResult.Busy;
            busy = true;
            IPreparedWorld? stage = null;
            bool published = false;
            try
            {
                if (source == null) return BridgeResult.InvalidSnapshot;
                if (!host.IsQuiescent()) return BridgeResult.Busy;
                object before = host.CurrentWorldToken;
                if (before == null) return Fail("LOAD_MISSING_WORLD");

                // Defensive private byte copy BEFORE any host-controlled callback.
                byte[] copied = (byte[])source.Clone();
                if (!ColdRestore.TryStage(copied, out ColdRestore? cold) || cold == null)
                    return BridgeResult.InvalidSnapshot;
                RunSnapshot snapshot = cold.Snapshot;
                if (snapshot.RunId != expectedRunId || expectedRunId <= 0)
                    return BridgeResult.WrongRun;
                if (minGeneration <= 0 || snapshot.Generation < minGeneration)
                    return BridgeResult.StaleGeneration;

                RunSnapshot priorSnapshot = host.CaptureCurrent();
                if (!SnapshotRules.Valid(priorSnapshot)) return Fail("CURRENT_WORLD_INVALID");
                if (snapshot.RunId != priorSnapshot.RunId) return BridgeResult.WrongRun;
                if (snapshot.Generation < priorSnapshot.Generation)
                    return BridgeResult.StaleGeneration;

                // Actual unchanged original W06 from #556 checked BEFORE building world.
                for (int i = 0; i < FrozenPartCatalog.Slots; ++i)
                {
                    PersistedPart part = snapshot.Parts[i];
                    if (cold.ContainsConsumedId(i, part.Id) != part.Consumed ||
                        cold.ConsumedCount(i) != (part.Consumed ? 1 : 0))
                        return BridgeResult.InvalidSnapshot;
                }
                stage = host.PrepareFreshWorld(snapshot);
                if (stage == null || !stage.IsReady) return BridgeResult.Rejected;
                for (int i = 0; i < FrozenPartCatalog.Slots; ++i)
                {
                    PersistedPart p = snapshot.Parts[i];
                    if (stage.ContainsConsumedId(i, p.Id) != p.Consumed ||
                        stage.ConsumedCount(i) != (p.Consumed ? 1 : 0))
                        return BridgeResult.Rejected;
                }
                if (!host.IsQuiescent() || !ReferenceEquals(host.CurrentWorldToken, before))
                    return BridgeResult.Busy;
                bool ok;
                try { ok = host.TryPublish(before, stage); }
                catch
                {
                    // A throwing Unity host may already have changed parent/body/ledger;
                    // restore whole previous world or immediately quarantine.
                    return Compensate(before, "PUBLISH_THROW");
                }
                if (!ok)
                {
                    if (!ReferenceEquals(host.CurrentWorldToken, before))
                        return Compensate(before, "FALSE_AFTER_PARTIAL_PUBLISH");
                    return BridgeResult.Rejected;
                }
                if (ReferenceEquals(host.CurrentWorldToken, before))
                    return Compensate(before, "SUCCESS_WITHOUT_PUBLICATION");

                published = true;
                return BridgeResult.Accepted;
            }
            catch
            {
                return Fail("HOST_STAGE_OR_VALIDATION_EXCEPTION");
            }
            finally
            {
                if (!published && stage != null)
                {
                    try { stage.Abort(); }
                    catch { Fail("STAGING_ABORT_FAILED"); }
                }
                busy = false;
            }
        }

        private BridgeResult Compensate(object before, string cause)
        {
            try
            {
                if (host.RestorePrevious(before) &&
                    ReferenceEquals(host.CurrentWorldToken, before))
                    return BridgeResult.Rejected;
            }
            catch { /* quarantine below */ }
            return Fail("WORLD_ROLLBACK_FAILED_" + cause);
        }

        private BridgeResult Fail(string reason)
        {
            quarantined = true;
            try { host.Quarantine(reason); } catch { }
            return BridgeResult.Quarantine;
        }
    }
}

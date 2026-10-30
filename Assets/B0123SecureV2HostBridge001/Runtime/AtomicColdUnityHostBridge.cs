using System;
using B01.DetachedSaveContract001;

namespace B01.SecureSaveHost001
{
    // All interface calls are synchronous on the caller-owned Unity main thread.
    // Never use this to mutate #510 private Rig or AtomicDockLedger.
    public interface IIsolatedRigWorld : IDisposable
    {
        bool IsVisible { get; }
        bool IsBusy { get; }
        RunSnapshot Capture(long runId, long generation);
        void SetVisible(bool visible);
    }

    public interface IIsolatedRigFactory
    {
        // Must build completely offstage and return an INVISIBLE, wholly owned world.
        // Implementations must clean up their own partial construction on exception.
        IIsolatedRigWorld Build(ColdRestore validated);
    }

    /// <summary>
    /// Two-phase whole-world swap. Physically inactive candidate -> validate all
    /// fixed stable IDs, original W06 consumed status and pose -> hide old -> show new.
    /// Before commit failure never changes the prior visible world. An exception in
    /// visibility or cleanup returns QUARANTINE, never false success.
    /// </summary>
    public sealed class AtomicColdUnityHostBridge
    {
        private readonly IIsolatedRigFactory factory;
        private readonly Func<bool> hostIdle;
        private IIsolatedRigWorld? current;
        private bool busy;
        private bool quarantine;
        private readonly long runId;
        private long generation;

        public bool IsQuarantined => quarantine;
        public bool HasWorld => current != null;
        public long Generation => generation;
        public long RunId => runId;

        public AtomicColdUnityHostBridge(long runId, IIsolatedRigFactory factory, Func<bool> hostIdle)
        {
            if (runId <= 0) throw new ArgumentOutOfRangeException(nameof(runId));
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.hostIdle = hostIdle ?? throw new ArgumentNullException(nameof(hostIdle));
            this.runId = runId;
        }

        private RestoreVerdict CheckIdle()
        {
            if (quarantine) return RestoreVerdict.Quarantine;
            try { return (hostIdle() && (current == null || !current.IsBusy))
                    ? RestoreVerdict.Accepted : RestoreVerdict.TransactionBusy; }
            catch { quarantine = true; return RestoreVerdict.Quarantine; }
        }

        public RestoreVerdict TrySave(out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (quarantine) return RestoreVerdict.Quarantine;
            if (busy) return RestoreVerdict.TransactionBusy;
            busy = true;
            try
            {
                if (current == null) return RestoreVerdict.InvalidSnapshot;
                RestoreVerdict idle = CheckIdle();
                if (idle != RestoreVerdict.Accepted) return idle;
                // Capture the actual current physical+ID state while all host commands
                // are gated, then verify the ONLY frozen #556 codec before output.
                RunSnapshot captured = current.Capture(runId, generation);
                if (!SnapshotRules.Valid(captured)) return RestoreVerdict.InvalidSnapshot;
                byte[] candidate = SnapshotCodec.Encode(captured);
                if (!ColdRestore.TryStage(candidate, out ColdRestore? plan) || plan == null)
                    return RestoreVerdict.InvalidSnapshot;
                if (!Equivalent(captured, plan.Snapshot)) return RestoreVerdict.InvalidSnapshot;
                idle = CheckIdle();
                if (idle != RestoreVerdict.Accepted) return idle;
                bytes = candidate;
                return RestoreVerdict.Accepted;
            }
            catch { quarantine = true; return RestoreVerdict.Quarantine; }
            finally { busy = false; }
        }

        public RestoreVerdict TryLoad(byte[] bytes, long minimumGeneration)
        {
            if (quarantine) return RestoreVerdict.Quarantine;
            if (busy) return RestoreVerdict.TransactionBusy;
            busy = true;
            IIsolatedRigWorld? staged = null;
            bool committed = false;
            try
            {
                RestoreVerdict idle = CheckIdle();
                if (idle != RestoreVerdict.Accepted) return idle;
                // Exact #556 parser and cold original W06 rehydration; ZERO changes
                // to existing world until all validation and staging finishes.
                if (!ColdRestore.TryStage(bytes, out ColdRestore? plan) || plan == null)
                    return RestoreVerdict.InvalidSnapshot;
                RunSnapshot snapshot = plan.Snapshot;
                if (snapshot.RunId != runId) return RestoreVerdict.WrongRun;
                if (snapshot.Generation < minimumGeneration ||
                    snapshot.Generation < generation || snapshot.Generation <= 0)
                    return RestoreVerdict.StaleGeneration;

                staged = factory.Build(plan);
                if (staged == null || staged.IsVisible || staged.IsBusy)
                {
                    quarantine = staged == null;
                    return RestoreVerdict.Quarantine;
                }
                RunSnapshot prepared = staged.Capture(snapshot.RunId, snapshot.Generation);
                if (!SnapshotRules.Valid(prepared) || !Equivalent(prepared, snapshot))
                    return RestoreVerdict.InvalidSnapshot;

                idle = CheckIdle();
                if (idle != RestoreVerdict.Accepted) return idle;
                IIsolatedRigWorld? old = current;
                // With no async yield between operations, the host performs one
                // synchronous exclusive visibility / authoritative pointer swap.
                // Never destroy the old world before the new world is active.
                try
                {
                    if (old != null) old.SetVisible(false);
                    staged.SetVisible(true);
                    current = staged;
                    generation = snapshot.Generation;
                    committed = true;
                    staged = null;
                }
                catch
                {
                    // Compensate without writing any old ID/pose/parent/ledger.
                    // If compensation itself fails, permanently quarantine.
                    bool repaired = true;
                    try { staged?.SetVisible(false); } catch { repaired = false; }
                    try { if (old != null) old.SetVisible(true); } catch { repaired = false; }
                    if (!repaired) quarantine = true;
                    return RestoreVerdict.Quarantine;
                }
                if (old != null)
                {
                    try { old.Dispose(); }
                    catch { quarantine = true; return RestoreVerdict.Quarantine; }
                }
                return RestoreVerdict.Accepted;
            }
            catch { quarantine = true; return RestoreVerdict.Quarantine; }
            finally
            {
                if (!committed && staged != null)
                {
                    try { staged.Dispose(); }
                    catch { quarantine = true; }
                }
                busy = false;
            }
        }

        public void Close()
        {
            if (busy) throw new InvalidOperationException("Cannot close during save/load");
            if (current == null) return;
            IIsolatedRigWorld old = current;
            current = null;
            try { old.Dispose(); }
            catch { quarantine = true; throw; }
        }

        private static bool Near(double a, double b) =>
            !double.IsNaN(a) && !double.IsNaN(b) && Math.Abs(a - b) <= 0.002;
        private static bool Near(B01.DebrisRepulse001.Vector3d a, B01.DebrisRepulse001.Vector3d b) =>
            Near(a.X,b.X) && Near(a.Y,b.Y) && Near(a.Z,b.Z);
        private static bool Equivalent(RunSnapshot left, RunSnapshot right)
        {
            if (left == null || right == null || left.RunId != right.RunId ||
                left.Generation != right.Generation) return false;
            PersistedPart[] a = left.Parts, b = right.Parts;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                PersistedPart x=a[i], y=b[i];
                if (x.Id!=y.Id || x.OwnerId!=y.OwnerId || x.ParentId!=y.ParentId ||
                    x.AssetGuid!=y.AssetGuid || x.SocketGuid!=y.SocketGuid ||
                    x.State!=y.State || x.Consumed!=y.Consumed || x.Kinematic!=y.Kinematic ||
                    !Near(x.Position,y.Position) || !Near(x.LinearVelocity,y.LinearVelocity) ||
                    !Near(x.AngularVelocity,y.AngularVelocity) || !Near(x.MassKg,y.MassKg) ||
                    !Near(x.Qx,y.Qx) || !Near(x.Qy,y.Qy) || !Near(x.Qz,y.Qz) || !Near(x.Qw,y.Qw))
                    return false;
            }
            return true;
        }
    }
}

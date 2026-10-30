using System;
using B01.DebrisRepulse001;

namespace B01.SecureReattachV2
{
    /// <summary>
    /// R2 single-owner ledger: no public Unity component may clear a detached ID.
    /// Reserve/rearm happens BEFORE physical mutation; a throwing commit compensates
    /// via a zero-impulse original W06 TryDetach, restoring the consumed ID.
    /// All callbacks run synchronously on Unity's main thread; no awaits.
    /// </summary>
    public sealed class AtomicDockLedger
    {
        private readonly DebrisImpulseLedger consumed = new DebrisImpulseLedger();
        private bool busy;
        private static readonly Vector3d Zero = new Vector3d(0, 0, 0);
        public bool Busy => busy;
        public int Count => consumed.Count;
        public bool Contains(long id) => consumed.Contains(id);

        public bool TryDetach(long id, Vector3d owner, Vector3d part,
            Vector3d velocity, double massKg, double desiredSpeed,
            double maxAddedSpeed, out Vector3d impulse)
        {
            impulse = Zero;
            return !busy && consumed.TryDetach(id, owner, part, velocity,
                massKg, desiredSpeed, maxAddedSpeed, out impulse);
        }

        /// <summary>Do not clear the ledger while a physical transaction is underway.</summary>
        public void ClearForNewRun()
        {
            if (busy) throw new InvalidOperationException("Cannot reset live dock transaction");
            consumed.ClearForNewRun();
        }

        /// <summary>
        /// ID must exist and is rearmed BEFORE physical mutation. If body/parent
        /// changes fail, restore them and reconsume the original ID at zero impulse.
        /// Rollback failure is never reported as success or silent false.
        /// </summary>
        public bool TryReattach(long id, Action applyPhysical, Action rollbackPhysical)
        {
            if (applyPhysical == null) throw new ArgumentNullException(nameof(applyPhysical));
            if (rollbackPhysical == null) throw new ArgumentNullException(nameof(rollbackPhysical));
            if (busy || id <= 0 || !consumed.Contains(id))
                return false;
            busy = true;
            try
            {
                if (!consumed.RearmAfterReattach(id))
                    return false; // missing ID: ZERO physical changes
                try
                {
                    applyPhysical();
                    return true;
                }
                catch (Exception originalFailure)
                {
                    Exception rollbackFailure = null!;
                    try { rollbackPhysical(); }
                    catch (Exception e) { rollbackFailure = e; }

                    // Reconsume original ID with zero physical force. Unlike
                    // an emitter, the original pure W06 ledger is authoritative.
                    bool restored = consumed.TryDetach(
                        id, Zero, Zero, Zero, 1.0, 0.0, 0.0, out Vector3d restoredImpulse);
                    if (!restored || restoredImpulse.MagnitudeSquared != 0 || rollbackFailure != null)
                        throw new InvalidOperationException(
                            "Dock rollback failed; physical/ledger state needs quarantine",
                            rollbackFailure ?? originalFailure);
                    return false; // physically rolled back AND original ID still consumed
                }
            }
            finally { busy = false; }
        }
    }
}

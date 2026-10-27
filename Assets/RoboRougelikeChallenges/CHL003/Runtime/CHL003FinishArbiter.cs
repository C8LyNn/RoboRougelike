using System;

namespace B01.CHL003
{
    /// <summary>
    /// Pure, deterministic two-phase finish arbitration. Physics callbacks
    /// for CP[last] and Finish may arrive in EITHER order in the same fixed
    /// simulation step. A contact is only queued after CP[last-1] has been
    /// validated; completion requires a LATER step and real retained overlap.
    /// </summary>
    public sealed class CHL003FinishArbiter
    {
        private long? queuedStep;
        private long lastCheckpointStep = long.MaxValue;
        public bool IsQueued => queuedStep.HasValue;

        public void Reset()
        {
            queuedStep = null;
            lastCheckpointStep = long.MaxValue;
        }

        public void LastCheckpointAccepted(long fixedStep)
        {
            if (fixedStep < 0) throw new ArgumentOutOfRangeException(nameof(fixedStep));
            lastCheckpointStep = fixedStep;
        }

        public bool ObserveFinishContact(long fixedStep, bool lastCheckpointNextOrDone)
        {
            if (fixedStep < 0) throw new ArgumentOutOfRangeException(nameof(fixedStep));
            if (!lastCheckpointNextOrDone) return false;
            if (!queuedStep.HasValue) queuedStep = fixedStep;
            return true;
        }

        public bool TryCommit(long fixedStep, bool allCheckpoints,
            bool stillPhysicallyOverlapping, bool paused)
        {
            if (fixedStep < 0) throw new ArgumentOutOfRangeException(nameof(fixedStep));
            if (!queuedStep.HasValue || paused || !allCheckpoints ||
                fixedStep <= queuedStep.Value || fixedStep <= lastCheckpointStep)
                return false;
            queuedStep = null;
            return stillPhysicallyOverlapping;
        }

        public void CancelPending() => queuedStep = null;
    }
}

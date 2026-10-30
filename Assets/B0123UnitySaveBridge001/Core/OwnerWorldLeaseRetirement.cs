using System;
using System.Collections.Generic;
using System.IO;

namespace B01.UnitySaveBridge001
{
    // W21 / #670: a MonoBehaviour can die WITHOUT the GameObject/physics dying.
    // Transfer the ALREADY OPEN OS-exclusive stream here; it MUST NOT be
    // disposed until a verifiable actual physical-world teardown completes.
    // Failure/unknown status remains permanently denied until process exit.
    // No unlinking the .owner inode; no background thread, timer or GC finalizer.
    public static class OwnerWorldLeaseRetirement
    {
        private sealed class Pending
        {
            public readonly FileStream Lease;
            public readonly Func<bool> AllWorldsActuallyDestroyed;
            public readonly bool CleanupSucceeded;
            public Pending(FileStream lease,Func<bool> destroyed,bool success)
            { Lease=lease;AllWorldsActuallyDestroyed=destroyed;CleanupSucceeded=success; }
        }
        private static readonly object Sync=new object();
        private static readonly Dictionary<string,Pending> PendingSlots=
            new Dictionary<string,Pending>(StringComparer.OrdinalIgnoreCase);

        public static void Hold(string checkpoint,FileStream lease,
            Func<bool> allWorldsActuallyDestroyed,bool cleanupSucceeded)
        {
            if(string.IsNullOrWhiteSpace(checkpoint)||lease==null||
               allWorldsActuallyDestroyed==null)throw new ArgumentException("lease");
            string key=Path.GetFullPath(checkpoint);
            lock(Sync)
            {
                // An existing pending authority MUST NEVER be overwritten by a
                // second owner, even if caller supplied a new stream by mistake.
                if(PendingSlots.ContainsKey(key))
                    throw new InvalidOperationException("duplicate pending owner");
                PendingSlots.Add(key,new Pending(lease,
                    allWorldsActuallyDestroyed,cleanupSucceeded));
            }
        }

        public static bool ReconcileBeforeNewAcquire(string checkpoint)
        {
            if(string.IsNullOrWhiteSpace(checkpoint))return false;
            string key=Path.GetFullPath(checkpoint);
            lock(Sync)
            {
                if(!PendingSlots.TryGetValue(key,out Pending? pending))
                    return true;
                if(!pending.CleanupSucceeded)return false;
                bool actuallyDestroyed=false;
                try{actuallyDestroyed=pending.AllWorldsActuallyDestroyed();}
                catch{return false;}
                if(!actuallyDestroyed)return false;
                try {pending.Lease.Dispose();}
                catch {return false;}
                PendingSlots.Remove(key);
                return true;
            }
        }

        // Native host remains responsible for the physical-world predicate.
        // Never expose a method that "forcibly clears" safety quarantine.
    }
}

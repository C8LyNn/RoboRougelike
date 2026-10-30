using System;
using System.Collections.Generic;
using System.IO;

namespace B01.CanonicalWorldApi001
{
    // V4: one already-held B01-62 #669 slot.lease is THE ONLY OS owner.
    // NO separate .owner file and NO standalone public TryAcquire.
    // Only the private, owner-approved session bridge can adopt the REAL slot.
    public sealed class CanonicalWorldSlotLease
    {
        private static readonly object Sync=new object();
        private static readonly Dictionary<string,CanonicalWorldSlotLease> Active=
            new Dictionary<string,CanonicalWorldSlotLease>(StringComparer.OrdinalIgnoreCase);
        private readonly string identity;
        private readonly Func<bool> liveSource;
        private readonly Func<bool> closeSource;
        private bool bound, released;
        public long RunId {get;}
        public Guid SlotId {get;}
        public string Identity=>identity;

        private CanonicalWorldSlotLease(string dir,long run,Guid slot,
            Func<bool> live,Func<bool> release)
        {
            identity=dir;RunId=run;SlotId=slot;
            liveSource=live;closeSource=release;
        }
        private bool IsRealSlotAlive()
        {
            try{return liveSource();}
            catch{return false;}
        }
        internal bool Live
        {
            get
            {
                lock(Sync)
                    return bound&&!released&&IsRealSlotAlive()&&
                        Active.TryGetValue(identity,out CanonicalWorldSlotLease? x)&&
                        ReferenceEquals(x,this);
            }
        }
        // Must be called ONLY AFTER the actual unchanged #669 SingleFileSaveSlot
        // has acquired the one exclusive FileStream(FileShare.None) slot.lease.
        // The owner session hides the raw slot; callers cannot dispose early.
        internal static bool TryAdoptRealSlot(string trustedSlotDirectory,long run,
            Guid slot,Func<bool> genuineOwnerLive,Func<bool> releaseRealOwner,
            out CanonicalWorldSlotLease? lease)
        {
            lease=null;
            if(run<=0||slot==Guid.Empty||
                string.IsNullOrWhiteSpace(trustedSlotDirectory)||
                genuineOwnerLive==null||releaseRealOwner==null)
                return false;
            try
            {
                string key=Path.GetFullPath(trustedSlotDirectory);
                if(!Path.IsPathFullyQualified(key))return false;
                lock(Sync)
                {
                    if(Active.ContainsKey(key))return false;
                    bool real=false;
                    try{real=genuineOwnerLive();}catch{return false;}
                    if(!real)return false;
                    var token=new CanonicalWorldSlotLease(key,run,slot,
                        genuineOwnerLive,releaseRealOwner);
                    Active.Add(key,token);
                    lease=token;
                    return true;
                }
            }
            catch(ArgumentException){return false;}
            catch(IOException){return false;}
        }
        internal void BindOnce()
        {
            lock(Sync)
            {
                if(bound||released||!IsRealSlotAlive()||
                    !Active.TryGetValue(identity,out CanonicalWorldSlotLease? active)||
                    !ReferenceEquals(active,this))
                    throw new InvalidOperationException("canonical authority unavailable");
                bound=true;
            }
        }
        // Safe only BEFORE gateway creation and BEFORE any physical allocation.
        internal bool TryCancelUnbound()
        {
            lock(Sync)
            {
                if(bound||released||!IsRealSlotAlive())return false;
                bool success=false;
                try{success=closeSource();}catch{return false;}
                if(!success)return false;
                released=true;Active.Remove(identity);return true;
            }
        }
        // No public Dispose. Gateway invokes this ONLY AFTER actual owner proof
        // all published AND partially staged physical objects are destroyed.
        internal bool ReleaseVerified()
        {
            lock(Sync)
            {
                if(!bound||released||!IsRealSlotAlive())return false;
                bool success=false;
                try{success=closeSource();}catch{return false;}
                if(!success)return false;
                released=true;Active.Remove(identity);return true;
            }
        }
    }
}

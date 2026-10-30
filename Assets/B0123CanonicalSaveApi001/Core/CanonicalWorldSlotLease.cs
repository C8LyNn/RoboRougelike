using System;
using System.Collections.Generic;
using System.IO;

namespace B01.CanonicalWorldApi001
{
    // W22 / #688 #700 #710: concrete process- and OS-exclusive lifetime lease.
    // NOT a replacement for B01-62 #669 atomic payload/revision storage.
    // One real OWNER-AUTHORIZED slot path must eventually be shared with #712.
    public sealed class CanonicalWorldSlotLease
    {
        private static readonly object RegistryLock=new object();
        // Strong references intentionally escrow quarantined leases until process
        // termination, even after a MonoBehaviour or gateway is collected.
        private static readonly Dictionary<string,CanonicalWorldSlotLease> Registry=
            new Dictionary<string,CanonicalWorldSlotLease>(StringComparer.OrdinalIgnoreCase);
        private readonly string key;
        private FileStream? stream;
        private bool bound, released;

        public long RunId {get;}
        public Guid SlotId {get;}
        public string Identity => key;
        internal bool Live
        {
            get
            {
                lock(RegistryLock)
                    return bound&&!released&&stream!=null&&
                        Registry.TryGetValue(key,out CanonicalWorldSlotLease? current)&&
                        ReferenceEquals(current,this);
            }
        }

        private CanonicalWorldSlotLease(string key,long runId,Guid slot,FileStream stream)
        {
            this.key=key;RunId=runId;SlotId=slot;this.stream=stream;
        }
        private static void RejectReparse(string path)
        {
            if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)
                throw new IOException("Foreign link in authority path");
        }
        public static bool TryAcquire(string privateRoot,long runId,Guid slot,
            out CanonicalWorldSlotLease? lease)
        {
            lease=null;
            if(string.IsNullOrWhiteSpace(privateRoot)||runId<=0||slot==Guid.Empty)
                return false;
            try
            {
                string root=Path.GetFullPath(privateRoot);
                if(!Directory.Exists(root))return false; // caller owns trusted private root
                RejectReparse(root);
                string directory=Path.Combine(root,"b0123_canonical_world_authority");
                Directory.CreateDirectory(directory);
                RejectReparse(directory);
                string key=Path.Combine(directory,"run_"+runId.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)+
                    "_slot_"+slot.ToString("N")+".owner");
                lock(RegistryLock)
                {
                    if(Registry.ContainsKey(key))return false;
                    if(File.Exists(key))RejectReparse(key);
                    FileStream file=new FileStream(key,FileMode.OpenOrCreate,
                        FileAccess.ReadWrite,FileShare.None);
                    try
                    {
                        RejectReparse(key);
                        var owner=new CanonicalWorldSlotLease(key,runId,slot,file);
                        Registry.Add(key,owner);
                        lease=owner;
                        return true;
                    }
                    catch
                    {
                        file.Dispose();
                        throw;
                    }
                }
            }
            catch(IOException){return false;}
            catch(UnauthorizedAccessException){return false;}
            catch(ArgumentException){return false;}
        }
        internal void BindOnce()
        {
            lock(RegistryLock)
            {
                if(bound||released||stream==null||
                   !Registry.TryGetValue(key,out CanonicalWorldSlotLease? current)||
                   !ReferenceEquals(current,this))
                    throw new InvalidOperationException("World authority already bound or lost");
                bound=true;
            }
        }
        // Before BindOnce NO GameObjects may have been created by gateway.
        public bool TryCancelUnbound()
        {
            lock(RegistryLock)
            {
                if(bound||released||stream==null)return false;
                try{stream.Dispose();}catch{return false;}
                stream=null;released=true;
                Registry.Remove(key);
                return true;
            }
        }
        // Only the canonical gateway can close its bound authority. There is NO
        // public Dispose that a caller or Unity OnDestroy can invoke unsafely.
        internal bool ReleaseVerified()
        {
            lock(RegistryLock)
            {
                if(!bound||released||stream==null)return false;
                try{stream.Dispose();}catch{return false;}
                stream=null;released=true;Registry.Remove(key);
                return true;
            }
        }
    }
}

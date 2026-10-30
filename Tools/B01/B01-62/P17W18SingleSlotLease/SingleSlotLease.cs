// B01-62 P17/W18: VERSIONED host slot lease adapter, NOT installed in Unity.
// Only selected exact #596 transaction core; original #510/#556/#596 immutable.
// Locks are advisory across OS processes, and enforced across instances within this CLR.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using B01.UnitySaveBridge001;
namespace B01.SingleSlotLease001
{
    public sealed class SlotLease : IDisposable
    {
        private static readonly object Sync=new object();
        private static readonly HashSet<string> Held=new HashSet<string>(
            Path.DirectorySeparatorChar=='\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        private readonly string key;
        private FileStream? stream;
        private bool disposed;
        private SlotLease(string key,FileStream stream){this.key=key;this.stream=stream;}
        public bool Owns
        {
            get
            {
                lock(Sync)return !disposed&&stream!=null&&
                    stream.CanRead&&Held.Contains(key);
            }
        }
        public static bool TryAcquire(string persistentRoot,string slot,out SlotLease? lease)
        {
            lease=null;
            if(string.IsNullOrWhiteSpace(persistentRoot)||!Path.IsPathRooted(persistentRoot)||
               string.IsNullOrEmpty(slot)||slot.Length>32||
               !Regex.IsMatch(slot,@"\A[A-Za-z0-9_-]+\z"))return false;
            try
            {
                string root=Path.GetFullPath(persistentRoot);
                // Caller must own a private local persistentDataPath, not a shared directory.
                if(!Directory.Exists(root)||IsLink(root))return false;
                string folder=Path.Combine(root,".b0162-single-host-leases");
                if(Directory.Exists(folder))
                {
                    if(IsLink(folder))return false;
                }
                else Directory.CreateDirectory(folder);
                string path=Path.Combine(folder,slot+".lock");
                if((File.Exists(path)||Directory.Exists(path))&&IsLink(path))return false;
                lock(Sync)
                {
                    if(Held.Contains(path))return false;
                    FileStream? acquired=null;
                    try
                    {
                        acquired=new FileStream(path,FileMode.OpenOrCreate,
                            FileAccess.ReadWrite,FileShare.None);
                        Held.Add(path);
                        lease=new SlotLease(path,acquired);
                        acquired=null;
                        return true;
                    }
                    catch{return false;}
                    finally{acquired?.Dispose();}
                }
            }
            catch{return false;}
        }
        private static bool IsLink(string path)=>
            (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0;
        public void Dispose()
        {
            lock(Sync)
            {
                if(disposed)return;
                disposed=true;
                // Do NOT unlink the reusable lock file: deletion+recreation would
                // create two distinct lock inodes and permit simultaneous owners.
                try{stream?.Dispose();}
                finally{stream=null;Held.Remove(key);}
            }
        }
    }
    // Versioned integration suggestion to B01-23, not a second authoritative host.
    // Explicit lifetime ensures stale hosts cannot mint a higher generation.
    public sealed class LeasedAtomicWorldHost : IDisposable
    {
        private readonly SlotLease lease;
        private readonly AtomicWorldSwap host;
        private bool stopped;
        private LeasedAtomicWorldHost(SlotLease lease,AtomicWorldSwap host)
        {this.lease=lease;this.host=host;}
        public static bool TryOpen(IWorldFactory factory,string persistentRoot,
            string slot,out LeasedAtomicWorldHost? result)
        {
            result=null;
            if(factory==null||!SlotLease.TryAcquire(persistentRoot,slot,out SlotLease? lease)||
               lease==null)return false;
            try
            {
                string checkpoint=Path.Combine(Path.GetFullPath(persistentRoot),slot+".checkpoint");
                result=new LeasedAtomicWorldHost(lease,
                    new AtomicWorldSwap(factory,new FileCheckpointStore(checkpoint)));
                return true;
            }
            catch{lease.Dispose();return false;}
        }
        public bool Owns=>!stopped&&lease.Owns;
        public bool TrySave(long run,Func<bool> idle,out byte[] bytes)
        {
            bytes=Array.Empty<byte>();
            return Owns&&host.TrySave(run,idle,out bytes);
        }
        public WorldSwapVerdict TryLoad(byte[] bytes,long run,long minimumGeneration,
            Func<bool> idle)
        {
            if(!Owns)return WorldSwapVerdict.RollbackQuarantine;
            return host.TryLoad(bytes,run,minimumGeneration,idle);
        }
        public bool IsConsumed(int slot,long id)=>Owns&&host.IsConsumed(slot,id);
        public long Generation=>Owns?host.Generation:0;
        public bool Quarantined=>!Owns||host.Quarantined;
        public void Dispose()
        {
            if(stopped)return;
            stopped=true;
            // Caller MUST first deactivate/retire its Unity world on the main thread,
            // or treat it as quarantined. We don't own the frozen #510 Rig[].
            lease.Dispose();
        }
    }
}

// B01-62 W18: independent owner-bound SLOT LEASE REFERENCE for frozen #596.
// Executes actual unchanged selected FileCheckpointStore via ICheckpointStore.
// NOT installed into frozen #596 Unity MonoBehaviour; only B01-23 may authorize
// a new host version that holds the lease across the complete live-world lifetime.
using System;
using System.IO;
using System.Text;
using B01.UnitySaveBridge001;

namespace B01.B0162.W18
{
    public sealed class OwnedCheckpointSlotLease : IDisposable
    {
        private const string Marker = "B01-62-OWNED-CHECKPOINT-SLOT-v1\n";
        private const string MarkerFile = "owner.marker";
        private const string LeaseFile = "slot.active.lock";
        private const string RecordFile = "checkpoint.state";
        private readonly FileStream exclusive;
        private bool disposed;
        private readonly string directory;
        public string SlotPath => Path.Combine(directory, RecordFile);
        public bool IsHeld => !disposed;

        private static void RejectLink(string path)
        {
            if ((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)
                throw new IOException("Refuse reparse/symlink");
        }

        // Allocates a UNIQUE never-before-used private slot directory.
        // Does NOT authorize use of an occupied shared Unity/Codex directory.
        public static void CreateOwnedSlot(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)||!Path.IsPathRooted(directory))
                throw new ArgumentException("Slot must have absolute path");
            string path=Path.GetFullPath(directory);
            string parent=Path.GetDirectoryName(path)??throw new IOException("slot parent");
            if (!Directory.Exists(parent) || File.Exists(path)||Directory.Exists(path))
                throw new IOException("Requires new empty owned slot path");
            RejectLink(parent);
            Directory.CreateDirectory(path);
            try
            {
                using(var f=new FileStream(Path.Combine(path,MarkerFile),FileMode.CreateNew,
                          FileAccess.Write,FileShare.None))
                {
                    byte[] data=Encoding.ASCII.GetBytes(Marker);
                    f.Write(data,0,data.Length);
                    f.Flush(flushToDisk:true);
                }
            }
            catch
            {
                // Delete only the new, *empty or marker-only* exclusively owned dir.
                // No recursive removal of any foreign file or subdirectory.
                try {File.Delete(Path.Combine(path,MarkerFile));Directory.Delete(path);}
                catch {}
                throw;
            }
        }

        public OwnedCheckpointSlotLease(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)||!Path.IsPathRooted(directory))
                throw new ArgumentException("Slot must have absolute path");
            this.directory=Path.GetFullPath(directory);
            if(!Directory.Exists(this.directory))throw new IOException("Unknown slot");
            RejectLink(this.directory);
            string marker=Path.Combine(this.directory,MarkerFile);
            if(!File.Exists(marker))throw new IOException("Missing exact provenance marker");
            RejectLink(marker);
            if(File.ReadAllText(marker,Encoding.ASCII)!=Marker)
                throw new IOException("Bad slot provenance");
            foreach(string entry in Directory.EnumerateFileSystemEntries(this.directory))
            {
                string n=Path.GetFileName(entry);
                if(n!=MarkerFile&&n!=LeaseFile&&n!=RecordFile&&n!=RecordFile+".lock")
                    throw new IOException("Foreign empty dir/file — never delete: "+n);
                RejectLink(entry);
                if(Directory.Exists(entry))throw new IOException("Unexpected directory");
            }
            string lease=Path.Combine(this.directory,LeaseFile);
            // No implicit clean-up; a zero-byte persistent lease inode is harmless,
            // and FileShare.None enforces current ownership across processes.
            exclusive=new FileStream(lease,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            if(exclusive.Length!=0)
            {
                exclusive.Dispose();
                throw new IOException("Foreign modified lease contents");
            }
        }

        public void RequireHeld()
        {
            if(disposed)throw new ObjectDisposedException(nameof(OwnedCheckpointSlotLease));
        }
        public void Dispose()
        {
            if(disposed)return;
            disposed=true;
            exclusive.Dispose();
        }
    }
    // Reference wrapper with the EXACT original selected #596 store; it is
    // impossible to access its Read/Commit through this reference after release.
    // DO NOT construct raw FileCheckpointStore or AtomicWorldSwap directly in
    // the eventual new owner-approved Unity host (those bypass this lease).
    public sealed class LeaseBoundOriginal596Store : ICheckpointStore
    {
        private readonly OwnedCheckpointSlotLease lease;
        private readonly ICheckpointStore actualOriginal;
        public LeaseBoundOriginal596Store(OwnedCheckpointSlotLease lease)
        {
            this.lease=lease??throw new ArgumentNullException(nameof(lease));
            lease.RequireHeld();
            actualOriginal=new FileCheckpointStore(lease.SlotPath);
        }
        public bool Read(long run,out long generation,out string hash)
        {
            generation=0;hash="";
            if(!lease.IsHeld)return false;
            return actualOriginal.Read(run,out generation,out hash);
        }
        public bool Commit(long run,long previous,string previousHash,long next,string hash)
            =>lease.IsHeld&&actualOriginal.Commit(run,previous,previousHash,next,hash);
    }
}

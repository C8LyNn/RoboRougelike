using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace B01.UnitySaveBridge001
{
    public enum FenceVerdict { Allowed, Stale, Conflict, Unverifiable }
    public interface ICheckpointFence
    {
        bool IsDurable {get;}
        // Same generation allowed ONLY after a complete host restart, exact hash,
        // and only with no currently published physical world.
        FenceVerdict Check(long run,long generation,byte[] hash,bool noCurrentWorld);
        // Must durably seal before a newly activated world can be published.
        // On ANY exception, caller must quarantine, even if physical rollback works.
        void Seal(long run,long generation,byte[] hash);
    }
    public static class CheckpointHash
    {
        public static byte[] Of(byte[] bytes)
        {
            if(bytes==null)throw new ArgumentNullException(nameof(bytes));
            using(var sha=SHA256.Create())return sha.ComputeHash(bytes);
        }
    }
    public sealed class VolatileCheckpointFence : ICheckpointFence
    {
        private long run,generation;
        private byte[]? hash;
        public bool IsDurable=>false;
        public FenceVerdict Check(long r,long g,byte[] h,bool noCurrent)
        {
            if(r<=0||g<=0||h==null||h.Length!=32)return FenceVerdict.Unverifiable;
            if(hash==null)return FenceVerdict.Allowed;
            if(r!=run)return FenceVerdict.Conflict;
            if(g<generation)return FenceVerdict.Stale;
            if(g==generation)return noCurrent&&Same(hash,h)
                ?FenceVerdict.Allowed:FenceVerdict.Conflict;
            return FenceVerdict.Allowed;
        }
        public void Seal(long r,long g,byte[] h)
        {
            if(Check(r,g,h,true)!=FenceVerdict.Allowed)throw new InvalidDataException("fence changed");
            run=r;generation=g;hash=(byte[])h.Clone();
        }
        internal static bool Same(byte[] a,byte[] b)
        {
            if(a.Length!=b.Length)return false;
            int diff=0;for(int i=0;i<a.Length;i++)diff|=a[i]^b[i];
            return diff==0;
        }
    }
    // Persistent per-checkpoint-slot watermark, not a complete durable savegame.
    // The owner must supply an absolute unique private scratch/slot path.
    // A checksum is corruption detection, NOT authentication against malicious users.
    public sealed class FileCheckpointFence : ICheckpointFence
    {
        private readonly string path;
        private static readonly byte[] Magic=Encoding.ASCII.GetBytes("B01W16F!");
        public bool IsDurable=>true;
        public FileCheckpointFence(string absolutePath)
        {
            if(string.IsNullOrWhiteSpace(absolutePath)||!Path.IsPathRooted(absolutePath))
                throw new ArgumentException("Fence path must be absolute",nameof(absolutePath));
            path=Path.GetFullPath(absolutePath);
        }
        private sealed class Entry
        {
            public long Run,Generation;
            public byte[] Hash=Array.Empty<byte>();
        }
        private bool Read(out Entry? e)
        {
            e=null;
            if(!File.Exists(path))return true;
            byte[] b=File.ReadAllBytes(path);
            if(b.Length!=88)return false;
            using(var sha=SHA256.Create())
            {
                byte[] digest=sha.ComputeHash(b,0,56);
                int diff=0;for(int i=0;i<32;i++)diff|=digest[i]^b[56+i];
                if(diff!=0)return false;
            }
            for(int i=0;i<8;i++)if(b[i]!=Magic[i])return false;
            using(var ms=new MemoryStream(b,8,48,false))
            using(var r=new BinaryReader(ms))
            {
                e=new Entry{Run=r.ReadInt64(),Generation=r.ReadInt64(),Hash=r.ReadBytes(32)};
                return e.Run>0&&e.Generation>0&&e.Hash.Length==32;
            }
        }
        public FenceVerdict Check(long run,long generation,byte[] hash,bool noCurrentWorld)
        {
            if(run<=0||generation<=0||hash==null||hash.Length!=32)
                return FenceVerdict.Unverifiable;
            try
            {
                if(!Read(out Entry? old))return FenceVerdict.Unverifiable;
                if(old==null)return FenceVerdict.Allowed;
                if(run!=old.Run)return FenceVerdict.Conflict;
                if(generation<old.Generation)return FenceVerdict.Stale;
                if(generation==old.Generation)return noCurrentWorld&&VolatileCheckpointFence.Same(hash,old.Hash)
                    ?FenceVerdict.Allowed:FenceVerdict.Conflict;
                return FenceVerdict.Allowed;
            }
            catch {return FenceVerdict.Unverifiable;}
        }
        public void Seal(long run,long generation,byte[] hash)
        {
            if(Check(run,generation,hash,true)!=FenceVerdict.Allowed)
                throw new InvalidDataException("Fence refusal");
            // Identical cold restart does not need a new write.
            if(Read(out Entry? old)&&old!=null&&old.Generation==generation&&
               VolatileCheckpointFence.Same(old.Hash,hash))return;
            string directory=Path.GetDirectoryName(path)??throw new InvalidDataException("directory");
            Directory.CreateDirectory(directory);
            byte[] data;
            using(var ms=new MemoryStream())
            {
                using(var w=new BinaryWriter(ms,Encoding.UTF8,true))
                {
                    w.Write(Magic);w.Write(run);w.Write(generation);w.Write(hash);
                    w.Flush();
                }
                using(var sha=SHA256.Create())
                {
                    byte[] pre=ms.ToArray();
                    byte[] digest=sha.ComputeHash(pre);
                    data=new byte[pre.Length+digest.Length];
                    Buffer.BlockCopy(pre,0,data,0,pre.Length);
                    Buffer.BlockCopy(digest,0,data,pre.Length,digest.Length);
                }
            }
            string tmp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var stream=new FileStream(tmp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    stream.Write(data,0,data.Length);
                    stream.Flush(true);
                }
                if(File.Exists(path))File.Replace(tmp,path,null);
                else File.Move(tmp,path);
            }
            finally
            {
                if(File.Exists(tmp))File.Delete(tmp);
            }
        }
    }
}

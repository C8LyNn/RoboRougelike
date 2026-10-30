// Isolated file-backed authority for original #587 receiver demonstrations.
// Accept ONLY newly owned directories, fail on unknown files/EMPTY directories.
// Full power-loss durability of directory rename NOT certified (no directory fsync).
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using B01.B0162.Selected587CheckpointGate;

namespace B01.B0162.Selected587CheckpointGate
{
    public sealed class FileCheckpointAuthority : ICheckpointAuthority,IDisposable
    {
        private readonly string root,path;
        private readonly FileStream lease;
        private CheckpointRecord? committed,pending;
        public CheckpointRecord? Committed=>committed;
        public bool Pending=>pending!=null;
        private static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
        public FileCheckpointAuthority(string directory)
        {
            root=Path.GetFullPath(directory??throw new ArgumentNullException(nameof(directory)));
            if(!Directory.Exists(root)||
               (File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0)
                throw new IOException("Directory must be an existing owned non-link scratch");
            path=Path.Combine(root,"checkpoint.bin");
            foreach(string p in Directory.EnumerateFileSystemEntries(root))
            {
                string n=Path.GetFileName(p);
                if(n!="checkpoint.bin"&&n!="checkpoint.lock")
                    throw new IOException("Foreign file/EMPTY directory: "+n);
                if((File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)
                    throw new IOException("Symlink in authority root");
                if(Directory.Exists(p))throw new IOException("Unexpected directory");
            }
            // OpenOrCreate + FileShare.None acts as a single writer lease across processes.
            lease=new FileStream(Path.Combine(root,"checkpoint.lock"),FileMode.OpenOrCreate,
                FileAccess.ReadWrite,FileShare.None);
            try {ReadOriginal();}
            catch {lease.Dispose();throw;}
        }
        private static void CheckRecord(CheckpointRecord? p)
        {
            if(p==null)return;
            if(p.Run<=0||p.Generation<=0||p.Fingerprint.Length!=64)
                throw new InvalidDataException("Invalid checkpoint authority");
            foreach(char c in p.Fingerprint)
                if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')))
                    throw new InvalidDataException("Invalid fingerprint");
        }
        private void ReadOriginal()
        {
            if(!File.Exists(path))return;
            string[] lines=File.ReadAllLines(path,Encoding.ASCII);
            if(lines.Length!=2)throw new InvalidDataException("Invalid authority receipt");
            string hex=Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(lines[0]))).ToLowerInvariant();
            if(hex!=lines[1])throw new InvalidDataException("Authority checksum invalid");
            string[] pieces=lines[0].Split('|');
            if(pieces.Length!=7||pieces[0]!="B01-W16-V1")throw new InvalidDataException("Authority version");
            long cr=long.Parse(pieces[1],Invariant),cg=long.Parse(pieces[2],Invariant);
            long pr=long.Parse(pieces[4],Invariant),pg=long.Parse(pieces[5],Invariant);
            committed=cr==0&&cg==0&&pieces[3]=="-"?null:new CheckpointRecord(cr,cg,pieces[3]);
            pending=pr==0&&pg==0&&pieces[6]=="-"?null:new CheckpointRecord(pr,pg,pieces[6]);
            CheckRecord(committed);CheckRecord(pending);
        }
        private void Write()
        {
            CheckRecord(committed);CheckRecord(pending);
            string row="B01-W16-V1|"+
                (committed?.Run??0).ToString(Invariant)+"|"+
                (committed?.Generation??0).ToString(Invariant)+"|"+(committed?.Fingerprint??"-")+"|"+
                (pending?.Run??0).ToString(Invariant)+"|"+
                (pending?.Generation??0).ToString(Invariant)+"|"+(pending?.Fingerprint??"-");
            string checksum=Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(row))).ToLowerInvariant();
            string temp=Path.Combine(root,"checkpoint.bin."+Guid.NewGuid().ToString("N")+".tmp");
            try
            {
                using(var fs=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    byte[] bytes=Encoding.ASCII.GetBytes(row+"\n"+checksum+"\n");
                    fs.Write(bytes,0,bytes.Length);
                    fs.Flush(flushToDisk:true);
                }
                // Same-directory replacement; pending intents prevent silent recovery
                // if power loss occurs during the physical publication window.
                File.Move(temp,path,overwrite:true);
            }
            finally
            {
                // Own newly created temp file ONLY, never arbitrary destination contents.
                if(File.Exists(temp))File.Delete(temp);
            }
        }
        public void Begin(CheckpointRecord next)
        {
            if(Pending)throw new InvalidOperationException("Unresolved persisted PREPARE intent");
            CheckRecord(next);
            pending=next;
            Write();
        }
        public void Commit()
        {
            if(pending==null)throw new InvalidOperationException("No intent to commit");
            committed=pending;
            pending=null;
            Write();
        }
        public void Cancel()
        {
            if(pending==null)throw new InvalidOperationException("No intent to cancel");
            pending=null;
            Write();
        }
        public void Dispose()=>lease.Dispose();
    }
}

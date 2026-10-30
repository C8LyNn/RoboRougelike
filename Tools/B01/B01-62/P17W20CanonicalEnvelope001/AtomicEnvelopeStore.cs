// B01-62 / P17 W20: VERSIONED independent single-record Save+revision authority.
// SOURCE ONLY. Not installed in Unity, not a replacement for #510 private Rig[].
// The one atomic filesystem publication contains the original EXACT #556 bytes,
// RUN, SLOT, GENERATION, prior-record evidence and complete-record SHA256.
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using B01.DetachedSaveContract001;

namespace B01.CanonicalAtomicSave001
{
    public enum SaveReadVerdict { Accepted, Missing, Corrupt, Unavailable }
    public enum SaveCommitVerdict { Accepted, Conflict, Invalid, Quarantined, Unavailable }
    public enum CommitFaultPhase { BeforeReplace, AfterReplace }
    // Injected test fault hook; NEVER use to claim a real power-loss/Unity test.
    public interface ICommitFault { void At(CommitFaultPhase phase); }

    public sealed class PersistedAuthority
    {
        public readonly byte[] Original556Bytes;
        public readonly long Generation;
        public readonly string Evidence;
        public PersistedAuthority(byte[] original556Bytes,long generation,string evidence)
        {Original556Bytes=(byte[])original556Bytes.Clone();Generation=generation;Evidence=evidence;}
    }

    public sealed class AtomicEnvelopeStore : IDisposable
    {
        private static readonly byte[] Magic=Encoding.ASCII.GetBytes("B0162S01");
        private static readonly object RegistryGuard=new object();
        private static readonly HashSet<string> Held=new HashSet<string>(
            Path.DirectorySeparatorChar=='\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        private readonly long run;
        private readonly Guid slot;
        private readonly string path,ownerPath;
        private readonly ICommitFault? faults;
        private FileStream? lease;
        private bool closed,quarantined;
        private AtomicEnvelopeStore(long run,Guid slot,string path,string ownerPath,
            FileStream lease,ICommitFault? faults)
        {this.run=run;this.slot=slot;this.path=path;this.ownerPath=ownerPath;
         this.lease=lease;this.faults=faults;}
        public bool Quarantined => quarantined;
        public bool Owns => !closed&&lease!=null;
        public string SlotFileName => Path.GetFileName(path);
        public static bool TryOpen(string persistentRoot,long run,Guid slot,
            out AtomicEnvelopeStore? store,ICommitFault? fault=null)
        {
            store=null;
            if(run<=0||slot==Guid.Empty||string.IsNullOrWhiteSpace(persistentRoot)||
                !Path.IsPathRooted(persistentRoot))return false;
            try
            {
                string root=Path.GetFullPath(persistentRoot);
                if(!Directory.Exists(root)||HasLinkInAncestry(root))return false;
                string folder=Path.Combine(root,"b0162_canonical_envelope_v1");
                if(Directory.Exists(folder))
                {
                    if(HasLinkInAncestry(folder))return false;
                }
                else
                {
                    Directory.CreateDirectory(folder);
                    if(HasLinkInAncestry(folder))return false;
                }
                string name="run_"+run.ToString(System.Globalization.CultureInfo.InvariantCulture)+
                    "_slot_"+slot.ToString("N");
                string file=Path.Combine(folder,name+".save");
                string owner=file+".owner";
                if((File.Exists(file)&&IsLink(file))||
                   (File.Exists(owner)&&IsLink(owner)))return false;
                lock(RegistryGuard)
                {
                    if(Held.Contains(owner))return false;
                    FileStream? handle=null;
                    try
                    {
                        handle=new FileStream(owner,FileMode.OpenOrCreate,
                            FileAccess.ReadWrite,FileShare.None);
                        Held.Add(owner);
                        store=new AtomicEnvelopeStore(run,slot,file,owner,handle,fault);
                        handle=null;
                        return true;
                    }
                    catch{return false;}
                    finally{handle?.Dispose();}
                }
            }
            catch{return false;}
        }
        private static bool IsLink(string p)=>
            (File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0;
        private static bool HasLinkInAncestry(string path)
        {
            DirectoryInfo? p=new DirectoryInfo(path);
            while(p!=null)
            {
                if((p.Attributes&FileAttributes.ReparsePoint)!=0)return true;
                p=p.Parent;
            }
            return false;
        }
        private static string Hex(byte[] bytes)=>
            BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();
        private static string Fingerprint(byte[] bytes)
        {
            using(var sha=SHA256.Create())return Hex(sha.ComputeHash(bytes));
        }
        private byte[] Encode(byte[] payload,long generation,string previous)
        {
            using(var stream=new MemoryStream())
            {
                using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
                {
                    writer.Write(Magic);writer.Write(run);writer.Write(slot.ToByteArray());
                    writer.Write(generation);
                    writer.Write(previous.Length==0?new byte[32]:Unhex(previous));
                    writer.Write(payload.Length);writer.Write(payload);
                    writer.Flush();
                }
                byte[] body=stream.ToArray();
                using(var sha=SHA256.Create())
                {
                    byte[] digest=sha.ComputeHash(body);
                    byte[] result=new byte[body.Length+digest.Length];
                    Buffer.BlockCopy(body,0,result,0,body.Length);
                    Buffer.BlockCopy(digest,0,result,body.Length,digest.Length);
                    return result;
                }
            }
        }
        private static byte[] Unhex(string value)
        {
            if(value.Length!=64)throw new InvalidDataException("wrong evidence");
            byte[] data=new byte[32];
            for(int i=0;i<32;i++)data[i]=Convert.ToByte(value.Substring(2*i,2),16);
            return data;
        }
        private bool Decode(byte[] raw,out PersistedAuthority? record)
        {
            record=null;
            if(raw.Length<8+8+16+8+32+4+32||raw.Length>SnapshotCodec.MaxBytes+128)
                return false;
            try
            {
                using(var sha=SHA256.Create())
                {
                    byte[] hash=sha.ComputeHash(raw,0,raw.Length-32);
                    int diff=0;
                    for(int i=0;i<32;i++)diff|=hash[i]^raw[raw.Length-32+i];
                    if(diff!=0)return false;
                }
                using(var s=new MemoryStream(raw,false))
                using(var reader=new BinaryReader(s,Encoding.UTF8,false))
                {
                    byte[] magic=reader.ReadBytes(8);
                    if(magic.Length!=8)return false;
                    for(int i=0;i<8;i++)if(magic[i]!=Magic[i])return false;
                    long actualRun=reader.ReadInt64();
                    Guid actualSlot=new Guid(reader.ReadBytes(16));
                    long generation=reader.ReadInt64();
                    byte[] prev=reader.ReadBytes(32);
                    int len=reader.ReadInt32();
                    if(actualRun!=run||actualSlot!=slot||generation<=0||
                       len<=0||len>SnapshotCodec.MaxBytes||
                       s.Length-s.Position!=len+32)return false;
                    byte[] payload=reader.ReadBytes(len);
                    if(payload.Length!=len)return false;
                    if(!ColdRestore.TryStage(payload,out ColdRestore? stage)||stage==null||
                       stage.Snapshot.RunId!=run||stage.Snapshot.Generation!=generation)
                        return false;
                    for(int i=0;i<FrozenPartCatalog.Slots;i++)
                        if(stage.ContainsConsumedId(i,FrozenPartCatalog.Id(i))!=
                           stage.Snapshot.Parts[i].Consumed)return false;
                    record=new PersistedAuthority(payload,generation,Fingerprint(raw));
                    return true;
                }
            }
            catch{return false;}
        }
        public SaveReadVerdict Read(out PersistedAuthority? state)
        {
            state=null;
            if(!Owns)return SaveReadVerdict.Unavailable;
            try
            {
                if(!File.Exists(path))return SaveReadVerdict.Missing;
                if(IsLink(path))return SaveReadVerdict.Corrupt;
                byte[] bytes=File.ReadAllBytes(path);
                return Decode(bytes,out state)?SaveReadVerdict.Accepted:SaveReadVerdict.Corrupt;
            }
            catch{return SaveReadVerdict.Corrupt;}
        }
        public SaveCommitVerdict Commit(byte[] original556,long expectedGeneration,
            string expectedPreviousEvidence,out PersistedAuthority? published)
        {
            published=null;
            if(!Owns)return SaveCommitVerdict.Unavailable;
            if(quarantined)return SaveCommitVerdict.Quarantined;
            if(original556==null||!ColdRestore.TryStage(original556,out ColdRestore? staged)||
                staged==null||staged.Snapshot.RunId!=run||
                expectedGeneration<0||expectedGeneration==long.MaxValue||
                staged.Snapshot.Generation!=expectedGeneration+1||
                expectedPreviousEvidence==null||
                (expectedGeneration==0?expectedPreviousEvidence.Length!=0:
                 expectedPreviousEvidence.Length!=64))
                return SaveCommitVerdict.Invalid;
            SaveReadVerdict previous=Read(out PersistedAuthority? prior);
            if(previous==SaveReadVerdict.Corrupt||previous==SaveReadVerdict.Unavailable)
            {quarantined=true;return SaveCommitVerdict.Quarantined;}
            if(expectedGeneration==0)
            {
                if(previous!=SaveReadVerdict.Missing)return SaveCommitVerdict.Conflict;
            }
            else if(previous!=SaveReadVerdict.Accepted||prior==null||
                    prior.Generation!=expectedGeneration||
                    prior.Evidence!=expectedPreviousEvidence)
                return SaveCommitVerdict.Conflict;
            string temp=path+"."+Guid.NewGuid().ToString("N")+".pending";
            try
            {
                byte[] record=Encode(original556,staged.Snapshot.Generation,
                    expectedPreviousEvidence);
                using(var stream=new FileStream(temp,FileMode.CreateNew,
                    FileAccess.Write,FileShare.None))
                {stream.Write(record,0,record.Length);stream.Flush(true);}
                faults?.At(CommitFaultPhase.BeforeReplace);
                if(File.Exists(path))File.Replace(temp,path,null);
                else File.Move(temp,path);
                faults?.At(CommitFaultPhase.AfterReplace);
                if(Read(out PersistedAuthority? observed)!=SaveReadVerdict.Accepted||
                   observed==null||observed.Generation!=staged.Snapshot.Generation||
                   observed.Evidence!=Fingerprint(record))
                    throw new IOException("durability outcome unknown");
                published=observed;
                return SaveCommitVerdict.Accepted;
            }
            catch
            {
                // Ambiguous after any external mutation; NEVER resume stale owner.
                quarantined=true;
                return SaveCommitVerdict.Quarantined;
            }
            finally
            {
                // Only delete the temporary file we created. NEVER unlink the
                // durable committed envelope or persistent lease inode.
                try{if(File.Exists(temp)&&!IsLink(temp))File.Delete(temp);}
                catch{quarantined=true;published=null;}
            }
        }
        public void Dispose()
        {
            lock(RegistryGuard)
            {
                if(closed)return;
                closed=true;
                try{lease?.Dispose();}
                finally{lease=null;Held.Remove(ownerPath);}
            }
        }
    }
}

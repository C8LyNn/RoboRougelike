// W23 R3: derived from exact original #669 storage blob db7745635164d9eb6b10f3246538700158130b7b.
// One owner-only authority #729 .owner; NO second .lease. SOURCE-ONLY.
// B01-62 W20 SLOTSAVE-001/R1: a single atomic envelope binds frozen #556
// snapshot bytes and generation+run+slot digest in ONE file, not a second CAS sidecar.
// SOURCE-ONLY storage protocol, NOT an installation into the frozen #510 MonoBehaviour.
// Guarantees are bounded to filesystem atomic same-directory rename/process crashes;
// actual power-loss directory fsync is platform-dependent and NOT PROVEN.
#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using B01.DetachedSaveContract001;
using B01.CanonicalWorldApi001;

namespace B01.SingleAuthoritySlot003
{
    public enum SlotStatus
    {
        Accepted, Empty, Invalid, WrongOwner, Stale, Busy, Corrupt,
        Quarantined, Closed, IOError
    }

    // The only publication boundary. Production uses atomic same-directory Move.
    // The injected publication hook allows testing an uncertain ACK AFTER a real Move.
    public interface IAtomicEnvelopePublisher
    {
        void Publish(string stage, string final);
    }

    public sealed class OsAtomicEnvelopePublisher : IAtomicEnvelopePublisher
    {
        public void Publish(string stage,string final) => File.Move(stage,final,true);
    }

    public sealed class SingleAuthoritySaveSlot : IDisposable
    {
        private const string Magic = "B0162S01"; // 8-byte protocol tag
        private const int Version = 1;
        private const int MaxBytes = 8*1024*1024;
        private const string Marker = "slot.owner";
        // Single selected V3 authority physically owns ONE .owner OS FileStream.
        // NEVER create/open a competing .lease file.
        private const string Final = "save.envelope";
        private readonly string dir;
        private readonly long runId;
        private readonly Guid slotId;
        private readonly CanonicalWorldSlotLease worldOwner;
        private readonly IAtomicEnvelopePublisher publisher;
        private bool quarantined, closed, busy;

        public bool Quarantined => quarantined;
        public bool Closed => closed;
        public long RunId => runId;
        public Guid SlotId => slotId;

        private SingleAuthoritySaveSlot(string dir,long run,Guid slot,
            CanonicalWorldSlotLease lease,IAtomicEnvelopePublisher writer)
        {
            this.dir=dir;runId=run;slotId=slot;worldOwner=lease;publisher=writer;
        }

        private static string Identity(long run,Guid slot)
            => "B0162_SINGLE_ENVELOPE_V1|"+run+"|"+slot.ToString("N")+"\n";

        private static bool IsUnsafeExisting(string path)
        {
            if(!File.Exists(path)&&!Directory.Exists(path))return false;
            try{return (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0;}
            catch{return true;}
        }

        private static bool ValidPath(string path,long run,Guid slot)
        {
            if(run<=0||slot==Guid.Empty||string.IsNullOrWhiteSpace(path))return false;
            // A caller/host must supply a dedicated, authorized NEW slot directory.
            // Symlinked parent traversal is a deployment-time trust boundary.
            try{return Path.IsPathFullyQualified(path) &&
                    !path.EndsWith(Path.DirectorySeparatorChar.ToString(),StringComparison.Ordinal);}
            catch{return false;}
        }

        private static bool OwnerMatches(CanonicalWorldSlotLease? authority,
            long run,Guid slot,string directory)
        {
            if(authority==null||authority.RunId!=run||authority.SlotId!=slot)
                return false;
            try
            {
                // The trusted private root is the parent of the authority directory.
                // Data path must be a direct NEW child of that exact same root.
                string trusted=Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(authority.Identity)!, ".."));
                string parent=Path.GetFullPath(Path.GetDirectoryName(directory)!);
                return string.Equals(parent,trusted,StringComparison.OrdinalIgnoreCase);
            }
            catch{return false;}
        }

        public static SlotStatus TryCreateNew(string directory,long run,Guid slot,
            CanonicalWorldSlotLease authority,out SingleAuthoritySaveSlot? opened,IAtomicEnvelopePublisher? hook=null)
        {
            opened=null;
            if(!ValidPath(directory,run,slot)||!OwnerMatches(authority,run,slot,directory))
                return SlotStatus.Invalid;
            if(Directory.Exists(directory)||File.Exists(directory))
                return SlotStatus.WrongOwner; // preserve foreign EMPTY directories
            try
            {
                Directory.CreateDirectory(directory);
                using(var f=new FileStream(Path.Combine(directory,Marker),
                    FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    byte[] id=Encoding.ASCII.GetBytes(Identity(run,slot));
                    f.Write(id,0,id.Length);
                    f.Flush(true);
                }
                if(IsUnsafeExisting(directory))return SlotStatus.WrongOwner;
                opened=new SingleAuthoritySaveSlot(directory,run,slot,authority,
                    hook??new OsAtomicEnvelopePublisher());
                return SlotStatus.Accepted;
            }
            catch(IOException){return SlotStatus.IOError;}
            catch(UnauthorizedAccessException){return SlotStatus.IOError;}
        }

        public static SlotStatus TryOpenExisting(string directory,long run,Guid slot,
            out SingleAuthoritySaveSlot? opened,IAtomicEnvelopePublisher? hook=null)
        {
            opened=null;
            if(!ValidPath(directory,run,slot)||!OwnerMatches(authority,run,slot,directory))
                return SlotStatus.Invalid;
            if(!Directory.Exists(directory)||IsUnsafeExisting(directory))
                return SlotStatus.WrongOwner;
            try
            {
                string marker=Path.Combine(directory,Marker);
                if(!File.Exists(marker)||IsUnsafeExisting(marker)||
                    File.ReadAllText(marker,Encoding.ASCII)!=Identity(run,slot))
                    return SlotStatus.WrongOwner;
                if(IsUnsafeExisting(Path.Combine(directory,Final)))
                    return SlotStatus.WrongOwner;
                opened=new SingleAuthoritySaveSlot(directory,run,slot,authority,
                    hook??new OsAtomicEnvelopePublisher());
                return SlotStatus.Accepted;
            }
            catch(IOException){return SlotStatus.IOError;}
            catch(UnauthorizedAccessException){return SlotStatus.IOError;}
        }

        private byte[] EncodeEnvelope(byte[] raw,long generation)
        {
            using var stream=new MemoryStream();
            using(var bw=new BinaryWriter(stream,Encoding.UTF8,true))
            {
                bw.Write(Encoding.ASCII.GetBytes(Magic));
                bw.Write(Version);
                bw.Write(runId);
                bw.Write(slotId.ToByteArray());
                bw.Write(generation);
                bw.Write(raw.Length);
                bw.Write(raw);
                bw.Flush();
            }
            byte[] prefix=stream.ToArray();
            byte[] digest=SHA256.HashData(prefix);
            stream.Write(digest,0,digest.Length);
            return stream.ToArray();
        }

        private SlotStatus DecodeEnvelope(byte[] data,out long epoch,out byte[] raw)
        {
            epoch=0;raw=Array.Empty<byte>();
            try
            {
                if(data.Length<8+4+8+16+8+4+32||data.Length>MaxBytes)
                    return SlotStatus.Corrupt;
                var body=data.AsSpan(0,data.Length-32);
                byte[] computed=SHA256.HashData(body);
                if(!CryptographicOperations.FixedTimeEquals(
                    computed,data.AsSpan(data.Length-32,32)))return SlotStatus.Corrupt;
                using var ms=new MemoryStream(data,false);
                using var br=new BinaryReader(ms,Encoding.UTF8,true);
                if(Encoding.ASCII.GetString(br.ReadBytes(8))!=Magic ||
                    br.ReadInt32()!=Version)return SlotStatus.Corrupt;
                if(br.ReadInt64()!=runId||
                    new Guid(br.ReadBytes(16))!=slotId)return SlotStatus.WrongOwner;
                long gen=br.ReadInt64();
                int n=br.ReadInt32();
                if(gen<=0||n<0||n>MaxBytes-128 ||
                    ms.Position+n+32!=data.Length)return SlotStatus.Corrupt;
                byte[] payload=br.ReadBytes(n);
                if(payload.Length!=n || !ColdRestore.TryStage(payload,
                    out ColdRestore? cold) || cold==null ||
                    cold.Snapshot.RunId!=runId || cold.Snapshot.Generation!=gen)
                    return SlotStatus.Corrupt;
                epoch=gen;raw=payload;
                return SlotStatus.Accepted;
            }
            catch{return SlotStatus.Corrupt;}
        }

        private SlotStatus ReadCore(out long generation,out byte[] raw)
        {
            generation=0;raw=Array.Empty<byte>();
            string path=Path.Combine(dir,Final);
            if(!File.Exists(path))return SlotStatus.Empty;
            if(IsUnsafeExisting(path))return SlotStatus.WrongOwner;
            try
            {
                byte[] bytes=File.ReadAllBytes(path);
                return DecodeEnvelope(bytes,out generation,out raw);
            }
            catch(IOException){return SlotStatus.IOError;}
            catch(UnauthorizedAccessException){return SlotStatus.IOError;}
        }

        public SlotStatus TryRead(out long generation,out byte[] raw)
        {
            generation=0;raw=Array.Empty<byte>();
            if(closed)return SlotStatus.Closed;
            if(quarantined||!worldOwner.Live)return SlotStatus.Quarantined;
            if(busy)return SlotStatus.Busy;
            busy=true;
            try{return ReadCore(out generation,out raw);}
            finally{busy=false;}
        }

        // Exact generation check and monotonic commit; no independent sidecar.
        // If the physical publish might have happened, even a lost ACK MUST
        // seal this owner. A fresh owner observes the authoritative final file.
        public SlotStatus TryCommit(long expectedGeneration,byte[] bytes)
        {
            if(closed)return SlotStatus.Closed;
            if(quarantined||!worldOwner.Live)return SlotStatus.Quarantined;
            if(busy)return SlotStatus.Busy;
            if(bytes==null||bytes.Length==0||bytes.Length>MaxBytes-128||
                !ColdRestore.TryStage((byte[])bytes.Clone(),out ColdRestore? cold)||
                cold==null||cold.Snapshot.RunId!=runId ||
                cold.Snapshot.Generation!=expectedGeneration+1 ||
                expectedGeneration<0||expectedGeneration==long.MaxValue)
                return SlotStatus.Invalid;
            busy=true;
            bool publicationAttempted=false;
            try
            {
                SlotStatus current=ReadCore(out long gen,out _);
                if(current!=SlotStatus.Accepted&&current!=SlotStatus.Empty)
                    return current;
                if(gen!=expectedGeneration)return SlotStatus.Stale;
                string destination=Path.Combine(dir,Final);
                string staged=Path.Combine(dir,
                    "pending-"+Guid.NewGuid().ToString("N")+".stage");
                byte[] encoded=EncodeEnvelope((byte[])bytes.Clone(),expectedGeneration+1);
                // CreateNew, no-overwrite, same-directory staging and Flush(true).
                using(var f=new FileStream(staged,FileMode.CreateNew,FileAccess.Write,
                    FileShare.None,65536,FileOptions.WriteThrough))
                {
                    f.Write(encoded,0,encoded.Length);
                    f.Flush(true);
                }
                // Verify the staged bytes before touching authoritative storage.
                if(DecodeEnvelope(File.ReadAllBytes(staged),out long checkGen,out _)!=
                    SlotStatus.Accepted || checkGen!=expectedGeneration+1)
                    return SlotStatus.Corrupt;
                // No foreign file deletion; abandoned stages are never recovered.
                publicationAttempted=true;
                publisher.Publish(staged,destination);
                // This readback is required: a successful return alone is not proof.
                SlotStatus after=ReadCore(out long observed,out byte[] published);
                if(after!=SlotStatus.Accepted||observed!=expectedGeneration+1||
                    !published.SequenceEqual(bytes))
                {
                    quarantined=true;
                    return SlotStatus.Quarantined;
                }
                return SlotStatus.Accepted;
            }
            catch
            {
                if(publicationAttempted)
                {
                    quarantined=true;
                    return SlotStatus.Quarantined;
                }
                return SlotStatus.IOError;
            }
            finally{busy=false;}
        }

        public void Dispose()
        {
            if(closed)return;
            closed=true;
            // The canonical GATEWAY alone owns the .owner FileStream lifetime.
            // Disposing this wrapper MUST NOT release any OS authority.
        }
    }
}

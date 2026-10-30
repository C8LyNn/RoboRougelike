// B01-62 W24 NEW candidate version based SOLE SELECTED #749 exact 8634e71dfcf633323073fe584a6c0884f1ca5d23.
// FROZEN #749 original source is unchanged. This is NOT yet OWNER SELECTED.
// P0 repair: single exact owner-bound canonical path + one escrowed full store per real lease.
// B01-62 W23 NEW VERSIONED exact #669-derived storage; original #669 SHA d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121 remains FROZEN.
// SAME original whole snapshot+revision+digest atomic envelope, but ONE #729 .owner lease (no .lease).
// New incompatible slot marker deliberately denies legacy raw #669 TryOpenExisting (and vice versa).
// B01-62 W20 SLOTSAVE-001/R1: a single atomic envelope binds frozen #556
// snapshot bytes and generation+run+slot digest in ONE file, not a second CAS sidecar.
// SOURCE-ONLY storage protocol, NOT an installation into the frozen #510 MonoBehaviour.
// Guarantees are bounded to filesystem atomic same-directory rename/process crashes;
// actual power-loss directory fsync is platform-dependent and NOT PROVEN.
#nullable enable
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using B01.DetachedSaveContract001;
using B01.CanonicalWorldApi001;

namespace B01.CanonicalSingleAuthorityStore004
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

    public sealed class CanonicalSingleAuthorityStoreV3R2 : IDisposable
    {
        private const string Magic = "B0162S01"; // 8-byte protocol tag
        private const int Version = 1;
        private const int MaxBytes = 8*1024*1024;
        private const string Marker = "slot.canonical-v3";
        // No slot.lease: only the #729 CanonicalWorldSlotLease .owner is authoritative.
        private const string Final = "save.envelope";
        private readonly string dir;
        private readonly long runId;
        private readonly Guid slotId;
        private readonly CanonicalWorldSlotLease authority;
        private readonly IAtomicEnvelopePublisher publisher;
        private bool quarantined, closed, busy;

        public bool Quarantined => quarantined;
        public bool Closed => closed;
        public long RunId => runId;
        public Guid SlotId => slotId;

        private CanonicalSingleAuthorityStoreV3R2(string dir,long run,Guid slot,
            CanonicalWorldSlotLease lease,IAtomicEnvelopePublisher writer)
        {
            this.dir=dir;runId=run;slotId=slot;authority=lease;publisher=writer;
        }

        private static string Identity(long run,Guid slot,string ownerIdentity)
            => "B0162_CANONICAL_ONLY_V3|"+run+"|"+slot.ToString("N")+"|"+ownerIdentity+"\n";

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

        // R2: the original #749 verified only run/slot, not physical Save path.
        // This owner object can now authorize exactly ONE canonical directory.
        private static bool OwnerMatches(CanonicalWorldSlotLease owner,long run,Guid slot,string directory)
        {
            if(owner==null||owner.RunId!=run||owner.SlotId!=slot)return false;
            try
            {
                string? leaseDirectory=Path.GetDirectoryName(owner.Identity);
                string? trustedPrivateRoot=leaseDirectory==null?null:
                    Path.GetDirectoryName(leaseDirectory);
                if(trustedPrivateRoot==null)return false;
                string expected=Path.Combine(trustedPrivateRoot,
                    "b0162_canonical_single_authority_v3",
                    "run_"+run.ToString(System.Globalization.CultureInfo.InvariantCulture)+
                    "_slot_"+slot.ToString("N"));
                if(!string.Equals(Path.GetFullPath(expected),
                    Path.GetFullPath(directory),StringComparison.OrdinalIgnoreCase))
                    return false;
                string baseDirectory=Path.GetDirectoryName(expected)!;
                if(Directory.Exists(baseDirectory) &&
                    (File.GetAttributes(baseDirectory)&FileAttributes.ReparsePoint)!=0)
                    return false;
                return true;
            }
            catch{return false;}
        }

        // Original #729's one physical OS lease prevents TWO lease owners, but
        // original #749 permitted TWO public store instances bound to SAME lease.
        // Escrow ONE full Save store per exact physical lease object, even after
        // storage.Dispose(); only a NEW physical lease object after original V3
        // verified retirement can replace this entry.
        private static readonly object SingleStoreGate=new object();
        private static readonly Dictionary<string,CanonicalSingleAuthorityStoreV3R2>
            StoresByOwner=new Dictionary<string,CanonicalSingleAuthorityStoreV3R2>(
                StringComparer.OrdinalIgnoreCase);

        private static bool DuplicateStore(CanonicalWorldSlotLease owner)
            => StoresByOwner.TryGetValue(owner.Identity,out var previous)&&
                ReferenceEquals(previous.authority,owner);

        private static SlotStatus TryCreateNewCore(string directory,long run,Guid slot,
            CanonicalWorldSlotLease owner,out CanonicalSingleAuthorityStoreV3R2? opened,
            IAtomicEnvelopePublisher? hook=null)
        {
            opened=null;
            if(!ValidPath(directory,run,slot)||
                !OwnerMatches(owner,run,slot,directory))return SlotStatus.Invalid;
            lock(SingleStoreGate)
            {
                if(DuplicateStore(owner))return SlotStatus.Busy;
                SlotStatus status=TryCreateNewCore(directory,run,slot,owner,
                    out opened,hook);
                if(status==SlotStatus.Accepted&&opened!=null)
                    StoresByOwner[owner.Identity]=opened;
                return status;
            }
        }
        private static SlotStatus TryOpenExistingCore(string directory,long run,Guid slot,
            CanonicalWorldSlotLease owner,out CanonicalSingleAuthorityStoreV3R2? opened,
            IAtomicEnvelopePublisher? hook=null)
        {
            opened=null;
            if(!ValidPath(directory,run,slot)||
                !OwnerMatches(owner,run,slot,directory))return SlotStatus.Invalid;
            lock(SingleStoreGate)
            {
                if(DuplicateStore(owner))return SlotStatus.Busy;
                SlotStatus status=TryOpenExistingCore(directory,run,slot,owner,
                    out opened,hook);
                if(status==SlotStatus.Accepted&&opened!=null)
                    StoresByOwner[owner.Identity]=opened;
                return status;
            }
        }


        public static SlotStatus TryCreateNew(string directory,long run,Guid slot,
            CanonicalWorldSlotLease owner,out CanonicalSingleAuthorityStoreV3R2? opened,
            IAtomicEnvelopePublisher? hook=null)
        {
            opened=null;
            if(!ValidPath(directory,run,slot)||!OwnerMatches(owner,run,slot,directory))return SlotStatus.Invalid;
            if(Directory.Exists(directory)||File.Exists(directory))
                return SlotStatus.WrongOwner; // preserve foreign EMPTY directories
            try
            {
                Directory.CreateDirectory(directory);
                using(var f=new FileStream(Path.Combine(directory,Marker),
                    FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    byte[] id=Encoding.ASCII.GetBytes(Identity(run,slot,owner.Identity));
                    f.Write(id,0,id.Length);
                    f.Flush(true);
                }
                if(IsUnsafeExisting(directory))return SlotStatus.WrongOwner;
                opened=new CanonicalSingleAuthorityStoreV3R2(directory,run,slot,owner,
                    hook??new OsAtomicEnvelopePublisher());
                return SlotStatus.Accepted;
            }
            catch(IOException){return SlotStatus.IOError;}
            catch(UnauthorizedAccessException){return SlotStatus.IOError;}
        }

        public static SlotStatus TryOpenExisting(string directory,long run,Guid slot,
            CanonicalWorldSlotLease owner,out CanonicalSingleAuthorityStoreV3R2? opened,
            IAtomicEnvelopePublisher? hook=null)
        {
            opened=null;
            if(!ValidPath(directory,run,slot)||!OwnerMatches(owner,run,slot,directory))return SlotStatus.Invalid;
            if(!Directory.Exists(directory)||IsUnsafeExisting(directory))
                return SlotStatus.WrongOwner;
            try
            {
                string marker=Path.Combine(directory,Marker);
                if(!File.Exists(marker)||IsUnsafeExisting(marker)||
                    File.ReadAllText(marker,Encoding.ASCII)!=Identity(run,slot,owner.Identity))
                    return SlotStatus.WrongOwner;
                if(IsUnsafeExisting(Path.Combine(directory,Final)))
                    return SlotStatus.WrongOwner;
                opened=new CanonicalSingleAuthorityStoreV3R2(directory,run,slot,owner,
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
            if(!authority.Live){quarantined=true;return SlotStatus.Quarantined;}
            if(quarantined)return SlotStatus.Quarantined;
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
            if(!authority.Live){quarantined=true;return SlotStatus.Quarantined;}
            if(quarantined)return SlotStatus.Quarantined;
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
            // Authority release belongs ONLY to original V3 gateway after proven roots retirement.
        }
    }
}

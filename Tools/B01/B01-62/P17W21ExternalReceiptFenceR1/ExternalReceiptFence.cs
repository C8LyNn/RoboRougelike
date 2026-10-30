#nullable enable
// B01-62 / W21 R1. Optional COLD-RESTART anti-rollback receipt, NOT a claim
// of cryptographic authenticity. The witness location MUST be trusted
// independently from the replaceable Save root. Do not install into game yet.
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using B01.SingleFileSaveSlot001;

namespace B01.ExternalReceiptFence001
{
    public enum FenceResult
    {
        Accepted, Empty, Stale, Fork, Quarantined, Invalid, IOError, Closed
    }

    public interface IReceiptPublisher
    {
        void Publish(string staged,string target);
    }

    public sealed class AtomicReceiptPublisher : IReceiptPublisher
    {
        public void Publish(string staged,string target)
            => File.Move(staged,target,true);
    }

    public readonly struct TrustedReceipt
    {
        public readonly long Generation;
        public readonly string PayloadSha;
        public TrustedReceipt(long generation,string payloadSha)
        {Generation=generation;PayloadSha=payloadSha;}
    }

    // "Trusted" here means caller policy ensures this separate root cannot be
    // replaced together with a backed-up Save. It is NOT guaranteed by SHA256.
    public sealed class ExternalReceiptAuthority : IDisposable
    {
        private const string Mark="witness.owner";
        private const string Lock="witness.lease";
        private const string Rec="witness.record";
        private const string Magic="B0162WR1";
        private readonly string directory;
        private readonly long run;
        private readonly Guid slot;
        private readonly FileStream lease;
        private readonly IReceiptPublisher publisher;
        private bool closed,quarantine;
        public bool Closed=>closed;
        public bool Quarantined=>quarantine;

        private ExternalReceiptAuthority(string dir,long r,Guid id,
            FileStream l,IReceiptPublisher p)
        {directory=dir;run=r;slot=id;lease=l;publisher=p;}

        private static bool Unsafe(string path)
        {
            try {
                if(!File.Exists(path)&&!Directory.Exists(path))return false;
                return (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0;
            }
            catch{return true;}
        }

        private static string Identity(long r,Guid id)
            =>"B0162-RECEIPT-OWNER-V1|"+r.ToString(CultureInfo.InvariantCulture)+
                "|"+id.ToString("N")+"\n";

        private static bool ValidArgs(string path,long run,Guid id)
            =>run>0&&id!=Guid.Empty&&!string.IsNullOrWhiteSpace(path)&&
              Path.IsPathFullyQualified(path);

        public static bool CreateOwned(string dir,long run,Guid id,
            out ExternalReceiptAuthority? receipt,IReceiptPublisher? publisher=null)
        {
            receipt=null;
            if(!ValidArgs(dir,run,id)||Directory.Exists(dir)||File.Exists(dir))
                return false; // refuse foreign EMPTY directories
            try
            {
                Directory.CreateDirectory(dir);
                byte[] identity=Encoding.ASCII.GetBytes(Identity(run,id));
                using(var f=new FileStream(Path.Combine(dir,Mark),FileMode.CreateNew,
                    FileAccess.Write,FileShare.None))
                {f.Write(identity,0,identity.Length);f.Flush(true);}
                FileStream l=new FileStream(Path.Combine(dir,Lock),FileMode.CreateNew,
                    FileAccess.ReadWrite,FileShare.None);
                receipt=new ExternalReceiptAuthority(dir,run,id,l,
                    publisher??new AtomicReceiptPublisher());
                if(!receipt.PublishInitial()) {receipt.Dispose();receipt=null;return false;}
                return true;
            }
            catch(IOException){return false;}
            catch(UnauthorizedAccessException){return false;}
        }

        public static bool OpenOwned(string dir,long run,Guid id,
            out ExternalReceiptAuthority? receipt,IReceiptPublisher? publisher=null)
        {
            receipt=null;
            if(!ValidArgs(dir,run,id)||!Directory.Exists(dir)||Unsafe(dir))
                return false;
            try
            {
                string marker=Path.Combine(dir,Mark),record=Path.Combine(dir,Rec);
                if(!File.Exists(marker)||Unsafe(marker)||!File.Exists(record)||
                    Unsafe(record)||Unsafe(Path.Combine(dir,Lock))||
                    File.ReadAllText(marker,Encoding.ASCII)!=Identity(run,id))
                    return false;
                FileStream l=new FileStream(Path.Combine(dir,Lock),FileMode.Open,
                    FileAccess.ReadWrite,FileShare.None);
                receipt=new ExternalReceiptAuthority(dir,run,id,l,
                    publisher??new AtomicReceiptPublisher());
                if(!receipt.TryRead(out _))
                {receipt.Dispose();receipt=null;return false;}
                return true;
            }
            catch(IOException){return false;}
            catch(UnauthorizedAccessException){return false;}
        }

        private byte[] Serialize(TrustedReceipt rec)
        {
            using var ms=new MemoryStream();
            using(var wr=new BinaryWriter(ms,Encoding.UTF8,true))
            {
                wr.Write(Encoding.ASCII.GetBytes(Magic));
                wr.Write(run);
                wr.Write(slot.ToByteArray());
                wr.Write(rec.Generation);
                wr.Write(rec.PayloadSha.Length==0?new byte[32]:
                    Convert.FromHexString(rec.PayloadSha));
                wr.Flush();
            }
            byte[] prefix=ms.ToArray();
            ms.Write(SHA256.HashData(prefix));
            return ms.ToArray();
        }

        private bool TryParse(byte[] data,out TrustedReceipt rec)
        {
            rec=new TrustedReceipt();
            try
            {
                if(data.Length!=104)return false;
                if(!CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(data.AsSpan(0,72)),data.AsSpan(72,32)))
                    return false;
                using var ms=new MemoryStream(data,false);
                using var rd=new BinaryReader(ms,Encoding.UTF8,true);
                if(Encoding.ASCII.GetString(rd.ReadBytes(8))!=Magic||
                    rd.ReadInt64()!=run||new Guid(rd.ReadBytes(16))!=slot)
                    return false;
                long gen=rd.ReadInt64();
                byte[] sha=rd.ReadBytes(32);
                if(gen<0 || (gen==0&&sha.Any(b=>b!=0)))return false;
                rec=new TrustedReceipt(gen,gen==0?"":Convert.ToHexString(sha));
                return true;
            }
            catch{return false;}
        }

        private bool PublishInitial()
        {
            // Own freshly-created root only. A fresh witness starts at zero.
            try
            {
                byte[] v=Serialize(new TrustedReceipt(0,""));
                using(var f=new FileStream(Path.Combine(directory,Rec),FileMode.CreateNew,
                    FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
                {f.Write(v,0,v.Length);f.Flush(true);}
                return true;
            }
            catch{return false;}
        }

        public bool TryRead(out TrustedReceipt record)
        {
            record=new TrustedReceipt();
            if(closed||quarantine)return false;
            string path=Path.Combine(directory,Rec);
            if(Unsafe(path)) {quarantine=true;return false;}
            try
            {
                if(!TryParse(File.ReadAllBytes(path),out record))
                {quarantine=true;return false;}
                return true;
            }
            catch {quarantine=true;return false;}
        }

        public bool TryAdvance(TrustedReceipt expected,long next,string nextSha)
        {
            if(closed||quarantine||expected.Generation<0||
               expected.Generation==long.MaxValue||next!=expected.Generation+1||
               nextSha==null||nextSha.Length!=64)
                return false;
            try{if(Convert.FromHexString(nextSha).Length!=32)return false;}
            catch{return false;}
            if(!TryRead(out TrustedReceipt existing)||
                existing.Generation!=expected.Generation||
                existing.PayloadSha!=expected.PayloadSha)
                {quarantine=true;return false;}
            string pending=Path.Combine(directory,
                "pending-"+Guid.NewGuid().ToString("N")+".witness");
            byte[] bytes=Serialize(new TrustedReceipt(next,nextSha));
            bool attempted=false;
            try
            {
                using(var f=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,
                    FileShare.None,4096,FileOptions.WriteThrough))
                {f.Write(bytes,0,bytes.Length);f.Flush(true);}
                attempted=true;
                publisher.Publish(pending,Path.Combine(directory,Rec));
                if(!TryRead(out TrustedReceipt saved)||
                    saved.Generation!=next||saved.PayloadSha!=nextSha)
                {quarantine=true;return false;}
                return true;
            }
            catch
            {
                // A durable rename could already have happened. No retry.
                if(attempted)quarantine=true;
                return false;
            }
        }

        public void Dispose()
        {
            if(closed)return;
            closed=true;lease.Dispose();
        }
    }

    // An additive opt-in storage guard. The trust root is chosen separately
    // by an authorized operator, NOT the same attacker-replaceable Save folder.
    // Does not change original #669 and cannot prevent raw #669 callers bypassing.
    public sealed class WitnessBoundSave
    {
        private readonly SingleFileSaveSlot slot;
        private readonly ExternalReceiptAuthority witness;
        private bool quarantine;
        public bool Quarantined=>quarantine||slot.Quarantined||witness.Quarantined;

        public WitnessBoundSave(SingleFileSaveSlot slot,ExternalReceiptAuthority witness)
        {this.slot=slot??throw new ArgumentNullException(nameof(slot));
         this.witness=witness??throw new ArgumentNullException(nameof(witness));}

        private static string Hash(byte[] data)
            =>Convert.ToHexString(SHA256.HashData(data));

        public FenceResult Read(out long generation,out byte[] bytes)
        {
            generation=0;bytes=Array.Empty<byte>();
            if(Quarantined)return FenceResult.Quarantined;
            if(slot.Closed||witness.Closed)return FenceResult.Closed;
            if(!witness.TryRead(out TrustedReceipt expected))
            {quarantine=true;return FenceResult.Quarantined;}
            SlotStatus result=slot.TryRead(out long physical,out byte[] payload);
            if(result==SlotStatus.Empty&&expected.Generation==0)return FenceResult.Empty;
            if(result!=SlotStatus.Accepted)
            {quarantine=true;return FenceResult.Quarantined;}
            if(physical<expected.Generation)
            {quarantine=true;return FenceResult.Stale;}
            if(physical>expected.Generation)
            {quarantine=true;return FenceResult.Quarantined;} // interrupted cross-file handshake
            if(Hash(payload)!=expected.PayloadSha)
            {quarantine=true;return FenceResult.Fork;}
            generation=physical;bytes=payload;
            return FenceResult.Accepted;
        }

        public FenceResult Commit(long previous,byte[] original556)
        {
            if(Quarantined)return FenceResult.Quarantined;
            if(original556==null)return FenceResult.Invalid;
            FenceResult r=Read(out long physical,out _);
            if(r!=FenceResult.Accepted&&r!=FenceResult.Empty)return r;
            if(physical!=previous) return FenceResult.Stale;
            if(!witness.TryRead(out TrustedReceipt before))
            {quarantine=true;return FenceResult.Quarantined;}
            // Original #669 validates frozen #556 RunId, generation and ID plan.
            SlotStatus persisted=slot.TryCommit(previous,original556);
            if(persisted!=SlotStatus.Accepted)
            {
                if(slot.Quarantined)quarantine=true;
                return persisted==SlotStatus.Stale?FenceResult.Stale:
                    persisted==SlotStatus.Invalid?FenceResult.Invalid:
                    FenceResult.Quarantined;
            }
            if(!witness.TryAdvance(before,previous+1,Hash(original556)))
            {quarantine=true;return FenceResult.Quarantined;}
            FenceResult after=Read(out long observed,out byte[] data);
            if(after!=FenceResult.Accepted||observed!=previous+1||
                !data.SequenceEqual(original556))
            {quarantine=true;return FenceResult.Quarantined;}
            return FenceResult.Accepted;
        }
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.SingleFileSaveSlot001;
namespace B01.CanonicalSingleAuthorityW23
{
    public enum SessionOpenResult { Accepted, Busy, Invalid, Corrupt, Foreign }
    // ONE physical #669 slot.lease for disk + world; no other OS owner lock.
    public sealed class CanonicalSingleAuthoritySession:ICanonicalCommitPort,IDisposable
    {
        private sealed class Physical669:ICanonicalExternalLease
        {
            private readonly SingleFileSaveSlot slot;
            private readonly string directory;
            public Physical669(SingleFileSaveSlot s,string p){slot=s;directory=p;}
            public long RunId=>slot.RunId;
            public Guid SlotId=>slot.SlotId;
            public string LeaseFile=>Path.Combine(directory,"slot.lease");
            public bool IsOpen=>!slot.Closed;
            public void CloseVerified()=>slot.Dispose();
        }
        private static readonly object Sync=new object();
        private static readonly List<CanonicalSingleAuthoritySession> Escrow=
            new List<CanonicalSingleAuthoritySession>();
        private readonly SingleFileSaveSlot slot;
        private readonly CanonicalWorldSlotLease token;
        private readonly long run;
        private CanonicalWorldGateway? gateway;
        private byte[]? armed;
        private string armedHash="";
        private bool restoring,closed,quarantined;
        private CanonicalSingleAuthoritySession(SingleFileSaveSlot s,CanonicalWorldSlotLease t)
        {slot=s;token=t;run=s.RunId;}
        public bool OwnsSlot=>!closed&&!slot.Closed;
        public bool Quarantined=>quarantined||slot.Quarantined||(gateway?.Quarantined??false);
        public bool HasWorld=>gateway?.HasWorld??false;
        public long Generation=>gateway?.Generation??0;
        public long RunId=>run;
        public Guid SlotId=>slot.SlotId;
        public static SessionOpenResult TryOpen(string dir,long run,Guid guid,
            bool create,out CanonicalSingleAuthoritySession? session,
            IAtomicEnvelopePublisher? publisher=null)
        {
            session=null;
            SlotStatus result=create
                ?SingleFileSaveSlot.TryCreateNew(dir,run,guid,out var s,publisher)
                :SingleFileSaveSlot.TryOpenExisting(dir,run,guid,out s,publisher);
            if(result!=SlotStatus.Accepted||s==null)
                return result==SlotStatus.Busy?SessionOpenResult.Busy:
                    result==SlotStatus.WrongOwner?SessionOpenResult.Foreign:
                    result==SlotStatus.Invalid?SessionOpenResult.Invalid:SessionOpenResult.Corrupt;
            if(!CanonicalWorldSlotLease.TryAdopt669(
                new Physical669(s,Path.GetFullPath(dir)),
                out CanonicalWorldSlotLease? token)||token==null)
            {s.Dispose();return SessionOpenResult.Busy;}
            session=new CanonicalSingleAuthoritySession(s,token);
            return SessionOpenResult.Accepted;
        }
        public bool Attach(ICanonicalMutationGate gate,ICanonicalRigFactory factory)
        {
            if(closed||Quarantined||gateway!=null||gate==null||factory==null)return false;
            try{gateway=new CanonicalWorldGateway(gate,factory,this,token);return true;}
            catch{quarantined=true;return false;}
        }
        private static string Hash(byte[] bytes)
        {
            using var d=SHA256.Create();
            return BitConverter.ToString(d.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
        }
        public CanonicalGateResult TryRestore(byte[] bytes,long runId,long minimum)
        {
            if(Quarantined)return CanonicalGateResult.RollbackQuarantine;
            if(closed||restoring||!OwnsSlot)return CanonicalGateResult.HostBusy;
            if(runId!=run)return CanonicalGateResult.WrongRun;
            if(gateway==null)return CanonicalGateResult.HostException;
            if(bytes==null||!ColdRestore.TryStage(bytes,out ColdRestore? cold)||
               cold==null||cold.Snapshot.RunId!=run)return CanonicalGateResult.InvalidSnapshot;
            restoring=true;armed=(byte[])bytes.Clone();armedHash=Hash(armed);
            try
            {
                CanonicalGateResult v=gateway.TryRestore(armed,runId,minimum);
                if(Quarantined){quarantined=true;return CanonicalGateResult.RollbackQuarantine;}
                return v;
            }
            catch{quarantined=true;return CanonicalGateResult.RollbackQuarantine;}
            finally{restoring=false;armed=null;armedHash="";}
        }
        public bool TryCapturePreview(out byte[] bytes)
        {
            bytes=Array.Empty<byte>();
            return !closed&&!restoring&&!Quarantined&&gateway!=null&&
                gateway.TryCapturePreview(out bytes);
        }
        public bool Read(long runId,out long generation,out string hash)
        {
            generation=0;hash="";
            if(closed||quarantined||slot.Quarantined||runId!=run)return false;
            SlotStatus s=slot.TryRead(out long epoch,out byte[] raw);
            if(s==SlotStatus.Empty)return true;
            if(s!=SlotStatus.Accepted||!ColdRestore.TryStage(raw,out ColdRestore? cold)||
               cold==null||cold.Snapshot.RunId!=run||cold.Snapshot.Generation!=epoch)
            {quarantined=true;return false;}
            generation=epoch;hash=Hash(raw);return true;
        }
        public bool Commit(long runId,long old,string oldHash,long next,string hash)
        {
            if(closed||quarantined||slot.Quarantined||!restoring||runId!=run||
               armed==null||old<0||old==long.MaxValue||next!=old+1||
               hash!=armedHash||oldHash==null)return false;
            if(!ColdRestore.TryStage(armed,out ColdRestore? cold)||cold==null||
               cold.Snapshot.RunId!=run||cold.Snapshot.Generation!=next)return false;
            if(!Read(run,out long previous,out string actualHash)||
               previous!=old||actualHash!=oldHash)
            {quarantined=true;return false;}
            SlotStatus status=slot.TryCommit(old,armed);
            if(status!=SlotStatus.Accepted){quarantined=true;return false;}
            if(!Read(run,out long verified,out string digest)||
               verified!=next||digest!=hash){quarantined=true;return false;}
            return true;
        }
        public bool TryCloseAfterPhysicalRetirement(Func<bool> rootsDestroyed)
        {
            if(closed)return true;
            if(restoring||rootsDestroyed==null)return false;
            bool done=gateway==null?token.TryCancelUnbound():
                gateway.TryCloseAfterPhysicalRetirement(rootsDestroyed);
            if(!done){quarantined=true;return false;}
            closed=true;lock(Sync)Escrow.Remove(this);return true;
        }
        public void Dispose()
        {
            if(closed)return;
            if(gateway==null){TryCloseAfterPhysicalRetirement(()=>true);return;}
            quarantined=true;
            lock(Sync){if(!Escrow.Contains(this))Escrow.Add(this);}
        }
    }
}

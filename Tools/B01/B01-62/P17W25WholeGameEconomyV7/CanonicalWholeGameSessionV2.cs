// B01-62 W25 NEW V2 canonical original #729 API adapter for original #780 V7 section under sole .owner.
// B01-62 W25 NEW versioned canonical WHOLE GAME session. Only original #729
// CanonicalWorldGateway may own one physical .owner; ONE game+econ Store provides
// the durable ICanonicalCommitPort. No #749/#773 active writer may co-install.
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.WholeGameEconomy002;
using RoboRougelike.B01.Economy.ApprovedV7;

namespace B01.CanonicalWholeGameAdapter002
{
    public enum WholeGameSessionOpen { Accepted, Busy, Invalid, WrongOwner, IOError }

    public sealed class CanonicalWholeGameSessionV2 : ICanonicalCommitPort, IDisposable
    {
        private readonly CanonicalWorldSlotLease lease;
        private readonly CanonicalWorldGateway gateway;
        private readonly long runId;
        private readonly Guid slotId;
        private WholeGameEconomySlotV2? store;
        private byte[]? armedWorld;
        private ApprovedEconomySnapshotV7? armedEconomy;
        private bool restoring,closed,quarantined;
        private static readonly object EscrowGate=new object();
        private static readonly List<CanonicalWholeGameSessionV2> Unretired=
            new List<CanonicalWholeGameSessionV2>();

        private CanonicalWholeGameSessionV2(long run,Guid slot,
            CanonicalWorldSlotLease physical,ICanonicalMutationGate gate,
            ICanonicalRigFactory factory)
        {
            runId=run;slotId=slot;lease=physical;
            gateway=new CanonicalWorldGateway(gate,factory,this,physical);
        }
        public long RunId=>runId;
        public Guid SlotId=>slotId;
        public bool HasWorld=>gateway.HasWorld;
        public bool Quarantined=>quarantined||gateway.Quarantined||
            (store?.Quarantined??true);
        public bool OwnsSlot=>!closed&&lease.Live;

        private static WholeGameSessionOpen Status(WholeGameResult s)
            =>s==WholeGameResult.Busy?WholeGameSessionOpen.Busy:
              s==WholeGameResult.WrongOwner?WholeGameSessionOpen.WrongOwner:
              s==WholeGameResult.Invalid?WholeGameSessionOpen.Invalid:
              WholeGameSessionOpen.IOError;

        // Original .owner is physically obtained FIRST and bound by the exact
        // original #729 Gateway before opening the one whole-game store.
        // The factory cannot stage physics until TryRestore is called.
        public static WholeGameSessionOpen TryOpen(string trustedPrivateRoot,
            long run,Guid slot,bool create,ICanonicalMutationGate gate,
            ICanonicalRigFactory factory,out CanonicalWholeGameSessionV2? session,
            IOneFilePublisher? publisher=null)
        {
            session=null;
            if(gate==null||factory==null||
                !CanonicalWorldSlotLease.TryAcquire(trustedPrivateRoot,run,slot,
                    out CanonicalWorldSlotLease? lease)||lease==null)
                return WholeGameSessionOpen.Busy;
            CanonicalWholeGameSessionV2? newSession=null;
            try
            {
                newSession=new CanonicalWholeGameSessionV2(run,slot,lease,gate,factory);
                string root=System.IO.Path.GetFullPath(trustedPrivateRoot);
                string basePath=System.IO.Path.Combine(root,
                    "b0162_canonical_single_authority_v3");
                if(System.IO.File.Exists(basePath)||
                   (System.IO.Directory.Exists(basePath)&&
                   (System.IO.File.GetAttributes(basePath)&
                    System.IO.FileAttributes.ReparsePoint)!=0))
                {
                    newSession.gateway.TryCloseAfterPhysicalRetirement(()=>true);
                    return WholeGameSessionOpen.WrongOwner;
                }
                System.IO.Directory.CreateDirectory(basePath);
                string directory=System.IO.Path.Combine(basePath,
                    "run_"+run.ToString(CultureInfo.InvariantCulture)+
                    "_slot_"+slot.ToString("N"));
                WholeGameResult status=create
                    ?WholeGameEconomySlotV2.Create(directory,run,slot,lease,
                        out var opened,publisher)
                    :WholeGameEconomySlotV2.Open(directory,run,slot,lease,
                        out opened,publisher);
                if(status==WholeGameResult.Accepted&&opened!=null)
                {
                    newSession.store=opened;session=newSession;
                    return WholeGameSessionOpen.Accepted;
                }
                // No world stage was created. The original V3 gateway performs
                // the ONLY physical lock release after verified empty root list.
                newSession.gateway.TryCloseAfterPhysicalRetirement(()=>true);
                return Status(status);
            }
            catch
            {
                newSession?.gateway.TryCloseAfterPhysicalRetirement(()=>true);
                return WholeGameSessionOpen.IOError;
            }
        }
        private static string Fingerprint(byte[] data)
            =>Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
        private bool ValidatePair(byte[] world,ApprovedEconomySnapshotV7 economy)
        {
            if(world==null||economy==null||!ApprovedEconomyPolicyV7.Valid(economy)||
               !ColdRestore.TryStage(world,out ColdRestore? staged)||staged==null||
               staged.Snapshot.RunId!=runId||
               economy.OwnerId!=slotId.ToString("N")||
               economy.RunId!=runId.ToString(CultureInfo.InvariantCulture)||
               economy.Generation!=staged.Snapshot.Generation)
                return false;
            var expected=staged.Snapshot.Parts.Select(x=>
                WholeGameEconomySlotV2.EconomicPartId(x.Id)).ToArray();
            return expected.Length==economy.Parts.Count &&
                new HashSet<string>(expected,StringComparer.Ordinal).SetEquals(
                    economy.Parts.Select(x=>x.Id));
        }
        private bool EquivalentEconomy(ApprovedEconomySnapshotV7 a,
            ApprovedEconomySnapshotV7 b)
            => CanonicalEconomySectionV7.TryEncode(a,out byte[] left,
                    out string _) &&
               CanonicalEconomySectionV7.TryEncode(b,out byte[] right,
                    out string _) && left.SequenceEqual(right);

        // This is a true pair transaction, not a half-economy API. On replay
        // a same-world-generation different wallet is an explicit fork RED.
        public CanonicalGateResult TryRestore(byte[] world,
            ApprovedEconomySnapshotV7 economy,long expectedRun,long minimum)
        {
            if(Quarantined)return CanonicalGateResult.RollbackQuarantine;
            if(closed||restoring)return CanonicalGateResult.HostBusy;
            if(expectedRun!=runId||!ValidatePair(world,economy))
                return CanonicalGateResult.InvalidSnapshot;
            if(store==null)return CanonicalGateResult.HostException;
            restoring=true;
            try
            {
                WholeGameResult existing=store.Read(out long diskGen,
                    out byte[] priorWorld,out ApprovedEconomySnapshotV7? priorEcon);
                if(existing!=WholeGameResult.Accepted&&existing!=WholeGameResult.Empty)
                {quarantined=true;return CanonicalGateResult.RollbackQuarantine;}
                long proposed=economy.Generation;
                if(proposed<minimum || proposed<diskGen||
                   diskGen==long.MaxValue||
                   proposed>diskGen+1)
                    return CanonicalGateResult.StaleGeneration;
                if(proposed==diskGen&&
                   (priorEcon==null||!priorWorld.SequenceEqual(world)||
                    !EquivalentEconomy(priorEcon,economy)))
                    return CanonicalGateResult.StaleGeneration;
                armedWorld=(byte[])world.Clone();
                armedEconomy=economy.Copy();
                CanonicalGateResult outcome=gateway.TryRestore(world,runId,minimum);
                if(gateway.Quarantined||store.Quarantined)
                {quarantined=true;return CanonicalGateResult.RollbackQuarantine;}
                return outcome;
            }
            catch{quarantined=true;return CanonicalGateResult.RollbackQuarantine;}
            finally{restoring=false;armedWorld=null;armedEconomy=null;}
        }
        public bool Read(long run,out long generation,out string hash)
        {
            generation=0;hash="";
            if(closed||quarantined||run!=runId||store==null)return false;
            var result=store.Read(out long epoch,out byte[] world,out var econ);
            if(result==WholeGameResult.Empty)return true;
            if(result!=WholeGameResult.Accepted||econ==null||
                !ValidatePair(world,econ))
            {quarantined=true;return false;}
            generation=epoch;hash=Fingerprint(world);
            return true;
        }
        public bool Commit(long run,long prior,string priorHash,
            long next,string hash)
        {
            if(closed||Quarantined||store==null||!restoring||
               run!=runId||next<=0||prior<0||next!=prior+1||
               armedWorld==null||armedEconomy==null||
               armedEconomy.Generation!=next||hash!=Fingerprint(armedWorld)||
               priorHash==null)return false;
            if(!Read(run,out long disk,out string oldHash)||disk!=prior||
               oldHash!=priorHash)
            {quarantined=true;return false;}
            WholeGameResult committed=store.Commit(prior,armedWorld,armedEconomy);
            if(committed!=WholeGameResult.Accepted)
            {quarantined=true;return false;}
            WholeGameResult after=store.Read(out long current,
                out byte[] world,out var econ);
            if(after!=WholeGameResult.Accepted||current!=next||econ==null||
               !world.SequenceEqual(armedWorld)||
               !EquivalentEconomy(econ,armedEconomy))
            {quarantined=true;return false;}
            return true;
        }
        public bool TryCapture(out byte[] frozen556,
            out ApprovedEconomySnapshotV7? economic)
        {
            frozen556=Array.Empty<byte>();economic=null;
            if(closed||Quarantined||store==null||restoring||!gateway.HasWorld||
               !gateway.TryCapturePreview(out frozen556))return false;
            // Moving Rigidbody transforms/velocity legitimately differ from the
            // last durable checkpoint at the SAME generation. The genuine #729
            // gateway already authenticates the published disk checkpoint; do
            // NOT compare the live physics preview bytes to frozen disk bytes.
            if(store.Read(out long g,out _,out var econ)!=
                WholeGameResult.Accepted||econ==null||g!=gateway.Generation||
                !ColdRestore.TryStage(frozen556,out ColdRestore? live)||
                live==null||live.Snapshot.RunId!=runId||
                live.Snapshot.Generation!=g)
            {quarantined=true;return false;}
            economic=econ.Copy();return true;
        }

        public bool TryCloseAfterPhysicalRetirement(Func<bool> allRootsDestroyed)
        {
            if(closed)return true;
            if(restoring||allRootsDestroyed==null)return false;
            if(!gateway.TryCloseAfterPhysicalRetirement(allRootsDestroyed))
                return false;
            store?.Dispose();closed=true;return true;
        }
        public void Dispose()
        {
            if(closed)return;
            quarantined=true;
            lock(EscrowGate)if(!Unretired.Contains(this))Unretired.Add(this);
            // Deliberately cannot unlock an unresolved partial Unity world.
        }
    }
}

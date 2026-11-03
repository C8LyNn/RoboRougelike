// B01-62 W29: read-only cold boot transport for exact #826 one-file V3.
// This adds NO writer, no alternate disk format and no .lease or sidecar.
// A temporary original #729 .owner lifetime authenticates the on-disk pair;
// it is VERIFIABLY retired before any bytes are handed back to the caller.
#nullable enable
using System;
using System.Globalization;
using System.IO;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.WholeGameEconomy003;
using RoboRougelike.B01.Economy.ApprovedV7;

namespace B01.CanonicalColdRead003
{
    public static class CanonicalColdReadV3
    {
        private sealed class ReadOnlyGate:ICanonicalMutationGate
        {
            public bool IsQuiescent()=>false; // never stage a physical scene
        }
        private sealed class NoWorldFactory:ICanonicalRigFactory
        {
            public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,ColdRestore plan)
                =>throw new InvalidOperationException("Cold reader may not stage Unity physics");
        }
        private sealed class NoWritePort:ICanonicalCommitPort
        {
            public bool Read(long run,out long generation,out string hash)
            {generation=0;hash="";return false;}
            public bool Commit(long run,long prior,string hash,long next,string nextHash)
                =>false; // no commit capability
        }

        public static WholeGameResult ReadClosedSlot(string trustedPrivateRoot,
            long run,Guid slot,out long generation,out byte[] world,
            out ApprovedEconomySnapshotV7? economy)
        {
            generation=0;world=Array.Empty<byte>();economy=null;
            if(string.IsNullOrWhiteSpace(trustedPrivateRoot)||run<=0||slot==Guid.Empty)
                return WholeGameResult.Invalid;
            string root,storeDir;
            try
            {
                root=Path.GetFullPath(trustedPrivateRoot);
                if(!Directory.Exists(root))return WholeGameResult.Invalid;
                storeDir=Path.Combine(root,"b0162_canonical_single_authority_v3",
                    "run_"+run.ToString(CultureInfo.InvariantCulture)+
                    "_slot_"+slot.ToString("N"));
                // Empty cold boot is NON-DESTRUCTIVE; do not create .owner.
                if(!Directory.Exists(storeDir)&&!File.Exists(storeDir))
                    return WholeGameResult.Empty;
            }
            catch{return WholeGameResult.Invalid;}

            CanonicalWorldSlotLease? owner=null;
            CanonicalWorldGateway? gateway=null;
            WholeGameEconomySlotV3? store=null;
            WholeGameResult status=WholeGameResult.IOError;
            long stagedGeneration=0;
            byte[] stagedWorld=Array.Empty<byte>();
            ApprovedEconomySnapshotV7? stagedEconomy=null;
            bool releaseProven=false;
            try
            {
                if(!CanonicalWorldSlotLease.TryAcquire(root,run,slot,out owner)||
                    owner==null)return WholeGameResult.Busy;
                // Binding an ORIGINAL gateway is mandatory before Store.Open
                // (the original lease is not Live before owner.BindOnce).
                gateway=new CanonicalWorldGateway(new ReadOnlyGate(),
                    new NoWorldFactory(),new NoWritePort(),owner);
                status=WholeGameEconomySlotV3.Open(storeDir,run,slot,owner,out store);
                if(status==WholeGameResult.Accepted&&store!=null)
                    status=store.Read(out stagedGeneration,out stagedWorld,
                        out stagedEconomy);
            }
            catch(Exception)
            {
                status=WholeGameResult.IOError;
            }
            finally
            {
                try{store?.Dispose();}catch{status=WholeGameResult.Quarantined;}
                if(gateway!=null)
                {
                    try{releaseProven=gateway.TryCloseAfterPhysicalRetirement(()=>true);}
                    catch{releaseProven=false;}
                }
                else if(owner!=null)
                {
                    try{releaseProven=owner.TryCancelUnbound();}
                    catch{releaseProven=false;}
                }
            }
            // A leaked OS owner is a HARD HOLD: never pass a cold payload
            // to an integrator that might re-open a competing session.
            if(!releaseProven)return WholeGameResult.Quarantined;
            if(status!=WholeGameResult.Accepted||stagedEconomy==null||
                stagedGeneration<=0||stagedWorld.Length==0)
                return status==WholeGameResult.Accepted
                    ?WholeGameResult.Corrupt:status;
            generation=stagedGeneration;
            world=(byte[])stagedWorld.Clone();
            economy=stagedEconomy.Copy();
            return WholeGameResult.Accepted;
        }
    }
}

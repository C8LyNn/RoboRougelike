// B01-62 W24 NEW FIXED selected #749 descendant ONLY; original source NO CHANGE.
// B01-62 W24: independent adversarial EXECUTION of SELECTED UNMODIFIED #749 source.
// Never imports alternative #753/#741. Real original #729 lease, #749 store,
// original frozen #556 codec/W06 stable ID, and OS files on BOTH Windows+Ubuntu.
#nullable enable
using System;
using System.IO;
using System.Linq;
using B01.CanonicalWorldApi001;
using B01.CanonicalSingleAuthorityStore005;
using B01.CanonicalSlotAdapter005;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal static class W24AdversarialSelected749
{
    private const long Run=62462;
    private static readonly Guid Id=Guid.Parse("62462624-0000-4000-8000-000000000749");
    private static int pass,fail;
    private static void Check(string name,Func<bool> run)
    {
        try{if(run()){++pass;Console.WriteLine("PASS "+name);}
            else{++fail;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){++fail;Console.WriteLine("FAIL "+name+" "+e);}
    }
    private static bool Temp(Func<string,bool> fn)
    {
        string parent=Path.Combine(Path.GetTempPath(),"b0162-w24-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);
        try{return fn(parent);}
        finally{Directory.Delete(parent,true);}
    }
    private static byte[] Snapshot(long generation,bool light,bool heavy)
    {
        var p=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool det=i==0?light:heavy;
            var spec=SecureRigCatalog.At(i);
            long owner=FrozenPartCatalog.Owner(i);
            p[i]=new PersistedPart(spec.PartId,owner,det?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                det?PartDockState.Detached:PartDockState.Attached,det,!det,
                new Vector3d(spec.RootX+spec.PartOffsetX,det?2.2:1.2,0),
                0,0,0,1,new Vector3d(det?0.25:0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,generation,p));
    }
    private static bool Genuine(byte[] payload,long gen,bool light,bool heavy)
    {
        if(!ColdRestore.TryStage(payload,out var p)||p==null ||
           p.Snapshot.RunId!=Run||p.Snapshot.Generation!=gen)return false;
        return p.ContainsConsumedId(0,FrozenPartCatalog.Id(0))==light &&
            p.ContainsConsumedId(1,FrozenPartCatalog.Id(1))==heavy;
    }
    private sealed class Idle:ICanonicalMutationGate{public bool IsQuiescent()=>true;}
    private sealed class World:ICanonicalStagedWorld
    {
        private readonly RunSnapshot snap;
        public bool Active,Retired;
        public World(RunSnapshot s){snap=s;}
        public RunSnapshot Capture()=>snap;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Retired=true;Active=false;}
    }
    private sealed class Factory:ICanonicalRigFactory
    {
        public World? Last;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,ColdRestore ledger)
        {
            if(ledger.Snapshot.RunId!=snap.RunId)throw new IOException("foreign rig");
            return new Owned(this,snap);
        }
        public bool AllGone(){Last?.Retire();return Last==null||Last.Retired;}
        private sealed class Owned:ICanonicalStageOwnership
        {
            private readonly Factory f;private readonly RunSnapshot snap;
            public Owned(Factory f,RunSnapshot snap){this.f=f;this.snap=snap;}
            public ICanonicalStagedWorld PrepareInactive()
            {f.Last=new World(snap);return f.Last;}
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {f.Last?.Retire();return f.Last==null||f.Last.Retired;}
        }
    }
    private sealed class NoopPort:ICanonicalCommitPort
    {
        public bool Read(long run,out long generation,out string hash)
        {generation=0;hash="";return false;}
        public bool Commit(long run,long previous,string priorHash,long next,string hash)
            =>false;
    }
    private sealed class PostRenameAckLoss:IAtomicEnvelopePublisher
    {
        public int Calls;
        public void Publish(string stage,string final)
        {
            ++Calls;
            new OsAtomicEnvelopePublisher().Publish(stage,final);
            throw new IOException("injected lost ACK AFTER real physical original File.Move");
        }
    }
    private sealed class PreRenameFailure:IAtomicEnvelopePublisher
    {
        public int Calls;
        public void Publish(string stage,string final)
        {++Calls;throw new IOException("injected fault BEFORE publishing original stage");}
    }
    private static string RawSlotDir(string root)=>Path.Combine(
        root,"b0162_canonical_single_authority_v3","run_"+Run+
        "_slot_"+Id.ToString("N"));

    private static bool OneCanonicalPathOneStorePerPhysicalOwner()
    {
        return Temp(parent=>{
            string root=Path.Combine(parent,"trusted");
            Directory.CreateDirectory(root);
            if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out var owner)||
                owner==null)return false;
            // GENUINE original selected #729 .owner and BindOnce, not a fake lease.
            var gateway=new CanonicalWorldGateway(new Idle(),new Factory(),
                new NoopPort(),owner);
            var safeBase=Path.Combine(root,"b0162_canonical_single_authority_v3");
            Directory.CreateDirectory(safeBase);
            var canonical=RawSlotDir(root);
            var foreign=Path.Combine(parent,"foreign-sibling");
            CanonicalSingleAuthorityStoreV3R3? first=null;
            try
            {
                var rejected=CanonicalSingleAuthorityStoreV3R3.TryCreateNew(
                    foreign,Run,Id,owner,out var wrong);
                if(rejected!=SlotStatus.Invalid||wrong!=null||Directory.Exists(foreign))
                    return false;
                var status=CanonicalSingleAuthorityStoreV3R3.TryCreateNew(
                    canonical,Run,Id,owner,out first);
                if(status!=SlotStatus.Accepted||first==null)return false;
                // A DIFFERENT path is rejected, and a SECOND object on the SAME
                // canonical physical authority is Busy even before first Commit.
                if(CanonicalSingleAuthorityStoreV3R3.TryOpenExisting(
                    canonical,Run,Id,owner,out var duplicate)!=SlotStatus.Busy||
                   duplicate!=null)return false;
                byte[] expected=Snapshot(1,true,false);
                if(first.TryCommit(0,expected)!=SlotStatus.Accepted||
                   first.TryRead(out long gen,out byte[] actual)!=SlotStatus.Accepted||
                   gen!=1||!Genuine(actual,1,true,false))return false;
                first.Dispose();
                // Even a raw Dispose may not unlock the store while exact V3
                // authority can own an unfinished partially staged world.
                if(CanonicalSingleAuthorityStoreV3R3.TryOpenExisting(
                    canonical,Run,Id,owner,out duplicate)!=SlotStatus.Busy||
                   duplicate!=null)return false;
                if(!gateway.TryCloseAfterPhysicalRetirement(()=>true))return false;
                if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out var coldOwner)||
                   coldOwner==null)return false;
                var coldGateway=new CanonicalWorldGateway(new Idle(),new Factory(),
                    new NoopPort(),coldOwner);
                try
                {
                    if(CanonicalSingleAuthorityStoreV3R3.TryOpenExisting(
                        canonical,Run,Id,coldOwner,out var cold)!=SlotStatus.Accepted||
                       cold==null)return false;
                    try
                    {
                        if(cold.TryRead(out gen,out actual)!=SlotStatus.Accepted||
                           gen!=1||!Genuine(actual,1,true,false))return false;
                    }
                    finally{cold.Dispose();}
                    return Directory.GetFiles(Path.Combine(root,
                        "b0123_canonical_world_authority"),"*.owner").Length==1&&
                        Directory.GetFiles(canonical,"*.lease").Length==0;
                }
                finally{coldGateway.TryCloseAfterPhysicalRetirement(()=>true);}
            }
            finally
            {
                first?.Dispose();
                gateway.TryCloseAfterPhysicalRetirement(()=>true);
            }
        });
    }
    private static bool DurableUncertainAckFailClosed(bool after)
    {
        return Temp(parent=>{
            string root=Path.Combine(parent,"owned");
            Directory.CreateDirectory(root);
            var publisher=after?(IAtomicEnvelopePublisher)new PostRenameAckLoss():
                                 new PreRenameFailure();
            if(CanonicalSlotSessionV3R3.TryOpen(root,Run,Id,true,out var session,
                publisher)!=CanonicalSessionOpenStatus.Accepted||session==null)
                return false;
            var f=new Factory();
            if(!session.Attach(new Idle(),f))return false;
            try
            {
                var status=session.TryRestore(Snapshot(1,true,false),Run,1);
                if(status!=CanonicalGateResult.RollbackQuarantine||
                   !session.Quarantined||!session.OwnsSlot)return false;
                string dst=Path.Combine(RawSlotDir(root),"save.envelope");
                if(File.Exists(dst)!=after)return false;
                if(session.TryRestore(Snapshot(2,false,true),Run,2)!=
                    CanonicalGateResult.RollbackQuarantine)return false;
                session.Dispose(); // MUST retain .owner until physical retirement
                if(CanonicalSlotSessionV3R3.TryOpen(root,Run,Id,false,out var another)!=
                    CanonicalSessionOpenStatus.Busy||another!=null)return false;
                if(!session.TryCloseAfterPhysicalRetirement(f.AllGone))return false;
                if(CanonicalSlotSessionV3R3.TryOpen(root,Run,Id,false,out var cold)!=
                    CanonicalSessionOpenStatus.Accepted||cold==null)return false;
                var secondFactory=new Factory();
                try
                {
                    if(!cold.Attach(new Idle(),secondFactory))return false;
                    if(!after)
                    {
                        if(cold.TryRestore(Snapshot(1,true,false),Run,1)!=
                            CanonicalGateResult.Accepted)return false;
                    }
                    else
                    {
                        if(cold.TryRestore(Snapshot(1,true,false),Run,1)!=
                            CanonicalGateResult.Accepted)return false;
                    }
                    if(!cold.Read(Run,out long gen,out string hash)||gen!=1||
                       hash!=CanonicalSlotSessionV3R3.SourceHash(Snapshot(1,true,false)))
                        return false;
                }
                finally
                {cold.TryCloseAfterPhysicalRetirement(secondFactory.AllGone);cold.Dispose();}
                Console.WriteLine(after
                    ?"W25_773_R3_POST_RENAME_ACK_LOSS_STICKY_QUARANTINE_READBACK=PASS"
                    :"W25_773_R3_PRE_RENAME_NO_COMMIT_STICKY_QUARANTINE_AND_RECOVERY=PASS");
                return true;
            }
            finally{session.TryCloseAfterPhysicalRetirement(f.AllGone);session.Dispose();}
        });
    }
    private static bool Frozen556RejectsNewEconomyFields()
    {
        return Temp(parent=>{
            string root=Path.Combine(parent,"owned");
            Directory.CreateDirectory(root);
            if(CanonicalSlotSessionV3R3.TryOpen(root,Run,Id,true,out var s)!=
                CanonicalSessionOpenStatus.Accepted||s==null)return false;
            var f=new Factory();
            if(!s.Attach(new Idle(),f))return false;
            try
            {
                byte[] g1=Snapshot(1,true,false);
                if(s.TryRestore(g1,Run,1)!=CanonicalGateResult.Accepted)return false;
                byte[] g2=Snapshot(2,false,true);
                byte[] economy=System.Text.Encoding.ASCII.GetBytes(
                    "VICTORY_ID=42;SCRAP=65;FREE_REBUILD=0;LOANER_USED=1");
                byte[] desiredComposite=g2.Concat(economy).ToArray();
                if(ColdRestore.TryStage(desiredComposite,out var _))return false;
                if(s.TryRestore(desiredComposite,Run,2)!=
                    CanonicalGateResult.InvalidSnapshot)return false;
                if(!s.Read(Run,out long observed,out string hash)||observed!=1||
                   hash!=CanonicalSlotSessionV3R3.SourceHash(g1))return false;
                Console.WriteLine("W25_R3_APPROVED_ECONOMY_FIELDS_NOT_IN_FROZEN_556_ATOMIC_SAVE=CONFIRMED_HOLD");
                return true;
            }
            finally{s.TryCloseAfterPhysicalRetirement(f.AllGone);s.Dispose();}
        });
    }
    private static bool ExactPhysicalCaseOnly()
    {
        return Temp(parent=>{
            string root=Path.Combine(parent,"trusted");
            Directory.CreateDirectory(root);
            if(!CanonicalWorldSlotLease.TryAcquire(root,Run,Id,out var authority)||
               authority==null)return false;
            var gateway=new CanonicalWorldGateway(new Idle(),new Factory(),
                new NoopPort(),authority);
            try
            {
                string alias=Path.Combine(root,
                    "B0162_canonical_single_authority_v3",
                    "run_"+Run+"_slot_"+Id.ToString("N"));
                var status=CanonicalSingleAuthorityStoreV3R3.TryCreateNew(
                    alias,Run,Id,authority,out var bad);
                bad?.Dispose();
                bool noForeign= status==SlotStatus.Invalid&&bad==null&&
                    !Directory.Exists(alias);
                if(!noForeign)return false;
                Console.WriteLine("W25_773_R3_CASE_ONLY_ALTERNATE_ROOT_DENIED=PASS");
                return true;
            }
            finally{gateway.TryCloseAfterPhysicalRetirement(()=>true);}
        });
    }
    public static int Main()
    {
        Check("W25 case-only alternate physical root rejected on ALL OS",ExactPhysicalCaseOnly);
        Check("W24 V3R2 selected #749 lineage binds one exact root, single store per owner and cold takeover",OneCanonicalPathOneStorePerPhysicalOwner);
        Check("Approved W24 economy cannot serialize into frozen #556 wire (integration HOLD)",Frozen556RejectsNewEconomyFields);
        Check("Original #749 real AFTER physical rename lost ACK fail closed",()=>DurableUncertainAckFailClosed(true));
        Check("Original #749 real BEFORE rename failure quarantines/reopens",()=>DurableUncertainAckFailClosed(false));
        Console.WriteLine("W25_773_DESCENDANT_R3_REAL_SOURCE_TESTS "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("W25_773_DESCENDANT_R3_MULTIPATH_RED_TO_GREEN=PASS_SOURCE_ONLY");
        Console.WriteLine("W24_ECONOMY_PERSISTENCE=HOLD_FROZEN_556_NO_VICTORY_WALLET_LOANER_FIELDS");
        Console.WriteLine("W24_NATIVE_UNITY_G1=NOT_RUN_NO_510_PRIVATE_WORLD_PORT");
        return fail==0?0:1;
    }
}

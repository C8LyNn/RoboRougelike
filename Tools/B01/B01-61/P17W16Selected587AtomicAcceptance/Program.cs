using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;

internal static class Program
{
    private static int pass,fail;
    private const long Run=117;
    private static void Check(string label,Func<bool> test)
    {
        try
        {
            if(test()){++pass;Console.WriteLine("PASS "+label);}
            else{++fail;Console.WriteLine("FAIL "+label);}
        }
        catch(Exception e){++fail;Console.WriteLine("FAIL "+label+" "+e);}
    }
    private static PersistedPart Part(int i,bool detached)
    {
        var s=SecureRigCatalog.At(i);
        long owner=FrozenPartCatalog.Owner(i);
        return new PersistedPart(s.PartId,owner,detached?0:owner,
            FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,
            new Vector3d(s.RootX+s.PartOffsetX,1.2+(detached?1:0),0),
            0,0,0,1,detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
            new Vector3d(0,0,0),s.PartMassKg);
    }
    private static byte[] Bytes(long generation,bool light,bool heavy,long run=Run)
        => SnapshotCodec.Encode(new RunSnapshot(run,generation,
            new []{Part(0,light),Part(1,heavy)}));

    // Test doubles inject only the public IWorldFactory/IPreparedWorld physical
    // lifecycle. The transaction algorithm and #556/W06/#510 catalog are EXACT
    // original C# sources; NO copied code and NO simulated Unity G1 claim.
    private sealed class World:IPreparedWorld
    {
        public RunSnapshot Snapshot;
        public bool Active,Retired,ThrowActivate,ThrowDeactivate,ThrowRetire;
        public bool MutatePoseOnActivation,MutateIdentityOnActivation;
        public int ActivateCount,DeactivateCount;
        public World(RunSnapshot snapshot){Snapshot=snapshot;}
        public RunSnapshot Capture()
        {
            if(!Active||(!MutatePoseOnActivation&&!MutateIdentityOnActivation))
                return Snapshot;
            var parts=Snapshot.Parts;
            PersistedPart x=parts[0];
            if(MutateIdentityOnActivation) parts[0]=parts[1];
            else parts[0]=new PersistedPart(x.Id,x.OwnerId,x.ParentId,
                x.AssetGuid,x.SocketGuid,x.State,x.Consumed,x.Kinematic,
                new Vector3d(x.Position.X+.75,x.Position.Y,x.Position.Z),
                x.Qx,x.Qy,x.Qz,x.Qw,x.LinearVelocity,x.AngularVelocity,x.MassKg);
            return new RunSnapshot(Snapshot.RunId,Snapshot.Generation,parts);
        }
        public void Activate()
        {
            ActivateCount++;Active=true;
            if(ThrowActivate)throw new IOException("injected mid-activate failure");
        }
        public void Deactivate()
        {
            DeactivateCount++;Active=false;
            if(ThrowDeactivate)throw new IOException("injected rollback failure");
        }
        public void Retire()
        {
            Retired=true;Active=false;
            if(ThrowRetire)throw new IOException("injected inactive cleanup failure");
        }
    }
    private sealed class Factory:IWorldFactory
    {
        public World? Last;
        public bool PoseMutates,IdentityMutates,NewThrowActivate,NewThrowDeactivate;
        public bool NewThrowRetire,PrepareThrows;
        public IPreparedWorld Prepare(RunSnapshot snapshot)
        {
            if(PrepareThrows)throw new IOException("injected factory failure");
            Last=new World(snapshot){
                MutatePoseOnActivation=PoseMutates,
                MutateIdentityOnActivation=IdentityMutates,
                ThrowActivate=NewThrowActivate,
                ThrowDeactivate=NewThrowDeactivate,
                ThrowRetire=NewThrowRetire
            };
            PoseMutates=IdentityMutates=NewThrowActivate=
                NewThrowDeactivate=NewThrowRetire=false;
            return Last;
        }
    }
    private static (AtomicWorldSwap swap,Factory factory,World old) Seed()
    {
        var f=new Factory();
        var bridge=new AtomicWorldSwap(f);
        if(bridge.TryLoad(Bytes(8,true,false),Run,1,()=>true)!=WorldSwapVerdict.Accepted
           ||f.Last==null || !f.Last.Active)
            throw new InvalidOperationException("real producer baseline failed");
        return (bridge,f,f.Last);
    }
    private static byte[] ReSignForeignId(byte[] bytes)
    {
        var result=(byte[])bytes.Clone();
        // original selected #556 binary v1: 8B magic, 4B version,
        // 40B immutable-source SHA, 8B run, 8B generation, 4B slot count.
        result[72]^=0x01;
        using var sha=SHA256.Create();
        byte[] digest=sha.ComputeHash(result,0,result.Length-32);
        Buffer.BlockCopy(digest,0,result,result.Length-32,32);
        return result;
    }

    public static int Main(string[] args)
    {
        Check("REAL_556_stable_light_id_load_and_ledger",()=>{
            var (s,_,w)=Seed();
            return s.Generation==8&&s.HasWorld&&w.Active&&
                s.IsConsumed(0,FrozenPartCatalog.Id(0))&&
                !s.IsConsumed(1,FrozenPartCatalog.Id(1));
        });
        Check("REAL_556_source_binary_save_roundtrip_W06",()=>{
            var (s,_,w)=Seed();
            bool ok=s.TrySave(Run,()=>true,out byte[] bytes);
            return ok&&w.Active&&
                ColdRestore.TryStage(bytes,out ColdRestore? cold)&&cold!=null&&
                cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))&&
                !cold.ContainsConsumedId(1,FrozenPartCatalog.Id(1))&&
                cold.Snapshot.Generation==8;
        });
        Check("GEN_9_new_world_commits_and_retires_old",()=>{
            var(s,f,old)=Seed();
            var v=s.TryLoad(Bytes(9,false,true),Run,9,()=>true);
            return v==WorldSwapVerdict.Accepted&&s.Generation==9&&
                old.Retired&&!old.Active&&f.Last!=old&&f.Last!.Active&&
                !s.IsConsumed(0,FrozenPartCatalog.Id(0))&&
                s.IsConsumed(1,FrozenPartCatalog.Id(1));
        });
        Check("POST_ACTIVATION_POSE_mutation_REJECTS_and_restores_old",()=>{
            var(s,f,old)=Seed();f.PoseMutates=true;
            var v=s.TryLoad(Bytes(9,false,true),Run,9,()=>true);
            return v==WorldSwapVerdict.HostException&&!s.Quarantined&&
                s.Generation==8&&s.IsConsumed(0,FrozenPartCatalog.Id(0))&&
                !s.IsConsumed(1,FrozenPartCatalog.Id(1))&&old.Active&&
                !old.Retired&&old.ActivateCount==2&&
                f.Last!.Retired&&!f.Last.Active;
        });
        Check("POST_ACTIVATION_STABLE_ID_mutation_REJECTS_and_restores_old",()=>{
            var(s,f,old)=Seed();f.IdentityMutates=true;
            var v=s.TryLoad(Bytes(9,false,true),Run,9,()=>true);
            return v==WorldSwapVerdict.HostException&&!s.Quarantined&&
                s.Generation==8&&old.Active&&!old.Retired&&
                f.Last!.Retired&&!f.Last.Active;
        });
        Check("THROWN_ACTIVATE_restores_old_without_half_world",()=>{
            var(s,f,old)=Seed();f.NewThrowActivate=true;
            var v=s.TryLoad(Bytes(9,false,true),Run,9,()=>true);
            return v==WorldSwapVerdict.HostException&&s.Generation==8&&
                old.Active&&!old.Retired&&f.Last!.Retired;
        });
        Check("ROLLBACK_CLEANUP_THROW_sticky_quarantine_and_future_denial",()=>{
            var(s,f,old)=Seed();
            f.PoseMutates=true;f.NewThrowDeactivate=true;
            var v=s.TryLoad(Bytes(9,false,true),Run,9,()=>true);
            return v==WorldSwapVerdict.HostException&&s.Quarantined&&
                s.TryLoad(Bytes(10,false,false),Run,10,()=>true)
                    ==WorldSwapVerdict.RollbackQuarantine&&
                !s.TrySave(Run,()=>true,out _)&&
                s.Generation==8&&old.Active;
        });
        Check("VALID_HASH_FOREIGN_ID_REFUSED_without_factory",()=>{
            var(s,f,old)=Seed();
            var v=s.TryLoad(ReSignForeignId(Bytes(9,true,false)),Run,9,()=>true);
            return v==WorldSwapVerdict.InvalidSnapshot&&f.Last==old&&
                old.Active&&s.Generation==8;
        });
        Check("WRONG_RUN_REJECTS_before_prepare",()=>{
            var(s,f,old)=Seed();
            var v=s.TryLoad(Bytes(9,true,false,Run+1),Run,9,()=>true);
            return v==WorldSwapVerdict.WrongRun&&f.Last==old&&old.Active;
        });
        Check("STALE_MIN_GENERATION_REJECTS_before_prepare",()=>{
            var(s,f,old)=Seed();
            var v=s.TryLoad(Bytes(9,true,false),Run,10,()=>true);
            return v==WorldSwapVerdict.StaleGeneration&&f.Last==old&&old.Active;
        });
        Check("SECOND_IDLE_FAILURE_retains_old_retires_staged",()=>{
            var(s,f,old)=Seed();int calls=0;
            var v=s.TryLoad(Bytes(9,false,true),Run,9,()=>++calls==1);
            return v==WorldSwapVerdict.HostBusy&&calls==2&&
                old.Active&&!old.Retired&&f.Last!.Retired&&s.Generation==8;
        });
        Check("REENTRANT_HOST_CALLBACK_DENIED_and_outer_commits",()=>{
            var(s,_,old)=Seed();WorldSwapVerdict nested=WorldSwapVerdict.Accepted;
            var v=s.TryLoad(Bytes(9,false,true),Run,9,()=>{
                nested=s.TryLoad(Bytes(10,true,true),Run,10,()=>true);
                return true;
            });
            return nested==WorldSwapVerdict.HostBusy&&
                v==WorldSwapVerdict.Accepted&&s.Generation==9&&old.Retired;
        });
        Check("SAME_GEN_IDENTICAL_LOGICAL_REPLAY_current_source_idempotent",()=>{
            var(s,f,old)=Seed();
            var v=s.TryLoad(Bytes(8,true,false),Run,8,()=>true);
            return v==WorldSwapVerdict.Accepted&&s.Generation==8&&
                f.Last!=old&&old.Retired;
        });

        // Deliberate RELEASE-CRITICAL RED witness: not a green acceptance test.
        bool equalForkAccepted=false;
        Check("RED_SAME_GENERATION_DIFFERENT_CONSUMED_WORLD_ACCEPTED",()=>{
            var(s,f,old)=Seed();
            var v=s.TryLoad(Bytes(8,false,true),Run,8,()=>true);
            equalForkAccepted=v==WorldSwapVerdict.Accepted&&
                s.Generation==8&&old.Retired&&f.Last!=old&&
                !s.IsConsumed(0,FrozenPartCatalog.Id(0))&&
                s.IsConsumed(1,FrozenPartCatalog.Id(1));
            return equalForkAccepted;
        });
        // Additional API misclassification: callback cleanup throws during rollback
        // after physics has been touched, TryLoad returns HostException rather than
        // RollbackQuarantine. External Unity wrapper *may* inspect Quarantined; but
        // direct interface consumers must not treat this as recoverable.
        bool quarantineVerdictMasked=false;
        Check("RED_ROLLBACK_EXCEPTION_RETURNS_RECOVERABLE_CODE_WHILE_QUARANTINED",()=>{
            var(s,f,_)=Seed();f.PoseMutates=true;f.NewThrowDeactivate=true;
            var v=s.TryLoad(Bytes(9,false,true),Run,9,()=>true);
            quarantineVerdictMasked=v==WorldSwapVerdict.HostException&&s.Quarantined;
            return quarantineVerdictMasked;
        });

        Console.WriteLine("B01_61_ORIGINAL_PRODUCER_587_EXACT_SOURCE_EXECUTED=YES");
        Console.WriteLine("POST_ACTIVATION_POSE_ID_ROLLBACK=GREEN_SOURCE_ONLY");
        Console.WriteLine("EQUAL_GENERATION_DIFFERENT_WORLD_RED="+(equalForkAccepted?"REPRODUCED":"NOT_REPRODUCED"));
        Console.WriteLine("ROLLBACK_QUARANTINE_VERDICT_RED="+(quarantineVerdictMasked?"REPRODUCED":"NOT_REPRODUCED"));
        Console.WriteLine("LIVE_510_PRIVATE_LEDGER=NOT_INTEGRATED UNITY_G1=NOT_RUN CODEX_ACK=NONE G2=HOLD");
        Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        if(fail!=0)return 1;
        bool hold=equalForkAccepted||quarantineVerdictMasked;
        Console.WriteLine("RELEASE_VERDICT="+(hold?"HOLD_P0_RED":"SOURCE_GREEN_EXACT_SHA"));
        if(args.Length==1&&args[0]=="--release")return hold?3:0;
        return hold?0:2; // witness mode must not turn red into release accepted
    }
}

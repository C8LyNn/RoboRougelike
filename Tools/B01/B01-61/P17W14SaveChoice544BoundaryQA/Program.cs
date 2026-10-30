// B01-61 P17 W14: independently execute REAL original #544 Save/Load codec, REAL original
// frozen #510 AtomicDockLedger and unchanged original W06. No copies of the ledger/codec.
// QA-positive means the producer safety RED is reproduced, NOT that Save/Load is approved.
using System;
using System.IO;
using B01.SecureV2SaveContract001;
using B01.SecureReattachV2;

internal static class Program
{
    private static int assertions;
    private static int tests;
    private static readonly Guid Run=Guid.Parse("4a3df6d1-0b57-4e92-a8d9-1c1c6bd2a888");
    private static void Check(bool condition,string reason)
    {
        assertions++;
        if(!condition)throw new Exception("QA_FAIL "+reason);
    }
    private static void Test(string label,Action fn)
    {
        fn();tests++;Console.WriteLine("QA_PASS "+label);
    }
    private static void Reject(Action fn,string label)
    {
        bool denied=false;
        try { fn(); }
        catch(InvalidDataException) { denied=true; }
        catch(InvalidOperationException) { denied=true; }
        Check(denied,label);
    }
    private static TrustedManifest Authority()=>TrustedManifest.SelectedFixture();
    private static PartRecord Part(TrustedPart p,bool detached,int index)
    {
        V3 pos=detached?new V3(p.RootX+p.Mount.X+1,index+2,0):
            new V3(p.RootX+p.Mount.X,p.Mount.Y,p.Mount.Z);
        return new PartRecord(p.Id,p.ModuleGuid,detached,detached,!detached,pos,Q4.Identity,
            detached?new V3(1,0,0):V3.Zero,V3.Zero,p.Mount);
    }
    private static RunSnapshot Original(TrustedManifest m,bool a,bool b,long generation=11)
        =>new RunSnapshot(TrustedManifest.SelectedSourceSha,Run,generation,false,
            new[]{Part(m.Parts[0],a,0),Part(m.Parts[1],b,1)});
    private static byte[] RealBytes(TrustedManifest m,bool a,bool b)
        =>StableIdSnapshotCodec.Encode(Original(m,a,b),m);
    private static PreparedRestore Restore(TrustedManifest m,bool a,bool b)
        =>StableIdSnapshotCodec.Prepare(RealBytes(m,a,b),m,Run,11,false);
    private static void ReferenceControl()
    {
        TrustedManifest m=Authority();
        for(int a=0;a<2;a++)for(int b=0;b<2;b++)
        {
            byte[] bytes=RealBytes(m,a==1,b==1);
            Check(bytes.Length==StableIdSnapshotCodec.TotalBytes,"original binary length");
            var snap=StableIdSnapshotCodec.Decode(bytes,m,Run,11);
            Check(snap.Parts[0].Detached==(a==1)&&snap.Parts[1].Detached==(b==1),
                "real original codec roundtrip");
            PreparedRestore p=StableIdSnapshotCodec.Prepare(bytes,m,Run,11,false);
            Check(p.StagedLedger(0).Contains(m.Parts[0].Id)==(a==1),
                "original W06+R2 Light staged ledger");
            Check(p.StagedLedger(1).Contains(m.Parts[1].Id)==(b==1),
                "original W06+R2 Heavy staged ledger");
        }
    }
    private static void InputClonePositiveControl()
    {
        TrustedManifest m=Authority();
        var input=new[]{Part(m.Parts[0],true,0),Part(m.Parts[1],false,1)};
        var s=new RunSnapshot(TrustedManifest.SelectedSourceSha,Run,11,false,input);
        input[0]=input[1];
        Check(s.Parts[0].Id==m.Parts[0].Id,
            "producer input array is cloned at constructor (good, but getter later leaks)");
    }
    private static void PostPublishSnapshotMutation()
    {
        TrustedManifest m=Authority();
        PreparedRestore published=Restore(m,true,false);
        long stable=m.Parts[0].Id;
        Check(published.Snapshot.Parts[0].Id==stable,"valid published source snapshot");
        Check(published.StagedLedger(0).Contains(stable),"real W06 original consumed before attack");
        // REAL production RunSnapshot property exposes mutable array; publication boundary fails.
        PartRecord malicious=published.Snapshot.Parts[1];
        published.Snapshot.Parts[0]=malicious; // no reflection, no copied code
        Check(published.Snapshot.Parts[0].Id==m.Parts[1].Id,
            "SOURCE_RED: post-validate published snapshot slot can mutate to other rig");
        Check(published.StagedLedger(0).Contains(stable),
            "real ledger still consumed ORIGINAL Light id while published snapshot now says Heavy");
        Reject(()=>StableIdSnapshotCodec.Validate(published.Snapshot,m,Run,11),
            "mutated published snapshot now fails its ORIGINAL validator");
    }
    private static void PostPublishW06OriginalLedgerAccess()
    {
        TrustedManifest m=Authority();
        PreparedRestore published=Restore(m,true,false);
        long id=m.Parts[0].Id;
        AtomicDockLedger exposed=published.StagedLedger(0);
        Check(exposed.Contains(id),"precondition original consumed id");
        int unauthorizedPhysicalCall=0;
        Check(exposed.TryReattach(id,()=>unauthorizedPhysicalCall++,()=>unauthorizedPhysicalCall--),
            "SOURCE_RED: caller invokes real #510 original R2 TryReattach on publicly handed-out staged ledger");
        Check(unauthorizedPhysicalCall==1 && !exposed.Contains(id) && exposed.Count==0,
            "SOURCE_RED: actual original consumed ID rearmed without authorized host/publish transaction");
        Check(published.Snapshot.Parts[0].Detached && published.Snapshot.Parts[0].Consumed,
            "SOURCE_RED: immutable PartRecord claims DETACHED+CONSUMED after externally mutating ledger");
    }
    private static void PostPublishClearForNewRun()
    {
        TrustedManifest m=Authority();
        var published=Restore(m,true,true);
        var exposed=published.StagedLedger(1);
        long id=m.Parts[1].Id;
        Check(exposed.Contains(id),"Heavy original consumed before caller");
        exposed.ClearForNewRun(); // REAL original AtomicDockLedger API; no transaction
        Check(!exposed.Contains(id) && exposed.Count==0,
            "SOURCE_RED: caller can clear published staged original W06 Heavy ledger without owner");
        Check(published.Snapshot.Parts[1].Detached && published.Snapshot.Parts[1].Consumed,
            "published consumed Heavy state now diverges");
    }
    private static void PublicManifestMutation()
    {
        TrustedManifest m=Authority();
        var valid=RealBytes(m,true,false);
        var prepared=StableIdSnapshotCodec.Prepare(valid,m,Run,11,false);
        Check(prepared.StagedLedger(0).Contains(m.Parts[0].Id),"prepared before manifest mutation");
        m.Parts[0]=m.Parts[1];
        Check(m.Parts[0].Id==m.Parts[1].Id,
            "SOURCE_RED: TrustedManifest readonly Parts is a writable array reference");
        Reject(()=>StableIdSnapshotCodec.Decode(valid,m,Run,11),
            "previously accepted binary now fails after caller mutates trusted manifest");
        Check(prepared.StagedLedger(0).Count==1,
            "prepared old W06 consumed ID unaffected by mutation of authority registry");
    }
    private static void NoLiveMutationOnBadFile()
    {
        TrustedManifest m=Authority();
        var prior=Restore(m,true,false);
        long id=m.Parts[0].Id;
        byte[] bytes=RealBytes(m,true,false);
        bytes[bytes.Length-5]^=1;
        int physical=0;
        Reject(()=>StableIdSnapshotCodec.Prepare(bytes,m,Run,11,false),
            "original codec rejects corrupt SHA256 before any new ledger publication");
        Reject(()=>StableIdSnapshotCodec.Prepare(RealBytes(m,true,false),m,Run,11,true),
            "original Prepare busy transaction refused");
        Check(prior.StagedLedger(0).Contains(id) && physical==0,
            "malformed/busy source plan does not mutate old independent ledger/host");
    }
    private static void PreventOtherCandidate()
    {
        TrustedManifest m=Authority();
        var bytes=RealBytes(m,true,false);
        Reject(()=>StableIdSnapshotCodec.Decode(bytes,m,Guid.NewGuid(),11),"other run refused");
        Reject(()=>StableIdSnapshotCodec.Decode(bytes,m,Run,12),"old generation refused");
    }
    private static void FrozenOriginalW06ReattachControl()
    {
        var m=Authority();
        var stage=Restore(m,true,false);
        var ledger=stage.StagedLedger(0);
        int physical=14;
        Check(!ledger.TryReattach(m.Parts[0].Id,()=>{physical=99;throw new Exception("fault");},
            ()=>physical=14),"real frozen R2 failed commit must rollback");
        Check(physical==14 && ledger.Contains(m.Parts[0].Id),
            "true original W06 ID reconsumed after rollback");
    }
    public static int Main(string[] args)
    {
        try {
            Test("all four real original #544 save+W06/R2 roundtrips",ReferenceControl);
            Test("input clone positive control",InputClonePositiveControl);
            Test("ORIGINAL #544 post-publish Parts mutation SECURITY RED",PostPublishSnapshotMutation);
            Test("ORIGINAL #544 publicly exposed mutable R2 ledger SECURITY RED",PostPublishW06OriginalLedgerAccess);
            Test("ORIGINAL #544 exposed ledger ClearForNewRun SECURITY RED",PostPublishClearForNewRun);
            Test("ORIGINAL #544 trusted manifest post-validation mutation SECURITY RED",PublicManifestMutation);
            Test("original malformed/busy defensive rejection leaves old ledger intact",NoLiveMutationOnBadFile);
            Test("original run and monotonic generation protections",PreventOtherCandidate);
            Test("actual original W06/R2 callback rollback compensation",FrozenOriginalW06ReattachControl);
            Console.WriteLine("B0161_W14_REAL_544_PRODUCER_SECURITY_RED_WITNESS_PASS tests="+tests+
                " assertions="+assertions+" original_544_sha=e77529262b8f03c6e9f0e25b642038c351872044"+
                " frozen_510_sha=d94400d95d13db7f27e1533df05c9292d5380431"+
                " ORIGINAL_W06=YES ORIGINAL_R2=YES UNITY_NATIVE=NOT_RUN LIVE_SAVELOAD=HOLD");
            if(args.Length>0 && args[0]=="--release") {
                Console.WriteLine("RELEASE_GATE_RED_544=POST_VALIDATION_ARRAY_MUTABILITY_AND_EXPOSED_R2_LEDGER_AND_AUTHORITY_MUTABILITY");
                return 3;
            }
            return 0;
        } catch(Exception e) {
            Console.Error.WriteLine("QA_INFRA_FAILURE "+e);
            return 2;
        }
    }
}

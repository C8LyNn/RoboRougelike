// B01-61 W14 independent, ORIGINAL source C#9 #544 and ORIGINAL #510 AtomicDockLedger/W06.
// A successful QA run MUST reproduce SOURCE RELEASE RED. Native Unity G1 NOT_RUN.
using System;
using System.IO;
using B01.SecureV2SaveContract001;
using B01.SecureReattachV2;
using B01.DebrisRepulse001;

internal static class Candidate544OriginalQA
{
    static readonly Guid Run=Guid.Parse("abcde123-ff12-4789-9a2b-eeee00001234");
    static int assertions, baseline, negatives, securityRed;
    static void Assert(bool yes,string message)
    {
        assertions++;
        if(!yes)throw new Exception("B01_61_QA_INFRA_FAILURE "+message);
    }
    static void Reject(Action test,string message)
    {
        bool rejected=false;
        try{test();}
        catch(InvalidDataException){rejected=true;}
        catch(InvalidOperationException){rejected=true;}
        Assert(rejected,"expected reject: "+message);
        negatives++;
    }
    static PartRecord Part(TrustedManifest m,int index,bool detached)
    {
        var t=m.Parts[index];
        return new PartRecord(t.Id,t.ModuleGuid,detached,detached,!detached,
            detached?new V3(t.RootX+t.Mount.X+.36,1.45,-.3):
                new V3(t.RootX+t.Mount.X,t.Mount.Y,t.Mount.Z),
            Q4.Identity,detached?new V3(.75,.3,-.4):V3.Zero,
            detached?new V3(.05,-.03,0):V3.Zero,t.Mount);
    }
    static RunSnapshot Snap(TrustedManifest m,bool detached0=true,bool detached1=false,long generation=4)
        => new RunSnapshot(TrustedManifest.SelectedSourceSha,Run,generation,false,
            new[]{Part(m,0,detached0),Part(m,1,detached1)});
    static byte[] Wire(TrustedManifest m,bool detached0=true,bool detached1=false,long generation=4)
        => StableIdSnapshotCodec.Encode(Snap(m,detached0,detached1,generation),m);

    static void ActualSourceControls()
    {
        for(int bit=0;bit<4;bit++)
        {
            var authority=TrustedManifest.SelectedFixture();
            bool d0=(bit&1)!=0,d1=(bit&2)!=0;
            var raw=Wire(authority,d0,d1);
            Assert(raw.Length==StableIdSnapshotCodec.TotalBytes,
                "actual original #544 fixed-byte serialization");
            var decoded=StableIdSnapshotCodec.Decode(raw,authority,Run,4);
            var staged=StableIdSnapshotCodec.Prepare(raw,authority,Run,4,false);
            Assert(decoded.Parts.Length==2 && staged.Snapshot.Parts.Length==2,
                "original #544 actual decode/stage");
            for(int slot=0;slot<2;slot++)
            {
                var part=decoded.Parts[slot];
                var originalRig=slot==0?SecureRigCatalog.Light:SecureRigCatalog.Heavy;
                Assert(part.Id==originalRig.PartId && part.ModuleGuid==authority.Parts[slot].ModuleGuid,
                    "frozen original #510 Light/Heavy source ID mapping");
                Assert(part.Detached == ((bit&(1<<slot))!=0) &&
                    staged.StagedLedger(slot).Contains(part.Id)==part.Detached,
                    "original #544 stage rebuilds true original R2/W06 ledger");
            }
            baseline++;
        }
        var auth=TrustedManifest.SelectedFixture();
        var bytes=Wire(auth);
        Reject(()=>StableIdSnapshotCodec.Prepare(bytes,auth,Run,4,true),"busy host rejects before staging");
        Reject(()=>StableIdSnapshotCodec.Decode(bytes,auth,Guid.NewGuid(),4),"wrong run");
        Reject(()=>StableIdSnapshotCodec.Decode(bytes,auth,Run,5),"stale generation");
        var bad=(byte[])bytes.Clone();
        bad[bad.Length/2]^=0x80;
        Reject(()=>StableIdSnapshotCodec.Decode(bad,auth,Run,4),"corrupted actual binary checksum");
        Reject(()=>StableIdSnapshotCodec.Decode(new byte[8],auth,Run,4),"truncated checkpoint");
        var orig=Snap(auth);
        Reject(()=>StableIdSnapshotCodec.Encode(
            new RunSnapshot(orig.SourceSha,orig.RunId,orig.Generation,false,
                new[]{orig.Parts[0],orig.Parts[0]}),auth),"duplicate id");
        Reject(()=>StableIdSnapshotCodec.Encode(
            new RunSnapshot(orig.SourceSha,orig.RunId,orig.Generation,false,
                new[]{orig.Parts[0]}),auth),"missing second part");
        Reject(()=>StableIdSnapshotCodec.Encode(
            new RunSnapshot("baf65f2664e38f3b9c38815c3f07285f8107c1b6",
                Run,4,false,orig.Parts),auth),"alternative #511 SHA");
        Reject(()=>StableIdSnapshotCodec.Encode(
            new RunSnapshot(orig.SourceSha,Run,4,true,orig.Parts),auth),"pending save");
    }
    static void ConfirmPostValidationArrayMutation()
    {
        var authority=TrustedManifest.SelectedFixture();
        var staged=StableIdSnapshotCodec.Prepare(Wire(authority),authority,Run,4,false);
        var id=staged.Snapshot.Parts[0].Id;
        Assert(staged.StagedLedger(0).Contains(id),"before accepted snapshot mutation has consumed ID");
        // This modifies ONLY the actual producer's public Parts ARRAY element;
        // no reflection, no cloned or test-only production code.
        staged.Snapshot.Parts[0]=staged.Snapshot.Parts[1];
        Reject(()=>StableIdSnapshotCodec.Validate(staged.Snapshot,authority,Run,4),
            "REAL RED: published accepted snapshot now invalid");
        Assert(staged.StagedLedger(0).Contains(id),
            "REAL RED: consumed original W06 ID still present but snapshot now foreign/duplicate");
        securityRed++;
        Console.WriteLine("ORIGINAL_544_RED_MUTABLE_ACCEPTED_SNAPSHOT_ARRAY=REPRODUCED");
    }
    static void ConfirmExposedOriginalAtomicLedger()
    {
        var auth=TrustedManifest.SelectedFixture();
        var staged=StableIdSnapshotCodec.Prepare(Wire(auth),auth,Run,4,false);
        long id=staged.Snapshot.Parts[0].Id;
        Assert(staged.Snapshot.Parts[0].Detached && staged.Snapshot.Parts[0].Consumed &&
            staged.StagedLedger(0).Contains(id),"precondition detached consumed true W06");
        // Actual selected original R2 AtomicDockLedger public TryReattach,
        // callable through producer's exposed StagedLedger without guarded PhysX.
        Assert(staged.StagedLedger(0).TryReattach(id,()=>{},()=>{}),
            "REAL RED: caller rearmed original W06 ID without physical dock transaction");
        Assert(staged.Snapshot.Parts[0].Consumed && staged.Snapshot.Parts[0].Detached &&
            !staged.StagedLedger(0).Contains(id),
            "REAL RED: logical published detached+consumed snapshot diverged from original ledger");
        securityRed++;
        Console.WriteLine("ORIGINAL_544_RED_EXPOSED_MUTABLE_ATOMIC_LEDGER=REPRODUCED");
    }
    static void ConfirmReceiverAuthorityArrayMutation()
    {
        var authority=TrustedManifest.SelectedFixture();
        var original=authority.Parts[0];
        Assert(original.Id==SecureRigCatalog.Light.PartId,
            "genuine trusted manifest agrees with immutable selected source catalog");
        // Public array makes 'trusted' authority writable by any consumer holding its reference.
        // This is a trust boundary flaw IF a receiver leaks the TrustedManifest object.
        authority.Parts[0]=new TrustedPart(101,Guid.Parse("00000001-0000-4000-8000-000000000123"),
                                         original.Mount,original.RootX);
        byte[] forged=Wire(authority);
        var staged=StableIdSnapshotCodec.Prepare(forged,authority,Run,4,false);
        Assert(staged.Snapshot.Parts[0].Id==101 && staged.StagedLedger(0).Contains(101) &&
            !staged.StagedLedger(0).Contains(SecureRigCatalog.Light.PartId),
            "REAL RED: mutated authority can stage a foreign ID without immutable catalog check");
        securityRed++;
        Console.WriteLine("ORIGINAL_544_RED_PUBLIC_TRUSTED_MANIFEST_ARRAY=REPRODUCED_IF_REFERENCE_EXPOSED");
    }
    static void ConfirmRealLedgerRollbackStillSafe()
    {
        var authority=TrustedManifest.SelectedFixture();
        var prepared=StableIdSnapshotCodec.Prepare(Wire(authority),authority,Run,4,false);
        var ledger=prepared.StagedLedger(0);
        var id=prepared.Snapshot.Parts[0].Id;
        int rollbacks=0;
        Assert(!ledger.TryReattach(id,
            ()=>throw new InvalidOperationException("simulated physical commit fault"),
            ()=>rollbacks++),"actual original R2 rejected throwing physical commit");
        Assert(rollbacks==1 && ledger.Contains(id) && !ledger.Busy,
            "original W06 consumed ID rolled back after failed transaction");
        baseline++;
    }
    public static int Main()
    {
        try
        {
            ActualSourceControls();
            ConfirmPostValidationArrayMutation();
            ConfirmExposedOriginalAtomicLedger();
            ConfirmReceiverAuthorityArrayMutation();
            ConfirmRealLedgerRollbackStillSafe();
            Assert(baseline==5 && negatives==10 && securityRed==3,
                "all exact producer source test groups completed");
            Console.WriteLine("QA_EXECUTION_PASS asserts="+assertions+
                " valid_roundtrip_combinations=4 expected_rejections="+negatives+
                " true_original_R2_rollback_groups=1 source_security_red="+securityRed);
            Console.WriteLine("CANDIDATE_544_SOURCE_RELEASE_GATE_RED=POST_VALIDATION_MUTATION_AND_EXPOSED_LEDGER");
            Console.WriteLine("FROZEN_510_ORIGINAL_R2_AND_W06_EXECUTED=YES");
            Console.WriteLine("CANDIDATE_549=HISTORICAL_RED_BY_PR550_NOT_TESTED_HERE");
            Console.WriteLine("UNITY_NATIVE=NOT_RUN; SAVELOAD_HOST=NOT_INTEGRATED; G2=HOLD");
            return 0;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine("QA_INFRA_FAILURE "+ex);
            return 1;
        }
    }
}

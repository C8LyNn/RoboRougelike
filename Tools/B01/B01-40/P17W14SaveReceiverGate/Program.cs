// B01-40 independent executable source RED test of VERSIONED #544, not #549.
// Compiles the ORIGINAL #544 codec, frozen #510 AtomicDockLedger and W06 ledger,
// with no rewritten producer logic or Unity shim. RED witness => release HOLD.
using System;
using System.IO;
using B01.SecureV2SaveContract001;

internal static class Original544Boundaries
{
    private static int assertionCount;
    private static void Assert(bool ok, string id)
    {
        if (!ok) throw new InvalidOperationException("QA_UNEXPECTED_GREEN_OR_TEST_FAILURE:"+id);
        assertionCount++;
        Console.WriteLine("PASS "+id);
    }

    private static PartRecord Part(TrustedPart t, bool detached)
    {
        V3 pos = detached ? new V3(t.RootX+t.Mount.X+2,3,1)
                          : new V3(t.RootX+t.Mount.X,t.Mount.Y,t.Mount.Z);
        return new PartRecord(t.Id,t.ModuleGuid,detached,detached,!detached,
                              pos,Q4.Identity,detached?new V3(1,0,0):V3.Zero,
                              V3.Zero,t.Mount);
    }

    public static void Main()
    {
        TrustedManifest authority = TrustedManifest.SelectedFixture();
        Guid runId=Guid.Parse("f6d7128a-460f-4f9c-b1a0-214cb9833c11");
        var original = new RunSnapshot(TrustedManifest.SelectedSourceSha,runId,11,false,
            new [] { Part(authority.Parts[0],true), Part(authority.Parts[1],false) });
        byte[] encoded = StableIdSnapshotCodec.Encode(original,authority);
        Assert(encoded.Length==StableIdSnapshotCodec.TotalBytes,"original-real-code-encode");
        var staged = StableIdSnapshotCodec.Prepare(encoded,authority,runId,11,false);
        long detachedId=authority.Parts[0].Id;
        Assert(staged.Snapshot.Parts[0].Id==detachedId &&
            staged.Snapshot.Parts[0].Detached && staged.Snapshot.Parts[0].Consumed,
            "published-snapshot-was-validated");
        Assert(staged.StagedLedger(0).Contains(detachedId),
            "original-W06-and-R2-ledger-actually-consumed-ID");

        // ORIGINAL codec exposes its post-validated mutable array through Parts.
        var oldRecord = staged.Snapshot.Parts[0];
        staged.Snapshot.Parts[0] = staged.Snapshot.Parts[1];
        Assert(staged.Snapshot.Parts[0].Id==staged.Snapshot.Parts[1].Id,
            "ACTUAL_544_RED_POST_VALIDATION_DUPLICATE_ID");
        Assert(staged.StagedLedger(0).Contains(detachedId) &&
               staged.Snapshot.Parts[0].Id!=detachedId,
            "ACTUAL_544_RED_PUBLISHED_DATA_DIVERGES_FROM_REAL_LEDGER");
        staged.Snapshot.Parts[0]=oldRecord;

        // ORIGINAL PreparedRestore.StagedLedger(index) returns the SAME mutable
        // AtomicDockLedger reference later presented as a validated restore.
        var owned = staged.StagedLedger(0);
        Assert(Object.ReferenceEquals(owned,staged.StagedLedger(0)),
            "actual-internal-ledger-reference-escapes");
        Assert(owned.TryReattach(detachedId,()=>{},()=>{}),
            "ACTUAL_544_RED_OUT_OF_BAND_REARM_BEFORE_PHYSICAL_DOCK");
        Assert(!staged.StagedLedger(0).Contains(detachedId) &&
               staged.Snapshot.Parts[0].Consumed && staged.Snapshot.Parts[0].Detached,
            "ACTUAL_544_RED_CONSUMED_MISMATCH_AFTER_EXTERNAL_REARM");
        Assert(!owned.TryReattach(detachedId,()=>{},()=>{}),
            "out-of-band-rearm-consumed-original-W06-ID");
        // New preparation must be possible, demonstrating witness didn't damage
        // the immutable physical producer source or any actual Unity host.
        var again=StableIdSnapshotCodec.Prepare(encoded,authority,runId,11,false);
        Assert(again.StagedLedger(0).Contains(detachedId),
            "actual-original-source-cold-restore-repeats-in-offstage-ledger");
        Assert(!Object.ReferenceEquals(again.StagedLedger(0),staged.StagedLedger(0)),
            "two-independent-source-staging-ledgers-created");
        Console.WriteLine("SOURCE_QA_TEST_PASS="+assertionCount+
                          " ORIGINAL_544_RELEASE_GATE_RED=MUTABLE_PUBLISHED_ARRAY_AND_ESCAPED_ATOMIC_LEDGER");
        Console.WriteLine("UNITY_NATIVE=NOT_RUN CODEX_ACK=NO SAVE_CONTRACT_RELEASE=HOLD");
    }
}

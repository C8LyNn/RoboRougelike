using System;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;

// Independent EXACT-SHA acceptance checks for separately versioned producer #556.
// This file contains no production implementation, mocks, or Unity substitutes.
internal static class CorrectedGuardTests
{
    private static int passed, failed;
    private static void Check(string name, Func<bool> check)
    {
        try
        {
            if (check()) { passed++; Console.WriteLine("PASS " + name); }
            else { failed++; Console.WriteLine("FAIL " + name); }
        }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + " " + error); }
    }

    private static PersistedPart Part(int slot, bool detached)
    {
        long owner = FrozenPartCatalog.Owner(slot);
        return new PersistedPart(
            FrozenPartCatalog.Id(slot), owner, detached ? 0 : owner,
            FrozenPartCatalog.Asset(slot), FrozenPartCatalog.Socket(slot),
            detached ? PartDockState.Detached : PartDockState.Attached,
            detached, !detached, new Vector3d(slot == 0 ? 1 : 5, 2, 0),
            0, 0, 0, 1,
            detached ? new Vector3d(.1, 0, 0) : new Vector3d(0, 0, 0),
            new Vector3d(0, 0, 0), FrozenPartCatalog.Mass(slot));
    }

    private static byte[] Bytes(long generation, bool a, bool b) =>
        SnapshotCodec.Encode(new RunSnapshot(77, generation,
            new[] { Part(0, a), Part(1, b) }));

    private static ColdSessionHandle Bootstrap()
    {
        var host = new ColdSessionHandle();
        if (host.TrySwap(Bytes(1, true, false), 77, 1, () => true) != RestoreVerdict.Accepted)
            throw new InvalidOperationException("real original source failed bootstrap");
        return host;
    }

    public static int Main()
    {
        Check("GREEN_original_W06_consumed_ID_cold_roundtrip", () =>
        {
            var host = Bootstrap();
            return host.Current != null
                && host.Current.ContainsConsumedId(0, FrozenPartCatalog.Id(0))
                && !host.Current.ContainsConsumedId(1, FrozenPartCatalog.Id(1))
                && host.Current.ConsumedCount(0) == 1
                && host.Current.ConsumedCount(1) == 0;
        });
        Check("GREEN_reentrant_false_idle_denies_nested_and_retains_pointer", () =>
        {
            var host = Bootstrap();
            var old = host.Current;
            RestoreVerdict nested = RestoreVerdict.InvalidSnapshot;
            var outer = host.TrySwap(Bytes(2, false, true), 77, 1, () =>
            {
                nested = host.TrySwap(Bytes(3, false, true), 77, 1, () => true);
                return false;
            });
            return outer == RestoreVerdict.TransactionBusy
                && nested == RestoreVerdict.TransactionBusy
                && ReferenceEquals(old, host.Current)
                && host.Current!.Snapshot.Generation == 1;
        });
        Check("GREEN_reentrant_idle_exception_denies_nested_and_retains_pointer", () =>
        {
            var host = Bootstrap();
            var old = host.Current;
            RestoreVerdict nested = RestoreVerdict.InvalidSnapshot;
            var outer = host.TrySwap(Bytes(2, false, true), 77, 1, () =>
            {
                nested = host.TrySwap(Bytes(3, false, true), 77, 1, () => true);
                throw new InvalidOperationException("idle probe failure");
            });
            return outer == RestoreVerdict.Quarantine
                && nested == RestoreVerdict.TransactionBusy
                && ReferenceEquals(old, host.Current);
        });
        Check("GREEN_reentrant_nested_new_generation_denied_outer_commits_once", () =>
        {
            var host = Bootstrap();
            int callbacks = 0;
            RestoreVerdict nested = RestoreVerdict.InvalidSnapshot;
            var outer = host.TrySwap(Bytes(2, false, true), 77, 1, () =>
            {
                callbacks++;
                nested = host.TrySwap(Bytes(4, false, true), 77, 1, () => true);
                return true;
            });
            return outer == RestoreVerdict.Accepted
                && nested == RestoreVerdict.TransactionBusy
                && callbacks >= 1
                && host.Current!.Snapshot.Generation == 2
                && host.Current.ContainsConsumedId(1, FrozenPartCatalog.Id(1));
        });
        Check("GREEN_second_idle_guard_refusal_is_failure_atomic", () =>
        {
            var host = Bootstrap();
            var old = host.Current;
            int calls = 0;
            var verdict = host.TrySwap(Bytes(2, false, true), 77, 1,
                () => ++calls == 1);
            return verdict == RestoreVerdict.TransactionBusy
                && calls == 2 && ReferenceEquals(old, host.Current);
        });
        Check("GREEN_published_snapshot_array_is_defensive_copy", () =>
        {
            var host = Bootstrap();
            var snap = host.Current!.Snapshot;
            var before = snap.Parts[0];
            snap.Parts[0] = snap.Parts[1]; // mutate ONLY the untrusted return copy
            return ReferenceEquals(before, snap.Parts[0])
                && snap.Parts[0].Id == FrozenPartCatalog.Id(0)
                && host.Current.ContainsConsumedId(0, FrozenPartCatalog.Id(0));
        });
        Check("GREEN_original_W06_mutator_not_publicly_exposed", () =>
        {
            var host = Bootstrap();
            var snap = host.Current!;
            return typeof(ColdRestore).GetMethod("GetLedger") == null
                && typeof(ColdRestore).GetMethod("RearmAfterReattach") == null
                && snap.ContainsConsumedId(0, FrozenPartCatalog.Id(0))
                && snap.ConsumedCount(0) == 1;
        });
        Check("GREEN_invalid_snapshot_cannot_replace_active_cold_session", () =>
        {
            var host = Bootstrap();
            var old = host.Current;
            byte[] bytes = Bytes(2, false, true);
            bytes[16] ^= 0x55;
            return host.TrySwap(bytes, 77, 1, () => true)
                    == RestoreVerdict.InvalidSnapshot
                && ReferenceEquals(old, host.Current);
        });
        Console.WriteLine("CORRECTED_CANDIDATE_SOURCE_QA=GREEN_GUARD_ONLY");
        Console.WriteLine("NATIVE_UNITY=NOT_RUN HOST_ADAPTER=NOT_INTEGRATED G2=HOLD");
        Console.WriteLine("TOTAL " + (passed + failed) + " PASS " + passed + " FAIL " + failed);
        return failed == 0 ? 0 : 1;
    }
}

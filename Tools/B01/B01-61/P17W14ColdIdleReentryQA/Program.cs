using System;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;

internal static class Program
{
    private static int passed, failed;
    private static void Check(string name, Func<bool> proof)
    {
        try
        {
            if (!proof()) { failed++; Console.WriteLine("FAIL " + name); }
            else { passed++; Console.WriteLine("PASS " + name); }
        }
        catch (Exception error)
        {
            failed++;
            Console.WriteLine("FAIL " + name + " " + error);
        }
    }

    private static PersistedPart Part(int slot, bool detached)
    {
        var owner = FrozenPartCatalog.Owner(slot);
        return new PersistedPart(
            FrozenPartCatalog.Id(slot), owner, detached ? 0 : owner,
            FrozenPartCatalog.Asset(slot), FrozenPartCatalog.Socket(slot),
            detached ? PartDockState.Detached : PartDockState.Attached,
            detached, !detached, new Vector3d(slot == 0 ? 1 : 5, 2, 0),
            0, 0, 0, 1,
            detached ? new Vector3d(.1, 0, 0) : new Vector3d(0, 0, 0),
            new Vector3d(0, 0, 0), FrozenPartCatalog.Mass(slot));
    }

    private static byte[] Bytes(long generation, bool lightDetached, bool heavyDetached)
    {
        return SnapshotCodec.Encode(new RunSnapshot(77, generation,
            new[] { Part(0, lightDetached), Part(1, heavyDetached) }));
    }

    private static bool Bootstrap(ColdSessionHandle host)
    {
        return host.TrySwap(Bytes(1, true, false), 77, 1, () => true)
            == RestoreVerdict.Accepted
            && host.Current != null
            && host.Current.Snapshot.Generation == 1
            && host.Current.GetLedger(0).Contains(FrozenPartCatalog.Id(0));
    }

    public static int Main()
    {
        // Tests compile and invoke original W13 source and unchanged original W06.
        Check("BASELINE_original_W06_ledger_and_save_restore", () =>
        {
            var host = new ColdSessionHandle();
            return Bootstrap(host);
        });
        Check("BASELINE_nonreentrant_busy_denial_is_atomic", () =>
        {
            var host = new ColdSessionHandle();
            if (!Bootstrap(host)) return false;
            var previous = host.Current;
            var result = host.TrySwap(Bytes(2, false, true), 77, 1, () => false);
            return result == RestoreVerdict.TransactionBusy
                && ReferenceEquals(previous, host.Current);
        });
        Check("BASELINE_nonreentrant_idle_exception_is_atomic", () =>
        {
            var host = new ColdSessionHandle();
            if (!Bootstrap(host)) return false;
            var previous = host.Current;
            var result = host.TrySwap(Bytes(2, false, true), 77, 1,
                () => throw new InvalidOperationException("probe failed"));
            return result == RestoreVerdict.Quarantine
                && ReferenceEquals(previous, host.Current);
        });

        // These are RED WITNESSES: PASS means an unsafe original production
        // behavior is observed, NOT that the contract is safe to release.
        Check("RED_busy_outer_denial_still_publishes_reentrant_swap", () =>
        {
            var host = new ColdSessionHandle();
            if (!Bootstrap(host)) return false;
            var previous = host.Current;
            RestoreVerdict nested = RestoreVerdict.InvalidSnapshot;
            var outer = host.TrySwap(Bytes(2, false, true), 77, 1, () =>
            {
                nested = host.TrySwap(Bytes(3, false, true), 77, 1, () => true);
                return false;
            });
            return nested == RestoreVerdict.Accepted
                && outer == RestoreVerdict.TransactionBusy
                && !ReferenceEquals(previous, host.Current)
                && host.Current!.Snapshot.Generation == 3
                && host.Current.GetLedger(1).Contains(FrozenPartCatalog.Id(1));
        });
        Check("RED_quarantine_outer_exception_still_publishes_nested_swap", () =>
        {
            var host = new ColdSessionHandle();
            if (!Bootstrap(host)) return false;
            var previous = host.Current;
            RestoreVerdict nested = RestoreVerdict.InvalidSnapshot;
            var outer = host.TrySwap(Bytes(2, false, true), 77, 1, () =>
            {
                nested = host.TrySwap(Bytes(3, false, true), 77, 1, () => true);
                throw new InvalidOperationException("probe failed after nested swap");
            });
            return nested == RestoreVerdict.Accepted
                && outer == RestoreVerdict.Quarantine
                && !ReferenceEquals(previous, host.Current)
                && host.Current!.Snapshot.Generation == 3;
        });
        Check("RED_stale_outer_denial_still_publishes_nested_generation", () =>
        {
            var host = new ColdSessionHandle();
            if (!Bootstrap(host)) return false;
            var previous = host.Current;
            RestoreVerdict nested = RestoreVerdict.InvalidSnapshot;
            var outer = host.TrySwap(Bytes(2, false, true), 77, 1, () =>
            {
                nested = host.TrySwap(Bytes(4, false, true), 77, 1, () => true);
                return true;
            });
            return nested == RestoreVerdict.Accepted
                && outer == RestoreVerdict.StaleGeneration
                && !ReferenceEquals(previous, host.Current)
                && host.Current!.Snapshot.Generation == 4;
        });
        Console.WriteLine("SOURCE_PRODUCTION_RELEASE_GATE=RED_IDLE_CALLBACK_REENTRANCY");
        Console.WriteLine("NATIVE_UNITY=NOT_RUN SAVEGAME_HOST=NOT_INTEGRATED CODEX_ACK=NONE");
        Console.WriteLine("TOTAL " + (passed + failed) + " PASS " + passed + " FAIL " + failed);
        return failed == 0 ? 0 : 1;
    }
}

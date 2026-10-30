using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;

internal static class Program
{
    private const long Run = 77;
    private static int passed, failed;

    private static PersistedPart Part(int slot, bool detached)
    {
        long owner = FrozenPartCatalog.Owner(slot);
        return new PersistedPart(
            FrozenPartCatalog.Id(slot), owner, detached ? 0 : owner,
            FrozenPartCatalog.Asset(slot), FrozenPartCatalog.Socket(slot),
            detached ? PartDockState.Detached : PartDockState.Attached,
            detached, !detached,
            new Vector3d(slot == 0 ? 1 : 5, 2, 0),
            0, 0, 0, 1,
            detached ? new Vector3d(.1, 0, 0) : new Vector3d(0, 0, 0),
            new Vector3d(0, 0, 0), FrozenPartCatalog.Mass(slot));
    }

    private static byte[] Bytes(long generation, bool light, bool heavy)
        => SnapshotCodec.Encode(new RunSnapshot(Run, generation,
               new[] { Part(0, light), Part(1, heavy) }));

    private static void Check(string name, Func<bool> assertion)
    {
        try
        {
            if (assertion()) { passed++; Console.WriteLine("PASS " + name); }
            else { failed++; Console.WriteLine("FAIL " + name); }
        }
        catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + " " + e); }
    }

    private static string Child(params string[] args)
    {
        using var p = new Process();
        p.StartInfo = new ProcessStartInfo("dotnet") {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        p.StartInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
        foreach (string arg in args) p.StartInfo.ArgumentList.Add(arg);
        if (!p.Start()) throw new InvalidOperationException("child did not start");
        string output = p.StandardOutput.ReadToEnd();
        string error = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0)
            throw new InvalidOperationException("child exit " + p.ExitCode + ": " + error);
        return output.Trim();
    }

    private static bool Has(string stdout, string token)
        => stdout.Contains(token, StringComparison.Ordinal);

    private static int Worker(string[] args)
    {
        if (args[0] == "write" && args.Length == 5)
        {
            long generation = long.Parse(args[2]);
            bool light = args[3] == "1", heavy = args[4] == "1";
            File.WriteAllBytes(args[1], Bytes(generation, light, heavy));
            Console.WriteLine("WROTE=" + generation);
            return 0;
        }
        if (args[0] == "load" && args.Length == 4)
        {
            var host = new ColdSessionHandle(); // REAL new process / empty host
            var verdict = host.TrySwap(File.ReadAllBytes(args[1]),
                long.Parse(args[2]), long.Parse(args[3]), () => true);
            var plan = host.Current;
            Console.WriteLine("VERDICT=" + verdict
                + " RUN=" + (plan?.Snapshot.RunId.ToString() ?? "NONE")
                + " GEN=" + (plan?.Snapshot.Generation.ToString() ?? "NONE")
                + " ID0=" + (plan?.ContainsConsumedId(0, FrozenPartCatalog.Id(0)).ToString() ?? "NONE")
                + " ID1=" + (plan?.ContainsConsumedId(1, FrozenPartCatalog.Id(1)).ToString() ?? "NONE")
                + " COUNT0=" + (plan?.ConsumedCount(0).ToString() ?? "NONE")
                + " COUNT1=" + (plan?.ConsumedCount(1).ToString() ?? "NONE"));
            return 0;
        }
        Console.Error.WriteLine("unknown worker command");
        return 2;
    }

    private static byte[] ReSign(byte[] input)
    {
        byte[] result = (byte[])input.Clone();
        int end = result.Length - 32;
        using var sha = SHA256.Create();
        byte[] digest = sha.ComputeHash(result, 0, end);
        Buffer.BlockCopy(digest, 0, result, end, 32);
        return result;
    }

    private static int Tests()
    {
        string root = Path.Combine(Path.GetTempPath(), "b01-61-w15-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string latest = Path.Combine(root, "latest.v1.save");
        string older = Path.Combine(root, "older.v1.save");
        string fork = Path.Combine(root, "fork.v1.save");
        string damaged = Path.Combine(root, "damaged.v1.save");
        string forged = Path.Combine(root, "forged.v1.save");
        try
        {
            Check("REAL_PROCESS_A_writes_valid_generation_9", () =>
                Has(Child("write", latest, "9", "1", "0"), "WROTE=9")
                && File.Exists(latest));
            Check("REAL_PROCESS_A_writes_valid_older_generation_8", () =>
                Has(Child("write", older, "8", "0", "1"), "WROTE=8")
                && File.Exists(older));
            Check("REAL_PROCESS_B_cold_load_restores_9_and_original_W06_ID", () =>
            {
                string s = Child("load", latest, "77", "1");
                return Has(s, "VERDICT=Accepted") && Has(s, "GEN=9")
                    && Has(s, "ID0=True") && Has(s, "ID1=False")
                    && Has(s, "COUNT0=1") && Has(s, "COUNT1=0");
            });
            Check("REAL_PROCESS_C_stale_8_REPLAY_ACCEPTED_without_external_watermark", () =>
            {
                // SOURCE LIMITATION WITNESS: PASS means bridge MUST provide durable anchor.
                string s = Child("load", older, "77", "1");
                return Has(s, "VERDICT=Accepted") && Has(s, "GEN=8")
                    && Has(s, "ID0=False") && Has(s, "ID1=True");
            });
            Check("REAL_PROCESS_D_stale_8_REJECTED_with_host_supplied_min_9", () =>
            {
                string s = Child("load", older, "77", "9");
                return Has(s, "VERDICT=StaleGeneration") && Has(s, "GEN=NONE")
                    && Has(s, "COUNT0=NONE");
            });
            Check("ORIGINAL_same_host_generation_9_then_8_is_failure_atomic", () =>
            {
                var host = new ColdSessionHandle();
                var accepted = host.TrySwap(File.ReadAllBytes(latest), 77, 1, () => true);
                var previous = host.Current;
                var denied = host.TrySwap(File.ReadAllBytes(older), 77, 1, () => true);
                return accepted == RestoreVerdict.Accepted
                    && denied == RestoreVerdict.StaleGeneration
                    && ReferenceEquals(previous, host.Current)
                    && host.Current!.Snapshot.Generation == 9;
            });
            Check("REAL_PROCESS_E_wrong_run_rejected_and_no_cold_IDs_published", () =>
            {
                string s = Child("load", latest, "88", "1");
                return Has(s, "VERDICT=WrongRun") && Has(s, "GEN=NONE")
                    && Has(s, "ID0=NONE");
            });
            Check("REAL_PROCESS_F_torn_snapshot_rejected_without_publishing", () =>
            {
                byte[] b = File.ReadAllBytes(latest);
                File.WriteAllBytes(damaged, b[..^5]);
                string s = Child("load", damaged, "77", "1");
                return Has(s, "VERDICT=InvalidSnapshot") && Has(s, "GEN=NONE");
            });
            Check("REAL_PROCESS_G_resigned_foreign_ID_rejected_without_publishing", () =>
            {
                byte[] b = File.ReadAllBytes(latest);
                b[72] ^= 1; // first stable part ID (8 magic+4 schema+40 SHA+8 run+8 gen+4 count)
                File.WriteAllBytes(forged, ReSign(b)); // corruption digest alone is not authentication
                string s = Child("load", forged, "77", "1");
                return Has(s, "VERDICT=InvalidSnapshot") && Has(s, "GEN=NONE");
            });
            Check("REAL_PROCESS_H_same_generation_fork_requires_host_transaction_policy", () =>
            {
                Child("write", fork, "9", "0", "1");
                string s = Child("load", fork, "77", "9");
                return Has(s, "VERDICT=Accepted") && Has(s, "GEN=9")
                    && Has(s, "ID0=False") && Has(s, "ID1=True")
                    && !Convert.ToHexString(File.ReadAllBytes(latest)).Equals(
                        Convert.ToHexString(File.ReadAllBytes(fork)), StringComparison.Ordinal);
            });
            Console.WriteLine("CROSS_PROCESS_REPLAY_WITHOUT_DURABLE_WATERMARK=REPRODUCED");
            Console.WriteLine("SAME_GENERATION_FORK_WITHOUT_HOST_COMMIT_ID=REPRODUCED");
            Console.WriteLine("CONTRACT_556_SOURCE_ONLY=TRUE LIVE_HOST_ADAPTER=NOT_TESTED");
            Console.WriteLine("NATIVE_UNITY=NOT_RUN SAVELOAD_ACCEPTED=HOLD G2=HOLD");
            Console.WriteLine("TOTAL " + (passed + failed) + " PASS " + passed + " FAIL " + failed);
            return failed == 0 ? 0 : 1;
        }
        finally { Directory.Delete(root, true); }
    }

    public static int Main(string[] args)
        => args.Length == 0 ? Tests() : Worker(args);
}

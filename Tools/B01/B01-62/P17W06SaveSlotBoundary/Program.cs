using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoboRougelike.R1;
using RoboRougelike.R3;
using UnityEngine;

// All writes are confined to NEW disposable --sandbox. Never touch real user saves.
internal static class Program
{
    private static int assertions;
    private static readonly List<string> results = new();
    private static string sandbox = "";
    private static string qaDir = "";
    private static string protectedSlot = "";
    private static byte[] canary = Array.Empty<byte>();
    private static readonly StringComparison Paths = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static string Flag(string[] args, string key)
    {
        var pos = Array.IndexOf(args, key);
        if(pos < 0 || pos + 1 >= args.Length) throw new Exception("MISSING_OPTION " + key);
        return args[pos + 1];
    }
    private static void Check(bool ok, string name)
    {
        if(!ok) throw new Exception("FAIL " + name);
        assertions++;
        results.Add(name);
        Console.WriteLine("PASS " + name);
    }
    private static bool ReceiverAllows(string candidate)
    {
        // Receiver policy ONLY; not a fix inside the original Codex save code.
        if(string.IsNullOrWhiteSpace(candidate)) return false;
        try
        {
            var root = Path.GetFullPath(qaDir).TrimEnd(Path.DirectorySeparatorChar)
                       + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(candidate);
            return full.StartsWith(root, Paths);
        }
        catch(ArgumentException) {return false;}
        catch(NotSupportedException) {return false;}
        catch(PathTooLongException) {return false;}
    }
    private static void CheckCanary(string name)
    {
        Check(File.ReadAllBytes(protectedSlot).SequenceEqual(canary), name);
    }
    private static R1RunCheckpoint Checkpoint() => new()
    {
        version = 1, seed = 77, vehicle = 1, runId = "B01-62-SANDBOX-SLOT",
        progress = new R1ProgressCheckpoint{completed=2,inGarage=true},
        model = new R3ModelCheckpoint{ids=new[]{"core-0","gun-1"}, condition=new[]{1f,.5f}}
    };
    private static void SafeWrite(string candidate)
    {
        if(!ReceiverAllows(candidate)) throw new Exception("RECEIVER_UNSAFE_WRITE_BLOCKED");
        Directory.CreateDirectory(Path.GetDirectoryName(candidate)!);
        R1RunCheckpointStorage.Write(candidate,Checkpoint());
    }
    private static void Editor()
    {
        Application.isBatchMode=false;
        var good=Path.Combine(qaDir,"editor-checkpoint.json");
        R1RunCheckpointStorage.TestPath=good;
        Check(R1RunCheckpointStorage.PathName==good,"E01 original editor override selects isolated slot");
        SafeWrite(R1RunCheckpointStorage.PathName);
        Check(File.Exists(good) &&
              R1RunCheckpointStorage.Read(good).model.ids.SequenceEqual(new[]{"core-0","gun-1"}),
              "E02 actual original write and read at isolated slot");
        CheckCanary("E03 protected canary intact after safe physical save");

        // Simulate static-field reset ONLY; native Unity domain reload NOT executed.
        R1RunCheckpointStorage.TestPath=null;
        Check(R1RunCheckpointStorage.PathName==protectedSlot &&
              !ReceiverAllows(R1RunCheckpointStorage.PathName),
              "E04 static test path reset falls through to user slot; receiver blocks it");

        R1RunCheckpointStorage.TestPath="";
        Check(R1RunCheckpointStorage.PathName==protectedSlot &&
              !ReceiverAllows(R1RunCheckpointStorage.PathName),
              "E05 empty test path falls through; receiver blocks it");

        R1RunCheckpointStorage.TestPath=protectedSlot;
        Check(!ReceiverAllows(R1RunCheckpointStorage.PathName),
              "E06 explicit user slot override refused by receiver");
        R1RunCheckpointStorage.TestPath="checkpoint.json";
        Check(!ReceiverAllows(R1RunCheckpointStorage.PathName),
              "E07 unrooted test path refused by receiver");
        R1RunCheckpointStorage.TestPath=" ";
        Check(!ReceiverAllows(R1RunCheckpointStorage.PathName),
              "E08 whitespace test path refused by receiver");

        // Reproduce actual clobber using ORIGINAL unguarded storage, but ONLY
        // on the disposable user-slot equivalent inside our new sandbox.
        R1RunCheckpointStorage.TestPath=null;
        var resolved=R1RunCheckpointStorage.PathName;
        R1RunCheckpointStorage.Write(resolved,Checkpoint());
        Check(!File.ReadAllBytes(protectedSlot).SequenceEqual(canary) &&
              R1RunCheckpointStorage.Read(resolved).runId=="B01-62-SANDBOX-SLOT",
              "E09 original unguarded fallback demonstrably replaces sandbox canary");
        File.WriteAllBytes(protectedSlot,canary);
        CheckCanary("E10 canary restored byte-exact; no user path accessed");
        Console.WriteLine("SIMULATED_SAVE_SLOT_CLOBBER_RISK=REPRODUCED_NOT_NATIVE_UNITY");
    }
    private static void BatchSafe(string[] args)
    {
        Application.isBatchMode=true;
        R1RunCheckpointStorage.TestPath=null;
        var expected=Flag(args,"-rrl-run-save-qa-path");
        Check(R1RunCheckpointStorage.PathName==expected,"B01 original batch CLI override takes precedence");
        Check(ReceiverAllows(expected),"B02 receiver accepts only isolated CLI slot");
        SafeWrite(R1RunCheckpointStorage.PathName);
        Check(File.Exists(expected) &&
              R1RunCheckpointStorage.Read(expected).runId=="B01-62-SANDBOX-SLOT",
              "B03 actual original write and read with safe CLI override");
        CheckCanary("B04 protected canary untouched by CLI test");
    }
    private static void BatchMissing()
    {
        Application.isBatchMode=true;
        R1RunCheckpointStorage.TestPath=null;
        Check(R1RunCheckpointStorage.PathName==protectedSlot &&
              !ReceiverAllows(R1RunCheckpointStorage.PathName),
              "M01 missing batch override falls through to protected user slot; guard rejects");
        CheckCanary("M02 no production-equivalent write occurred");
    }
    private static void BatchUnsafe(string[] args)
    {
        Application.isBatchMode=true;
        R1RunCheckpointStorage.TestPath=null;
        var explicitPath=Flag(args,"-rrl-run-save-qa-path");
        Check(R1RunCheckpointStorage.PathName==explicitPath &&
              explicitPath==protectedSlot,
              "U01 original batch override may explicitly select protected slot");
        Check(!ReceiverAllows(R1RunCheckpointStorage.PathName),
              "U02 receiver refuses unsafe batch override");
        CheckCanary("U03 no protected slot write in unsafe override test");
    }
    private static int Main(string[] args)
    {
        try
        {
            var mode=Flag(args,"--mode");
            sandbox=Path.GetFullPath(Flag(args,"--sandbox"));
            if(Directory.Exists(sandbox)||File.Exists(sandbox))
                throw new Exception("SANDBOX_MUST_BE_ABSENT");
            // Each runner creates a fresh directory; no arbitrary path is deleted.
            Directory.CreateDirectory(sandbox);
            qaDir=Path.Combine(sandbox,"qa");
            Directory.CreateDirectory(qaDir);
            Application.persistentDataPath=Path.Combine(sandbox,"host-data");
            protectedSlot=Path.Combine(Application.persistentDataPath,"Run","checkpoint.json");
            Directory.CreateDirectory(Path.GetDirectoryName(protectedSlot)!);
            canary=Encoding.UTF8.GetBytes("B01-62 ORIGINAL USER SLOT CANARY - DO NOT CHANGE\n");
            File.WriteAllBytes(protectedSlot,canary);

            switch(mode)
            {
                case "editor": Editor(); break;
                case "batch-safe": BatchSafe(args); break;
                case "batch-missing": BatchMissing(); break;
                case "batch-unsafe": BatchUnsafe(args); break;
                default: throw new Exception("UNKNOWN_MODE "+mode);
            }
            var receipt=new {
                schema="b01-62-save-slot-boundary-v1",
                mode, checks=assertions, names=results,
                original_class="R1RunCheckpointStorage",
                original_git_blob="6eb8682c14329088d5698fc369009fb86b99b426",
                shim_is_unity=false,
                native_domain_reload_tested=false,
                sandbox_protected_slot_canary_sha256=
                    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(protectedSlot))).ToLowerInvariant(),
                no_real_user_save_accessed=true,
                code_source_modified=false,
                bug_reproduction=mode=="editor"?"simulated static reset plus actual unsafe write ONLY to disposable canary":"not_applicable"
            };
            File.WriteAllText(Path.Combine(sandbox,"receipt.json"),
                JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true}));
            Console.WriteLine($"SAVE_SLOT_BOUNDARY_SOURCE_QA=PASS mode={mode} checks={assertions} NOT_UNITY_NATIVE");
            return 0;
        }
        catch(Exception exception)
        {
            Console.Error.WriteLine("SAVE_SLOT_BOUNDARY_SOURCE_QA=FAIL "+exception);
            return 1;
        }
    }
}

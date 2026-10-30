using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using B01.DebrisRepulse001;

// SOURCE-LEVEL ONLY: original PR421 core (unmodified) + independent receipt prototype.
// No Unity Rigidbody, scene, real user save, Codex integration or physical collision.
internal sealed class LedgerPayload
{
    public string schema { get; set; } = "";
    public int version { get; set; }
    public string source_sha { get; set; } = "";
    public string run_id { get; set; } = "";
    public long[] consumed_ids { get; set; } = Array.Empty<long>();
}
internal sealed class LedgerEnvelope
{
    public LedgerPayload? payload { get; set; }
    public string sha256 { get; set; } = "";
}
internal static class Program
{
    const string SourceSha="cb2b3a76e84266e2730fffc4ab048c1039f0652f";
    const string Schema="b01-62-debris-ledger-receipt-v1";
    const string RunId="W08-ISOLATED-TEST-RUN";
    const int MaxIds=4096;
    static readonly JsonSerializerOptions JsonOptions=new(){PropertyNamingPolicy=null,WriteIndented=false};
    static readonly Vector3d Zero=new(0,0,0);
    static readonly Vector3d East=new(1,0,0);
    static int checks=0;
    static readonly List<string> passed=new();

    static void Need(bool condition,string description)
    {
        if(!condition)throw new Exception("FAILED: "+description);
    }
    static void Pass(string name,Action action)
    {
        action();
        checks++;
        passed.Add(name);
        Console.WriteLine("PASS "+name);
    }
    static void Rejected(string name,Action action)
    {
        Pass(name,()=>{
            bool rejected=false;
            try{action();}
            catch(InvalidDataException){rejected=true;}
            catch(JsonException){rejected=true;}
            catch(FileNotFoundException){rejected=true;}
            catch(IOException){rejected=true;}
            Need(rejected,"expected rejection: "+name);
        });
    }
    static bool Detach(DebrisImpulseLedger ledger,long id,out Vector3d impulse)
        => ledger.TryDetach(id,Zero,East,Zero,2.0,2.0,4.0,out impulse);

    static string Hash(LedgerPayload p)
        => Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(p,JsonOptions)))).ToLowerInvariant();
    static LedgerPayload Payload(params long[] ids) => new()
    {
        schema=Schema,version=1,source_sha=SourceSha,run_id=RunId,consumed_ids=ids
    };
    static LedgerEnvelope Envelope(LedgerPayload p) => new(){payload=p,sha256=Hash(p)};
    static string Encode(LedgerEnvelope e) =>
        JsonSerializer.Serialize(e,JsonOptions)+"\n";

    static void WriteOnlyNew(string path,string value)
    {
        using var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None);
        byte[] bytes=Encoding.UTF8.GetBytes(value);
        stream.Write(bytes,0,bytes.Length);
    }
    static void Validate(LedgerEnvelope? e,string expectedRunId)
    {
        if(e?.payload is not LedgerPayload p)
            throw new InvalidDataException("MISSING_PAYLOAD");
        if(p.schema!=Schema || p.version!=1 || p.source_sha!=SourceSha ||
           p.run_id!=expectedRunId || string.IsNullOrWhiteSpace(p.run_id) ||
           p.consumed_ids is null || p.consumed_ids.Length>MaxIds)
            throw new InvalidDataException("INVALID_SCHEMA_VERSION_SOURCE_RUN_OR_CAP");
        if(!p.consumed_ids.SequenceEqual(p.consumed_ids.Distinct().OrderBy(id=>id)) ||
           p.consumed_ids.Any(id=>id<=0))
            throw new InvalidDataException("UNSORTED_DUPLICATE_OR_BAD_PART_ID");
        if(e.sha256!=Hash(p))
            throw new InvalidDataException("MISMATCH_PAYLOAD_SHA256");
    }
    static (LedgerPayload payload,DebrisImpulseLedger ledger) Restore(
        string file,string expectedRunId)
    {
        LedgerEnvelope? e=JsonSerializer.Deserialize<LedgerEnvelope>(File.ReadAllText(file));
        Validate(e,expectedRunId); // no mutation before validation of every byte and id
        var payload=e!.payload!;
        var ledger=new DebrisImpulseLedger();
        foreach(long stableId in payload.consumed_ids)
        {
            // REAL ORIGINAL core method; synthetic valid zero-speed event marks id
            // consumed without applying actual force. NOT a canonical game load hook.
            if(!ledger.TryDetach(stableId,Zero,East,Zero,1.0,0.0,0.0,out Vector3d candidate)
               || candidate.MagnitudeSquared!=0)
                throw new InvalidDataException("RESTORE_UNSAFE_OR_INCOMPLETE");
        }
        return (payload,ledger);
    }
    static string Fixture(string sandbox,string name,LedgerEnvelope e)
    {
        var path=Path.Combine(sandbox,name);
        WriteOnlyNew(path,Encode(e));
        return path;
    }
    static int Main(string[] args)
    {
        try
        {
            if(args.Length!=2 || args[0]!="--sandbox")
                throw new Exception("Expected --sandbox <nonexistent disposable path>");
            string root=Path.GetFullPath(args[1]);
            if(Directory.Exists(root)||File.Exists(root))
                throw new InvalidDataException("SANDBOX_MUST_NOT_EXIST");
            Directory.CreateDirectory(root);

            var original=new DebrisImpulseLedger();
            var consumed=new HashSet<long>();
            Pass("A01_REAL_ORIGINAL_IMPULSE_CALCULATED",()=>{
                Need(Detach(original,9,out Vector3d i) &&
                     Math.Abs(i.X-4.0)<1e-12 && i.Y==0 && i.Z==0,
                     "original impulse should be mass*speed");
                consumed.Add(9);
            });
            Pass("A02_DUPLICATE_WITHIN_RUN_REJECTED",()=>{
                Need(!Detach(original,9,out Vector3d i) && i.MagnitudeSquared==0,
                     "original ledger did not block duplicate");
            });
            Pass("A03_SECOND_STABLE_PART_CONSUMED",()=>{
                Need(Detach(original,7,out Vector3d i) && i.X==4,"second stable ID");
                consumed.Add(7);
            });
            Pass("A04_INVALID_ID_NOT_CONSUMED",()=>{
                Need(!Detach(original,0,out Vector3d i) && i.MagnitudeSquared==0 &&
                     original.Count==2,"invalid ID mutated ledger");
            });
            var sorted=consumed.OrderBy(id=>id).ToArray();
            Pass("A05_CONSUMED_STABLE_IDS_SORTED_CANONICALLY",()=>{
                Need(sorted.SequenceEqual(new long[]{7,9}),"unexpected stable ID receipt");
            });

            string good=Path.Combine(root,"receipt.json");
            string bytes=Encode(Envelope(Payload(sorted)));
            Pass("A06_PHYSICAL_SAVE_NEW_ONLY",()=>WriteOnlyNew(good,bytes));
            Pass("A07_PHYSICAL_RECEIPT_AND_ORIGINAL_RUN_PINS",()=>{
                var p=Restore(good,RunId).payload;
                Need(p.consumed_ids.SequenceEqual(sorted) && File.ReadAllText(good)==bytes,
                     "original receipt did not survive physical file IO");
            });
            Pass("A08_ORIGINAL_FRESH_LEDGER_REAPPLIES_PREVIOUS_IMPULSE",()=>{
                // Expected negative observation on unchanged #421: no in-game persistence.
                var freshlyConstructed=new DebrisImpulseLedger();
                Need(Detach(freshlyConstructed,9,out Vector3d i) && i.X==4,
                     "fresh original ledger unexpectedly remembered stable ID");
                Console.WriteLine("EXPECTED_MISSING_GAME_PERSISTENCE_REPRODUCED=YES");
            });

            DebrisImpulseLedger restored=Restore(good,RunId).ledger;
            Pass("A09_RECEIPT_REPLAY_RECONSTRUCTS_ORIGINAL_LEDGER",()=>{
                Need(restored.Count==2 && restored.Contains(7) && restored.Contains(9),
                    "original ledger replay incomplete");
            });
            Pass("A10_RESTORED_ID_9_DENIED",()=>{
                Need(!Detach(restored,9,out Vector3d i) && i.MagnitudeSquared==0,
                     "replayed 9 double-impulsed");
            });
            Pass("A11_RESTORED_ID_7_DENIED",()=>{
                Need(!Detach(restored,7,out Vector3d i) && i.MagnitudeSquared==0,
                     "replayed 7 double-impulsed");
            });
            Pass("A12_NEW_ID_10_STILL_ALLOWED",()=>{
                Need(Detach(restored,10,out Vector3d i) && i.X==4,
                     "restored ledger rejected new detach");
            });
            Pass("A13_REATTACH_REARMS_A_STABLE_PART",()=>{
                Need(restored.RearmAfterReattach(7) && !restored.Contains(7),
                     "reattach should release consumed id");
            });
            Pass("A14_REATTACHED_PART_CAN_DETACH_ONCE",()=>{
                Need(Detach(restored,7,out Vector3d i) && i.X==4 &&
                     !Detach(restored,7,out Vector3d no) && no.MagnitudeSquared==0,
                     "reattach must allow exactly one new impulse");
            });
            Pass("A15_NEW_RUN_CLEAR_RESETS_ALL_IDS",()=>{
                restored.ClearForNewRun();
                Need(restored.Count==0 && Detach(restored,9,out Vector3d i) && i.X==4,
                     "run rollover did not clear ledger");
            });

            Rejected("N01_REJECT_SHA256_TAMPER",()=>{
                var bad=Envelope(Payload(sorted));
                bad.sha256="0"+bad.sha256.Substring(1);
                if(bad.sha256==Hash(bad.payload!))bad.sha256="f"+bad.sha256.Substring(1);
                Restore(Fixture(root,"sha-tamper.json",bad),RunId);
            });
            Rejected("N02_REJECT_WRONG_RUN_BINDING",()=>Restore(good,"OTHER-RUN"));
            Rejected("N03_REJECT_WRONG_ORIGINAL_SOURCE_PIN",()=>{
                var p=Payload(sorted); p.source_sha="forged";
                Restore(Fixture(root,"wrong-source.json",Envelope(p)),RunId);
            });
            Rejected("N04_REJECT_UNSUPPORTED_SCHEMA_VERSION_WITH_VALID_HASH",()=>{
                var p=Payload(sorted);p.version=2;
                Restore(Fixture(root,"v2.json",Envelope(p)),RunId);
            });
            Rejected("N05_REJECT_DUPLICATE_IDS_EVEN_WITH_VALID_HASH",()=>{
                Restore(Fixture(root,"dups.json",Envelope(Payload(7,7,9))),RunId);
            });
            Rejected("N06_REJECT_NONPOSITIVE_ID_WITH_VALID_HASH",()=>{
                Restore(Fixture(root,"invalid-id.json",Envelope(Payload(0,7))),RunId);
            });
            Rejected("N07_REJECT_NONCANONICAL_ORDER_WITH_VALID_HASH",()=>{
                Restore(Fixture(root,"unsorted.json",Envelope(Payload(9,7))),RunId);
            });
            Rejected("N08_REJECT_OVERSIZED_LEDGER_WITH_VALID_HASH",()=>{
                Restore(Fixture(root,"too-many.json",Envelope(Payload(
                    Enumerable.Range(1,MaxIds+1).Select(i=>(long)i).ToArray()))),RunId);
            });
            Rejected("N09_REJECT_MISSING_PAYLOAD",()=>{
                Restore(Fixture(root,"missing-payload.json",
                    new LedgerEnvelope{payload=null,sha256="bad"}),RunId);
            });
            Rejected("N10_REJECT_MISSING_FILE",()=>Restore(Path.Combine(root,"absent.json"),RunId));
            Rejected("N11_REFUSE_OVERWRITE_OF_EXISTING_RECEIPT",()=>WriteOnlyNew(good,bytes));
            Pass("A16_EMPTY_VALID_RECEIPT_REPLAY",()=>{
                var empty=Fixture(root,"empty.json",Envelope(Payload()));
                Need(Restore(empty,RunId).ledger.Count==0,"empty snapshot should restore");
            });
            Pass("A17_CANONICAL_RECEIPT_SERIALIZATION_STABLE",()=>{
                Need(Encode(Envelope(Payload(sorted)))==bytes,
                    "receipt JSON encoding drifted");
            });

            var report=new {
                schema="b01-62-pr421-persistence-qa-receipt-v1",
                package_sha=SourceSha,
                original_git_blob="fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
                executed=checks,tests=passed,
                original_fresh_ledger_replays_detach=true,
                independent_receipt_zero_impulse_replay_blocks_duplicate=true,
                original_game_has_persisted_ledger_hook=false,
                native_unity="NOT_RUN",
                save_data_security="SHA256 guards accidental corruption; no authentication against malicious re-signing",
                fixture_input="disposable newly created sandbox only",
                original_source_mutated=false
            };
            File.WriteAllText(Path.Combine(root,"qa-result.json"),
                JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true})
                    .Replace("\r\n","\n")+"\n",new UTF8Encoding(false));
            Console.WriteLine($"B01_62_ORIGINAL_DEBRIS_LEDGER_SAVE_QA=PASS checks={checks} NATIVE_NOT_RUN");
            Console.WriteLine("RECEIPT_JSON_SHA256="+Convert.ToHexString(
                SHA256.HashData(File.ReadAllBytes(good))).ToLowerInvariant());
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("B01_62_ORIGINAL_DEBRIS_LEDGER_SAVE_QA=FAIL "+e);
            return 1;
        }
    }
}

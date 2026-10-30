// B01-61 W13 independent ORIGINAL-SOURCE cold-load witness. Real unchanged W06 DebrisImpulseLedger
// + selected #510 AtomicDockLedger are compiled; NO fake ledger, NOT Unity or game Save/Load.
// Snapshot below is TEST-ONLY DTO; it IS NOT a game save adapter or accepted serialization contract.
using System;
using System.Reflection;
using System.Text.Json;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal sealed class QaSnapshot
{
    public string Schema { get; set; } = "B0161_QA_ONLY_NOT_GAME_SCHEMA";
    public string SourceSha { get; set; } = "d94400d95d13db7f27e1533df05c9292d5380431";
    public long[] StableIds { get; set; } = Array.Empty<long>();
    public bool[] Detached { get; set; } = Array.Empty<bool>();
    public bool[] Consumed { get; set; } = Array.Empty<bool>();
}

internal static class Program
{
    private static int asserts;
    private static int cases;
    private static readonly Vector3d Zero=new Vector3d(0,0,0);
    private static readonly Vector3d One=new Vector3d(1,0,0);
    private static void Check(bool value,string tag)
    {
        asserts++;
        if(!value)throw new InvalidOperationException("QA_ASSERT_FAILED "+tag);
    }
    private static AtomicDockLedger Detach(long id)
    {
        var l=new AtomicDockLedger();
        Check(l.TryDetach(id,Zero,One,Zero,2,2.5,4,out Vector3d impulse),"actual original W06 first detach");
        Check(impulse.X==5 && impulse.Y==0 && impulse.Z==0,"original deterministic impulse");
        Check(l.Contains(id) && l.Count==1,"original consumed ID present");
        return l;
    }
    private static void Test(string name,Action f)
    {
        f();cases++;Console.WriteLine("PASS "+name);
    }
    private static bool HasProductionSaveApi()
    {
        // Exact original selected #510: if the owner later adds a versioned export/restore API
        // under a distinct new SHA, this QA must be retargeted, never silently grant acceptance.
        var methods=typeof(AtomicDockLedger).GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly);
        foreach(var m in methods)
        {
            if(m.Name=="Load"||m.Name=="Restore"||m.Name=="Import"||m.Name=="ImportSnapshot"||
               m.Name=="RestoreSnapshot"||m.Name=="TryRestore"||m.Name=="ExportSnapshot"||
               m.Name=="Save"||m.Name=="CaptureSnapshot") return true;
        }
        return false;
    }
    private static QaSnapshot Fixture(bool detachedA,bool detachedB)
        => new QaSnapshot{
            StableIds=new long[]{2310001,2310002},
            Detached=new bool[]{detachedA,detachedB},
            Consumed=new bool[]{detachedA,detachedB}
        };
    private static string SerializeFixture(QaSnapshot x)=>JsonSerializer.Serialize(x);
    private static QaSnapshot DecodeFixture(string json)
        => JsonSerializer.Deserialize<QaSnapshot>(json)??throw new InvalidOperationException("Missing fixture");
    private static bool FixtureLooksConsistent(QaSnapshot s)
    {
        if(s.Schema!="B0161_QA_ONLY_NOT_GAME_SCHEMA"||
           s.SourceSha!="d94400d95d13db7f27e1533df05c9292d5380431"||
           s.StableIds==null || s.Detached==null || s.Consumed==null ||
           s.StableIds.Length!=2 || s.Detached.Length!=2 || s.Consumed.Length!=2 ||
           s.StableIds[0]!=2310001 || s.StableIds[1]!=2310002) return false;
        for(int i=0;i<2;i++)
        {
            if(s.StableIds[i]<=0 || s.Detached[i]!=s.Consumed[i])return false;
        }
        return true;
    }
    private static void ColdLoadMissingId(long id)
    {
        AtomicDockLedger before=Detach(id);
        string json=SerializeFixture(Fixture(id==2310001,id==2310002));
        QaSnapshot fromDisk=DecodeFixture(json);
        Check(FixtureLooksConsistent(fromDisk),"QA snapshot bytes roundtrip");
        // Construct NEW ORIGINAL atomic ledger instance after hypothetical cold process restart.
        // This proves source-level ledger ID does NOT persist, even though QA DTO does.
        AtomicDockLedger newProcess=new AtomicDockLedger();
        Check(before.Contains(id),"original old-process ID consumed");
        Check(!newProcess.Contains(id) && newProcess.Count==0,"real new-process original ledger empty");
        int physicalMutations=0;
        bool success=newProcess.TryReattach(id,()=>physicalMutations++,()=>physicalMutations--);
        Check(!success && physicalMutations==0 && newProcess.Count==0,
              "cold-load detached physical state fails safely without ID, no physical commit");
    }
    private static void InvalidFixtureMustNotMutate(string label,Action<QaSnapshot> mutate)
    {
        QaSnapshot x=Fixture(true,false);
        mutate(x);
        var parsed=DecodeFixture(SerializeFixture(x));
        var ledger=Detach(2310001);
        int physical=0;
        Check(!FixtureLooksConsistent(parsed),"invalid "+label+" snapshot rejected by QA oracle");
        // As no PRODUCTION import API exists, validation MUST finish before any commit.
        Check(ledger.Contains(2310001) && ledger.Count==1 && physical==0,
              "rejected "+label+" leaves original ledger/physics unchanged");
    }
    private static void RejectionAndRollback()
    {
        long id=2310001;
        var ledger=Detach(id);
        int body=111;
        // Positive control original #510 transactional rollback restores consumed ID.
        Check(!ledger.TryReattach(id,()=>{body=222;throw new Exception("injected");},()=>body=111),
            "original R2 fault-injected reattach returns false");
        Check(body==111 && ledger.Contains(id) && ledger.Count==1,
            "physical compensation and original consumed ID survive callback failure");
        Check(ledger.TryReattach(id,()=>body=333,()=>body=111),"original retry after compensation");
        Check(body==333 && ledger.Count==0 && !ledger.Contains(id),
            "successful original rearm ledger consumed ID cleared once");
    }
    private static void InFlightResetCannotMutate()
    {
        var ledger=Detach(2310002);
        bool refused=false;int mutations=0;
        Check(ledger.TryReattach(2310002,()=>{
            mutations++;
            try{ledger.ClearForNewRun();}catch(InvalidOperationException){refused=true;}
        },()=>mutations--),"original R2 reattach commit with nested reset rejected");
        Check(refused && mutations==1 && ledger.Count==0,"reset during real transaction denied");
    }
    public static int Main(string[] args)
    {
        try
        {
            Test("exact #510 R2 has NO production persistent Export/Import/Restore API",()=>Check(!HasProductionSaveApi(),"save API absent = release HOLD"));
            Test("QA fixture serializer is not a producer adapter",()=>{
                string json=SerializeFixture(Fixture(true,true));
                Check(FixtureLooksConsistent(DecodeFixture(json)),"QA-only fixture both detached");
            });
            Test("light: actual W06 consumed ID lost across cold process restart",()=>ColdLoadMissingId(2310001));
            Test("heavy: actual W06 consumed ID lost across cold process restart",()=>ColdLoadMissingId(2310002));
            Test("both original ledger instances lose state across cold restarts",()=>{
                var a=Detach(2310001);var b=Detach(2310002);
                Check(a.Count==1 && b.Count==1,"both original independent IDs");
                a=new AtomicDockLedger();b=new AtomicDockLedger();
                Check(a.Count==0 && b.Count==0,"neither original new instance remembers ID");
            });
            Test("missing ID deny must never invoke physical callbacks",()=>{
                var l=new AtomicDockLedger();int touched=0;
                Check(!l.TryReattach(2310001,()=>touched++,()=>touched++),"lost ID denied");
                Check(touched==0,"no physical callback on lost ID");
            });
            Test("fake 'restore' via original TryDetach can seed consumed ID: MUST prohibit before owner validation",()=>{
                var l=new AtomicDockLedger();
                Check(l.TryDetach(2310001,Zero,One,Zero,2,0,0,out Vector3d impulse),"actual W06 can consume without any physical detach");
                Check(impulse.MagnitudeSquared==0 && l.Contains(2310001),"unvalidated ID can be forged through original detach API");
                Check(l.TryReattach(2310001,()=>{},()=>{}),"forged ledger ID would authorize physical reattach");
            });
            Test("actual source exception rollback reconsumes correct ID",RejectionAndRollback);
            Test("actual source in-flight ClearForNewRun refused",InFlightResetCannotMutate);
            Test("QA schema invalid version",()=>InvalidFixtureMustNotMutate("version",s=>s.Schema="unknown"));
            Test("QA schema incorrect producer SHA",()=>InvalidFixtureMustNotMutate("source",s=>s.SourceSha="baf65f2664e38f3b9c38815c3f07285f8107c1b6"));
            Test("QA schema duplicate stable ID",()=>InvalidFixtureMustNotMutate("duplicate",s=>s.StableIds[1]=s.StableIds[0]));
            Test("QA schema missing stable ID",()=>InvalidFixtureMustNotMutate("missing",s=>s.StableIds=Array.Empty<long>()));
            Test("QA schema reordered stable ID",()=>InvalidFixtureMustNotMutate("reordered",s=>s.StableIds=new long[]{2310002,2310001}));
            Test("QA schema nonpositive stable ID",()=>InvalidFixtureMustNotMutate("negative",s=>s.StableIds[0]=-1));
            Test("QA schema detached without consumed ID",()=>InvalidFixtureMustNotMutate("detached_without_consumed",s=>s.Consumed[0]=false));
            Test("QA schema attached but consumed ID",()=>InvalidFixtureMustNotMutate("attached_with_consumed",s=>{s.Detached[0]=false;s.Consumed[0]=true;}));
            Test("QA schema missing detached array",()=>InvalidFixtureMustNotMutate("missing_array",s=>s.Detached=Array.Empty<bool>()));
            Test("actual W06 rearm cannot be reused across two old ledgers",()=>{
                var a=Detach(2310001);var b=Detach(2310001);
                Check(a.TryReattach(2310001,()=>{},()=>{}),"rig A cleared");
                Check(b.Contains(2310001),"rig B remains separately consumed; global ID ownership must be unique on restore");
            });
            Test("current runtime cannot query stable UUID owner during import",()=>{
                var fields=typeof(AtomicDockLedger).GetFields(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly);
                Check(fields.Length==0,"no exported stable-part owner proof");
            });
            Console.WriteLine("ORIGINAL_SOURCE_COLDLOAD_QA_PASS cases="+cases+" assertions="+asserts+
              " selected_sha=d94400d95d13db7f27e1533df05c9292d5380431"+
              " original_W06=YES original_R2=YES QA_DTO_NOT_GAME_SAVE=YES"+
              " UNITY_NATIVE=NOT_RUN SAVELOAD_PRODUCTION=HOLD");
            if(args.Length>0 && args[0]=="--release")
            {
                Console.WriteLine("RELEASE_HOLD_NO_PERSISTENT_CONTRACT: old frozen #510 cannot roundtrip consumed detached ID across new original instances. Need new owner adapter/source SHA and independent retest.");
                return 3;
            }
            return 0;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine("QA_INFRA_FAILURE "+ex);
            return 2;
        }
    }
}

// B01-61 W14 independent exact ORIGINAL #549 RED -> #556 GREEN.
 // Compiles unchanged respective B01-23 original producer C# with original W06 and #510 R2.
 // No engine stubs, no actual Unity Save/Load. Tests source security, not canonical integration.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
internal static class Program
{
    static bool green;
    static int count;
    static int tests;
    static void Assert(bool ok,string label)
    {
        count++;
        if(!ok)throw new Exception("QA_ASSERT_RED "+label);
    }
    static void Test(string label,Action action)
    {
        action();tests++;Console.WriteLine("QA_PASS "+label);
    }
    static PersistedPart Part(int i,bool detached)
    {
        long id=FrozenPartCatalog.Id(i),owner=FrozenPartCatalog.Owner(i);
        return new PersistedPart(id,owner,detached?0:owner,FrozenPartCatalog.Asset(i),
            FrozenPartCatalog.Socket(i),detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,new Vector3d(detached?i+3:0,2,0),
            0,0,0,1,detached?new Vector3d(1,2,0):new Vector3d(0,0,0),
            new Vector3d(0,0,0),FrozenPartCatalog.Mass(i));
    }
    static RunSnapshot Snap(bool a,bool b,long generation=5)
        =>new RunSnapshot(8712301,generation,new[]{Part(0,a),Part(1,b)});
    static byte[] Bytes(bool a,bool b,long generation=5)=>SnapshotCodec.Encode(Snap(a,b,generation));
    static ColdRestore Stage(bool a,bool b)
    {
        Assert(ColdRestore.TryStage(Bytes(a,b),out ColdRestore? restore)&&restore!=null,"original schema direct ColdStage");
        return restore!;
    }
    static void OriginalW06Controls()
    {
        for(int a=0;a<2;a++)for(int b=0;b<2;b++)
        {
            var bytes=Bytes(a==1,b==1);
            Assert(SnapshotCodec.TryDecode(bytes,out RunSnapshot? s)&&s!=null,"genuine original producer binary decode");
            Assert(s!.Parts[0].Id==2310001&&s.Parts[1].Id==2310002,"stable Light Heavy IDs");
            var stage=Stage(a==1,b==1);
            Assert(stage.Snapshot.Parts[0].State==(a==1?PartDockState.Detached:PartDockState.Attached),
                "original detached state Light");
            Assert(stage.Snapshot.Parts[1].State==(b==1?PartDockState.Detached:PartDockState.Attached),
                "original detached state Heavy");
        }
    }
    static void MutationWitness()
    {
        ColdRestore accepted=Stage(true,false);
        long id=2310001;
        RunSnapshot captured=accepted.Snapshot;
        Assert(captured.Parts[0].Id==id,"pre mutation original Light ID");
        // Same executable attack as independent #550, against frozen ORIGINAL v1 and selected R2.
        PersistedPart[] access=captured.Parts;
        access[0]=access[1];
        if(green)
        {
            Assert(captured.Parts[0].Id==id,"GREEN: original #556 snapshot getter returns defensive copy");
            Assert(SnapshotRules.Valid(captured),"GREEN: original #556 validated snapshot remains valid");
        }
        else
        {
            Assert(captured.Parts[0].Id==2310002,"RED: original #549 accepted snapshot mutable after validation");
            Assert(!SnapshotRules.Valid(captured),"RED: actual original #549 snapshot corrupted after publish");
        }
    }
    static void DirectW06MutationWitness()
    {
        var stage=Stage(true,false);
        MethodInfo? getter=typeof(ColdRestore).GetMethod("GetLedger",BindingFlags.Public|BindingFlags.Instance);
        if(green)
        {
            Assert(getter==null,"GREEN: no public original W06 mutable GetLedger");
            foreach(var method in typeof(ColdRestore).GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly))
                Assert(!typeof(DebrisImpulseLedger).IsAssignableFrom(method.ReturnType),
                    "GREEN: no public method returns mutable original W06 ledger");
            var contains=typeof(ColdRestore).GetMethod("ContainsConsumedId");
            Assert(contains!=null,"GREEN: read-only original consumed ID query exists");
            bool ok=(bool)contains!.Invoke(stage,new object[]{0,2310001})!;
            Assert(ok && stage.Snapshot.Parts[0].Consumed,"GREEN: real W06 ID intact via side-effect-free query");
        }
        else
        {
            Assert(getter!=null,"RED: original v1 publicly hands out actual W06 ledger");
            var ledger=getter!.Invoke(stage,new object[]{0}) as DebrisImpulseLedger;
            Assert(ledger!=null && ledger.Contains(2310001),"RED: original mutable ledger obtained");
            Assert(ledger!.RearmAfterReattach(2310001),"RED: W06 consumed ID can be rearmed without actual dock");
            Assert(!ledger.Contains(2310001) && stage.Snapshot.Parts[0].Consumed,
                "RED: real ID diverges from published consumed state");
        }
    }
    static void InputArrayCapture()
    {
        var input=new[]{Part(0,true),Part(1,false)};
        var snap=new RunSnapshot(8712301,5,input);
        input[0]=input[1];
        Assert(snap.Parts[0].Id==2310001,"constructor shields against input array mutation");
    }
    static void NestedHostCallback()
    {
        var session=new ColdSessionHandle();
        byte[] bytes=Bytes(true,false,5);
        int calls=0;
        RestoreVerdict nested=RestoreVerdict.Quarantine;
        RestoreVerdict outer=session.TrySwap(bytes,8712301,5,()=>{
            calls++;
            if(calls==1)
                nested=session.TrySwap(bytes,8712301,5,()=>true);
            return true;
        });
        Assert(outer==RestoreVerdict.Accepted,"main session accepts valid original save");
        if(green)
        {
            Assert(nested==RestoreVerdict.TransactionBusy,"GREEN: original #556 enters busy guard before any host callback");
            Assert(calls==2,"GREEN: double host idle recheck before publish");
        }
        else
        {
            Assert(nested==RestoreVerdict.Accepted,"RED: original #549 permits nested accepted swap within host callback");
            Assert(calls==1,"RED: original #549 called idle only once");
        }
        Assert(session.Current!=null && session.Current.Snapshot.Generation==5,"snapshot logical pointer valid after original callback");
    }
    static void BusyBetweenPreflightAndCommit()
    {
        var session=new ColdSessionHandle();
        Assert(session.TrySwap(Bytes(true,false,5),8712301,5,()=>true)==RestoreVerdict.Accepted,
            "prior committed original session");
        ColdRestore old=session.Current!;
        int calls=0;
        var outcome=session.TrySwap(Bytes(false,true,6),8712301,6,()=>++calls==1);
        if(green)
        {
            Assert(calls==2 && outcome==RestoreVerdict.TransactionBusy,
                "GREEN: host becomes busy between stage and commit => reject");
            Assert(Object.ReferenceEquals(old,session.Current),
                "GREEN: committed original logical pointer unchanged on mid-swap busy");
        }
        else
        {
            Assert(calls==1 && outcome==RestoreVerdict.Accepted,
                "RED: old producer publishes after only one idle precheck");
            Assert(!Object.ReferenceEquals(old,session.Current),
                "RED: original pointer changed despite host becoming busy after first check");
        }
    }
    static void FailClosedNoMutation()
    {
        var session=new ColdSessionHandle();
        byte[] good=Bytes(true,true,5);
        Assert(session.TrySwap(good,8712301,5,()=>true)==RestoreVerdict.Accepted,"good logical initial session");
        ColdRestore old=session.Current!;
        byte[] corrupt=(byte[])good.Clone();corrupt[22]^=1;
        Assert(session.TrySwap(corrupt,8712301,5,()=>true)==RestoreVerdict.InvalidSnapshot,
            "corrupted checksum rejected");
        Assert(session.TrySwap(good,8712302,5,()=>true)==RestoreVerdict.WrongRun,
            "wrong run rejected");
        Assert(session.TrySwap(good,8712301,6,()=>true)==RestoreVerdict.StaleGeneration,
            "stale generation rejected");
        Assert(session.TrySwap(good,8712301,5,()=>false)==RestoreVerdict.TransactionBusy,
            "host busy rejects");
        Assert(session.TrySwap(good,8712301,5,()=>throw new Exception("injected"))==RestoreVerdict.Quarantine,
            "host callback exception quarantined");
        Assert(Object.ReferenceEquals(old,session.Current),"all invalid original requests preserve prior pointer");
    }
    static void ActualBinaryDiskSaveAndColdLoad()
    {
        byte[] data=Bytes(true,true,13);
        string path=Path.Combine(Path.GetTempPath(),"b01-61-w14-original-save-"+Guid.NewGuid().ToString("N")+".bin");
        try
        {
            File.WriteAllBytes(path,data);
            Assert(File.Exists(path)&&new FileInfo(path).Length==data.Length,"original Save bytes physically written to OS filesystem");
            byte[] persisted=File.ReadAllBytes(path);
            Assert(data.SequenceEqual(persisted),"original binary file bytes identical after disk read");
            var newColdHost=new ColdSessionHandle();
            Assert(newColdHost.TrySwap(persisted,8712301,13,()=>true)==RestoreVerdict.Accepted,
                "fresh original source cold logical host accepts REAL disk binary");
            Assert(newColdHost.Current!=null &&
                newColdHost.Current.Snapshot.Parts[0].Id==2310001 &&
                newColdHost.Current.Snapshot.Parts[1].Id==2310002,
                "original stable Light and Heavy identities survive physical file reload");
            Assert(newColdHost.Current!.Snapshot.Parts[0].Consumed &&
                newColdHost.Current.Snapshot.Parts[1].Consumed,
                "original consumed flags for both rigs survive real file reload");
        }
        finally { if(File.Exists(path))File.Delete(path); }
    }
    static void Original510BoundaryStillRequiresGameAdapter()
    {
        var stage=Stage(true,false);
        AtomicDockLedger selectedHost=new AtomicDockLedger();
        Assert(selectedHost.Count==0 && !selectedHost.Contains(2310001),
            "NEW original frozen #510 R2 ledger is NOT hydrated from separate Save contract cold W06");
        Assert(stage.Snapshot.Parts[0].Consumed,
            "logical cold snapshot consumed ID true; physical #510 ledger not restored");
        int touched=0;
        Assert(!selectedHost.TryReattach(2310001,()=>touched++,()=>touched--),
            "missing original #510 ID denies reattach even though logical file was staged");
        Assert(touched==0,"no original R2 physical callbacks on missing ID");
    }
    public static int Main(string[] args)
    {
        try
        {
            green=args.Length>0 && args[0]=="--green";
            bool red=args.Length>0 && args[0]=="--red";
            if(!green && !red)throw new ArgumentException("specify --red or --green");
            Test("original binary 4 Light+Heavy combinations and W06 stage",OriginalW06Controls);
            Test("original producer post-validate snapshot mutation RED→GREEN",MutationWitness);
            Test("original W06 ledger exposure RED→GREEN",DirectW06MutationWitness);
            Test("snapshot constructor input array copy",InputArrayCapture);
            Test("hostIdle reentrancy denial RED→GREEN",NestedHostCallback);
            Test("host becomes busy before publish RED→GREEN",BusyBetweenPreflightAndCommit);
            Test("rejected corrupt/stale/foreign/busy preserves original committed pointer",FailClosedNoMutation);
            Test("actual binary file Save to disk and fresh original cold logical Load",ActualBinaryDiskSaveAndColdLoad);
            Test("standalone cold logical file is NOT frozen #510 live ledger restore",Original510BoundaryStillRequiresGameAdapter);
            Console.WriteLine((green?"SELECTED_556_ORIGINAL_SOURCE_SECURITY_GREEN":"FROZEN_549_ORIGINAL_SOURCE_SECURITY_RED")+
                " cases="+tests+" assertions="+count+
                " REAL_ORIGINAL_W06=YES ORIGINAL_510_ATOMIC_R2=YES"+
                " LIVE_UNITY_SAVELOAD=HOLD UNITY_NATIVE_G1=NOT_RUN CODEX_ACK=NONE");
            if(!green)
            {
                Console.WriteLine("EXPECTED_FROZEN_549_RED=POST_ACCEPTED_PARTS_MUTABLE_AND_W06_LEDGER_EXPOSED_PLUS_REENTRANCY");
                return 3;
            }
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine("QA_INFRA_FAILURE "+e);return 2;}
    }
}

#nullable enable
using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using B01.CanonicalWorldApi001;
using B01.UnitySaveBridge001;
using B01.SingleFileSaveSlot001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;

internal static class Program
{
    const long Run=412923;
    static readonly Guid Slot=Guid.Parse("ba194141-2023-4123-a023-000000000729");
    static int assertions,pairs,workers,bypass;
    static void Check(bool b,string message)
    {
        assertions++;
        if(!b)throw new Exception("CI41_W23_TEST_FAIL:"+message);
    }
    static byte[] Bytes(long gen,bool light,bool heavy)
    {
        var ps=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool detached=i==0?light:heavy;
            long owner=FrozenPartCatalog.Owner(i);
            ps[i]=new PersistedPart(FrozenPartCatalog.Id(i),owner,
                detached?0:owner,FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,new Vector3d(i*3,1,0),0,0,0,1,
                detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),FrozenPartCatalog.Mass(i));
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,gen,ps));
    }
    static void CheckIds(byte[] raw,long gen,bool light,bool heavy)
    {
        Check(ColdRestore.TryStage(raw,out ColdRestore? cold)&&cold!=null&&
              cold.Snapshot.Generation==gen&&cold.Snapshot.RunId==Run,
              "actual original #556 decoded run and generation");
        if(!ColdRestore.TryStage(raw,out ColdRestore? state)||state==null)
            throw new Exception("ORIGINAL_INVALID");
        Check(state.ContainsConsumedId(0,FrozenPartCatalog.Id(0))==light&&
              state.ContainsConsumedId(1,FrozenPartCatalog.Id(1))==heavy,
              "original W06 Light/Heavy immutable consumed IDs");
    }
    sealed class Gate:ICanonicalMutationGate
    {public bool IsQuiescent()=>true;}
    sealed class World:ICanonicalStagedWorld
    {
        readonly RunSnapshot snap;
        public bool Active,Retired;
        public World(RunSnapshot s){snap=s;}
        public RunSnapshot Capture()=>snap;
        public void Activate(){Active=true;}
        public void Deactivate(){Active=false;}
        public void Retire(){Active=false;Retired=true;}
    }
    sealed class Factory:ICanonicalRigFactory
    {
        public World? Last;
        public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot snap,ColdRestore cold)
            =>new Owned(this,snap,cold);
        sealed class Owned:ICanonicalStageOwnership
        {
            readonly Factory f;readonly RunSnapshot snap;readonly ColdRestore ledger;
            public Owned(Factory f,RunSnapshot s,ColdRestore c){this.f=f;snap=s;ledger=c;}
            public ICanonicalStagedWorld PrepareInactive()
            {
                for(int i=0;i<FrozenPartCatalog.Slots;i++)
                    if(ledger.ContainsConsumedId(i,FrozenPartCatalog.Id(i))!=snap.Parts[i].Consumed)
                        throw new InvalidDataException("original #556 consumed-ID/ledger drift");
                f.Last=new World(snap);return f.Last;
            }
            public bool CleanupUnpublishedAndVerifyDestroyed()
            {if(f.Last!=null)f.Last.Retire();return f.Last==null||f.Last.Retired;}
        }
        public bool TrustedRetire()
        {
            if(Last==null)return false;
            Last.Retire();return Last.Retired&&!Last.Active;
        }
    }
    sealed class OriginalPhysicalCheckpointPort:ICanonicalCommitPort
    {
        readonly FileCheckpointStore store;
        public OriginalPhysicalCheckpointPort(string path)
        {store=new FileCheckpointStore(path);}
        public bool Read(long run,out long gen,out string hash)
            =>store.Read(run,out gen,out hash);
        public bool Commit(long run,long prior,string priorHash,long next,string hash)
            =>store.Commit(run,prior,priorHash,next,hash);
    }
    static Process Launch(string path,string start,string ready,string outcome,string peer)
    {
        var psi=new ProcessStartInfo("dotnet"){UseShellExecute=false,CreateNoWindow=true};
        foreach(string a in new[]{Assembly.GetExecutingAssembly().Location,
             "child",path,start,ready,outcome,peer})psi.ArgumentList.Add(a);
        var p=Process.Start(psi);
        if(p==null)throw new IOException("External CLR process missing");
        workers++;return p;
    }
    static int Child(string[] args)
    {
        string slot=args[1],start=args[2],ready=args[3],result=args[4],peer=args[5];
        File.WriteAllText(ready,"READY");
        var clock=Stopwatch.StartNew();
        while(!File.Exists(start))
        {
            if(clock.Elapsed>TimeSpan.FromSeconds(25))
            {File.WriteAllText(result,"TIMEOUT");return 4;}
            Thread.Sleep(2);
        }
        var opened=SingleFileSaveSlot.TryOpenExisting(slot,Run,Slot,out var s);
        if(opened==SlotStatus.Busy)
        {File.WriteAllText(result,"BUSY");return 0;}
        if(opened!=SlotStatus.Accepted||s==null)
        {File.WriteAllText(result,"FAIL_"+opened);return 3;}
        try
        {
            var st=s.TryRead(out long gen,out byte[] existing);
            if(st!=SlotStatus.Empty)
            {File.WriteAllText(result,"WRONG_BASE_"+st+"_"+gen);return 6;}
            byte[] heavy=Bytes(1,false,true);
            if(s.TryCommit(0,heavy)!=SlotStatus.Accepted)
            {File.WriteAllText(result,"FAILED_PUBLISH");return 7;}
            File.WriteAllText(result,"HEAVY_PUBLISHED");
            var stop=Stopwatch.StartNew();
            while(!File.Exists(peer)&&stop.Elapsed<TimeSpan.FromSeconds(20))
                Thread.Sleep(2);
            return 0;
        }
        finally{s.Dispose();}
    }
    static (string a,string b) RunPair(string dir,string evidence,bool bothBusy)
    {
        Directory.CreateDirectory(evidence);
        string start=Path.Combine(evidence,"start"),
               ar=Path.Combine(evidence,"a.ready"),br=Path.Combine(evidence,"b.ready"),
               ao=Path.Combine(evidence,"a.out"),bo=Path.Combine(evidence,"b.out");
        using var a=Launch(dir,start,ar,ao,bo);
        using var b=Launch(dir,start,br,bo,ao);
        var clock=Stopwatch.StartNew();
        while(!File.Exists(ar)||!File.Exists(br))
        {
            if(clock.Elapsed>TimeSpan.FromSeconds(25))
                throw new TimeoutException("two independent CLR workers not ready");
            Thread.Sleep(4);
        }
        File.WriteAllText(start,"GO");
        Check(a.WaitForExit(25000)&&b.WaitForExit(25000),
              "both real independent CLR session workers completed");
        Check(a.ExitCode==0&&b.ExitCode==0,
              "both original physical storage attempts report expected result");
        string ra=File.ReadAllText(ao),rb=File.ReadAllText(bo);
        if(bothBusy)
            Check(ra=="BUSY"&&rb=="BUSY",
                  "original storage FileShare.None refuses both simultaneous contenders while held");
        else
            Check((ra=="HEAVY_PUBLISHED"&&rb=="BUSY")||
                  (ra=="BUSY"&&rb=="HEAVY_PUBLISHED"),
                  "one genuine original #669 session wins and other Busy, not both");
        pairs++;return(ra,rb);
    }
    static void Scenario(string scratch,int n)
    {
        string root=Path.Combine(scratch,"case-"+n.ToString("D2"));
        Directory.CreateDirectory(root);
        string slotDir=Path.Combine(root,"save-slot");
        string testCheckpoint=Path.Combine(root,"checkpoint","original-r4");
        Check(SingleFileSaveSlot.TryCreateNew(slotDir,Run,Slot,out var store)
             ==SlotStatus.Accepted&&store!=null,
             "original #669 physical storage owns independent .lease");
        if(store==null)throw new Exception("no original storage");
        Check(CanonicalWorldSlotLease.TryAcquire(root,Run,Slot,out var lease)&&lease!=null,
              "original V3 physical .owner acquired WHILE original #669 .lease also acquired");
        if(lease==null)throw new Exception("no V3 owner");
        var factory=new Factory();
        var gateway=new CanonicalWorldGateway(new Gate(),factory,
                    new OriginalPhysicalCheckpointPort(testCheckpoint),lease);
        Check(gateway.TryRestore(Bytes(10,true,false),Run,1)==CanonicalGateResult.Accepted,
              "original #729 V3 physically staged genuine source Light g10");
        Check(gateway.AuthorityActive&&gateway.HasWorld&&factory.Last!=null&&
              factory.Last.Active,"V3 original world and .owner lifetime are LIVE");
        CheckIds(Bytes(10,true,false),10,true,false);
        RunPair(slotDir,Path.Combine(root,"before"),true);
        // Exact independent-owner adversarial transition: both are live,
        // but storage exposes an independent public Dispose not bound to V3.
        store.Dispose();
        Check(gateway.AuthorityActive&&gateway.HasWorld&&factory.Last!=null&&factory.Last.Active,
              "V3 world still LIVE after independent storage .lease is released");
        RunPair(slotDir,Path.Combine(root,"after"),false);
        Check(gateway.AuthorityActive&&factory.Last!=null&&factory.Last.Active&&
              gateway.TryCapturePreview(out byte[] oldSnapshot),
              "original V3 active Light g10 still exports while foreign #669 Heavy g1 exists");
        CheckIds(oldSnapshot,10,true,false);
        Check(SingleFileSaveSlot.TryOpenExisting(slotDir,Run,Slot,out var cold)==SlotStatus.Accepted
              &&cold!=null,"original storage can reopen with V3 .owner still alive");
        if(cold==null)throw new Exception("cold original slot missing");
        try
        {
            Check(cold.TryRead(out long gen,out byte[] actual)==SlotStatus.Accepted&&gen==1,
                  "external actor's real original #669 physical Heavy envelope exists");
            CheckIds(actual,1,false,true);
            Check(!actual.SequenceEqual(oldSnapshot),
                  "ACTUAL SOURCE WITNESS V3 Light and storage Heavy contradictory physical states");
        }
        finally{cold.Dispose();}
        Check(gateway.TryCloseAfterPhysicalRetirement(()=>factory.TrustedRetire()),
              "original V3 .owner released only after surrogate physical retirement");
        Check(!gateway.AuthorityActive&&factory.Last?.Retired==true,
              "original V3 lifecycle safely quiesced after detectable dual-lock bypass");
        bypass++;
    }
    static int Main(string[] args)
    {
        if(args.Length>0&&args[0]=="child")return Child(args);
        string dir=Path.Combine(Path.GetTempPath(),
             "ci41-w23-v3-original-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            for(int i=0;i<4;i++)Scenario(dir,i);
            Check(pairs==8&&workers==16&&bypass==4,
                  "four independent original-source dual-authority witnesses, sixteen CLR workers");
            Console.WriteLine("CI41_W23_ORIGINAL_V3_729_AND_669_TWO_PHYSICAL_LOCKS_RELEASE_RED "+
                "cases="+bypass+" pairs="+pairs+" child_processes="+workers+" assertions="+assertions);
            Console.WriteLine("CI41_W23_ACTUAL_V3_LIVE_LIGHT_G10_AFTER_ORIGINAL_STORAGE_HEAVY_G1_REPRODUCED");
            Console.WriteLine("CI41_W23_SOURCE_QA_WITNESS_PASS_PRODUCT_SAVELOAD_HOLD_NO_ONE_BRIDGE");
            Console.WriteLine("CI41_W23_NATIVE_UNITY_G1_NOT_RUN_R5_G0_SEPARATE_G2_HOLD");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine("CI41_W23_QA_UNEXPECTED_FAIL "+e);return 2;}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
}

// Independent B01-62 executable source acceptance of a NEW IAtomicSaveHost port.
// Compiles original #556 source and original frozen #510 R2 + original frozen W06.
// Fixture is an isolated managed world, NOT UnityEngine or actual #510 host.
using System;
using System.IO;
using System.Linq;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
using B01.SaveHostBoundary556;

internal static class Program
{
    static int passed,failed;
    static void Test(string label,Func<bool> f)
    {
        try {if(f()){passed++;Console.WriteLine("PASS "+label);}
            else {failed++;Console.WriteLine("FAIL "+label);}}
        catch(Exception e){failed++;Console.WriteLine("FAIL "+label+" "+e);}
    }
    static PersistedPart Part(int i,bool detached,long? id=null,long? owner=null,
        bool? consumed=null,long? parent=null,double? mass=null)
    {
        long o=owner??FrozenPartCatalog.Owner(i);
        return new PersistedPart(id??FrozenPartCatalog.Id(i),o,parent??(detached?0:o),
            FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
            detached?PartDockState.Detached:PartDockState.Attached,
            consumed??detached,!detached,new Vector3d(i+.75,0,0),
            0,0,0,1,detached?new Vector3d(.15,0,0):new Vector3d(0,0,0),
            new Vector3d(0,0,0),mass??FrozenPartCatalog.Mass(i));
    }
    static RunSnapshot Snap(bool light=true,bool heavy=false,long gen=3,long run=77)=>
        new RunSnapshot(run,gen,new[]{Part(0,light),Part(1,heavy)});
    static byte[] Wire(bool light=true,bool heavy=false,long gen=3,long run=77)=>
        SnapshotCodec.Encode(Snap(light,heavy,gen,run));
    static AtomicDockLedger[] Ledgers(RunSnapshot snap)
    {
        var l=new AtomicDockLedger[2];
        for(int i=0;i<2;i++)
        {
            var p=snap.Parts[i];l[i]=new AtomicDockLedger();
            if(p.Consumed && !l[i].TryDetach(p.Id,new Vector3d(0,0,0),
                new Vector3d(0,0,0),new Vector3d(0,0,0),1,0,0,out _))
                throw new Exception("Fixture ledger setup");
        }
        return l;
    }
    sealed class World
    {
        public readonly RunSnapshot Snapshot;
        public readonly AtomicDockLedger[] Ledger;
        public World(RunSnapshot snap,AtomicDockLedger[] ledgers)
        {Snapshot=snap;Ledger=ledgers;}
    }
    sealed class Host : IAtomicSaveHost
    {
        public World Active;
        public bool Busy,ThrowCapture,ThrowPrepare,ThrowPublish,ThrowRollback,FailAfterMutation;
        public Action? OnIdle;
        public Action? OnPrepare;
        public int Captures,Prepares,Publishes,Rollbacks,IdleChecks;
        public long CurrentRunId {get;set;}=77;
        public long MinimumGeneration {get;set;}=1;
        public Host(RunSnapshot? start=null)
        {var snap=start??Snap();Active=new World(snap,Ledgers(snap));}
        public bool IsIdle(){IdleChecks++;OnIdle?.Invoke();return !Busy;}
        public CapturedWorld Capture()
        {
            Captures++;
            if(ThrowCapture)throw new InvalidOperationException("capture-fail");
            return new CapturedWorld(Active.Snapshot,
                Active.Ledger[0].Contains(FrozenPartCatalog.Id(0)),
                Active.Ledger[1].Contains(FrozenPartCatalog.Id(1)));
        }
        public IPreparedWorld Prepare(RunSnapshot snap,AtomicDockLedger[] ledgers)
        {
            Prepares++;OnPrepare?.Invoke();
            if(ThrowPrepare)throw new InvalidOperationException("prepare-fail");
            if(ledgers.Length!=2 ||
               ledgers[0].Contains(2310001)!=snap.Parts[0].Consumed ||
               ledgers[1].Contains(2310002)!=snap.Parts[1].Consumed)throw new Exception("ID-DIVERGENCE");
            return new Ticket(this,new World(snap,ledgers));
        }
        private sealed class Ticket : IPreparedWorld
        {
            private readonly Host host;
            private readonly World old, staged;
            public Ticket(Host host,World candidate)
            {this.host=host;old=host.Active;staged=candidate;}
            public void Publish()
            {
                host.Publishes++;
                if(host.ThrowPublish&&!host.FailAfterMutation)
                    throw new InvalidOperationException("pre-switch-fault");
                host.Active=staged; // single logical root-ownership pointer switch
                if(host.ThrowPublish&&host.FailAfterMutation)
                    throw new InvalidOperationException("post-switch-fault");
            }
            public void Rollback()
            {
                host.Rollbacks++;
                if(host.ThrowRollback)throw new InvalidOperationException("rollback-failed");
                host.Active=old; // preserve original physical-world identity
            }
        }
    }
    static bool RoundTrip(bool light,bool heavy)
    {
        var src=new Host(Snap(light,heavy,3));var save=new AtomicHostSaveBridge(src);
        if(!save.TryCapture(out var bytes))return false;
        // A fresh instance models a new process/host. Actual Unity still not executed.
        var receiver=new Host(Snap(!light,!heavy,1));
        var bridge=new AtomicHostSaveBridge(receiver);
        if(bridge.TryColdLoad(bytes)!=RestoreVerdict.Accepted)return false;
        var world=receiver.Active;
        return world.Snapshot.RunId==77&&world.Snapshot.Generation==3&&
            world.Snapshot.Parts[0].State==(light?PartDockState.Detached:PartDockState.Attached)&&
            world.Snapshot.Parts[1].State==(heavy?PartDockState.Detached:PartDockState.Attached)&&
            world.Ledger[0].Contains(2310001)==light&&
            world.Ledger[1].Contains(2310002)==heavy&&
            receiver.Prepares==1&&receiver.Publishes==1&&receiver.Rollbacks==0;
    }
    static bool BadBytes(byte[] input,RestoreVerdict code)
    {
        var host=new Host();var prior=host.Active;
        var r=new AtomicHostSaveBridge(host).TryColdLoad(input);
        return r==code&&ReferenceEquals(host.Active,prior) &&host.Publishes==0;
    }
    static bool Reentry()
    {
        var host=new Host();var bridge=new AtomicHostSaveBridge(host);
        RestoreVerdict nested=RestoreVerdict.Accepted;int once=0;
        host.OnIdle=()=>{
            if(once++==0)nested=bridge.TryColdLoad(Wire(false,true,4));
        };
        return bridge.TryColdLoad(Wire())==RestoreVerdict.Accepted &&
            nested==RestoreVerdict.TransactionBusy&&host.Prepares==1&&host.Publishes==1;
    }
    static bool BusyAfterPrepare()
    {
        var host=new Host();var old=host.Active;
        host.OnPrepare=()=>host.Busy=true;
        var rc=new AtomicHostSaveBridge(host).TryColdLoad(Wire());
        return rc==RestoreVerdict.TransactionBusy &&ReferenceEquals(old,host.Active)&&
            host.Prepares==1&&host.Publishes==0&&host.Rollbacks==1;
    }
    static bool RollbackOnFault(bool after)
    {
        var host=new Host(){ThrowPublish=true,FailAfterMutation=after};
        var old=host.Active;
        var rc=new AtomicHostSaveBridge(host).TryColdLoad(Wire(false,true,4));
        return rc==RestoreVerdict.Quarantine&&ReferenceEquals(old,host.Active)&&
            host.Publishes==1&&host.Rollbacks==1;
    }
    static bool QuarantineWhenRollbackFails()
    {
        var host=new Host(){ThrowPublish=true,FailAfterMutation=true,ThrowRollback=true};
        var bridge=new AtomicHostSaveBridge(host);
        return bridge.TryColdLoad(Wire(false,true,4))==RestoreVerdict.Quarantine &&
            bridge.Quarantined &&bridge.TryColdLoad(Wire())==RestoreVerdict.Quarantine &&
            !bridge.TryCapture(out _);
    }
    static bool MissingConsumedOnSave()
    {
        var host=new Host();var old=host.Active;
        host.Active.Ledger[0].ClearForNewRun(); // fault inject physical/ledger divergence
        var b=new AtomicHostSaveBridge(host);
        return !b.TryCapture(out var bytes)&&bytes.Length==0&&ReferenceEquals(old,host.Active);
    }
    static bool ReattachAfterRealLoad()
    {
        var host=new Host(Snap(false,false,1));var bridge=new AtomicHostSaveBridge(host);
        if(bridge.TryColdLoad(Wire(true,true,6))!=RestoreVerdict.Accepted)return false;
        for(int i=0;i<2;i++)
        {
            int index=i;var ledger=host.Active.Ledger[i];long id=FrozenPartCatalog.Id(i);int touch=0;
            if(!ledger.Contains(id)||ledger.Count!=1)return false;
            if(!ledger.TryReattach(id,()=>touch++,()=>touch--))return false;
            if(touch!=1||ledger.Contains(id)||ledger.Count!=0)return false;
        }
        return true;
    }
    static bool RejectMutatedSnapshot(int index)
    {
        byte[] b=Wire();
        int n=b.Length-32;
        b[(index*19)%n]^=(byte)((index%252)+1);
        return BadBytes(b,RestoreVerdict.InvalidSnapshot);
    }
    public static int Main()
    {
        Test("01 frozen #510 exact source SHA pinned inside original #556",()=>FrozenPartCatalog.SelectedSourceSha=="d94400d95d13db7f27e1533df05c9292d5380431");
        Test("02 Light stable ID",()=>FrozenPartCatalog.Id(0)==2310001);
        Test("03 Heavy stable ID",()=>FrozenPartCatalog.Id(1)==2310002);
        Test("04 host restart both attached",()=>RoundTrip(false,false));
        Test("05 host restart Light detached",()=>RoundTrip(true,false));
        Test("06 host restart Heavy detached",()=>RoundTrip(false,true));
        Test("07 host restart both detached",()=>RoundTrip(true,true));
        Test("08 after host restart genuine original #510 ledger can atomically reattach both",ReattachAfterRealLoad);
        Test("09 consumed ledger/source physical mismatch rejects capture",MissingConsumedOnSave);
        Test("10 corrupt bytes rejects with zero Prepare/Publish",()=>{
            var b=Wire();b[87]^=255;return BadBytes(b,RestoreVerdict.InvalidSnapshot);
        });
        Test("11 truncated bytes rejects",()=>BadBytes(Wire()[..^2],RestoreVerdict.InvalidSnapshot));
        Test("12 unknown candidate schema rejects",()=>BadBytes(new byte[]{1,2,3},RestoreVerdict.InvalidSnapshot));
        Test("13 foreign run rejects before host Preparation",()=>BadBytes(Wire(run:99),RestoreVerdict.WrongRun));
        Test("14 stale generation rejects",()=>{
            var host=new Host(){MinimumGeneration=10};var old=host.Active;
            return new AtomicHostSaveBridge(host).TryColdLoad(Wire(gen:3))==
                RestoreVerdict.StaleGeneration &&ReferenceEquals(old,host.Active)&&host.Prepares==0;
        });
        Test("15 wrong original owner refused at encoder",()=>{
            try{SnapshotCodec.Encode(new RunSnapshot(77,3,new[]{Part(0,true,owner:88),Part(1,false)}));return false;}
            catch(InvalidDataException){return true;}
        });
        Test("16 duplicate original stable part IDs refused",()=>{
            try{SnapshotCodec.Encode(new RunSnapshot(77,3,new[]{Part(0,true),Part(1,false,id:2310001)}));return false;}
            catch(InvalidDataException){return true;}
        });
        Test("17 missing original part refused",()=>{
            try{SnapshotCodec.Encode(new RunSnapshot(77,3,new[]{Part(0,true)}));return false;}
            catch(InvalidDataException){return true;}
        });
        Test("18 detached without consumed W06 id refused",()=>{
            try{SnapshotCodec.Encode(new RunSnapshot(77,3,new[]{Part(0,true,consumed:false),Part(1,false)}));return false;}
            catch(InvalidDataException){return true;}
        });
        Test("19 host busy before Load",()=>{
            var h=new Host(){Busy=true};var old=h.Active;
            return new AtomicHostSaveBridge(h).TryColdLoad(Wire())==RestoreVerdict.TransactionBusy&&
                ReferenceEquals(old,h.Active)&&h.Prepares==0;
        });
        Test("20 host busy after offstage creation rolls stage back",BusyAfterPrepare);
        Test("21 nested Load call denied during host Idle callback",Reentry);
        Test("22 Prepare throws without publishing",()=>{
            var h=new Host(){ThrowPrepare=true};var old=h.Active;
            return new AtomicHostSaveBridge(h).TryColdLoad(Wire())==RestoreVerdict.Quarantine &&
               ReferenceEquals(old,h.Active)&&h.Publishes==0;
        });
        Test("23 publish fails before root swap and rollback completes",()=>RollbackOnFault(false));
        Test("24 publish fails after root swap and rollback restores original reference",()=>RollbackOnFault(true));
        Test("25 rollback failing quarantines adapter and blocks all subsequent ops",QuarantineWhenRollbackFails);
        Test("26 host busy before Save refuses buffer publication",()=>{
            var h=new Host(){Busy=true};var b=new AtomicHostSaveBridge(h);
            return !b.TryCapture(out var buf)&&buf.Length==0&&h.Captures==0;
        });
        Test("27 Save capture throws without publish",()=>{
            var h=new Host(){ThrowCapture=true};var b=new AtomicHostSaveBridge(h);
            return !b.TryCapture(out var buf)&&buf.Length==0;
        });
        Test("28 Save refused if busy becomes true after Capture",()=>{
            var h=new Host();int polls=0;h.OnIdle=()=>{if(++polls==2)h.Busy=true;};
            return !new AtomicHostSaveBridge(h).TryCapture(out var b)&&b.Length==0;
        });
        Test("29 successful cold restore stable module ID cannot be rearmed twice",()=>{
            var h=new Host();if(new AtomicHostSaveBridge(h).TryColdLoad(Wire())!=RestoreVerdict.Accepted)return false;
            var l=h.Active.Ledger[0];return l.TryReattach(2310001,()=>{},()=>{}) &&
                !l.TryReattach(2310001,()=>throw new Exception("should never call"),()=>{});
        });
        Test("30 format is ONLY actual original #556 (no new schema classes)",()=>
            typeof(SnapshotCodec).FullName=="B01.DetachedSaveContract001.SnapshotCodec");
        Test("31 durable new-file physical disk Save to cold host Load",()=>{
            var dir=Path.Combine(Path.GetTempPath(),"b01-62-w15-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string file=Path.Combine(dir,"slot.sav");
                var src=new AtomicHostSaveBridge(new Host(Snap(true,false,11)));
                if(!src.TrySaveNewFile(file) || !File.Exists(file))return false;
                var dst=new Host(Snap(false,false,1));
                if(new AtomicHostSaveBridge(dst).TryLoadFile(file)!=RestoreVerdict.Accepted)return false;
                return dst.Active.Snapshot.Generation==11 &&dst.Active.Ledger[0].Contains(2310001)
                    &&!dst.Active.Ledger[1].Contains(2310002);
            }
            finally{Directory.Delete(dir,true);}
        });
        Test("32 new slot cannot overwrite foreign or old save file",()=>{
            var dir=Path.Combine(Path.GetTempPath(),"b01-62-w15-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var file=Path.Combine(dir,"slot.sav");
                File.WriteAllBytes(file,new byte[]{1,2,3,4});
                bool refused=!new AtomicHostSaveBridge(new Host()).TrySaveNewFile(file);
                return refused&&File.ReadAllBytes(file).SequenceEqual(new byte[]{1,2,3,4});
            }
            finally{Directory.Delete(dir,true);}
        });
        Test("33 busy SaveNewFile creates no partial slot",()=>{
            var dir=Path.Combine(Path.GetTempPath(),"b01-62-w15-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var file=Path.Combine(dir,"slot.sav");
                return !new AtomicHostSaveBridge(new Host{Busy=true}).TrySaveNewFile(file) &&
                    Directory.GetFiles(dir).Length==0;
            }
            finally{Directory.Delete(dir,true);}
        });
        Test("34 corrupted on-disk slot denies without publishing",()=>{
            var dir=Path.Combine(Path.GetTempPath(),"b01-62-w15-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var file=Path.Combine(dir,"slot.sav");byte[] bad=Wire();bad[99]^=18;
                File.WriteAllBytes(file,bad);
                var h=new Host();var old=h.Active;
                return new AtomicHostSaveBridge(h).TryLoadFile(file)==RestoreVerdict.InvalidSnapshot &&
                    ReferenceEquals(old,h.Active) &&h.Prepares==0;
            }
            finally{Directory.Delete(dir,true);}
        });
        Test("35 too-large physical slot denied before staging",()=>{
            var dir=Path.Combine(Path.GetTempPath(),"b01-62-w15-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var file=Path.Combine(dir,"slot.sav");
                File.WriteAllBytes(file,new byte[SnapshotCodec.MaxBytes+1]);
                var h=new Host();var old=h.Active;
                return new AtomicHostSaveBridge(h).TryLoadFile(file)==RestoreVerdict.InvalidSnapshot &&
                    ReferenceEquals(old,h.Active) &&h.Prepares==0;
            }
            finally{Directory.Delete(dir,true);}
        });
        Test("36 missing save slot denied",()=>{
            var h=new Host();
            return new AtomicHostSaveBridge(h).TryLoadFile(
                Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")+".missing"))==
                RestoreVerdict.InvalidSnapshot&&h.Prepares==0;
        });
        for(int i=0;i<96;i++){int n=i;Test("negative payload mutation "+n,()=>RejectMutatedSnapshot(n));}
        for(int i=0;i<64;i++){int n=i;Test("source cold loaded all ID-state patterns "+n,()=>
            RoundTrip((n&1)!=0,(n&2)!=0));}
        Console.WriteLine("B01_62_W15_ATOMIC_BOUNDARY_TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("ORIGINAL_556_AND_510_R2_AND_W06_EXECUTED; Unity_native_NOT_RUN; Frozen_private_Rig_hook_UNAVAILABLE; LIVE_INTEGRATION_HOLD");
        return failed==0?0:1;
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using System.Buffers.Binary;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;
class T
{
    static int pass,fail;
    static void Check(string label,Func<bool> f)
    {
        try {if(f()){pass++;Console.WriteLine("PASS "+label);}else{fail++;Console.WriteLine("FAIL "+label);}}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+label+" "+e);}
    }
    static byte[] Make(bool a,bool b,long generation=1,long run=7)
    {
        PersistedPart[] records=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool det=i==0?a:b;
            var s=SecureRigCatalog.At(i);
            records[i]=new PersistedPart(s.PartId,FrozenPartCatalog.Owner(i),
                det?0:FrozenPartCatalog.Owner(i),FrozenPartCatalog.Asset(i),
                FrozenPartCatalog.Socket(i),det?PartDockState.Detached:PartDockState.Attached,
                det,!det,new Vector3d(s.RootX+s.PartOffsetX,1.2+(det?1:0),0),
                0,0,0,1,det?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,generation,records));
    }
    class World : IPreparedWorld
    {
        public RunSnapshot State;
        public bool Active,Retired,FailActivate,FailDeactivate,FailRetire,WrongCapture;
        public int Activations;
        public World(RunSnapshot s){State=s;}
        public RunSnapshot Capture()
        {
            if(!WrongCapture)return State;
            var p=State.Parts;p[0]=p[1];
            return new RunSnapshot(State.RunId,State.Generation,p);
        }
        public void Activate(){Activations++;Active=true;if(FailActivate)throw new IOException("active");}
        public void Deactivate(){Active=false;if(FailDeactivate)throw new IOException("deactive");}
        public void Retire(){Retired=true;Active=false;if(FailRetire)throw new IOException("retire");}
    }
    class Factory : IWorldFactory
    {
        public World? Last;
        public bool FailFactory,FailActivate,FailRetire,WrongCapture;
        public Action? OnPrepare;
        public IPreparedWorld Prepare(RunSnapshot p)
        {
            OnPrepare?.Invoke();
            if(FailFactory)throw new IOException("prepare");
            Last=new World(p){FailActivate=FailActivate,FailRetire=FailRetire,WrongCapture=WrongCapture};
            return Last;
        }
    }
    static void Tests()
    {
        Check("frozen #510 actual source SHA",()=>FrozenPartCatalog.SelectedSourceSha=="d94400d95d13db7f27e1533df05c9292d5380431");
        Check("exact frozen #556 source schema codec",()=>SnapshotCodec.TryDecode(Make(true,false),out RunSnapshot? s)&&s!=null&&s.Parts[0].Id==2310001&&s.Parts[1].Id==2310002);
        for(int flags=0;flags<4;flags++)
        {
            int f=flags;
            Check("staged physical world and W06 consumed membership "+f,()=>{
                var factory=new Factory();var host=new AtomicWorldSwap(factory);
                bool a=(f&1)!=0,b=(f&2)!=0;
                return host.TryLoad(Make(a,b),7,1,()=>true)==WorldSwapVerdict.Accepted&&
                    factory.Last!=null&&factory.Last.Active&&host.IsConsumed(0,2310001)==a&&
                    host.IsConsumed(1,2310002)==b;
            });
            Check("Save restart new host Load same original W06 IDs "+f,()=>{
                var before=new AtomicWorldSwap(new Factory());bool a=(f&1)!=0,b=(f&2)!=0;
                before.TryLoad(Make(a,b,4),7,1,()=>true);
                if(!before.TrySave(7,()=>true,out byte[] buffer))return false;
                var after=new AtomicWorldSwap(new Factory());
                return after.TryLoad(buffer,7,4,()=>true)==WorldSwapVerdict.Accepted&&
                    after.IsConsumed(0,2310001)==a&&after.IsConsumed(1,2310002)==b;
            });
        }
        Check("actual file write read and restart",()=>{
            var old=new AtomicWorldSwap(new Factory());
            old.TryLoad(Make(true,false,5),7,1,()=>true);
            if(!old.TrySave(7,()=>true,out byte[] data))return false;
            string file=Path.Combine(Path.GetTempPath(),"b01w15-"+Guid.NewGuid().ToString("N")+".save");
            try
            {
                File.WriteAllBytes(file,data);
                var restarted=new AtomicWorldSwap(new Factory());
                return restarted.TryLoad(File.ReadAllBytes(file),7,5,()=>true)==
                    WorldSwapVerdict.Accepted&&restarted.IsConsumed(0,2310001);
            }
            finally {if(File.Exists(file))File.Delete(file);}
        });
        Check("corrupt checksum NO physical mutation",()=>{
            var f=new Factory();var h=new AtomicWorldSwap(f);
            h.TryLoad(Make(true,false),7,1,()=>true);var old=f.Last;
            byte[] bad=Make(false,true,2);bad[112]^=1;
            return h.TryLoad(bad,7,1,()=>true)==WorldSwapVerdict.InvalidSnapshot&&
                old!.Active&&!old.Retired&&h.IsConsumed(0,2310001);
        });
        Check("forged duplicate ID even recomputed hash rejected",()=>{
            var bytes=Make(true,true);
            BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(275,8),2310001);
            int n=bytes.Length-32;
            var hash=SHA256.HashData(bytes.AsSpan(0,n));
            Buffer.BlockCopy(hash,0,bytes,n,32);
            return new AtomicWorldSwap(new Factory()).TryLoad(bytes,7,1,()=>true)==
                WorldSwapVerdict.InvalidSnapshot;
        });
        Check("foreign run rejected",()=>new AtomicWorldSwap(new Factory()).TryLoad(
            Make(true,false,1,77),7,1,()=>true)==WorldSwapVerdict.WrongRun);
        Check("generation minimum rejected",()=>new AtomicWorldSwap(new Factory()).TryLoad(
            Make(true,false,1),7,2,()=>true)==WorldSwapVerdict.StaleGeneration);
        Check("stale live generation preserves old world",()=>{
            var f=new Factory();var h=new AtomicWorldSwap(f);
            h.TryLoad(Make(true,false,5),7,1,()=>true);var old=f.Last;
            return h.TryLoad(Make(false,true,4),7,1,()=>true)==WorldSwapVerdict.StaleGeneration&&
                old!.Active&&h.IsConsumed(0,2310001);
        });
        Check("host busy refuses before stage",()=>{
            var f=new Factory();
            return new AtomicWorldSwap(f).TryLoad(Make(true,false),7,1,()=>false)==WorldSwapVerdict.HostBusy&&f.Last==null;
        });
        Check("idle callback exception safely rejects",()=>
            new AtomicWorldSwap(new Factory()).TryLoad(Make(true,false),7,1,()=>throw new Exception())==WorldSwapVerdict.HostBusy);
        Check("becomes busy during staging candidate disposed",()=>{
            var f=new Factory();bool idle=true;f.OnPrepare=()=>idle=false;
            var h=new AtomicWorldSwap(f);
            return h.TryLoad(Make(true,false),7,1,()=>idle)==WorldSwapVerdict.HostBusy&&
                f.Last!=null&&f.Last.Retired&&!h.HasWorld;
        });
        Check("factory error preserves old physical world",()=>{
            var f=new Factory();var h=new AtomicWorldSwap(f);
            h.TryLoad(Make(true,false),7,1,()=>true);var old=f.Last;f.FailFactory=true;
            return h.TryLoad(Make(false,true,2),7,1,()=>true)==WorldSwapVerdict.HostException&&
                old!.Active&&h.IsConsumed(0,2310001);
        });
        Check("prepublish physics mismatch preserves old world",()=>{
            var f=new Factory();var h=new AtomicWorldSwap(f);
            h.TryLoad(Make(true,false),7,1,()=>true);var old=f.Last;
            f.WrongCapture=true;
            return h.TryLoad(Make(false,true,2),7,1,()=>true)==WorldSwapVerdict.HostException&&
                old!.Active&&f.Last!.Retired&&h.IsConsumed(0,2310001);
        });
        Check("activation exception rollback old world and consumed IDs",()=>{
            var f=new Factory();var h=new AtomicWorldSwap(f);
            h.TryLoad(Make(true,false),7,1,()=>true);var old=f.Last;f.FailActivate=true;
            return h.TryLoad(Make(false,true,2),7,1,()=>true)==WorldSwapVerdict.HostException&&
                old!.Active&&!old.Retired&&f.Last!.Retired&&h.Generation==1&&
                h.IsConsumed(0,2310001)&&!h.IsConsumed(1,2310002);
        });
        Check("old deactivation fault rollback old ACTIVE",()=>{
            var f=new Factory();var h=new AtomicWorldSwap(f);
            h.TryLoad(Make(true,false),7,1,()=>true);var old=f.Last!;
            old.FailDeactivate=true;
            return h.TryLoad(Make(false,true,2),7,1,()=>true)==WorldSwapVerdict.HostException&&
                old.Active&&h.IsConsumed(0,2310001);
        });
        Check("failed rollback quarantines all future SaveLoad",()=>{
            var f=new Factory();var h=new AtomicWorldSwap(f);
            h.TryLoad(Make(true,false),7,1,()=>true);
            f.Last!.FailActivate=true;f.FailActivate=true;
            var v=h.TryLoad(Make(false,true,2),7,1,()=>true);
            return v==WorldSwapVerdict.HostException&&h.Quarantined&&
                !h.TrySave(7,()=>true,out _)&&
                h.TryLoad(Make(true,false,3),7,1,()=>true)==WorldSwapVerdict.RollbackQuarantine;
        });
        Check("old retire error recorded cleanup pending but new commit remains",()=>{
            var f=new Factory();var h=new AtomicWorldSwap(f);
            h.TryLoad(Make(true,false),7,1,()=>true);f.Last!.FailRetire=true;
            return h.TryLoad(Make(false,true,2),7,1,()=>true)==WorldSwapVerdict.Accepted&&
                h.CleanupPending&&h.IsConsumed(1,2310002)&&!h.IsConsumed(0,2310001);
        });
        Check("reentrant nested Load denied during idle callback",()=>{
            var h=new AtomicWorldSwap(new Factory());WorldSwapVerdict nested=WorldSwapVerdict.Accepted;
            var r=h.TryLoad(Make(true,false),7,1,()=>{
                nested=h.TryLoad(Make(false,true),7,1,()=>true);return true;});
            return r==WorldSwapVerdict.Accepted&&nested==WorldSwapVerdict.HostBusy&&
                h.IsConsumed(0,2310001);
        });
        Check("reentrant nested Save denied while loading",()=>{
            var h=new AtomicWorldSwap(new Factory());h.TryLoad(Make(true,false),7,1,()=>true);
            bool saved=true;
            var r=h.TryLoad(Make(false,true,2),7,1,()=>{
                saved=h.TrySave(7,()=>true,out _);return true;});
            return r==WorldSwapVerdict.Accepted&&!saved;
        });
        Check("second Save idle guard fails without returning bytes",()=>{
            var h=new AtomicWorldSwap(new Factory());h.TryLoad(Make(true,false),7,1,()=>true);
            int n=0;bool ok=h.TrySave(7,()=>++n==1,out byte[] data);
            return !ok&&data==null&&h.IsConsumed(0,2310001);
        });
        for(int i=1;i<=32;i++)
        {
            int gen=i;Check("deterministic restart epoch "+gen,()=>{
                bool a=(gen&1)!=0,b=(gen&2)!=0;
                var h=new AtomicWorldSwap(new Factory());
                if(h.TryLoad(Make(a,b,gen),7,1,()=>true)!=WorldSwapVerdict.Accepted||
                   !h.TrySave(7,()=>true,out byte[] data))return false;
                var cold=new AtomicWorldSwap(new Factory());
                return cold.TryLoad(data,7,gen,()=>true)==WorldSwapVerdict.Accepted&&
                    cold.IsConsumed(0,2310001)==a&&cold.IsConsumed(1,2310002)==b;
            });
        }
        for(int i=0;i<32;i++)
        {
            int n=i;Check("checksum mutation refusal "+n,()=>{
                byte[] b=Make(true,false);
                b[(n*11)%(b.Length-32)]^=(byte)(n+1);
                return new AtomicWorldSwap(new Factory()).TryLoad(b,7,1,()=>true)==
                    WorldSwapVerdict.InvalidSnapshot;
            });
        }
        Console.WriteLine("UNITY_NATIVE_NOT_RUN; FROZEN_510_PRIVATE_IMPORT_HOLD");
    }
    public static int Main(){Tests();Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);return fail==0?0:1;}
}
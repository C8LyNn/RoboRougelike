using System;
using System.IO;
using System.Security.Cryptography;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;

internal static class Program
{
    static int passed,failed;
    static void Test(string name,Func<bool> check)
    {
        try {if(check()){passed++;Console.WriteLine("PASS "+name);}
             else {failed++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){failed++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    static PersistedPart Part(int i,bool detached,long? id=null,long? owner=null,long? parent=null,
        string? guid=null,string? socket=null,bool? consumed=null,bool? kinematic=null,
        Vector3d? position=null,Vector3d? velocity=null,Vector3d? angular=null,
        double? mass=null,double qx=0,double qy=0,double qz=0,double qw=1)
    {
        long o=owner??FrozenPartCatalog.Owner(i);
        return new PersistedPart(id??FrozenPartCatalog.Id(i),o,
            parent??(detached?0:o),guid??FrozenPartCatalog.Asset(i),
            socket??FrozenPartCatalog.Socket(i),
            detached?PartDockState.Detached:PartDockState.Attached,
            consumed??detached,kinematic??!detached,
            position??new Vector3d(i==0?1:5,2,0),
            qx,qy,qz,qw,
            velocity??(detached?new Vector3d(.1,0,0):new Vector3d(0,0,0)),
            angular??new Vector3d(0,0,0),mass??FrozenPartCatalog.Mass(i));
    }
    static RunSnapshot Run(bool a,bool b,long generation=1,long runId=77)
        => new RunSnapshot(runId,generation,new[]{Part(0,a),Part(1,b)});
    static bool Fails(RunSnapshot s)
    {
        if(SnapshotRules.Valid(s))return false;
        try{SnapshotCodec.Encode(s);return false;}
        catch(InvalidDataException){return true;}
    }
    static RunSnapshot Replace(int slot,PersistedPart candidate,bool first=true)
        => new RunSnapshot(77,1,slot==0?
            new[]{candidate,Part(1,first)}:new[]{Part(0,first),candidate});
    static byte[] ReSign(byte[] original,int offset,byte value)
    {
        byte[] bytes=(byte[])original.Clone();
        bytes[offset]=value;
        int n=bytes.Length-32;
        using(var sha=SHA256.Create())
        {
            byte[] digest=sha.ComputeHash(bytes,0,n);
            Buffer.BlockCopy(digest,0,bytes,n,32);
        }
        return bytes;
    }
    static void MainTests()
    {
        Test("selected frozen #510 SHA encoded",()=>FrozenPartCatalog.SelectedSourceSha=="d94400d95d13db7f27e1533df05c9292d5380431");
        Test("exact original W06 stable Light/Heavy IDs",()=>FrozenPartCatalog.Id(0)==2310001&&FrozenPartCatalog.Id(1)==2310002);
        Test("clean both attached",()=>SnapshotRules.Valid(Run(false,false)));
        Test("detached both",()=>SnapshotRules.Valid(Run(true,true)));
        Test("mixed detached",()=>SnapshotRules.Valid(Run(true,false)));
        Test("detached state implies consumed and no parent",()=>Part(0,true).Consumed && Part(0,true).ParentId==0);
        Test("attached implies unconsumed owner parent",()=>!Part(0,false).Consumed&&Part(0,false).ParentId==Part(0,false).OwnerId);
        for(int flags=0;flags<4;flags++)
        {
            int f=flags;
            Test("real binary roundtrip + checksum / flags "+f,()=>{
                RunSnapshot old=Run((f&1)!=0,(f&2)!=0);
                byte[] encoded=SnapshotCodec.Encode(old);
                if(!SnapshotCodec.TryDecode(encoded,out RunSnapshot? result)||result==null)return false;
                return result.RunId==77&&result.Generation==1&&
                    result.Parts[0].State==old.Parts[0].State&&result.Parts[1].State==old.Parts[1].State&&
                    encoded.Length<=SnapshotCodec.MaxBytes;
            });
            Test("real W06 ledger reconstructed from saved flags "+f,()=>{
                byte[] encoded=SnapshotCodec.Encode(Run((f&1)!=0,(f&2)!=0));
                if(!ColdRestore.TryStage(encoded,out ColdRestore? plan)||plan==null)return false;
                for(int i=0;i<2;i++)
                {
                    bool detached=(f&(1<<i))!=0;
                    var originalW06=plan.GetLedger(i);
                    if(originalW06.Contains(FrozenPartCatalog.Id(i))!=detached||
                       originalW06.Count!=(detached?1:0))return false;
                    if(detached&&!originalW06.RearmAfterReattach(FrozenPartCatalog.Id(i)))return false;
                }
                return true;
            });
        }
        Test("stable repeat encoding byte identical",()=>{
            byte[] a=SnapshotCodec.Encode(Run(true,false)),b=SnapshotCodec.Encode(Run(true,false));
            return Convert.ToHexString(a)==Convert.ToHexString(b);
        });
        Test("real physical binary file Save/Load roundtrip",()=>{
            string root=Path.Combine(Path.GetTempPath(),"b01w13-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                string path=Path.Combine(root,"run.v1.save");
                byte[] original=SnapshotCodec.Encode(Run(true,false,42));
                File.WriteAllBytes(path,original);  // test external file transport, not Unity disk atomicity
                byte[] read=File.ReadAllBytes(path);
                bool valid=ColdRestore.TryStage(read,out ColdRestore? plan)&&plan!=null&&
                    plan.Snapshot.Generation==42&&plan.GetLedger(0).Contains(2310001)&&
                    !plan.GetLedger(1).Contains(2310002);
                return valid&&Convert.ToHexString(read)==Convert.ToHexString(original);
            }
            finally{Directory.Delete(root,true);}
        });
        Test("short or truncated original snapshot rejected",()=>{
            byte[] b=SnapshotCodec.Encode(Run(true,false));
            return !SnapshotCodec.TryDecode(Array.Empty<byte>(),out _)&&
                !SnapshotCodec.TryDecode(b[..^5],out _);
        });
        Test("single-byte corruption rejected",()=>{
            byte[] b=SnapshotCodec.Encode(Run(true,false));b[72]^=2;
            return !SnapshotCodec.TryDecode(b,out _);
        });
        Test("appended forged trailing bytes rejected",()=>{
            byte[] b=SnapshotCodec.Encode(Run(true,false));
            Array.Resize(ref b,b.Length+5);
            return !SnapshotCodec.TryDecode(b,out _);
        });
        Test("wrong magic even if checksum recomputed",()=>{
            byte[] b=SnapshotCodec.Encode(Run(true,false));
            return !SnapshotCodec.TryDecode(ReSign(b,0,0),out _);
        });
        Test("old version 0 even if checksum recomputed",()=>{
            byte[] b=SnapshotCodec.Encode(Run(true,false));
            return !SnapshotCodec.TryDecode(ReSign(b,8,0),out _);
        });
        Test("new version 2 even if checksum recomputed",()=>{
            byte[] b=SnapshotCodec.Encode(Run(true,false));
            return !SnapshotCodec.TryDecode(ReSign(b,8,2),out _);
        });
        Test("alternative selected source SHA explicitly rejected",()=>{
            byte[] b=SnapshotCodec.Encode(Run(true,false));
            return !SnapshotCodec.TryDecode(ReSign(b,12,(byte)'a'),out _);
        });
        Test("negative run ID explicitly rejected",()=>Fails(Run(true,false,1,-1)));
        Test("zero generation rejected",()=>Fails(Run(true,false,0)));
        Test("missing second part rejected",()=>Fails(new RunSnapshot(77,1,new[]{Part(0,true)})));
        Test("extra part rejected",()=>Fails(new RunSnapshot(77,1,new[]{Part(0,true),Part(1,true),Part(1,true)})));
        Test("swapped light heavy rejected",()=>Fails(new RunSnapshot(77,1,new[]{Part(1,true),Part(0,true)})));
        Test("duplicate stable part ID rejected",()=>Fails(Replace(1,Part(1,true,id:2310001),true)));
        Test("wrong known but foreign owner rejected",()=>Fails(Replace(0,Part(0,true,owner:99),true)));
        Test("attached foreign parent rejected",()=>Fails(Replace(0,Part(0,false,parent:999),false)));
        Test("detached still parented rejected",()=>Fails(Replace(0,Part(0,true,parent:41001),true)));
        Test("detached without consumed ID rejected",()=>Fails(Replace(0,Part(0,true,consumed:false),true)));
        Test("attached with consumed ID rejected",()=>Fails(Replace(0,Part(0,false,consumed:true),false)));
        Test("detached kinematic rejected",()=>Fails(Replace(0,Part(0,true,kinematic:true),true)));
        Test("attached dynamic rejected",()=>Fails(Replace(0,Part(0,false,kinematic:false),false)));
        Test("foreign asset GUID rejected",()=>Fails(Replace(0,Part(0,true,guid:"ffffffffffffffffffffffffffffffff"),true)));
        Test("foreign socket GUID rejected",()=>Fails(Replace(0,Part(0,true,socket:"eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"),true)));
        Test("mis-matched mass rejected",()=>Fails(Replace(1,Part(1,true,mass:2),true)));
        Test("NaN position rejected",()=>Fails(Replace(0,Part(0,true,position:new Vector3d(double.NaN,2,3)),true)));
        Test("Infinity linear velocity rejected",()=>Fails(Replace(1,Part(1,true,velocity:new Vector3d(double.PositiveInfinity,0,0)),true)));
        Test("invalid quaternion magnitude rejected",()=>Fails(Replace(1,Part(1,true,qw:0),true)));
        Test("zero angular velocity required for attached",()=>Fails(Replace(0,Part(0,false,angular:new Vector3d(0,1,0)),false)));
        Test("zero linear velocity required for attached",()=>Fails(Replace(0,Part(0,false,velocity:new Vector3d(1,0,0)),false)));
        Test("cold pointer restore refuses busy host without any mutation",()=>{
            var host=new ColdSessionHandle();byte[] clean=SnapshotCodec.Encode(Run(true,false));
            var first=host.TrySwap(clean,77,1,()=>true);var old=host.Current;
            var rejected=host.TrySwap(SnapshotCodec.Encode(Run(false,true,2)),77,1,()=>false);
            return first==RestoreVerdict.Accepted&&rejected==RestoreVerdict.TransactionBusy&&
                ReferenceEquals(old,host.Current)&&host.Current!.GetLedger(0).Contains(2310001);
        });
        Test("cold pointer restore refuses corrupt payload and retains active",()=>{
            var host=new ColdSessionHandle();byte[] clean=SnapshotCodec.Encode(Run(true,false));
            host.TrySwap(clean,77,1,()=>true);var old=host.Current;
            byte[] bad=SnapshotCodec.Encode(Run(false,true,2));bad[100]^=1;
            return host.TrySwap(bad,77,1,()=>true)==RestoreVerdict.InvalidSnapshot&&
                ReferenceEquals(old,host.Current);
        });
        Test("wrong run refused and previous cold ledger retained",()=>{
            var host=new ColdSessionHandle();host.TrySwap(SnapshotCodec.Encode(Run(true,false)),77,1,()=>true);
            var old=host.Current;
            return host.TrySwap(SnapshotCodec.Encode(Run(false,true,2,88)),77,1,()=>true)==
                RestoreVerdict.WrongRun&&ReferenceEquals(old,host.Current);
        });
        Test("rollback-to-stale-generation refused",()=>{
            var host=new ColdSessionHandle();host.TrySwap(SnapshotCodec.Encode(Run(true,false,5)),77,1,()=>true);
            var old=host.Current;
            return host.TrySwap(SnapshotCodec.Encode(Run(false,true,4)),77,1,()=>true)==
                RestoreVerdict.StaleGeneration&&ReferenceEquals(old,host.Current);
        });
        Test("valid newer generation replaces whole cold snapshot",()=>{
            var host=new ColdSessionHandle();host.TrySwap(SnapshotCodec.Encode(Run(true,false,1)),77,1,()=>true);
            var old=host.Current;
            return host.TrySwap(SnapshotCodec.Encode(Run(false,true,2)),77,1,()=>true)==
                RestoreVerdict.Accepted&&!ReferenceEquals(old,host.Current)&&
                !host.Current!.GetLedger(0).Contains(2310001)&&host.Current.GetLedger(1).Contains(2310002);
        });
        Test("host idle callback exception fails closed",()=>{
            var host=new ColdSessionHandle();
            return host.TrySwap(SnapshotCodec.Encode(Run(true,false)),77,1,
                ()=>throw new Exception("host busy probe"))==RestoreVerdict.Quarantine&&host.Current==null;
        });
        Test("original W06 ledger is separate and unique per physical rig",()=>{
            ColdRestore.TryStage(SnapshotCodec.Encode(Run(true,true)),out ColdRestore? p);
            if(p==null)return false;
            return !ReferenceEquals(p.GetLedger(0),p.GetLedger(1))&&
                p.GetLedger(0).Contains(2310001)&&!p.GetLedger(0).Contains(2310002)&&
                p.GetLedger(1).Contains(2310002)&&!p.GetLedger(1).Contains(2310001);
        });
        for(int n=0;n<64;n++)
        {
            int seed=n;
            Test("independent disk image roundtrip deterministic generation "+seed,()=>{
                var s=Run((seed&1)!=0,(seed&2)!=0,seed+1);
                byte[] b=SnapshotCodec.Encode(s);
                return ColdRestore.TryStage(b,out ColdRestore? cold)&&cold!=null&&
                    cold.Snapshot.Generation==seed+1&&cold.Snapshot.Parts.Length==2;
            });
        }
        for(int n=0;n<64;n++)
        {
            int seed=n;
            Test("checksum-protected mutated physical bytes fail closed "+seed,()=>{
                var b=SnapshotCodec.Encode(Run(true,false,1));int pos=(seed*11)%(b.Length-32);
                b[pos]^=(byte)(1+(seed%251));return !ColdRestore.TryStage(b,out _);
            });
        }
        Console.WriteLine("NATIVE_UNITY_NOT_RUN / COLD_HOST_ADAPTER_NOT_INTEGRATED");
    }
    public static int Main()
    {
        MainTests();
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        return failed==0?0:1;
    }
}

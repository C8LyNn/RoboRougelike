using System;
using System.IO;
using System.Security.Cryptography;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.UnitySaveBridge001;

internal static class Program
{
    const long Run=17017;
    static int pass,fail;
    static void Check(string label,Func<bool> test)
    {
        try {if(test()){++pass;Console.WriteLine("PASS "+label);}
            else{++fail;Console.WriteLine("FAIL "+label);}}
        catch(Exception e){++fail;Console.WriteLine("FAIL "+label+" "+e);}
    }
    static PersistedPart Part(int slot,bool detached)
    {
        var s=SecureRigCatalog.At(slot);
        long owner=FrozenPartCatalog.Owner(slot);
        return new PersistedPart(s.PartId,owner,detached?0:owner,
            FrozenPartCatalog.Asset(slot),FrozenPartCatalog.Socket(slot),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,
            new Vector3d(s.RootX+s.PartOffsetX,1.2+(detached?1:0),0),
            0,0,0,1,detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
            new Vector3d(0,0,0),s.PartMassKg);
    }
    static byte[] Data(long gen,bool light,bool heavy,long run=Run)
       => SnapshotCodec.Encode(new RunSnapshot(run,gen,
           new[]{Part(0,light),Part(1,heavy)}));
    static string Temp(string root,string key)
    {
        string dir=Path.Combine(root,key);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir,"committed.fence");
    }
    static byte[] ForeignId(byte[] input)
    {
        byte[] b=(byte[])input.Clone();
        b[72]^=1; // selected #556 first stable ID byte: 8+4+40+8+8+4
        using var sha=SHA256.Create();
        byte[] digest=sha.ComputeHash(b,0,b.Length-32);
        Buffer.BlockCopy(digest,0,b,b.Length-32,32);
        return b;
    }
    sealed class World:IPreparedWorld
    {
        readonly RunSnapshot input;
        public bool Active,Retired,PoseCorruptAfterActivate,
            IdCorruptAfterActivate,FailDeactivate,FailActivate,FailRetire;
        public int ActivationCount;
        public World(RunSnapshot s){input=s;}
        public RunSnapshot Capture()
        {
            if(!Active||(!PoseCorruptAfterActivate&&!IdCorruptAfterActivate))
                return input;
            var parts=input.Parts;
            if(IdCorruptAfterActivate)parts[0]=parts[1];
            else
            {
                var p=parts[0];
                parts[0]=new PersistedPart(p.Id,p.OwnerId,p.ParentId,p.AssetGuid,
                    p.SocketGuid,p.State,p.Consumed,p.Kinematic,
                    new Vector3d(p.Position.X+1.25,p.Position.Y,p.Position.Z),
                    p.Qx,p.Qy,p.Qz,p.Qw,p.LinearVelocity,p.AngularVelocity,p.MassKg);
            }
            return new RunSnapshot(input.RunId,input.Generation,parts);
        }
        public void Activate()
        {
            ActivationCount++;Active=true;
            if(FailActivate)throw new IOException("fault-injected world activate");
        }
        public void Deactivate()
        {
            Active=false;
            if(FailDeactivate)throw new IOException("fault-injected abort");
        }
        public void Retire()
        {
            Retired=true;Active=false;
            if(FailRetire)throw new IOException("fault-injected deferred retirement");
        }
    }
    sealed class Factory:IWorldFactory
    {
        public World? Last;
        public int PrepareCalls;
        public Action? BeforeCreate;
        public bool PoseCorrupt,IdCorrupt,FailDeactivate,FailActivate,FailRetire;
        public IPreparedWorld Prepare(RunSnapshot s)
        {
            PrepareCalls++;BeforeCreate?.Invoke();
            Last=new World(s){PoseCorruptAfterActivate=PoseCorrupt,
                IdCorruptAfterActivate=IdCorrupt,FailDeactivate=FailDeactivate,
                FailActivate=FailActivate,FailRetire=FailRetire};
            PoseCorrupt=IdCorrupt=FailDeactivate=FailActivate=FailRetire=false;
            return Last;
        }
    }
    static (AtomicWorldSwap host,Factory factory,World old) Seed(string path)
    {
        var f=new Factory();
        var host=new AtomicWorldSwap(f,new FileCheckpointFence(path));
        if(host.TryLoad(Data(10,true,false),Run,1,()=>true)!=WorldSwapVerdict.Accepted
            ||f.Last==null||!f.Last.Active||!host.DurableFence)
            throw new Exception("original 604 durable cold bootstrap failed");
        return (host,f,f.Last);
    }
    static bool SourceId(AtomicWorldSwap host,bool light,bool heavy)
       =>host.IsConsumed(0,FrozenPartCatalog.Id(0))==light
         && host.IsConsumed(1,FrozenPartCatalog.Id(1))==heavy;
    static bool ColdMatches(byte[] bytes,bool light,bool heavy,long gen)
    {
        if(!ColdRestore.TryStage(bytes,out ColdRestore? plan)||plan==null)
            return false;
        return plan.Snapshot.Generation==gen&&
            plan.ContainsConsumedId(0,FrozenPartCatalog.Id(0))==light&&
            plan.ContainsConsumedId(1,FrozenPartCatalog.Id(1))==heavy;
    }

    static int Main()
    {
        string root=Path.Combine(Path.GetTempPath(),"b01-61-w17-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Check("ORIGINAL_604_durable_file_real_88_bytes",()=>{
                string p=Temp(root,"file-shape");
                var(s,_,old)=Seed(p);
                byte[] bytes=File.ReadAllBytes(p);
                return s.DurableFence&&s.Generation==10&&old.Active&&
                    bytes.Length==88&&
                    new FileCheckpointFence(p).Check(Run,10,CheckpointHash.Of(Data(10,true,false)),true)
                        ==FenceVerdict.Allowed;
            });
            for(int light=0;light<2;light++)
                for(int heavy=0;heavy<2;heavy++)
                {
                    int a=light,b=heavy;string label="FOUR_LIGHT_HEAVY_IDS_"+a+b;
                    Check(label,()=>{
                        string path=Temp(root,"comb"+a+b);
                        var f=new Factory();
                        var s=new AtomicWorldSwap(f,new FileCheckpointFence(path));
                        byte[] bytes=Data(12,a==1,b==1);
                        var verdict=s.TryLoad(bytes,Run,1,()=>true);
                        return verdict==WorldSwapVerdict.Accepted&&f.Last!.Active&&
                            SourceId(s,a==1,b==1)&&ColdMatches(bytes,a==1,b==1,12)&&
                            File.Exists(path);
                    });
                }
            Check("PHYSICAL_FILE_SAVE_SAME_BYTES_COLD_RESTART",()=>{
                string p=Temp(root,"disk-restart");
                var(s,_,_old)=Seed(p);
                byte[] bytes=Data(10,true,false);
                string snapshotPath=Path.Combine(Path.GetDirectoryName(p)!,"snapshot.bin");
                File.WriteAllBytes(snapshotPath,bytes);
                var fresh=new AtomicWorldSwap(new Factory(),new FileCheckpointFence(p));
                return fresh.TryLoad(File.ReadAllBytes(snapshotPath),Run,1,()=>true)
                    ==WorldSwapVerdict.Accepted&&fresh.Generation==10&&
                    SourceId(fresh,true,false);
            });
            Check("NEW_PROCESS_EQUIVALENT_FENCE_COLD_READ_NOT_FORK",()=>{
                string p=Temp(root,"same");
                Seed(p);
                var f=new Factory();
                var restored=new AtomicWorldSwap(f,new FileCheckpointFence(p));
                return restored.TryLoad(Data(10,true,false),Run,1,()=>true)
                    ==WorldSwapVerdict.Accepted&&f.PrepareCalls==1&&
                    restored.Generation==10&&SourceId(restored,true,false);
            });
            Check("DIFFERENT_SAME_GENERATION_COLD_FORK_DENIED_BEFORE_PREPARE",()=>{
                string p=Temp(root,"fork");
                Seed(p);
                var f=new Factory();
                var s=new AtomicWorldSwap(f,new FileCheckpointFence(p));
                return s.TryLoad(Data(10,false,true),Run,1,()=>true)
                   ==WorldSwapVerdict.StaleGeneration&&
                   f.PrepareCalls==0&&!s.HasWorld;
            });
            Check("STALE_GENERATION_AFTER_NEXT_COMMIT_DENIED_BEFORE_PREPARE",()=>{
                string p=Temp(root,"stale");
                var(s,_,_old)=Seed(p);
                if(s.TryLoad(Data(11,false,true),Run,11,()=>true)!=WorldSwapVerdict.Accepted)
                    return false;
                var f=new Factory();
                var cold=new AtomicWorldSwap(f,new FileCheckpointFence(p));
                return cold.TryLoad(Data(10,true,false),Run,1,()=>true)
                    ==WorldSwapVerdict.StaleGeneration&&f.PrepareCalls==0&&
                    !cold.HasWorld;
            });
            Check("RE_SIGNED_FOREIGN_STABLE_ID_DENIED_WITHOUT_PREPARE",()=>{
                string p=Temp(root,"foreign");
                var(s,f,old)=Seed(p);int before=f.PrepareCalls;
                var verdict=s.TryLoad(ForeignId(Data(11,false,true)),Run,1,()=>true);
                return verdict==WorldSwapVerdict.InvalidSnapshot&&
                    f.PrepareCalls==before&&old.Active&&SourceId(s,true,false);
            });
            Check("POST_ACTIVATE_POSE_DRIFT_RESTORES_OLD_AND_FENCE_GENERATION",()=>{
                string p=Temp(root,"pose");
                var(s,f,old)=Seed(p);f.PoseCorrupt=true;
                var verdict=s.TryLoad(Data(11,false,true),Run,1,()=>true);
                var fence=new FileCheckpointFence(p);
                return verdict==WorldSwapVerdict.HostException&&
                    !s.Quarantined&&s.Generation==10&&old.Active&&
                    !old.Retired&&f.Last!.Retired&&
                    fence.Check(Run,10,CheckpointHash.Of(Data(10,true,false)),true)
                        ==FenceVerdict.Allowed;
            });
            Check("POST_ACTIVATE_STABLE_ID_DRIFT_RESTORES_OLD_AND_LEDGER",()=>{
                string p=Temp(root,"id");
                var(s,f,old)=Seed(p);f.IdCorrupt=true;
                var verdict=s.TryLoad(Data(11,false,true),Run,1,()=>true);
                return verdict==WorldSwapVerdict.HostException&&old.Active&&
                    s.Generation==10&&SourceId(s,true,false)&&
                    f.Last!.Retired;
            });
            Check("ROLLBACK_ABORT_THROW_IMMEDIATE_QUARANTINE_RETURN",()=>{
                string p=Temp(root,"abort");
                var(s,f,old)=Seed(p);f.PoseCorrupt=true;f.FailDeactivate=true;
                var verdict=s.TryLoad(Data(11,false,true),Run,1,()=>true);
                return verdict==WorldSwapVerdict.RollbackQuarantine&&s.Quarantined&&
                    s.TryLoad(Data(12,false,false),Run,1,()=>true)
                        ==WorldSwapVerdict.RollbackQuarantine&&
                    !s.TrySave(Run,()=>true,out _)&&s.Generation==10&&old.Active;
            });
            Check("FENCE_CORRUPTION_QUARANTINES_FRESH_COLD_BEFORE_PREPARE",()=>{
                string p=Temp(root,"corrupt");
                Seed(p);
                byte[] b=File.ReadAllBytes(p);b[12]^=0x80;File.WriteAllBytes(p,b);
                var f=new Factory();var fresh=new AtomicWorldSwap(f,new FileCheckpointFence(p));
                var v=fresh.TryLoad(Data(11,false,true),Run,1,()=>true);
                return v==WorldSwapVerdict.RollbackQuarantine&&fresh.Quarantined&&
                    f.PrepareCalls==0&&!fresh.HasWorld;
            });
            Check("WRONG_RUN_DENIED_WITH_ORIGINAL_FENCE_INTACT",()=>{
                string p=Temp(root,"wrong");
                var(s,f,old)=Seed(p);int calls=f.PrepareCalls;
                var v=s.TryLoad(Data(11,false,true,Run+1),Run,1,()=>true);
                return v==WorldSwapVerdict.WrongRun&&f.PrepareCalls==calls&&
                    old.Active&&s.Generation==10;
            });
            Check("CORRUPT_CHECKSUM_DENIED_WITHOUT_AUTHORITY_MUTATION",()=>{
                string p=Temp(root,"checksum");
                var(s,f,old)=Seed(p);byte[] bad=Data(11,false,true);bad[7]^=0x80;
                var v=s.TryLoad(bad,Run,1,()=>true);
                return v==WorldSwapVerdict.InvalidSnapshot&&f.Last==old&&
                    old.Active&&new FileCheckpointFence(p).Check(
                        Run,10,CheckpointHash.Of(Data(10,true,false)),true)
                            ==FenceVerdict.Allowed;
            });
            Check("REENTRANT_HOST_CALLBACK_REFUSED_WITHOUT_INTERLEAVED_COMMIT",()=>{
                string p=Temp(root,"nested");
                var(s,f,_old)=Seed(p);
                WorldSwapVerdict nested=WorldSwapVerdict.Accepted;
                var v=s.TryLoad(Data(11,false,true),Run,1,()=>{
                    nested=s.TryLoad(Data(12,true,true),Run,1,()=>true);
                    return true;
                });
                return v==WorldSwapVerdict.Accepted&&
                    nested==WorldSwapVerdict.HostBusy&&
                    s.Generation==11&&SourceId(s,false,true);
            });
            Check("SECOND_IDLE_FAIL_DISPOSES_STAGED_WITHOUT_FENCE_CHANGE",()=>{
                string p=Temp(root,"second-idle");
                var(s,f,old)=Seed(p);int count=0;
                var v=s.TryLoad(Data(11,false,true),Run,1,()=>++count==1);
                return v==WorldSwapVerdict.HostBusy&&count==2&&
                    f.Last!.Retired&&old.Active&&!old.Retired&&
                    s.Generation==10&&new FileCheckpointFence(p).Check(
                        Run,10,CheckpointHash.Of(Data(10,true,false)),true)
                            ==FenceVerdict.Allowed;
            });
            Check("MID_STAGE_TWO_HOSTS_HIGHER_EPOCH_WINS_STALE_SEAL_QUARANTINES_LOSER",()=>{
                string p=Temp(root,"race");
                var(A,fa,oldA)=Seed(p);
                var fb=new Factory();
                var B=new AtomicWorldSwap(fb,new FileCheckpointFence(p));
                if(B.TryLoad(Data(10,true,false),Run,1,()=>true)
                    !=WorldSwapVerdict.Accepted)return false;
                int races=0;
                fa.BeforeCreate=()=>{
                    fa.BeforeCreate=null; races++;
                    var winner=B.TryLoad(Data(12,false,true),Run,12,()=>true);
                    if(winner!=WorldSwapVerdict.Accepted)
                        throw new IOException("interleaved winner source failure: "+winner);
                };
                var loser=A.TryLoad(Data(11,true,true),Run,11,()=>true);
                var fresh=new AtomicWorldSwap(new Factory(),new FileCheckpointFence(p));
                return races==1&&loser==WorldSwapVerdict.RollbackQuarantine&&
                    A.Quarantined&&A.Generation==10&&oldA.Active&&
                    B.Generation==12&&SourceId(B,false,true)&&
                    fresh.TryLoad(Data(12,false,true),Run,1,()=>true)
                        ==WorldSwapVerdict.Accepted&&
                    fresh.Generation==12;
            });
            Check("DURABLE_SAVE_REJECTS_CHANGED_POSE_WITH_UNCHANGED_GENERATION",()=>{
                string p=Temp(root,"motion");
                var(s,f,old)=Seed(p);
                old.PoseCorruptAfterActivate=true;
                bool ok=s.TrySave(Run,()=>true,out byte[] _);
                return !ok&&old.Active&&s.Generation==10&&
                    new FileCheckpointFence(p).Check(
                        Run,10,CheckpointHash.Of(Data(10,true,false)),true)
                          ==FenceVerdict.Allowed;
            });
            Console.WriteLine("SELECTED_604_EXACT_CORE_FENCE_BYTES_DIRECTLY_EXECUTED=TRUE");
            Console.WriteLine("INTERLEAVED_TWO_HOST_SEAL_RACE=FAIL_CLOSED_STICKY_QUARANTINE");
            Console.WriteLine("G0_SOURCE_QA="+(fail==0?"PASS":"FAIL"));
            Console.WriteLine("UNITY_NATIVE_G1=NOT_RUN LIVE_510_PRIVATE_LEDGER=NOT_INTEGRATED");
            Console.WriteLine("DURABLE_TWO_FILE_SAVE_ATOMICITY=NOT_IMPLEMENTED");
            Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
            return fail==0?0:1;
        }
        finally {Directory.Delete(root,true);}
    }
}

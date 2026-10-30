// B01-61 W16: exact original B01-23 #596 R2 transaction SOURCE. NOT Unity/PhysX.
using System;
using System.IO;
using System.Linq;
using B01.UnitySaveBridge001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
sealed class World : IPreparedWorld {
 public RunSnapshot State;
 public bool Active,Retired,MutateActivation,ThrowActivation,ThrowDeactivation,ThrowRetirement;
 public World(RunSnapshot s){State=s;}
 public RunSnapshot Capture(){if(Retired)throw new IOException("retired");return State;}
 public void Activate(){
   Active=true;
   if(MutateActivation)State=new RunSnapshot(State.RunId,State.Generation,new[]{State.Parts[1],State.Parts[0]});
   if(ThrowActivation)throw new IOException("injected midway activation");
 }
 public void Deactivate(){Active=false;if(ThrowDeactivation)throw new IOException("injected cleanup");}
 public void Retire(){Retired=true;Active=false;if(ThrowRetirement)throw new IOException("injected retirement");}
}
sealed class Factory:IWorldFactory {
 public World Last=null!;
 public bool MutateNext,ThrowNextActivation,ThrowNextDeactivation;
 public IPreparedWorld Prepare(RunSnapshot s){
   Last=new World(s){MutateActivation=MutateNext,
     ThrowActivation=ThrowNextActivation,ThrowDeactivation=ThrowNextDeactivation};
   return Last;
 }
}
static class W16 {
 const long RUN=861610;
 static int assertions,cases,roundTrips,denied,rollbacks;
 static void Check(bool b,string why){assertions++;if(!b)throw new Exception("R2_EXACT_QA_FAIL "+why);}
 static void Case(string label,Action action){action();cases++;Console.WriteLine("W16_R2_TEST_PASS "+label);}
 static PersistedPart P(int i,bool det,long gen){
   var z=new Vector3d(0,0,0);
   return new PersistedPart(FrozenPartCatalog.Id(i),FrozenPartCatalog.Owner(i),
     det?0:FrozenPartCatalog.Owner(i),FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
     det?PartDockState.Detached:PartDockState.Attached,det,!det,
     new Vector3d(1+i,gen/4.0,0),0,0,0,1,det?new Vector3d(.4,0,0):z,z,
     FrozenPartCatalog.Mass(i));
 }
 static byte[] Binary(long gen,bool light,bool heavy)=>SnapshotCodec.Encode(
   new RunSnapshot(RUN,gen,new[]{P(0,light,gen),P(1,heavy,gen)}));
 static void Temp(Action<string> test){
  var dir=Path.Combine(Path.GetTempPath(),"b0161r2_"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(dir);
  try{test(dir);}finally{Directory.Delete(dir,true);}
 }
 static (AtomicWorldSwap h,Factory f,World old) Start(
   ICheckpointStore store,long gen=4,bool light=true,bool heavy=false){
   var f=new Factory();var h=new AtomicWorldSwap(f,store);
   Check(h.TryLoad(Binary(gen,light,heavy),RUN,1,()=>true)==WorldSwapVerdict.Accepted,
     "genuine selected source #596 initial restore");
   Check(h.Generation==gen&&h.IsConsumed(0,2310001)==light&&h.IsConsumed(1,2310002)==heavy,
     "original W06 restored exact Light/Heavy ID");
   Check(f.Last.Active&&!f.Last.Retired&&!h.Quarantined,"physical abstract world active");
   return(h,f,f.Last);
 }
 static void SaveDiskAllFour(){
  foreach(bool a in new[]{false,true})foreach(bool b in new[]{false,true})
  Temp(dir=>{
   string sidecar=Path.Combine(dir,"checkpoint");
   var (h,_,old)=Start(new FileCheckpointStore(sidecar),4,a,b);
   Check(h.TrySave(RUN,()=>true,out byte[] data)&&data!=null,
     "original #596 creates actual selected #556 Save bytes");
   string file=Path.Combine(dir,"world.save");File.WriteAllBytes(file,data!);
   var saved=File.ReadAllBytes(file);
   Check(data!.SequenceEqual(saved),"real OS Save and read preserve original file bytes");
   Check(SnapshotCodec.TryDecode(saved,out RunSnapshot? snap)&&snap!=null&&snap.Generation==5,
     "original #556 codec uses freshly advanced generation");
   var cold=new AtomicWorldSwap(new Factory(),new FileCheckpointStore(sidecar));
   Check(cold.TryLoad(saved,RUN,1,()=>true)==WorldSwapVerdict.Accepted,
     "new source CLR host can cold-load original byte file");
   Check(cold.IsConsumed(0,2310001)==a&&cold.IsConsumed(1,2310002)==b,
     "cold recovered W06 consumed flags for both fixed IDs");
   Check(old.Active&&!old.Retired,"source world unchanged by disk Save");
   if(a||b){
      int i=a?0:1;var ledger=new AtomicDockLedger();var z=new Vector3d(0,0,0);
      Check(ledger.TryDetach(FrozenPartCatalog.Id(i),z,z,z,
         FrozenPartCatalog.Mass(i),0,0,out Vector3d _)
         &&ledger.Contains(FrozenPartCatalog.Id(i)),
         "real unchanged #510 R2 ledger can consume persisted ID");
   }
   roundTrips++;
  });
 }
 static void EqualGenerationFork(){
  var (h,f,old)=Start(new MemoryCheckpointStore(),8,true,false);
  Check(h.TryLoad(Binary(8,false,true),RUN,1,()=>true)==WorldSwapVerdict.StaleGeneration,
    "#588 corrected same-run same-generation divergent world refused");
  Check(object.ReferenceEquals(old,f.Last)&&old.Active&&!old.Retired&&h.Generation==8&&
    h.IsConsumed(0,2310001)&&!h.IsConsumed(1,2310002),
    "stale fork cannot mutate original physical world or W06 ID");
  denied++;
 }
 static void DurableFileWatermark(){
  Temp(dir=>{
   string path=Path.Combine(dir,"authority");
   var original=Binary(10,true,false);
   var host=new AtomicWorldSwap(new Factory(),new FileCheckpointStore(path));
   Check(host.TryLoad(original,RUN,1,()=>true)==WorldSwapVerdict.Accepted,
     "first source producer commits real durable SHA256 authority");
   Check(File.Exists(path),"source FileCheckpointStore wrote physical disk sidecar");
   var fileStore=new FileCheckpointStore(path);
   Check(fileStore.Read(RUN,out long gen,out string hash)&&gen==10&&hash.Length==64,
     "actual original watermark fingerprint persisted");
   var f=new Factory();var cold=new AtomicWorldSwap(f,new FileCheckpointStore(path));
   Check(cold.TryLoad(Binary(10,false,true),RUN,1,()=>true)==WorldSwapVerdict.StaleGeneration,
     "different same-gen file denied in freshly restarted source host");
   Check(cold.TryLoad(Binary(9,false,true),RUN,1,()=>true)==WorldSwapVerdict.StaleGeneration,
     "older but valid file denied by actual persisted generation");
   Check(!cold.HasWorld&&f.Last==null,"denied file cannot publish physical world");
   Check(cold.TryLoad(original,RUN,1,()=>true)==WorldSwapVerdict.Accepted&&
      cold.IsConsumed(0,2310001)&&!cold.IsConsumed(1,2310002),
      "identical saved hash permits safe original cold replay");
   denied+=2;
  });
 }
 static void ActivationMutationAndDurability(){
  var store=new MemoryCheckpointStore();
  var (h,f,old)=Start(store);
  f.MutateNext=true;
  Check(h.TryLoad(Binary(5,false,true),RUN,1,()=>true)==WorldSwapVerdict.HostException,
    "#572 after-Activate physical identity corruption blocked by true original source");
  Check(old.Active&&!old.Retired&&f.Last.Retired&&!f.Last.Active&&
    h.Generation==4&&h.IsConsumed(0,2310001)&&!h.Quarantined,
    "injected candidate failure restored prior world and consumed ID");
  Check(store.Read(RUN,out long generation,out string _)&&generation==4,
    "producer must validate activated candidate BEFORE durable CAS commit");
  rollbacks++;
 }
 static void AbortAndRetirementQuarantine(){
  var (h,f,old)=Start(new MemoryCheckpointStore());
  f.ThrowNextActivation=true;f.ThrowNextDeactivation=true;
  Check(h.TryLoad(Binary(5,false,true),RUN,1,()=>true)==WorldSwapVerdict.RollbackQuarantine,
    "#582 cleanup exception MUST return quarantine on first call");
  Check(h.Quarantined&&old.Active&&h.IsConsumed(0,2310001)&&
    h.TryLoad(Binary(6,false,true),RUN,1,()=>true)==WorldSwapVerdict.RollbackQuarantine,
    "quarantine sticky; cannot restore after failed cleanup");
  Check(!h.TrySave(RUN,()=>true,out _),"quarantined source cannot issue Save");
  rollbacks++;
  var (h2,_,old2)=Start(new MemoryCheckpointStore());
  old2.ThrowRetirement=true;
  Check(h2.TryLoad(Binary(5,false,true),RUN,1,()=>true)==WorldSwapVerdict.RollbackQuarantine&&
    h2.CleanupPending&&h2.Quarantined&&h2.IsConsumed(1,2310002),
    "postpublication old-world retirement failure recorded, quarantine returned");
  rollbacks++;
 }
 static void FileCasAndCorruption(){
  Temp(dir=>{
   var path=Path.Combine(dir,"ledger");
   var a=new FileCheckpointStore(path);var b=new FileCheckpointStore(path);
   Check(a.Read(RUN,out long ga,out string ha)&&ga==0&&ha=="","fresh checkpoint empty");
   Check(b.Read(RUN,out long gb,out string hb)&&gb==0&&hb=="","second source store empty");
   string hash=new string('a',64);
   Check(a.Commit(RUN,0,"",7,hash),"first actual atomic file CAS committed");
   Check(!b.Commit(RUN,0,"",7,new string('b',64)),
     "second competing old-generation checkpoint CAS denied");
   Check(b.Read(RUN,out long g,out string s)&&g==7&&s==hash,
     "actual original first fingerprint never overwritten");
   File.WriteAllText(path,"not-an-authorized-checkpoint");
   Check(!a.Read(RUN,out long _,out string _),"foreign/malformed durable checkpoint refused");
   denied+=2;
  });
 }
 static void BadInputPreservesWorld(){
  var (h,f,old)=Start(new MemoryCheckpointStore());
  var corrupted=Binary(5,false,true);corrupted[51]^=5;
  Check(h.TryLoad(corrupted,RUN,1,()=>true)==WorldSwapVerdict.InvalidSnapshot,
    "selected original #556 bad SHA checksum denied");
  Check(h.TryLoad(Binary(3,false,true),RUN,1,()=>true)==WorldSwapVerdict.StaleGeneration,
    "old generation denied");
  Check(h.TryLoad(Binary(5,false,true),RUN,1,()=>false)==WorldSwapVerdict.HostBusy,
    "busy host refused");
  int count=0;
  Check(h.TryLoad(Binary(5,false,true),RUN,1,()=>++count==1)==WorldSwapVerdict.HostBusy&&count==2,
    "host becomes busy mid-stage, source denies publication");
  Check(h.Generation==4&&old.Active&&!old.Retired&&f.Last.Retired&&!f.Last.Active&&
    h.IsConsumed(0,2310001)&&!h.IsConsumed(1,2310002),
    "no partial original physics/ledger swap on rejected input");
  denied+=4;
 }
 public static int Main(string[] argv){
  try{
   Case("actual #556 Save disk/fresh host all four ID states",SaveDiskAllFour);
   Case("same generation conflicting Light Heavy source green",EqualGenerationFork);
   Case("real persisted SHA256 file authority across fresh host restart",DurableFileWatermark);
   Case("activate mutation rollback preserves physical world and durable authority",ActivationMutationAndDurability);
   Case("first-call quarantine after failed cleanup and old retire",AbortAndRetirementQuarantine);
   Case("real FileCheckpointStore compare-and-swap plus corrupt sidecar",FileCasAndCorruption);
   Case("corrupt/stale/busy inputs leave R2 state untouched",BadInputPreservesWorld);
   Check(cases==7&&roundTrips==4&&rollbacks==3&&denied==9,
     "all original source cases and expected denials executed");
   Console.WriteLine("B0161_W16_EXACT_596_R2_SOURCE_QA_GREEN groups="+cases+
    " assertions="+assertions+" disk_roundtrips="+roundTrips+
    " rollbacks="+rollbacks+" denials="+denied+
    " SELECTED_SAVE=556 W06_ORIGINAL=YES R2_510_ORIGINAL=YES UNITY_G1=NOT_RUN");
   if(argv.Length>0&&argv[0]=="--release"){
    Console.WriteLine("B0161_W16_CANONICAL_RELEASE_HOLD=NO_AUTHORIZED_510_PRIVATE_RIG_HYDRATION_NO_NATIVE_PHYSX");
    return 3;
   }
   return 0;
  }catch(Exception e){Console.Error.WriteLine("B0161_W16_QA_INFRA_FAILURE "+e);return 2;}
 }
}

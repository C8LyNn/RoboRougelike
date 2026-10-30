#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using B01.UnitySaveBridge001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;

static class Program {
 static int assertions, groups, denied,accepted;
 const long Run=61721;
 static void A(bool ok,string msg){assertions++;if(!ok)throw new Exception("R5_W21_FAILED "+msg);Console.WriteLine("PASS "+msg);}
 static void G(string name,Action test){test();groups++;Console.WriteLine("R5_SOURCE_GROUP_PASS="+name);}
 static string Root(){string r=Path.Combine(Path.GetTempPath(),"b0161-w21-r5-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(r);return r;}
 static int Child(string path){try{using var x=new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);return 0;}catch(IOException){return 3;}}
 static int External(string path){
  var p=new ProcessStartInfo("dotnet"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
  p.ArgumentList.Add(typeof(Program).Assembly.Location);p.ArgumentList.Add("child");p.ArgumentList.Add(path);
  using var c=Process.Start(p)??throw new Exception("cannot launch CLR child");
  if(!c.WaitForExit(20000)){try{c.Kill(true);}catch{}throw new Exception("child timed out");}
  string stdout=c.StandardOutput.ReadToEnd(),stderr=c.StandardError.ReadToEnd();
  if(c.ExitCode!=0&&c.ExitCode!=3)throw new Exception("bad child "+stdout+stderr);
  if(c.ExitCode==3)denied++;else accepted++;
  return c.ExitCode;
 }
 static void DeferredDestroy() {
  string root=Root(),slot=Path.Combine(root,"slot"),file=slot+".owner";bool destroyed=false;
  var lease=new FileStream(file,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
  OwnerWorldLeaseRetirement.Hold(slot,lease,()=>destroyed,true);
  A(!OwnerWorldLeaseRetirement.ReconcileBeforeNewAcquire(slot),"original R5 pending physical roots deny handoff");
  for(int i=0;i<4;i++)A(External(file)==3,"real other CLR process denied while roots alive "+i);
  A(!OwnerWorldLeaseRetirement.ReconcileBeforeNewAcquire(Path.Combine(root,".","slot")),
    "canonical equivalent checkpoint path cannot bypass R5 pending owner");
  destroyed=true;A(OwnerWorldLeaseRetirement.ReconcileBeforeNewAcquire(slot),"actual destruction predicate releases R5 lease");
  A(External(file)==0,"real other CLR process acquires after roots verified destroyed");
  A(OwnerWorldLeaseRetirement.ReconcileBeforeNewAcquire(slot),"second harmless reconcile is idempotent");
  Directory.Delete(root,true);
  Console.WriteLine("W21_R5_690_CROSSPROCESS_DEFERRED_DESTROY_GREEN");
 }
 static void CleanupRefusal() {
  string root=Root(),slot=Path.Combine(root,"cleanup-failed"),file=slot+".owner";
  var lease=new FileStream(file,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
  OwnerWorldLeaseRetirement.Hold(slot,lease,()=>true,false);
  A(!OwnerWorldLeaseRetirement.ReconcileBeforeNewAcquire(slot),"failed R5 cleanup refuses release even if predicate says true");
  A(External(file)==3,"real external CLR process denied after failed cleanup");
  A(!OwnerWorldLeaseRetirement.ReconcileBeforeNewAcquire(slot),"failed cleanup quarantine cannot be reset by retry");
  Console.WriteLine("W21_R5_690_FAILED_CLEANUP_EXTERNAL_LEASE_GREEN");
  // Intentionally retain unsafe lease for process lifetime; no destructive cleanup.
 }
 static void UnknownPredicate() {
  string root=Root(),slot=Path.Combine(root,"probe-throws"),file=slot+".owner";
  var lease=new FileStream(file,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
  OwnerWorldLeaseRetirement.Hold(slot,lease,()=>throw new IOException("Unity children unknown"),true);
  A(!OwnerWorldLeaseRetirement.ReconcileBeforeNewAcquire(slot),"R5 original source fails closed on predicate exception");
  A(External(file)==3,"real external process denied when Unity physical destruction cannot be proven");
  Console.WriteLine("W21_R5_690_UNKNOWN_CHILDREN_EXTERNAL_LEASE_GREEN");
 }
 static void DuplicateRegistry() {
  string root=Root(),slot=Path.Combine(root,"duplicate"),file=slot+".owner";bool gone=false;
  var lease=new FileStream(file,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
  OwnerWorldLeaseRetirement.Hold(slot,lease,()=>gone,true);
  bool reject=false;
  try{OwnerWorldLeaseRetirement.Hold(slot,lease,()=>true,true);}catch(InvalidOperationException){reject=true;}
  A(reject,"R5 owner lease registry rejects duplicate pending slot");
  A(!OwnerWorldLeaseRetirement.ReconcileBeforeNewAcquire(slot),"duplicate cannot prematurely release first lease");
  gone=true;A(OwnerWorldLeaseRetirement.ReconcileBeforeNewAcquire(slot),"first owner legitimately reclaims after physical gone");
  A(External(file)==0,"real next OS process can acquire only after first owner release");
  Directory.Delete(root,true);
 }
 sealed class World:IPreparedWorld{
  readonly RunSnapshot data;
  public bool Active,Retired,FailRetire;
  public World(RunSnapshot s){data=s;}
  public RunSnapshot Capture()=>data;
  public void Activate(){Active=true;}
  public void Deactivate(){Active=false;}
  public void Retire(){if(FailRetire)throw new IOException("physical retire failed");Retired=true;Active=false;}
 }
 sealed class Factory:IWorldFactory {
  public World? Last;
  public IPreparedWorld Prepare(RunSnapshot s)=>Last=new World(s);
 }
 static byte[] Snapshot(long gen,bool light,bool heavy) {
  PersistedPart[] parts=new PersistedPart[2];
  var z=new Vector3d(0,0,0);
  for(int i=0;i<2;i++){
   bool detach=i==0?light:heavy;
   parts[i]=new PersistedPart(FrozenPartCatalog.Id(i),FrozenPartCatalog.Owner(i),
    detach?0:FrozenPartCatalog.Owner(i),FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
    detach?PartDockState.Detached:PartDockState.Attached,detach,!detach,
    new Vector3d(i+1,1.5,0),0,0,0,1,detach?new Vector3d(.25,0,0):z,z,FrozenPartCatalog.Mass(i));
  }
  return SnapshotCodec.Encode(new RunSnapshot(Run,gen,parts));
 }
 static void OriginalR5CoreRetirement() {
  string root=Root(),checkpoint=Path.Combine(root,"durable"),before="";
  var f=new Factory();var h=new AtomicWorldSwap(f,new FileCheckpointStore(checkpoint));
  A(h.TryLoad(Snapshot(10,true,false),Run,1,()=>true)==WorldSwapVerdict.Accepted,"original R5 source real frozen #556 Light10 restore");
  A(f.Last!=null&&f.Last.Active&&h.IsConsumed(0,2310001)&&!h.IsConsumed(1,2310002),
    "original frozen #510/W06 Light detached consumed ID");
  A(new FileCheckpointStore(checkpoint).Read(Run,out long g,out before)&&g==10&&before.Length==64,
    "original FileCheckpointStore durable g10 physical disk");
  World old=f.Last!;
  A(h.TryRetireForOwnerRemoval()&&h.Quarantined&&!h.HasWorld&&old.Retired&&!old.Active,
    "original R5 owner remove physically retires world and permanently quarantines");
  A(!h.TrySave(Run,()=>true,out byte[] _),"retired R5 owner cannot export stale Light save");
  A(h.TryLoad(Snapshot(11,false,true),Run,11,()=>true)==WorldSwapVerdict.RollbackQuarantine,
    "retired R5 cannot import contradictory Heavy consumed ID");
  A(new FileCheckpointStore(checkpoint).Read(Run,out g,out string digest)&&g==10&&digest==before,
    "old durable disk unchanged through R5 retirement");
  Directory.Delete(root,true);
  Console.WriteLine("W21_R5_690_REAL_556_ID_RETIRE_QUARANTINE_GREEN");
 }
 static void OriginalR5FailedRetire(){
  string root=Root(),path=Path.Combine(root,"failure");
  var f=new Factory();var h=new AtomicWorldSwap(f,new FileCheckpointStore(path));
  A(h.TryLoad(Snapshot(15,false,true),Run,1,()=>true)==WorldSwapVerdict.Accepted,
    "original R5 Heavy gen15 physical OS baseline");
  A(h.IsConsumed(1,2310002)&&!h.IsConsumed(0,2310001),"original W06 consumed Heavy ledger");
  f.Last!.FailRetire=true;
  A(!h.TryRetireForOwnerRemoval()&&h.Quarantined&&h.CleanupPending,
    "failed private physical retirement becomes sticky cleanupPending quarantine");
  A(!h.TrySave(Run,()=>true,out byte[] _)&&
    h.TryLoad(Snapshot(16,true,false),Run,16,()=>true)==WorldSwapVerdict.RollbackQuarantine,
    "failed R5 retirement cannot Save or rehydrate conflicting Light");
  A(!h.TryRetireForOwnerRemoval(),"failed R5 cleanup cannot be falsely retried to success");
  A(new FileCheckpointStore(path).Read(Run,out long gen,out string _)&&gen==15,
    "physical checkpoint unchanged when R5 cleanup fails");
  Directory.Delete(root,true);
  Console.WriteLine("W21_R5_690_FAILED_RETIRE_STICKY_QUARANTINE_GREEN");
 }
 static int Main(string[] args){
  if(args.Length==2&&args[0]=="child")return Child(args[1]);
  try {
   G("crossprocess pending deferred child destruction",DeferredDestroy);
   G("failed teardown permanent OS lease",CleanupRefusal);
   G("unknown child physical predicate throws",UnknownPredicate);
   G("duplicate registry owner refused",DuplicateRegistry);
   G("R5 original C# core frozen IDs normal owner retirement",OriginalR5CoreRetirement);
   G("R5 original C# core failed physical Retire quarantine",OriginalR5FailedRetire);
   A(groups==6&&denied==6&&accepted==2,"6 real groups and exact 8 external CLR child probes");
   Console.WriteLine("B0161_W21_R5_690_ORIGINAL_SOURCE_QA_PASS groups="+groups+
     " assertions="+assertions+" real_child_denied="+denied+" real_child_accepted="+accepted+
     " Native_Unity_G1=NOT_RUN");
   if(args.Length>0&&args[0]=="--release"){
    Console.WriteLine("W21_R5_690_FULL_GAME_RELEASE_HOLD=MANAGED_PHYSICS_SURROGATE_NO_NATIVE_G1_CANONICAL_510_UNBOUND");
    return 3;
   }
   return 0;
  }catch(Exception ex){Console.Error.WriteLine("R5_ORIGINAL_QA_FAIL "+ex);return 2;}
 }
}
#nullable enable
using System;
using System.IO;
using B01.CanonicalWorldApi001;
using B01.UnitySaveBridge001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;

static class Program {
 const long Run=617211;
 static int n,groups,orphanCleanGreen,quarantineGreen,dualOwnerRed;
 static void Check(bool x,string why){n++;if(!x)throw new Exception("B0161_691_FAILED "+why);Console.WriteLine("PASS "+why);}
 static void Group(string name,Action test){test();groups++;Console.WriteLine("W21_API691_GROUP_PASS="+name);}
 static string Root(){string x=Path.Combine(Path.GetTempPath(),"b0161-api691-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(x);return x;}
 sealed class Gate:ICanonicalMutationGate {public bool IsQuiescent()=>true;}
 sealed class Disk:ICanonicalCommitPort {
  readonly FileCheckpointStore physical;
  public Disk(string file){physical=new FileCheckpointStore(file);}
  public bool Read(long run,out long g,out string sha)=>physical.Read(run,out g,out sha);
  public bool Commit(long run,long prev,string hash,long next,string sha)=>
    physical.Commit(run,prev,hash,next,sha);
 }
 sealed class World:ICanonicalStagedWorld {
  readonly RunSnapshot saved;
  public bool Active,Retired;
  public World(RunSnapshot s){saved=s;}
  public RunSnapshot Capture()=>saved;
  public void Activate(){Active=true;}
  public void Deactivate(){Active=false;}
  public void Retire(){Retired=true;Active=false;}
 }
 enum Fault { None,ThrowAfterAlloc,ThrowAfterAllocCleanupFails }
 sealed class Factory:ICanonicalRigFactory {
  public Fault Next;
  public World? Last;
  public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore plan)
    =>new Owner(this,s,Next);
 }
 sealed class Owner:ICanonicalStageOwnership {
  readonly Factory factory;readonly RunSnapshot snap;readonly Fault fault;
  World? world;
  public Owner(Factory f,RunSnapshot s,Fault fault){factory=f;snap=s;this.fault=fault;}
  public ICanonicalStagedWorld PrepareInactive(){
   world=new World(snap);factory.Last=world;
   if(fault!=Fault.None){world.Active=true;throw new IOException("partially-created private rig throws before stage returned");}
   return world;
  }
  public bool CleanupUnpublishedAndVerifyDestroyed(){
   if(fault==Fault.ThrowAfterAllocCleanupFails)return false;
   if(world!=null){world.Deactivate();world.Retire();}
   return true;
  }
 }
 static byte[] Snapshot(long gen,bool light,bool heavy) {
  var p=new PersistedPart[2];var z=new Vector3d(0,0,0);
  for(int i=0;i<2;i++){
   bool d=i==0?light:heavy;
   p[i]=new PersistedPart(FrozenPartCatalog.Id(i),FrozenPartCatalog.Owner(i),
     d?0:FrozenPartCatalog.Owner(i),FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
     d?PartDockState.Detached:PartDockState.Attached,d,!d,
     new Vector3d(i+1,1.5,0),0,0,0,1,d?new Vector3d(.25,0,0):z,z,FrozenPartCatalog.Mass(i));
  }
  return SnapshotCodec.Encode(new RunSnapshot(Run,gen,p));
 }
 static void OwnedPartialStageCleanup() {
  string root=Root();var f=new Factory();var d=new Disk(Path.Combine(root,"authority"));
  var g=new CanonicalWorldGateway(new Gate(),f,d);
  Check(g.TryRestore(Snapshot(10,true,false),Run,1)==CanonicalGateResult.Accepted,
    "new canonical #691 real frozen #556 Light g10 Restore accepted");
  World old=f.Last!;
  Check(old.Active&&g.HasWorld&&g.Generation==10,
    "original g10 physical old world pointer and W06 ledger active");
  f.Next=Fault.ThrowAfterAlloc;
  var result=g.TryRestore(Snapshot(11,false,true),Run,11);
  World partiallyBuilt=f.Last!;
  Check(result==CanonicalGateResult.HostException&&!g.Quarantined,
    "failed PrepareInactive with verifiably successful owned cleanup is recoverable");
  Check(partiallyBuilt.Retired&&!partiallyBuilt.Active,
    "new owner-tracked API stage cleans leaked private rig before returning failure");
  Check(old.Active&&g.Generation==10,
    "old original Light owner remains after safe abort");
  Check(d.Read(Run,out long disk,out string _)&&disk==10,
    "actual durable original FileCheckpointStore unaffected by failed stage");
  f.Next=Fault.None;
  Check(g.TryRestore(Snapshot(11,false,true),Run,11)==CanonicalGateResult.Accepted,
    "safe new Heavy snapshot may retry ONLY after proven cleanup");
  Check(g.Generation==11&&f.Last!=null&&f.Last.Active&&old.Retired,
    "new Heavy world publishes and original Light properly retires");
  orphanCleanGreen++;
  Directory.Delete(root,true);
  Console.WriteLine("W21_691_685_PREPARE_PARTIAL_CLEANUP_GREEN=NO_ORPHAN");
 }
 static void FailedCleanupMustQuarantine() {
  string root=Root();var f=new Factory();var d=new Disk(Path.Combine(root,"authority"));
  var g=new CanonicalWorldGateway(new Gate(),f,d);
  Check(g.TryRestore(Snapshot(10,true,false),Run,1)==CanonicalGateResult.Accepted,
    "restore old Light baseline before failed private cleanup");
  World old=f.Last!;
  f.Next=Fault.ThrowAfterAllocCleanupFails;
  var result=g.TryRestore(Snapshot(11,false,true),Run,11);
  World leaked=f.Last!;
  Check(result==CanonicalGateResult.RollbackQuarantine&&g.Quarantined,
    "new #691 same-call sticky quarantine when tracked private-stage cleanup fails");
  Check(leaked.Active&&!leaked.Retired,
    "test deliberately retains physically orphaned world to validate quarantine gate");
  Check(old.Active&&g.Generation==10,"previous Light world preserved under quarantine");
  Check(g.TryRestore(Snapshot(12,false,true),Run,12)==CanonicalGateResult.RollbackQuarantine,
    "quarantine rejects retry while partial unretired owner exists");
  Check(!g.TryCapturePreview(out byte[] _),
    "API refuses stale Light preview after unverified private cleanup");
  Check(d.Read(Run,out long disk,out string _)&&disk==10,
    "actual disk generation not published after partial private stage failure");
  quarantineGreen++;
  Directory.Delete(root,true);
  Console.WriteLine("W21_691_685_FAILED_STAGE_CLEANUP_QUARANTINE_GREEN");
 }
 static void TwoOwnerAuthorityStillUnfencedRed() {
  string root=Root(),path=Path.Combine(root,"same-slot");var disk=new Disk(path);
  var fa=new Factory();var fb=new Factory();
  var a=new CanonicalWorldGateway(new Gate(),fa,disk);
  var b=new CanonicalWorldGateway(new Gate(),fb,new Disk(path));
  byte[] raw=Snapshot(10,true,false);
  Check(a.TryRestore(raw,Run,1)==CanonicalGateResult.Accepted,
    "owner A real original #556 Light consumed ID checkpoint accepted");
  Check(b.TryRestore(raw,Run,1)==CanonicalGateResult.Accepted,
    "second #691 owner replays same original durable g10 while A physically alive");
  Check(fa.Last!=null&&fa.Last.Active&&fb.Last!=null&&fb.Last.Active,
    "two independent active managed private rigs same canonical disk+run coexist");
  Check(a.TryCapturePreview(out byte[] first)&&b.TryCapturePreview(out byte[] second),
    "both original source gateways export source previews while physically live");
  Check(Convert.ToHexString(first)==Convert.ToHexString(second),
    "two distinct owner preview bytes from same original #556 Light/epoch");
  Check(disk.Read(Run,out long diskGen,out string _)&&diskGen==10,
    "genuine OS disk generation cannot itself exclude second live owner on replay");
  dualOwnerRed++;
  Directory.Delete(root,true);
  Console.WriteLine("W21_691_INHERITED_688_SAME_SLOT_TWO_LIVE_OWNER_SOURCE_RED=REPRODUCED");
 }
 static int Main(string[] args) {
  try {
   Group("new-original-691-owned-stage-safe-abort-and-retry",OwnedPartialStageCleanup);
   Group("new-original-691-failed-retire-cleanup-sticky-quarantine",FailedCleanupMustQuarantine);
   Group("new-original-691-inherited-688-unfenced-dual-owner",TwoOwnerAuthorityStillUnfencedRed);
   Check(groups==3&&orphanCleanGreen==1&&quarantineGreen==1&&dualOwnerRed==1,
     "all exact new #691 independent source groups executed");
   Console.WriteLine("B0161_W21_API691_EXACT_SOURCE_WITNESSES_PASS groups="+groups+
     " assertions="+n+" ORPHAN_FIX_GREEN="+orphanCleanGreen+
     " FAILED_CLEANUP_SAFE="+quarantineGreen+" UNFENCED_DUAL_OWNER_RELEASE_RED="+dualOwnerRed);
   if(args.Length>0&&args[0]=="--release"){
    Console.WriteLine("W21_CANONICAL_API691_RELEASE_HOLD=CONFIRMED_UNFENCED_MULTI_OWNER_AND_NO_PRIVATE_510_UNITY");
    return 3;
   }
   return 0;
  }catch(Exception e){Console.Error.WriteLine("B0161_W21_API691_QA_FAIL "+e);return 2;}
 }
}
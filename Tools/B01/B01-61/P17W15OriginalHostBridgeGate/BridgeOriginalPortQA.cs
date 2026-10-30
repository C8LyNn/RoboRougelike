// Independently execute byte-exact selected B01-62 #577 host transaction port, #556 Save,
// original W06 ledger and frozen #510 AtomicDockLedger. Managed host ONLY, NOT Unity/PhysX.
using System;
using System.IO;
using B01.B0162.AtomicHostBridge;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
class World
{
 public readonly RunSnapshot Snapshot;
 public readonly AtomicDockLedger[] Ledger=new AtomicDockLedger[2];
 public World(RunSnapshot s){
  Snapshot=s;
  for(int i=0;i<2;i++){
   Ledger[i]=new AtomicDockLedger();
   var p=s.Parts[i];var zero=new Vector3d(0,0,0);
   if(p.Consumed && !Ledger[i].TryDetach(p.Id,zero,zero,zero,p.MassKg,0,0,out Vector3d _))
    throw new Exception("original R2 ledger could not rehydrate");
  }
 }
}
class Stage:IPreparedWorld
{
 public readonly World World;
 public bool ThrowAbort,WasAborted;
 public bool IsReady=>true;
 public Stage(RunSnapshot s){World=new World(s);}
 public bool ContainsConsumedId(int i,long id)=>World.Ledger[i].Contains(id);
 public int ConsumedCount(int i)=>World.Ledger[i].Count;
 public void Abort(){WasAborted=true;if(ThrowAbort)throw new IOException("injected cleanup exception");}
}
class Host:IAtomicWorldHost
{
 public World Current;
 public Stage Last;
 public bool Reject,ThrowAbort,ThrowAfterPublish,FailRestore,Quarantined;
 public int Restores;
 public Host(RunSnapshot s){Current=new World(s);}
 public object CurrentWorldToken=>Current;
 public bool IsQuiescent()=>true;
 public RunSnapshot CaptureCurrent()=>Current.Snapshot;
 public bool ContainsConsumedId(int i,long id)=>Current.Ledger[i].Contains(id);
 public int ConsumedCount(int i)=>Current.Ledger[i].Count;
 public IPreparedWorld PrepareFreshWorld(RunSnapshot s){
  Last=new Stage(s){ThrowAbort=ThrowAbort};return Last;
 }
 public bool TryPublish(object prior,IPreparedWorld ready){
  if(Reject)return false;
  Current=((Stage)ready).World;
  if(ThrowAfterPublish)throw new IOException("injected publish failure");
  return true;
 }
 public bool RestorePrevious(object prior){
  Restores++;if(FailRestore)return false;Current=(World)prior;return true;
 }
 public void Quarantine(string reason){Quarantined=true;}
}
internal static class Program
{
 static int checks,cases;
 static void Check(bool yes,string label){checks++;if(!yes)throw new Exception("QA_FAILURE "+label);}
 static void Case(string name,Action run){run();cases++;Console.WriteLine("BRIDGE_SOURCE_TEST_PASS "+name);}
 static PersistedPart Part(int i,bool detached){
  long owner=FrozenPartCatalog.Owner(i);
  return new PersistedPart(FrozenPartCatalog.Id(i),owner,detached?0:owner,
   FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
   detached?PartDockState.Detached:PartDockState.Attached,detached,!detached,
   new Vector3d(i+1,2,0),0,0,0,1,
   detached?new Vector3d(1,0,0):new Vector3d(0,0,0),
   new Vector3d(0,0,0),FrozenPartCatalog.Mass(i));
 }
 static RunSnapshot S(bool a,bool b,long gen=4)
  =>new RunSnapshot(86161,gen,new[]{Part(0,a),Part(1,b)});
 static byte[] Save(bool a,bool b,long gen=5)=>SnapshotCodec.Encode(S(a,b,gen));
 static void RealOriginalPositiveControl(){
  foreach(bool a in new[]{false,true})foreach(bool b in new[]{false,true}){
   var host=new Host(S(false,false));var bridge=new FrozenSaveHostBridge(host);
   Check(bridge.TryLoad(Save(a,b),86161,4)==BridgeResult.Accepted,
    "actual original port accepted valid selected #556 bytes");
   Check(host.Current.Ledger[0].Contains(2310001)==a &&
    host.Current.Ledger[1].Contains(2310002)==b,
    "new original #510 R2 ledgers follow snapshot consumed status");
   Check(!host.Quarantined&&host.Last!=null&&!host.Last.WasAborted,
    "successful publication does not abort staged world");
  }
 }
 static void SameGenerationFork(){
  var h=new Host(S(false,false));var adapter=new FrozenSaveHostBridge(h);
  Check(adapter.TryLoad(Save(true,false,5),86161,4)==BridgeResult.Accepted,
   "first original checkpoint accepted");
  World previous=h.Current;
  Check(previous.Ledger[0].Contains(2310001),"first original W06 Light consumed");
  BridgeResult response=adapter.TryLoad(Save(false,true,5),86161,4);
  Check(response==BridgeResult.Accepted,
   "SECURITY_RED original #577 accepted divergent checkpoint with same run and generation");
  Check(!object.ReferenceEquals(previous,h.Current)&&
    !h.Current.Ledger[0].Contains(2310001)&&h.Current.Ledger[1].Contains(2310002),
   "SECURITY_RED original consumed identity switched without monotonic generation");
 }
 static void AbortFailureVerdict(){
  var host=new Host(S(false,false)){Reject=true,ThrowAbort=true};
  var bridge=new FrozenSaveHostBridge(host);World before=host.Current;
  BridgeResult r=bridge.TryLoad(Save(true,false),86161,4);
  Check(host.Last!=null&&host.Last.WasAborted,"actual original port invoked Abort on rejected stage");
  Check(host.Quarantined&&bridge.Quarantined,"Abort throw caused global quarantine");
  Check(r==BridgeResult.Rejected,
   "SECURITY_RED actual port returned Rejected although its finally quarantined");
  Check(object.ReferenceEquals(before,host.Current),"original world not published on rejection");
  Check(bridge.TryLoad(Save(true,false),86161,4)==BridgeResult.Quarantine,
    "later API correctly refuses due quarantined flag contradicting earlier Rejected");
 }
 static void RollbackFaultControls(){
  var ok=new Host(S(false,false)){ThrowAfterPublish=true};
  var bridge=new FrozenSaveHostBridge(ok);var original=ok.Current;
  Check(bridge.TryLoad(Save(true,false),86161,4)==BridgeResult.Rejected,
    "original source compensates a throwing publish");
  Check(object.ReferenceEquals(original,ok.Current)&&ok.Restores==1&&!bridge.Quarantined,
    "actual original world token fully restored");
  var bad=new Host(S(false,false)){ThrowAfterPublish=true,FailRestore=true};
  var compromised=new FrozenSaveHostBridge(bad);
  Check(compromised.TryLoad(Save(true,false),86161,4)==BridgeResult.Quarantine &&
    compromised.Quarantined&&bad.Quarantined,
    "original bridge quarantines failed commit rollback");
 }
 static void CorruptAndSaveLedgerDenials(){
  var host=new Host(S(true,false,5));
  var bridge=new FrozenSaveHostBridge(host);
  host.Current.Ledger[0].ClearForNewRun();
  Check(bridge.TrySave(out byte[] b)==BridgeResult.InvalidSnapshot&&b==null,
    "source refuses original W06 consumed ID mismatch on capture");
  var valid=new Host(S(false,false,4));var second=new FrozenSaveHostBridge(valid);
  var raw=Save(true,false);raw[11]^=44;
  Check(second.TryLoad(raw,86161,4)==BridgeResult.InvalidSnapshot,
   "source rejects corrupt original selected #556 checksum");
  Check(valid.Current.Ledger[0].Count==0,"no partial ID consumption from corrupted save");
 }
 public static int Main(string[] args){
  try{
   Case("all four original W06/R2 consumed-ID cold load states",RealOriginalPositiveControl);
   Case("RED conflicting snapshot same run and generation",SameGenerationFork);
   Case("RED abort exception returns misleading rejection with quarantine",AbortFailureVerdict);
   Case("original publish exception atomic compensation and quarantine positive controls",RollbackFaultControls);
   Case("original Save actual ledger inconsistency and corrupt bytes denied",CorruptAndSaveLedgerDenials);
   Console.WriteLine("B0161_W15_EXACT_577_ORIGINAL_BRIDGE_SECURITY_RED_PASS cases="+cases+
    " assertions="+checks+" SOURCE_BRIDGE_SHA=7f26c1852783062d05ff0cb61fe3d07b848bf998"+
    " EXACT_556=YES REAL_W06_AND_510_R2=YES UNITY_NATIVE_G1=NOT_RUN");
   if(args.Length>0&&args[0]=="--release"){
    Console.WriteLine("W15_BRIDGE_RELEASE_HOLD=SAME_GENERATION_FORK_AND_ABORT_QUARANTINE_VERDICT_AND_NO_REAL_UNITY_HOST");
    return 3;
   }
   return 0;
  }catch(Exception e){Console.Error.WriteLine("BRIDGE_INDEPENDENT_QA_INFRA_ERROR "+e);return 2;}
 }
}

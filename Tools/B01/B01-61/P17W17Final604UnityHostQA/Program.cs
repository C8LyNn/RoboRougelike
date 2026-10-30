// Original B01-23 #604 Unity-facing source linked directly; UnityEngine is a MANAGED stub, NOT Unity/PhysX.
using System;using System.IO;using System.Linq;using System.Reflection;
using B01.UnitySaveBridge001;using B01.DetachedSaveContract001;using UnityEngine;
public sealed class SecureDebrisPlaygroundV2:MonoBehaviour{}
internal static class W17{
 const long RUN=1;
 static int checks,groups,rigModes,red;
 static void Check(bool b,string msg){checks++;if(!b)throw new Exception("QA_FAIL "+msg);}
 static void Group(string name,Action f){f();groups++;Console.WriteLine("W17_UNITY_SOURCE_GROUP_PASS "+name);}
 static void Temp(Action<string> test){
  string path=Path.Combine(Path.GetTempPath(),"b01w17-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(path);try{test(path);}finally{Directory.Delete(path,true);}
 }
 static IsolatedTwinRigSaveHost Fresh(string directory){
  Application.persistentDataPath=directory;
  MonoBehaviour.ResetRegistry();GameObject.All.Clear();
  var host=new IsolatedTwinRigSaveHost();MonoBehaviour.Register(host);
  var method=typeof(IsolatedTwinRigSaveHost).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance);
  Check(method!=null,"real original Awake Unity callback exists");
  method!.Invoke(host,null); // lifecycle callback ONLY, no frozen #510 private access
  Check(!host.HasWorld&&!host.Quarantined&&host.Generation==0,"new original isolated host");
  return host;
 }
 static byte[] Seed(bool a,bool b,long gen=1)=>
  IsolatedTwinRigSaveHost.MakeFixture(RUN,gen,a,b);
 static void SlotAndLegacy(){
  Temp(p=>{
   var h=Fresh(p);
   Check(!h.PersistentCheckpointEnabled,"source volatile by default");
   foreach(var bad in new[]{"","../escape","A space",new string('a',33),"é"})
     Check(!h.UsePersistentCheckpointSlot(bad),"unsafe slot denied: "+bad);
   Check(h.UsePersistentCheckpointSlot("W17_01"),"whitelisted private slot enabled");
   Check(h.PersistentCheckpointEnabled,"selected actual source reports durable fence");
   var legacy=new SecureDebrisPlaygroundV2();MonoBehaviour.Register(legacy);
   Check(!h.TryRestoreBytes(Seed(true,false),RUN,1)&&
      h.LastRestoreVerdict==WorldSwapVerdict.HostBusy&&!h.HasWorld,
      "old controller coexistence denied before stage");
   MonoBehaviour.Unregister(legacy);
   Check(h.TryRestoreBytes(Seed(true,false),RUN,1),"original host stages after legacy removed");
   Check(h.IsConsumed(0,2310001)&&!h.IsConsumed(1,2310002),"original W06 consumed Light ID");
   Check(!h.UsePersistentCheckpointSlot("W17_02"),"no switch persistent authority after physical world published");
  });
 }
 static void FourOriginalHostWorlds(){
  foreach(bool a in new[]{false,true})foreach(bool b in new[]{false,true})
  Temp(p=>{
   var host=Fresh(p);byte[] original=Seed(a,b);
   Check(SnapshotCodec.TryDecode(original,out RunSnapshot? snapshot)&&snapshot!=null,
      "actual original host only selected #556 format");
   Check(host.TryRestoreBytes(original,RUN,1),"direct unchanged Unity host Source stage");
   Check(host.HasWorld&&host.Generation==1&&!host.Quarantined,"original source state published");
   Check(host.IsConsumed(0,2310001)==a&&host.IsConsumed(1,2310002)==b,"original W06 Light/Heavy IDs");
   var parts=GameObject.All.Where(o=>o.name.StartsWith("original stable module ")).ToArray();
   Check(parts.Length==2&&parts.All(o=>o.activeInHierarchy&&!o.Destroyed),
     "both source Unity part GameObjects active after parent publication");
   Check(parts.All(o=>o.Body!=null&&o.Body.mass>0),
     "both source Rigidbody component mass values reconstructed");
   Check(host.TrySaveBytes(out byte[] saved)&&saved!=null,"actual source host volatile Save");
   Check(SnapshotCodec.TryDecode(saved,out RunSnapshot? s)&&s!=null&&
       s.Parts[0].Id==2310001&&s.Parts[1].Id==2310002,
       "actual original codec roundtrip stable Light/Heavy part IDs");
   rigModes++;
  });
 }
 static void PhysicalPersistentFenceAndFork(){
  Temp(p=>{
   var host=Fresh(p);
   Check(host.UsePersistentCheckpointSlot("slot1"),"select real persistent source slot");
   var seed=Seed(true,false);
   Check(host.TryRestoreBytes(seed,RUN,1),"stage exact #556 save to original host");
   string path=Path.Combine(p,"B0123UnitySaveBridge001","W16CheckpointFences","slot1.fence");
   Check(File.Exists(path)&&new FileInfo(path).Length==88,
     "original FileCheckpointFence wrote real 88 byte sidecar");
   var cold=Fresh(p);
   Check(cold.UsePersistentCheckpointSlot("slot1"),"new C# host same physical disk slot");
   Check(!cold.TryRestoreBytes(Seed(false,true),RUN,1)&&
     cold.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
     "genuine original fresh host refuses same-generation forged different consumed ID world");
   Check(!cold.HasWorld,"refused fork never published a world");
   Check(cold.TryRestoreBytes(seed,RUN,1)&&cold.IsConsumed(0,2310001),
     "fresh original source host may replay identical persisted 88-byte fenced checkpoint");
   Check(!cold.TryRestoreBytes(Seed(false,true,2),RUN,2)==false,
     "genuine source permits advancing to higher generation");
  });
 }
 static void DurableUnchangedRoundtrip(){
  Temp(p=>{
   var h=Fresh(p);byte[] seed=Seed(true,false);
   Check(h.UsePersistentCheckpointSlot("roundtrip"),"durable host setup");
   Check(h.TryRestoreBytes(seed,RUN,1),"exact initial fixture accepted by durable host");
   bool allowed=h.TrySaveBytes(out byte[] b);
   if(!allowed){
    var demo=Fresh(Path.Combine(p,"volatile"));
    Check(demo.TryRestoreBytes(seed,RUN,1)&&demo.TrySaveBytes(out byte[] fresh)&&fresh!=null,
      "same original Unity source with volatile fence can Save without movement");
    Check(!seed.SequenceEqual(fresh),
      "original world float readback changes binary double representation despite no motion");
    Check(h.IsConsumed(0,2310001)&&!h.Quarantined,
      "durable save refusal did not alter original consumed ID");
    red++;
    Console.WriteLine("W17_HOST_SOURCE_RED_PERSISTENT_NO_MOTION_SAVE=DETECTED_FLOAT_ROUNDTRIP");
   }else{
    Check(b!=null&&b.Length>0,"durable Save successful valid bytes");
    Console.WriteLine("W17_HOST_SOURCE_PERSISTENT_NO_MOTION_SAVE=GREEN");
   }
  });
 }
 static void InvalidDoesNotOverwrite(){
  Temp(p=>{
   var h=Fresh(p);Check(h.UsePersistentCheckpointSlot("safe"),"source persistent slot");
   Check(h.TryRestoreBytes(Seed(true,false),RUN,1),"original source baseline");
   var corrupt=Seed(false,true,2);corrupt[44]^=1;
   Check(!h.TryRestoreBytes(corrupt,RUN,1)&&h.LastRestoreVerdict==WorldSwapVerdict.InvalidSnapshot,
      "original #556 corrupt binary rejected before world swap");
   Check(!h.TryRestoreBytes(Seed(false,true),RUN,1)&&
      h.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
      "live same-gen conflicting world rejected");
   Check(h.HasWorld&&h.Generation==1&&h.IsConsumed(0,2310001)&&!h.IsConsumed(1,2310002)&&
      !h.Quarantined,"two abort paths leave original physical world/ledger authoritative");
  });
 }
 public static int Main(string[] args){
  try{
   Group("original public Unity host whitelist legacy/slot refusal",SlotAndLegacy);
   Group("original full Unity host API 4 Light Heavy snapshots, Rigidbody, W06",FourOriginalHostWorlds);
   Group("original physical disk fence and cold-run fork refusal",PhysicalPersistentFenceAndFork);
   Group("untouched durable Save binary float readback regression",DurableUnchangedRoundtrip);
   Group("corrupt and stale host rollback preserve consumed IDs",InvalidDoesNotOverwrite);
   Check(groups==5&&rigModes==4,"all unique Unity host cases actually exercised");
   Console.WriteLine("B0161_W17_604_EXACT_ORIGINAL_UNITY_HOST_SOURCE_QA_PASS groups="+groups+
      " checks="+checks+" physical_rig_variations="+rigModes+" durable_no_motion_red="+red+
      " MANAGED_UNITY_API_ONLY=TRUE UNITY_PHYSX_G1=NOT_RUN");
   if(args.Length>0&&args[0]=="--release"){
     Console.WriteLine("B0161_W17_604_RELEASE_HOLD=NO_NATIVE_UNITY_NO_CANONICAL_510_HYDRATION"+
       (red>0?"_AND_UNCHANGED_DURABLE_SAVE_RED":""));
     return 3;
   }
   return 0;
  }catch(Exception ex){Console.Error.WriteLine("B0161_W17_QA_INFRA_FAILURE "+ex);return 2;}
 }
}
// W17 sole selected #596 direct ORIGINAL Unity MonoBehaviour source.
// UnityEngine only MANAGED public-API stub -- NEVER real UnityEngine/PhysX G1.
using System;using System.IO;using System.Linq;using System.Reflection;
using B01.UnitySaveBridge001;using B01.DetachedSaveContract001;using UnityEngine;
public sealed class SecureDebrisPlaygroundV2:MonoBehaviour{}
static class W17 {
 const long RUN=1;
 static int asserts,groups,sessions,restartChecks;
 static void Check(bool ok,string what){asserts++;if(!ok)throw new InvalidOperationException("B0161_W17_SELECTED596_SOURCE_FAILURE "+what);}
 static void Case(string label,Action run){run();groups++;Console.WriteLine("W17_596_UNITY_SOURCE_GROUP_PASS "+label);}
 static void Temp(Action<string> action){
  string path=Path.Combine(Path.GetTempPath(),"b0161w17-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(path);try{action(path);}finally{Directory.Delete(path,true);}
 }
 static IsolatedTwinRigSaveHost Host(string path){
  Application.persistentDataPath=path;MonoBehaviour.ResetRegistry();GameObject.All.Clear();
  var host=new IsolatedTwinRigSaveHost();MonoBehaviour.Register(host);
  var method=typeof(IsolatedTwinRigSaveHost).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance);
  Check(method!=null,"original Unity MonoBehaviour Awake exists");
  method!.Invoke(host,null); // invokes normal Unity lifecycle ONLY; never reads #510 private fields
  Check(!host.HasWorld&&host.Generation==0&&!host.Quarantined,"fresh original selected #596 host");
  return host;
 }
 static byte[] Seed(bool light,bool heavy,long generation=1,long run=RUN)=>
  IsolatedTwinRigSaveHost.MakeFixture(run,generation,light,heavy);
 static void FourWorldsDiskColdAndIncrementingEpoch(){
  foreach(bool light in new[]{false,true})foreach(bool heavy in new[]{false,true})
  Temp(p=>{
   var first=Host(p);byte[] initial=Seed(light,heavy);
   Check(SnapshotCodec.TryDecode(initial,out RunSnapshot? s)&&s!=null,
     "actual original #556 codec for original #596 Unity host");
   Check(first.TryRestoreBytes(initial,RUN,1),"actual original Unity-facing C# stages physical mock rig");
   Check(first.HasWorld&&first.Generation==1&&!first.Quarantined,
     "selected original source published exact generation 1");
   Check(first.IsConsumed(0,2310001)==light&&first.IsConsumed(1,2310002)==heavy,
     "original W06 consumed IDs match Light/Heavy");
   var parts=GameObject.All.Where(x=>x.name.StartsWith("original stable module ")).ToArray();
   Check(parts.Length==2&&parts.All(x=>x.activeInHierarchy&&!x.Destroyed),
     "both staged part objects active after source transaction");
   Check(parts.All(x=>x.Body!=null&&x.Body.mass>0),
     "source staged original #510 mass on simulated Unity Rigidbody API");
   string durable=Path.Combine(p,"b0123_one_save_host_v2.checkpoint");
   Check(File.Exists(durable),"selected original FileCheckpointStore really wrote disk authority");
   Check(first.TrySaveBytes(out byte[] saved)&&saved!=null,
     "selected #596 emits next-generation save even from unchanged float-readback scene");
   Check(SnapshotCodec.TryDecode(saved,out RunSnapshot? next)&&next!=null&&next.Generation==2,
     "original #596 advances Save generation instead of conflicting same-gen SHA");
   string file=Path.Combine(p,"world.save");File.WriteAllBytes(file,saved!);
   byte[] fromDisk=File.ReadAllBytes(file);
   Check(fromDisk.SequenceEqual(saved!),"exact original #556 binary persists through OS file");
   var cold=Host(p);
   Check(cold.TryRestoreBytes(fromDisk,RUN,2),"fresh original Unity-facing #596 host restores persisted generation 2");
   Check(cold.Generation==2&&cold.IsConsumed(0,2310001)==light&&
     cold.IsConsumed(1,2310002)==heavy,"cold physical world and W06 ID stable after saved generation advance");
   restartChecks++;sessions++;
  });
 }
 static void RealDiskSameGenForkAndReplay(){
  Temp(p=>{
   var h=Host(p);var initial=Seed(true,false,8);
   Check(h.TryRestoreBytes(initial,RUN,1),"actual selected producer baseline generation8");
   var forged=Host(p);
   Check(!forged.TryRestoreBytes(Seed(false,true,8),RUN,1)&&
     forged.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
     "same generation valid alternate detached ledger refused after fresh process-like host");
   Check(!forged.HasWorld&&!forged.Quarantined,"refused snapshot never stages owner world");
   Check(!forged.TryRestoreBytes(Seed(false,true,7),RUN,1)&&
     forged.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
     "stale generation rejected from durable OS checkpoint");
   Check(forged.TryRestoreBytes(initial,RUN,1)&&forged.IsConsumed(0,2310001),
     "exact same SHA original checkpoint permitted cold replay");
  });
 }
 static void LegacyAndTransformQuiescence(){
  Temp(p=>{
   var h=Host(p);var legacy=new SecureDebrisPlaygroundV2();MonoBehaviour.Register(legacy);
   Check(!h.TryRestoreBytes(Seed(true,false),RUN,1)&&
       h.LastRestoreVerdict==WorldSwapVerdict.HostBusy&&!h.HasWorld,
       "actual selected Unity host refuses legacy #510 controller overlap");
   MonoBehaviour.Unregister(legacy);
   Check(h.TryRestoreBytes(Seed(true,false),RUN,1),"legacy removed source Load can proceed");
   h.transform.position=new Vector3(1,0,0);
   Check(!h.TrySaveBytes(out byte[] _),"active non-origin host transform fails quiescence");
   h.transform.position=Vector3.zero;
   Check(h.TrySaveBytes(out byte[] bytes)&&bytes!=null,
     "original source returns Save bytes again when host transform restored");
  });
 }
 static void CorruptAndSameGenNoPartialWorldSwap(){
  Temp(p=>{
   var h=Host(p);Check(h.TryRestoreBytes(Seed(true,false),RUN,1),"load baseline");
   byte[] corrupted=Seed(false,true,2);corrupted[48]^=0x20;
   Check(!h.TryRestoreBytes(corrupted,RUN,1)&&
     h.LastRestoreVerdict==WorldSwapVerdict.InvalidSnapshot,
     "original #556 checksum corruption denied before mutation");
   Check(!h.TryRestoreBytes(Seed(false,true,1),RUN,1)&&
     h.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
     "same-run same-generation conflicting W06 ID refused");
   Check(!h.TryRestoreBytes(Seed(false,true,2,999),RUN,1)&&
     h.LastRestoreVerdict==WorldSwapVerdict.WrongRun,
     "foreign run refused without checkpoint changes");
   Check(h.HasWorld&&h.Generation==1&&h.IsConsumed(0,2310001)&&
     !h.IsConsumed(1,2310002)&&!h.Quarantined,
     "source physical world/IDs unchanged after three faulted snapshots");
  });
 }
 static void SaveEpochMonotonic(){
  Temp(p=>{
   var h=Host(p);Check(h.TryRestoreBytes(Seed(true,false,3),RUN,1),"initial generation3");
   Check(h.TrySaveBytes(out byte[] a)&&SnapshotCodec.TryDecode(a,out RunSnapshot? first)&&
      first!=null&&first.Generation==4,"first actual source Save next epoch 4");
   Check(h.TrySaveBytes(out byte[] b)&&SnapshotCodec.TryDecode(b,out RunSnapshot? second)&&
      second!=null&&second.Generation==5,"second exact original source Save advances epoch 5");
   Check(h.Generation==3&&h.IsConsumed(0,2310001),
     "Save does not mutate published world generation or original W06 ledger");
  });
 }
 public static int Main(string[] args){
  try{
    Case("four original Unity host rig modes with actual Save files and cold restart",FourWorldsDiskColdAndIncrementingEpoch);
    Case("real durable OS checkpoint same generation fork/stale and exact replay",RealDiskSameGenForkAndReplay);
    Case("legacy controller isolation and Unity host transform quiescence",LegacyAndTransformQuiescence);
    Case("corrupt/foreign/stale binary preserves published source world",CorruptAndSameGenNoPartialWorldSwap);
    Case("generation monotonic repeated Save without physical publication",SaveEpochMonotonic);
    Check(groups==5&&sessions==4&&restartChecks==4,"all independent original #596 Unity source groups executed");
    Console.WriteLine("B0161_W17_FINAL596_ORIGINAL_UNITY_HOST_SOURCE_QA_GREEN groups="+groups+
      " assertions="+asserts+" four_rig_modes="+sessions+" physical_disk_cold_restart="+restartChecks+
      " ORIGINAL_SOURCE=596 ORIGINAL_SAVE=556 W06_IDS=YES NATIVE_UNITY_G1=NOT_RUN");
    if(args.Length>0&&args[0]=="--release"){
       Console.WriteLine("B0161_W17_GAME_RELEASE_HOLD=FROZEN510_PRIVATE_RIG_NOT_HYDRATED_AND_NATIVE_G1_NOT_RUN");
       return 3;
    }
    return 0;
  }catch(Exception e){Console.Error.WriteLine("B0161_W17_QA_FAILURE "+e);return 2;}
 }
}
// B01-61 W18: independently execute the ORIGINAL frozen #596 FileCheckpointStore
// across genuinely separate CLR operating-system processes. No Unity engine or PhysX.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using B01.UnitySaveBridge001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
static class Program {
 const long Run=176118;
 static int assertions, groups, races,denied,files;
 static void Check(bool yes,string why) { assertions++; if(!yes)throw new Exception("W18_QA_FAILURE "+why); }
 static void Group(string name,Action work){work();groups++;Console.WriteLine("W18_SELECTED596_CASE_PASS "+name);}
 static void Temp(Action<string> test){
  var root=Path.Combine(Path.GetTempPath(),"b0161-w18-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(root);
  try{test(root);}finally{ if(Directory.Exists(root))Directory.Delete(root,true); }
 }
 static string Hash(int round,int actor)=>new string((char)('a'+round),62)+actor.ToString("D2");
 static int Child(string[] args){
  if(args.Length!=9) return 2;
  string path=args[1], ready=args[2], go=args[3];
  long run=long.Parse(args[4]),prev=long.Parse(args[5]),next=long.Parse(args[7]);
  string prevHash=args[6],hash=args[8];
  File.WriteAllText(ready,"ready");
  var until=DateTime.UtcNow.AddSeconds(25);
  while(!File.Exists(go)&&DateTime.UtcNow<until)Thread.Sleep(3);
  if(!File.Exists(go))return 2;
  bool ok=new FileCheckpointStore(path).Commit(run,prev,prevHash,next,hash);
  Console.WriteLine("ACTUAL_CHILD_PROCESS_COMMIT="+(ok?"ACCEPTED":"DENIED")+" HASH="+hash);
  return ok?0:9;
 }
 static void RaceProcesses(){
  Temp(root=>{
   string slot=Path.Combine(root,"real-checkpoint.txt");
   long prev=0;string prevHash="";
   for(int round=1;round<=4;round++){
    string barrier=Path.Combine(root,"launch-"+round);
    var children=new List<(Process proc,string hash,string ready)>();
    try{
      for(int actor=0;actor<8;actor++){
       string hash=Hash(round,actor),ready=Path.Combine(root,"child-"+round+"-"+actor+".ready");
       var start=new ProcessStartInfo("dotnet"){
         RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,
         CreateNoWindow=true
       };
       start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
       foreach(var v in new[]{"worker",slot,ready,barrier,Run.ToString(),prev.ToString(),prevHash,round.ToString(),hash})
          start.ArgumentList.Add(v);
       var p=Process.Start(start)??throw new Exception("child launch");
       children.Add((p,hash,ready));
      }
      var deadline=DateTime.UtcNow.AddSeconds(25);
      while(children.Any(c=>!File.Exists(c.ready))&&DateTime.UtcNow<deadline) Thread.Sleep(5);
      Check(children.All(c=>File.Exists(c.ready)),"all eight OS child processes ready before release");
      File.WriteAllText(barrier,"go");
      var accepted=new List<string>();
      foreach(var (proc,hash,ready) in children){
       Check(proc.WaitForExit(30000),"each child process completed deadline");
       string stdout=proc.StandardOutput.ReadToEnd(),stderr=proc.StandardError.ReadToEnd();
       Check(proc.ExitCode==0||proc.ExitCode==9,
        "child returned legitimate commit/reject, not infrastructure "+stdout+stderr);
       Check(stdout.Contains("ACTUAL_CHILD_PROCESS_COMMIT="),"child original source executed");
       if(proc.ExitCode==0)accepted.Add(hash);else denied++;
      }
      Check(accepted.Count==1,"EXACTLY one real OS process won same old-generation CAS round");
      var store=new FileCheckpointStore(slot);
      Check(store.Read(Run,out long finalGen,out string actual)&&finalGen==round&&
         actual==accepted[0],"OS checkpoint keeps only winner's run, exact epoch and 64-byte hash");
      Check(!Directory.GetFiles(root,"*.tmp",SearchOption.TopDirectoryOnly).Any(),
        "owner's temp artifacts cleaned after failed OS CAS attempts");
      prev=finalGen;prevHash=actual;races++;
      Console.WriteLine("ACTUAL_RACE_ROUND_PASS="+round+" processes=8 wins=1 rejects=7 checkpoint="+actual);
    } finally {
     foreach(var (p,_,_) in children){try{if(!p.HasExited)p.Kill(true);}catch{}p.Dispose();}
    }
   }
   Check(races==4&&denied==28,"four full actual interprocess writer races with 28 denied");
   Check(!new FileCheckpointStore(slot).Commit(Run,0,"",5,Hash(1,1)),
      "stale OS process cannot force rollback to older checkpoint");
   Check(!new FileCheckpointStore(slot).Read(Run+1,out long _,out string _),
      "foreign Run ID cannot claim selected original file checkpoint");
   files++;
  });
 }
 static void LockContentionAndCorruption(){
  Temp(root=>{
    string path=Path.Combine(root,"checkpoint");
    var store=new FileCheckpointStore(path);
    Check(store.Commit(Run,0,"",1,Hash(1,1)),"actual selected source creates checkpoint");
    byte[] original=File.ReadAllBytes(path);
    using(var lockStream=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
       Check(!new FileCheckpointStore(path).Read(Run,out long _,out string _),
         "busy OS lock fail-closed for concurrent readers");
       Check(!new FileCheckpointStore(path).Commit(Run,1,Hash(1,1),2,Hash(2,1)),
         "busy OS lock fails before altering checkpoint");
       Check(original.SequenceEqual(File.ReadAllBytes(path)),
         "failed locked-source write did not modify original checkpoint bytes");
       Check(!Directory.GetFiles(root,"*.tmp").Any(),
         "locked aborted source does not leave orphan pending file");
    }
    Check(store.Read(Run,out long x,out string h)&&x==1&&h==Hash(1,1),
      "after exclusive lock release old checkpoint restored");
    Check(store.Commit(Run,1,h,2,Hash(2,2)),"retry after lock release commits intended epoch");
    File.WriteAllText(path,"FOREIGN|bad|path|not-authority");
    var before=File.ReadAllBytes(path);
    Check(!store.Read(Run,out long _,out string _),"foreign checkpoint detected before use");
    Check(!store.Commit(Run,2,Hash(2,2),3,Hash(3,1)),
      "corrupted checkpoint cannot advance generation or destroy evidence");
    Check(before.SequenceEqual(File.ReadAllBytes(path)),
      "bad checkpoint bytes remain untouched for receiver analysis");
    files++;
  });
 }
 static void ExactFrozenSnapshotId(){
   var z=new Vector3d(0,0,0);
   foreach(bool light in new[]{false,true})foreach(bool heavy in new[]{false,true}){
    var parts=new PersistedPart[2];
    for(int slot=0;slot<2;slot++){
      bool det=slot==0?light:heavy;
      parts[slot]=new PersistedPart(FrozenPartCatalog.Id(slot),FrozenPartCatalog.Owner(slot),
       det?0:FrozenPartCatalog.Owner(slot),FrozenPartCatalog.Asset(slot),
       FrozenPartCatalog.Socket(slot),det?PartDockState.Detached:PartDockState.Attached,
       det,!det,new Vector3d(1+slot,1.5,0),0,0,0,1,det?new Vector3d(.25,0,0):z,z,
       FrozenPartCatalog.Mass(slot));
    }
    byte[] bytes=SnapshotCodec.Encode(new RunSnapshot(Run,1,parts));
    Check(ColdRestore.TryStage(bytes,out ColdRestore? cold)&&cold!=null,
      "real frozen #556 binary contract parsed to real W06 ColdRestore");
    Check(cold!.ContainsConsumedId(0,2310001)==light&&
          cold.ContainsConsumedId(1,2310002)==heavy,
      "original consumed ID reconstruction for Light and Heavy");
    Check(cold.Snapshot.Parts[0].AssetGuid==FrozenPartCatalog.Asset(0)&&
          cold.Snapshot.Parts[1].SocketGuid==FrozenPartCatalog.Socket(1),
      "original immutable source asset/socket stable identifiers persist through codec");
    var realLedger=new AtomicDockLedger();
    int id=light?0:1;
    if(light||heavy){
      Check(realLedger.TryDetach(FrozenPartCatalog.Id(id),z,z,z,
         FrozenPartCatalog.Mass(id),0,0,out Vector3d _)
         &&realLedger.Contains(FrozenPartCatalog.Id(id)),
         "original frozen #510 R2 consumed ledger recognizes stable part ID");
    }
   }
 }
 public static int Main(string[] args){
  try{
   if(args.Length>0&&args[0]=="worker")return Child(args);
   Group("genuine eight-way OS process source checkpoint CAS four epochs",RaceProcesses);
   Group("original source OS lock held, failed write, retry, corrupt-fence rollback",LockContentionAndCorruption);
   Group("original selected #556+W06+#510 R2 real Light Heavy IDs all four modes",ExactFrozenSnapshotId);
   Check(groups==3&&races==4&&denied==28&&files==2,
      "all independent race/rollback/identity suites truly executed");
   Console.WriteLine("B0161_W18_FINAL596_REAL_INTERPROCESS_SOURCE_QA_PASS groups="+groups+
      " asserts="+assertions+" real_child_processes=32 CAS_winners="+races+
      " refused="+denied+" OS_files="+files+
      " ORIGINAL_596_CHECKPOINT=YES EXACT_556_W06_510_IDS=YES NATIVE_UNITY=NOT_RUN");
   if(args.Length>0&&args[0]=="--release"){
      Console.WriteLine("W18_LIVE_RELEASE_HOLD=NO_CANONICAL_510_RIG_HYDRATION_OR_NATIVE_G1_OR_COMPLETE_SAVE_FENCE_TRANSACTION");
      return 3;
   }
   return 0;
  } catch(Exception ex){Console.Error.WriteLine("W18_ACTUAL_SOURCE_QA_FAILURE "+ex);return 2;}
 }
}

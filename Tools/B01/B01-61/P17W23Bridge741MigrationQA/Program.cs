#nullable enable
using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using B01.CanonicalSlotAdapter003;
using B01.CanonicalWorldApi001;
using B01.SingleFileSaveSlot001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
internal static class IndependentQA
{
 const long Run=61241;
 static readonly Guid Slot=Guid.Parse("61241000-6124-4124-8124-000000006124");
 static int passed,failed,children;
 static byte[] Data(long gen,bool light,bool heavy)
 {
  var arr=new PersistedPart[2];
  for(int i=0;i<2;i++)
  {
   var s=SecureRigCatalog.At(i);bool det=i==0?light:heavy;
   long own=FrozenPartCatalog.Owner(i);
   arr[i]=new PersistedPart(s.PartId,own,det?0:own,
     FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
     det?PartDockState.Detached:PartDockState.Attached,det,!det,
     new Vector3d(s.RootX+s.PartOffsetX,det?2.2:1.2,0),
     0,0,0,1,det?new Vector3d(.25,0,0):new Vector3d(0,0,0),
     new Vector3d(0,0,0),s.PartMassKg);
  }
  return SnapshotCodec.Encode(new RunSnapshot(Run,gen,arr));
 }
 static bool Ids(byte[] raw,long gen,bool light,bool heavy)
 {
  return ColdRestore.TryStage(raw,out ColdRestore? c)&&c!=null&&
    c.Snapshot.RunId==Run&&c.Snapshot.Generation==gen&&
    c.Snapshot.Parts[0].Id==2310001&&c.Snapshot.Parts[1].Id==2310002&&
    c.ContainsConsumedId(0,2310001)==light&&c.ContainsConsumedId(1,2310002)==heavy;
 }
 sealed class Gate:ICanonicalMutationGate{public bool IsQuiescent()=>true;}
 sealed class World:ICanonicalStagedWorld
 {
  readonly RunSnapshot snap;
  public bool Active,Retired;
  public World(RunSnapshot snap){this.snap=snap;}
  public RunSnapshot Capture()=>snap;
  public void Activate(){Active=true;}
  public void Deactivate(){Active=false;}
  public void Retire(){Active=false;Retired=true;}
 }
 sealed class Factory:ICanonicalRigFactory
 {
  public World? Published,Partial;
  public bool BreakPrepare,BreakCleanup;
  public ICanonicalStageOwnership BeginOwnedStage(RunSnapshot s,ColdRestore c)
  {
   if(c.ContainsConsumedId(0,2310001)!=s.Parts[0].Consumed)
     throw new IOException("original ledger mismatch");
   return new Owner(this,s);
  }
  sealed class Owner:ICanonicalStageOwnership
  {
   readonly Factory f;readonly RunSnapshot s;
   public Owner(Factory f,RunSnapshot s){this.f=f;this.s=s;}
   public ICanonicalStagedWorld PrepareInactive()
   {
    if(f.BreakPrepare)
    {
     f.Partial=new World(s){Active=true};
     throw new IOException("partial rig remains");
    }
    f.Published=new World(s);return f.Published;
   }
   public bool CleanupUnpublishedAndVerifyDestroyed()
   {
    if(f.BreakCleanup)return false;
    f.Partial?.Retire();return true;
   }
  }
 }
 static bool Open(string path,bool create,Factory f,out CanonicalSlotSessionR3? s)
 {
  var status=CanonicalSlotSessionR3.TryOpen(path,Run,Slot,create,out s);
  return status==B01.CanonicalSlotAdapter003.CanonicalSessionOpenStatus.Accepted&&
    s!=null&&s.Attach(new Gate(),f);
 }
 static bool Close(CanonicalSlotSessionR3? s,Factory f)
 {
  if(s==null)return false;
  f.Partial?.Retire();f.Published?.Retire();
  bool done=s.TryCloseAfterPhysicalRetirement(()=>
    (f.Partial==null||f.Partial.Retired&&!f.Partial.Active)&&
    (f.Published==null||f.Published.Retired&&!f.Published.Active));
  s.Dispose();return done;
 }
 static int Child(string mode,string path)
 {
  if(mode=="--r3")
  {
   var status=CanonicalSlotSessionR3.TryOpen(path,Run,Slot,false,out var owner);
   if(status==B01.CanonicalSlotAdapter003.CanonicalSessionOpenStatus.Busy&&owner==null)
   {Console.WriteLine("W23_741_CHILD_R3_SINGLE_OWNER_BUSY");return 5;}
   if(owner!=null)owner.Dispose();
   return 6;
  }
  if(mode=="--legacy")
  {
   SlotStatus status=SingleFileSaveSlot.TryOpenExisting(path,Run,Slot,out var legacy);
   if(status!=SlotStatus.Accepted||legacy==null)return 7;
   using(legacy)
   {
    if(legacy.TryCommit(1,Data(2,false,true))!=SlotStatus.Accepted)return 8;
    if(legacy.TryRead(out long g,out byte[] b)!=SlotStatus.Accepted||
       g!=2||!Ids(b,2,false,true))return 9;
    Console.WriteLine("W23_741_CHILD_RAW_669_WRITER_BYPASSES_R3_OWNER");
    return 0;
   }
  }
  if(mode=="--cold")
  {
   var f=new Factory();
   if(!Open(path,false,f,out var s)||s==null)return 10;
   bool good=s.TryRestore(Data(2,false,true),Run,2)==CanonicalGateResult.Accepted&&
     s.TryCapturePreview(out byte[] p)&&Ids(p,2,false,true)&&Close(s,f);
   if(good){Console.WriteLine("W23_741_CHILD_R3_COLD_ORIGINAL_HEAVY_GEN2");return 0;}
   return 11;
  }
  return 12;
 }
 static bool ChildProbe(string mode,string path,int expected)
 {
  var p=new ProcessStartInfo("dotnet")
  {UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
  p.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
  p.ArgumentList.Add(mode);p.ArgumentList.Add(path);
  using var c=Process.Start(p);
  if(c==null)return false;
  if(!c.WaitForExit(13000)){try{c.Kill(true);}catch{} return false;}
  string output=c.StandardOutput.ReadToEnd(),errors=c.StandardError.ReadToEnd();
  children++;
  if(c.ExitCode!=expected)
   Console.WriteLine("W23_741_CHILD_EXIT_FAIL "+mode+" want="+expected+
    " got="+c.ExitCode+" out="+output+" errors="+errors);
  string marker=mode=="--r3"?"W23_741_CHILD_R3_SINGLE_OWNER_BUSY":
    mode=="--legacy"?"W23_741_CHILD_RAW_669_WRITER_BYPASSES_R3_OWNER":
    "W23_741_CHILD_R3_COLD_ORIGINAL_HEAVY_GEN2";
  return c.ExitCode==expected&&output.Contains(marker);
 }
 static void Test(string label,Func<string,bool> run)
 {
  string root=Path.Combine(Path.GetTempPath(),"B0161-W23-R3-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(root);
  try
  {
   bool result=run(Path.Combine(root,"owned-slot"));
   if(result){passed++;Console.WriteLine("PASS "+label);}
   else {failed++;Console.WriteLine("FAIL "+label);}
  }
  catch(Exception ex){failed++;Console.WriteLine("FAIL "+label+" "+ex);}
  finally {try{Directory.Delete(root,true);}catch(IOException){}
           catch(UnauthorizedAccessException){}}
 }
 static bool NormalSingleOwner(string p)
 {
  var f=new Factory();
  if(!Open(p,true,f,out var s)||s==null)return false;
  if(s.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
      f.Published==null||!f.Published.Active||
      !s.TryCapturePreview(out byte[] raw)||!Ids(raw,1,true,false))return false;
  bool noSecondFile=!File.Exists(Path.Combine(p,"slot.lease"));
  bool busy=ChildProbe("--r3",p,5);
  bool same=CanonicalSlotSessionR3.TryOpen(p,Run,Slot,false,out var second)==
    B01.CanonicalSlotAdapter003.CanonicalSessionOpenStatus.Busy&&second==null;
  bool closed=Close(s,f);
  return noSecondFile&&busy&&same&&closed;
 }
 static bool LegacyRawBypass(string p)
 {
  var f=new Factory();
  if(!Open(p,true,f,out var s)||s==null)return false;
  if(s.TryRestore(Data(1,true,false),Run,1)!=CanonicalGateResult.Accepted||
     f.Published==null||!f.Published.Active||!ChildProbe("--r3",p,5))return false;
  bool legacy=ChildProbe("--legacy",p,0);
  bool live=f.Published.Active&&s.HasWorld&&s.OwnsSlot;
  bool rejected=!s.TryCapturePreview(out _)&&s.Quarantined;
  Console.WriteLine("W23_741_IF_LEGACY_669_RAW_SOURCE_COINSTALLED_RED="+
    (legacy&&live?"REPRODUCED":"NOT_REPRODUCED"));
  Console.WriteLine("W23_741_R3_STALE_PREVIEW_AFTER_RAW_WRITE_GREEN="+
    (rejected?"QUARANTINED":"FAILED"));
  bool noExtra=!File.Exists(Path.Combine(p,"slot.lease"));
  bool retirement=Close(s,f);
  bool cold=ChildProbe("--cold",p,0);
  return legacy&&live&&rejected&&retirement&&cold&&!noExtra;
 }
 static bool BadCleanupLocks(string p)
 {
  var f=new Factory{BreakPrepare=true,BreakCleanup=true};
  if(!Open(p,true,f,out var s)||s==null)return false;
  bool quarantined=s.TryRestore(Data(1,true,false),Run,1)==
    CanonicalGateResult.RollbackQuarantine&&s.Quarantined&&!s.HasWorld&&
    f.Partial!=null&&f.Partial.Active;
  s.Dispose();
  bool held=s.OwnsSlot&&ChildProbe("--r3",p,5);
  bool refused=!s.TryCloseAfterPhysicalRetirement(()=>false)&&
     !s.TryCloseAfterPhysicalRetirement(()=>
       throw new IOException("retirement not certified"))&&
      ChildProbe("--r3",p,5);
  f.Partial?.Retire();
  bool release=s.TryCloseAfterPhysicalRetirement(()=>
    f.Partial!=null&&f.Partial.Retired&&!f.Partial.Active);
  return quarantined&&held&&refused&&release;
 }
 static bool FourFrozenMasks()
 {
  for(int i=0;i<4;i++)
  {
   bool light=(i&1)!=0,heavy=(i&2)!=0;
   if(!Ids(Data(1,light,heavy),1,light,heavy))return false;
  }
  return true;
 }
 public static int Main(string[] args)
 {
  if(args.Length==2&&args[0].StartsWith("--",StringComparison.Ordinal))
    return Child(args[0],args[1]);
  if(args.Length>0)return 12;
  Test("DIRECT_ORIGINAL_556_FOUR_W06_LIGHT_HEAVY_MASKS",_=>FourFrozenMasks());
  Test("R3_ONE_729_OWNER_AND_NO_STORAGE_LEASE",NormalSingleOwner);
  Test("R3_LEGACY_RAW_669_COINSTALL_MIGRATION_BYPASS_NEGATIVE",LegacyRawBypass);
  Test("R3_PARTIAL_WORLD_FALSE_CLEANUP_HOLDS_ONLY_OWNER",BadCleanupLocks);
  Console.WriteLine("B0161_W23_NEW_R3_741_SOURCE_CASES PASS="+passed+" FAIL="+failed);
  Console.WriteLine("B0161_W23_NEW_R3_741_REAL_CHILD_CLR="+children);
  Console.WriteLine("B0161_W23_INTEGRATION_DISPOSITION="+(failed==0?
    "R3_STANDALONE_SCOPE_GREEN_LEGACY_RAW_COINSTALL_RELEASE_HOLD":"UNVERIFIED"));
  Console.WriteLine("NATIVE_UNITY_G1=NOT_RUN CANONICAL_PRIVATE_510_WORLD_UNIMPLEMENTED NO_MERGE");
  return failed==0?0:1;
 }
}

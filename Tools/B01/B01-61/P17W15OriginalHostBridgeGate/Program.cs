// Independent W15 ORIGINAL #510 Unity C# + selected #556 Save C# cross-source test.
// Only UnityEngine is a historical synthetic QA boundary. Real Unity/PhysX NOT_RUN.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using B01.SecureReattachV2;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using UnityEngine;
internal static class Program
{
 static int checks,cases,lost,denials;
 static void Check(bool x,string message){checks++;if(!x)throw new Exception("QA_FAIL "+message);}
 static void Case(string s,Action a){a();cases++;Console.WriteLine("W15_CASE_PASS "+s);}
 static SecureDebrisPlaygroundV2 NewHost(){
    Physics.Reset();var g=new GameObject("real #510 test host");
    var d=g.AddComponent<SecureDebrisPlaygroundV2>();d.BuildDemo();return d;
 }
 static void Detach(SecureDebrisPlaygroundV2 d,int i){
    Check(d.Detach(i),"actual original #510 Detach");
    var b=d.PartBody(i);b.transform.position=b.transform.position+new Vector3(.5f+i,.2f,0);
    b.linearVelocity=new Vector3(1+i,.5f,.2f);b.angularVelocity=new Vector3(.1f,0,.3f);
    Check(d.AppliedCount(i)==1&&d.IsDetached(i),"original R2 and W06 consumed ID");
 }
 static PersistedPart Part(SecureDebrisPlaygroundV2 d,int i){
    var b=d.PartBody(i);bool detached=d.IsDetached(i);
    Check(d.PartId(i)==FrozenPartCatalog.Id(i),"selected source catalog ID match");
    Check(b.mass==FrozenPartCatalog.Mass(i),"actual Rigidbody mass matches source catalog");
    Check(d.AppliedCount(i)==(detached?1:0),"detached equals W06 consumed");
    Check((b.transform.parent==null)==detached,"actual part parent matches detached");
    return new PersistedPart(d.PartId(i),FrozenPartCatalog.Owner(i),
      detached?0:FrozenPartCatalog.Owner(i),FrozenPartCatalog.Asset(i),
      FrozenPartCatalog.Socket(i),detached?PartDockState.Detached:PartDockState.Attached,
      detached,!detached,new Vector3d(b.transform.position.x,b.transform.position.y,b.transform.position.z),
      0,0,0,1,new Vector3d(b.linearVelocity.x,b.linearVelocity.y,b.linearVelocity.z),
      new Vector3d(b.angularVelocity.x,b.angularVelocity.y,b.angularVelocity.z),b.mass);
 }
 static byte[] ActualSave(SecureDebrisPlaygroundV2 d,long generation){
    var s=new RunSnapshot(771105,generation,new[]{Part(d,0),Part(d,1)});
    Check(SnapshotRules.Valid(s),"#556 accepts genuine original Unity host state");
    return SnapshotCodec.Encode(s);
 }
 static void FourStates(){
    for(int a=0;a<2;a++)for(int b=0;b<2;b++){
      var host=NewHost();if(a==1)Detach(host,0);if(b==1)Detach(host,1);
      var raw=ActualSave(host,11);
      Check(SnapshotCodec.TryDecode(raw,out RunSnapshot decoded)&&decoded!=null,
        "genuine original #556 binary Save decoded");
      Check(ColdRestore.TryStage(raw,out ColdRestore cold)&&cold!=null,
        "genuine original #556 W06 cold preparation");
      Check(cold.ContainsConsumedId(0,2310001)==(a==1),
        "Light original W06 consumed ID survived Save");
      Check(cold.ContainsConsumedId(1,2310002)==(b==1),
        "Heavy original W06 consumed ID survived Save");
    }
 }
 static void ActualDiskColdReload(){
    var previous=NewHost();Detach(previous,0);Detach(previous,1);
    var p0=previous.PartBody(0).transform.position;
    var p1=previous.PartBody(1).transform.position;
    var path=Path.Combine(Path.GetTempPath(),"b01w15-host-save-"+Guid.NewGuid().ToString("N")+".bin");
    try {
      var source=ActualSave(previous,13);File.WriteAllBytes(path,source);
      var bytes=File.ReadAllBytes(path);
      Check(source.SequenceEqual(bytes),"actual physical binary file Save/read identical");
      var fresh=NewHost();var logical=new ColdSessionHandle();
      Check(logical.TrySwap(bytes,771105,13,()=>true)==RestoreVerdict.Accepted,
         "selected #556 original ColdSessionHandle accepts host-derived file");
      for(int i=0;i<2;i++){
        var body=fresh.PartBody(i);
        Check(logical.Current!=null&&logical.Current.Snapshot.Parts[i].Id==fresh.PartId(i),
          "actual stable ID survives process-style fresh cold load");
        Check(logical.Current.ContainsConsumedId(i,fresh.PartId(i)),
          "genuine original W06 cold consumed status exists logically");
        Check(!fresh.IsDetached(i)&&fresh.AppliedCount(i)==0&&body.isKinematic&&
          body.transform.parent!=null,
          "BRIDGE_MISSING: newly built original #510 host unhydrated after accepted binary file");
        Check(body.transform.position!=(i==0?p0:p1),
          "BRIDGE_MISSING: original saved Rigidbody pose was not installed");
        for(int route=0;route<3;route++){
          var parent=body.transform.parent;var velocity=body.linearVelocity;
          bool result=route==0?fresh.Reattach(i):route==1?fresh.TrySafeReattach(i):fresh.ReattachFromGui(i);
          Check(!result&&fresh.LastDockResult==DockResult.NotDetached,
             "all actual public docking routes fail closed without original physical restoration");
          Check(parent==body.transform.parent&&velocity==body.linearVelocity,
             "denied route does not damage fresh body state");
        }
        lost++;
      }
    } finally {if(File.Exists(path))File.Delete(path);}
 }
 static void InvalidSaveCannotAlterLiveHost(){
    var active=NewHost();Detach(active,0);
    var b=active.PartBody(0);var parent=b.transform.parent;
    var position=b.transform.position;var velocity=b.linearVelocity;
    var good=ActualSave(active,16);var cold=new ColdSessionHandle();
    Check(cold.TrySwap(good,771105,16,()=>true)==RestoreVerdict.Accepted,"initial logical session");
    var old=cold.Current;
    void Reject(byte[] raw,long run,long min,Func<bool> idle,RestoreVerdict expected){
      Check(cold.TrySwap(raw,run,min,idle)==expected,"original #556 reject faulty swap");
      Check(object.ReferenceEquals(cold.Current,old),"original logical session pointer unchanged");
      Check(active.IsDetached(0)&&active.AppliedCount(0)==1&&b.transform.parent==parent&&
        b.transform.position==position&&b.linearVelocity==velocity,
        "original physical host unchanged on rejected transaction");
      denials++;
    }
    var corrupt=(byte[])good.Clone();corrupt[36]^=1;
    Reject(corrupt,771105,16,()=>true,RestoreVerdict.InvalidSnapshot);
    Reject(good,771106,16,()=>true,RestoreVerdict.WrongRun);
    Reject(good,771105,17,()=>true,RestoreVerdict.StaleGeneration);
    Reject(good,771105,16,()=>false,RestoreVerdict.TransactionBusy);
    Reject(good,771105,16,()=>throw new Exception("injected"),RestoreVerdict.Quarantine);
    Reject(good.Take(good.Length-2).ToArray(),771105,16,()=>true,RestoreVerdict.InvalidSnapshot);
 }
 static void NoMutationOrUnsafePublicRestore(){
    var live=NewHost();Detach(live,0);
    var bytes=ActualSave(live,18);
    Check(SnapshotCodec.TryDecode(bytes,out RunSnapshot original)&&original!=null,"valid control save");
    var originalPart=original.Parts[0];
    var fake=new PersistedPart(2310002,originalPart.OwnerId,originalPart.ParentId,
      originalPart.AssetGuid,originalPart.SocketGuid,originalPart.State,originalPart.Consumed,
      originalPart.Kinematic,originalPart.Position,0,0,0,1,
      originalPart.LinearVelocity,originalPart.AngularVelocity,originalPart.MassKg);
    bool rejected=false;
    try{SnapshotCodec.Encode(new RunSnapshot(original.RunId,original.Generation,
      new[]{fake,original.Parts[1]}));}catch(InvalidDataException){rejected=true;}
    Check(rejected,"producer #556 rejects duplicated ID/foreign owner before binary commit");
    Check(live.AppliedCount(0)==1,"forged ID does not affect original #510 live ledger");
    var publicHost=typeof(SecureDebrisPlaygroundV2).GetMethods(
       BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly);
    Check(publicHost.All(m=>!m.Name.Contains("Restore")&&!m.Name.Contains("Hydrat")&&
       !m.Name.Contains("Load")&&m.ReturnType!=typeof(AtomicDockLedger)),
       "frozen selected game #510 has no public authoritative rehydrate bridge");
 }
 public static int Main(string[] args){
   try{
     Case("actual original #510 host to #556 codec all four Light Heavy states",FourStates);
     Case("physical disk save to fresh 510 host with actual 556 logical cold load",ActualDiskColdReload);
     Case("six original rejected save attempts preserve live physical rig",InvalidSaveCannotAlterLiveHost);
     Case("foreign duplicate ID reject and no public authoritative live ledger hydrate",NoMutationOrUnsafePublicRestore);
     Console.WriteLine("W15_ACTUAL_510_PLUS_556_SOURCE_BRIDGE_GAP_WITNESSED cases="+cases+
       " assertions="+checks+" live_physical_unrestored="+lost+" rejected_loads="+denials+
       " ORIGINAL_W06=YES QA_UNITY_BOUNDARY=SIMULATED UNITY_G1=NOT_RUN");
     if(args.Length>0&&args[0]=="--release"){
       Console.WriteLine("LIVE_SAVELOAD_RELEASE_HOLD=NO_AUTHORIZED_ORIGINAL_510_PRIVATE_RIG_LEDGER_PHYSICAL_RESTORE");
       return 3;
     }
     return 0;
   }catch(Exception ex){Console.Error.WriteLine("W15_QA_INFRA_FAILURE "+ex);return 2;}
 }
}

using System;using System.Collections.Generic;using System.Linq;using System.IO;using System.Text.Json;
using RoboRougelike.B01.Rewards;
sealed class Host:IAuthoritativeRewardPort {
 public HashSet<string> Owned=new(StringComparer.Ordinal),Wins=new(StringComparer.Ordinal),Done=new(StringComparer.Ordinal);
 public bool Block,Broken;public int Commits;
 public IReadOnlyCollection<string> ExistingPhysicalIds=>Owned;
 public bool IsVerifiedVictory(string e)=>Wins.Contains(e);
 public bool WasCommitRecorded(string e)=>Done.Contains(e);
 public bool TryCommitSalvageAtomically(string e,IReadOnlyList<RewardCandidate> c,out string why){
  if(Block){why="HOST_BUSY";return false;}
  if(Done.Contains(e)||c.Any(p=>Owned.Contains(p.InstanceId))){why="HOST_DUPLICATE";return false;}
  foreach(var p in c)Owned.Add(p.InstanceId);
  if(!Broken)Done.Add(e);
  Commits++;why="OK";return true;
 }
}
static class Program {
 static int serial,pass,fail;static readonly List<object> Cases=new();
 static string Id()=>(++serial).ToString("x32");
 static RewardCandidate P(string kind="Gun",string def=null,float hp=1,string id=null)=>
  new(id??Id(),kind,def,hp);
 static Host H(string win="v"){var h=new Host();h.Wins.Add(win);return h;}
 static RewardKeep3Service S(Host h)=>new(h);
 static void A(bool x,string msg="failed"){if(!x)throw new Exception(msg);}
 static void E<T>(T x,T y){A(EqualityComparer<T>.Default.Equals(x,y),$"want {y}, got {x}");}
 static void Bad(bool x,string got,string want){A(!x);E(got,want);}
 static void T(string name,Action f){
  try{f();pass++;Cases.Add(new{name,status="PASS"});Console.WriteLine("PASS "+name);}
  catch(Exception ex){fail++;Cases.Add(new{name,status="FAIL",error=ex.Message});Console.Error.WriteLine("FAIL "+name+" "+ex);}
 }
 static void Open(RewardKeep3Service s,params RewardCandidate[] p){A(s.TryOpen("v",p,out var e),e);}
 static void Pick(RewardKeep3Service s,RewardCandidate p){A(s.TryToggleSelection(p.InstanceId,out var e),e);}
 static void Commit(RewardKeep3Service s){A(s.TryCommit(out var e),e);}
 static string Save(RewardKeep3Service s){A(s.TrySave(out var j,out var e),e);return j;}
 static int Main(string[] a){
  T("zero_selected_is_valid",()=>{var h=H();var s=S(h);Open(s,P("Armor"));Commit(s);E(h.Commits,1);E(h.Owned.Count,0);});
  T("one_owned_after_choice",()=>{var h=H();var s=S(h);var p=P();Open(s,p);Pick(s,p);Commit(s);A(h.Owned.Contains(p.InstanceId));});
  T("three_kept",()=>{var h=H();var s=S(h);var p=new[]{P(),P("Wheel"),P("HoverEngine")};Open(s,p);foreach(var x in p)Pick(s,x);Commit(s);E(h.Owned.Count,3);});
  T("fourth_rejected_without_loss",()=>{var h=H();var s=S(h);var p=new[]{P(),P(),P(),P()};Open(s,p);foreach(var x in p.Take(3))Pick(s,x);Bad(s.TryToggleSelection(p[3].InstanceId,out var e),e,"KEEP_LIMIT_3");Commit(s);E(h.Owned.Count,3);});
  T("armor_refused",()=>{var h=H();var s=S(h);var p=P("Armor");Open(s,p);Bad(s.TryToggleSelection(p.InstanceId,out var e),e,"ARMOR_EXCLUDED");E(s.SelectedCount,0);});
  T("unverified_victory_refused",()=>{var h=new Host();var s=S(h);Bad(s.TryOpen("v",new[]{P()},out var e),e,"VICTORY_NOT_VERIFIED");});
  T("replayed_event_refused",()=>{var h=H();var s=S(h);Open(s,P());Commit(s);Bad(s.TryOpen("v",new[]{P()},out var e),e,"ENCOUNTER_ALREADY_COMMITTED");E(h.Commits,1);});
  T("same_legend_different_instances_allowed",()=>{var h=H();var s=S(h);var x=P("Gun","LEG-WPN-001");var y=P("Gun","LEG-WPN-001");Open(s,x,y);Pick(s,x);Pick(s,y);Commit(s);E(h.Owned.Count,2);});
  T("duplicate_offer_refused",()=>{var h=H();var s=S(h);var p=P();Bad(s.TryOpen("v",new[]{p,p.Copy()},out var e),e,"INVALID_OR_DUPLICATE_OFFER");});
  T("existing_physical_guid_refused",()=>{var h=H();var p=P();h.Owned.Add(p.InstanceId);var s=S(h);Bad(s.TryOpen("v",new[]{p},out var e),e,"OFFER_CONTAINS_OWNED_INSTANCE");});
  T("uppercase_guid_refused",()=>{var h=H();var s=S(h);Bad(s.TryOpen("v",new[]{P(id:"ABCDEF00112233445566778899AABBCC")},out var e),e,"INVALID_OR_DUPLICATE_OFFER");});
  T("empty_guid_refused",()=>{var h=H();var s=S(h);Bad(s.TryOpen("v",new[]{P(id:Guid.Empty.ToString("N"))},out var e),e,"INVALID_OR_DUPLICATE_OFFER");});
  T("core_not_salvageable",()=>{var h=H();var s=S(h);Bad(s.TryOpen("v",new[]{P("Core")},out var e),e,"INVALID_OR_DUPLICATE_OFFER");});
  T("second_window_cannot_replace_open_choice",()=>{var h=H();h.Wins.Add("other");var s=S(h);Open(s,P());var save=Save(s);Bad(s.TryOpen("other",new[]{P()},out var e),e,"WINDOW_ALREADY_OPEN");E(Save(s),save);});
  T("deselect_releases_slot",()=>{var h=H();var s=S(h);var p=new[]{P(),P(),P(),P()};Open(s,p);foreach(var x in p.Take(3))Pick(s,x);Pick(s,p[0]);Pick(s,p[3]);Commit(s);A(!h.Owned.Contains(p[0].InstanceId));E(h.Owned.Count,3);});
  T("host_failure_retriable_without_loss",()=>{var h=H();h.Block=true;var s=S(h);var p=P();Open(s,p);Pick(s,p);var before=Save(s);Bad(s.TryCommit(out var e),e,"HOST_BUSY");E(Save(s),before);E(h.Owned.Count,0);h.Block=false;Commit(s);E(h.Owned.Count,1);});
  T("host_partial_commit_quarantines",()=>{var h=H();h.Broken=true;var s=S(h);var p=P();Open(s,p);Pick(s,p);Bad(s.TryCommit(out var e),e,"HOST_REWARD_CONTRACT_BROKEN");A(s.IsQuarantined);});
  T("save_reload_open_selection",()=>{var h=H();var s=S(h);var p=P();Open(s,p);Pick(s,p);var r=S(h);A(r.TryRestore(Save(s),out var e),e);E(r.SelectedCount,1);Commit(r);A(h.Owned.Contains(p.InstanceId));});
  T("corrupt_save_armor_selection",()=>{var h=H();var s=S(h);var p=P("Armor");Open(s,p);var snapshot=UnityEngine.JsonUtility.FromJson<RewardWindowSave>(Save(s));snapshot.SelectedPhysicalIds=new[]{p.InstanceId};var r=S(h);Bad(r.TryRestore(UnityEngine.JsonUtility.ToJson(snapshot),out var e),e,"CORRUPT_SELECTION");});
  T("duplicate_completed_event_in_save",()=>{var h=H();var s=S(h);Open(s,P());Commit(s);var data=UnityEngine.JsonUtility.FromJson<RewardWindowSave>(Save(s));data.CompletedEncounterIds=new[]{"v","v"};var r=S(h);Bad(r.TryRestore(UnityEngine.JsonUtility.ToJson(data),out var e),e,"CORRUPT_COMMITTED_EVENT");});
  T("zero_hp_kept_as_zero_hp",()=>{var h=H();var s=S(h);var p=P(hp:0);Open(s,p);Pick(s,p);Commit(s);E(p.Health,0f);A(h.Owned.Contains(p.InstanceId));});
  T("options_copy_does_not_change_real_item",()=>{var h=H();var s=S(h);var p=P();Open(s,p);s.VisibleOptions[0].PartKind="Armor";Pick(s,p);Commit(s);E(h.Owned.Count,1);});
  T("seeded_3000_choice_cycles",()=>{
    var h=H();var s=S(h);var rnd=new Random(25022);
    for(int i=0;i<3000;i++){
      var evt="victory-"+i;h.Wins.Add(evt);var choices=Enumerable.Range(0,6).Select(j=>P(j==0?"Armor":"Gun")).ToArray();
      A(s.TryOpen(evt,choices,out var error),error);
      foreach(var p in choices.Skip(1).OrderBy(_=>rnd.Next()).Take(rnd.Next(4)))Pick(s,p);
      if(i%101==0){var r=S(h);A(r.TryRestore(Save(s),out error),error);s=r;}
      Commit(s);A(h.WasCommitRecorded(evt));
    }
    E(h.Commits,3000);A(!s.IsQuarantined);
  });
  var output=a.Length>0?a[0]:"artifacts/reward-keep3/results.json";
  Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
  File.WriteAllText(output,JsonSerializer.Serialize(new{schema="b01.keep3.test.v1",total=pass+fail,passed=pass,failed=fail,unityNative="NOT_RUN",cases=Cases},new JsonSerializerOptions{WriteIndented=true}));
  Console.WriteLine($"RESULT {pass}/{pass+fail} PASS, failed={fail} NATIVE_UNITY_NOT_RUN");return fail==0?0:1;
 }
}

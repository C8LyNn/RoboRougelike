using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using RoboRougelike.B01.Economy.Rebuild;

sealed class R3Mock : IAuthoritativeRebuildPort
{
 public readonly List<RebuildPartView> Parts=new();
 public readonly Dictionary<string,string> Reservations=new(StringComparer.Ordinal);
 public int Scrap=350,Purchase=100,PaidQuote=70,Payments,Completes;
 public bool RejectReservation,RejectBatch,BreakPostcondition;
 public IReadOnlyList<RebuildPartView> OwnedParts=>Parts;
 public int PurchasePrice(string id)=>Purchase;
 public int PaidRebuildQuote(string id)=>PaidQuote;
 public bool HasReservation(string id,string key)=>Reservations.TryGetValue(id,out var value)&&value==key;
 public bool TryReserveAndDebitAtomically(string id,int charge,string key,out string reason)
 {
  var p=Parts.FirstOrDefault(x=>x.InstanceId==id);
  if(RejectReservation||p==null||p.Health>0||Reservations.ContainsKey(id))
  {reason="RESERVATION_REJECTED";return false;}
  if(charge>Scrap){reason="INSUFFICIENT_SCRAP";return false;}
  Scrap-=charge;Reservations[id]=key;Payments++;reason="OK";return true;
 }
 public bool TryCompleteReservedBatchAtomically(string eventId,IReadOnlyList<RebuildTicket> items,out string reason)
 {
  if(RejectBatch||items.Any(t=>!HasReservation(t.InstanceId,t.ReservationKey)))
  {reason="BATCH_REJECTED";return false;}
  foreach(var t in items) {
   var p=Parts.Single(x=>x.InstanceId==t.InstanceId);
   p.Health=1;
   if(!BreakPostcondition)Reservations.Remove(t.InstanceId);
  }
  Completes++;reason="OK";return true;
 }
 public string NewDestroyed()
 {
  var id=Guid.NewGuid().ToString("N");Parts.Add(new RebuildPartView(id,"Gun",0));return id;
 }
}
static class Program
{
 static readonly List<object> Results=new();
 static int good,bad;
 static void Check(bool ok,string description="failed assertion") {
  if(!ok)throw new Exception(description);
 }
 static void Eq<T>(T a,T b){Check(EqualityComparer<T>.Default.Equals(a,b),$"wanted={b} got={a}");}
 static void Case(string name,Action a) {
  try{a();good++;Results.Add(new{name,status="PASS"});Console.WriteLine("PASS "+name);}
  catch(Exception e){bad++;Results.Add(new{name,status="FAIL",message=e.Message});
    Console.Error.WriteLine("FAIL "+name+": "+e);}
 }
 static string Saved(RebuildQueueService q){Check(q.TrySave(out var json,out var error),error);return json;}
 static void Queue(RebuildQueueService q,string id){Check(q.TryQueueDestroyed(id,out var error),error);}
 static void Victory(RebuildQueueService q,string eventId){Check(q.TryFinishVictory(eventId,true,out var error),error);}
 static void Refuse(bool accepted,string reason,string wanted){Check(!accepted);Eq(reason,wanted);}
 static int Main(string[] args)
 {
  Case("one_free_rebuild_exactly_next_victory",()=>{
    var h=new R3Mock();var id=h.NewDestroyed();var q=new RebuildQueueService(h);
    Queue(q,id);Eq(h.Scrap,350);Eq(h.Payments,1);Check(q.IsRebuilding(id));
    Refuse(q.TryFinishVictory("loss",false,out var e),e,"NO_VICTORY_NO_PROGRESS");
    Check(q.IsRebuilding(id));Eq(h.Parts.Single().Health,0f);
    Victory(q,"w1");Eq(q.WonChallenges,1);Eq(h.Parts.Single().Health,1f);
    Check(!q.IsRebuilding(id));Check(q.FreeSlotAvailable);Eq(h.Completes,1);
  });
  Case("second_rebuild_paid_below_purchase",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();var b=h.NewDestroyed();var q=new RebuildQueueService(h);
    Queue(q,a);Queue(q,b);Eq(h.Scrap,280);Eq(q.PendingCount,2);
    Eq(h.Payments,2);Victory(q,"w1");Eq(h.Completes,1);
    Check(h.Parts.All(p=>p.Health==1));Eq(h.Scrap,280);
  });
  Case("zero_hp_repair_never_automatic",()=>{
    var h=new R3Mock();var id=h.NewDestroyed();var q=new RebuildQueueService(h);
    Queue(q,id);Check(h.Parts.Single().Health==0);
    Refuse(q.TryFinishVictory("retry-loss",false,out var e),e,"NO_VICTORY_NO_PROGRESS");
    Eq(h.Parts.Single().Health,0f);
  });
  Case("replay_victory_does_not_refund_or_advance",()=>{
    var h=new R3Mock();var id=h.NewDestroyed();var q=new RebuildQueueService(h);
    Queue(q,id);Victory(q,"first");
    Refuse(q.TryFinishVictory("first",true,out var e),e,"VICTORY_EVENT_REPLAY");
    Eq(q.WonChallenges,1);Eq(h.Completes,1);Eq(h.Payments,1);
  });
  Case("restart_scene_reused_free_slot_rejected",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();var b=h.NewDestroyed();var q=new RebuildQueueService(h);
    Queue(q,a);var before=Saved(q);
    var restored=new RebuildQueueService(h);
    Check(restored.TryRestore(before,out var e),e);
    Check(!restored.FreeSlotAvailable);Queue(restored,b);Eq(h.Scrap,280);
  });
  Case("missing_funds_atomic_refusal",()=>{
    var h=new R3Mock(){Scrap=0};var a=h.NewDestroyed();var b=h.NewDestroyed();
    var q=new RebuildQueueService(h);Queue(q,a);
    string before=Saved(q);Eq(q.PendingCount,1);
    Refuse(q.TryQueueDestroyed(b,out var e),e,"INSUFFICIENT_SCRAP");
    Eq(h.Scrap,0);Eq(q.PendingCount,1);Eq(Saved(q),before);
    Check(!h.Reservations.ContainsKey(b));
  });
  Case("reject_negative_or_non_discount_quote",()=>{
    foreach(var x in new[]{0,100,120,-3}){
      var h=new R3Mock(){PaidQuote=x};var id=h.NewDestroyed();var q=new RebuildQueueService(h);
      Refuse(q.TryQueueDestroyed(id,out var e),e,"INVALID_REBUILD_QUOTE");
      Eq(h.Payments,0);Eq(h.Scrap,350);
    }
  });
  Case("reservation_host_refusal_lossless",()=>{
    var h=new R3Mock(){RejectReservation=true};var id=h.NewDestroyed();var q=new RebuildQueueService(h);
    Refuse(q.TryQueueDestroyed(id,out var e),e,"RESERVATION_REJECTED");
    Eq(q.PendingCount,0);Eq(h.Scrap,350);
  });
  Case("batch_failure_preserves_tickets_for_same_win_retry",()=>{
    var h=new R3Mock(){RejectBatch=true};var a=h.NewDestroyed();var b=h.NewDestroyed();
    var q=new RebuildQueueService(h);Queue(q,a);Queue(q,b);
    Refuse(q.TryFinishVictory("valid-win",true,out var e),e,"BATCH_REJECTED");
    Eq(q.WonChallenges,0);Eq(q.PendingCount,2);Eq(h.Completes,0);
    h.RejectBatch=false;Victory(q,"valid-win");Eq(q.WonChallenges,1);
    Eq(q.PendingCount,0);Eq(h.Completes,1);
  });
  Case("host_postcondition_break_quarantined",()=>{
    var h=new R3Mock(){BreakPostcondition=true};var id=h.NewDestroyed();var q=new RebuildQueueService(h);
    Queue(q,id);
    Refuse(q.TryFinishVictory("w1",true,out var e),e,"HOST_BATCH_CONTRACT_BROKEN");
    Check(q.IsQuarantined);
    Refuse(q.TryFinishVictory("w1",true,out e),e,"REBUILD_QUARANTINED");
    Check(!q.TrySave(out _,out e));Eq(e,"REBUILD_QUARANTINED");
  });
  Case("one_free_per_real_victory_epoch",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();var b=h.NewDestroyed();var q=new RebuildQueueService(h);
    Queue(q,a);Victory(q,"win-one");Queue(q,b);Eq(h.Scrap,350);
    Victory(q,"win-two");Eq(q.WonChallenges,2);Eq(h.Completes,2);
    var secondSave=Saved(q);var reloaded=new RebuildQueueService(h);
    Check(reloaded.TryRestore(secondSave,out var e),e);Check(reloaded.FreeSlotAvailable);
  });
  Case("paid_next_cycle_after_free",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();var b=h.NewDestroyed();var c=h.NewDestroyed();var q=new RebuildQueueService(h);
    Queue(q,a);Queue(q,b);Victory(q,"w1");Queue(q,c);Eq(h.Scrap,280);
  });
  Case("no_inventory_dupe_destroyed_identity_survives",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();var q=new RebuildQueueService(h);
    Queue(q,a);Victory(q,"w1");Eq(h.Parts.Single().InstanceId,a);Eq(h.Parts.Count,1);
  });
  Case("already_rebuilding_duplicate_ticket_rejected",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();var q=new RebuildQueueService(h);Queue(q,a);
    Refuse(q.TryQueueDestroyed(a,out var e),e,"ALREADY_REBUILDING");Eq(h.Payments,1);
  });
  Case("healthy_parts_cannot_queue",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();h.Parts.Single().Health=.25f;
    var q=new RebuildQueueService(h);
    Refuse(q.TryQueueDestroyed(a,out var e),e,"NOT_DESTROYED");Eq(h.Payments,0);
  });
  Case("duplicate_physical_guid_reject",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();
    h.Parts.Add(new RebuildPartView(a,"Wheel",0));
    var q=new RebuildQueueService(h);
    Refuse(q.TryQueueDestroyed(a,out var e),e,"DUPLICATE_PHYSICAL_INSTANCE");
  });
  Case("noncanonical_guid_alias_and_empty_reject",()=>{
    var h=new R3Mock();h.Parts.Add(new RebuildPartView(Guid.Empty.ToString("N"),"Gun",0));
    var q=new RebuildQueueService(h);
    Refuse(q.TryQueueDestroyed(h.Parts[0].InstanceId,out var e),e,"INVALID_HOST_PART");
    h.Parts.Clear();var a=h.NewDestroyed();h.Parts[0].InstanceId=a.ToUpperInvariant();
    Refuse(q.TryQueueDestroyed(a,out e),e,"INVALID_HOST_PART");
  });
  Case("persistence_rejects_missing_reservation",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();var q=new RebuildQueueService(h);
    Queue(q,a);var s=Saved(q);h.Reservations.Clear();
    var restored=new RebuildQueueService(h);
    Refuse(restored.TryRestore(s,out var e),e,"UNMATCHED_HOST_RESERVATION");
  });
  Case("persistence_tampered_duplicate_ticket",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();var q=new RebuildQueueService(h);Queue(q,a);
    var data=UnityEngine.JsonUtility.FromJson<RebuildSave>(Saved(q));
    data.Pending=new[]{data.Pending[0],data.Pending[0]};
    var fresh=new RebuildQueueService(h);
    Refuse(fresh.TryRestore(UnityEngine.JsonUtility.ToJson(data),out var e),e,"INVALID_OR_DUPLICATE_TICKET");
    Eq(fresh.PendingCount,0);
  });
  Case("persistence_free_slot_forgery_rejected",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();var q=new RebuildQueueService(h);Queue(q,a);
    var data=UnityEngine.JsonUtility.FromJson<RebuildSave>(Saved(q));
    data.FreeSlotUsedAtWin=-1;
    var fresh=new RebuildQueueService(h);
    Refuse(fresh.TryRestore(UnityEngine.JsonUtility.ToJson(data),out var e),e,"FREE_SLOT_REPLAY_IN_SAVE");
  });
  Case("persistence_replayed_win_event_rejected",()=>{
    var h=new R3Mock();var q=new RebuildQueueService(h);Victory(q,"win1");
    var data=UnityEngine.JsonUtility.FromJson<RebuildSave>(Saved(q));
    data.AppliedVictoryEvents=new[]{"win1","win1"};
    var t=new RebuildQueueService(h);
    Refuse(t.TryRestore(UnityEngine.JsonUtility.ToJson(data),out var e),e,"REPLAYED_OR_INVALID_EVENT_IN_SAVE");
  });
  Case("load_corruption_does_not_reset_current_state",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();var q=new RebuildQueueService(h);Queue(q,a);
    var original=Saved(q);
    Refuse(q.TryRestore("{",out var e),e,"CORRUPT_REBUILD_SAVE");
    Eq(Saved(q),original);Eq(q.PendingCount,1);
  });
  Case("missing_owned_part_prevents_forged_finish",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();var q=new RebuildQueueService(h);Queue(q,a);
    h.Parts.Clear();
    Refuse(q.TryFinishVictory("win",true,out var e),e,"RESERVATION_STATE_MISMATCH");
    Eq(q.WonChallenges,0);Eq(h.Completes,0);
  });
  Case("defeat_seed_change_no_progress",()=>{
    var h=new R3Mock();var a=h.NewDestroyed();var q=new RebuildQueueService(h);Queue(q,a);
    for(int i=0;i<200;i++)Check(!q.TryFinishVictory("defeat-"+i,false,out _));
    Eq(q.WonChallenges,0);Eq(q.PendingCount,1);Eq(h.Parts[0].Health,0f);
  });
  Case("seeded_3000_queue_victory_replay_roundtrips",()=>{
    var h=new R3Mock(){Scrap=100000};var q=new RebuildQueueService(h);
    var rnd=new Random(136222);var names=new List<string>();
    for(int i=0;i<12;i++)names.Add(h.NewDestroyed());
    for(int i=0;i<3000;i++){
      if(rnd.Next(4)==0){
        var id=names[rnd.Next(names.Count)];
        if(h.Parts.Any(p=>p.InstanceId==id&&p.Health==1))h.Parts.Single(p=>p.InstanceId==id).Health=0;
        q.TryQueueDestroyed(id,out _);
      }else if(rnd.Next(3)==0)q.TryFinishVictory("v-"+i,true,out _);
      else q.TryFinishVictory("d-"+i,false,out _);
      Check(!q.IsQuarantined);
      string state=Saved(q);
      if(i%67==0){
        var r=new RebuildQueueService(h);
        Check(r.TryRestore(state,out var e),e);
        Eq(r.PendingCount,q.PendingCount);
        q=r;
      }
      Check(h.Parts.Select(p=>p.InstanceId).Distinct().Count()==h.Parts.Count);
    }
  });
  string target=args.Length>0?args[0]:"artifacts/econ-rebuild/test-results.json";
  Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target))!);
  File.WriteAllText(target,JsonSerializer.Serialize(new{
    schema="b01.econ-rebuild.surrogate.v1",
    nativeUnity="NOT_RUN",total=good+bad,passed=good,failed=bad,cases=Results},new JsonSerializerOptions{WriteIndented=true}));
  Console.WriteLine($"RESULT {good}/{good+bad} PASS; failed={bad}; NATIVE_UNITY=NOT_RUN");
  return bad==0?0:1;
 }
}

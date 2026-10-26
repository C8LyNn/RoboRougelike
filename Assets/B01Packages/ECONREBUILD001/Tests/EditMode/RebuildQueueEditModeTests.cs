using System;
using System.Collections.Generic;
using NUnit.Framework;
using RoboRougelike.B01.Economy.Rebuild;
namespace RoboRougelike.B01.Economy.Rebuild.Tests
{
 public class RebuildQueueEditModeTests
 {
  sealed class OwnedHost:IAuthoritativeRebuildPort {
   public readonly List<RebuildPartView> Parts=new List<RebuildPartView>();
   public readonly Dictionary<string,string> Reservations=new Dictionary<string,string>();
   public int Scrap=200;
   public IReadOnlyList<RebuildPartView> OwnedParts=>Parts;
   public int PurchasePrice(string id)=>100;
   public int PaidRebuildQuote(string id)=>70; // test hypothesis ONLY
   public bool HasReservation(string id,string token)=>Reservations.TryGetValue(id,out var value)&&value==token;
   public bool TryReserveAndDebitAtomically(string id,int cost,string token,out string error){
    if(cost>Scrap){error="NO_CASH";return false;}
    Scrap-=cost;Reservations[id]=token;error="OK";return true;
   }
   public bool TryCompleteReservedBatchAtomically(string victory,IReadOnlyList<RebuildTicket> tickets,out string error){
    foreach(var t in tickets){
     if(!HasReservation(t.InstanceId,t.ReservationKey)){error="NOT_RESERVED";return false;}
    }
    foreach(var t in tickets){Parts.Find(p=>p.InstanceId==t.InstanceId).Health=1;Reservations.Remove(t.InstanceId);}
    error="OK";return true;
   }
  }
  [Test] public void FreeThenPaidRequiresVictory() {
   var host=new OwnedHost();
   var a=Guid.NewGuid().ToString("N");var b=Guid.NewGuid().ToString("N");
   host.Parts.Add(new RebuildPartView(a,"Gun",0));host.Parts.Add(new RebuildPartView(b,"Wheel",0));
   var q=new RebuildQueueService(host);string err;
   Assert.IsTrue(q.TryQueueDestroyed(a,out err),err);
   Assert.IsTrue(q.TryQueueDestroyed(b,out err),err);
   Assert.AreEqual(130,host.Scrap);
   Assert.IsFalse(q.TryFinishVictory("lost",false,out err));Assert.AreEqual(0,q.WonChallenges);
   Assert.IsTrue(q.TryFinishVictory("win",true,out err),err);
   Assert.AreEqual(2,host.Parts.Count);Assert.AreEqual(0,q.PendingCount);
  }
  [Test] public void DuplicateWinBlocked() {
   var host=new OwnedHost();var q=new RebuildQueueService(host);string e;
   Assert.IsTrue(q.TryFinishVictory("win",true,out e),e);
   Assert.IsFalse(q.TryFinishVictory("win",true,out e));Assert.AreEqual("VICTORY_EVENT_REPLAY",e);
  }
  [Test] public void SaveRestorePreservesQueue() {
   var host=new OwnedHost();var id=Guid.NewGuid().ToString("N");host.Parts.Add(new RebuildPartView(id,"Gun",0));
   var q=new RebuildQueueService(host);string e,json;
   Assert.IsTrue(q.TryQueueDestroyed(id,out e),e);
   Assert.IsTrue(q.TrySave(out json,out e),e);
   var reloaded=new RebuildQueueService(host);
   Assert.IsTrue(reloaded.TryRestore(json,out e),e);
   Assert.IsFalse(reloaded.FreeSlotAvailable);Assert.IsTrue(reloaded.IsRebuilding(id));
  }
 }
}

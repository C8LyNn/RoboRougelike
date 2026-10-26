// Receiver example only (Tools/**), NOT imported in standalone Unity ZIP.
// Written against OBJ #210 R3VehicleModel public members. No unapproved live host hooks.
using System;
using System.Collections.Generic;
using System.Linq;
using RoboRougelike.R3;
using RoboRougelike.B01.Economy.Rebuild;
public interface IAtomicRebuildR3Hooks
{
  // Must journal alongside the original R3 wallet + part save atomically.
  bool ReserveAndDebit(R3PartState ownedPart,int cost,string reservationKey,out string reason);
  bool HasActiveReservation(string physicalPartId,string reservationKey);
  // All-or-nothing restored to 1HP without extra R3 Repair() charge.
  bool CompleteBatch(string genuineVictoryEventId,IReadOnlyList<RebuildTicket> tickets,out string reason);
  int PaidQuoteFor(R3PartState physicalPart); // B01-10 final value unapproved
}
public sealed class EconRebuildR3Port : IAuthoritativeRebuildPort
{
 private readonly R3VehicleModel model;
 private readonly IAtomicRebuildR3Hooks hooks;
 public EconRebuildR3Port(R3VehicleModel liveOwner,IAtomicRebuildR3Hooks hostHooks)
 {model=liveOwner??throw new ArgumentNullException(nameof(liveOwner));hooks=hostHooks;}
 private R3PartState Find(string id)=>model.Parts.Concat(model.Inventory).FirstOrDefault(x=>x.Id==id);
 public IReadOnlyList<RebuildPartView> OwnedParts =>
     model.Parts.Concat(model.Inventory).Select(p=>new RebuildPartView(p.Id,p.Kind.ToString(),p.Health)).ToArray();
 public int PurchasePrice(string id) {var p=Find(id);return p==null?0:model.Price(p.Kind,p.Weapon);}
 public int PaidRebuildQuote(string id){var p=Find(id);return p==null||hooks==null?0:hooks.PaidQuoteFor(p);}
 public bool HasReservation(string id,string token)=>hooks!=null&&hooks.HasActiveReservation(id,token);
 public bool TryReserveAndDebitAtomically(string id,int debit,string token,out string reason)
 {
   var p=Find(id);
   if(hooks==null||p==null){reason="R3_REBUILD_HOOK_PENDING";return false;}
   return hooks.ReserveAndDebit(p,debit,token,out reason);
 }
 public bool TryCompleteReservedBatchAtomically(string victory,IReadOnlyList<RebuildTicket> eligible,out string reason)
 {
   if(hooks==null){reason="R3_REBUILD_HOOK_PENDING";return false;}
   return hooks.CompleteBatch(victory,eligible,out reason);
 }
}

// B01-22 ECON-REBUILD-001 R1 — isolated Unity6 component, not an R3 inventory.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoboRougelike.B01.Economy.Rebuild
{
    [Serializable] public sealed class RebuildPartView
    {
        public string InstanceId, PartKind;
        public float Health;
        public RebuildPartView(string id,string kind,float hp)
        { InstanceId=id;PartKind=kind;Health=hp; }
    }

    // Port MUST implement monetary debit and reservation atomically in authoritative R3.
    // Do not implement this with raw R3 Repair(): it heals NOW and charges the WRONG price.
    public interface IAuthoritativeRebuildPort
    {
        IReadOnlyList<RebuildPartView> OwnedParts {get;} // live R3 Parts + existing Inventory, not copies owned twice
        int PurchasePrice(string instanceId); // authoritative dynamic current price; > 0
        int PaidRebuildQuote(string instanceId); // authoritative quote; 0 < quote < purchase
        bool HasReservation(string instanceId,string reservationKey);
        bool TryReserveAndDebitAtomically(string instanceId,int charge,string reservationKey,out string reason);
        bool TryCompleteReservedBatchAtomically(string victoryEventId,
            IReadOnlyList<RebuildTicket> eligible,out string reason); // same physical IDs, HP restored, charges exactly once
    }

    [Serializable] public sealed class RebuildTicket
    {
        public string InstanceId, ReservationKey;
        public int OrderedAfterWins, ChargedScrap, PurchasePriceAtOrder, PaidQuoteAtOrder;
        public bool UsedFreeSlot;
        public RebuildTicket() {}
        public RebuildTicket(string id,string key,int victories,int charged,int purchase,int quote,bool isFree)
        { InstanceId=id;ReservationKey=key;OrderedAfterWins=victories;ChargedScrap=charged;
          PurchasePriceAtOrder=purchase;PaidQuoteAtOrder=quote;UsedFreeSlot=isFree; }
    }

    [Serializable] public sealed class RebuildSave
    {
        public int Version=1, Wins;
        public int FreeSlotUsedAtWin=-1;
        public RebuildTicket[] Pending;
        public string[] AppliedVictoryEvents;
        public RebuildSave(){}
    }

    public sealed class RebuildQueueService
    {
        private readonly object sync=new object();
        private readonly IAuthoritativeRebuildPort host;
        private readonly Dictionary<string,RebuildTicket> pending=
            new Dictionary<string,RebuildTicket>(StringComparer.Ordinal);
        private readonly HashSet<string> completedEvents=new HashSet<string>(StringComparer.Ordinal);
        private int wins,freeUsedAt=-1;
        private bool quarantined;
        public int WonChallenges {get {lock(sync)return wins;} }
        public int PendingCount {get {lock(sync)return pending.Count;} }
        public bool IsQuarantined {get {lock(sync)return quarantined;} }
        public bool FreeSlotAvailable {get {lock(sync)return freeUsedAt!=wins;} }

        public RebuildQueueService(IAuthoritativeRebuildPort authoritativeHost)
        { host=authoritativeHost??throw new ArgumentNullException(nameof(authoritativeHost)); }

        private static bool PhysicalId(string id)
        {
            Guid parsed;
            return !string.IsNullOrWhiteSpace(id)&&Guid.TryParseExact(id,"N",out parsed)&&
                parsed!=Guid.Empty&&string.Equals(id,parsed.ToString("N"),StringComparison.Ordinal);
        }

        private bool ValidateWorld(out string reason)
        {
            if(host.OwnedParts==null){reason="R3_OWNER_UNAVAILABLE";return false;}
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var part in host.OwnedParts)
            {
                if(part==null||!PhysicalId(part.InstanceId)||string.IsNullOrEmpty(part.PartKind)||
                   float.IsNaN(part.Health)||float.IsInfinity(part.Health)||
                   part.Health<0||part.Health>1)
                {reason="INVALID_HOST_PART";return false;}
                if(!seen.Add(part.InstanceId)){reason="DUPLICATE_PHYSICAL_INSTANCE";return false;}
            }
            foreach(var ticket in pending.Values)
            {
                var part=host.OwnedParts.FirstOrDefault(p=>p.InstanceId==ticket.InstanceId);
                if(part==null||part.Health>0||!host.HasReservation(ticket.InstanceId,ticket.ReservationKey))
                {reason="RESERVATION_STATE_MISMATCH";return false;}
            }
            reason="OK";return true;
        }

        public bool TryQueueDestroyed(string instanceId,out string reason)
        {
            lock(sync)
            {
                if(quarantined){reason="REBUILD_QUARANTINED";return false;}
                if(!ValidateWorld(out reason))return false;
                if(!PhysicalId(instanceId)){reason="INVALID_INSTANCE_ID";return false;}
                if(pending.ContainsKey(instanceId)){reason="ALREADY_REBUILDING";return false;}
                var part=host.OwnedParts.FirstOrDefault(x=>x.InstanceId==instanceId);
                if(part==null){reason="PART_NOT_OWNED";return false;}
                if(part.Health>0){reason="NOT_DESTROYED";return false;}
                int purchase=host.PurchasePrice(instanceId);
                int quote=host.PaidRebuildQuote(instanceId);
                if(purchase<=1||quote<=0||quote>=purchase)
                {reason="INVALID_REBUILD_QUOTE";return false;}
                bool useFree=freeUsedAt!=wins;
                int charge=useFree?0:quote;
                var key="econ-rebuild-v1:"+wins.ToString()+":"+instanceId;
                if(!host.TryReserveAndDebitAtomically(instanceId,charge,key,out reason))
                    return false; // strict: host must leave cash and part unchanged on false
                // Do not mutate internal journal until authoritative host verifies its reservation.
                if(!host.HasReservation(instanceId,key))
                {
                    quarantined=true;reason="HOST_TRANSACTION_CONTRACT_BROKEN";return false;
                }
                pending.Add(instanceId,new RebuildTicket(instanceId,key,wins,charge,purchase,quote,useFree));
                if(useFree)freeUsedAt=wins;
                reason="OK";return true;
            }
        }

        public bool TryFinishVictory(string eventId,bool won,out string reason)
        {
            lock(sync)
            {
                if(quarantined){reason="REBUILD_QUARANTINED";return false;}
                if(!ValidateWorld(out reason))return false;
                if(!won){reason="NO_VICTORY_NO_PROGRESS";return false;}
                if(string.IsNullOrWhiteSpace(eventId)){reason="INVALID_VICTORY_EVENT";return false;}
                if(completedEvents.Contains(eventId)){reason="VICTORY_EVENT_REPLAY";return false;}
                var closing=pending.Values.OrderBy(t=>t.InstanceId,StringComparer.Ordinal).ToArray();
                // Every requested rebuild must consume precisely the NEXT genuine win.
                if(closing.Any(t=>t.OrderedAfterWins!=wins))
                {reason="INVALID_REBUILD_EPOCH";return false;}
                if(closing.Length>0&&!host.TryCompleteReservedBatchAtomically(eventId,closing,out reason))
                    return false; // no fake success; host MUST be atomic on failure
                // Verify all parts were physically rebuilt and the reservations disappeared.
                if(closing.Any(t=>host.HasReservation(t.InstanceId,t.ReservationKey)||
                      host.OwnedParts.Count(p=>p.InstanceId==t.InstanceId&&p.Health>0)!=1))
                {quarantined=true;reason="HOST_BATCH_CONTRACT_BROKEN";return false;}
                foreach(var t in closing)pending.Remove(t.InstanceId);
                completedEvents.Add(eventId);
                wins++;
                // Resetting free slot follows a real, unique win only. No UI/seed/restart reset API.
                reason="OK";return true;
            }
        }

        public bool IsRebuilding(string instanceId)
        {lock(sync)return instanceId!=null&&pending.ContainsKey(instanceId);}

        public bool TrySave(out string json,out string reason)
        {
            lock(sync)
            {
                json=null;
                if(quarantined){reason="REBUILD_QUARANTINED";return false;}
                if(!ValidateWorld(out reason))return false;
                json=JsonUtility.ToJson(new RebuildSave{
                    Wins=wins,FreeSlotUsedAtWin=freeUsedAt,
                    Pending=pending.Values.OrderBy(t=>t.InstanceId,StringComparer.Ordinal).ToArray(),
                    AppliedVictoryEvents=completedEvents.OrderBy(x=>x,StringComparer.Ordinal).ToArray()
                },true);
                reason="OK";return true;
            }
        }

        public bool TryRestore(string json,out string reason)
        {
            lock(sync)
            {
                if(quarantined){reason="REBUILD_QUARANTINED";return false;}
                RebuildSave save;
                try{save=JsonUtility.FromJson<RebuildSave>(json);}
                catch(Exception){reason="CORRUPT_REBUILD_SAVE";return false;}
                if(save==null||save.Version!=1||save.Wins<0||save.Pending==null||
                   save.AppliedVictoryEvents==null||
                   (save.FreeSlotUsedAtWin < -1 || save.FreeSlotUsedAtWin > save.Wins))
                {reason="CORRUPT_REBUILD_SAVE";return false;}
                var tickets=new Dictionary<string,RebuildTicket>(StringComparer.Ordinal);
                var events=new HashSet<string>(StringComparer.Ordinal);
                foreach(var ev in save.AppliedVictoryEvents)
                    if(string.IsNullOrWhiteSpace(ev)||!events.Add(ev))
                    {reason="REPLAYED_OR_INVALID_EVENT_IN_SAVE";return false;}
                int freeTickets=0;
                foreach(var t in save.Pending)
                {
                    if(t==null||!PhysicalId(t.InstanceId)||string.IsNullOrEmpty(t.ReservationKey)||
                       t.ReservationKey!="econ-rebuild-v1:"+save.Wins.ToString()+":"+t.InstanceId||
                       t.OrderedAfterWins!=save.Wins||t.PurchasePriceAtOrder<=1||
                       t.PaidQuoteAtOrder<=0||t.PaidQuoteAtOrder>=t.PurchasePriceAtOrder||
                       t.ChargedScrap!=(t.UsedFreeSlot?0:t.PaidQuoteAtOrder)||
                       !tickets.TryAdd(t.InstanceId,t))
                    {reason="INVALID_OR_DUPLICATE_TICKET";return false;}
                    if(t.UsedFreeSlot)freeTickets++;
                }
                if(freeTickets>1||freeTickets>0&&save.FreeSlotUsedAtWin!=save.Wins)
                {reason="FREE_SLOT_REPLAY_IN_SAVE";return false;}
                if(host.OwnedParts==null){reason="R3_OWNER_UNAVAILABLE";return false;}
                var unique=new HashSet<string>(StringComparer.Ordinal);
                foreach(var p in host.OwnedParts)
                    if(p==null||!PhysicalId(p.InstanceId)||float.IsNaN(p.Health)||
                       float.IsInfinity(p.Health)||p.Health<0||p.Health>1||
                       string.IsNullOrEmpty(p.PartKind)||!unique.Add(p.InstanceId))
                    {reason="INVALID_OR_DUPLICATE_HOST_INSTANCE";return false;}
                foreach(var t in tickets.Values)
                    if(host.OwnedParts.Count(p=>p.InstanceId==t.InstanceId&&p.Health==0)!=1||
                       !host.HasReservation(t.InstanceId,t.ReservationKey))
                    {reason="UNMATCHED_HOST_RESERVATION";return false;}
                // Complete validation BEFORE replacing active metadata.
                pending.Clear();
                foreach(var p in tickets)pending.Add(p.Key,p.Value);
                completedEvents.Clear();
                foreach(var ev in events)completedEvents.Add(ev);
                wins=save.Wins;freeUsedAt=save.FreeSlotUsedAtWin;
                reason="OK";return true;
            }
        }
    }
}

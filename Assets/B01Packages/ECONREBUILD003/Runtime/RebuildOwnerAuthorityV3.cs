// B01-22 ECONREBUILD002 / v3.0.0 draft. No edits to frozen ECONREBUILD001.
// All wallet, owner inventory, physical reservation, win ID and quota are ONE host transaction.
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboRougelike.B01.Economy.RebuildV3
{
    public enum PublishResultV3 { Committed, Conflict, RejectedNoWrite, Uncertain }

    [Serializable] public sealed class OwnedPartV3
    {
        public string Id, Kind, ReservationKey;
        public float Health;
        public int Purchase, PaidQuote;
        public OwnedPartV3 Copy() => (OwnedPartV3)MemberwiseClone();
    }

    [Serializable] public sealed class RebuildOrderV3
    {
        public string PartId, ReservationKey;
        public int OrderedAfterWins, Charged, Purchase, PaidQuote;
        public bool WasFree;
        public RebuildOrderV3 Copy() => (RebuildOrderV3)MemberwiseClone();
    }

    // This is a STAGED VIEW of the canonical R3 owner/wallet/world, NOT another live inventory.
    // The receiver must stage and publish canonical R3 state + ledger in the SAME durable slot.
    [Serializable] public sealed class OwnerRebuildStateV3
    {
        public int Version=3, Wins, FreeUsedAtWin=-1, Scrap;
        public string OwnerId, RunId;
        public long Generation;
        public List<OwnedPartV3> Parts=new List<OwnedPartV3>();
        public List<RebuildOrderV3> Pending=new List<RebuildOrderV3>();
        public List<string> AppliedVictoryIds=new List<string>();
        public OwnerRebuildStateV3 Copy() => new OwnerRebuildStateV3 {
            Version=Version, Wins=Wins, FreeUsedAtWin=FreeUsedAtWin, Scrap=Scrap,
            OwnerId=OwnerId, RunId=RunId, Generation=Generation,
            Parts=Parts.Select(x=>x.Copy()).ToList(),
            Pending=Pending.Select(x=>x.Copy()).ToList(),
            AppliedVictoryIds=new List<string>(AppliedVictoryIds)
        };
    }

    // Mandatory host guarantees:
    // - Read gives a detached snapshot from the currently AUTHORITATIVE durable slot.
    // - Publish compares owner/run/generation under ONE exclusive owner lock.
    // - Published wallet, parts, reservations, win IDs and quota become durable together.
    // - Conflict and RejectedNoWrite NEVER mutate persistent or physical host state.
    // - If the durable result is ambiguous, return Uncertain (including lost Commit ACK).
    // - Run/owner selection is pinned; stale restore may NEVER bypass this interface.
    // - A genuine victory is verified and consumed by the trusted host IN the same atomic publish;
    //   **never** trust the caller to set authenticatedWin=true.
    public interface IAtomicRebuildOwnerStoreV3
    {
        bool TryRead(string ownerId,string runId,out OwnerRebuildStateV3 state,out string reason);
        // winnerEventId is null for non-victory mutations, and a *candidate* on victory.
        // Under the SAME lock+durable publish, verify this ID against a TRUSTED combat
        // authority receipt issued by the actual encounter system, consume it ONCE,
        // and commit this economy transaction. Caller-supplied bool/string is NOT proof.
        // "RejectedNoWrite" on missing/used receipt; never debit/free/advance in that case.
        PublishResultV3 TryPublish(string ownerId,string runId,long expectedGeneration,
            OwnerRebuildStateV3 proposed,string winnerEventId,out string reason);
    }

    public sealed class RebuildEconomyAuthorityV3
    {
        readonly object gate=new object();
        readonly IAtomicRebuildOwnerStoreV3 store;
        readonly string owner,run;
        bool quarantined;
        public bool IsQuarantined { get { lock(gate)return quarantined; } }
        public RebuildEconomyAuthorityV3(IAtomicRebuildOwnerStoreV3 store,string ownerId,string runId)
        {
            this.store=store??throw new ArgumentNullException(nameof(store));
            if(string.IsNullOrWhiteSpace(ownerId)||string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException("OWNER_RUN_REQUIRED");
            owner=ownerId;run=runId;
        }
        static bool Id(string s) => Guid.TryParseExact(s,"N",out var g) &&
            g!=Guid.Empty && string.Equals(s,g.ToString("N"),StringComparison.Ordinal);

        bool Valid(OwnerRebuildStateV3 s)
        {
            if(s==null||s.Version!=3||s.OwnerId!=owner||s.RunId!=run||
               s.Generation<0||s.Wins<0||s.FreeUsedAtWin < -1||
               s.FreeUsedAtWin>s.Wins||s.Scrap<0||
               s.Parts==null||s.Pending==null||s.AppliedVictoryIds==null||
               s.AppliedVictoryIds.Count!=s.Wins)return false;
            var parts=new Dictionary<string,OwnedPartV3>(StringComparer.Ordinal);
            foreach(var p in s.Parts)
            {
                if(p==null||!Id(p.Id)||string.IsNullOrWhiteSpace(p.Kind)||
                   float.IsNaN(p.Health)||float.IsInfinity(p.Health)||
                   p.Health<0||p.Health>1||p.Purchase<=1||
                   p.PaidQuote<=0||p.PaidQuote>=p.Purchase||
                   !parts.TryAdd(p.Id,p))return false;
            }
            var events=new HashSet<string>(StringComparer.Ordinal);
            if(s.AppliedVictoryIds.Any(x=>string.IsNullOrWhiteSpace(x)||!events.Add(x)))
                return false;
            var orders=new HashSet<string>(StringComparer.Ordinal);
            int free=0;
            foreach(var t in s.Pending)
            {
                if(t==null||!Id(t.PartId)||!orders.Add(t.PartId)||
                   !parts.TryGetValue(t.PartId,out var p)||
                   p.Health!=0||string.IsNullOrEmpty(t.ReservationKey)||
                   t.ReservationKey!=p.ReservationKey||
                   t.OrderedAfterWins!=s.Wins||
                   t.Purchase<=1||t.PaidQuote<=0||t.PaidQuote>=t.Purchase||
                   t.Charged!=(t.WasFree?0:t.PaidQuote))return false;
                if(t.WasFree)free++;
            }
            if(free>1||(free==1&&s.FreeUsedAtWin!=s.Wins))return false;
            return s.Parts.All(p=>string.IsNullOrEmpty(p.ReservationKey)||
                                     orders.Contains(p.Id));
        }
        bool Read(out OwnerRebuildStateV3 state,out string reason)
        {
            state=null;
            if(quarantined){reason="OWNER_QUARANTINED";return false;}
            try
            {
                if(!store.TryRead(owner,run,out state,out reason))return false;
                if(Valid(state))return true;
            }
            catch(Exception){/* never resume from a possibly corrupt authority */ }
            quarantined=true;reason="INVALID_OR_UNREADABLE_AUTHORITATIVE_OWNER";
            return false;
        }
        bool Change(Func<OwnerRebuildStateV3,string> operation,string trustedEventId,out string reason)
        {
            lock(gate)
            {
                for(int attempt=0;attempt<8;attempt++)
                {
                    if(!Read(out var current,out reason))return false;
                    var next=current.Copy();
                    reason=operation(next);
                    if(reason!="OK")return false;
                    if(current.Generation==long.MaxValue)
                    {reason="GENERATION_EXHAUSTED";return false;}
                    next.Generation=current.Generation+1;
                    if(!Valid(next))
                    {quarantined=true;reason="INVALID_STAGED_OWNER";return false;}
                    PublishResultV3 result;
                    try {result=store.TryPublish(owner,run,current.Generation,next,trustedEventId,out reason);}
                    catch(Exception){result=PublishResultV3.Uncertain;reason="AMBIGUOUS_COMMIT_EXCEPTION";}
                    if(result==PublishResultV3.Committed){reason="OK";return true;}
                    if(result==PublishResultV3.Conflict)continue; // re-read & reprice/quota
                    if(result==PublishResultV3.RejectedNoWrite)return false;
                    quarantined=true;reason="AMBIGUOUS_COMMIT_QUARANTINE";return false;
                }
                reason="OWNER_CONTENTION_RETRY";return false;
            }
        }

        public bool TryReserveDestroyed(string partId,out string reason)
        {
            return Change(s=>{
                if(!Id(partId))return "INVALID_PART_ID";
                var p=s.Parts.SingleOrDefault(x=>x.Id==partId);
                if(p==null)return "NOT_OWNED";
                if(p.Health!=0)return "NOT_DESTROYED";
                if(!string.IsNullOrEmpty(p.ReservationKey))return "ALREADY_REBUILDING";
                bool free=s.FreeUsedAtWin!=s.Wins;
                int charge=free?0:p.PaidQuote;
                if(s.Scrap<charge)return "INSUFFICIENT_SCRAP";
                string key="econ-rebuild-v3:"+s.OwnerId+":"+s.RunId+":"+
                           (s.Generation+1).ToString()+":"+partId;
                p.ReservationKey=key;s.Scrap-=charge;
                s.Pending.Add(new RebuildOrderV3{
                    PartId=partId,ReservationKey=key,OrderedAfterWins=s.Wins,
                    Charged=charge,Purchase=p.Purchase,PaidQuote=p.PaidQuote,WasFree=free
                });
                if(free)s.FreeUsedAtWin=s.Wins;
                return "OK";
            },null,out reason);
        }

        // Only the host can attest combat victory, during its atomic Publish transaction.
        // Neither an arbitrary win ID nor an untrusted UI-provided bool may grant a win.
        public bool TryFinishChallenge(string victoryId,out string reason)
        {
            return Change(s=>{
                if(string.IsNullOrWhiteSpace(victoryId))return "INVALID_VICTORY_ID";
                if(s.AppliedVictoryIds.Contains(victoryId,StringComparer.Ordinal))
                    return "VICTORY_ID_REPLAY";
                foreach(var ticket in s.Pending)
                {
                    var p=s.Parts.SingleOrDefault(x=>x.Id==ticket.PartId);
                    if(p==null||p.Health!=0||p.ReservationKey!=ticket.ReservationKey)
                        return "PHYSICAL_RESERVATION_MISMATCH";
                }
                foreach(var ticket in s.Pending)
                {
                    var p=s.Parts.Single(x=>x.Id==ticket.PartId);
                    p.Health=1;p.ReservationKey=null; // same GUID; host must commit real HP
                }
                s.Pending.Clear();
                s.AppliedVictoryIds.Add(victoryId);
                if(s.Wins==int.MaxValue)return "VICTORY_COUNT_EXHAUSTED";
                s.Wins++; // unlocks at most ONE free claim for next victory epoch
                return "OK";
            },victoryId,out reason);
        }

        public bool TryReadAuthoritative(out OwnerRebuildStateV3 state,out string reason)
        {lock(gate)return Read(out state,out reason);}

        // Guard for a separately supplied save candidate. Never installs it.
        // Restore ONLY the current authoritative owner slot using TryReadAuthoritative.
        public bool TryAuthorizeResume(OwnerRebuildStateV3 candidate,out string reason)
        {
            lock(gate)
            {
                if(!Read(out var current,out reason))return false;
                if(!Valid(candidate)){reason="INVALID_OR_FOREIGN_CHECKPOINT";return false;}
                if(!Equivalent(current,candidate))
                {reason="STALE_OR_FORKED_REBUILD_CHECKPOINT";return false;}
                reason="OK";return true;
            }
        }
        static bool Equivalent(OwnerRebuildStateV3 a,OwnerRebuildStateV3 b)
        {
            if(a.Generation!=b.Generation||a.Wins!=b.Wins||
               a.Scrap!=b.Scrap||a.FreeUsedAtWin!=b.FreeUsedAtWin||
               a.Parts.Count!=b.Parts.Count||a.Pending.Count!=b.Pending.Count||
               a.AppliedVictoryIds.Count!=b.AppliedVictoryIds.Count)return false;
            if(!a.AppliedVictoryIds.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(
                b.AppliedVictoryIds.OrderBy(x=>x,StringComparer.Ordinal)))return false;
            foreach(var x in a.Parts)
            {
                var y=b.Parts.SingleOrDefault(p=>p.Id==x.Id);
                if(y==null||y.Kind!=x.Kind||y.Health!=x.Health||
                   y.Purchase!=x.Purchase||y.PaidQuote!=x.PaidQuote||
                   y.ReservationKey!=x.ReservationKey)return false;
            }
            foreach(var x in a.Pending)
            {
                var y=b.Pending.SingleOrDefault(t=>t.PartId==x.PartId);
                if(y==null||y.ReservationKey!=x.ReservationKey||
                   y.OrderedAfterWins!=x.OrderedAfterWins||y.Charged!=x.Charged||
                   y.Purchase!=x.Purchase||y.PaidQuote!=x.PaidQuote||
                   y.WasFree!=x.WasFree)return false;
            }
            return true;
        }
    }
}

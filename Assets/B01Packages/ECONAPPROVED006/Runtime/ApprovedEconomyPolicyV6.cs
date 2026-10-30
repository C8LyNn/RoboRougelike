// B01-22 / P17 W24 / ECONAPPROVED006.
// Approved gameplay rules as pure staged mutation of ONE canonical host snapshot.
// No disk owner, Session, Commit, Unity world, second wallet or Unity clock.
// Production receiver MUST invoke under selected #749 same-owner lease + atomic
// #556-compatible Save mutation, and validate/consume REAL combat proof in
// that SAME durable publish. Caller booleans and IDs are NOT proof of victory.
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoboRougelike.B01.Economy.ApprovedV6
{
    public enum LoanerPhaseV6 { None, Reserved, Entered, Spent }
    [Serializable] public sealed class OwnedModuleV6
    {
        public string Id,Category,ReservationId;
        public float Hp;
        public int PurchaseScrap;
        public OwnedModuleV6 Copy() => (OwnedModuleV6)MemberwiseClone();
    }
    [Serializable] public sealed class RebuildTicketV6
    {
        public string ModuleId,ReservationId;
        public int AfterWins,ChargedScrap,PurchaseScrap;
        public bool Free;
        public RebuildTicketV6 Copy() => (RebuildTicketV6)MemberwiseClone();
    }
    [Serializable] public sealed class OneShotLoanerV6
    {
        public LoanerPhaseV6 Phase;
        public bool EverEntered;
        public string EncounterId,PermitId;
        public OneShotLoanerV6 Copy() => (OneShotLoanerV6)MemberwiseClone();
    }
    [Serializable] public sealed class ApprovedEconomySnapshotV6
    {
        // MUST be embedded in the selected canonical owner Save, not standalone.
        // When receiver changes actual game schema it must version and migrate
        // with explicit review. Do NOT silently import old V3 snapshots.
        public int Version=6,Wins,Scrap,FreeUsedForWin;
        public long Generation;
        public string OwnerId,RunId;
        public List<OwnedModuleV6> Parts=new List<OwnedModuleV6>();
        public List<RebuildTicketV6> Pending=new List<RebuildTicketV6>();
        public List<string> ConsumedVictoryIds=new List<string>();
        public OneShotLoanerV6 Loaner=new OneShotLoanerV6();

        public ApprovedEconomySnapshotV6 Copy() => new ApprovedEconomySnapshotV6 {
            Version=Version,Wins=Wins,Scrap=Scrap,FreeUsedForWin=FreeUsedForWin,
            Generation=Generation,OwnerId=OwnerId,RunId=RunId,
            Parts=Parts.Select(x=>x.Copy()).ToList(),
            Pending=Pending.Select(x=>x.Copy()).ToList(),
            ConsumedVictoryIds=new List<string>(ConsumedVictoryIds),
            Loaner=Loaner.Copy()
        };
    }
    // Required trusted host observation from actual quiesced CanDeploy,
    // physical Core ID+HP, eligible next encounter. Immutable to the reducer.
    // The receiver must never accept this from HUD or a gameplay client.
    public sealed class CanonicalDeploymentFactsV6
    {
        public string OwnerId,RunId,CoreId,EncounterId;
        public long Generation;
        public bool CoreFunctional,CanDeploy,HasGenuineNextChallenge;
        public string[] EssentialDestroyedPhysicalIds;
    }
    public static class ApprovedEconomyPolicyV6
    {
        // Owner-approved: 65% of purchase, nearest integral Scrap with .5 up.
        // P=1 ->1; 5->3; 15->10; 30->20. No overflow even at int.MaxValue.
        public static bool TryPaidQuote(int purchase,out int scrap)
        {
            scrap=0;
            if(purchase<1)return false;
            scrap=checked((int)(((long)purchase*65L+50L)/100L));
            return scrap>=1;
        }
        public const int LoanerMaximumHealthPercent=60;
        public const int LoanerMaximumDamagePercent=60;
        public const int LoanerMaximumDrivePercent=75;
        public const bool LoanerMayTransferParts=false;
        public const bool LoanerMayPersistEquipment=false;

        static bool Id(string s) =>
            Guid.TryParseExact(s,"N",out var g)&&g!=Guid.Empty&&
            string.Equals(s,g.ToString("N"),StringComparison.Ordinal);

        public static bool Valid(ApprovedEconomySnapshotV6 s)
        {
            if(s==null||s.Version!=6||string.IsNullOrWhiteSpace(s.OwnerId)||
               string.IsNullOrWhiteSpace(s.RunId)||s.Generation<0||
               s.Wins<0||s.FreeUsedForWin<0||s.FreeUsedForWin>s.Wins||
               s.Scrap<0||s.Parts==null||s.Pending==null||
               s.ConsumedVictoryIds==null||s.Loaner==null||
               s.ConsumedVictoryIds.Count!=s.Wins||
               s.ConsumedVictoryIds.Any(string.IsNullOrWhiteSpace)||
               s.ConsumedVictoryIds.Distinct(StringComparer.Ordinal).Count()!=s.Wins)
                return false;
            var parts=new Dictionary<string,OwnedModuleV6>(StringComparer.Ordinal);
            foreach(var p in s.Parts)
            {
                if(p==null||!Id(p.Id)||string.IsNullOrWhiteSpace(p.Category)||
                   float.IsNaN(p.Hp)||float.IsInfinity(p.Hp)||p.Hp<0||p.Hp>1||
                   !TryPaidQuote(p.PurchaseScrap,out _)||
                   !parts.TryAdd(p.Id,p))return false;
            }
            var pendingIds=new HashSet<string>(StringComparer.Ordinal);
            int free=0;
            foreach(var t in s.Pending)
            {
                if(t==null||!Id(t.ModuleId)||!pendingIds.Add(t.ModuleId)||
                   !parts.TryGetValue(t.ModuleId,out var p)||p.Hp!=0||
                   string.IsNullOrWhiteSpace(t.ReservationId)||
                   p.ReservationId!=t.ReservationId||
                   t.AfterWins!=s.Wins||t.PurchaseScrap!=p.PurchaseScrap||
                   !TryPaidQuote(t.PurchaseScrap,out var quote)||
                   t.ChargedScrap!=(t.Free?0:quote))
                    return false;
                if(t.Free)free++;
            }
            if(free>1||(free>0&&s.FreeUsedForWin!=s.Wins)|| 
               s.Parts.Any(p=>!string.IsNullOrEmpty(p.ReservationId)&&
                                !pendingIds.Contains(p.Id)))return false;
            // No startup free before any authentic victory. A consumed initial
            // coupon is a source error; only a real win may mint the first.
            if(s.Wins==0&&s.FreeUsedForWin!=0)return false;
            switch(s.Loaner.Phase)
            {
                case LoanerPhaseV6.None:
                    return !s.Loaner.EverEntered &&
                        string.IsNullOrEmpty(s.Loaner.EncounterId)&&
                        string.IsNullOrEmpty(s.Loaner.PermitId);
                case LoanerPhaseV6.Reserved:
                    return !s.Loaner.EverEntered &&
                        !string.IsNullOrWhiteSpace(s.Loaner.EncounterId)&&
                        Id(s.Loaner.PermitId);
                case LoanerPhaseV6.Entered:
                case LoanerPhaseV6.Spent:
                    return s.Loaner.EverEntered &&
                        !string.IsNullOrWhiteSpace(s.Loaner.EncounterId)&&
                        Id(s.Loaner.PermitId);
                default:return false;
            }
        }
        static bool Stage(ApprovedEconomySnapshotV6 source,
            Func<ApprovedEconomySnapshotV6,string> mutate,
            out ApprovedEconomySnapshotV6 proposed,out string reason)
        {
            proposed=null;
            if(!Valid(source)){reason="INVALID_OR_FOREIGN_CANONICAL_SNAPSHOT";return false;}
            if(source.Generation==long.MaxValue)
            {reason="GENERATION_EXHAUSTED";return false;}
            var next=source.Copy();
            reason=mutate(next);
            if(reason!="OK")return false;
            next.Generation++;
            if(!Valid(next)){reason="INVALID_STAGED_POLICY";return false;}
            proposed=next;return true;
        }

        public static bool TryReserveDestroyed(ApprovedEconomySnapshotV6 s,string moduleId,
            out ApprovedEconomySnapshotV6 next,out string reason)
        {
            return Stage(s,x=>{
                if(!Id(moduleId))return "INVALID_MODULE_ID";
                var p=x.Parts.SingleOrDefault(y=>y.Id==moduleId);
                if(p==null)return "FOREIGN_MODULE";
                if(p.Hp!=0)return "MODULE_NOT_DESTROYED";
                if(!string.IsNullOrEmpty(p.ReservationId))return "ALREADY_RESERVED";
                bool free=x.Wins>0&&x.FreeUsedForWin!=x.Wins;
                if(!TryPaidQuote(p.PurchaseScrap,out var quote))return "INVALID_PURCHASE";
                int charge=free?0:quote;
                if(x.Scrap<charge)return "INSUFFICIENT_SCRAP";
                var reservation="econ-v6:"+x.OwnerId+":"+x.RunId+":"+
                     (x.Generation+1).ToString()+":"+p.Id;
                p.ReservationId=reservation;
                x.Scrap-=charge;
                x.Pending.Add(new RebuildTicketV6 {
                    ModuleId=p.Id,ReservationId=reservation,
                    AfterWins=x.Wins,ChargedScrap=charge,
                    PurchaseScrap=p.PurchaseScrap,Free=free
                });
                if(free)x.FreeUsedForWin=x.Wins;
                return "OK";
            },out next,out reason);
        }

        // Host must attest a true signed-by-owner combat victory AND consume
        // its receipt in the SAME canonical #749 publication as this proposal.
        // A user-created string passed here is NOT a legitimate win.
        public static bool TryStageTrustedVictory(ApprovedEconomySnapshotV6 s,
            string realVictoryId,string realEncounterId,
            out ApprovedEconomySnapshotV6 next,out string reason)
        {
            return Stage(s,x=>{
                if(string.IsNullOrWhiteSpace(realVictoryId)||
                   string.IsNullOrWhiteSpace(realEncounterId))
                    return "MISSING_TRUSTED_COMBAT_ID";
                if(x.ConsumedVictoryIds.Contains(realVictoryId,StringComparer.Ordinal))
                    return "VICTORY_ID_REPLAY";
                if(x.Wins==int.MaxValue)return "VICTORY_COUNTER_EXHAUSTED";
                if(x.Loaner.Phase==LoanerPhaseV6.Entered)
                {
                    if(x.Loaner.EncounterId!=realEncounterId)
                        return "WRONG_LOANER_ENCOUNTER";
                    x.Loaner.Phase=LoanerPhaseV6.Spent;
                }
                if(x.Loaner.Phase==LoanerPhaseV6.Reserved&&
                   x.Loaner.EncounterId==realEncounterId)
                    return "LOANER_NOT_ENTERED";
                foreach(var t in x.Pending)
                {
                    var p=x.Parts.SingleOrDefault(v=>v.Id==t.ModuleId);
                    if(p==null||p.Hp!=0||p.ReservationId!=t.ReservationId||
                       t.AfterWins>=x.Wins+1)
                        return "PENDING_PHYSICAL_ID_OR_NEXT_WIN_MISMATCH";
                }
                foreach(var t in x.Pending)
                {
                    var p=x.Parts.Single(v=>v.Id==t.ModuleId);
                    p.Hp=1;p.ReservationId=null;
                }
                x.Pending.Clear();
                x.Wins++;
                x.ConsumedVictoryIds.Add(realVictoryId);
                // Previous free quota expires; NEW genuine victory unlocks one.
                return "OK";
            },out next,out reason);
        }

        // Actual physical CanDeploy and Core identity must be provided by the
        // canonical game host while its selected world/session lease is held.
        public static bool TryReserveOneShotLoaner(ApprovedEconomySnapshotV6 s,
            CanonicalDeploymentFactsV6 facts,string canonicalPermitGuid,
            out ApprovedEconomySnapshotV6 next,out string reason)
        {
            return Stage(s,x=>{
                if(x.Loaner.Phase!=LoanerPhaseV6.None||x.Loaner.EverEntered)
                    return "ONE_SHOT_LOANER_ALREADY_RESERVED_OR_SPENT";
                if(facts==null||facts.OwnerId!=x.OwnerId||
                   facts.RunId!=x.RunId||facts.Generation!=x.Generation||
                   !Id(facts.CoreId)||!Id(canonicalPermitGuid)||
                   string.IsNullOrWhiteSpace(facts.EncounterId)||
                   !facts.HasGenuineNextChallenge||
                   facts.CanDeploy||!facts.CoreFunctional||
                   facts.EssentialDestroyedPhysicalIds==null)
                    return "INELIGIBLE_OR_UNTRUSTED_NEXT_CHALLENGE";
                var core=x.Parts.SingleOrDefault(p=>p.Id==facts.CoreId);
                if(core==null||core.Hp<=0)return "CORE_NOT_ALIVE";
                var ids=new HashSet<string>(StringComparer.Ordinal);
                foreach(var id in facts.EssentialDestroyedPhysicalIds)
                {
                    if(!Id(id)||!ids.Add(id)||id==core.Id)
                        return "INVALID_ESSENTIAL_BLOCKER";
                    var p=x.Parts.SingleOrDefault(t=>t.Id==id);
                    if(p==null||p.Hp!=0)return "NOT_AN_OWNED_DESTROYED_ESSENTIAL";
                }
                if(ids.Count==0)return "NO_DESTROYED_ESSENTIAL_BLOCKER";
                x.Loaner.Phase=LoanerPhaseV6.Reserved;
                x.Loaner.EncounterId=facts.EncounterId;
                x.Loaner.PermitId=canonicalPermitGuid;
                return "OK";
            },out next,out reason);
        }
        // Host must attest that the real WEAKER, separate, ephemeral borrowed
        // craft entered exactly this next challenge; stage+publish under same
        // owner lease BEFORE combat can begin. No invented player-side bool.
        public static bool TryConsumeLoanerOnTrustedChallengeEntry(
            ApprovedEconomySnapshotV6 s,string canonicalEncounterId,
            string canonicalPermitGuid,
            out ApprovedEconomySnapshotV6 next,out string reason)
        {
            return Stage(s,x=>{
                if(x.Loaner.Phase!=LoanerPhaseV6.Reserved||
                   x.Loaner.EncounterId!=canonicalEncounterId||
                   x.Loaner.PermitId!=canonicalPermitGuid)
                    return "NO_MATCHING_UNCONSUMED_LOANER_PERMIT";
                x.Loaner.Phase=LoanerPhaseV6.Entered;
                x.Loaner.EverEntered=true;
                return "OK";
            },out next,out reason);
        }
        // Only genuinely observed lost challenge: NO victory credit, NO heals,
        // NO replay of one-shot loaner; physical borrowed rig must be retired.
        public static bool TryStageTrustedLoss(ApprovedEconomySnapshotV6 s,
            string actualEncounterId,out ApprovedEconomySnapshotV6 next,out string reason)
        {
            return Stage(s,x=>{
                if(x.Loaner.Phase!=LoanerPhaseV6.Entered||
                   x.Loaner.EncounterId!=actualEncounterId)
                    return "NO_MATCHING_ENTERED_LOANER";
                x.Loaner.Phase=LoanerPhaseV6.Spent;
                return "OK";
            },out next,out reason);
        }
    }
}

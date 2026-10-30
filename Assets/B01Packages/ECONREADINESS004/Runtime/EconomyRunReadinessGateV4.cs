// B01-22 P17/W22 ECONREADINESS004 — read-only gate, no R3/Codex/host writes.
// Bound to frozen ECONREBUILD003 V3 authority source, NOT a new competing wallet/save.
using System;
using System.Collections.Generic;
using System.Linq;
using RoboRougelike.B01.Economy.RebuildV3;

namespace RoboRougelike.B01.Economy.ReadinessV4
{
    public enum ReadinessVerdictV4
    {
        Unverified, NormalChallengeReady, CriticalPending0HpBlock,
        CriticalUnqueued0HpBlock, NoGenuineChallengeWhilePending,
        HostDeployBlocked, NoGenuineChallenge
    }

    // The trusted, real game owner MUST implement this from actual CanDeploy,
    // active encounter selector and physical ownership. Never take UI guesses.
    public sealed class TrustedDeploymentObservationV4
    {
        public string OwnerId,RunId;
        public long OwnerGeneration;
        public bool CanDeploy,HasGenuineNextChallenge;
        public string[] BlockingPhysicalIds;
    }

    public interface ITrustedDeploymentProbeV4
    {
        bool TryInspectCanonical(OwnerRebuildStateV3 ownerSnapshot,
            out TrustedDeploymentObservationV4 observation,out string reason);
    }

    public sealed class ReadinessReportV4
    {
        public ReadinessVerdictV4 Verdict;
        public string OwnerId,RunId,Reason;
        public long Generation;
        public int Wins,PendingCount,FreeSlotsRemainingInEpoch;
        public int Scrap;
        public string[] BlockedPhysicalIds=Array.Empty<string>();
        public string[] PendingPhysicalIds=Array.Empty<string>();
        public bool CanDeploy,HasGenuineNextChallenge;
        public bool RequiresOwnerRecoveryDecision;
        // ADVISORY ONLY: no caller may authorize a real combat start from this report.
        public bool AuthorizesCombatStart => false;
    }

    public sealed class EconomyRunReadinessGateV4
    {
        readonly object sync=new object();
        readonly IAtomicRebuildOwnerStoreV3 ownerStore;
        readonly ITrustedDeploymentProbeV4 deploymentProbe;
        readonly string ownerId,runId;

        public EconomyRunReadinessGateV4(IAtomicRebuildOwnerStoreV3 store,
            ITrustedDeploymentProbeV4 probe,string owner,string run)
        {
            ownerStore=store??throw new ArgumentNullException(nameof(store));
            deploymentProbe=probe??throw new ArgumentNullException(nameof(probe));
            if(string.IsNullOrWhiteSpace(owner)||string.IsNullOrWhiteSpace(run))
                throw new ArgumentException("OWNER_RUN_REQUIRED");
            ownerId=owner;runId=run;
        }

        static bool PhysicalId(string s) =>
            Guid.TryParseExact(s,"N",out var g)&&g!=Guid.Empty&&
            string.Equals(s,g.ToString("N"),StringComparison.Ordinal);

        // Validate the original V3 journal read-only. Do not accept an externally
        // fabricated duplicate / forked physical part as deploy evidence.
        static bool Valid(OwnerRebuildStateV3 s,string owner,string run)
        {
            if(s==null||s.Version!=3||s.OwnerId!=owner||s.RunId!=run||
                s.Generation<0||s.Wins<0||s.Scrap<0||
                s.FreeUsedAtWin< -1||s.FreeUsedAtWin>s.Wins||
                s.Parts==null||s.Pending==null||s.AppliedVictoryIds==null||
                s.AppliedVictoryIds.Count!=s.Wins)return false;
            var parts=new Dictionary<string,OwnedPartV3>(StringComparer.Ordinal);
            foreach(var p in s.Parts)
            {
                if(p==null||!PhysicalId(p.Id)||string.IsNullOrWhiteSpace(p.Kind)||
                   p.Health<0||p.Health>1||float.IsNaN(p.Health)||
                   float.IsInfinity(p.Health)||p.Purchase<=1||
                   p.PaidQuote<=0||p.PaidQuote>=p.Purchase||
                   !parts.TryAdd(p.Id,p))return false;
            }
            if(s.AppliedVictoryIds.Any(e=>string.IsNullOrWhiteSpace(e))||
                s.AppliedVictoryIds.Distinct(StringComparer.Ordinal).Count()!=
                s.AppliedVictoryIds.Count)return false;
            var pending=new HashSet<string>(StringComparer.Ordinal);
            int free=0;
            foreach(var t in s.Pending)
            {
                if(t==null||!PhysicalId(t.PartId)||!pending.Add(t.PartId)||
                   !parts.TryGetValue(t.PartId,out var p)||p.Health!=0||
                   p.ReservationKey!=t.ReservationKey||
                   string.IsNullOrWhiteSpace(t.ReservationKey)||
                   t.OrderedAfterWins!=s.Wins||
                   t.Purchase<=1||t.PaidQuote<=0||t.PaidQuote>=t.Purchase||
                   t.Charged!=(t.WasFree?0:t.PaidQuote))return false;
                if(t.WasFree)free++;
            }
            return free<=1&&(free==0||s.FreeUsedAtWin==s.Wins)&&
                s.Parts.All(p=>string.IsNullOrEmpty(p.ReservationKey)||
                               pending.Contains(p.Id));
        }

        static bool Same(OwnerRebuildStateV3 a,OwnerRebuildStateV3 b)
        {
            if(a.Generation!=b.Generation||a.Wins!=b.Wins||
               a.FreeUsedAtWin!=b.FreeUsedAtWin||a.Scrap!=b.Scrap||
               a.Parts.Count!=b.Parts.Count||a.Pending.Count!=b.Pending.Count||
               a.AppliedVictoryIds.Count!=b.AppliedVictoryIds.Count)return false;
            if(!a.AppliedVictoryIds.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(
                b.AppliedVictoryIds.OrderBy(x=>x,StringComparer.Ordinal)))return false;
            foreach(var p in a.Parts)
            {
                var q=b.Parts.SingleOrDefault(x=>x.Id==p.Id);
                if(q==null||q.Kind!=p.Kind||q.Health!=p.Health||
                   q.Purchase!=p.Purchase||q.PaidQuote!=p.PaidQuote||
                   q.ReservationKey!=p.ReservationKey)return false;
            }
            foreach(var t in a.Pending)
            {
                var q=b.Pending.SingleOrDefault(x=>x.PartId==t.PartId);
                if(q==null||q.ReservationKey!=t.ReservationKey||
                   q.OrderedAfterWins!=t.OrderedAfterWins||
                   q.Charged!=t.Charged||q.Purchase!=t.Purchase||
                   q.PaidQuote!=t.PaidQuote||q.WasFree!=t.WasFree)return false;
            }
            return true;
        }
        static ReadinessReportV4 Unknown(string code) => new ReadinessReportV4 {
            Verdict=ReadinessVerdictV4.Unverified,Reason=code,
            RequiresOwnerRecoveryDecision=false
        };

        public bool TryEvaluate(out ReadinessReportV4 report,out string reason)
        {
            lock(sync)
            {
                report=Unknown("UNVERIFIED");
                OwnerRebuildStateV3 before,after;
                TrustedDeploymentObservationV4 obs;
                try
                {
                    if(!ownerStore.TryRead(ownerId,runId,out before,out reason))
                    {report=Unknown("OWNER_READ_UNAVAILABLE");return false;}
                    if(!Valid(before,ownerId,runId))
                    {reason="INVALID_OWNER_ECONOMY";report=Unknown(reason);return false;}
                    // Hand the probe a defensive copy: it cannot mutate authoritative
                    // state through our reference, even if badly implemented.
                    if(!deploymentProbe.TryInspectCanonical(before.Copy(),out obs,out reason))
                    {report=Unknown("DEPLOYMENT_PROBE_UNAVAILABLE");return false;}
                    if(!ownerStore.TryRead(ownerId,runId,out after,out reason))
                    {report=Unknown("OWNER_RECHECK_UNAVAILABLE");return false;}
                    if(!Valid(after,ownerId,runId)||!Same(before,after))
                    {reason="OWNER_CHANGED_DURING_PROBE";report=Unknown(reason);return false;}
                }
                catch(Exception)
                {reason="OBSERVATION_EXCEPTION";report=Unknown(reason);return false;}
                if(obs==null||obs.OwnerId!=ownerId||obs.RunId!=runId||
                   obs.OwnerGeneration!=after.Generation||obs.BlockingPhysicalIds==null)
                {reason="INVALID_DEPLOYMENT_PROOF";report=Unknown(reason);return false;}
                var all=new Dictionary<string,OwnedPartV3>(StringComparer.Ordinal);
                foreach(var p in after.Parts)all.Add(p.Id,p);
                var blocked=new HashSet<string>(StringComparer.Ordinal);
                foreach(var id in obs.BlockingPhysicalIds)
                {
                    // A blocker tagged "physical" must be one destroyed owned part.
                    if(!PhysicalId(id)||!blocked.Add(id)||!all.TryGetValue(id,out var p)||
                        p.Health!=0)
                    {reason="INVALID_BLOCKED_PHYSICAL_ID";report=Unknown(reason);return false;}
                }
                if(obs.CanDeploy&&blocked.Count>0)
                {reason="CONTRADICTORY_DEPLOYMENT_PROOF";report=Unknown(reason);return false;}

                var pending=after.Pending.Select(t=>t.PartId).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
                var blockedIds=blocked.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
                bool essentialPending=blockedIds.Any(pending.Contains);
                var state=ReadinessVerdictV4.HostDeployBlocked;
                if(!obs.HasGenuineNextChallenge)
                    state=pending.Length>0?ReadinessVerdictV4.NoGenuineChallengeWhilePending:
                                           ReadinessVerdictV4.NoGenuineChallenge;
                else if(obs.CanDeploy)
                    state=ReadinessVerdictV4.NormalChallengeReady;
                else if(essentialPending)
                    state=ReadinessVerdictV4.CriticalPending0HpBlock;
                else if(blockedIds.Length>0)
                    state=ReadinessVerdictV4.CriticalUnqueued0HpBlock;

                report=new ReadinessReportV4 {
                    Verdict=state,Reason="OK",OwnerId=ownerId,RunId=runId,
                    Generation=after.Generation,Wins=after.Wins,Scrap=after.Scrap,
                    PendingCount=pending.Length,PendingPhysicalIds=pending,
                    BlockedPhysicalIds=blockedIds,
                    FreeSlotsRemainingInEpoch=after.FreeUsedAtWin==after.Wins?0:1,
                    CanDeploy=obs.CanDeploy,
                    HasGenuineNextChallenge=obs.HasGenuineNextChallenge,
                    RequiresOwnerRecoveryDecision=state==
                        ReadinessVerdictV4.CriticalPending0HpBlock||
                        state==ReadinessVerdictV4.CriticalUnqueued0HpBlock||
                        state==ReadinessVerdictV4.NoGenuineChallengeWhilePending
                };
                // Read-only, not a battle/deployment/win authorization token.
                reason="OK";return true;
            }
        }
    }
}

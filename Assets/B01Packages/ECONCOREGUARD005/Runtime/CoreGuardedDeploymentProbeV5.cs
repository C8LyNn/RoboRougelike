// B01-22 P17/W23 ECONCOREGUARD005. Read-only adapter to frozen ECON V4.
// No new owner/lease, wallet, repair, win or session authority.
using System;
using System.Collections.Generic;
using RoboRougelike.B01.Economy.RebuildV3;
using RoboRougelike.B01.Economy.ReadinessV4;

namespace RoboRougelike.B01.Economy.CoreGuardV5
{
    // Implement ONLY from the future canonical private #510 host under its
    // selected #729/#712 single physical owner lease and main-thread quiescence.
    public interface ICanonicalCoreAndEncounterProbeV5
    {
        bool TryObserve(OwnerRebuildStateV3 detachedOwner,
            out CanonicalCoreAndEncounterV5 observation,out string reason);
    }

    public sealed class CanonicalCoreAndEncounterV5
    {
        public string OwnerId,RunId,CorePhysicalPartId;
        public long OwnerGeneration;
        public bool CoreFunctional,CanDeploy,HasGenuineNextChallenge;
        public string[] BlockingPhysicalIds;
    }

    // Compose into EXISTING V4 gate's ITrustedDeploymentProbeV4.
    // V4 alone owns full snapshot double-read / same-generation fork rejection.
    public sealed class CoreGuardedDeploymentProbeV5 : ITrustedDeploymentProbeV4
    {
        readonly ICanonicalCoreAndEncounterProbeV5 canonical;
        public CoreGuardedDeploymentProbeV5(ICanonicalCoreAndEncounterProbeV5 probe)
        {canonical=probe??throw new ArgumentNullException(nameof(probe));}
        static bool Id(string id) =>
            Guid.TryParseExact(id,"N",out var g)&&g!=Guid.Empty&&
            string.Equals(id,g.ToString("N"),StringComparison.Ordinal);

        public bool TryInspectCanonical(OwnerRebuildStateV3 owner,
            out TrustedDeploymentObservationV4 observation,out string reason)
        {
            observation=null;
            if(owner==null||owner.Parts==null)
            {reason="INVALID_OWNER";return false;}
            CanonicalCoreAndEncounterV5 facts;
            try
            {
                if(!canonical.TryObserve(owner.Copy(),out facts,out reason))
                    return false;
            }
            catch(Exception)
            {reason="TRUSTED_CORE_PROBE_EXCEPTION";return false;}
            if(facts==null||facts.OwnerId!=owner.OwnerId||
                facts.RunId!=owner.RunId||facts.OwnerGeneration!=owner.Generation||
                !Id(facts.CorePhysicalPartId)||facts.BlockingPhysicalIds==null)
            {reason="INVALID_CORE_PROOF";return false;}

            // Private canonical host must attest the designated physical Core.
            // Never infer it from untrusted part-name or UI Kind strings.
            OwnedPartV3 core=null;
            foreach(var part in owner.Parts)
            {
                if(part==null||!Id(part.Id))
                {reason="INVALID_OWNER_PART";return false;}
                if(part.Id!=facts.CorePhysicalPartId)continue;
                if(core!=null){reason="DUPLICATE_CORE_ID";return false;}
                core=part;
            }
            if(core==null){reason="CORE_NOT_OWNED";return false;}
            if(float.IsNaN(core.Health)||float.IsInfinity(core.Health)||
                core.Health<0||core.Health>1)
            {reason="INVALID_CORE_HP";return false;}

            bool destroyed=core.Health==0;
            if(destroyed&&facts.CoreFunctional)
            {reason="DEAD_CORE_SPOOFED_FUNCTIONAL";return false;}
            if(!facts.CoreFunctional&&facts.CanDeploy)
            {reason="DISABLED_CORE_SPOOFED_DEPLOYABLE";return false;}
            var blocked=new HashSet<string>(StringComparer.Ordinal);
            foreach(var id in facts.BlockingPhysicalIds)
                if(!Id(id)||!blocked.Add(id))
                {reason="DUPLICATE_OR_INVALID_BLOCKER";return false;}
            if(destroyed&&!blocked.Contains(core.Id))
            {reason="HIDDEN_DEAD_CORE";return false;}
            if(!destroyed&&blocked.Contains(core.Id))
            {reason="LIVE_CORE_CLAIMED_ZERO_HP";return false;}

            observation=new TrustedDeploymentObservationV4{
                OwnerId=facts.OwnerId,RunId=facts.RunId,
                OwnerGeneration=facts.OwnerGeneration,
                CanDeploy=facts.CanDeploy,
                HasGenuineNextChallenge=facts.HasGenuineNextChallenge,
                BlockingPhysicalIds=(string[])facts.BlockingPhysicalIds.Clone()
            };
            reason="OK";return true;
        }
    }
}

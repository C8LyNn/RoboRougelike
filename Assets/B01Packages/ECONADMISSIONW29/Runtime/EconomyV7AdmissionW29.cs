// B01-22/P17W29. Additional fail-closed V7 admission check.
// No economy reducer, no Save writer, no new OS owner and NO policy change.
// The canonical #826 host must call these guards inside its one lifetime
// authoritative transaction when it imports/stages the original V7 section.
using System;
using System.Linq;

namespace RoboRougelike.B01.Economy.AdmissionW29
{
    using RoboRougelike.B01.Economy.ApprovedV7;

    public sealed class CanonicalEconomyIdentityW29
    {
        // Must be derived from the original authenticated #826 full owner
        // session and actual full-world envelope, never UI/client strings.
        public readonly string OwnerId,RunId;
        public readonly long Generation;
        public CanonicalEconomyIdentityW29(string ownerId,string runId,long generation)
        {
            OwnerId=ownerId; RunId=runId; Generation=generation;
        }
    }

    public static class EconomyV7AdmissionW29
    {
        public static bool TryAdmit(ApprovedEconomySnapshotV7 snapshot,
            CanonicalEconomyIdentityW29 trustedIdentity,out string reason)
        {
            reason="INVALID_ORIGINAL_V7";
            if(!ApprovedEconomyPolicyV7.Valid(snapshot))return false;

            // Original V7 Valid accepts a manually built FREE ticket for Wins=0
            // when FreeUsedForWin=0. No genuine victory can have minted it.
            // Legitimate V7 has PAID=0 (Free=false) on a P=1 part; keep that.
            if(snapshot.Wins==0&&snapshot.Pending.Any(t=>t.Free))
            {
                reason="FREE_TICKET_BEFORE_FIRST_AUTHENTIC_VICTORY";
                return false;
            }
            if(trustedIdentity==null||string.IsNullOrWhiteSpace(trustedIdentity.OwnerId)||
                string.IsNullOrWhiteSpace(trustedIdentity.RunId)||
                trustedIdentity.Generation<0)
            {
                reason="MISSING_TRUSTED_CANONICAL_OWNER";
                return false;
            }
            if(!string.Equals(snapshot.OwnerId,trustedIdentity.OwnerId,
                   StringComparison.Ordinal)||
               !string.Equals(snapshot.RunId,trustedIdentity.RunId,
                   StringComparison.Ordinal))
            {
                reason="FOREIGN_OWNER_OR_RUN";
                return false;
            }
            if(snapshot.Generation!=trustedIdentity.Generation)
            {
                reason="FOREIGN_OR_STALE_GENERATION";
                return false;
            }
            reason="OK";
            return true;
        }

        // In-memory section only. Atomic full-world CAS is still the one
        // selected #826 writer's responsibility; checksum != authenticity.
        public static bool TryDecodeForCanonicalOwner(byte[] section,
            CanonicalEconomyIdentityW29 trustedIdentity,
            out ApprovedEconomySnapshotV7 accepted,out string reason)
        {
            accepted=null;
            if(!CanonicalEconomySectionV7.TryDecode(section,
                out var decoded,out reason))return false;
            if(!TryAdmit(decoded,trustedIdentity,out reason))return false;
            accepted=decoded;
            return true;
        }

        public static bool TryEncodeForCanonicalOwner(
            ApprovedEconomySnapshotV7 staged,
            CanonicalEconomyIdentityW29 trustedIdentity,
            out byte[] section,out string reason)
        {
            section=null;
            if(!TryAdmit(staged,trustedIdentity,out reason))return false;
            return CanonicalEconomySectionV7.TryEncode(staged,out section,out reason);
        }
    }
}

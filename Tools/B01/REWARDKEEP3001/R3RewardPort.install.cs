// Receiver installation example. Does NOT copy R3 inventory or integrate into game automatically.
// Verified OBJ #210 public R3 Part and Inventory names. R3/world owns real dropped PartState.
using System;
using System.Collections.Generic;
using System.Linq;
using RoboRougelike.R3;
using RoboRougelike.B01.Rewards;
public interface IAtomicR3RewardHooks
{
    bool IsAuthenticVictory(string encounterId); // authority from completed encounter, never UI claim
    bool WasAlreadyCommitted(string encounterId);
    IReadOnlyCollection<string> OtherCanonicalOwnedIds {get;} // other stored builds, world reward ledger
    // Resolves real physical R3PartState from trusted completed enemy drop store and
    // commits selected objects plus event marker to existing R3 ReceiveSalvage in one transaction.
    bool CommitVerifiedSalvageBatch(string encounterId,IReadOnlyList<RewardCandidate> selection,
        out string reason);
}
public sealed class R3RewardPort : IAuthoritativeRewardPort
{
    private readonly R3VehicleModel model;private readonly IAtomicR3RewardHooks hooks;
    public R3RewardPort(R3VehicleModel liveR3,IAtomicR3RewardHooks authoritativeHooks)
    {model=liveR3??throw new ArgumentNullException(nameof(liveR3));hooks=authoritativeHooks;}
    public bool IsVerifiedVictory(string eventId) => hooks!=null&&hooks.IsAuthenticVictory(eventId);
    public bool WasCommitRecorded(string eventId) => hooks!=null&&hooks.WasAlreadyCommitted(eventId);
    public IReadOnlyCollection<string> ExistingPhysicalIds {
        get {
            var self=model.Parts.Concat(model.Inventory).Select(p=>p.Id);
            // If additional ownership index not supplied, no safe cross-vehicle reward acceptance.
            if(hooks==null||hooks.OtherCanonicalOwnedIds==null)return null;
            return self.Concat(hooks.OtherCanonicalOwnedIds).ToArray();
        }
    }
    public bool TryCommitSalvageAtomically(string eventId,IReadOnlyList<RewardCandidate> selected,out string reason)
    {
        if(hooks==null){reason="TRUSTED_REWARD_HOST_PENDING";return false;}
        return hooks.CommitVerifiedSalvageBatch(eventId,selected,out reason);
    }
}

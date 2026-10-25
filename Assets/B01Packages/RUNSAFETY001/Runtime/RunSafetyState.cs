using System;
using System.Collections.Generic;

namespace B01.RUNSAFETY001
{
    // This package models a decision; it does NOT own the canonical R1/R3 run,
    // reward, currency, part HP or defeat state.
    public enum SafetyDecision
    {
        NormalVehicle, LoanerOffered, LoanerAlreadyActive,
        NoNextChallenge, WrongRunPhase, RunAlreadyEnded, MissingCore,
        UnrelatedBlocker, InvalidSnapshot
    }

    public enum RebuildOrderResult
    {
        QueuedFree, QueuedPaid, InvalidPart, NotDestroyed, AlreadyQueued,
        ActiveEncounter, Unaffordable, InvalidPrice
    }

    public enum ChallengeOutcomeResult
    {
        VictoryAccepted, DefeatRecorded, DuplicateOrStaleEvent
    }

    public enum SafetyPartRole { Core, Weapon, Drive, Structure, Other }

    /// <summary>
    /// Used by an eventual Codex adapter: each boolean must be derived from
    /// canonical actual (not merely present) mounted connectivity/functionality.
    /// The gate for launch is owned by R1RunController; this is only a proposal.
    /// </summary>
    public struct SafetyVehicleSnapshot
    {
        public bool RunActive;
        public bool InGarage;
        public bool NextChallengeAvailable;
        public bool CoreAlive;
        public bool StructureConnected;
        public bool FunctionalWeapon;
        public bool FunctionalDrive;
        public bool CanonicalCanDeploy;
        public bool OtherStartBlocker;

        public SafetyDecision Evaluate()
        {
            if (!RunActive) return SafetyDecision.RunAlreadyEnded;
            if (!InGarage) return SafetyDecision.WrongRunPhase;
            if (!NextChallengeAvailable) return SafetyDecision.NoNextChallenge;
            if (!CoreAlive) return SafetyDecision.MissingCore; // do not resurrect Core
            if (OtherStartBlocker) return SafetyDecision.UnrelatedBlocker;
            if (CanonicalCanDeploy)
            {
                // A false physical-function report cannot be overridden by mere
                // "part present": return a blocker for inconsistent snapshots.
                return (StructureConnected && FunctionalWeapon && FunctionalDrive)
                    ? SafetyDecision.NormalVehicle : SafetyDecision.InvalidSnapshot;
            }
            if (StructureConnected && FunctionalWeapon && FunctionalDrive)
                return SafetyDecision.UnrelatedBlocker; // likely another game guard
            return SafetyDecision.LoanerOffered;
        }
    }

    /// <summary>Owned part simulation keyed by canonical ID, not a new item spawn.</summary>
    public sealed class SafetyOwnedPart
    {
        public string Id { get; }
        public SafetyPartRole Role { get; }
        public int PurchasePrice { get; }
        public int RebuildQuote { get; }
        public int HealthPercent { get; private set; } // 0 = destroyed, 100 = intact
        public bool IsRebuilding { get; private set; }
        public bool IsMounted { get; private set; }
        public int OrderPlacedAtVictorySerial { get; private set; }

        public SafetyOwnedPart(string id, SafetyPartRole role, int hpPercent,
            int purchasePrice, int rebuildQuote, bool mounted)
        {
            if (string.IsNullOrWhiteSpace(id) || hpPercent < 0 || hpPercent > 100 ||
                purchasePrice < 2 || rebuildQuote < 0 || rebuildQuote >= purchasePrice)
                throw new ArgumentException("RUNSAFETY_INVALID_PART_OR_QUOTE");
            Id=id; Role=role; HealthPercent=hpPercent;
            PurchasePrice=purchasePrice; RebuildQuote=rebuildQuote; IsMounted=mounted;
            OrderPlacedAtVictorySerial=-1;
        }

        internal void MarkPending(int currentVictorySerial)
        {
            IsRebuilding=true; IsMounted=false;
            OrderPlacedAtVictorySerial=currentVictorySerial;
        }
        internal void Complete()
        {
            IsRebuilding=false; HealthPercent=100;
            IsMounted=false; // explicit garage mount, never auto-place on robot
            OrderPlacedAtVictorySerial=-1;
        }
        public bool TryMount()
        {
            if (IsRebuilding || HealthPercent == 0 || IsMounted) return false;
            IsMounted=true; return true;
        }
    }

    public struct SafetyLease
    {
        public string EncounterId;
        public string LeaseId;
        public bool UsesLoaner;
        public SafetyLease(string encounterId,bool usesLoaner)
        {
            EncounterId=encounterId;
            UsesLoaner=usesLoaner;
            LeaseId=usesLoaner?"RUNSAFETY-LEASE-"+encounterId:null;
        }
    }

    /// <summary>
    /// Isolated and deterministic prototype of the safety decision and one-win
    /// ECON queue. No cross-run persistence is invented: a future adapter must
    /// persist state in the authoritative save and run lifecycle.
    /// </summary>
    public sealed class RunSafetyState
    {
        private readonly Dictionary<string,SafetyOwnedPart> parts =
            new Dictionary<string,SafetyOwnedPart>(StringComparer.Ordinal);
        private readonly HashSet<string> closedEncounters =
            new HashSet<string>(StringComparer.Ordinal);
        private string activeEncounterId;
        private bool activeLease;
        private bool freeRebuildUsedThisCycle;
        private bool runEnded;

        public int Scrap { get; private set; }
        public int VictorySerial { get; private set; }
        public int AcceptedVictories { get; private set; }
        public int AcceptedDefeats { get; private set; }
        public string ActiveEncounterId => activeEncounterId;
        public bool LoanerInUse => activeEncounterId!=null && activeLease;
        public int PartCount => parts.Count;
        public IReadOnlyCollection<SafetyOwnedPart> Parts => parts.Values;

        public RunSafetyState(IEnumerable<SafetyOwnedPart> initialParts,int startingScrap)
        {
            if (initialParts==null||startingScrap<0)
                throw new ArgumentException("RUNSAFETY_INVALID_INITIAL_STATE");
            foreach (var part in initialParts)
            {
                if (part==null || parts.ContainsKey(part.Id))
                    throw new ArgumentException("RUNSAFETY_DUPLICATE_PART_ID");
                parts.Add(part.Id,part);
            }
            Scrap=startingScrap;
        }

        public SafetyOwnedPart FindPart(string id) =>
            id!=null && parts.TryGetValue(id,out var value) ? value : null;

        public bool FreeRebuildAlreadyClaimed => freeRebuildUsedThisCycle;

        public RebuildOrderResult TryQueueRebuild(string id,out int charged)
        {
            charged=0;
            if (runEnded || activeEncounterId!=null)
                return RebuildOrderResult.ActiveEncounter;
            SafetyOwnedPart part=FindPart(id);
            if(part==null)return RebuildOrderResult.InvalidPart;
            if(part.IsRebuilding)return RebuildOrderResult.AlreadyQueued;
            if(part.HealthPercent>0)return RebuildOrderResult.NotDestroyed;

            // Prices are explicit immutable quotes from ECON; never impose
            // an unapproved percent as a globally canonical price.
            if (part.RebuildQuote >= part.PurchasePrice ||
                part.RebuildQuote<0)
                return RebuildOrderResult.InvalidPrice;
            int cost=freeRebuildUsedThisCycle?part.RebuildQuote:0;
            if(Scrap<cost)return RebuildOrderResult.Unaffordable;
            Scrap-=cost;
            charged=cost;
            if(!freeRebuildUsedThisCycle)freeRebuildUsedThisCycle=true;
            part.MarkPending(VictorySerial);
            return cost==0?RebuildOrderResult.QueuedFree:
                RebuildOrderResult.QueuedPaid;
        }

        public SafetyDecision Evaluate(SafetyVehicleSnapshot snapshot) =>
            runEnded ? SafetyDecision.RunAlreadyEnded :
            activeEncounterId!=null ? SafetyDecision.LoanerAlreadyActive :
            snapshot.Evaluate();

        /// <summary>
        /// Only emits a lease proposal. Does NOT spawn a vehicle or bypass
        /// StartNextEncounter; Codex must own the actual encounter transition.
        /// </summary>
        public bool TryBeginChallenge(string encounterId,SafetyVehicleSnapshot snapshot,
            out SafetyLease lease,out SafetyDecision decision)
        {
            lease=default;
            decision=Evaluate(snapshot);
            if(string.IsNullOrWhiteSpace(encounterId) || closedEncounters.Contains(encounterId) ||
               activeEncounterId!=null)
                return false;
            if(decision!=SafetyDecision.NormalVehicle &&
               decision!=SafetyDecision.LoanerOffered)return false;

            activeEncounterId=encounterId;
            activeLease=decision==SafetyDecision.LoanerOffered;
            lease=new SafetyLease(encounterId,activeLease);
            return true;
        }

        public ChallengeOutcomeResult RecordOutcome(string encounterId,bool victory)
        {
            if (string.IsNullOrWhiteSpace(encounterId) ||
                activeEncounterId==null || encounterId!=activeEncounterId ||
                closedEncounters.Contains(encounterId))
                return ChallengeOutcomeResult.DuplicateOrStaleEvent;
            closedEncounters.Add(encounterId);
            activeEncounterId=null;
            activeLease=false; // lease ceases regardless of outcome; never inventory
            if(!victory)
            {
                AcceptedDefeats++;
                // Canonical R1 decides if this is a run ending defeat. Keep
                // pending orders unchanged; NEVER count a defeat as victory.
                return ChallengeOutcomeResult.DefeatRecorded;
            }
            AcceptedVictories++;
            VictorySerial++;
            foreach (var part in parts.Values)
                if(part.IsRebuilding &&
                   part.OrderPlacedAtVictorySerial < VictorySerial)
                    part.Complete();
            freeRebuildUsedThisCycle=false; // once per NEXT garage victory cycle
            return ChallengeOutcomeResult.VictoryAccepted;
        }

        /// <summary>Read-only to R1; do not use to reset/alter defeat semantics.</summary>
        public void MarkExternalRunEnded()
        {
            if(activeEncounterId!=null)throw new InvalidOperationException(
                "RUNSAFETY_CANNOT_END_WITH_ACTIVE_CHALLENGE");
            runEnded=true;
        }
    }
}

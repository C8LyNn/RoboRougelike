using System;
using System.Collections.Generic;
using System.Linq;
using RoboRougelike.B01.Rewards;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        readonly HashSet<string> committedRecoveryEvents=new HashSet<string>(StringComparer.Ordinal);
        RewardKeep3Service recoveryWindow;
        RecoveryHost recoveryHost;
        public RewardKeep3Service RecoveryWindow=>recoveryWindow;
        public string RecoveryError {get;private set;}

        void ResetRecoveryForEncounter(){recoveryWindow=null;recoveryHost=null;RecoveryError=null;}
        static string RecoveryDefinition(R4SalvageItem item)=>item.Kind==R3PartKind.Gun?"R4/"+item.Weapon:"R3/"+item.Kind;
        void OpenRecoveryWindow()
        {
            // Called only by the authentic cleared-encounter branch after OpenSalvage.
            // This is session state; the game has no durable run-save transaction.
            var evt=runId+"/victory/"+Progress.EncounterIndex;
            recoveryHost=new RecoveryHost(this,VehicleModel,Salvage,evt,committedRecoveryEvents);
            recoveryWindow=new RewardKeep3Service(new R3RewardPort(VehicleModel,recoveryHost));
            var choices=Salvage.Items.Select(p=>new RewardCandidate(p.Id,p.Kind.ToString(),RecoveryDefinition(p),p.Health));
            if(!recoveryWindow.TryOpen(evt,choices,out var error))
            {RecoveryError=error;Debug.LogError("[RC2 recovery] Cannot open authenticated reward window: "+error);}
        }
        bool CommitRecovery(string[] parts,int[] bonuses)
        {
            if(parts==null||bonuses==null||parts.Length>3||parts.Distinct().Count()!=parts.Length||
               bonuses.Length!=1||!Progress.CanCompleteUnifiedRewards(bonuses)||
               recoveryWindow==null||!recoveryWindow.HasOpenWindow||recoveryHost==null)return false;
            var options=recoveryWindow.VisibleOptions;
            if(parts.Any(id=>VehicleModel.Owns(id)||!options.Any(p=>p.InstanceId==id&&p.PartKind!="Armor"&&p.PartKind!="Core")))return false;
            var wanted=new HashSet<string>(parts,StringComparer.Ordinal);
            foreach(var id in recoveryWindow.SelectedIds.ToArray())
                if(!wanted.Contains(id)&&!recoveryWindow.TryToggleSelection(id,out _))return false;
            foreach(var id in parts)
                if(!recoveryWindow.SelectedIds.Contains(id)&&!recoveryWindow.TryToggleSelection(id,out _))return false;
            recoveryHost.BonusChoice=(int[])bonuses.Clone();
            recoveryHost.CommitAuthorized=true;
            try {bool success=recoveryWindow.TryCommit(out var reason);RecoveryError=success?null:reason;return success;}
            finally {recoveryHost.CommitAuthorized=false;}
        }

        sealed class RecoveryHost : IAtomicR3RewardHooks
        {
            readonly R1RunController owner;readonly R3VehicleModel model;readonly R4SalvageLedger ledger;
            readonly string eventId;readonly HashSet<string> committed;
            public int[] BonusChoice;
            public bool CommitAuthorized;
            public RecoveryHost(R1RunController run,R3VehicleModel state,R4SalvageLedger drops,string evt,HashSet<string> receipts)
            {owner=run;model=state;ledger=drops;eventId=evt;committed=receipts;}
            // There is one owned robot in this game session. Garage designs are
            // layouts, not a second persistent physical inventory.
            public IReadOnlyCollection<string> OtherCanonicalOwnedIds=>Array.Empty<string>();
            public bool IsAuthenticVictory(string evt)=>evt==eventId&&owner!=null&&owner.VehicleModel==model&&
                owner.Salvage==ledger&&!ledger.Closed&&owner.Progress?.State==R1RunState.Salvage;
            public bool WasAlreadyCommitted(string evt)=>committed.Contains(evt);
            public bool CommitVerifiedSalvageBatch(string evt,IReadOnlyList<RewardCandidate> selection,out string reason)
            {
                reason="INVALID_RECOVERY_TRANSACTION";
                if(!CommitAuthorized||!IsAuthenticVictory(evt)||committed.Contains(evt)||selection==null||BonusChoice==null)return false;
                foreach(var choice in selection)
                {
                    var original=ledger.Items.FirstOrDefault(p=>p.Id==choice.InstanceId);
                    if(original==null||!original.Selectable||choice.PartKind!=original.Kind.ToString()||
                       choice.DefinitionId!=RecoveryDefinition(original)||choice.Health!=original.Health)
                    {reason="FORGED_OR_STALE_DROP";return false;}
                }
                // Existing R4 performs the one atomic physical batch: original
                // enemy snapshots + independent bonus + scrap + progress.
                if(!R4RewardSettlement.TryClaim(owner.Progress,ledger,model,selection.Select(p=>p.InstanceId),BonusChoice))
                {reason="INVENTORY_OR_REWARD_COMMIT_REFUSED";return false;}
                committed.Add(evt);reason="OK";return true;
            }
        }
    }
}

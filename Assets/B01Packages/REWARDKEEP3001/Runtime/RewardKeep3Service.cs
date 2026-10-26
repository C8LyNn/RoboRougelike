// B01-22 REWARD-KEEP3-001 R1: ONE post-combat selection, NOT an inventory.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RoboRougelike.B01.Rewards
{
    [Serializable] public sealed class RewardCandidate
    {
        public string InstanceId, PartKind, DefinitionId;
        public float Health;
        public RewardCandidate(){}
        public RewardCandidate(string id,string kind,string def,float health)
        { InstanceId=id;PartKind=kind;DefinitionId=def;Health=health; }
        public RewardCandidate Copy()=>new RewardCandidate(InstanceId,PartKind,DefinitionId,Health);
    }
    public interface IAuthoritativeRewardPort
    {
        // Game-provided genuine encounter WIN proof, NOT a UI-controlled boolean.
        bool IsVerifiedVictory(string encounterEventId);
        // Existing live R3 Parts + Inventory across the relevant ownership domain.
        IReadOnlyCollection<string> ExistingPhysicalIds {get;}
        bool WasCommitRecorded(string encounterEventId);
        // Must atomically grant selected physical instances via existing R3 ReceiveSalvage
        // AND persist event id; false MUST mean zero host changes.
        bool TryCommitSalvageAtomically(string encounterEventId,
            IReadOnlyList<RewardCandidate> selected,out string error);
    }
    [Serializable] public sealed class RewardWindowSave
    {
        public int Version=1;
        public string ActiveEncounterId;
        public RewardCandidate[] Offered;
        public string[] SelectedPhysicalIds;
        public string[] CompletedEncounterIds;
        public RewardWindowSave(){}
    }
    public sealed class RewardKeep3Service
    {
        public const int MaxKeep=3;
        private readonly object gate=new object();
        private readonly IAuthoritativeRewardPort host;
        private string activeEvent;
        private RewardCandidate[] offered;
        private readonly HashSet<string> selected=new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> completed=new HashSet<string>(StringComparer.Ordinal);
        private bool quarantined;
        public RewardKeep3Service(IAuthoritativeRewardPort authoritative) {
            host=authoritative??throw new ArgumentNullException(nameof(authoritative));
        }
        // Exact canonical R3PartKind names from verified OBJ #210. No casing aliases:
        // an offered "armor" must never be reinterpreted as lootable equipment.
        private static readonly HashSet<string> KnownKinds=new HashSet<string>(StringComparer.Ordinal)
        {
            "Armor","Wheel","Gun","Core","Suspension","Booster","JumpDrive","Blink",
            "Gyroscope","OmniDrive","MagneticGrapple","HoverEngine","CrawlerTrack",
            "BonusModule","DirectionalShield","PointDefense","StructuralScanner",
            "ScannerPulse","IndependentSteering"
        };

        private static bool ValidGuid(string id) {
            Guid parsed;
            return !string.IsNullOrEmpty(id)&&Guid.TryParseExact(id,"N",out parsed)&&
                   parsed!=Guid.Empty&&id==parsed.ToString("N");
        }
        private bool ValidateOwner(out string error) {
            if(host.ExistingPhysicalIds==null){error="OWNER_UNAVAILABLE";return false;}
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var id in host.ExistingPhysicalIds)
                if(!ValidGuid(id)||!seen.Add(id)){error="DUPLICATE_OR_INVALID_OWNED_ID";return false;}
            error="OK";return true;
        }
        private bool ValidateOffer(RewardCandidate[] choices,out string error) {
            if(choices==null){error="INVALID_OFFER";return false;}
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var c in choices) {
                if(c==null||!ValidGuid(c.InstanceId)||!seen.Add(c.InstanceId)||
                   string.IsNullOrWhiteSpace(c.PartKind) || !KnownKinds.Contains(c.PartKind) || c.PartKind=="Core" ||
                   float.IsNaN(c.Health)||float.IsInfinity(c.Health)||c.Health<0||c.Health>1) {
                    error="INVALID_OR_DUPLICATE_OFFER";return false;
                }
            }
            error="OK";return true;
        }
        public int SelectedCount {get {lock(gate)return selected.Count;} }
        public bool HasOpenWindow {get {lock(gate)return activeEvent!=null;} }
        public bool IsQuarantined {get {lock(gate)return quarantined;} }
        public IReadOnlyList<RewardCandidate> VisibleOptions {
            get {lock(gate)return (offered??Array.Empty<RewardCandidate>()).Select(c=>c.Copy()).ToArray();}
        }
        public IReadOnlyList<string> SelectedIds {
            get {lock(gate)return selected.OrderBy(x=>x,StringComparer.Ordinal).ToArray();}
        }
        public bool TryOpen(string verifiedEncounterId,IEnumerable<RewardCandidate> candidates,out string error) {
            lock(gate) {
                if(quarantined){error="REWARD_QUARANTINED";return false;}
                if(activeEvent!=null){error="WINDOW_ALREADY_OPEN";return false;}
                if(string.IsNullOrWhiteSpace(verifiedEncounterId)){error="INVALID_ENCOUNTER";return false;}
                if(completed.Contains(verifiedEncounterId)||host.WasCommitRecorded(verifiedEncounterId))
                {error="ENCOUNTER_ALREADY_COMMITTED";return false;}
                if(!host.IsVerifiedVictory(verifiedEncounterId))
                {error="VICTORY_NOT_VERIFIED";return false;}
                if(!ValidateOwner(out error))return false;
                var items=candidates==null?null:candidates.Select(c=>c?.Copy()).ToArray();
                if(!ValidateOffer(items,out error))return false;
                var owned=new HashSet<string>(host.ExistingPhysicalIds,StringComparer.Ordinal);
                if(items.Any(p=>owned.Contains(p.InstanceId)))
                {error="OFFER_CONTAINS_OWNED_INSTANCE";return false;}
                activeEvent=verifiedEncounterId;
                offered=items;selected.Clear();error="OK";return true;
            }
        }
        public bool TryToggleSelection(string instanceId,out string error) {
            lock(gate) {
                if(quarantined){error="REWARD_QUARANTINED";return false;}
                if(activeEvent==null){error="NO_OPEN_WINDOW";return false;}
                if(!ValidGuid(instanceId)){error="INVALID_INSTANCE_ID";return false;}
                var item=offered.FirstOrDefault(x=>x.InstanceId==instanceId);
                if(item==null){error="ITEM_NOT_OFFERED";return false;}
                if(selected.Remove(instanceId)){error="OK";return true;}
                if(item.PartKind=="Armor"){error="ARMOR_EXCLUDED";return false;}
                if(selected.Count>=MaxKeep){error="KEEP_LIMIT_3";return false;}
                selected.Add(instanceId);error="OK";return true;
            }
        }
        public bool TryCommit(out string error) {
            lock(gate) {
                if(quarantined){error="REWARD_QUARANTINED";return false;}
                if(activeEvent==null){error="NO_OPEN_WINDOW";return false;}
                if(!host.IsVerifiedVictory(activeEvent))
                {error="VICTORY_NOT_VERIFIED";return false;}
                if(host.WasCommitRecorded(activeEvent)||completed.Contains(activeEvent))
                {error="ENCOUNTER_ALREADY_COMMITTED";return false;}
                if(!ValidateOwner(out error))return false;
                var chosen=offered.Where(x=>selected.Contains(x.InstanceId))
                    .OrderBy(x=>x.InstanceId,StringComparer.Ordinal).Select(x=>x.Copy()).ToArray();
                if(chosen.Length>MaxKeep||chosen.Any(x=>x.PartKind=="Armor"))
                {error="INVALID_SELECTION";return false;}
                var existing=new HashSet<string>(host.ExistingPhysicalIds,StringComparer.Ordinal);
                if(chosen.Any(x=>existing.Contains(x.InstanceId)))
                {error="DUPLICATE_PHYSICAL_INSTANCE";return false;}
                if(!host.TryCommitSalvageAtomically(activeEvent,chosen,out error))return false;
                // Postcommit prove event journal AND all selected instance IDs exist exactly once.
                var after=host.ExistingPhysicalIds;
                if(!host.WasCommitRecorded(activeEvent)||after==null||
                   after.Count!=after.Distinct(StringComparer.Ordinal).Count()||
                   chosen.Any(x=>after.Count(id=>id==x.InstanceId)!=1))
                {
                    quarantined=true;error="HOST_REWARD_CONTRACT_BROKEN";return false;
                }
                completed.Add(activeEvent);activeEvent=null;offered=null;selected.Clear();
                error="OK";return true;
            }
        }
        public bool TrySave(out string json,out string error) {
            lock(gate){
                json=null;
                if(quarantined){error="REWARD_QUARANTINED";return false;}
                if(!ValidateOwner(out error))return false;
                var data=new RewardWindowSave {
                    ActiveEncounterId=activeEvent,
                    Offered=(offered??Array.Empty<RewardCandidate>()).Select(p=>p.Copy()).ToArray(),
                    SelectedPhysicalIds=selected.OrderBy(x=>x,StringComparer.Ordinal).ToArray(),
                    CompletedEncounterIds=completed.OrderBy(x=>x,StringComparer.Ordinal).ToArray()
                };
                json=JsonUtility.ToJson(data,true);error="OK";return true;
            }
        }
        public bool TryRestore(string json,out string error) {
            lock(gate) {
                if(quarantined){error="REWARD_QUARANTINED";return false;}
                RewardWindowSave data;
                try{data=JsonUtility.FromJson<RewardWindowSave>(json);}
                catch(Exception){error="CORRUPT_SAVE";return false;}
                if(data==null||data.Version!=1||data.Offered==null||
                   data.SelectedPhysicalIds==null||data.CompletedEncounterIds==null||
                   !ValidateOffer(data.Offered,out error)) {error="CORRUPT_SAVE";return false;}
                var done=new HashSet<string>(StringComparer.Ordinal);
                foreach(var evt in data.CompletedEncounterIds) {
                    if(string.IsNullOrWhiteSpace(evt)||!done.Add(evt)||!host.WasCommitRecorded(evt)) {
                        error="CORRUPT_COMMITTED_EVENT";return false;
                    }
                }
                var chosen=new HashSet<string>(StringComparer.Ordinal);
                foreach(var id in data.SelectedPhysicalIds)
                    if(!ValidGuid(id)||!chosen.Add(id))
                    {error="CORRUPT_SELECTION";return false;}
                if(chosen.Count>MaxKeep||chosen.Any(id=>!data.Offered.Any(p=>p.InstanceId==id&&p.PartKind!="Armor")))
                {error="CORRUPT_SELECTION";return false;}
                if(!ValidateOwner(out error))return false;
                bool open=!string.IsNullOrEmpty(data.ActiveEncounterId);
                if(!open&&(data.Offered.Length>0||chosen.Count>0)||
                   open&&(done.Contains(data.ActiveEncounterId)||host.WasCommitRecorded(data.ActiveEncounterId)||
                        !host.IsVerifiedVictory(data.ActiveEncounterId)||
                        data.Offered.Any(p=>host.ExistingPhysicalIds.Contains(p.InstanceId))))
                {error="CORRUPT_OR_STALE_REWARD_WINDOW";return false;}
                completed.Clear();foreach(var evt in done)completed.Add(evt);
                activeEvent=open?data.ActiveEncounterId:null;
                offered=open?data.Offered.Select(p=>p.Copy()).ToArray():null;
                selected.Clear();foreach(var id in chosen)selected.Add(id);
                error="OK";return true;
            }
        }
    }
}

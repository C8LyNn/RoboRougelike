// B01-22 W17 source-only QA. Original frozen #252 C# compiled, unchanged.
// Detector green means original source RELEASE RED (never native Unity).
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using RoboRougelike.B01.Economy.Rebuild;

sealed class ReadOnlyHost : IAuthoritativeRebuildPort {
    public readonly List<RebuildPartView> Parts=new();
    public readonly Dictionary<string,string> Reservations=new(StringComparer.Ordinal);
    public int Cash=1000, Completions;
    public IReadOnlyList<RebuildPartView> OwnedParts=>Parts;
    public int PurchasePrice(string id)=>100;
    public int PaidRebuildQuote(string id)=>70;
    public bool HasReservation(string id,string key)=>Reservations.TryGetValue(id,out var value)&&value==key;
    public bool TryReserveAndDebitAtomically(string id,int cost,string key,out string reason){
        var part=Parts.SingleOrDefault(p=>p.InstanceId==id);
        if(part==null||part.Health!=0||Reservations.ContainsKey(id)||cost>Cash){
            reason="REJECT";return false;
        }
        Cash-=cost;Reservations.Add(id,key);reason="OK";return true;
    }
    public bool TryCompleteReservedBatchAtomically(string ev,IReadOnlyList<RebuildTicket> tickets,out string reason){
        if(tickets.Any(t=>!HasReservation(t.InstanceId,t.ReservationKey))){reason="REJECT";return false;}
        foreach(var t in tickets){
            Parts.Single(p=>p.InstanceId==t.InstanceId).Health=1;
            Reservations.Remove(t.InstanceId);
        }
        Completions++;reason="OK";return true;
    }
}

sealed class ReplayProcessSnapshot {
    public string Schema {get;set;}
    public string StaleCheckpoint {get;set;}
    public string CurrentCheckpoint {get;set;}
    public string PhysicalId {get;set;}
    public string PartKind {get;set;}
    public float PartHealth {get;set;}
    public int Cash {get;set;}
    public int CompletedHostBatches {get;set;}
}

static class Program {
    const string Source="12572117a3dce369257feabc57021cde58023369";
    static void Assert(bool b,string why){if(!b)throw new InvalidOperationException(why);}
    static string Save(RebuildQueueService q){
        Assert(q.TrySave(out var data,out var err),"SAVE:"+err);
        return data;
    }
    static void CurrentCheckpoint(){
        var h=new ReadOnlyHost();
        string id="11111111111111111111111111111111";
        h.Parts.Add(new RebuildPartView(id,"Wheel",0));
        var q=new RebuildQueueService(h);
        Assert(q.TryQueueDestroyed(id,out var err),"QUEUE:"+err);
        var current=Save(q);
        var reloaded=new RebuildQueueService(h);
        Assert(reloaded.TryRestore(current,out err),"CURRENT_RESTORE:"+err);
        Assert(reloaded.PendingCount==1&&!reloaded.FreeSlotAvailable,"CURRENT_CHECKPOINT_INCORRECT");
        Assert(h.Reservations.Count==1&&h.Cash==1000,"HOST_CHANGED_ON_RESTORE");
        Console.WriteLine("PASS current authorized checkpoint source C#");
    }
    static object StaleReplay(){
        var h=new ReadOnlyHost();
        string id="22222222222222222222222222222222";
        h.Parts.Add(new RebuildPartView(id,"Gun",0));
        var q=new RebuildQueueService(h);
        var stale=Save(q); // before win: Wins 0, no events, no reservations
        Assert(q.TryQueueDestroyed(id,out var err),"QUEUE:"+err);
        Assert(q.TryFinishVictory("win-0001",true,out err),"FIRST_WIN:"+err);
        Assert(q.WonChallenges==1&&h.Reservations.Count==0&&h.Parts[0].Health==1,
            "FIRST_WIN_POSTCONDITIONS");
        var valid=Save(q);
        bool acceptedStale=q.TryRestore(stale,out var restoreReason);
        var afterStale=acceptedStale?Save(q):null;
        bool replayAccepted=acceptedStale&&q.TryFinishVictory("win-0001",true,out err);
        Assert(acceptedStale,"DEFECT_NOT_REPRODUCED:stale restore refused");
        Assert(replayAccepted,"DEFECT_NOT_REPRODUCED:replayed event refused");
        Assert(h.Completions==1&&h.Parts[0].Health==1,"UNEXPECTED_PHYSICAL_MUTATION");
        Assert(valid!=afterStale,"HISTORY_NOT_REWOUND");
        Console.WriteLine("QA_DETECTOR_PASS stale save accepted + victory ID replayed");
        return new {
            name="stale_pre_victory_checkpoint_rewinds_victory_history",
            detector="PASS",source_release="RED",
            stale_save_accepted=acceptedStale,
            replay_of_original_win_id_accepted=replayAccepted,
            physical_completion_count=h.Completions,
            native_unity="NOT_RUN"
        };
    }
    static void SaveProcessState(string path) {
        var host=new ReadOnlyHost();
        string id="33333333333333333333333333333333";
        host.Parts.Add(new RebuildPartView(id,"Track",0));
        var q=new RebuildQueueService(host);
        string old=Save(q);
        Assert(q.TryQueueDestroyed(id,out var err),"PROC1_QUEUE:"+err);
        Assert(q.TryFinishVictory("persisted-victory-01",true,out err),"PROC1_WIN:"+err);
        Assert(q.WonChallenges==1&&host.Completions==1&&host.Reservations.Count==0
            &&host.Parts.Single().Health==1,"PROC1_HOST_DID_NOT_COMMIT");
        var info=new ReplayProcessSnapshot {
            Schema="b01-22-w18-original-source-restart-v1",
            StaleCheckpoint=old,CurrentCheckpoint=Save(q),
            PhysicalId=id,PartKind=host.Parts.Single().PartKind,
            PartHealth=host.Parts.Single().Health,Cash=host.Cash,
            CompletedHostBatches=host.Completions
        };
        File.WriteAllText(path,JsonSerializer.Serialize(info,new JsonSerializerOptions
            {WriteIndented=true})+"\n");
        Console.WriteLine("PASS process1: real original C# win and physical snapshot serialized");
    }
    static void ProbeFreshProcess(string input,string output) {
        var info=JsonSerializer.Deserialize<ReplayProcessSnapshot>(File.ReadAllText(input));
        Assert(info!=null&&info.Schema=="b01-22-w18-original-source-restart-v1",
            "BAD_ORIGINAL_SNAPSHOT");
        var host=new ReadOnlyHost{Cash=info.Cash,Completions=info.CompletedHostBatches};
        host.Parts.Add(new RebuildPartView(info.PhysicalId,info.PartKind,info.PartHealth));
        var fresh=new RebuildQueueService(host);
        Assert(fresh.TryRestore(info.CurrentCheckpoint,out var reason),
            "PROC2_CURRENT_RESTORE:"+reason);
        bool goodReplayRejected=!fresh.TryFinishVictory("persisted-victory-01",true,out reason);
        Assert(goodReplayRejected&&reason=="VICTORY_EVENT_REPLAY",
            "CURRENT_PERSISTED_EVENT_REPLAY_NOT_REJECTED");
        bool oldAccepted=fresh.TryRestore(info.StaleCheckpoint,out reason);
        var restored=oldAccepted?Save(fresh):null;
        bool replay=oldAccepted&&fresh.TryFinishVictory("persisted-victory-01",true,out reason);
        Assert(oldAccepted&&replay,"CROSS_PROCESS_DEFECT_NOT_REPRODUCED");
        Assert(restored==info.StaleCheckpoint,"EXPECTED_OLD_METADATA_NOT_RESTORED");
        Assert(host.Completions==1&&host.Cash==info.Cash&&host.Parts.Single().Health==1
            &&host.Reservations.Count==0,"PHYSICAL_STATE_CHANGED");
        var result=new {
            schema="b01-22-w18-cross-process-qa-v1",
            producer_source_sha=Source,
            source_blob_sha1="cac95b28647528a7a46cb818c6d78037dc3e80a2",
            actual_distinct_process_runs=true,
            host_state_restored_from_disk=true,
            current_checkpoint_rejected_duplicate_victory_id=goodReplayRejected,
            stale_checkpoint_accepted=oldAccepted,
            same_consumed_victory_id_replayed=replay,
            host_physical_batch_completed_count=host.Completions,
            qa_detector="PASS",source_release="RED",
            live_canonical_unity_host="NOT_TESTED",native_unity="NOT_RUN"
        };
        File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions
            {WriteIndented=true})+"\n");
        Console.WriteLine("QA_DETECTOR_PASS cross-process stale save + same event replay; SOURCE_RELEASE_RED; NATIVE_UNITY_NOT_RUN");
    }
    static int Main(string[] args){
        if(args.Length!=1 && args.Length!=2 && args.Length!=3)return 2;
        try{
            if(args.Length==2 && args[0]=="--make-process-snapshot"){
                SaveProcessState(args[1]);return 0;
            }
            if(args.Length==3 && args[0]=="--probe-after-process-restart"){
                ProbeFreshProcess(args[1],args[2]);return 0;
            }
            if(args.Length!=1)return 2;
            CurrentCheckpoint();
            var result=StaleReplay();
            string json=JsonSerializer.Serialize(new {
                schema="b01-22-w17-frozen-econ-source-witness-v1",
                producer_sha=Source,
                source_blob_sha1="cac95b28647528a7a46cb818c6d78037dc3e80a2",
                qa_detector="PASS",source_release="RED",
                live_integrated_game="NOT_TESTED",native_unity="NOT_RUN",
                cases=new object[]{new{name="valid_rebuild_checkpoint",detector="PASS"},result}
            },new JsonSerializerOptions{WriteIndented=true});
            File.WriteAllText(args[0],json+"\n");
            Console.WriteLine("QA_DETECTOR_PASS 2/2; SOURCE_RELEASE_RED; NATIVE_UNITY_NOT_RUN");
            return 0;
        }catch(Exception ex){
            Console.Error.WriteLine("WITNESS_ERROR:"+ex);return 1;
        }
    }
}

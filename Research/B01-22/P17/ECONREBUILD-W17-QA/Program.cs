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
    static int Main(string[] args){
        if(args.Length!=1)return 2;
        try{
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

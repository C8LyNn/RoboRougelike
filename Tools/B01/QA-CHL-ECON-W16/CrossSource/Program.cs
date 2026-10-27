// B01-22 W16 QA-ONLY: linked ACTUAL immutable CHL #285 C# source; NOT Unity/PhysX/R3.
using B01.CHL003;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

static class Program
{
    const string Pin="23d40eb21a7c0db97238516c9e9ec6e708f54998";
    static readonly List<(string Name,string Status,string Detail)> Cases=new();
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    static CHL003CourseCore New(int n=4,int actor=101)
    {
        var c=new CHL003CourseCore();
        c.Configure(n,actor);
        return c;
    }
    static void Finish(CHL003CourseCore c,int actor=101)
    {
        Check(c.Begin(),"begin rejected");
        for(int i=0;i<c.CheckpointCount;i++)Check(c.EnterCheckpoint(actor,i),"gate "+i);
        Check(c.EnterFinish(actor),"finish rejected");
    }
    static void Run(string name,Action fn)
    {
        try{fn();Cases.Add((name,"PASS",""));Console.WriteLine("PASS "+name);}
        catch(Exception e){Cases.Add((name,"FAIL",e.Message));Console.WriteLine("FAIL "+name+" "+e.Message);}
    }
    sealed class DetachedHostWitness
    {
        public int Scrap=175;
        public readonly Dictionary<string,float> Health=new(){{"part-A",1f},{"part-B",0.0f}};
        public readonly string[] PhysicalInventory={"part-A","part-B"};
        public readonly string SerializedVehicle="{\"modules\":[\"part-A\",\"part-B\"],\"rotation\":2}";
        public string Snapshot()=>JsonSerializer.Serialize(new{Scrap,HP=Health.OrderBy(x=>x.Key),Inventory=PhysicalInventory,SerializedVehicle});
    }
    static int Main(string[] args)
    {
        Run("C01_success_exactly_one_completed_within_attempt",()=>{
            var c=New();int calls=0;c.Completed+=_=>calls++;
            Finish(c);Check(calls==1,"winner count");
            Check(!c.EnterFinish(101),"duplicate finish accepted");
            Check(calls==1,"duplicate callback");
        });
        Run("C02_wrong_actor_skipped_checkpoint_and_early_finish_no_event",()=>{
            var c=New();int calls=0;c.Completed+=_=>calls++;
            Check(c.Begin(),"begin");
            Check(!c.EnterFinish(101),"early finish");
            Check(!c.EnterCheckpoint(102,0),"foreign actor");
            Check(!c.EnterCheckpoint(101,1),"skip");
            Check(!c.EnterFinish(102),"foreign finish");
            Check(calls==0,"unauthorized completion callbacks");
        });
        Run("C03_fall_fail_and_retry_no_completion",()=>{
            var c=New();int calls=0;c.Completed+=_=>calls++;
            Check(c.Begin(),"begin");
            Check(c.Fail(101,CHL003CourseCore.Failure.FellOffCourse),"fail");
            Check(!c.EnterFinish(101),"finished failed activity");
            c.Retry();Check(c.State==CHL003CourseCore.Phase.Ready,"retry state");
            Check(calls==0,"failed/retry created win callback");
        });
        Run("C04_cancel_and_pause_never_create_completion",()=>{
            var c=New();int calls=0;c.Completed+=_=>calls++;c.Begin();c.SetPaused(true);
            Check(!c.EnterCheckpoint(101,0),"gate while paused");
            Check(!c.EnterFinish(101),"finish while paused");
            c.Cancel();
            Check(c.State==CHL003CourseCore.Phase.Ready&&calls==0,"cancel created completion");
        });
        Run("C05_three_retries_reemit_completed_across_attempts",()=>{
            var c=New();int calls=0;c.Completed+=_=>calls++;
            for(int i=0;i<3;i++){if(i>0)c.Retry();Finish(c);}
            Check(calls==3,"expected per-attempt event not 3");
            Check(c.State==CHL003CourseCore.Phase.Completed,"last state");
            // Receiver MUST NOT equate three Completed callbacks with three rewards.
        });
        Run("C06_duplicate_external_subscriptions_emits_two_callbacks",()=>{
            var c=New();int calls=0;Action<float> onFinish=_=>calls++;
            c.Completed+=onFinish;c.Completed+=onFinish;Finish(c);
            Check(calls==2,"double subscriber should make host adapter liability visible");
            c.Completed-=onFinish;c.Completed-=onFinish;
            c.Retry();Finish(c);Check(calls==2,"callback leaked after unsubscribe");
        });
        Run("C07_no_mutation_to_disconnected_host_wallet_hp_inventory_or_saved_design",()=>{
            var h=new DetachedHostWitness();string before=h.Snapshot();
            var c=New();int completed=0;c.Completed+=_=>completed++;
            Finish(c);
            c.Retry();c.Begin();
            Check(c.EnterCheckpoint(101,0),"checkpoint");c.Cancel();
            c.Retry();Finish(c);
            Check(completed==2,"attempt count");
            Check(h.Snapshot()==before,"disconnected R3 witness modified");
            // Weak scope: receiver NOT connected here. Static full source inspection checks non-reachability.
        });
        Run("C08_failed_then_win_no_event_on_failure_and_one_on_success",()=>{
            var c=New();int calls=0;c.Completed+=_=>calls++;c.Begin();
            Check(c.Fail(101,CHL003CourseCore.Failure.FellOffCourse),"failure");
            Check(calls==0,"failure reported completion");
            c.Retry();Finish(c);Check(calls==1,"win after retry missing/duplicate");
        });
        Run("C09_randomized_512_replay_boundaries",()=>{
            var rng=new Random(20261027);
            for(int t=0;t<512;t++)
            {
                int n=rng.Next(3,6),actor=rng.Next(1000,99999);
                var c=New(n,actor);int wins=0;c.Completed+=_=>wins++;
                c.Begin();
                for(int gate=0;gate<n;gate++)
                {
                    Check(!c.EnterCheckpoint(actor+1,gate),"foreign actor");
                    Check(!c.EnterCheckpoint(actor,n),"skip attempt");
                    Check(c.EnterCheckpoint(actor,gate),"valid gate");
                    Check(!c.EnterCheckpoint(actor,gate),"duplicate gate");
                }
                Check(c.EnterFinish(actor),"valid finish");
                Check(!c.EnterFinish(actor),"replay finish");
                c.Retry();
                Check(c.State==CHL003CourseCore.Phase.Ready&&wins==1,"reset/replay drift");
                Check(!c.EnterFinish(actor),"early finish after reset");
            }
        });
        Run("C10_completed_event_exception_cannot_revert_completed_phase",()=>{
            var c=New();c.Completed+=_=>throw new InvalidOperationException("injected host subscriber failure");
            bool threw=false;try{Finish(c);}catch(InvalidOperationException){threw=true;}
            Check(threw,"callback exception not observed");
            Check(c.State==CHL003CourseCore.Phase.Completed,"state reverted");
            Check(!c.EnterFinish(101),"another finish accepted after exception");
            // Without transactional host settlement, subscriber exception leaves ambiguous external status.
        });
        Run("C11_real_finish_arbiter_no_same_step_or_nonoverlap_completion",()=>{
            var a=new CHL003FinishArbiter();
            Check(!a.ObserveFinishContact(4,false),"early finish queued");
            a.LastCheckpointAccepted(5);
            Check(a.ObserveFinishContact(5,true),"last-checkpoint/finish same step not queued");
            Check(!a.TryCommit(5,true,true,false),"same-step committed");
            Check(!a.TryCommit(6,true,false,false),"no-overlap committed");
            Check(!a.IsQueued,"stale finish after moved outside");
            Check(a.ObserveFinishContact(7,true),"new contact");
            Check(a.TryCommit(8,true,true,false),"physical overlapped next-step not committed");
            a.Reset();Check(!a.IsQueued,"retry retained finish");
        });
        Run("C12_real_trial_metrics_reset_does_not_retire_host_inventory",()=>{
            var m=new CHL003TrialMetrics();
            m.Reset("LIGHT",1,270f,2f,2.3f);m.Begin();
            m.RecordPhysics(0f,1f,1f,2f,0.1f);
            for(int k=1;k<=4;k++)m.RecordCheckpoint(k,k*0.2f);
            m.RecordCompleted(1.5f);
            Check(m.Completions==1 && m.TerminalState=="Completed","metrics finish");
            Check(m.ToCsv().Contains("COMPLETE"),"csv missing event");
            m.Reset("LIGHT",2,270f,2f,2.3f);
            Check(m.Completions==0 && m.Checkpoints==0 && m.Events.Count==1,
               "retry preserved stale metrics/reward-like event");
        });
        int pass=Cases.Count(x=>x.Status=="PASS");
        var result=new{source_pin=Pin,core_source="ACTUAL_CHL_285_CSHARP_DIRECTLY_COMPILED",
          host="DETACHED_TEST_WITNESS_ONLY",unity="NOT_RUN",physx="NOT_RUN",
          host_atomic_integration="NOT_RUN",passed=pass,total=Cases.Count,
          failures=Cases.Count-pass,
          receiver_risks=new[]{"COMPLETED_EMITS_AGAIN_AFTER_RETRY",
            "DUPLICATE_SUBSCRIBER_EMITS_DUPLICATE_CALLBACK",
            "THROWING_SUBSCRIBER_LEAVES_COMPLETED_STATE"},
          cases=Cases.Select(x=>new{name=x.Name,result=x.Status,detail=x.Detail})};
        string dest=args.Length>0?args[0]:"artifacts/chl-econ/chl-realcore-results.json";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dest))!);
        File.WriteAllText(dest,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("CHL285_ORIGINAL_CSHARP_CORE_CROSS_BOUNDARY "+pass+"/"+Cases.Count+
            " NATIVE_UNITY_NOT_RUN HOST_ATOMICITY_NOT_RUN");
        return pass==Cases.Count?0:1;
    }
}

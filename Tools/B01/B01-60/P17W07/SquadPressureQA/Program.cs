// B01-60 / PASS 17 WAVE 07: NEW, independently written state-machine QA.
// Compiles the unchanged B01-24 SquadPressure.cs and unchanged original Codex
// R3EnemyArchetype.cs. No Unity mocks or source copying. NOT native Unity.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RoboRougelike.R3;
using RoboRougelike.B0124.SquadPressure;

internal static class Program
{
    private static int tests=0, failed=0, transitions=0, starts=0, ends=0, actives=0, idles=0;
    private static readonly StringBuilder trace=new StringBuilder();
    private static void Ensure(bool ok,string message)
    {
        if(!ok)throw new InvalidOperationException(message);
    }
    private static void Test(string name, Action run)
    {
        try {run();tests++;Console.WriteLine("PASS "+name);}
        catch(Exception e) {failed++;Console.Error.WriteLine("FAIL "+name+" "+e);}
    }
    private static void MustReject(Action run, string name)
    {
        try {run();}catch(ArgumentException){return;}
        throw new InvalidOperationException("UNSAFE_INPUT_ACCEPTED: "+name);
    }
    private static bool Same(PressurePulse a,PressurePulse b)
    {
        return a.State==b.State && a.Intent==b.Intent &&
               a.MemberId==b.MemberId && a.UntilTick==b.UntilTick &&
               a.MarkedByScout==b.MarkedByScout;
    }
    private static void Match(PressurePulse a,PressurePulse b,string where)
    {
        Ensure(Same(a,b),where+" mismatch "+a.State+"/"+a.MemberId+" vs "+b.State+"/"+b.MemberId);
    }
    private struct XorShift
    {
        private uint x;
        public XorShift(uint seed){x=seed==0?0x9e3779b9U:seed;}
        public uint Next(){x^=x<<13;x^=x>>17;x^=x<<5;return x;}
        public int Range(int n){return (int)(Next()%(uint)n);}
    }
    private static readonly R3EnemyArchetype[] Roles = {
        R3EnemyArchetype.Scout, R3EnemyArchetype.Bruiser,
        R3EnemyArchetype.Gunner, R3EnemyArchetype.Flanker
    };
    private static readonly PressureIntent[] Intents = {
        PressureIntent.ScoutMark, PressureIntent.BruiserPush,
        PressureIntent.GunnerVolley, PressureIntent.FlankerPincer
    };
    private static PressureObservation[] Eligible(PressureMember[] roster)
    {
        return roster.Select(m=>new PressureObservation(m.Id,true,true,true,6f)).ToArray();
    }
    private static PressureObservation[] Shuffled(PressureObservation[] source,ref XorShift rng)
    {
        PressureObservation[] b=(PressureObservation[])source.Clone();
        for(int i=b.Length-1;i>0;i--){int j=rng.Range(i+1);(b[i],b[j])=(b[j],b[i]);}
        return b;
    }
    private static void FuzzLongStateMachine()
    {
        var std=SquadPressurePolicy.Standard();
        for(int seed=1;seed<=128;seed++)
        {
            var rng=new XorShift(unchecked((uint)seed*0x9e3779b9U));
            int size=1+seed%4;
            PressureMember[] roster=Enumerable.Range(0,size).Select(i=>
                new PressureMember(10_000+seed*10+i,Roles[(i+seed)%4])).ToArray();
            var a=new SquadPressureScheduler(roster,std);
            var b=new SquadPressureScheduler(roster,std);
            var lastStart=new Dictionary<int,long>();
            bool inWindow=false;
            int holder=-1;long until=-1, globalAllowed=0, clock=-1;
            long markUntil=0;
            for(int step=0;step<480;step++)
            {
                // Sparse ticks, changing capability/LOS/health and range; includes edge values.
                clock+=1+rng.Range(4);
                var obs=new PressureObservation[size];
                for(int k=0;k<size;k++)
                {
                    bool alive=rng.Range(19)!=0;
                    bool visible=rng.Range(8)!=0;
                    bool capable=rng.Range(9)!=0;
                    float range=std.RangeFor(roster[k].Archetype);
                    int choice=rng.Range(5);
                    float distance=choice==0?range:choice==1?range+0.25f:
                        choice==2?0f:choice==3?range-0.25f:range*0.5f;
                    obs[k]=new PressureObservation(roster[k].Id,alive,visible,capable,distance);
                }
                var perm=Shuffled(obs,ref rng);
                var outA=a.Tick(clock,obs);
                var outB=b.Tick(clock,perm);
                Match(outA,outB,"seed "+seed+", step "+step+" order-invariance");
                transitions++;
                trace.Append(seed).Append('|').Append(clock).Append('|').Append((int)outA.State)
                     .Append('|').Append(outA.MemberId).Append('|').Append((int)outA.Intent)
                     .Append('|').Append(outA.UntilTick).Append('|')
                     .Append(outA.MarkedByScout?'1':'0').Append('\n');
                var o=obs.FirstOrDefault(x=>x.Id==outA.MemberId);
                if(outA.State==PressureState.Started)
                {
                    starts++;
                    Ensure(!inWindow,"double active reservation @"+seed+"/"+clock);
                    Ensure(clock>=globalAllowed,"global gap ignored @"+seed+"/"+clock);
                    Ensure(outA.MemberId>=0 && o.Alive && o.TargetVisible && o.CanExecute,
                           "invalid living/capable start");
                    int idx=Array.FindIndex(roster,x=>x.Id==outA.MemberId);
                    Ensure(idx>=0 && o.Distance<=std.RangeFor(roster[idx].Archetype),
                           "out-of-range action was granted");
                    Ensure(outA.Intent==Intents[(int)roster[idx].Archetype],
                           "wrong canonical R3 role dispatch");
                    Ensure(outA.UntilTick==clock+std.WindowTicks,"wrong window");
                    if(lastStart.TryGetValue(outA.MemberId,out long prev))
                        Ensure(clock-prev>=std.CooldownFor(roster[idx].Archetype),
                               "member cooldown violated");
                    lastStart[outA.MemberId]=clock;
                    Ensure(outA.MarkedByScout ==
                           (outA.Intent!=PressureIntent.ScoutMark && clock<markUntil),
                           "ScoutMark bit mismatched previous start timing");
                    if(outA.Intent==PressureIntent.ScoutMark)
                        markUntil=clock+std.ScoutMarkTicks;
                    holder=outA.MemberId; until=outA.UntilTick;inWindow=true;
                    globalAllowed=until+std.GlobalGapTicks;
                }
                else if(outA.State==PressureState.Active)
                {
                    actives++;
                    Ensure(inWindow && outA.MemberId==holder && outA.UntilTick==until,
                           "active slot corrupted");
                    Ensure(clock<until && o.Alive && o.TargetVisible && o.CanExecute,
                           "stale/cancelled active slot");
                    int idx=Array.FindIndex(roster,x=>x.Id==holder);
                    Ensure(o.Distance<=std.RangeFor(roster[idx].Archetype),
                           "out-of-range slot not cancelled");
                }
                else if(outA.State==PressureState.Ended)
                {
                    ends++;
                    Ensure(inWindow && outA.MemberId==holder && outA.UntilTick==clock,
                           "bad Ended pulse or lost reservation");
                    int idx=Array.FindIndex(roster,x=>x.Id==holder);
                    Ensure(clock>=until || !o.Alive || !o.TargetVisible || !o.CanExecute ||
                           o.Distance>std.RangeFor(roster[idx].Archetype),
                           "ended valid live slot early");
                    inWindow=false;holder=-1;until=-1;
                }
                else if(outA.State==PressureState.Idle)
                {
                    idles++;
                    Ensure(!inWindow && outA.MemberId==-1 && outA.Intent==PressureIntent.None,
                           "idle while active");
                }
                else throw new InvalidOperationException("unknown pulse state");
                Ensure(a.HasActiveReservation==inWindow && b.HasActiveReservation==inWindow,
                       "reservation flag out of sync");
            }
        }
        Ensure(transitions==128*480,"missing randomized transitions");
        Ensure(starts>2000 && ends>1500 && actives>500 && idles>10000,
               "fuzz did not exercise all FSM branches");
    }
    private static void InvalidInputDoesNotChangeState()
    {
        var members=new[]{
            new PressureMember(5,R3EnemyArchetype.Scout),
            new PressureMember(17,R3EnemyArchetype.Gunner)
        };
        var valid=new[]{
            new PressureObservation(5,true,true,true,8f),
            new PressureObservation(17,true,true,true,18f)
        };
        for(int mode=0;mode<11;mode++)
        {
            var s=new SquadPressureScheduler(members,SquadPressurePolicy.Standard());
            var reference=new SquadPressureScheduler(members,SquadPressurePolicy.Standard());
            Match(s.Tick(0,valid),reference.Tick(0,valid),"initial");
            switch(mode)
            {
                case 0:MustReject(()=>s.Tick(0,valid),"duplicate tick");break;
                case 1:MustReject(()=>s.Tick(-1,valid),"negative tick");break;
                case 2:MustReject(()=>s.Tick(1,null!),"null observations");break;
                case 3:MustReject(()=>s.Tick(1,new[]{valid[0]}),"incomplete");break;
                case 4:MustReject(()=>s.Tick(1,new[]{valid[0],valid[0]}),"duplicate IDs");break;
                case 5:MustReject(()=>s.Tick(1,new[]{valid[0],new PressureObservation(999,true,true,true,18)}),"foreign ID");break;
                case 6:MustReject(()=>s.Tick(1,new[]{new PressureObservation(5,true,true,true,float.NaN),valid[1]}),"NaN");break;
                case 7:MustReject(()=>s.Tick(1,new[]{new PressureObservation(5,true,true,true,float.PositiveInfinity),valid[1]}),"positive infinity");break;
                case 8:MustReject(()=>s.Tick(1,new[]{new PressureObservation(5,true,true,true,-.01f),valid[1]}),"negative distance");break;
                case 9:MustReject(()=>s.Tick(1,new[]{new PressureObservation(5,true,true,true,2000.01f),valid[1]}),"out of contract distance");break;
                case 10:MustReject(()=>s.Tick(long.MaxValue,valid),"overflow tick");break;
            }
            // The next legal tick must produce exactly the state of an untouched scheduler.
            Match(s.Tick(1,valid),reference.Tick(1,valid),"transactional rejection #"+mode);
        }
    }
    private static void RepeatedErrorNeverMutatesIdleOrFairness()
    {
        var members=Roles.Select((role,i)=>new PressureMember(200+i,role)).ToArray();
        var a=new SquadPressureScheduler(members,SquadPressurePolicy.Standard());
        var b=new SquadPressureScheduler(members,SquadPressurePolicy.Standard());
        var valid=Eligible(members);
        for(int t=0;t<250;t++)
        {
            var mutant=(PressureObservation[])valid.Clone();
            int index=t%4;
            mutant[index]=new PressureObservation(members[index].Id,true,true,true,float.NaN);
            MustReject(()=>a.Tick(t,mutant),"repeated NaN tick");
            Match(a.Tick(t,valid),b.Tick(t,valid),"repeat errors were not atomic at "+t);
        }
    }
    private static void SparseTickExpiryAndLongLimit()
    {
        var members=new[]{new PressureMember(22,R3EnemyArchetype.Bruiser)};
        var s=new SquadPressureScheduler(members,SquadPressurePolicy.Standard());
        var obs=Eligible(members);
        Ensure(s.Tick(0,obs).State==PressureState.Started,"expected start");
        Ensure(s.Tick(500,obs).State==PressureState.Ended,"expected Ended before new Start after huge skip");
        Ensure(s.Tick(501,obs).State==PressureState.Started,"expected resume after huge skipped time");
        var high=new SquadPressureScheduler(members,SquadPressurePolicy.Standard());
        var p=high.Tick(long.MaxValue-1000,obs);
        Ensure(p.State==PressureState.Started && p.UntilTick==long.MaxValue-997,"long valid edge");
        MustReject(()=>high.Tick(long.MaxValue-999,obs),"above time overflow guard");
    }
    private static void FairnessWithTransientIneligibleMembers()
    {
        var roster=Roles.Select((role,i)=>new PressureMember(700+i,role)).ToArray();
        var s=new SquadPressureScheduler(roster,SquadPressurePolicy.Standard());
        var records=new Dictionary<int,int>();
        for(int t=0;t<220;t++)
        {
            // Only one out of every 13 ticks blocks Bruiser; all others remain eligible.
            var o=Eligible(roster);
            if(t%13==0)o[1]=new PressureObservation(701,true,true,false,6);
            var pulse=s.Tick(t,o);
            if(pulse.State==PressureState.Started)
                records[pulse.MemberId]=records.GetValueOrDefault(pulse.MemberId)+1;
        }
        Ensure(records.Count==4,"one canonical role was starved by transient disable");
        Ensure(records.Values.Min()>=5,"insufficient per-role pressure events");
    }
    private static void IndependentSquadIsolation()
    {
        var roster=Roles.Select((role,i)=>new PressureMember(100+i,role)).ToArray();
        var obs=Eligible(roster);
        var a=new SquadPressureScheduler(roster,SquadPressurePolicy.Standard());
        var b=new SquadPressureScheduler(roster,SquadPressurePolicy.Standard());
        var control=new SquadPressureScheduler(roster,SquadPressurePolicy.Standard());
        for(int t=0;t<120;t++)
        {
            // Independent A receives intermittent full suppression; B and control must be identical.
            var onlyA=(PressureObservation[])obs.Clone();
            if(t%6==1)for(int i=0;i<onlyA.Length;i++)
                onlyA[i]=new PressureObservation(roster[i].Id,true,false,false,6);
            a.Tick(t,onlyA);
            Match(b.Tick(t,obs),control.Tick(t,obs),"cross-squad state leak "+t);
        }
    }
    public static int Main()
    {
        Test("long randomized 128x480 original CSharp scheduler transitions and observation permutations",FuzzLongStateMachine);
        Test("eleven malformed tick/id/observation inputs rejected without any state mutation",InvalidInputDoesNotChangeState);
        Test("250 repeated hostile failed ticks do not affect fairness or state progression",RepeatedErrorNeverMutatesIdleOrFairness);
        Test("sparse time jumps and 64-bit tick guard",SparseTickExpiryAndLongLimit);
        Test("four-role fairness under transient target capability loss",FairnessWithTransientIneligibleMembers);
        Test("cross-squad isolation under one-squad adversarial LOS drop",IndependentSquadIsolation);
        string hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trace.ToString()))).ToLowerInvariant();
        Console.WriteLine("B01_60_W07_INDEPENDENT_SOURCE_QA "+tests+" PASS "+failed+" FAIL");
        Console.WriteLine("B01_60_W07_FUZZ_TRACE_SHA256 "+hash);
        Console.WriteLine("B01_60_W07_FUZZ_EVENTS "+transitions+" STARTED "+starts+
                          " ACTIVE "+actives+" ENDED "+ends+" IDLE "+idles);
        Console.WriteLine("B01_60_W07_GATES native_Unity=NOT_RUN PhysX=NOT_RUN Codex_ACK=PENDING");
        return failed==0?0:1;
    }
}

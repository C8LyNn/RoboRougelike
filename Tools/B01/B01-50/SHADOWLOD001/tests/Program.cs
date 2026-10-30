// Original real .NET test harness: executes exact production C# core, no fake shadow API.
// Native Unity/URP renderer and actual shadow draw-call cost are intentionally NOT_RUN.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using B01.Performance.ShadowBudget;

internal static class Program
{
    private sealed class Toy
    {
        public readonly string Label;
        public double Distance;
        public Toy(string label, double distance) { Label = label; Distance = distance; }
    }
    private static int pass, fail;
    private static readonly List<string> errors = new List<string>();
    private static void Yes(bool condition, string why) { if (!condition) throw new InvalidOperationException(why); }
    private static void Throws<E>(Action action) where E : Exception
    {
        try { action(); } catch (E) { return; }
        throw new Exception("Expected " + typeof(E).Name);
    }
    private static void Case(string name, Action test)
    {
        try { test(); pass++; Console.WriteLine("PASS " + name); }
        catch(Exception ex) { fail++; errors.Add(name + ": " + ex.Message); Console.Error.WriteLine("FAIL " + name + ": " + ex); }
    }
    private static ShadowDistanceBudget<Toy> New(int slots = 10, int budget = 3, double near = 20, double far = 30)
        => new ShadowDistanceBudget<Toy>(slots, budget, near, far);
    private static int Tick(ShadowDistanceBudget<Toy> b, List<string> changed)
        => b.EvaluateFrame(t=>t.Distance, (t,enabled)=>changed.Add(t.Label + ":" + enabled));
    public static int Main(string[] args)
    {
        Case("constructor-reject-zero-capacity",()=>Throws<ArgumentOutOfRangeException>(()=>New(slots:0)));
        Case("constructor-reject-excess-capacity",()=>Throws<ArgumentOutOfRangeException>(()=>New(slots:4097)));
        Case("constructor-reject-zero-frame-budget",()=>Throws<ArgumentOutOfRangeException>(()=>New(budget:0)));
        Case("constructor-reject-too-large-frame-budget",()=>Throws<ArgumentOutOfRangeException>(()=>New(budget:129)));
        Case("constructor-reject-negative-near",()=>Throws<ArgumentOutOfRangeException>(()=>New(near:-1)));
        Case("constructor-reject-nan-near",()=>Throws<ArgumentOutOfRangeException>(()=>New(near:double.NaN)));
        Case("constructor-reject-infinite-near",()=>Throws<ArgumentOutOfRangeException>(()=>New(near:double.PositiveInfinity)));
        Case("constructor-reject-too-large-near",()=>Throws<ArgumentOutOfRangeException>(()=>New(near:10001,far:10002)));
        Case("constructor-reject-no-hysteresis",()=>Throws<ArgumentOutOfRangeException>(()=>New(near:20,far:20)));
        Case("constructor-reject-reversed-hysteresis",()=>Throws<ArgumentOutOfRangeException>(()=>New(near:35,far:30)));
        Case("constructor-reject-nan-far",()=>Throws<ArgumentOutOfRangeException>(()=>New(far:double.NaN)));
        Case("constructor-reject-infinite-far",()=>Throws<ArgumentOutOfRangeException>(()=>New(far:double.PositiveInfinity)));
        Case("constructor-reject-too-large-far",()=>Throws<ArgumentOutOfRangeException>(()=>New(far:10001)));
        Case("reject-null-registration",()=>{var b=New();Throws<ArgumentNullException>(()=>b.Register(null!));Yes(b.Count==0,"mutated");});
        Case("reject-duplicate-by-reference",()=>{var b=New();var x=new Toy("x",3);b.Register(x);Throws<InvalidOperationException>(()=>b.Register(x));Yes(b.Count==1,"duplicate admitted");});
        Case("allow-distinct-items-with-same-value",()=>{var b=New();b.Register(new Toy("x",3));b.Register(new Toy("x",3));Yes(b.Count==2,"value equality used");});
        Case("reject-overflow-register",()=>{var b=New(slots:1);b.Register(new Toy("a",3));Throws<InvalidOperationException>(()=>b.Register(new Toy("b",3)));});
        Case("empty-frame-no-op",()=>{var b=New();var list=new List<string>();Yes(Tick(b,list)==0&&b.Checks==0,"empty changed");});
        Case("reject-null-distance-provider",()=>{var b=New();b.Register(new Toy("x",2));Throws<ArgumentNullException>(()=>b.EvaluateFrame(null!,(_,_)=>{}));});
        Case("reject-null-state-callback",()=>{var b=New();b.Register(new Toy("x",2));Throws<ArgumentNullException>(()=>b.EvaluateFrame(t=>t.Distance,null!));});
        Case("switch-off-only-past-far",()=>{var b=New();var x=new Toy("x",30);b.Register(x);var list=new List<string>();Tick(b,list);Yes(b.IsCasting(0)&&list.Count==0,"far equality switched off");x.Distance=30.1;Tick(b,list);Yes(!b.IsCasting(0)&&list.Count==1&&list[0]=="x:False","beyond far not off");});
        Case("switch-on-only-strictly-within-near",()=>{var b=New();var x=new Toy("x",31);b.Register(x);var list=new List<string>();Tick(b,list);x.Distance=20;Tick(b,list);Yes(!b.IsCasting(0),"near equality reenabled");x.Distance=19.9;Tick(b,list);Yes(b.IsCasting(0)&&b.Reenabled==1,"inside near not on");});
        Case("hysteresis-prevents-thrashing",()=>{var b=New();var x=new Toy("x",40);b.Register(x);var list=new List<string>();Tick(b,list);foreach(double d in new[]{21d,29d,20d,25d,30d}) { x.Distance=d;Tick(b,list); }Yes(list.Count==1&&b.Disabled==1,"boundary thrash");});
        Case("no-callback-on-stable-state",()=>{var b=New();var x=new Toy("x",2);b.Register(x);var list=new List<string>();for(int i=0;i<10;i++)Tick(b,list);Yes(list.Count==0&&b.Checks==10,"repeated callback");});
        Case("round-robin-budget-three",()=>{var b=New(slots:10,budget:3);for(int i=0;i<10;i++)b.Register(new Toy(i.ToString(),50));var list=new List<string>();Yes(Tick(b,list)==3&&list.Count==3,"frame budget exceeded");Yes(Tick(b,list)==3&&list.Count==6,"second frame");Yes(Tick(b,list)==3&&list.Count==9,"third frame");Yes(Tick(b,list)==3&&list.Count==10&&b.Checks==12,"wrap");});
        Case("budget-greater-than-count-check-once",()=>{var b=New(slots:4,budget:128);b.Register(new Toy("a",50));b.Register(new Toy("b",50));var list=new List<string>();Yes(Tick(b,list)==2&&b.Checks==2&&list.Count==2,"checked duplicates");});
        Case("reject-nan-distance-without-state-advance",()=>{var b=New();var x=new Toy("x",double.NaN);b.Register(x);var list=new List<string>();Throws<ArgumentOutOfRangeException>(()=>Tick(b,list));Yes(b.Checks==0&&b.IsCasting(0)&&list.Count==0,"invalid advanced");x.Distance=50;Yes(Tick(b,list)==1&&list.Count==1,"retry failed");});
        Case("reject-negative-distance",()=>{var b=New();b.Register(new Toy("x",-1));Throws<ArgumentOutOfRangeException>(()=>Tick(b,new List<string>()));});
        Case("reject-infinite-distance",()=>{var b=New();b.Register(new Toy("x",double.PositiveInfinity));Throws<ArgumentOutOfRangeException>(()=>Tick(b,new List<string>()));});
        Case("failed-transition-callback-retriable",()=>{var b=New();var x=new Toy("x",40);b.Register(x);Throws<InvalidOperationException>(()=>b.EvaluateFrame(t=>t.Distance,(_,_)=>throw new InvalidOperationException("fail")));Yes(b.IsCasting(0)&&b.Checks==0,"state falsely committed");var list=new List<string>();Tick(b,list);Yes(!b.IsCasting(0)&&list.Count==1,"retry lost");});
        Case("restore-off-state-only",()=>{var b=New();b.Register(new Toy("x",40));b.Register(new Toy("y",2));Tick(b,new List<string>());var list=new List<string>();b.RestoreAll((t,enabled)=>list.Add(t.Label+":"+enabled));Yes(list.Count==1&&list[0]=="x:True"&&b.IsCasting(0)&&b.IsCasting(1)&&b.Restored==1,"restore");});
        Case("restore-no-change-is-idempotent",()=>{var b=New();var x=new Toy("x",40);b.Register(x);Tick(b,new List<string>());int calls=0;b.RestoreAll((_,_)=>calls++);b.RestoreAll((_,_)=>calls++);Yes(calls==1&&b.Restored==1,"duplicate restore");});
        Case("restore-null-callback-rejected",()=>{var b=New();Throws<ArgumentNullException>(()=>b.RestoreAll(null!));});
        Case("restore-throw-keeps-flag-for-retry",()=>{var b=New();b.Register(new Toy("x",40));Tick(b,new List<string>());Throws<InvalidOperationException>(()=>b.RestoreAll((_,_)=>throw new InvalidOperationException("fail")));Yes(!b.IsCasting(0),"false restored state");int calls=0;b.RestoreAll((_,_)=>calls++);Yes(calls==1&&b.IsCasting(0),"not retryable");});
        Case("invalid-iscasting-indices",()=>{var b=New();b.Register(new Toy("x",2));Throws<ArgumentOutOfRangeException>(()=>b.IsCasting(-1));Throws<ArgumentOutOfRangeException>(()=>b.IsCasting(1));});
        Case("stress-1024-casters-16-checks-per-frame",()=>{
            var b=New(slots:1024,budget:16,near:25,far:35);
            for(int i=0;i<1024;i++)b.Register(new Toy(i.ToString(),70));
            int toggles=0,maxChecks=0;for(int frame=0;frame<64;frame++){
                int checks=b.EvaluateFrame(t=>t.Distance,(_,enabled)=>{if(!enabled)toggles++;});
                maxChecks=Math.Max(maxChecks,checks);Yes(checks<=16,"check spike");
            }
            Yes(toggles==1024&&b.Disabled==1024&&b.Checks==1024&&maxChecks==16,"stress failed");
        });
        Case("stress-transition-back-1024-casters",()=>{
            var b=New(slots:1024,budget:32);var data=new Toy[1024];
            for(int i=0;i<1024;i++){data[i]=new Toy(i.ToString(),90);b.Register(data[i]);}
            for(int frame=0;frame<32;frame++)b.EvaluateFrame(t=>t.Distance,(_,_)=>{});
            foreach(var d in data)d.Distance=10;
            int enabled=0;for(int frame=0;frame<32;frame++)b.EvaluateFrame(t=>t.Distance,(_,cast)=>{if(cast)enabled++;});
            Yes(enabled==1024&&b.Reenabled==1024,"not all restored");
        });
        Case("two-phase-camera-segmentation-stable",()=>{
            var b=New(slots:2,budget:1);var a=new Toy("a",31);var c=new Toy("c",31);b.Register(a);b.Register(c);
            var list=new List<string>();Tick(b,list);Tick(b,list);a.Distance=24;c.Distance=24;
            Tick(b,list);Tick(b,list);Yes(list.Count==2&&b.Disabled==2,"hysteresis crossed too early");
            a.Distance=19;c.Distance=19;Tick(b,list);Tick(b,list);Yes(list.Count==4&&b.Reenabled==2,"re-enable");
        });
        int total=pass+fail;
        var receipt=new {
            package="B01-50-SHADOWLOD-001/R1", classification="ACTUAL_DOTNET8_CSHARP_SHADOW_POLICY_TEST_NOT_UNITY_GPU_FPS",
            passed=pass,failed=fail,total,errors=errors.ToArray(),
            stress="1024 decorative shadow casters evaluated in 64 simulated frames, max 16 checks/frame",
            nativeUnity="NOT_RUN",actualGpuDrawCalls="NOT_MEASURED",actualFps="NOT_MEASURED"
        };
        if(args.Length==2&&args[0]=="--receipt"){
            if(File.Exists(args[1])){Console.Error.WriteLine("REFUSE overwrite original receipt");return 3;}
            File.WriteAllText(args[1],JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true})+"\n");
        }
        Console.WriteLine("RESULT "+pass+"/"+total+" PASS; "+fail+" FAIL");
        return fail==0?0:1;
    }
}

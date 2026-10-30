// B01-50 W10 original 40-case executable C# acceptance.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using B01.Performance.ParticleEmission;

internal static class Program
{
    private sealed class Emitter { public readonly string Name; public double Distance; public Emitter(string name, double distance) { Name=name; Distance=distance; } }
    private static int pass, fail;
    private static readonly List<string> errors = new List<string>();
    private static void Must(bool yes, string msg) { if (!yes) throw new InvalidOperationException(msg); }
    private static void Throws<T>(Action a) where T:Exception { try { a(); } catch(T) { return; } throw new InvalidOperationException("Expected "+typeof(T).Name); }
    private static void Case(string label,Action test) { try { test(); pass++; Console.WriteLine("PASS "+label); } catch(Exception e) { fail++; errors.Add(label+": "+e.Message); Console.Error.WriteLine("FAIL "+label+": "+e); } }
    private static OptionalParticleEmissionBudget<Emitter> New(int capacity=4,int work=3,double near=20,double far=40,double hyst=2,double medium=.35,double distant=0)
      => new OptionalParticleEmissionBudget<Emitter>(capacity,work,near,far,hyst,medium,distant);
    private static int Tick(OptionalParticleEmissionBudget<Emitter> b,List<string> applied)
      => b.EvaluateFrame(e=>e.Distance,(e,value)=>applied.Add(e.Name+":"+value.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)));
    private static void CheckOne(string expected, double distance, double original=4)
    {
        var b=New();var item=new Emitter("dust",distance);b.Register(item,original);
        var got=new List<string>();Tick(b,got);
        Must(got.Count==1&&got[0]==expected,"expected "+expected+" got "+String.Join(",",got));
    }
    public static int Main(string[] args)
    {
        Case("reject-zero-capacity",()=>Throws<ArgumentOutOfRangeException>(()=>New(capacity:0)));
        Case("reject-too-large-capacity",()=>Throws<ArgumentOutOfRangeException>(()=>New(capacity:4097)));
        Case("reject-zero-checks",()=>Throws<ArgumentOutOfRangeException>(()=>New(work:0)));
        Case("reject-excess-checks",()=>Throws<ArgumentOutOfRangeException>(()=>New(work:129)));
        Case("reject-zero-near",()=>Throws<ArgumentOutOfRangeException>(()=>New(near:0)));
        Case("reject-nan-near",()=>Throws<ArgumentOutOfRangeException>(()=>New(near:double.NaN)));
        Case("reject-infinite-near",()=>Throws<ArgumentOutOfRangeException>(()=>New(near:double.PositiveInfinity)));
        Case("reject-too-far-near",()=>Throws<ArgumentOutOfRangeException>(()=>New(near:10001,far:10002)));
        Case("reject-far-at-near",()=>Throws<ArgumentOutOfRangeException>(()=>New(far:20)));
        Case("reject-far-infinite",()=>Throws<ArgumentOutOfRangeException>(()=>New(far:double.PositiveInfinity)));
        Case("reject-hysteresis-nan",()=>Throws<ArgumentOutOfRangeException>(()=>New(hyst:double.NaN)));
        Case("reject-negative-hysteresis",()=>Throws<ArgumentOutOfRangeException>(()=>New(hyst:-1)));
        Case("reject-hysteresis-larger-than-near",()=>Throws<ArgumentOutOfRangeException>(()=>New(hyst:20)));
        Case("reject-overlapping-hysteresis",()=>Throws<ArgumentOutOfRangeException>(()=>New(near:20,far:23,hyst:2)));
        Case("reject-medium-scale-above-one",()=>Throws<ArgumentOutOfRangeException>(()=>New(medium:1.1)));
        Case("reject-medium-nan",()=>Throws<ArgumentOutOfRangeException>(()=>New(medium:double.NaN)));
        Case("reject-far-scale-above-medium",()=>Throws<ArgumentOutOfRangeException>(()=>New(distant:.5)));
        Case("reject-negative-far-scale",()=>Throws<ArgumentOutOfRangeException>(()=>New(distant:-.01)));
        Case("register-reject-null",()=>{var b=New();Throws<ArgumentNullException>(()=>b.Register(null!,2));Must(b.Count==0,"mutated");});
        Case("register-reject-invalid-baseline",()=>{var b=New();foreach(var v in new[]{-1d,10001d,double.NaN,double.PositiveInfinity})Throws<ArgumentOutOfRangeException>(()=>b.Register(new Emitter("x",2),v));Must(b.Count==0,"mutated");});
        Case("register-reject-duplicate-object",()=>{var b=New();var e=new Emitter("x",2);b.Register(e,1);Throws<InvalidOperationException>(()=>b.Register(e,2));});
        Case("register-allow-distinct-same-label",()=>{var b=New();b.Register(new Emitter("x",2),1);b.Register(new Emitter("x",2),2);Must(b.Count==2,"wrong identity");});
        Case("register-reject-capacity-overflow",()=>{var b=New(capacity:1);b.Register(new Emitter("x",2),1);Throws<InvalidOperationException>(()=>b.Register(new Emitter("y",3),1));});
        Case("empty-frame-no-callbacks",()=>{var b=New();var got=new List<string>();Must(Tick(b,got)==0&&got.Count==0&&b.Checks==0,"not empty");});
        Case("null-distance-rejected",()=>{var b=New();b.Register(new Emitter("x",10),1);Throws<ArgumentNullException>(()=>b.EvaluateFrame(null!,(_,_)=>{}));});
        Case("null-emission-callback-rejected",()=>{var b=New();b.Register(new Emitter("x",10),1);Throws<ArgumentNullException>(()=>b.EvaluateFrame(x=>x.Distance,null!));});
        Case("invalid-distance-nan-retry",()=>{var b=New();var e=new Emitter("x",double.NaN);b.Register(e,3);Throws<ArgumentOutOfRangeException>(()=>Tick(b,new List<string>()));Must(b.Checks==0&&b.BandAt(0)==EmissionBand.Near,"changed state");e.Distance=50;Must(Tick(b,new List<string>())==1,"retry lost");});
        Case("invalid-negative-infinite-distance",()=>{var b=New();var e=new Emitter("x",-1);b.Register(e,3);Throws<ArgumentOutOfRangeException>(()=>Tick(b,new List<string>()));e.Distance=double.PositiveInfinity;Throws<ArgumentOutOfRangeException>(()=>Tick(b,new List<string>()));});
        Case("near-emission-does-not-call-setter",()=>{var b=New();b.Register(new Emitter("x",1),3);var list=new List<string>();for(int i=0;i<5;i++)Tick(b,list);Must(list.Count==0&&b.Checks==5,"spurious setter");});
        Case("medium-scale-real-value",()=>{CheckOne("dust:1.400",23);});
        Case("far-scale-real-value",()=>{CheckOne("dust:0.000",45);});
        Case("exact-near-outer-boundary-stable",()=>{var b=New();var e=new Emitter("x",22);b.Register(e,4);var list=new List<string>();Tick(b,list);Must(b.BandAt(0)==EmissionBand.Near&&list.Count==0,"near boundary shifted");});
        Case("exact-far-outer-boundary-stable",()=>{var b=New();var e=new Emitter("x",42);b.Register(e,4);Tick(b,new List<string>());Must(b.BandAt(0)==EmissionBand.Medium,"far boundary shifted");});
        Case("medium-to-near-hysteresis",()=>{var b=New();var e=new Emitter("x",25);b.Register(e,4);var list=new List<string>();Tick(b,list);e.Distance=18;Tick(b,list);Must(b.BandAt(0)==EmissionBand.Medium,"near boundary");e.Distance=17.9;Tick(b,list);Must(b.BandAt(0)==EmissionBand.Near&&b.AppliedAt(0)==4,"not restored");});
        Case("far-to-medium-hysteresis",()=>{var b=New();var e=new Emitter("x",48);b.Register(e,4);var list=new List<string>();Tick(b,list);e.Distance=38;Tick(b,list);Must(b.BandAt(0)==EmissionBand.Far,"far boundary");e.Distance=37.9;Tick(b,list);Must(b.BandAt(0)==EmissionBand.Medium&&b.AppliedAt(0)==1.4,"not medium");});
        Case("far-to-near-direct-jump",()=>{var b=New();var e=new Emitter("x",90);b.Register(e,4);Tick(b,new List<string>());e.Distance=1;Tick(b,new List<string>());Must(b.BandAt(0)==EmissionBand.Near&&b.AppliedAt(0)==4,"not direct near");});
        Case("callback-failure-retriable",()=>{var b=New();var e=new Emitter("x",90);b.Register(e,4);Throws<InvalidOperationException>(()=>b.EvaluateFrame(x=>x.Distance,(_,_)=>throw new InvalidOperationException("setter")));Must(b.Checks==0&&b.AppliedAt(0)==4,"fault committed");Tick(b,new List<string>());Must(b.AppliedAt(0)==0,"retry lost");});
        Case("restore-exact-original-and-idempotent",()=>{var b=New();b.Register(new Emitter("x",90),7);Tick(b,new List<string>());var list=new List<string>();b.RestoreAll((e,v)=>list.Add(e.Name+":"+v));b.RestoreAll((e,v)=>list.Add(e.Name+":"+v));Must(list.Count==1&&list[0]=="x:7"&&b.AppliedAt(0)==7&&b.Restorations==1,"bad restore");});
        Case("restore-error-retriable",()=>{var b=New();b.Register(new Emitter("x",90),3);Tick(b,new List<string>());Throws<InvalidOperationException>(()=>b.RestoreAll((_,_)=>throw new InvalidOperationException()));Must(b.AppliedAt(0)==0,"false restore");b.RestoreAll((_,_)=>{});Must(b.AppliedAt(0)==3&&b.Restorations==1,"retry broken");});
        Case("stress-1024-emitters-16-evaluations-per-frame",()=>{
            var b=New(capacity:1024,work:16);for(int i=0;i<1024;i++)b.Register(new Emitter(i.ToString(),100),1);
            int changed=0;for(int f=0;f<64;f++){int n=b.EvaluateFrame(x=>x.Distance,(_,v)=>{if(v==0)changed++;});Must(n==16,"frame exceeded");}
            Must(changed==1024&&b.Checks==1024&&b.Applications==1024,"incomplete budget");});
        Case("stress-return-1024-to-full-rate",()=>{
            var b=New(capacity:1024,work:32);var all=new Emitter[1024];
            for(int i=0;i<all.Length;i++){all[i]=new Emitter(i.ToString(),100);b.Register(all[i],2);}
            for(int f=0;f<32;f++)b.EvaluateFrame(x=>x.Distance,(_,_)=>{});
            foreach(var e in all)e.Distance=1;
            int restored=0;for(int f=0;f<32;f++)b.EvaluateFrame(x=>x.Distance,(_,v)=>{if(v==2)restored++;});
            Must(restored==1024&&b.Transitions==2048&&b.Applications==2048,"reverse transition incomplete");});
        int total=pass+fail;
        var receipt=new {
            package="B01-50-PARTICLELOD-001/R1",classification="REAL_CSHARP_PARTICLE_EMISSION_POLICY_NOT_UNITY_RENDERING",
            passed=pass,failed=fail,total,errors=errors.ToArray(),
            stress="1024 opted-in decorative emitter slots, max 16 inspections/frame, 64 synthetic frames",
            nativeUnity="NOT_RUN",actualParticleCount="NOT_MEASURED",actualFPS="NOT_MEASURED"
        };
        if(args.Length==2&&args[0]=="--receipt"){
            if(File.Exists(args[1])){Console.Error.WriteLine("REFUSE_OVERWRITE");return 3;}
            File.WriteAllText(args[1],JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true})+"\n");
        }
        Console.WriteLine("RESULT "+pass+"/"+total+" PASS; "+fail+" FAIL");
        return fail==0?0:1;
    }
}

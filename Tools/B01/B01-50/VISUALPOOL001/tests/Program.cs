using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using B01.Performance.VisualPool;

internal static class Program
{
    private sealed class Visual
    {
        public string Name;
        public bool Shown;
        public Visual(string name) { Name = name; }
    }
    private static int pass, fail;
    private static readonly List<string> errors = new List<string>();

    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch(T) { return; }
        throw new Exception("Missing exception " + typeof(T).Name);
    }
    private static void Case(string title, Action test)
    {
        try { test(); pass++; Console.WriteLine("PASS " + title); }
        catch(Exception ex) { fail++; errors.Add(title + ": " + ex.Message); Console.Error.WriteLine("FAIL " + title + ": " + ex); }
    }
    private static OptionalVisualLeasePool<Visual> Pool(int capacity = 3) => new OptionalVisualLeasePool<Visual>(capacity);
    private static void Hide(Visual visual) { visual.Shown = false; }
    private static bool Acquire(OptionalVisualLeasePool<Visual> pool, double now, out VisualLease lease, double life = 100)
        => pool.TryAcquire(now, life, out lease);
    private static Visual[] Warm(OptionalVisualLeasePool<Visual> pool, int count)
    {
        var array = new Visual[count];
        for (int i = 0; i < count; i++)
        {
            array[i] = new Visual("v" + i);
            Require(pool.Register(array[i]) == i, "unexpected slot registration");
        }
        return array;
    }

    public static int Main(string[] args)
    {
        Case("reject-capacity-zero", () => Throws<ArgumentOutOfRangeException>(() => Pool(0)));
        Case("reject-capacity-257", () => Throws<ArgumentOutOfRangeException>(() => Pool(257)));
        Case("allow-max-256-capacity", () => Require(Pool(256).Capacity == 256, "max"));
        Case("reject-null-registration", () => { var p=Pool(); Throws<ArgumentNullException>(()=>p.Register(null!)); Require(p.Registered==0,"mutated"); });
        Case("reject-duplicate-reference", () => { var p=Pool(); var a=new Visual("a"); p.Register(a); Throws<InvalidOperationException>(()=>p.Register(a)); Require(p.Registered==1,"duplicate"); });
        Case("allow-distinct-instances-with-same-name", () => { var p=Pool(); Warm(p,2); Require(p.Registered==2,"count"); });
        Case("reject-registration-over-capacity", () => { var p=Pool(1); Warm(p,1); Throws<InvalidOperationException>(()=>p.Register(new Visual("extra"))); });
        Case("empty-pool-denies-acquire", () => { var p=Pool(); Require(!Acquire(p,0,out VisualLease _),"unregistered acquired"); Require(p.RejectedCapacity==1,"counter"); });
        Case("reject-lifetime-zero", () => { var p=Pool(); Warm(p,1); Throws<ArgumentOutOfRangeException>(()=>Acquire(p,0,out VisualLease _,0)); });
        Case("reject-lifetime-negative", () => { var p=Pool(); Warm(p,1); Throws<ArgumentOutOfRangeException>(()=>Acquire(p,0,out VisualLease _,-1)); });
        Case("reject-lifetime-nan", () => { var p=Pool(); Warm(p,1); Throws<ArgumentOutOfRangeException>(()=>Acquire(p,0,out VisualLease _,double.NaN)); });
        Case("reject-lifetime-infinite", () => { var p=Pool(); Warm(p,1); Throws<ArgumentOutOfRangeException>(()=>Acquire(p,0,out VisualLease _,double.PositiveInfinity)); });
        Case("reject-lifetime-above-minute", () => { var p=Pool(); Warm(p,1); Throws<ArgumentOutOfRangeException>(()=>Acquire(p,0,out VisualLease _,60001)); });
        Case("allow-lifetime-exact-minute", () => { var p=Pool(); Warm(p,1); Require(Acquire(p,0,out VisualLease _,60000),"one minute"); });
        Case("reject-negative-clock", () => { var p=Pool(); Warm(p,1); Throws<ArgumentOutOfRangeException>(()=>Acquire(p,-1,out VisualLease _)); });
        Case("reject-nan-clock", () => { var p=Pool(); Warm(p,1); Throws<ArgumentOutOfRangeException>(()=>Acquire(p,double.NaN,out VisualLease _)); });
        Case("reject-infinite-clock", () => { var p=Pool(); Warm(p,1); Throws<ArgumentOutOfRangeException>(()=>Acquire(p,double.PositiveInfinity,out VisualLease _)); });
        Case("reject-clock-regression", () => { var p=Pool(); Warm(p,2); Acquire(p,20,out VisualLease _); Throws<ArgumentOutOfRangeException>(()=>Acquire(p,19,out VisualLease _)); Require(p.ActiveCount==1,"regression mutated"); });
        Case("first-acquire-is-slot-zero", () => { var p=Pool(); Warm(p,2); Require(Acquire(p,0,out VisualLease l) && l.Slot==0 && l.Token==1 && l.IsValid,"first"); });
        Case("second-acquire-uses-distinct-object", () => { var p=Pool(); var a=Warm(p,2); Acquire(p,0,out VisualLease x); Acquire(p,0,out VisualLease y); Require(x.Slot!=y.Slot && ReferenceEquals(p.Get(x),a[0]) && ReferenceEquals(p.Get(y),a[1]),"overlap"); });
        Case("capacity-never-steals-live-visual", () => { var p=Pool(1); Warm(p,1); Acquire(p,0,out VisualLease a); Require(!Acquire(p,1,out VisualLease rejected),"steal"); Require(!rejected.IsValid && p.Owns(a) && p.ActiveCount==1 && p.RejectedCapacity==1,"wrong cap"); });
        Case("lease-get-valid", () => { var p=Pool(); var v=Warm(p,1); Acquire(p,0,out VisualLease lease); Require(ReferenceEquals(p.Get(lease),v[0]),"wrong item"); });
        Case("lease-get-invalid", () => { var p=Pool(); Warm(p,1); Throws<InvalidOperationException>(()=>p.Get(default)); });
        Case("lease-owns-invalid-false", () => { var p=Pool(); Warm(p,1); Require(!p.Owns(default) && !p.Owns(new VisualLease(-1,1)),"invalid owns"); });
        Case("release-free-slot-noop", () => { var p=Pool(); Warm(p,1); Require(!p.TryRelease(new VisualLease(0,99),Hide),"released unused"); });
        Case("release-null-callback-always-rejected", () => { var p=Pool(); Warm(p,1); Throws<ArgumentNullException>(()=>p.TryRelease(default,null!)); });
        Case("release-hides-before-free", () => { var p=Pool(); var v=Warm(p,1); Acquire(p,0,out VisualLease a); v[0].Shown=true; Require(p.TryRelease(a,Hide) && !v[0].Shown && p.ActiveCount==0 && p.Released==1,"bad release"); });
        Case("release-twice-noop", () => { var p=Pool(); Warm(p,1); Acquire(p,0,out VisualLease a); Require(p.TryRelease(a,Hide) && !p.TryRelease(a,Hide),"double"); });
        Case("old-lease-cannot-release-new-owner", () => { var p=Pool(1); Warm(p,1); Acquire(p,0,out VisualLease old); p.TryRelease(old,Hide); Acquire(p,1,out VisualLease newer); Require(!p.TryRelease(old,Hide) && newer.Token!=old.Token && p.Owns(newer),"stale kill"); });
        Case("release-callback-throw-keeps-lease", () => { var p=Pool(); Warm(p,1); Acquire(p,0,out VisualLease a); Throws<InvalidOperationException>(()=>p.TryRelease(a,_=>throw new InvalidOperationException("fail"))); Require(p.Owns(a)&&p.ActiveCount==1&&p.FailedRetirementCallbacks==1,"lost retry"); p.TryRelease(a,Hide); Require(p.ActiveCount==0,"retry failed"); });
        Case("retire-before-deadline-not-expired", () => { var p=Pool(); Warm(p,1); Acquire(p,0,out VisualLease a,100); Require(p.RetireExpired(99,Hide)==0 && p.Owns(a),"premature expiry"); });
        Case("retire-exact-deadline", () => { var p=Pool(); Warm(p,1); Acquire(p,0,out VisualLease a,100); Require(p.RetireExpired(100,Hide)==1 && !p.Owns(a) && p.Expired==1,"boundary"); });
        Case("retire-only-expired-slots", () => { var p=Pool(); Warm(p,2); Acquire(p,0,out VisualLease a,10); Acquire(p,0,out VisualLease b,100); Require(p.RetireExpired(10,Hide)==1 && !p.Owns(a) && p.Owns(b) && p.ActiveCount==1,"partial"); });
        Case("retire-callback-throw-keeps-slot", () => { var p=Pool(); Warm(p,1); Acquire(p,0,out VisualLease a,10); Throws<InvalidOperationException>(()=>p.RetireExpired(10,_=>throw new InvalidOperationException("cannot hide"))); Require(p.Owns(a) && p.FailedRetirementCallbacks==1,"unsafe reuse"); Require(p.RetireExpired(10,Hide)==1,"retry"); });
        Case("retire-null-callback-rejected-before-clock", () => { var p=Pool(); Warm(p,1); Acquire(p,10,out VisualLease _); Throws<ArgumentNullException>(()=>p.RetireExpired(20,null!)); Require(p.RetireExpired(11,Hide)==0,"clock moved"); });
        Case("retire-time-regression-rejected", () => { var p=Pool(); Warm(p,1); Acquire(p,10,out VisualLease a); Throws<ArgumentOutOfRangeException>(()=>p.RetireExpired(9,Hide)); Require(p.Owns(a),"mutated"); });
        Case("retire-nan-time-rejected", () => { var p=Pool(); Warm(p,1); Throws<ArgumentOutOfRangeException>(()=>p.RetireExpired(double.NaN,Hide)); });
        Case("retire-finite-positive-infinity-rejected", () => { var p=Pool(); Warm(p,1); Throws<ArgumentOutOfRangeException>(()=>p.RetireExpired(double.PositiveInfinity,Hide)); });
        Case("return-to-pool-after-expiry", () => { var p=Pool(1); Warm(p,1); Acquire(p,0,out VisualLease a,5); Require(!Acquire(p,5,out VisualLease _),"implicit dangerous recycle"); p.RetireExpired(5,Hide); Require(Acquire(p,5,out VisualLease b)&&b.Token!=a.Token,"not reusable"); });
        Case("roundrobin-slot-reuse", () => { var p=Pool(3); Warm(p,3); Acquire(p,0,out VisualLease a); p.TryRelease(a,Hide); Acquire(p,0,out VisualLease b); Require(b.Slot==1,"not rotating"); });
        Case("releaseall-null-rejected", () => { var p=Pool(); Warm(p,1); Throws<ArgumentNullException>(()=>p.ReleaseAll(null!)); });
        Case("releaseall-idempotent", () => { var p=Pool(); Warm(p,3); for(int i=0;i<3;i++)Acquire(p,0,out VisualLease _); Require(p.ReleaseAll(Hide)==3 && p.ReleaseAll(Hide)==0 && p.Cleared==3 && p.ActiveCount==0,"bad clear"); });
        Case("releaseall-partial-failure-retry", () => { var p=Pool(); Warm(p,2); Acquire(p,0,out VisualLease a); Acquire(p,0,out VisualLease b); int n=0; Throws<InvalidOperationException>(()=>p.ReleaseAll(_=>{ if(++n==2)throw new InvalidOperationException(); })); Require(p.ActiveCount==1 && !p.Owns(a) && p.Owns(b),"wrong partial state"); Require(p.ReleaseAll(Hide)==1,"remaining not reclaimed"); });
        Case("stress-256-visuals-no-overallocation", () => { var p=Pool(256); Warm(p,256); for(int i=0;i<256;i++)Require(Acquire(p,0,out VisualLease _),"premature cap"); Require(!Acquire(p,0,out VisualLease _),"overflow"); Require(p.ActiveCount==256&&p.Granted==256&&p.RejectedCapacity==1,"capacity math"); });
        Case("stress-2048-bursts-reuse-only-eight-objects", () => { var p=Pool(8); var visuals=Warm(p,8); int cycles=256; for(int j=0;j<cycles;j++){ var leases=new VisualLease[8]; for(int i=0;i<8;i++){Require(Acquire(p,j*10,out leases[i],5),"missing lease"); Require(ReferenceEquals(p.Get(leases[i]),visuals[leases[i].Slot]),"new allocation");} Require(!Acquire(p,j*10,out VisualLease _),"should cap"); Require(p.RetireExpired(j*10+5,Hide)==8 && p.ActiveCount==0,"failed rearm");} Require(p.Granted==2048&&p.Expired==2048&&p.RejectedCapacity==256,"stress totals"); });
        Case("no-visual-can-be-leased-to-two-consumers", () => { var p=Pool(2); var arr=Warm(p,2); Acquire(p,0,out VisualLease a); Acquire(p,0,out VisualLease b); Require(!ReferenceEquals(p.Get(a),p.Get(b))&&p.ActiveCount==2,"shared live instance"); });
        Case("expiry-clears-object-prior-to-reacquire", () => { var p=Pool(1); var v=Warm(p,1); Acquire(p,0,out VisualLease a,5); v[0].Shown=true; p.RetireExpired(5,Hide); Require(!v[0].Shown,"visible in free slot"); Acquire(p,5,out VisualLease b); Require(b.Token!=a.Token && p.Owns(b),"stale identity"); });
        Case("same-timestamp-acquire-allowed", () => { var p=Pool(2); Warm(p,2); Require(Acquire(p,0,out VisualLease a)&&Acquire(p,0,out VisualLease b)&&a.Token!=b.Token,"same clock"); });
        Case("stable-inactive-retire-does-not-call-host", () => { var p=Pool(); Warm(p,1); Require(p.RetireExpired(100,_=>throw new Exception("not in use"))==0,"inactive called"); });
        Case("no-implicit-auto-instantiation", () => { var p=Pool(4); Require(p.Registered==0&&p.ActiveCount==0,"precreated without host"); });
        int total=pass+fail;
        var receipt = new {
            package="B01-50-VISUALPOOL-001/R1", classification="REAL_ORIGINAL_DOTNET8_CSHARP_COSMETIC_LEASE_TEST_NOT_NATIVE_UNITY",
            passed=pass,failed=fail,total,failures=errors.ToArray(),
            stress="2048 real CSharp lease acquisitions with exactly 8 prewarmed object references, 256 cycles, no active visual overcap",
            nativeUnity="NOT_RUN",fps="NOT_MEASURED",unityInstantiateCount="NOT_MEASURED"
        };
        if(args.Length==2&&args[0]=="--receipt")
        {
            if(File.Exists(args[1])){Console.Error.WriteLine("Refuse to overwrite existing source QA receipt");return 3;}
            File.WriteAllText(args[1],JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true})+"\n");
        }
        Console.WriteLine("RESULT "+pass+"/"+total+" PASS; "+fail+" FAIL");
        return fail==0?0:1;
    }
}

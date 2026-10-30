// B01-60 / PASS17 W08: distinct independent mathematical and state QA.
// Compiles original unmodified B01-23 DebrisImpulseCore.cs directly.
// Simulated reference mathematics; NOT Unity/PhysX/Rigidbody/native G1.
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using B01.DebrisRepulse001;

internal static class Program
{
    static int passes, fails;
    static long totalCases, positive, zeroImpulse, invalidRejected, repeatedRejected, rearms;
    static readonly StringBuilder digestLog = new StringBuilder();
    static void Assert(bool cond,string detail)
    {
        if(!cond)throw new InvalidOperationException(detail);
    }
    static void Near(double observed,double expected,string detail,double tolerance=1e-8)
    {
        Assert(double.IsFinite(observed)&&double.IsFinite(expected)&&
               Math.Abs(observed-expected)<=tolerance*Math.Max(1,Math.Abs(expected)),
               detail+": actual "+observed.ToString("R")+" wanted "+expected.ToString("R"));
    }
    static void Check(string name,Action test)
    {
        try {test(); passes++;Console.WriteLine("PASS "+name);}
        catch(Exception e){fails++;Console.Error.WriteLine("FAIL "+name+" "+e);}
    }
    struct Deterministic
    {
        uint state;
        public Deterministic(uint seed){state=seed==0?1:seed;}
        public uint Next(){state^=state<<13;state^=state>>17;state^=state<<5;return state;}
        public int Int(int max){return (int)(Next()%(uint)max);}
    }
    static Vector3d V(double x,double y,double z)=>new Vector3d(x,y,z);
    static Vector3d Add(Vector3d a,Vector3d b)=>V(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
    static Vector3d R90(Vector3d v)=>V(-v.Z,v.Y,v.X);
    static double Length(Vector3d v)=>Math.Sqrt(v.MagnitudeSquared);
    static double Dot(Vector3d a,Vector3d b)=>Vector3d.Dot(a,b);
    static bool Equal(Vector3d a,Vector3d b,double tol=1e-8)=>
        Math.Abs(a.X-b.X)<=tol&&Math.Abs(a.Y-b.Y)<=tol&&Math.Abs(a.Z-b.Z)<=tol;
    static long Quant(double x)=>checked((long)Math.Round(x*1_000_000d,MidpointRounding.AwayFromZero));
    static void OutwardFormulaInvariant()
    {
        var ledger=new DebrisImpulseLedger();
        var translated=new DebrisImpulseLedger();
        var rotated=new DebrisImpulseLedger();
        var r=new Deterministic(0x5a173f08);
        const int n=50000;
        for(int i=0;i<n;i++)
        {
            long id=1000000L+i;
            Vector3d origin=V(r.Int(201)-100,r.Int(201)-100,r.Int(201)-100);
            int dx=r.Int(101)-50,dy=r.Int(101)-50,dz=r.Int(101)-50;
            if(dx==0&&dy==0&&dz==0)dx=1;
            Vector3d radial=V(dx,dy,dz);
            Vector3d centre=Add(origin,radial);
            Vector3d speed=V(r.Int(801)-400,r.Int(801)-400,r.Int(801)-400);
            double mass=(1+r.Int(10000))*.1d;
            double target=r.Int(5001)*.01d,cap=r.Int(5001)*.01d;
            Vector3d unit=radial*(1d/Length(radial));
            double radialVelocity=Dot(speed,unit);
            double deltaV=Math.Clamp(target-radialVelocity,0d,cap);
            Vector3d expected=unit*(mass*deltaV);
            Assert(ledger.TryDetach(id,origin,centre,speed,mass,target,cap,out var p),
                   "failed valid original core input #"+i);
            Near(p.X,expected.X,"analytical X #"+i);
            Near(p.Y,expected.Y,"analytical Y #"+i);
            Near(p.Z,expected.Z,"analytical Z #"+i);
            Near(Dot(p,unit)/mass,deltaV,"deltaV authorization #"+i);
            Assert(Length(p)<=mass*cap+1e-7,"max impulse exceeded mass*cap");
            Near(Dot(p,unit),Length(p),"injected tangential/lateral impulse #"+i,2e-8);
            double after=radialVelocity+Dot(p,unit)/mass;
            Near(after,radialVelocity+deltaV,"outward post-impulse velocity #"+i);
            // Position translation must not alter a world-relative impulse (integer coordinates).
            Vector3d shift=V(128,-32,64);
            Assert(translated.TryDetach(id,Add(origin,shift),Add(centre,shift),
                    speed,mass,target,cap,out var t),"translation rejects valid");
            Near(t.X,p.X,"translation X",3e-8);
            Near(t.Y,p.Y,"translation Y",3e-8);
            Near(t.Z,p.Z,"translation Z",3e-8);
            // 90-degree yaw of the full world must rotate the output by the same transform.
            Assert(rotated.TryDetach(id,R90(origin),R90(centre),R90(speed),
                    mass,target,cap,out var z),"rotation rejects valid");
            Vector3d rotatedP=R90(p);
            Near(z.X,rotatedP.X,"yaw-equivariance X",3e-8);
            Near(z.Y,rotatedP.Y,"yaw-equivariance Y",3e-8);
            Near(z.Z,rotatedP.Z,"yaw-equivariance Z",3e-8);
            if(deltaV==0){zeroImpulse++;Near(p.MagnitudeSquared,0,"zero pulse");}
            else positive++;
            // Same logical detach may have arbitrary additional collision callbacks: must all refuse.
            Assert(!ledger.TryDetach(id,origin,centre,speed,mass,target,cap,out var duplicate)
                   && duplicate.MagnitudeSquared==0,"double authorization");
            repeatedRejected++;
            if((i%127)==0)
            {
                Assert(ledger.RearmAfterReattach(id),"reattach rearm failed");
                rearms++;
                Assert(ledger.TryDetach(id,origin,centre,speed,mass,target,cap,out var again),
                       "reattach did not restore valid one shot");
                Near(again.X,p.X,"rearm identical X");
                Near(again.Y,p.Y,"rearm identical Y");
                Near(again.Z,p.Z,"rearm identical Z");
            }
            digestLog.Append(id).Append('|').Append(Quant(p.X)).Append('|')
              .Append(Quant(p.Y)).Append('|').Append(Quant(p.Z)).Append('|')
              .Append(Quant(deltaV)).Append('\n');
            totalCases++;
        }
        Assert(ledger.Count==n && translated.Count==n && rotated.Count==n,
               "stable ledger not tracking unique 50k detach IDs");
        Assert(positive>0&&zeroImpulse>0&&rearms>390,"uncovered randomized branches");
    }
    static void CoincidentFallbackEightWay()
    {
        var l=new DebrisImpulseLedger();
        var dirs=new HashSet<string>(StringComparer.Ordinal);
        var zero=V(100,1,-200);
        for(long id=1;id<=64;id++)
        {
            Assert(l.TryDetach(id,zero,zero,V(0,0,0),3,2.5,4,out var p),
                   "coincident fallback refused id "+id);
            Near(Length(p),7.5d,"fallback unit length");
            Near(p.Y,0d,"coincident fallback should not push vertically");
            var key=Quant(p.X)+":"+Quant(p.Z);
            dirs.Add(key);
            if(id<=8)Assert(!l.TryDetach(id,zero,zero,V(0,0,0),3,2.5,4,out _),
                            "coincident duplicate accepted");
        }
        Assert(dirs.Count==8,"expected 8 distinct stable hashed XZ fallback directions");
        Assert(l.Count==64,"coincident ID ledger mismatch");
    }
    static void AtomicRejectBadNumericAndRecover()
    {
        var l=new DebrisImpulseLedger();
        const double nan=double.NaN, inf=double.PositiveInfinity;
        var validOwner=V(0,0,0);var part=V(3,4,0);var still=V(0,0,0);
        for(int i=0;i<2000;i++)
        {
            long id=2000000+i;
            double mass=5,desired=2,cap=4;
            Vector3d owner=validOwner, centre=part, velocity=still;
            switch(i%16)
            {
                case 0:mass=nan;break;case 1:mass=inf;break;
                case 2:mass=0;break;case 3:mass=-1;break;
                case 4:mass=10001;break;case 5:desired=nan;break;
                case 6:desired=-1;break;case 7:desired=51;break;
                case 8:cap=inf;break;case 9:cap=-1;break;
                case 10:cap=51;break;case 11:owner=V(nan,0,0);break;
                case 12:centre=V(0,inf,0);break;
                case 13:velocity=V(0,0,-inf);break;
                case 14:velocity=V(10001,0,0);break;
                case 15:centre=V(1000001,0,0);break;
            }
            Assert(!l.TryDetach(id,owner,centre,velocity,mass,desired,cap,out var outBad)
                   && outBad.MagnitudeSquared==0,"unsafe bad accepted #"+i);
            Assert(!l.Contains(id),"invalid call burned stable ID #"+i);
            Assert(l.TryDetach(id,validOwner,part,still,5,2,4,out var ok)
                   && ok.MagnitudeSquared>0,"valid call after bad did not recover");
            invalidRejected++;
        }
        Assert(l.Count==2000,"invalid source led to unexpected ledger entries");
    }
    static void ZeroImpulseConsumesUntilExplicitRearm()
    {
        var l=new DebrisImpulseLedger();var owner=V(0,0,0);var part=V(1,0,0);
        Assert(l.TryDetach(11,owner,part,V(3,0,0),2,2.5,4,out var zero)
              && zero.MagnitudeSquared==0,"already-fast debris should have zero impulse");
        Assert(l.Contains(11) && !l.TryDetach(11,owner,part,V(0,0,0),2,2.5,4,out _),
               "zero impulse failed to consume original part one-shot");
        Assert(l.RearmAfterReattach(11),"reattach should clear zero impulse");
        Assert(l.TryDetach(11,owner,part,V(0,0,0),2,2.5,4,out var p),
               "valid rearmed event rejected");
        Near(p.X,5,"rearm nonzero impulse");
        Assert(!l.RearmAfterReattach(987654),"unknown ID rearmed");
        l.ClearForNewRun();
        Assert(l.Count==0 && l.TryDetach(11,owner,part,V(0,0,0),2,2.5,4,out _),
               "new-run reset did not clear events");
    }
    static void ConservativeAtNumericalBounds()
    {
        // Valid absolute position and velocity at upper API limits, actual output still finite.
        var l=new DebrisImpulseLedger();
        Assert(l.TryDetach(3001,V(1000000,1000000,1000000),
                             V(999999,999999,1000000),V(-10000,10000,10000),
                             10000,50,50,out var impulse),"API endpoint rejected");
        Assert(double.IsFinite(impulse.X)&&double.IsFinite(impulse.Y)&&
               double.IsFinite(impulse.Z),"nonfinite impulse at allowed bound");
        Assert(Length(impulse)<=500000.001,"finite max impulse violated");
        Assert(!l.TryDetach(0,V(0,0,0),V(1,0,0),V(0,0,0),1,2,4,out _),"id zero accepted");
        Assert(!l.RearmAfterReattach(-1),"negative rearm id accepted");
    }
    public static int Main()
    {
        Check("50k seed-based exact original DebrisImpulseCore CSharp radial metamorphic physics invariants",OutwardFormulaInvariant);
        Check("eight deterministic coincident centre XZ directions + stable one-shot",CoincidentFallbackEightWay);
        Check("2000 invalid numeric API detaches rejected without burning stable part identity",AtomicRejectBadNumericAndRecover);
        Check("already-fast zero impulse consumes one-shot; explicit reattach/new run rearming only",ZeroImpulseConsumesUntilExplicitRearm);
        Check("at API limits force stays finite and capped; invalid IDs fail-closed",ConservativeAtNumericalBounds);
        string digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(digestLog.ToString()))).ToLowerInvariant();
        Console.WriteLine("B01_60_W08_DEBRIS_INDEPENDENT_QA "+passes+" PASS "+fails+" FAIL");
        Console.WriteLine("B01_60_W08_DEBRIS_RANDOM_CASES "+totalCases+" POSITIVE "+positive+
                          " ZERO "+zeroImpulse+" DUPLICATE_REJECTED "+repeatedRejected+
                          " INVALID_REJECTED "+invalidRejected+" REARMED "+rearms);
        Console.WriteLine("B01_60_W08_DEBRIS_TRACE_SHA256 "+digest);
        Console.WriteLine("B01_60_W08_GATES native_Unity=NOT_RUN PhysX=NOT_RUN game_Integrated=NO");
        return fails==0?0:1;
    }
}

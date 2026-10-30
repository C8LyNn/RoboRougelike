// B01-60 PASS17/W08 independent physical-law source QA of ORIGINAL B01-23 #421 DebrisImpulseCore.
// This file NEVER replaces/patches original producer code. No Unity/PhysX runtime claims.
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using B01.DebrisRepulse001;
internal static class IndependentDebrisLawQA
{
    private static int passed,failed;
    private static long assertions;
    private static long fuzzEvents;
    private static readonly Vector3d Zero=new Vector3d(0,0,0);
    private static Vector3d V(double x,double y,double z)=>new Vector3d(x,y,z);
    private static Vector3d Plus(Vector3d a,Vector3d b)=>V(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
    private static Vector3d Neg(Vector3d a)=>V(-a.X,-a.Y,-a.Z);
    private static Vector3d Div(Vector3d a,double n)=>V(a.X/n,a.Y/n,a.Z/n);
    private static double Norm(Vector3d a)=>Math.Sqrt(a.MagnitudeSquared);
    private static void Check(bool ok,string message)
    {
        assertions++;
        if(!ok)throw new InvalidOperationException(message);
    }
    private static void Close(double a,double b,string message)
    {
        double allowed=1e-8*Math.Max(1,Math.Max(Math.Abs(a),Math.Abs(b)));
        Check(!double.IsNaN(a)&&!double.IsInfinity(a)&&
              Math.Abs(a-b)<=allowed,message+" ["+a.ToString("R",CultureInfo.InvariantCulture)+
              " vs "+b.ToString("R",CultureInfo.InvariantCulture)+"]");
    }
    private static void Test(string name,Action action)
    {
        try {action();passed++;Console.WriteLine("PASS "+name);}
        catch(Exception ex){failed++;Console.Error.WriteLine("FAIL "+name+": "+ex);}
    }
    private static bool Detach(DebrisImpulseLedger ledger,long id,Vector3d owner,Vector3d part,
        Vector3d vel,double mass,double desired,double cap,out Vector3d j)=>
        ledger.TryDetach(id,owner,part,vel,mass,desired,cap,out j);
    private static double Rand(Random r,double lo,double hi)=>lo+(hi-lo)*r.NextDouble();
    private static Vector3d RandomVector(Random r,double lo,double hi)=>
        V(Rand(r,lo,hi),Rand(r,lo,hi),Rand(r,lo,hi));
    private static void StepPhysicalInvariant(long id,Vector3d owner,Vector3d delta,
        Vector3d velocity,double mass,double desired,double cap,DebrisImpulseLedger ledger,
        StringBuilder digest)
    {
        Vector3d pos=Plus(owner,delta);
        double length=Norm(delta);Check(length>1e-5,"invalid nonzero fixture");
        Vector3d direction=Div(delta,length);
        double before=Vector3d.Dot(velocity,direction);
        double expectedAdded=Math.Min(cap,Math.Max(0,desired-before));
        Check(Detach(ledger,id,owner,pos,velocity,mass,desired,cap,out Vector3d j),
            "valid detach rejected at id="+id);
        Vector3d change=Div(j,mass);
        Vector3d after=Plus(velocity,change);
        Close(Norm(j),mass*expectedAdded,"J=mass*delta-v physical dimension");
        Close(Vector3d.Dot(change,direction),expectedAdded,"radial delta-v");
        Close(Vector3d.Dot(after,direction),before+expectedAdded,"radial final speed");
        Close(change.X,direction.X*expectedAdded,"perpendicular contamination X");
        Close(change.Y,direction.Y*expectedAdded,"perpendicular contamination Y");
        Close(change.Z,direction.Z*expectedAdded,"perpendicular contamination Z");
        Check(expectedAdded>=0 && expectedAdded<=cap+1e-10,"speed limit bypass");
        Check(ledger.Contains(id),"valid ID not reserved");
        Check(!Detach(ledger,id,owner,pos,velocity,mass,desired,cap,out Vector3d repeated),
            "double impulse / duplicate detach");
        Close(Norm(repeated),0,"duplicate leaked impulse");
        if(id%512==0)
        {
            // Rounded receipt is intentionally cross-OS stable without claiming raw IEEE bit parity.
            digest.Append(id).Append('|').Append(j.X.ToString("F5",CultureInfo.InvariantCulture))
                .Append('|').Append(j.Y.ToString("F5",CultureInfo.InvariantCulture))
                .Append('|').Append(j.Z.ToString("F5",CultureInfo.InvariantCulture)).Append('\n');
        }
        fuzzEvents++;
    }
    private static void RejectAtomic(long id,Vector3d owner,Vector3d part,Vector3d vel,
        double mass,double desired,double cap)
    {
        var l=new DebrisImpulseLedger();
        Check(!Detach(l,id,owner,part,vel,mass,desired,cap,out Vector3d j),
            "invalid input was accepted");
        Check(l.Count==0,"invalid event consumed ledger ID");
        Close(Norm(j),0,"invalid input leaked force");
    }
    public static int Main()
    {
        Test("ORIGINAL_RUNTIME_COMPILES_AND_NEWTON_SECONDS",()=>{
            var ledger=new DebrisImpulseLedger();
            Check(Detach(ledger,1,Zero,V(1,0,0),Zero,2,2.5,4,out Vector3d j),"valid");
            Close(j.X,5,"mass times delta velocity");
            Close(j.Y,0,"orthogonal force zero");
            Close(j.Z,0,"orthogonal force zero");
            Check(ledger.Count==1,"ledger count");
        });
        Test("65536_SEEDED_REAL_CSHARP_IMPULSES_AND_VELOCITY_LIMITS",()=>{
            var r=new Random(20261030);
            var ledger=new DebrisImpulseLedger();var receipt=new StringBuilder();
            for(int i=0;i<65536;i++)
            {
                var owner=RandomVector(r,-300,300);
                var delta=RandomVector(r,-80,80);
                if(Norm(delta)<.01)delta=V(1,.2,.3);
                var v=RandomVector(r,-30,30);
                double mass=Rand(r,.25,200),desired=Rand(r,0,12),cap=Rand(r,0,9);
                StepPhysicalInvariant(i+1,owner,delta,v,mass,desired,cap,ledger,receipt);
            }
            Check(ledger.Count==65536,"ledger lost or conflated IDs");
            byte[] sha=SHA256.HashData(Encoding.UTF8.GetBytes(receipt.ToString()));
            Console.WriteLine("DETERMINISTIC_QUANTIZED_RECEIPT_SHA256 "+
                Convert.ToHexString(sha).ToLowerInvariant()+"; 128 representative impulse vectors");
        });
        Test("TRANSLATIONAL_SYMMETRY_3D_1024_CONFIGURATIONS",()=>{
            var rng=new Random(22);
            for(int i=0;i<1024;i++)
            {
                var part=RandomVector(rng,-20,20);
                var velocity=RandomVector(rng,-6,6);
                var shift=RandomVector(rng,-400,400);
                var l1=new DebrisImpulseLedger();var l2=new DebrisImpulseLedger();
                Check(Detach(l1,1,Zero,part,velocity,7,2.5,4,out Vector3d first),"base");
                Check(Detach(l2,1,shift,Plus(part,shift),velocity,7,2.5,4,out Vector3d shifted),"shift");
                Close(first.X,shifted.X,"translation changed impulse X");
                Close(first.Y,shifted.Y,"translation changed impulse Y");
                Close(first.Z,shifted.Z,"translation changed impulse Z");
            }
        });
        Test("MIRROR_SYMMETRY_XZ_1024_CONFIGURATIONS",()=>{
            var rng=new Random(23);
            for(int i=0;i<1024;i++)
            {
                var p=RandomVector(rng,-50,50);
                if(Norm(p)<.001)p=V(3,2,1);
                var v=RandomVector(rng,-10,10);
                var a=new DebrisImpulseLedger();var b=new DebrisImpulseLedger();
                Check(Detach(a,1,Zero,p,v,2.5,3,5,out Vector3d left),"source");
                Check(Detach(b,1,Zero,Neg(p),Neg(v),2.5,3,5,out Vector3d right),"mirror");
                Close(left.X,-right.X,"mirrored x");
                Close(left.Y,-right.Y,"mirrored y");
                Close(left.Z,-right.Z,"mirrored z");
            }
        });
        Test("MASS_LINEARITY_WITH_EQUAL_DELTA_V",()=>{
            var rng=new Random(24);
            for(int i=0;i<2048;i++)
            {
                Vector3d p=RandomVector(rng,-10,10),v=RandomVector(rng,-8,8);
                if(Norm(p)<.01)p=V(3,4,5);
                var a=new DebrisImpulseLedger();var b=new DebrisImpulseLedger();
                Check(Detach(a,1,Zero,p,v,1,3,4,out Vector3d small),"mass1");
                Check(Detach(b,1,Zero,p,v,10,3,4,out Vector3d large),"mass10");
                Close(large.X,small.X*10,"mass linear x");
                Close(large.Y,small.Y*10,"mass linear y");
                Close(large.Z,small.Z*10,"mass linear z");
            }
        });
        Test("EIGHT_COINCIDENT_CENTER_STABLE_FALLBACKS",()=>{
            for(long id=1;id<=64;id++)
            {
                DebrisImpulseLedger a=new DebrisImpulseLedger(),b=new DebrisImpulseLedger();
                Check(Detach(a,id,Zero,Zero,Zero,2,2,4,out Vector3d first),"fallback A");
                Check(Detach(b,id,Zero,Zero,Zero,2,2,4,out Vector3d second),"fallback B");
                Close(Norm(first),4,"fallback magnitude");
                Close(first.X,second.X,"fallback deterministic x");
                Close(first.Y,second.Y,"fallback deterministic y");
                Close(first.Z,second.Z,"fallback deterministic z");
                Close(first.Y,0,"coincident fallback unexpectedly vertical");
                Check(!Detach(a,id,Zero,Zero,Zero,2,2,4,out _),"fallback not idempotent");
            }
        });
        Test("REARM_AND_RESET_IDENTITY_AND_MASS",()=>{
            var ledger=new DebrisImpulseLedger();
            Check(Detach(ledger,777,Zero,V(4,0,0),Zero,8,2,4,out var a),"initial");
            Check(!Detach(ledger,777,Zero,V(4,0,0),Zero,8,2,4,out _),"dup");
            Check(!ledger.RearmAfterReattach(778),"rearm missing ID accepted");
            Check(ledger.RearmAfterReattach(777),"rearm existing ID rejected");
            Check(Detach(ledger,777,Zero,V(4,0,0),Zero,8,2,4,out var b),"reattach retry");
            Close(a.X,b.X,"rearmed impulse changed");
            ledger.ClearForNewRun();
            Check(ledger.Count==0,"reset kept seen IDs");
            Check(Detach(ledger,777,Zero,V(4,0,0),Zero,8,2,4,out _),"new run refused original ID");
        });
        Test("ZERO_IMPULSE_STILL_CONSUMES_ONLY_VALID_EVENT",()=>{
            var l=new DebrisImpulseLedger();
            Check(Detach(l,22,Zero,V(1,0,0),V(8,0,0),5,2.5,4,out Vector3d j),"zero grant rejected");
            Close(Norm(j),0,"fast outward body received impulse");
            Check(l.Contains(22),"zero grant should consume ID");
            Check(!Detach(l,22,Zero,V(1,0,0),Zero,5,2.5,4,out _),"duplicate after zero grant");
        });
        Test("NEGATIVE_NAN_INFINITY_AND_UPPER_BOUNDS_FAIL_ATOMICALLY",()=>{
            RejectAtomic(-1,Zero,V(1,0,0),Zero,2,2.5,4);
            RejectAtomic(0,Zero,V(1,0,0),Zero,2,2.5,4);
            RejectAtomic(42,V(double.NaN,0,0),V(1,0,0),Zero,2,2.5,4);
            RejectAtomic(42,Zero,V(double.PositiveInfinity,0,0),Zero,2,2.5,4);
            RejectAtomic(42,Zero,V(1,0,0),V(double.NegativeInfinity,0,0),2,2.5,4);
            RejectAtomic(42,Zero,V(1,0,0),Zero,0,2.5,4);
            RejectAtomic(42,Zero,V(1,0,0),Zero,10001,2.5,4);
            RejectAtomic(42,Zero,V(1,0,0),Zero,2,-.01,4);
            RejectAtomic(42,Zero,V(1,0,0),Zero,2,51,4);
            RejectAtomic(42,Zero,V(1,0,0),Zero,2,2.5,51);
            RejectAtomic(42,Zero,V(1,0,0),V(10001,0,0),2,2.5,4);
            RejectAtomic(42,V(1000001,0,0),V(1,0,0),Zero,2,2.5,4);
        });
        Test("VALID_BOUNDARY_EXTREMES_FINITE_NO_OVERFLOW",()=>{
            var l=new DebrisImpulseLedger();
            Check(Detach(l,long.MaxValue,V(1000000,1000000,1000000),
                V(-1000000,-1000000,-1000000),V(-10000,10000,-10000),
                10000,50,50,out var j),"valid extreme rejected");
            Check(!double.IsNaN(Norm(j))&&!double.IsInfinity(Norm(j)),"extreme nonfinite J");
            Check(Norm(j)<=500000.000001,"extreme J over mass*cap");
        });
        Console.WriteLine("B01-60 W08 DEBRIS KINEMATICS: "+passed+" PASS / "+failed+
            " FAIL; "+assertions+" ASSERTIONS; "+fuzzEvents+
            " ORIGINAL C# SOURCE FUZZ EVENTS; UNITY_NATIVE/PHYSX/PLAYER NOT_RUN");
        return failed==0?0:1;
    }
}

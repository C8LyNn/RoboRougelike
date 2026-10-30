using System;
using System.Reflection;
using B01.DebrisRepulse001;
using UnityEngine;

// Independent original-source behavioral receiver. Original game C# source is
// injected byte-for-byte by the pinned Git fetch runner; never copied by hand.
internal static class Program
{
    private static int passed, failed;
    private static readonly Vector3 O = new Vector3(0,0,0);
    private static readonly Vector3 X = new Vector3(1,0,0);
    private static void Test(string title, Func<bool> body)
    {
        try { if (body()) { passed++; Console.WriteLine("PASS "+title); }
              else { failed++; Console.WriteLine("FAIL "+title); } }
        catch(Exception error) { failed++; Console.WriteLine("FAIL "+title+" "+error); }
    }
    private static Rigidbody Body(float mass=2, float x=1,float y=0,float z=0,
                                  float vx=0,float vy=0,float vz=0)
        => new Rigidbody {
           mass=mass,worldCenterOfMass=new Vector3(x,y,z),
           linearVelocity=new Vector3(vx,vy,vz)
        };
    private static void Speed(DebrisRepulsionEmitter e,float desired,float cap)
    {
        var type=typeof(DebrisRepulsionEmitter);
        type.GetField("desiredSpeed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(e,desired);
        type.GetField("maxAddedSpeed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(e,cap);
    }
    private static bool Near(double x,double y) => Math.Abs(x-y) < 1e-4;
    private static bool Applied(Rigidbody b,float x,float y=0,float z=0)
        => b.AddForceCalls==1 && b.LastMode==ForceMode.Impulse &&
           Near(b.LastForce.x,x) && Near(b.LastForce.y,y) && Near(b.LastForce.z,z);
    private static bool RejectAndRetry(Action<Rigidbody> poison)
    {
        var e=new DebrisRepulsionEmitter(); var b=Body(); poison(b);
        if(e.TryApplyDetachImpulse(710,b,O)||e.AppliedCount!=0||b.AddForceCalls!=0) return false;
        b=Body();
        return e.TryApplyDetachImpulse(710,b,O)&&Applied(b,5)&&e.AppliedCount==1;
    }
    private static void MainTests()
    {
        Test("real adapter east mass2 5 Nseconds uses Impulse",() => {
            var e=new DebrisRepulsionEmitter();var b=Body();
            return e.TryApplyDetachImpulse(1,b,O)&&Applied(b,5)&&e.AppliedCount==1;
        });
        Test("real adapter west negative outward force",() => {
            var e=new DebrisRepulsionEmitter();var b=Body(x:-1);
            return e.TryApplyDetachImpulse(2,b,O)&&Applied(b,-5);
        });
        Test("real adapter vertical outward through Rigidbody",() => {
            var e=new DebrisRepulsionEmitter();var b=Body(x:0,y:3);
            return e.TryApplyDetachImpulse(3,b,O)&&Applied(b,0,5,0);
        });
        Test("real adapter diagonal 3-4 normalization",() => {
            var e=new DebrisRepulsionEmitter();var b=Body(x:3,y:4);
            return e.TryApplyDetachImpulse(4,b,O)&&Applied(b,3,4,0);
        });
        Test("real adapter respect owner world centre shift",() => {
            var e=new DebrisRepulsionEmitter();var b=Body(x:10);
            return e.TryApplyDetachImpulse(5,b,new Vector3(11,0,0))&&Applied(b,-5);
        });
        Test("real adapter uses preexisting outward linearVelocity",() => {
            var e=new DebrisRepulsionEmitter();var b=Body(vx:1);
            return e.TryApplyDetachImpulse(6,b,O)&&Applied(b,3);
        });
        Test("real adapter outward velocity already fast consumes without AddForce",() => {
            var e=new DebrisRepulsionEmitter();var b=Body(vx:6);
            return e.TryApplyDetachImpulse(7,b,O)&&b.AddForceCalls==0&&e.AppliedCount==1;
        });
        Test("real adapter strong inward motion caps delta-v at 4",() => {
            var e=new DebrisRepulsionEmitter();var b=Body(vx:-99);
            return e.TryApplyDetachImpulse(8,b,O)&&Applied(b,8);
        });
        Test("real adapter lateral velocity ignored radial",() => {
            var e=new DebrisRepulsionEmitter();var b=Body(vy:8,vz:-5);
            return e.TryApplyDetachImpulse(9,b,O)&&Applied(b,5);
        });
        Test("real adapter does not use ForceMode.Force",() => {
            var e=new DebrisRepulsionEmitter();var b=Body();
            return e.TryApplyDetachImpulse(10,b,O)&&b.LastMode==ForceMode.Impulse;
        });
        Test("null body rejected before ledger ID consumption",() => {
            var e=new DebrisRepulsionEmitter();
            bool r=e.TryApplyDetachImpulse(11,null,O);
            var b=Body();
            return !r&&e.AppliedCount==0&&e.TryApplyDetachImpulse(11,b,O)&&Applied(b,5);
        });
        Test("kinematic rigidbody rejection preserves ID for dynamic retry",() => {
            var e=new DebrisRepulsionEmitter();var b=Body();b.isKinematic=true;
            bool rejected=!e.TryApplyDetachImpulse(12,b,O);
            b.isKinematic=false;
            return rejected&&e.AppliedCount==0&&e.TryApplyDetachImpulse(12,b,O)&&Applied(b,5);
        });
        Test("zero stable ID cannot add physics force",() => {
            var e=new DebrisRepulsionEmitter();var b=Body();
            return !e.TryApplyDetachImpulse(0,b,O)&&b.AddForceCalls==0&&e.AppliedCount==0;
        });
        Test("negative stable ID cannot add physics force",() => {
            var e=new DebrisRepulsionEmitter();var b=Body();
            return !e.TryApplyDetachImpulse(-123,b,O)&&b.AddForceCalls==0&&e.AppliedCount==0;
        });
        Test("duplicate detach event same body no extra force",() => {
            var e=new DebrisRepulsionEmitter();var b=Body();
            bool once=e.TryApplyDetachImpulse(15,b,O);
            return once&&!e.TryApplyDetachImpulse(15,b,O)&&Applied(b,5)&&e.AppliedCount==1;
        });
        Test("duplicate stable owner ID on different Rigidbody suppressed",() => {
            var e=new DebrisRepulsionEmitter();var a=Body();var b=Body();
            return e.TryApplyDetachImpulse(16,a,O)&&!e.TryApplyDetachImpulse(16,b,O)&&
                   Applied(a,5)&&b.AddForceCalls==0&&e.AppliedCount==1;
        });
        Test("distinct positive IDs can both detach same body",() => {
            var e=new DebrisRepulsionEmitter();var b=Body();
            return e.TryApplyDetachImpulse(17,b,O)&&e.TryApplyDetachImpulse(18,b,O)&&
                   b.AddForceCalls==2&&e.AppliedCount==2;
        });
        Test("rearm one previously seen ID enables exactly another force",() => {
            var e=new DebrisRepulsionEmitter();var b=Body();
            return e.TryApplyDetachImpulse(19,b,O)&&e.RearmAfterReattach(19)&&
                  e.TryApplyDetachImpulse(19,b,O)&&b.AddForceCalls==2&&e.AppliedCount==1;
        });
        Test("rearm missing ID returns false",() => !new DebrisRepulsionEmitter().RearmAfterReattach(123));
        Test("rearm nonpositive ID rejected",() => {
            var e=new DebrisRepulsionEmitter();
            return !e.RearmAfterReattach(0)&&!e.RearmAfterReattach(-1);
        });
        Test("new run clears only the ledger and reuses same ID",() => {
            var e=new DebrisRepulsionEmitter();var b=Body();
            bool a=e.TryApplyDetachImpulse(20,b,O);
            e.ClearForNewRun();
            return a&&e.AppliedCount==0&&e.TryApplyDetachImpulse(20,b,O)&&b.AddForceCalls==2;
        });
        Test("coincident world centres deterministic ID mod8 fallback",() => {
            var e=new DebrisRepulsionEmitter();var b=Body(x:0);
            return e.TryApplyDetachImpulse(24,b,O)&&Applied(b,5);
        });
        Test("coincident centre other ID fallback north direction",() => {
            var e=new DebrisRepulsionEmitter();var b=Body(x:0);
            return e.TryApplyDetachImpulse(26,b,O)&&Applied(b,0,0,5);
        });
        Test("coincident fallback reattach same position same vector",() => {
            var e=new DebrisRepulsionEmitter();var b=Body(x:0);
            if(!e.TryApplyDetachImpulse(25,b,O)||b.AddForceCalls!=1)return false;
            var first=b.LastForce;
            return e.RearmAfterReattach(25)&&e.TryApplyDetachImpulse(25,b,O)&&
                b.AddForceCalls==2&&Near(first.x,b.LastForce.x)&&Near(first.z,b.LastForce.z);
        });
        Test("config desired=0 means consumed with zero physical impulse",() => {
            var e=new DebrisRepulsionEmitter();Speed(e,0,4);var b=Body();
            return e.TryApplyDetachImpulse(28,b,O)&&b.AddForceCalls==0&&
                   e.AppliedCount==1&&!e.TryApplyDetachImpulse(28,b,O);
        });
        Test("config cap=0 means consumed with zero physical impulse",() => {
            var e=new DebrisRepulsionEmitter();Speed(e,2.5f,0);var b=Body();
            return e.TryApplyDetachImpulse(29,b,O)&&b.AddForceCalls==0&&e.AppliedCount==1;
        });
        Test("config desired=1 cap=4 yields actual 2Ns",() => {
            var e=new DebrisRepulsionEmitter();Speed(e,1,4);var b=Body();
            return e.TryApplyDetachImpulse(30,b,O)&&Applied(b,2);
        });
        Test("config desired=4 cap=1 caps actual to 2Ns",() => {
            var e=new DebrisRepulsionEmitter();Speed(e,4,1);var b=Body();
            return e.TryApplyDetachImpulse(31,b,O)&&Applied(b,2);
        });
        Test("config NaN desired rejected and retry valid",() => {
            var e=new DebrisRepulsionEmitter();Speed(e,float.NaN,4);var b=Body();
            bool denied=!e.TryApplyDetachImpulse(32,b,O)&&e.AppliedCount==0&&b.AddForceCalls==0;
            Speed(e,2.5f,4);
            return denied&&e.TryApplyDetachImpulse(32,b,O)&&Applied(b,5);
        });
        Test("config Infinity cap rejects without consuming ID",() => {
            var e=new DebrisRepulsionEmitter();Speed(e,2.5f,float.PositiveInfinity);var b=Body();
            bool denied=!e.TryApplyDetachImpulse(33,b,O)&&e.AppliedCount==0;
            Speed(e,2.5f,4);
            return denied&&e.TryApplyDetachImpulse(33,b,O)&&Applied(b,5);
        });
        Test("config negative desired rejected",() => {
            var e=new DebrisRepulsionEmitter();Speed(e,-1,4);var b=Body();
            return !e.TryApplyDetachImpulse(34,b,O)&&e.AppliedCount==0&&b.AddForceCalls==0;
        });
        Test("config desired over original maximum rejected",() => {
            var e=new DebrisRepulsionEmitter();Speed(e,51,4);var b=Body();
            return !e.TryApplyDetachImpulse(35,b,O)&&e.AppliedCount==0;
        });
        Test("NaN physical mass rejected then valid body retry",()=>RejectAndRetry(b=>b.mass=float.NaN));
        Test("Infinity physical mass rejected then valid body retry",()=>RejectAndRetry(b=>b.mass=float.PositiveInfinity));
        Test("zero physical mass rejected then valid body retry",()=>RejectAndRetry(b=>b.mass=0));
        Test("negative physical mass rejected then valid body retry",()=>RejectAndRetry(b=>b.mass=-1));
        Test("mass beyond original cap rejected then valid body retry",()=>RejectAndRetry(b=>b.mass=10001));
        Test("NaN centre position rejected then valid body retry",()=>RejectAndRetry(b=>b.worldCenterOfMass=new Vector3(float.NaN,0,0)));
        Test("Infinity centre position rejected then valid body retry",()=>RejectAndRetry(b=>b.worldCenterOfMass=new Vector3(1,float.PositiveInfinity,0)));
        Test("oversized centre position rejected then valid body retry",()=>RejectAndRetry(b=>b.worldCenterOfMass=new Vector3(1000001,0,0)));
        Test("NaN linearVelocity rejected then valid body retry",()=>RejectAndRetry(b=>b.linearVelocity=new Vector3(float.NaN,0,0)));
        Test("Infinity linearVelocity rejected then valid body retry",()=>RejectAndRetry(b=>b.linearVelocity=new Vector3(0,float.NegativeInfinity,0)));
        Test("overspeed linearVelocity rejected then valid body retry",()=>RejectAndRetry(b=>b.linearVelocity=new Vector3(10001,0,0)));
        Test("owner NaN rejected, id remains available",() => {
            var e=new DebrisRepulsionEmitter();var b=Body();
            bool invalid=!e.TryApplyDetachImpulse(44,b,new Vector3(float.NaN,0,0));
            return invalid&&e.AppliedCount==0&&e.TryApplyDetachImpulse(44,b,O)&&Applied(b,5);
        });
        Test("owner Infinity rejected without physics",() => {
            var e=new DebrisRepulsionEmitter();var b=Body();
            return !e.TryApplyDetachImpulse(45,b,new Vector3(0,0,float.NegativeInfinity))&&
                   e.AppliedCount==0&&b.AddForceCalls==0;
        });
        Test("mass 10000 max accepted creates finite force",() => {
            var e=new DebrisRepulsionEmitter();var b=Body(mass:10000);
            return e.TryApplyDetachImpulse(46,b,O)&&Applied(b,25000);
        });
        Test("multiple IDs and reset old body is no longer prevented",() => {
            var e=new DebrisRepulsionEmitter();var b=Body();
            for(int i=1;i<=8;i++)if(!e.TryApplyDetachImpulse(i,b,O))return false;
            if(e.AppliedCount!=8||b.AddForceCalls!=8)return false;
            e.ClearForNewRun();
            return e.AppliedCount==0&&e.TryApplyDetachImpulse(8,b,O)&&b.AddForceCalls==9;
        });
    }
    public static int Main()
    {
        MainTests();
        Console.WriteLine("B0161_W07_ORIGINAL_EMITTER_TESTS="+passed+" PASS / "+failed+" FAIL");
        return failed==0&&passed==47?0:2;
    }
}

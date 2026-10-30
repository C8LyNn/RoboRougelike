using System;
using B01.DebrisRepulse001;

internal static class Program
{
    private static int passed, failed;
    private static readonly Vector3d Zero = new Vector3d(0,0,0);
    private static readonly Vector3d East = new Vector3d(1,0,0);
    private static bool Near(double x,double y) => Math.Abs(x-y)<0.000001;
    private static void Test(string name,Func<bool> fn)
    {
        try { if (fn()) { passed++; Console.WriteLine("PASS "+name); }
              else { failed++; Console.WriteLine("FAIL "+name); } }
        catch (Exception e) { failed++; Console.WriteLine("FAIL "+name+" "+e.Message); }
    }
    private static bool Detach(DebrisImpulseLedger l,long id,Vector3d pos,Vector3d vel,
        double mass,double target,double cap,out Vector3d impulse) =>
        l.TryDetach(id,Zero,pos,vel,mass,target,cap,out impulse);

    private static int Main()
    {
        Test("stationary mass2 outward impulse5",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,1,East,Zero,2,2.5,4,out var p) && Near(p.X,5) && Near(p.Y,0);
        });
        Test("outward already fast -> zero impulse",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,2,East,new Vector3d(3,0,0),2,2.5,4,out var p)
                && Near(p.MagnitudeSquared,0) && l.Contains(2);
        });
        Test("inward velocity capped",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,3,East,new Vector3d(-100,0,0),2,2.5,4,out var p)
                && Near(p.X,8);
        });
        Test("lateral movement ignored in radial velocity",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,4,East,new Vector3d(0,8,9),2,2.5,4,out var p)
                && Near(p.X,5);
        });
        Test("partial outward velocity reduces impulse",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,5,East,new Vector3d(1,0,0),2,2.5,4,out var p)
                && Near(p.X,3);
        });
        Test("double mass gives double impulse",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,6,East,Zero,4,2.5,4,out var p) && Near(p.X,10);
        });
        Test("west produces negative impulse",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,7,new Vector3d(-4,0,0),Zero,1,2,4,out var p) && Near(p.X,-2);
        });
        Test("3-4-5 diagonal normalization",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,8,new Vector3d(3,4,0),Zero,1,5,5,out var p)
                && Near(p.X,3) && Near(p.Y,4);
        });
        Test("duplicate detach rejected",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,9,East,Zero,1,2,3,out _) &&
                !Detach(l,9,East,Zero,1,2,3,out var p) &&
                l.Count==1 && Near(p.MagnitudeSquared,0);
        });
        Test("rearm after reattach enables new impulse",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,10,East,Zero,1,2,3,out _) &&
                l.RearmAfterReattach(10) && Detach(l,10,East,Zero,1,2,3,out _);
        });
        Test("unknown rearm returns false",()=>!new DebrisImpulseLedger().RearmAfterReattach(10));
        Test("clear for new run",() => {
            var l=new DebrisImpulseLedger();
            bool a=Detach(l,11,East,Zero,1,2,3,out _);
            l.ClearForNewRun();
            return a && l.Count==0 && Detach(l,11,East,Zero,1,2,3,out _);
        });
        Test("zero id rejected",()=>!Detach(new DebrisImpulseLedger(),0,East,Zero,1,2,3,out _));
        Test("negative id rejected",()=>!Detach(new DebrisImpulseLedger(),-1,East,Zero,1,2,3,out _));
        Test("mass zero rejected",()=>!Detach(new DebrisImpulseLedger(),12,East,Zero,0,2,3,out _));
        Test("mass negative rejected",()=>!Detach(new DebrisImpulseLedger(),12,East,Zero,-1,2,3,out _));
        Test("mass NaN rejected",()=>!Detach(new DebrisImpulseLedger(),12,East,Zero,double.NaN,2,3,out _));
        Test("mass over cap rejected",()=>!Detach(new DebrisImpulseLedger(),12,East,Zero,10001,2,3,out _));
        Test("NaN centre rejected",()=>!Detach(new DebrisImpulseLedger(),12,new Vector3d(double.NaN,0,0),Zero,1,2,3,out _));
        Test("infinite centre rejected",()=>!Detach(new DebrisImpulseLedger(),12,new Vector3d(double.PositiveInfinity,0,0),Zero,1,2,3,out _));
        Test("oversized centre rejected",()=>!Detach(new DebrisImpulseLedger(),12,new Vector3d(1000001,0,0),Zero,1,2,3,out _));
        Test("infinite velocity rejected",()=>!Detach(new DebrisImpulseLedger(),12,East,new Vector3d(0,double.NegativeInfinity,0),1,2,3,out _));
        Test("overspeed velocity rejected",()=>!Detach(new DebrisImpulseLedger(),12,East,new Vector3d(10001,0,0),1,2,3,out _));
        Test("negative desired speed rejected",()=>!Detach(new DebrisImpulseLedger(),12,East,Zero,1,-1,3,out _));
        Test("desired speed over cap rejected",()=>!Detach(new DebrisImpulseLedger(),12,East,Zero,1,51,3,out _));
        Test("negative added speed rejected",()=>!Detach(new DebrisImpulseLedger(),12,East,Zero,1,2,-1,out _));
        Test("infinite added speed rejected",()=>!Detach(new DebrisImpulseLedger(),12,East,Zero,1,2,double.PositiveInfinity,out _));
        Test("invalid does not consume ID",() => {
            var l=new DebrisImpulseLedger();
            return !Detach(l,13,East,Zero,double.NaN,2,3,out _) &&
                l.Count==0 && Detach(l,13,East,Zero,1,2,3,out _);
        });
        Test("zero desired speed consumes without impulse",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,14,East,Zero,1,0,3,out var p) &&
                Near(p.MagnitudeSquared,0) && !Detach(l,14,East,Zero,1,2,3,out _);
        });
        Test("zero cap consumes without impulse",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,15,East,Zero,1,2,0,out var p) && Near(p.MagnitudeSquared,0);
        });
        Test("coincident centres stable nonzero fallback",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,16,Zero,Zero,2,2.5,4,out var p) &&
                Near(p.MagnitudeSquared,25) && Near(p.Y,0);
        });
        Test("fallback same after reset",() => {
            var l=new DebrisImpulseLedger();
            bool a=Detach(l,17,Zero,Zero,1,2,4,out var p);
            l.ClearForNewRun();
            return a && Detach(l,17,Zero,Zero,1,2,4,out var q) &&
                Near(p.X,q.X) && Near(p.Z,q.Z);
        });
        Test("fallback differs for distinct IDs",() => {
            var l=new DebrisImpulseLedger();
            return Detach(l,18,Zero,Zero,1,2,4,out var p) &&
                Detach(l,19,Zero,Zero,1,2,4,out var q) &&
                (!Near(p.X,q.X)||!Near(p.Z,q.Z));
        });
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        return failed==0?0:1;
    }
}

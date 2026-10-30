using System;
using B01.DebrisPlayground001;
using B01.DebrisRepulse001;

internal static class Program
{
    private static int pass, fail;
    private static bool Near(double x,double y,double tolerance=1e-6) => Math.Abs(x-y)<=tolerance;
    private static void Verify(string name, Func<bool> predicate)
    {
        try { if (predicate()) { pass++; Console.WriteLine("PASS "+name); }
              else { fail++; Console.WriteLine("FAIL "+name); } }
        catch(Exception e) { fail++; Console.WriteLine("FAIL "+name+": "+e.Message); }
    }
    private static bool Throws(Action action)
    {
        try { action(); return false; }
        catch(ArgumentOutOfRangeException) { return true; }
    }
    private static int Main()
    {
        var light=DebrisRigDemoCatalog.Light;
        var heavy=DebrisRigDemoCatalog.Heavy;
        Verify("two stable positive IDs",()=>light.PartId>0&&heavy.PartId>0&&light.PartId!=heavy.PartId);
        Verify("two different mass rigs",()=>Near(light.PartMassKg,1.5)&&Near(heavy.PartMassKg,12));
        Verify("two different chassis sizes",()=>Near(light.ChassisWidth,1.4)&&Near(heavy.ChassisWidth,2.4));
        Verify("light offset clears chassis",()=>light.PartOffsetX>(light.ChassisWidth+light.PartWidth)/2);
        Verify("heavy offset clears chassis",()=>heavy.PartOffsetX>(heavy.ChassisWidth+heavy.PartWidth)/2);
        Verify("light source spawn is left",()=>light.RootX<0);
        Verify("heavy source spawn is right",()=>heavy.RootX>0);
        Verify("rigs' original envelopes do not overlap",()=>light.RootX+light.PartOffsetX+light.PartWidth/2 <
            heavy.RootX-heavy.ChassisWidth/2);
        Verify("source scenario index 0",()=>DebrisRigDemoCatalog.ByIndex(0).PartId==light.PartId);
        Verify("source scenario index 1",()=>DebrisRigDemoCatalog.ByIndex(1).PartId==heavy.PartId);
        Verify("out-of-bounds scenario index rejected",()=>Throws(()=>DebrisRigDemoCatalog.ByIndex(2)));
        Verify("negative scenario index rejected",()=>Throws(()=>DebrisRigDemoCatalog.ByIndex(-1)));
        Verify("invalid part ID rejected",()=>Throws(()=>new RigDemoDefinition(0,"x",0,1,1,.1,1,0)));
        Verify("negative part ID rejected",()=>Throws(()=>new RigDemoDefinition(-1,"x",0,1,1,.1,1,0)));
        Verify("empty name rejected",()=>Throws(()=>new RigDemoDefinition(1," ",0,1,1,.1,1,0)));
        Verify("NaN root rejected",()=>Throws(()=>new RigDemoDefinition(1,"x",double.NaN,1,1,.1,1,0)));
        Verify("infinite mass rejected",()=>Throws(()=>new RigDemoDefinition(1,"x",0,1,1,.1,double.PositiveInfinity,0)));
        Verify("zero mass rejected",()=>Throws(()=>new RigDemoDefinition(1,"x",0,1,1,.1,0,0)));
        Verify("negative chassis width rejected",()=>Throws(()=>new RigDemoDefinition(1,"x",0,-1,1,.1,1,0)));
        Verify("zero part width rejected",()=>Throws(()=>new RigDemoDefinition(1,"x",0,1,0,.1,1,0)));
        Verify("zero clearance rejected",()=>Throws(()=>new RigDemoDefinition(1,"x",0,1,1,0,1,0)));
        Verify("negative clearance rejected",()=>Throws(()=>new RigDemoDefinition(1,"x",0,1,1,-.1,1,0)));
        Verify("infinite speed rejected",()=>Throws(()=>new RigDemoDefinition(1,"x",0,1,1,.1,1,double.NegativeInfinity)));
        Verify("invalid huge speed rejected",()=>Throws(()=>new RigDemoDefinition(1,"x",0,1,1,.1,1,21)));
        Verify("huge root outside range rejected",()=>Throws(()=>new RigDemoDefinition(1,"x",101,1,1,.1,1,0)));
        Verify("huge chassis rejected",()=>Throws(()=>new RigDemoDefinition(1,"x",0,21,1,.1,1,0)));
        Verify("huge part mass rejected",()=>Throws(()=>new RigDemoDefinition(1,"x",0,1,1,.1,1001,0)));
        Verify("source original #421 core predicts light physically",()=>{
            var l=new DebrisImpulseLedger();
            return l.TryDetach(light.PartId,new Vector3d(light.RootX,1.2,0),
                new Vector3d(light.RootX+light.PartOffsetX,1.2,0),
                new Vector3d(light.InitialRadialSpeed,0,0),light.PartMassKg,2.5,4,out var p) &&
                Near(p.X,3.75) && l.Count==1;
        });
        Verify("source original #421 core predicts heavy physical delta",()=>{
            var l=new DebrisImpulseLedger();
            return l.TryDetach(heavy.PartId,new Vector3d(heavy.RootX,1.2,0),
                new Vector3d(heavy.RootX+heavy.PartOffsetX,1.2,0),
                new Vector3d(heavy.InitialRadialSpeed,0,0),heavy.PartMassKg,2.5,4,out var p) &&
                Near(p.X,42) && l.Count==1;
        });
        Verify("repeated source detach rejected",()=>{
            var l=new DebrisImpulseLedger();
            bool first=l.TryDetach(light.PartId,new Vector3d(light.RootX,1.2,0),
                new Vector3d(light.RootX+light.PartOffsetX,1.2,0),
                new Vector3d(0,0,0),light.PartMassKg,2.5,4,out _);
            return first && !l.TryDetach(light.PartId,new Vector3d(light.RootX,1.2,0),
                new Vector3d(light.RootX+light.PartOffsetX,1.2,0),
                new Vector3d(0,0,0),light.PartMassKg,2.5,4,out _);
        });
        Verify("source reattach rearm",()=>{
            var l=new DebrisImpulseLedger();
            l.TryDetach(light.PartId,new Vector3d(light.RootX,1.2,0),
                new Vector3d(light.RootX+light.PartOffsetX,1.2,0),
                new Vector3d(0,0,0),light.PartMassKg,2.5,4,out _);
            return l.RearmAfterReattach(light.PartId) && l.Count==0;
        });
        Verify("source C#9 compatibility positive light",()=>light.PartOffsetX>0);
        Verify("source C#9 compatibility positive heavy",()=>heavy.PartOffsetX>0);
        Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        return fail==0 ? 0:1;
    }
}

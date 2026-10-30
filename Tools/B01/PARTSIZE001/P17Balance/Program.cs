// Runs EXACT same production source linked from Assets, not a reimplementation.
using System;
using System.Collections.Generic;
using B01.PARTSIZE001.P17Balance;

internal static class Program
{
    private static int count;
    private static void Check(bool ok,string name)
    {
        if(!ok) throw new Exception("FAIL "+name);
        count++;
        Console.WriteLine("PASS "+name);
    }
    private static void Near(double actual,double expected,double tol,string name)
    { Check(Math.Abs(actual-expected)<=tol,name); }
    private static void Reject(string code,Action action,string name)
    {
        try {action();}
        catch(BalanceInputException e)
        { Check(e.Code==code,name+" "+e.Code); return; }
        throw new Exception("FAIL "+name+" expected "+code);
    }
    private static PartMass M(string id,double kg,double x,double y,double z)
    {return new PartMass(id,kg,new PointM(x,y,z));}
    private static GroundContact C(string id,double x,double z,double y=0)
    {return new GroundContact(id,new PointM(x,y,z));}
    private static List<GroundContact> Square()
    {return new List<GroundContact>{C("FL",-1,-1),C("FR",1,-1),C("BR",1,1),C("BL",-1,1)};}
    private static List<PartMass> At(double x,double y,double z)
    {return new List<PartMass>{M("core",10,x,y,z)};}

    private static void Main()
    {
        var centered=MassBalanceSolver.Evaluate(At(0,1,0),Square());
        Near(centered.TotalKilograms,10,1e-12,"measured mass");
        Near(centered.CenterMeters.X,0,1e-12,"center X");
        Near(centered.CenterMeters.Z,0,1e-12,"center Z");
        Near(centered.SignedEdgeMarginMeters,1,1e-12,"square positive margin");
        Near(centered.IdealTipAngleDegrees,45,1e-9,"ideal tip angle");
        Check(centered.ProjectionWithinSupport,"inside square");
        Check(centered.HullVertexCount==4,"four hull vertices");
        var outside=MassBalanceSolver.Evaluate(At(2,1,0),Square());
        Near(outside.SignedEdgeMarginMeters,-1,1e-12,"outside negative margin");
        Near(outside.IdealTipAngleDegrees,-45,1e-9,"outside negative tip angle");
        Check(!outside.ProjectionWithinSupport,"outside square");
        Near(MassBalanceSolver.Evaluate(At(1,1,0),Square()).SignedEdgeMarginMeters,0,1e-12,"boundary zero margin");
        var weighted=MassBalanceSolver.Evaluate(
            new List<PartMass>{M("L",1,-1,1,0),M("R",3,1,1,0)},Square());
        Near(weighted.CenterMeters.X,.5,1e-12,"weighted COM");
        Near(weighted.TotalKilograms,4,1e-12,"weighted total");
        var tri=MassBalanceSolver.Evaluate(At(.5,1,.5),
            new List<GroundContact>{C("A",0,0),C("B",2,0),C("C",0,2)});
        Check(tri.ProjectionWithinSupport && tri.HullVertexCount==3,"triangular hull");
        Near(tri.SignedEdgeMarginMeters,.5,1e-12,"triangle edge distance");
        var withInner=Square();
        withInner.Add(C("center",0,0));
        Check(MassBalanceSolver.Evaluate(At(0,1,0),withInner).HullVertexCount==4,"internal contact ignored for hull");
        var reversed=Square(); reversed.Reverse();
        Near(MassBalanceSolver.Evaluate(At(.2,1,.4),Square()).SignedEdgeMarginMeters,
             MassBalanceSolver.Evaluate(At(.2,1,.4),reversed).SignedEdgeMarginMeters,1e-12,"contact order invariant");
        for(int turn=0;turn<4;turn++)
        {
            var pos=MassBalanceSolver.RotateYawQuarterTurns(new PointM(.2,1,.4),turn);
            var rotated=new List<GroundContact>();
            foreach(var c in Square())
                rotated.Add(new GroundContact(c.Id,MassBalanceSolver.RotateYawQuarterTurns(c.PointMeters,turn)));
            Near(MassBalanceSolver.Evaluate(At(pos.X,pos.Y,pos.Z),rotated).SignedEdgeMarginMeters,
                 .6,1e-12,"yaw margin invariant "+turn);
        }
        Near(MassBalanceSolver.RotateYawQuarterTurns(new PointM(1,2,3),1).X,3,1e-12,"yaw 90 x");
        Near(MassBalanceSolver.RotateYawQuarterTurns(new PointM(1,2,3),1).Z,-1,1e-12,"yaw 90 z");
        Near(MassBalanceSolver.RotateYawQuarterTurns(new PointM(1,2,3),2).X,-1,1e-12,"yaw 180");
        Near(MassBalanceSolver.RotateYawQuarterTurns(new PointM(1,2,3),3).Z,1,1e-12,"yaw 270");
        Reject("UNSUPPORTED_YAW_TURNS",()=>MassBalanceSolver.RotateYawQuarterTurns(new PointM(0,0,0),4),"bad turn");
        Reject("NONFINITE_POSITION",()=>MassBalanceSolver.RotateYawQuarterTurns(new PointM(double.NaN,0,0),0),"bad yaw position");
        Reject("MISSING_OR_EXCESS_PART_MASSES",()=>MassBalanceSolver.Evaluate(new List<PartMass>(),Square()),"zero mass owners");
        Reject("INSUFFICIENT_OR_EXCESS_CONTACTS",()=>MassBalanceSolver.Evaluate(At(0,1,0),new List<GroundContact>{C("a",0,0),C("b",1,0)}),"two contacts");
        Reject("UNMEASURED_OR_INVALID_PART_MASS",()=>MassBalanceSolver.Evaluate(AtMass(0),Square()),"zero mass");
        Reject("UNMEASURED_OR_INVALID_PART_MASS",()=>MassBalanceSolver.Evaluate(AtMass(-1),Square()),"negative mass");
        Reject("UNMEASURED_OR_INVALID_PART_MASS",()=>MassBalanceSolver.Evaluate(AtMass(double.NaN),Square()),"NaN mass");
        Reject("MASS_TOTAL_OVERFLOW",()=>MassBalanceSolver.Evaluate(
            new List<PartMass>{M("a",double.MaxValue,0,1,0),M("b",double.MaxValue,0,1,0)},Square()),"mass overflow");
        Reject("INVALID_OR_DUPLICATE_PART_ID",()=>MassBalanceSolver.Evaluate(
            new List<PartMass>{M("same",1,0,1,0),M("same",1,0,1,0)},Square()),"duplicate part ID");
        Reject("INVALID_OR_DUPLICATE_PART_ID",()=>MassBalanceSolver.Evaluate(
            new List<PartMass>{M("",1,0,1,0)},Square()),"missing part ID");
        Reject("NONFINITE_POSITION",()=>MassBalanceSolver.Evaluate(At(double.PositiveInfinity,1,0),Square()),"nonfinite COM");
        Reject("INVALID_OR_DUPLICATE_CONTACT_ID",()=>MassBalanceSolver.Evaluate(At(0,1,0),
            new List<GroundContact>{C("a",-1,-1),C("a",1,-1),C("b",0,1)}),"duplicate contact ID");
        Reject("DUPLICATE_SUPPORT_POSITION",()=>MassBalanceSolver.Evaluate(At(0,1,0),
            new List<GroundContact>{C("a",0,0),C("b",0,0),C("c",0,1)}),"duplicate contact position");
        Reject("CONTACTS_NOT_COPLANAR",()=>MassBalanceSolver.Evaluate(At(0,1,0),
            new List<GroundContact>{C("a",0,0),C("b",1,0),C("c",0,1,.006)}),"nonplanar real wheel");
        Reject("COLLINEAR_CONTACTS",()=>MassBalanceSolver.Evaluate(At(0,1,0),
            new List<GroundContact>{C("a",0,0),C("b",1,0),C("c",2,0)}),"collinear contacts");
        Reject("COM_NOT_ABOVE_SUPPORT_PLANE",()=>MassBalanceSolver.Evaluate(At(0,0,0),Square()),"nonpositive COM height");
        Reject("NONFINITE_POSITION",()=>MassBalanceSolver.Evaluate(At(0,1,0),
            new List<GroundContact>{C("a",0,0),C("b",1,0),C("c",double.NaN,1)}),"NaN support");
        var tooMany=new List<GroundContact>(); for(int i=0;i<65;i++)tooMany.Add(C("s"+i,i,i*i));
        Reject("INSUFFICIENT_OR_EXCESS_CONTACTS",()=>MassBalanceSolver.Evaluate(At(0,1,0),tooMany),"contact cap");
        var tooManyParts=new List<PartMass>();for(int i=0;i<181;i++)tooManyParts.Add(M("p"+i,1,0,1,0));
        Reject("MISSING_OR_EXCESS_PART_MASSES",()=>MassBalanceSolver.Evaluate(tooManyParts,Square()),"part cap");
        Console.WriteLine("SOURCE_CORE_TESTS_PASS="+count+" NATIVE_UNITY=NOT_RUN PHYSX=NOT_RUN");
    }
    private static List<PartMass> AtMass(double kg)
    {return new List<PartMass>{M("core",kg,0,1,0)};}
}

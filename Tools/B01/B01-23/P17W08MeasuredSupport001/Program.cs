// Compile actual new Unity adapter AND immutable #417 original MassBalanceSolver.
// Only UnityEngine API is explicitly stubbed: not PhysX, not Unity Editor/Player.
using System;
using System.Collections.Generic;
using B01.MeasuredSupport001;
using B01.PARTSIZE001.P17Balance;
using UnityEngine;

internal static class Program
{
    private static int tests;
    private static void Require(bool yes,string name)
    {
        if(!yes) throw new Exception("FAIL "+name);
        tests++;Console.WriteLine("PASS "+name);
    }
    private static void Near(double actual,double expected,string label,double tolerance=1e-6)
    {
        Require(!double.IsNaN(actual)&&Math.Abs(actual-expected)<=tolerance,label);
    }
    private static Rigidbody Body(float mass,float x,float y,float z,bool kinematic=false)
    {
        return new Rigidbody{mass=mass,worldCenterOfMass=new Vector3(x,y,z),isKinematic=kinematic};
    }
    private static WheelCollider Wheel(float x,float z,float y=0,bool grounded=true)
    {
        return new WheelCollider{IsGrounded=grounded,Hit=new WheelHit{point=new Vector3(x,y,z)}};
    }
    private static WheelCollider[] Square()
    { return new[]{Wheel(-1,-1),Wheel(1,-1),Wheel(1,1),Wheel(-1,1)}; }
    private static (bool ok,StabilityResult result,string reason) Capture(Rigidbody[] bodies,WheelCollider[] wheels)
    {
        StabilityResult r;string e;
        bool ok=MeasuredSupportCapture.TryMeasure(bodies,wheels,out r,out e);
        return (ok,r,e);
    }
    private static void Reject(Rigidbody[] bodies,WheelCollider[] wheels,string code)
    {
        var p=Capture(bodies,wheels);
        Require(!p.ok&&p.result==null&&p.reason==code,"reject "+code);
    }
    private static void Main()
    {
        var centered=Capture(new[]{Body(10,0,1,0)},Square());
        Require(centered.ok&&centered.reason==null,"real hit four-wheel success");
        Near(centered.result.TotalKilograms,10,"actual Rigidbody mass");
        Near(centered.result.CenterMeters.X,0,"actual Rigidbody COM x");
        Near(centered.result.CenterMeters.Y,1,"actual Rigidbody COM y");
        Near(centered.result.SignedEdgeMarginMeters,1,"measured square support margin");
        Near(centered.result.IdealTipAngleDegrees,45,"measured ideal tip angle");
        Require(centered.result.HullVertexCount==4,"convex hull vertex count");
        Require(centered.result.ProjectionWithinSupport,"inside support");
        var outside=Capture(new[]{Body(10,2,1,0)},Square());
        Require(outside.ok&&!outside.result.ProjectionWithinSupport,"outside support");
        Near(outside.result.SignedEdgeMarginMeters,-1,"negative outside margin");
        var multiple=Capture(new[]{Body(1,-1,1,0),Body(3,1,1,0)},Square());
        Require(multiple.ok,"multiple truly separate Rigidbody masses");
        Near(multiple.result.CenterMeters.X,.5,"measured two-body weighted COM");
        Near(multiple.result.TotalKilograms,4,"measured two-body total");
        var ordered=Square();
        Array.Reverse(ordered);
        var reversed=Capture(new[]{Body(10,.2f,1,.4f)},ordered);
        Require(reversed.ok,"reversed actual wheel order");
        Near(reversed.result.SignedEdgeMarginMeters,.6f,"order-independent footprint");
        var four=Square();four[0].IsGrounded=false;
        var tri=Capture(new[]{Body(10,.2f,1,.2f)},four);
        Require(tri.ok&&tri.result.HullVertexCount==3,"real 3 wheel contact triangle");
        Require(four[0].Calls==1,"actual GetGroundHit sampled on airborne wheel");
        Reject(new Rigidbody[0],Square(),"MISSING_OR_EXCESS_RIGIDBODIES");
        Reject(new[]{Body(10,0,1,0)},new WheelCollider[0],"MISSING_OR_EXCESS_WHEEL_COLLIDERS");
        Reject(null,Square(),"MISSING_OR_EXCESS_RIGIDBODIES");
        Reject(new[]{Body(10,0,1,0)},null,"MISSING_OR_EXCESS_WHEEL_COLLIDERS");
        var original=Body(1,0,1,0);
        Reject(new[]{original,original},Square(),"MISSING_OR_DUPLICATE_RIGIDBODY");
        Reject(new Rigidbody[]{null},Square(),"MISSING_OR_DUPLICATE_RIGIDBODY");
        Reject(new[]{Body(-1,0,1,0)},Square(),"INVALID_MEASURED_MASS");
        Reject(new[]{Body(0,0,1,0)},Square(),"INVALID_MEASURED_MASS");
        Reject(new[]{Body(float.NaN,0,1,0)},Square(),"INVALID_MEASURED_MASS");
        Reject(new[]{Body(10,0,1,0,true)},Square(),"BODY_INACTIVE_OR_KINEMATIC");
        var inactiveBody=Body(10,0,1,0);inactiveBody.gameObject.activeInHierarchy=false;
        Reject(new[]{inactiveBody},Square(),"BODY_INACTIVE_OR_KINEMATIC");
        var originalWheel=Wheel(-1,-1);
        Reject(new[]{Body(10,0,1,0)},new[]{originalWheel,originalWheel,Wheel(1,1)},
            "MISSING_OR_DUPLICATE_WHEEL_COLLIDER");
        Reject(new[]{Body(10,0,1,0)},new WheelCollider[]{Wheel(-1,-1),Wheel(1,1),null},
            "MISSING_OR_DUPLICATE_WHEEL_COLLIDER");
        var airborne=Square();airborne[0].IsGrounded=false;airborne[1].IsGrounded=false;
        Reject(new[]{Body(10,0,1,0)},airborne,"INSUFFICIENT_ACTUALLY_GROUNDED_WHEELS");
        var disabled=Square();disabled[0].enabled=false;disabled[1].enabled=false;
        Reject(new[]{Body(10,0,1,0)},disabled,"INSUFFICIENT_ACTUALLY_GROUNDED_WHEELS");
        var inactive=Square();inactive[0].gameObject.activeInHierarchy=false;
        inactive[1].gameObject.activeInHierarchy=false;
        Reject(new[]{Body(10,0,1,0)},inactive,"INSUFFICIENT_ACTUALLY_GROUNDED_WHEELS");
        Require(inactive[0].Calls==0&&inactive[1].Calls==0,"no physics calls for inactive wheels");
        var nans=Square();nans[0].Hit=new WheelHit{point=new Vector3(float.NaN,0,0)};
        Reject(new[]{Body(10,0,1,0)},nans,"NONFINITE_POSITION");
        Reject(new[]{Body(10,float.NaN,1,0)},Square(),"NONFINITE_POSITION");
        var heights=Square();heights[0].Hit=new WheelHit{point=new Vector3(-1,.02f,-1)};
        Reject(new[]{Body(10,0,1,0)},heights,"CONTACTS_NOT_COPLANAR");
        var duplicate=Square();duplicate[1].Hit=duplicate[0].Hit;
        Reject(new[]{Body(10,0,1,0)},duplicate,"DUPLICATE_SUPPORT_POSITION");
        Reject(new[]{Body(10,0,0,0)},Square(),"COM_NOT_ABOVE_SUPPORT_PLANE");
        var line=new[]{Wheel(-2,0),Wheel(0,0),Wheel(2,0)};
        Reject(new[]{Body(10,0,1,0)},line,"COLLINEAR_CONTACTS");
        var manyBodies=new Rigidbody[181];
        for(int i=0;i<manyBodies.Length;i++) manyBodies[i]=Body(1,0,1,0);
        Reject(manyBodies,Square(),"MISSING_OR_EXCESS_RIGIDBODIES");
        var manyWheels=new WheelCollider[65];
        for(int i=0;i<manyWheels.Length;i++)manyWheels[i]=Wheel(i,i);
        Reject(new[]{Body(10,0,1,0)},manyWheels,"MISSING_OR_EXCESS_WHEEL_COLLIDERS");
        Console.WriteLine("ORIGINAL_SOURCE_ADAPTER_ASSERTIONS_PASS="+tests+
           " PHYSICS=STUB_NOT_REAL UNITY_EDITOR=NOT_RUN PLAYMODE=NOT_RUN");
    }
}

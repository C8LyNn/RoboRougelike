using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.CHL003;

// External B01-21 SOURCE-ONLY QA: uses the exact original CHL003GateCrossing
// production C#, plus independent analytic shapes sourced from B01-20 demo.
// Does NOT execute Unity, actual Collider.bounds, PhysX or real Rigidbody.
internal static class GateGeometryReceipt
{
    const double WheelRadius=.41, Guard=.25, GateWidth=3.5, BridgeWidth=3.7;
    static readonly List<object> Rows=new();
    static int pass,fail;

    static double HalfWidth(bool heavy,double yaw)
    {
        double a=yaw*Math.PI/180, sn=Math.Sin(a), cs=Math.Cos(a);
        double hullW=heavy?2.5:1.45, hullL=heavy?3.4:2.3;
        double wheelX=heavy?1.0:.59, wheelZ=heavy?1.15:.75;
        // Real rotated cuboid AABB or 4 rotated sphere AABBs.
        return Math.Max(hullW/2*cs+hullL/2*sn,wheelX*cs+wheelZ*sn+WheelRadius);
    }

    static double AngleLimit(bool heavy,double aperture)
    {
        double lo=0,hi=40;
        for(int i=0;i<80;i++)
        {
            double mid=(lo+hi)/2;
            if(aperture/2-HalfWidth(heavy,mid)>=Guard)lo=mid;
            else hi=mid;
        }
        return lo;
    }

    static bool ActualPolicy(bool heavy,double yaw,double centerX,double gateWidth,
        double previousZ=-.12,double currentZ=.12)
    {
        double h=HalfWidth(heavy,yaw);
        return CHL003GateCrossing.IsForwardFullBodyCrossing(
            (float)previousZ,(float)currentZ,
            (float)(centerX-h),(float)(centerX+h),
            .40f,2.90f,
            (float)(-gateWidth/2),(float)(gateWidth/2),0f,4.5f,
            (float)Math.Cos(yaw*Math.PI/180));
    }

    static void Check(string id,bool heavy,double yaw,double x,bool expected,
        string reasoning,double aperture=GateWidth)
    {
        bool got=ActualPolicy(heavy,yaw,x,aperture);
        double w=2*HalfWidth(heavy,yaw);
        double gateSideReserve=aperture/2-HalfWidth(heavy,yaw)-Math.Abs(x);
        double bridgeSideReserve=BridgeWidth/2-HalfWidth(heavy,yaw)-Math.Abs(x);
        Rows.Add(new {
            test=id,rig=heavy?"HEAVY":"LIGHT",yawDeg=yaw,centerOffsetMeters=x,
            sourceResult=got,expected,passed=got==expected,
            projectedWidthMeters=Math.Round(w,6),
            gateMarginMeters=Math.Round(gateSideReserve,6),
            bridgeMarginMeters=Math.Round(bridgeSideReserve,6),
            requiredMarginMeters=Guard,reasoning,nativeUnity="NOT_RUN"
        });
        if(got==expected){pass++;Console.WriteLine("CHL_LATEST_GATE_SOURCE_PASS "+id);}
        else{fail++;Console.Error.WriteLine("CHL_LATEST_GATE_SOURCE_FAIL "+id+" expected="+expected+" got="+got);}
    }

    static int Main(string[] args)
    {
        if(args.Length!=1){Console.Error.WriteLine("Usage: dotnet run -- <json out>");return 3;}
        // Every result below runs ORIGINAL B01-20 production C# class,
        // with independently derived actual hull+wheel AABB coordinates.
        Check("HEAVY_CENTERED_STRAIGHT_ACCEPT",true,0,0,true,"Nominal heavy straight");
        Check("HEAVY_OFFSET_0_08_ACCEPT",true,0,.08,true,"Within actual gate side reserve");
        Check("HEAVY_OFFSET_0_10_REJECT",true,0,.10,false,"On wide actual physical bridge but gate 0.25m guard violated");
        Check("HEAVY_OFFSET_NEG_0_10_REJECT",true,0,-.10,false,"Left/right symmetry");
        Check("HEAVY_CENTERED_YAW_3_ACCEPT",true,3,0,true,"Subthreshold yaw");
        Check("HEAVY_CENTERED_YAW_4_ACCEPT",true,4,0,true,"Near edge");
        Check("HEAVY_CENTERED_YAW_5_REJECT",true,5,0,false,"Even centered, gate rejects geometry");
        Check("HEAVY_CENTERED_YAW_8_REJECT",true,8,0,false,"Bridge clearance .25 PASS, gate clearance FAIL");
        Check("HEAVY_CENTERED_YAW_10_REJECT",true,10,0,false,"Bridge still >=.25 PASS, gate FAIL");
        Check("HEAVY_YAW_8_ON_BRIDGE_POLICY_PASS",true,8,0,true,"Physically broader 3.70m lane, same production gate predicate",BridgeWidth);
        Check("HEAVY_YAW_12_ON_BRIDGE_POLICY_REJECT",true,12,0,false,"Bridge review guard .25 also violated",BridgeWidth);
        Check("LIGHT_CENTERED_STRAIGHT_ACCEPT",false,0,0,true,"Light baseline");
        Check("LIGHT_OFFSET_0_40_ACCEPT",false,0,.40,true,"Light generous side clearance");
        Check("LIGHT_OFFSET_0_55_REJECT",false,0,.55,false,"Light also constrained by physical aperture");
        Check("LIGHT_YAW_8_ACCEPT",false,8,0,true,"Actual Light envelope");
        Check("LIGHT_YAW_30_ACCEPT",false,30,0,true,"Actual Light envelope");
        // Backward signed center-depth is asserted separately below.
        // Final guard: previous and current positions backwards or no travel
        // via the ACTUAL policy, independent from scene trigger callback.
        bool backwards=ActualPolicy(true,0,0,GateWidth,.12,-.12);
        if(!backwards)pass++;else fail++;
        Rows.Add(new{test="HEAVY_NEGATIVE_SIGN_DEPTH_REJECT",sourceResult=backwards,expected=false,passed=!backwards,nativeUnity="NOT_RUN"});

        double gateLimit=AngleLimit(true,GateWidth);
        double bridgeLimit=AngleLimit(true,BridgeWidth);
        if(!(gateLimit>4.6&&gateLimit<4.7&&bridgeLimit>10.3&&bridgeLimit<10.4&&
             Math.Abs((GateWidth/2-HalfWidth(true,0)-Guard)-.09)<1e-6))fail++;
        else pass++;
        Rows.Add(new {test="HEAVY_GUARD_YAW_LIMITS",
            actualGateLimitDeg=gateLimit,actualBridgeLimitDeg=bridgeLimit,
            straightGateCenterOffsetLimitMeters=GateWidth/2-HalfWidth(true,0)-Guard,
            passed=gateLimit>4.6&&gateLimit<4.7&&bridgeLimit>10.3&&bridgeLimit<10.4,
            nativeUnity="NOT_RUN"});
        var receipt=new {
          taskId="B01-21-C-DQ-NEWHEAD-GATE-20261027",
          sourceExactCommit="f4ca318577edbdf4d003ab3e104ac7ec71d83cfe",
          actualProductionClass="B01.CHL003.CHL003GateCrossing",
          originalPolicyOnly=true, actualUnity="NOT_RUN",physicalDrive="NOT_RUN",
          sourceTestPassed=pass,sourceTestFailed=fail,total=pass+fail,
          realCHLWidthMeters=GateWidth,realBridgeWidthMeters=BridgeWidth,
          guardEachSideMeters=Guard,results=Rows
        };
        var f=Path.GetFullPath(args[0]);Directory.CreateDirectory(Path.GetDirectoryName(f)!);
        File.WriteAllText(f,JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"CHL_SOURCE_GATE_EXACT_RESULT PASS={pass} FAIL={fail} NATIVE_UNITY=NOT_RUN");
        return fail==0?0:2;
    }
}

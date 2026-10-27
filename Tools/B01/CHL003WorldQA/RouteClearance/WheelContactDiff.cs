using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using B01.CHL003;

// Independent B01-21 source contract execution, no fake Unity engine.
internal static class WheelContactDiff
{
    private static int pass, fail;
    private static readonly List<object> Rows = new();
    private const string Producer = "2355bff6026e22443b164aa9de816c27d3a289e9";

    private static void Assert(string label, bool predicate)
    {
        if (predicate) { pass++; Console.WriteLine("B01_21_SOURCE_CSHARP_PASS " + label); }
        else { fail++; Console.Error.WriteLine("B01_21_SOURCE_CSHARP_FAIL " + label); }
    }

    private static void Pose(string rig, string segment, string scenario,
        float topY, float rootY, bool expectNew, bool expectOld, float radius=.41f)
    {
        // Exact unchanged demo wheel local vertical -.52; radius .41; new source
        // origin lift .14 and allowance .03. These are algebraic hypothetical poses.
        float wheelBottomGap = rootY-.52f-radius-topY;
        float distance = rootY-.52f+CHL003WheelSupportWindow.OriginLiftMeters-topY;
        bool oldCanRaycast = distance>=0 && distance<=.95f;
        bool newProductionPolicy = CHL003WheelSupportWindow.WithinWheelReach(distance,radius);
        bool physicallySeparated = wheelBottomGap>0.0001f;
        Assert(label:rig+"_"+segment+"_"+scenario,
            oldCanRaycast==expectOld && newProductionPolicy==expectNew);
        Rows.Add(new {
            rig,segment,scenario,rootY,sourceFloorY=topY,
            physicalWheelBottomGapMeters=Math.Round(wheelBottomGap,5),
            originalRayHitDistanceMeters=Math.Round(distance,5),
            preFixLong095mRayWouldReach=oldCanRaycast,
            expectedOld=expectOld,
            productionWheelReachAccepts=newProductionPolicy,
            expectedNew=expectNew,
            wheelSeparated=physicallySeparated,
            unityPhysXObserved="NOT_RUN"
        });
    }

    private static int Main(string[] args)
    {
        if(args.Length!=1) { Console.Error.WriteLine("Usage: dotnet run -- <out.json>");return 3;}
        foreach (var rig in new[]{"Light","Heavy"})
        {
            Pose(rig,"GROUND","ACTUAL_SPAWN_GAP_009",0f,1.02f,false,true);
            Pose(rig,"GROUND","HOVER_GAP_035",0f,1.28f,false,true);
            Pose(rig,"GROUND","OUTSIDE_BOTH_GAP_041",0f,1.34f,false,false);
            Pose(rig,"GROUND","IDEAL_TOUCH",0f,.93f,true,true);
            Pose(rig,"BRIDGE","IDEAL_TOUCH",1.68f,2.61f,true,true);
            Pose(rig,"BRIDGE","HOVER_GAP_009",1.68f,2.70f,false,true);
            Pose(rig,"BRIDGE","HOVER_GAP_035",1.68f,2.96f,false,true);
            Pose(rig,"BARRIER","IDEAL_TOUCH",.26f,1.19f,true,true);
            Pose(rig,"BARRIER","HOVER_GAP_009",.26f,1.28f,false,true);
        }
        // Actual ORIGINAL producer negative/positive parameter validation.
        Assert("SOURCE_ACTUAL_R41_CAST_058",
            Math.Abs(CHL003WheelSupportWindow.MaximumCastMeters(.41f)-.58f)<.000001f);
        Assert("SOURCE_ACTUAL_R82_CAST_099",
            Math.Abs(CHL003WheelSupportWindow.MaximumCastMeters(.82f)-.99f)<.000001f);
        Assert("SOURCE_MAX_BOUNDARY_ACCEPT",
            CHL003WheelSupportWindow.WithinWheelReach(
                CHL003WheelSupportWindow.MaximumCastMeters(.41f),.41f));
        Assert("SOURCE_OVER_BOUNDARY_REJECT",
            !CHL003WheelSupportWindow.WithinWheelReach(
                CHL003WheelSupportWindow.MaximumCastMeters(.41f)+.0001f,.41f));
        Assert("SOURCE_INVALID_RADIUS_ZERO_REJECT",
            CHL003WheelSupportWindow.MaximumCastMeters(0f)==0f);
        Assert("SOURCE_INVALID_RADIUS_NAN_REJECT",
            CHL003WheelSupportWindow.MaximumCastMeters(float.NaN)==0f);
        Assert("SOURCE_INVALID_RADIUS_INFINITY_REJECT",
            CHL003WheelSupportWindow.MaximumCastMeters(float.PositiveInfinity)==0f);
        Assert("SOURCE_INVALID_HIT_NEGATIVE_REJECT",
            !CHL003WheelSupportWindow.WithinWheelReach(-.01f,.41f));
        Assert("SOURCE_INVALID_HIT_NAN_REJECT",
            !CHL003WheelSupportWindow.WithinWheelReach(float.NaN,.41f));
        Assert("SOURCE_INVALID_HIT_INFINITY_REJECT",
            !CHL003WheelSupportWindow.WithinWheelReach(float.PositiveInfinity,.41f));
        var receipt=new {
            taskId="B01-21-PASS3-OLD-NEW-MOTOR-DIFFERENTIAL",
            producerInputSHA=Producer,
            compiledOriginalPureCSharp="Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003WheelSupportWindow.cs",
            originalSourceWindowBlob="edfc98c0adb55bf8b092db43d93a15be29e99490",
            motorSourceBlob="9b777837179253300cd73cb5e156abe5221bb8d5",
            demoSourceBlob="9308ceb9eab89d1ab483b560ff3ed58fa6d2d32e",
            sourceTestPass=pass,sourceTestFail=fail,
            nativeUnity="NOT_RUN",actualRaycast="NOT_RUN",actualWheelCollision="NOT_RUN",
            formerFalseGroundIsFixedAtSourcePolicy=true,
            noClaimOfPhysicsGameReady=true,rows=Rows
        };
        var path=Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path,JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"B01_21_PASS3_ACTUAL_ORIGINAL_WINDOW_CSHARP_RESULT PASS={pass} FAIL={fail} NATIVE_NOT_RUN");
        return fail==0?0:2;
    }
}
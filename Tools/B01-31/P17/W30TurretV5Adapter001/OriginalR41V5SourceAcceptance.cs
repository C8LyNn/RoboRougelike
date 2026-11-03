using System;
using RoboRougelike.B0131.W30;

static class OriginalR41V5SourceAcceptance
{
    static int assertions;
    static void Yes(bool condition,string test)
    {
        if (!condition)throw new InvalidOperationException("ORIGINAL_R41V5_MUZZLE_SOURCE_RED: "+test);
        Console.WriteLine("R41V5_ORIGINAL_PRODUCTION_CODE_PASS "+(++assertions)+" "+test);
    }
    static bool Near(double a,double b,double tol=1e-9)=>Math.Abs(a-b)<tol;
    static void Main()
    {
        Yes(Near(R41V5MuzzleGeometry.GridMeters,0.55),"01-existing-R41-grid-preserved");
        Yes(R41V5MuzzleGeometry.TryPose(0,0,R41V5MuzzleSide.Left,out var zeroL),"02-left-zero");
        Yes(R41V5MuzzleGeometry.TryPose(0,0,R41V5MuzzleSide.Right,out var zeroR),"03-right-zero");
        Yes(Near(zeroL.X,-.06)&&Near(zeroL.Y,.135)&&Near(zeroL.Z,.70),"04-left-neutral-physical-coordinate");
        Yes(Near(zeroR.X,.06)&&Near(zeroR.Y,.135)&&Near(zeroR.Z,.70),"05-right-neutral-physical-coordinate");
        Yes(Near(zeroL.ForwardZ,1)&&Near(zeroR.ForwardZ,1)&&
            Near(zeroL.ForwardX,0),"06-corrected-YawPitch-output-Z-forward");
        Yes(R41V5MuzzleGeometry.ValidatePosePair(in zeroL,in zeroR),"07-neutral-12cm-pair");
        Yes(R41V5MuzzleGeometry.ValidateImportedNeutral(-.06,.135,.70,.06,.135,.70,out _),
            "08-actual-reported-baked-FBX-positions-accepted");
        Yes(!R41V5MuzzleGeometry.ValidateImportedNeutral(.06,.135,-.70,-.06,.135,-.70,out var reason)
            && reason.Contains("AXIS_CONVERSION"),"09-actual-reported-legacy-default-unbaked-negative-Z-rejected");
        Yes(!R41V5MuzzleGeometry.ValidateImportedNeutral(-.06,.135,.700,.06,.135,.65,out _),
            "10-short-right-barrel-rejected");
        Yes(!R41V5MuzzleGeometry.ValidateImportedNeutral(double.NaN,.135,.70,.06,.135,.70,out _),
            "11-NaN-rejected-before-pose-authority");
        Yes(!R41V5MuzzleGeometry.ValidateImportedNeutral(-.06,.135,.70,.06,.135,double.PositiveInfinity,out _),
            "12-Infinity-rejected");
        Yes(R41V5MuzzleGeometry.ValidateImportedNeutral(-.059,.135,.700,.059,.135,.699,out _),
            "13-within-10mm-original-import-position-tolerance");
        double[] yaw={0,45,-90,180};
        double[] pitch={0,-20,10};
        foreach(var y in yaw)
        foreach(var p in pitch)
        {
            bool positive=R41V5MuzzleGeometry.TryPose(y,p,R41V5MuzzleSide.Left,out var l)
                       && R41V5MuzzleGeometry.TryPose(y,p,R41V5MuzzleSide.Right,out var r)
                       && R41V5MuzzleGeometry.ValidatePosePair(in l,in r);
            Yes(positive,$"14-25-physical-symmetric-muzzle-pair-{y}-{p}");
            Yes(Near(l.Y,r.Y,1e-9)&&Near(l.ForwardY,r.ForwardY,1e-9),
                $"26-37-both-muzzles-share-same-physical-pitch-{y}-{p}");
        }
        Yes(R41V5MuzzleGeometry.TryPose(90,0,R41V5MuzzleSide.Left,out var xForward)
            && Near(xForward.ForwardX,1)&&Near(xForward.ForwardZ,0,1e-9),
            "38-yaw-90-fire-positive-X");
        Yes(R41V5MuzzleGeometry.TryPose(0,-20,R41V5MuzzleSide.Left,out var raised)
            && raised.ForwardY>0 && raised.Y>.135,
            "39-negative-pitch-raises-physical-forward");
        foreach(var invalid in new [] {double.NaN,double.PositiveInfinity,double.NegativeInfinity,-181,181})
        {
            Yes(!R41V5MuzzleGeometry.TryPose(invalid,0,R41V5MuzzleSide.Left,out _),
                "40-44-reject-invalid-yaw-"+invalid);
            Yes(!R41V5MuzzleGeometry.TryPose(0,invalid,R41V5MuzzleSide.Left,out _),
                "45-49-reject-invalid-pitch-"+invalid);
        }
        Yes(!R41V5MuzzleGeometry.TryPose(0,46,R41V5MuzzleSide.Right,out _),
            "50-source-QA-pitch-envelope-guard");
        Yes(!R41V5MuzzleGeometry.TryPose(0,0,(R41V5MuzzleSide)99,out _),
            "51-undefined-muzzle-refused");
        Yes(assertions==51,"52-exact-executed-source-assertions");
        Console.WriteLine("B01_31_W30_ORIGINAL_CSHARP_MUZZLE_52_ASSERTIONS_PASS__REAL_UNITY_FBX_IMPORT_NOT_RUN");
    }
}

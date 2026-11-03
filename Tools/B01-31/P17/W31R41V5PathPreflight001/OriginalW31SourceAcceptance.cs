using System;
using System.IO;
using RoboRougelike.B0131.W30;

static class OriginalW31SourceCsharpAcceptance
{
    private static int count=0;
    private static void Check(bool good,string label)
    {
        if (!good) throw new InvalidOperationException("W31_REAL_SOURCE_CSHARP_RED: "+label);
        Console.WriteLine("B01_31_W31_REAL_PATH_SOURCE_PASS "+(++count)+" "+label);
    }

    private static void Invalid(string root,string? path,string expected,string name)
    {
        string p; string reason;
        bool ok=R41V5AssetPaths.TryResolveFbxPath(root,path!,out p,out reason);
        Check(!ok && p=="" && reason==expected,name);
    }

    public static void Main()
    {
        string assets=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"RRL_W31_Native_QA_Project","Assets"));
        var full=Path.Combine(assets,"B01_31","W30R41V5Adapter","Incoming","R41_Turret_v5.fbx");
        Check(R41V5AssetPaths.TryResolveFbxPath(assets,R41V5AssetPaths.ExactV5AssetPath,
              out var resolved,out var reason),"01-normal-source-project-root-resolution");
        Check(resolved==full && reason=="","02-no-CWD-FBX-address-and-positive-reason-empty");
        Check(Path.IsPathRooted(resolved),"03-path-is-absolute");
        Check(resolved!.EndsWith("R41_Turret_v5.fbx",StringComparison.Ordinal),
              "04-exact-source-producer-name-preserved");
        Check(!File.Exists(resolved),"05-no-fabricated-external-FBX-file-in-test-project");
        var originalCwd=Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(Path.GetTempPath());
            Check(R41V5AssetPaths.TryResolveFbxPath(assets,R41V5AssetPaths.ExactV5AssetPath,
                  out var another,out _),"06-with-process-working-directory-moved-resolution-valid");
            Check(another==full,"07-current-working-directory-does-not-change-import-path");
        }
        finally {Directory.SetCurrentDirectory(originalCwd);}
        Check(Directory.GetCurrentDirectory()==originalCwd,"08-process-current-directory-restored");
        Invalid("relative/Assets",R41V5AssetPaths.ExactV5AssetPath,
                "UNITY_ASSETS_DIRECTORY_NOT_ABSOLUTE","09-relative-project-not-approved");
        Invalid("",R41V5AssetPaths.ExactV5AssetPath,
                "UNITY_ASSETS_DIRECTORY_NOT_ABSOLUTE","10-empty-project-not-approved");
        Invalid(Path.GetTempPath(),R41V5AssetPaths.ExactV5AssetPath,
                "UNITY_ASSETS_ROOT_NAME_MISMATCH","11-wrong-project-root-name-rejected");
        Invalid(assets,"Assets/Other/R41_Turret_v5.fbx",
                "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","12-foreign-source-sibling-rejected");
        Invalid(assets,"Assets/../OUTSIDE/R41_Turret_v5.fbx",
                "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","13-parent-traversal-rejected");
        Invalid(assets,@"Assets\B01_31\W30R41V5Adapter\Incoming\R41_Turret_v5.fbx",
                "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","14-Windows-backslash-alias-rejected");
        Invalid(assets,"Assets//B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx",
                "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","15-double-separator-rejected");
        Invalid(assets,"Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v4.fbx",
                "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","16-old-version-rejected");
        Invalid(assets,"Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.glb",
                "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","17-unexpected-glb-rejected");
        Invalid(assets,"Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx.meta",
                "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","18-unexpected-meta-rejected");
        Invalid(assets,"assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx",
                "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","19-different-case-root-alias-rejected");
        Invalid(assets,"Assets/B01_31/W30R41V5Adapter/Incoming/./R41_Turret_v5.fbx",
                "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","20-dot-component-rejected");
        Invalid(assets,"Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx ",
                "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","21-trailing-space-rejected");
        Invalid(assets,"https://host/Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx",
                "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","22-source-url-rejected");
        Invalid(assets,null,"FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","23-null-asset-path-rejected");
        Invalid(assets,"","FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","24-empty-asset-path-rejected");
        Invalid(assets,"Assets/B01_31/W30R41V5Adapter/Generated/R41_Turret_v5.fbx",
                "FOREIGN_OR_UNAPPROVED_V5_ASSET_PATH","25-generated-adapter-cannot-be-used-as-original");
        Check(R41V5MuzzleGeometry.TryPose(0,0,R41V5MuzzleSide.Left,out var left) &&
              R41V5MuzzleGeometry.TryPose(0,0,R41V5MuzzleSide.Right,out var right),
              "26-original-W30-kinematic-source-linkage-intact");
        Check(R41V5MuzzleGeometry.ValidatePosePair(in left,in right),
              "27-original-W30-geometry-both-muzzles-still-physical-12cm");
        Check(R41V5MuzzleGeometry.GridMeters==0.55,
              "28-existing-055m-grid-must-remain-unchanged");
        Check(count==28,"29-exact-real-original-source-assertions");
        Console.WriteLine("B01_31_W31_ORIGINAL_PATH_SAFETY_29_CASES_PASS__UNITY_NATIVE_G1_NOT_RUN");
    }
}

// PASS13 independent executable source QA. Compiles the original, unchanged
// #310 Assets/MapEditor/MapDraft.cs, pinned by workflow Git blob SHA.
// System.Text.Json is ONLY a shim for UnityEngine.JsonUtility, not actual Unity.
using System;
using System.IO;
using System.Linq;
using RoboRougelike.MapEditor;

internal static class Program
{
    static int ok, fail;
    const string MissingGround = "Dodaj podłoże pod punktem startowym.";
    static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    static void Case(string name, Action body)
    {
        try { body(); ok++; Console.WriteLine("SOURCE_CSHARP_SHIM_PASS | "+name); }
        catch(Exception e) { fail++; Console.Error.WriteLine("SOURCE_CSHARP_SHIM_FAIL | "+name+" | "+e); }
    }
    static MapDraft LoadFile(string name, int count, bool global)
    {
        string path=Path.Combine("Research","B01-25",name.StartsWith("LD") && name.Contains("NEG") ? "P12":"P11",name+".json");
        var draft=new MapDraft();
        string json=File.ReadAllText(path);
        Need(draft.TryLoad(json,out string error),"TryLoad rejected "+name+": "+error);
        Need(draft.Items.Count==count,"Count "+name+" expected="+count+" actual="+draft.Items.Count);
        Need(draft.GlobalFloor==global,"GlobalFloor mismatch "+name);
        return draft;
    }
    static void RotatedScaledSpawn(int yaw,int missingX,int missingZ)
    {
        var draft=new MapDraft();
        Need(draft.Place(MapItemKind.Ground,0,0,0),"initial Ground");
        Need(draft.Place(MapItemKind.Spawn,0,0,yaw,2,1,1),"scaled Spawn");
        Need(draft.Place(MapItemKind.Finish,10,10,0),"Finish");
        Need(draft.TestError()==MissingGround,"missing support not rejected");
        var before=draft.ToJson();
        Need(draft.Place(MapItemKind.Ground,missingX,missingZ,0),"missing Ground");
        Need(draft.TestError()==null,"complete support not accepted");
        var after=draft.ToJson();
        Need(draft.Undo() && draft.ToJson()==before && draft.TestError()==MissingGround,
            "Undo not exact / preflight");
        Need(draft.Redo() && draft.ToJson()==after && draft.TestError()==null,
            "Redo not exact / preflight");
        var roundtrip=new MapDraft();
        Need(roundtrip.TryLoad(after,out string error),"v2 TryLoad "+error);
        Need(roundtrip.Spawn.rotation==yaw && roundtrip.Spawn.scaleX==2,
            "rotation/scale lost");
        Need(roundtrip.TestError()==null,"roundtrip support lost");
    }
    static int OriginalDataRoute(MapDraft d, (int x,int z)[] cells, bool allowGeometryObstacles)
    {
        Need(cells.Length>=2,"no route cells");
        Need(d.Spawn.x==cells[0].x && d.Spawn.z==cells[0].z,"route origin not Spawn");
        Need(d.Finish.x==cells[cells.Length-1].x && d.Finish.z==cells[cells.Length-1].z,
            "route end not Finish");
        var ground=new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>(
            d.Items.Where(p=>p.kind==MapItemKind.Ground).SelectMany(MapDraft.Footprint));
        var obstacles=new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>(
            d.Items.Where(p=>p.kind!=MapItemKind.Ground &&
                             p.kind!=MapItemKind.Spawn && p.kind!=MapItemKind.Finish)
                   .SelectMany(MapDraft.Footprint));
        int encountered=0;
        for(int i=0;i<cells.Length;i++)
        {
            var (x,z)=cells[i];
            Need(Math.Abs(x)<=MapDraft.MaxCoordinate && Math.Abs(z)<=MapDraft.MaxCoordinate,
                "route cell exceeds source boundary");
            var p=new UnityEngine.Vector2Int(x,z);
            Need(d.GlobalFloor || ground.Contains(p),"route cell has no original Ground");
            if(obstacles.Contains(p)) encountered++;
            if(i>0)
                Need(Math.Abs(x-cells[i-1].x)+Math.Abs(z-cells[i-1].z)==1,
                    "non-contiguous 4-neighbour route witness");
        }
        Need(allowGeometryObstacles || encountered==0,"supposed clear corridor meets object footprint");
        return encountered;
    }

    // D06 exact original-source repro. 2000 individual Ground tiles are retained
    // under GlobalFloor and count toward MaxItems even though MapWorld skips
    // rendering those per-cell Ground objects. No MapDraft C# modified.
    static MapDraft MakeD06FullGroundDraft()
    {
        // Load the exact published original Git JSON bytes, not a generated mirror.
        string file=File.ReadAllText("Research/B01-21/P14/CV05_D06_GlobalFloor_2000Ground.json");
        var draft=new MapDraft();
        Need(draft.TryLoad(file,out var error),"D06 exact fixture TryLoad rejected: "+error);
        Need(draft.Items.Count==MapDraft.MaxItems,"D06 actual fixture count");
        Need(draft.GlobalFloor,"D06 published fixture must have GlobalFloor ON");
        Need(draft.Items.All(p=>p.kind==MapItemKind.Ground),"D06 original fixture has unexpected item kind");
        return draft;
    }
    static int knownDefects;
    static void KnownDefect(string name, Action checkFailingContract)
    {
        try
        {
            checkFailingContract();
            fail++;
            Console.Error.WriteLine("KNOWN_DEFECT_UNEXPECTED_GREEN_REBASE_REQUIRED | "+name);
        }
        catch (InvalidOperationException e)
        {
            knownDefects++;
            Console.WriteLine("KNOWN_PRODUCT_DEFECT_RED_CONFIRMED | "+name+" | "+e.Message);
        }
        catch (Exception e)
        {
            fail++;
            Console.Error.WriteLine("KNOWN_DEFECT_FIXTURE_ERROR_NOT_A_PRODUCT_REPRO | "+name+" | "+e);
        }
    }

    static int Main()
    {
        Console.WriteLine("SOURCE_KIND=EXACT_ORIGINAL_CODEX_MAPDRAFT_CS");
        Console.WriteLine("SOURCE_SHA=231005c6d0351945a8ee6684f3856af118f0e94f");
        Console.WriteLine("SOURCE_GIT_BLOB=b5f8f395f93972c0ede000701dbbada8fcfcdfdb");
        Console.WriteLine("ENGINE=DOTNET8_WITH_JSONUTILITY_VECTOR2INT_SHIMS__NOT_UNITY");
        foreach(var (yaw,x,z) in new[]{(0,1,0),(1,0,-1),(2,-1,0),(3,0,1)})
        {
            int y=yaw, dx=x, dz=z;
            Case("A_ROTATED_SPAWN_YAW_"+(y*90),()=>RotatedScaledSpawn(y,dx,dz));
        }
        Case("B_LD01_ORIGINAL_POSITIVE",()=>
        {
            var d=LoadFile("LD01",59,false);
            Need(d.Spawn.x==0 && d.Spawn.z==1,"Spawn LD01");
            Need(d.Finish.x==12 && d.Finish.z==1,"Finish LD01");
            Need(d.TestError()==null,"TestError LD01");
        });
        Case("B_LD02_ORIGINAL_POSITIVE",()=>
        {
            var d=LoadFile("LD02",10,true);
            Need(d.Spawn.x==0 && d.Spawn.z==5,"Spawn LD02");
            Need(d.Finish.x==11 && d.Finish.z==5,"Finish LD02");
            Need(d.TestError()==null,"TestError LD02");
        });
        Case("B_LD03_ORIGINAL_POSITIVE",()=>
        {
            var d=LoadFile("LD03",45,false);
            Need(d.Spawn.x==0 && d.Spawn.z==2,"Spawn LD03");
            Need(d.Finish.x==8 && d.Finish.z==2,"Finish LD03");
            Need(d.TestError()==null,"TestError LD03");
        });
        Case("B_LD01_NEG_PARTIAL_GROUND_PRECHECK_REJECT",()=>
        {
            var d=LoadFile("LD01_NEG",58,false);
            Need(d.TestError()==MissingGround,"Expected underfloor start rejection");
        });
        Case("B_LD02_NEG_DUPLICATE_FINISH_TRANSACTIONAL",()=>
        {
            var original=LoadFile("LD02",10,true);
            string prior=original.ToJson();
            string bad=File.ReadAllText("Research/B01-25/P12/LD02_NEG.json");
            Need(!original.TryLoad(bad,out string error),"duplicate Finish accepted");
            Need(error=="Mapa może mieć tylko jeden start i jedną metę.","wrong error: "+error);
            Need(prior==original.ToJson(),"invalid import mutated existing map");
        });
        Case("B_LD03_NEG_CUT_DETOUR_STILL_SANDBOX_VALID",()=>
        {
            var d=LoadFile("LD03_NEG",43,false);
            Need(d.TestError()==null,"Editor TestError should only check Spawn support");
            Need(d.Spawn.x==0 && d.Spawn.z==2 && d.Finish.x==8 && d.Finish.z==2,
                "marker drift");
            // No route pass is asserted. Deleted Ground tiles can break design
            // even when the editor's basic sandbox preflight succeeds.
        });
        Case("C_LD01_DETOUR_SPAWN_TO_FINISH_SOURCE_GEOMETRY",()=>
        {
            var d=LoadFile("LD01",59,false);
            var points=new System.Collections.Generic.List<(int x,int z)> { (0,1),(0,2),(0,3) };
            for(int x=1;x<=12;x++) points.Add((x,3));
            points.Add((12,2));points.Add((12,1));
            Need(points.Count==17,"LD01 16 transitions expected");
            Need(OriginalDataRoute(d,points.ToArray(),false)==0,"unexpected obstacles");
        });
        Case("C_LD02_GLOBAL_FLOOR_DIRECT_SPATIAL_LANE",()=>
        {
            var d=LoadFile("LD02",10,true);
            var points=Enumerable.Range(0,12).Select(x=>(x,5)).ToArray();
            Need(OriginalDataRoute(d,points,false)==0,"LD02 centerline obstructed");
        });
        Case("C_LD03_TWO_BRANCH_HAZARD_VS_CLEAR_CORRIDOR",()=>
        {
            var d=LoadFile("LD03",45,false);
            var risky=Enumerable.Range(0,9).Select(x=>(x,2)).ToArray();
            var safe=new System.Collections.Generic.List<(int x,int z)> { (0,2),(0,3),(0,4),(0,5) };
            for(int x=1;x<=8;x++) safe.Add((x,5));
            safe.Add((8,4));safe.Add((8,3));safe.Add((8,2));
            Need(risky.Length==9 && safe.Count==15,"8 vs 14 transitions expected");
            Need(OriginalDataRoute(d,risky,true)==2,"LD03 risky branch must cross barrier+ramp cells");
            Need(OriginalDataRoute(d,safe.ToArray(),false)==0,
                "LD03 clear branch does not avoid obstacle footprints");
            // This is a 4-neighbour, authoring-level witness using original
            // C# MapDraft.Items and Footprint; NOT Rigidbody clearance or a route success.
        });
        KnownDefect("D06_GLOBALFLOOR_HIDDEN_2000_GROUND_PREVENTS_SPAWN",() =>
        {
            var d=MakeD06FullGroundDraft();
            Need(d.GlobalFloor,"GlobalFloor unexpectedly OFF");
            Need(d.Items.Count==2000,"different cap state");
            Need(!d.Remove(-25,-20),"hidden ground unexpectedly editable");
            Need(d.Place(MapItemKind.Spawn,30,30,0),
                "D06: valid visible free-cell Spawn rejected because 2000 hidden Ground consume MaxItems");
        });
        Case("D06_WORKAROUND_TWO_SLOTS_FOR_SPAWN_AND_FINISH",() =>
        {
            var d=MakeD06FullGroundDraft();
            Need(d.SetGlobalFloor(false),"toggle OFF for cleanup");
            Need(d.Remove(-25,-20),"remove retained Ground #1");
            Need(d.Remove(-25,-19),"remove retained Ground #2");
            Need(d.Items.Count==1998,"expected 2 spare entries");
            Need(d.SetGlobalFloor(true),"toggle GlobalFloor ON again");
            Need(d.Place(MapItemKind.Spawn,30,30,0),"start after removing old Ground");
            Need(d.Place(MapItemKind.Finish,31,31,0),"finish after freeing second slot");
            Need(d.Items.Count==2000 && d.TestError()==null,
                "full-floor Draft should be test-drive eligible after manual workaround");
        });
        Case("P15_LD02_ORIGINAL_CSHARP_LOAD_AND_PHYSICAL_SOURCE_CORNER_SEALS",LD02ArenaP15Tests.Run);
        Case("P15_COVER_CHOICE_ORIGINAL_CSHARP_ROUTES_SOURCE_LOS",LD02CoverChoiceP15Tests.Run);
        Case("P15_B0125_OWNER_CHANGED_ORIGINAL_CSHARP_CORNER_FIX",LD02OwnerFixedP15Tests.Run);
        Console.WriteLine("KNOWN_PRODUCT_DEFECTS_REPRODUCED="+knownDefects);
        Console.WriteLine("ORIGINAL_CSHARP_SHIM_TESTS="+ok+" PASS / "+fail+" FAIL");
        Console.WriteLine("UNITY_EDITOR_NOT_RUN; PHYSX_NOT_RUN; HUMAN_NOT_RUN; G1_NOT_RUN");
        return fail==0 ? 0 : 1;
    }
}

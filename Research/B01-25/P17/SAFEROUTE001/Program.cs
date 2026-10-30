using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using B01.LevelChallenges.SafeRoute;

public static class OriginalSafeRouteTests
{
    private static readonly List<string> Passed=new List<string>();
    private static int negatives;
    private static LevelGrid Map()=>SafeRouteExample.Create();
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    private static void Test(string name,Action act)
    {
        try{act();Passed.Add(name);Console.WriteLine("PASS "+name);}
        catch(Exception error){Console.Error.WriteLine("FAIL "+name+" "+error);throw;}
    }
    private static void Reject(string name,Action act)
    {
        Test("NEGATIVE_"+name,()=>{
            bool denied=false;
            try{act();}catch(ArgumentException){denied=true;}
            Check(denied,"Malformed source unexpectedly accepted");
        });negatives++;
    }
    private static SafeRoutePlan Plan(int r,bool gate,bool hazard)=>
        SafeRoutePlanner.Find(Map(),r,new RouteConditions(gate,hazard));
    private static void VerifyPhysicalGridWitness(LevelGrid map,SafeRoutePlan path,
         int radius,RouteConditions state)
    {
        Check(path.Reachable,"missing safe route");
        Check(path.Cells.Length>=2,"bad endpoints");
        Check(path.Cells[0].Equals(map.Start),"wrong spawn");
        Check(path.Cells[path.Cells.Length-1].Equals(map.Exit),"wrong exit");
        Check(path.Cells.Length<=map.Width*map.Height,"loops");
        var used=new HashSet<GridCell>();
        for(int i=0;i<path.Cells.Length;i++)
        {
            var p=path.Cells[i];
            Check(SafeRoutePlanner.IsClear(map,p,radius,state),"unsafe footprint "+p);
            Check(used.Add(p),"repeated BFS cell");
            if(i>0)
            {
                var prev=path.Cells[i-1];
                Check(Math.Abs(prev.X-p.X)+Math.Abs(prev.Z-p.Z)==1,"noncontiguous route");
            }
        }
    }
    private static void Run(string[] args)
    {
        Test("MAP_ORIGINAL_DIMENSIONS_15X11",()=>{var m=Map();Check(m.Width==15&&m.Height==11,"size");});
        Test("MAP_START_AND_EXIT_ARE_DISTINCT",()=>Check(!Map().Start.Equals(Map().Exit),"positions"));
        Test("MAP_ORIGINAL_GATES_AND_HAZARDS_PRESENT",()=>{
            var m=Map();Check(m.At(7,4)=='G'&&m.At(10,4)=='H',"tiles");
        });
        Test("BOTH_FOOTPRINTS_HAVE_ALWAYS_SAFE_BYPASS",()=>{
            var a=SafeRoutePlanner.Audit(Map());
            Check(a.Cases.Length==8&&a.AllReachable,"at least one unsafe gate/hazard scenario");
        });
        for(int radius=0;radius<=1;radius++)
        for(int gate=0;gate<=1;gate++)
        for(int hazard=0;hazard<=1;hazard++)
        {
            int r=radius;bool g=gate==1,h=hazard==1;
            Test("SAFE_WITNESS_RADIUS_"+r+"_GATE_"+g+"_HAZARD_"+h,()=>{
                var map=Map();var state=new RouteConditions(g,h);
                VerifyPhysicalGridWitness(map,SafeRoutePlanner.Find(map,r,state),r,state);
            });
        }
        Test("CLOSED_GATE_NEVER_IN_ROUTE_LIGHT",()=>{
            var p=Plan(0,false,false);
            foreach(var cell in p.Cells)Check(Map().At(cell.X,cell.Z)!='G',"closed gate path");
        });
        Test("CLOSED_GATE_NEVER_IN_ROUTE_HEAVY_FOOTPRINT",()=>{
            var state=new RouteConditions(false,false);var map=Map();
            VerifyPhysicalGridWitness(map,SafeRoutePlanner.Find(map,1,state),1,state);
        });
        Test("ACTIVE_HAZARD_NEVER_IN_ROUTE_LIGHT",()=>{
            var map=Map();var p=Plan(0,true,true);
            foreach(var c in p.Cells)Check(map.At(c.X,c.Z)!='H',"active hazard path");
        });
        Test("ACTIVE_HAZARD_NEVER_WITHIN_HEAVY_FOOTPRINT",()=>{
            var m=Map();var c=new RouteConditions(true,true);
            VerifyPhysicalGridWitness(m,SafeRoutePlanner.Find(m,1,c),1,c);
        });
        Test("LIGHT_CAN_USE_OPEN_GATE_SHORTCUT",()=>{
            var m=Map();var p=Plan(0,true,false);
            Check(Array.Exists(p.Cells,c=>m.At(c.X,c.Z)=='G'),"light no gate shortcut");
        });
        Test("HEAVY_CAN_USE_OPEN_GATE_SHORTCUT",()=>{
            var m=Map();var p=Plan(1,true,false);
            Check(Array.Exists(p.Cells,c=>m.At(c.X,c.Z)=='G'),"heavy no gate shortcut");
        });
        Test("GATE_CLOSED_DETOUR_LONGER_THAN_OPEN_LIGHT",()=>{
            Check(Plan(0,false,false).Cells.Length>Plan(0,true,false).Cells.Length,"no shortcut");
        });
        Test("GATE_CLOSED_DETOUR_LONGER_THAN_OPEN_HEAVY",()=>{
            Check(Plan(1,false,false).Cells.Length>Plan(1,true,false).Cells.Length,"no heavy benefit");
        });
        Test("HEAVY_NEVER_LESS_CONSERVATIVE_THAN_LIGHT",()=>{
            for(int g=0;g<2;g++)for(int h=0;h<2;h++)
                Check(Plan(1,g==1,h==1).Cells.Length>=Plan(0,g==1,h==1).Cells.Length,"heavy cheaper");
        });
        Test("ALL_8_AUDIT_CASES_STABLE_ORDER",()=>{
            var audit=SafeRoutePlanner.Audit(Map());
            Check(audit.Cases.Length==8&&audit.Cases[0].FootprintRadius==0&&
                audit.Cases[7].FootprintRadius==1,"case metadata");
        });
        Test("PATH_DETERMINISM_100_REPLAYS",()=>{
            var m=Map();var s=new RouteConditions(false,true);
            var a=SafeRoutePlanner.Find(m,1,s);
            for(int i=0;i<100;i++)
            {
                var b=SafeRoutePlanner.Find(m,1,s);
                Check(a.Cells.Length==b.Cells.Length,"different path length");
                for(int j=0;j<a.Cells.Length;j++)Check(a.Cells[j].Equals(b.Cells[j]),"unstable tie-break");
            }
        });
        Test("PATH_HAS_NO_REPEATED_NODES",()=>{
            var p=Plan(1,true,true);
            Check(new HashSet<GridCell>(p.Cells).Count==p.Cells.Length,"route loop");
        });
        Test("NO_UNSAFE_OUT_OF_BOUNDS_CLEARANCE",()=>{
            var m=Map();
            Check(!SafeRoutePlanner.IsClear(m,new GridCell(0,0),1,new RouteConditions(true,false)),"edge");
        });
        Test("GATE_IS_BLOCKED_IF_CLOSED_LIGHT",()=>{
            var m=Map();
            Check(!SafeRoutePlanner.IsClear(m,new GridCell(7,4),0,new RouteConditions(false,false)),"gate");
        });
        Test("GATE_IS_CLEAR_IF_OPEN_LIGHT",()=>{
            var m=Map();
            Check(SafeRoutePlanner.IsClear(m,new GridCell(7,4),0,new RouteConditions(true,false)),"gate");
        });
        Test("HAZARD_IS_UNSAFE_IF_ACTIVE",()=>{
            var m=Map();
            Check(!SafeRoutePlanner.IsClear(m,new GridCell(10,4),0,new RouteConditions(true,true)),"hazard");
        });
        Test("HAZARD_IS_CLEAR_IF_INACTIVE",()=>{
            var m=Map();
            Check(SafeRoutePlanner.IsClear(m,new GridCell(10,4),0,new RouteConditions(true,false)),"inactive hazard");
        });
        Test("IMPOSSIBLE_MAP_FAILS_CLOSED_NO_ROUTE",()=>{
            var m=LevelGrid.Parse("#####","#S#E#","#####");
            var p=SafeRoutePlanner.Find(m,0,new RouteConditions(true,false));
            Check(!p.Reachable&&p.Cells.Length==0,"impossible level accepted");
        });
        Test("HEAVY_NO_ROUTE_WHEN_NARROW_ONE_CELL_TUNNEL",()=>{
            var m=LevelGrid.Parse("#######","#S...E#","#######");
            var p=SafeRoutePlanner.Find(m,1,new RouteConditions(true,false));
            Check(!p.Reachable,"heavy cannot pass");
            Check(SafeRoutePlanner.Find(m,0,new RouteConditions(true,false)).Reachable,"light should pass");
        });
        Test("AUDIT_REPORTS_SOFTLOCK_WHEN_BOUNDARY_BLOCKED",()=>{
            var m=LevelGrid.Parse("#######","#S#..E#","#.#.###","#.#.###","#...###","#######");
            Check(!SafeRoutePlanner.Audit(m).AllReachable,"softlock not detected");
        });
        Test("MUTATING_RESULT_ROUTE_DOES_NOT_CHANGE_FUTURE_RESULTS",()=>{
            var m=Map();var c=new RouteConditions(false,true);
            var a=SafeRoutePlanner.Find(m,0,c);
            a.Cells[0]=new GridCell(-100,99);
            var b=SafeRoutePlanner.Find(m,0,c);
            Check(b.Cells[0].Equals(m.Start),"shared mutable plan storage");
        });
        Test("GRID_REJECTS_INVALID_OUTSIDE_AT",()=>{
            var m=Map();bool fail=false;
            try{m.At(-1,1);}catch(ArgumentOutOfRangeException){fail=true;}
            Check(fail,"unsafe index");
        });
        Test("PLAN_SCANS_AT_MOST_GRID_CELLS",()=>{
            var m=Map();var p=SafeRoutePlanner.Find(m,1,new RouteConditions(false,true));
            Check(p.NodesExplored<=m.Width*m.Height,"budget overrun");
        });
        Test("ZERO_WIDTH_UNSAFE_ROUTE_IS_REJECTED",()=>{
            var m=LevelGrid.Parse("###","#S#","#E#");
            Check(!SafeRoutePlanner.Find(m,1,new RouteConditions(false,false)).Reachable,"wide passed");
        });
        Reject("NULL_ROWS",()=>LevelGrid.Parse((string[])null!));
        Reject("EMPTY_MAP",()=>LevelGrid.Parse());
        Reject("UNEVEN_ROW_WIDTH",()=>LevelGrid.Parse("#####","#S.E#","###"));
        Reject("UNKNOWN_CELL_SYMBOL",()=>LevelGrid.Parse("#####","#S?E#","#####"));
        Reject("DUPLICATE_SPAWN",()=>LevelGrid.Parse("#####","#SSE#","#####"));
        Reject("DUPLICATE_EXIT",()=>LevelGrid.Parse("#####","#SEE#","#####"));
        Reject("MISSING_SPAWN",()=>LevelGrid.Parse("#####","#..E#","#####"));
        Reject("MISSING_EXIT",()=>LevelGrid.Parse("#####","#S..#","#####"));
        Reject("TWO_ROW_MAP",()=>LevelGrid.Parse("#####","#S.E#"));
        Reject("TOO_WIDE_ROW",()=>LevelGrid.Parse(new string('#',65),new string('S',65),new string('E',65)));
        Reject("NULL_CONTROLLER_MAP",()=>SafeRoutePlanner.Find(null!,0,new RouteConditions(false,false)));
        Reject("NEGATIVE_RADIUS",()=>SafeRoutePlanner.Find(Map(),-1,new RouteConditions(false,false)));
        Reject("UNSUPPORTED_LARGE_RADIUS",()=>SafeRoutePlanner.Find(Map(),3,new RouteConditions(false,false)));
        Reject("NULL_AUDIT_MAP",()=>SafeRoutePlanner.Audit(null!));
        Check(args.Length==1,"One results path required");
        string file=Path.GetFullPath(args[0]);Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file,JsonSerializer.Serialize(new{package="B01-25-SAFEROUTE001/R1",
            sourcePassed=Passed.Count,negativeTests=negatives,nativeUnity="NOT_RUN",
            names=Passed},new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine("SAFEROUTE_ORIGINAL_CSHARP9_PASS "+Passed.Count+"/"+Passed.Count+
            " NEGATIVE="+negatives+" UNITY_NATIVE_NOT_RUN");
    }
    public static void Main(string[] args)=>Run(args);
}

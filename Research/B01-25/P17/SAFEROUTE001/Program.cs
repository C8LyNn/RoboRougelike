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
    // From a real live origin rather than the map design spawn.
    private static void VerifyPhysicalGridWitnessFrom(LevelGrid map,GridCell from,
        SafeRoutePlan path,int radius,RouteConditions state)
    {
        Check(path.Reachable&&path.Cells.Length>=1,"missing live route");
        Check(path.Cells[0].Equals(from),"live origin mismatch");
        Check(path.Cells[path.Cells.Length-1].Equals(map.Exit),"live exit mismatch");
        var visited=new HashSet<GridCell>();
        for(int i=0;i<path.Cells.Length;i++)
        {
            Check(SafeRoutePlanner.IsClear(map,path.Cells[i],radius,state),"unsafe live cell");
            Check(visited.Add(path.Cells[i]),"live route repeats a cell");
            if(i>0)
            {
                var a=path.Cells[i-1];var b=path.Cells[i];
                Check(Math.Abs(a.X-b.X)+Math.Abs(a.Z-b.Z)==1,"nonadjacent live route");
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
        // W24 R2 independent tests of genuine live-origin route planning:
        Test("R2_ROUTE_FROM_LIVE_PLAYER_NOT_OLD_SPAWN",()=>{
            var m=Map();var cond=new RouteConditions(false,true);
            var original=SafeRoutePlanner.Find(m,0,cond);
            var location=original.Cells[original.Cells.Length/2];
            var live=SafeRoutePlanner.FindFrom(m,location,0,cond);
            Check(live.Reachable&&live.Cells[0].Equals(location),"stale spawn instead of player");
            Check(live.Cells[live.Cells.Length-1].Equals(m.Exit),"wrong destination");
            Check(live.Cells.Length<original.Cells.Length,"did not shorten route");
        });
        Test("R2_ROUTE_FROM_EXIT_IS_SINGLE_CELL",()=>{
            var m=Map();var live=SafeRoutePlanner.FindFrom(m,m.Exit,0,new RouteConditions(false,true));
            Check(live.Reachable&&live.Cells.Length==1&&live.Cells[0].Equals(m.Exit),"exit");
        });
        Test("R2_LIVE_ORIGIN_TELEPORT_OUT_OF_BOUNDS_FAIL_CLOSED",()=>{
            var m=Map();var route=SafeRoutePlanner.FindFrom(m,new GridCell(-99,7),0,new RouteConditions(true,false));
            Check(!route.Reachable&&route.Cells.Length==0&&route.NodesExplored==0,"outside accepted");
        });
        Test("R2_LIVE_ORIGIN_IN_CLOSED_GATE_FAILS_CLOSED",()=>{
            var m=Map();var route=SafeRoutePlanner.FindFrom(m,new GridCell(7,4),0,new RouteConditions(false,false));
            Check(!route.Reachable,"inside locked gate accepted");
        });
        Test("R2_LIVE_ORIGIN_INSIDE_ACTIVE_HAZARD_FAILS_CLOSED",()=>{
            var m=Map();var route=SafeRoutePlanner.FindFrom(m,new GridCell(10,4),0,new RouteConditions(true,true));
            Check(!route.Reachable,"active hazard origin accepted");
        });
        Test("R2_OPEN_GATE_ORIGIN_HAS_VALID_PATH",()=>{
            var m=Map();var cond=new RouteConditions(true,false);
            var route=SafeRoutePlanner.FindFrom(m,new GridCell(7,4),0,cond);
            VerifyPhysicalGridWitnessFrom(m,new GridCell(7,4),route,0,cond);
        });
        Test("R2_EIGHT_STATES_REPLAN_AFTER_FIRST_STEP_LIGHT_HEAVY",()=>{
            var m=Map();
            for(int r=0;r<=1;r++)for(int g=0;g<=1;g++)for(int h=0;h<=1;h++)
            {
                var cond=new RouteConditions(g==1,h==1);
                var first=SafeRoutePlanner.Find(m,r,cond);
                Check(first.Reachable&&first.Cells.Length>2,"prerequisite source-safe route");
                var from=first.Cells[1];
                var live=SafeRoutePlanner.FindFrom(m,from,r,cond);
                VerifyPhysicalGridWitnessFrom(m,from,live,r,cond);
                Check(live.Cells.Length<=first.Cells.Length,"route longer from next step");
            }
        });
        Test("R2_LEGACY_FIND_REMAINS_BYTEWISE_EQUIVALENT_TO_FIND_FROM_SPAWN",()=>{
            var m=Map();
            for(int r=0;r<=1;r++)for(int g=0;g<=1;g++)for(int h=0;h<=1;h++)
            {
                var cond=new RouteConditions(g==1,h==1);
                var legacy=SafeRoutePlanner.Find(m,r,cond);
                var current=SafeRoutePlanner.FindFrom(m,m.Start,r,cond);
                Check(legacy.Cells.Length==current.Cells.Length,"legacy path changed");
                for(int k=0;k<legacy.Cells.Length;k++)
                    Check(legacy.Cells[k].Equals(current.Cells[k]),"legacy route diverged");
            }
        });
        Test("R2_FROM_CELL_CANNOT_ALIAS_OTHER_CALLS",()=>{
            var m=Map();var cond=new RouteConditions(false,true);
            var p=SafeRoutePlanner.Find(m,0,cond);
            var from=p.Cells[1];
            var a=SafeRoutePlanner.FindFrom(m,from,0,cond);
            a.Cells[0]=new GridCell(-77,-77);
            var b=SafeRoutePlanner.FindFrom(m,from,0,cond);
            Check(b.Cells[0].Equals(from),"shared mutated route across source calls");
        });
        Test("R2_HEAVY_NARROW_CURRENT_ORIGIN_FAILS_CLOSED",()=>{
            var m=LevelGrid.Parse("#######","#S...E#","#######");
            var route=SafeRoutePlanner.FindFrom(m,new GridCell(3,1),1,new RouteConditions(true,false));
            Check(!route.Reachable,"heavy under-sized hallway accepted");
        });
        Reject("R2_FIND_FROM_NULL_GRID",()=>SafeRoutePlanner.FindFrom(null!,new GridCell(0,0),0,new RouteConditions(false,false)));
        Reject("R2_FIND_FROM_NEGATIVE_RADIUS",()=>SafeRoutePlanner.FindFrom(Map(),Map().Start,-1,new RouteConditions(false,false)));
        Reject("R2_FIND_FROM_OVERSIZE_RADIUS",()=>SafeRoutePlanner.FindFrom(Map(),Map().Start,3,new RouteConditions(false,false)));

        Test("R3_REAL_PHYSICAL_CRATE_STAGING_SAFE_ALL_8",()=>{
            var a=PhysicalObstacleRouter.AuditAllEight(Map(),new[]{new GridCell(3,8)});
            Check(a.AllReachable&&a.Cases.Length==8,"staging unexpectedly unsafe");
        });
        Test("R3_BLOCK_HEAVY_LOWER_BYPASS_REJECT_ALL8",()=>{
            var a=PhysicalObstacleRouter.AuditAllEight(Map(),new[]{new GridCell(7,8)});
            Check(!a.AllReachable&&!a.Cases[5].Reachable,"heavy softlock missed");
        });
        Test("R3_LIGHT_STILL_NAVIGATES_DANGEROUS_HEAVY_BLOCK",()=>{
            var a=PhysicalObstacleRouter.AuditAllEight(Map(),new[]{new GridCell(7,8)});
            Check(a.Cases[0].Reachable&&a.Cases[1].Reachable,"light should not be softlocked");
        });
        Test("R3_ALTERNATE_CRATE_MOVE_SAFE_ALL_8",()=>{
            Check(PhysicalObstacleRouter.AuditAllEight(Map(),new[]{new GridCell(4,4)}).AllReachable,"4 4");
        });
        Test("R3_REAL_CRATE_NOT_ON_LIGHT_ROUTE",()=>{
            var m=Map();var block=new GridCell(3,8);
            var p=PhysicalObstacleRouter.FindFrom(m,m.Start,0,new RouteConditions(false,true),new[]{block});
            Check(p.Reachable,"light should have route");
            foreach(var cell in p.Cells)Check(!cell.Equals(block),"route crosses crate");
        });
        Test("R3_HEAVY_3X3_FOOTPRINT_NEVER_OVERLAPS_REAL_CRATE",()=>{
            var m=Map();var block=new GridCell(3,8);
            var p=PhysicalObstacleRouter.FindFrom(m,m.Start,1,new RouteConditions(false,true),new[]{block});
            Check(p.Reachable,"heavy blocked at safe stage");
            foreach(var cell in p.Cells)
                Check(Math.Abs(cell.X-block.X)>1||Math.Abs(cell.Z-block.Z)>1,
                    "real crate overlaps heavy swept footprint");
        });
        Test("R3_REAL_BLOCKER_ON_START_IS_NO_ROUTE",()=>{
            var m=Map();var p=PhysicalObstacleRouter.FindFrom(m,m.Start,0,
                new RouteConditions(true,false),new[]{m.Start});
            Check(!p.Reachable&&p.Cells.Length==0,"occupied start false positive");
        });
        Test("R3_REAL_BLOCKER_ON_EXIT_IS_NO_ROUTE",()=>{
            var m=Map();var p=PhysicalObstacleRouter.FindFrom(m,m.Start,0,
                new RouteConditions(true,false),new[]{m.Exit});
            Check(!p.Reachable&&p.Cells.Length==0,"occupied exit false positive");
        });
        Test("R3_REAL_BLOCKED_CELLS_FROM_LIVE_ORIGIN",()=>{
            var m=Map();var c=new RouteConditions(false,true);
            var old=PhysicalObstacleRouter.FindFrom(m,m.Start,0,c,new[]{new GridCell(3,8)});
            Check(old.Reachable&&old.Cells.Length>2,"baseline");
            var next=old.Cells[1];
            var live=PhysicalObstacleRouter.FindFrom(m,next,0,c,new[]{new GridCell(3,8)});
            VerifyPhysicalGridWitnessFrom(m,next,live,0,c);
            Check(live.Cells.Length<=old.Cells.Length,"stale origin");
        });
        Test("R3_EMPTY_PHYSICAL_INPUT_LEGACY_R2_PARITY_EIGHT_CASES",()=>{
            var m=Map();for(int r=0;r<=1;r++)for(int g=0;g<=1;g++)
            for(int h=0;h<=1;h++)
            {
                var state=new RouteConditions(g==1,h==1);
                var a=SafeRoutePlanner.Find(m,r,state);
                var b=PhysicalObstacleRouter.FindFrom(m,m.Start,r,state,Array.Empty<GridCell>());
                Check(a.Cells.Length==b.Cells.Length,"R2 ABI changed");
                for(int j=0;j<a.Cells.Length;j++)
                    Check(a.Cells[j].Equals(b.Cells[j]),"BFS order diverged");
            }
        });
        Test("R3_REAL_OBSTACLE_DUPLICATE_SAMPLES_ARE_IDEMPOTENT",()=>{
            var m=Map();var c=new RouteConditions(false,true);
            var a=PhysicalObstacleRouter.FindFrom(m,m.Start,0,c,new[]{new GridCell(3,8)});
            var b=PhysicalObstacleRouter.FindFrom(m,m.Start,0,c,
                new[]{new GridCell(3,8),new GridCell(3,8)});
            Check(a.Cells.Length==b.Cells.Length,"duplicate physics hits");
        });
        Test("R3_SNAPSHOT_IS_NOT_SHARED_ACROSS_PHYSICS_UPDATES",()=>{
            var m=Map();var sample=new[]{new GridCell(3,8)};
            var a=PhysicalObstacleRouter.FindFrom(m,m.Start,1,new RouteConditions(false,true),sample);
            sample[0]=new GridCell(7,8);
            var b=PhysicalObstacleRouter.FindFrom(m,m.Start,1,new RouteConditions(false,true),sample);
            Check(a.Reachable&&!b.Reachable,"stale/aliased occupancy");
        });
        Test("R3_CRATE_AUDIT_100_IDENTICAL_REPLAYS",()=>{
            var m=Map();var b=new[]{new GridCell(7,8)};
            var a=PhysicalObstacleRouter.AuditAllEight(m,b);
            for(int i=0;i<100;i++)
            {
                var x=PhysicalObstacleRouter.AuditAllEight(m,b);
                Check(a.AllReachable==x.AllReachable,"non-deterministic accept");
                for(int j=0;j<8;j++)
                    Check(a.Cases[j].Reachable==x.Cases[j].Reachable,"unstable reject");
            }
        });
        Reject("R3_NULL_PHYSICAL_OCCUPANCY",()=>PhysicalObstacleRouter.FindFrom(
            Map(),Map().Start,0,new RouteConditions(false,true),null!));
        Reject("R3_OUTSIDE_PHYSICS_HIT",()=>PhysicalObstacleRouter.FindFrom(
            Map(),Map().Start,0,new RouteConditions(false,true),new[]{new GridCell(-1,9)}));
        Reject("R3_OVER_BUDGET_PHYSICAL_HITS",()=>PhysicalObstacleRouter.FindFrom(
            Map(),Map().Start,0,new RouteConditions(false,true),new GridCell[Map().Width*Map().Height+1]));
        Reject("R3_AUDIT_NULL_PHYSICS",()=>PhysicalObstacleRouter.AuditAllEight(Map(),null!));
        Reject("R3_AUDIT_NULL_LEVEL",()=>PhysicalObstacleRouter.AuditAllEight(null!,new[]{new GridCell(3,8)}));
        Reject("R3_BAD_HEAVY_FOOTPRINT",()=>PhysicalObstacleRouter.FindFrom(
            Map(),Map().Start,3,new RouteConditions(false,true),Array.Empty<GridCell>()));
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

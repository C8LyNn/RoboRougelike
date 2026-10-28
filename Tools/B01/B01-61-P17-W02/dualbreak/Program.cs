// B01-61 PASS17/WAVE02 — independent two-owner-loss structural QA.
// Compiles ORIGINAL B01-23 frozen PARTSIZE + P17 rig validator source unchanged.
// No producer source edits. New cases: every unordered pair of two separate owners.
// LOGICAL cell adjacency only; never interpret as Unity/PhysX physics or sockets.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.PARTSIZE001;
using B01.P17Rigs;

public sealed record DualBreakCase(
    string BlueprintId,
    string[] RemovedPartIds,
    bool CoreLost,
    string[] DetachedPartIds,
    string[] ConnectedPartIds,
    bool SelectedDriveOperable,
    int ConnectedGunCount,
    string NativePhysicalStatus="NOT_RUN"
);

public static class Program
{
    private static readonly Cell3[] Faces={
        new(1,0,0),new(-1,0,0),new(0,1,0),new(0,-1,0),new(0,0,1),new(0,0,-1)
    };
    private static readonly string[] RigNames={
        "P17_Flanker_Wheels.json","P17_Bulwark_Tracks.json","P17_Skirmisher_Hover.json"
    };
    private static int assertions;
    private static void Check(bool condition,string reason)
    {
        assertions++;
        if(!condition)throw new InvalidDataException("DUALBREAK_CSHARP_RED:"+reason);
    }
    private static DualBreakCase Analyze(P17Rig rig,params int[] removed)
    {
        Check(RigValidator.Validate(rig)==null,"original input invalid "+rig.BlueprintId);
        Check(removed.Distinct().Count()==removed.Length&&removed.All(i=>i>=0&&i<rig.Parts.Count),"invalid removal indexes");
        var removedSet=removed.ToHashSet();
        var alive=Enumerable.Range(0,rig.Parts.Count).Where(i=>!removedSet.Contains(i)).ToArray();
        var cellToPart=new Dictionary<Cell3,int>();
        var owned=new Dictionary<int,Cell3[]>();
        foreach(int i in alive)
        {
            var p=rig.Parts[i];
            var cells=PartSizeGrid.Occupied(PartSizeBaselineCatalog.ForSourceKind(p.Kind),
                p.Cell.ToCell(),p.RotationQuarterTurns.ToCell());
            owned[i]=cells;
            foreach(var cell in cells)
                Check(cellToPart.TryAdd(cell,i),"two parts share occupied source logical cell");
        }
        int core=rig.Parts.FindIndex(p=>p.Kind=="Core");
        var connected=new HashSet<int>();
        var queue=new Queue<int>();
        if(core>=0&&!removedSet.Contains(core)){connected.Add(core);queue.Enqueue(core);}
        while(queue.Count!=0)
        {
            var owner=queue.Dequeue();
            foreach(var cell in owned[owner])
                foreach(var face in Faces)
                {
                    var neighbor=cell+face;
                    if(cellToPart.TryGetValue(neighbor,out int next)&&next!=owner&&connected.Add(next))
                        queue.Enqueue(next);
                }
        }
        var detached=alive.Except(connected).Select(i=>rig.Parts[i].LocalId)
                            .OrderBy(s=>s,StringComparer.Ordinal).ToArray();
        var remaining=connected.Select(i=>rig.Parts[i]).ToArray();
        string desired=rig.SelectedDrive switch{
            "Wheels"=>"Wheel","Tracks"=>"CrawlerTrack","Hover"=>"HoverEngine",
            _=>throw new InvalidDataException("UNRECOGNIZED_SELECTED_DRIVE")
        };
        bool functioning=rig.SelectedDrive=="Hover"
            ?remaining.Any(x=>x.Kind==desired)
            :remaining.Any(x=>x.Kind==desired&&x.Cell.X<0)
              &&remaining.Any(x=>x.Kind==desired&&x.Cell.X>0);
        var guns=remaining.Count(x=>x.Kind=="Gun");
        return new DualBreakCase(
            rig.BlueprintId,
            removed.Select(i=>rig.Parts[i].LocalId).OrderBy(s=>s,StringComparer.Ordinal).ToArray(),
            removedSet.Contains(core),detached,
            remaining.Select(x=>x.LocalId).OrderBy(s=>s,StringComparer.Ordinal).ToArray(),
            functioning,guns
        );
    }
    private static JsonSerializerOptions Json=new(){PropertyNameCaseInsensitive=true,WriteIndented=true,
        PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
    private static void OriginalSingleCheck(P17Rig rig,JsonElement oneRig)
    {
        Check(oneRig.GetProperty("blueprintId").GetString()==rig.BlueprintId,"38 data blueprint source");
        var cases=oneRig.GetProperty("singleBreakCases").EnumerateArray().ToArray();
        Check(cases.Length==rig.Parts.Count,"single-break count source");
        for(int i=0;i<rig.Parts.Count;i++)
        {
            var original=cases[i];
            var actual=Analyze(rig,i);
            var absent=original.GetProperty("detachedPartIds").EnumerateArray().Select(x=>x.GetString()).ToArray();
            Check(original.GetProperty("removedPartId").GetString()==actual.RemovedPartIds[0],"owner id "+i);
            Check(original.GetProperty("coreLost").GetBoolean()==actual.CoreLost,"core loss "+i);
            Check(original.GetProperty("detachedCount").GetInt32()==actual.DetachedPartIds.Length,"detach count "+i);
            Check(absent.SequenceEqual(actual.DetachedPartIds),"detached owner IDs "+i);
            Check(original.GetProperty("selectedDriveOperable").GetBoolean()==actual.SelectedDriveOperable,"drive "+i);
            Check(original.GetProperty("connectedGunCount").GetInt32()==actual.ConnectedGunCount,"guns "+i);
            Check(original.GetProperty("nativePhysicalStatus").GetString()=="NOT_RUN","physical status honest");
        }
    }
    private static void CounterexampleChecks(P17Rig[] rigs,List<DualBreakCase> all)
    {
        foreach(var rig in rigs)
        {
            int core=rig.Parts.FindIndex(p=>p.Kind=="Core");
            var withCore=all.Where(x=>x.BlueprintId==rig.BlueprintId&&
                x.RemovedPartIds.Contains(rig.Parts[core].LocalId)).ToArray();
            Check(withCore.Length==rig.Parts.Count-1,"core pair coverage");
            Check(withCore.All(x=>x.CoreLost&&!x.SelectedDriveOperable&&x.ConnectedGunCount==0
                &&x.ConnectedPartIds.Length==0),"core pair cannot retain connected fake modules");
            foreach(var pair in all.Where(x=>x.BlueprintId==rig.BlueprintId))
            {
                Check(pair.RemovedPartIds.Length==2&&pair.RemovedPartIds[0]!=pair.RemovedPartIds[1],
                    "two unique physical owner removals");
                Check(pair.RemovedPartIds.All(id=>!pair.ConnectedPartIds.Contains(id)&&!pair.DetachedPartIds.Contains(id)),
                    "destroyed part cannot be survivor/detached");
                Check(!pair.DetachedPartIds.Intersect(pair.ConnectedPartIds).Any(),
                    "disconnected part cannot also be core-reachable");
                Check(pair.DetachedPartIds.Length+pair.ConnectedPartIds.Length==rig.Parts.Count-2,
                    "exact owner accounting / cannot regenerate after damage");
                Check(pair.NativePhysicalStatus=="NOT_RUN","do not promote unrun collisions");
            }
        }
        var input=rigs[0];
        var invalid=RigValidator.Read(Path.Combine("original",RigNames[0]));
        invalid.Parts[1].LocalId=invalid.Parts[0].LocalId;
        Check(RigValidator.Validate(invalid)!=null,"negative duplicate part owner accepted");
        invalid=RigValidator.Read(Path.Combine("original",RigNames[0]));
        invalid.Parts[1].Cell=invalid.Parts[0].Cell;
        Check(RigValidator.Validate(invalid)!=null,"negative overlapping core cell accepted");
        var allGuns=all.Where(c=>c.ConnectedGunCount==0).ToArray();
        Check(allGuns.Length>0,"no battle-affecting double hits detected");
    }
    public static int Main(string[] args)
    {
        try
        {
            if(args.Length!=0)throw new InvalidDataException("NO_UNTRUSTED_RUNTIME_PARAMETERS");
            var rigs=RigNames.Select(name=>RigValidator.Read(Path.Combine("original",name))).ToArray();
            using var document=JsonDocument.Parse(File.ReadAllText("original/P17_SingleBreakOutcomes.json"));
            var previous=document.RootElement.GetProperty("rigs").EnumerateArray().ToArray();
            Check(previous.Length==3&&rigs.Length==3,"exact 3 owner rigs source");
            var outcomes=new List<DualBreakCase>();
            for(int r=0;r<rigs.Length;r++)
            {
                var rig=rigs[r];
                Check(RigValidator.Validate(rig)==null,"producer rig acceptance "+rig.BlueprintId);
                OriginalSingleCheck(rig,previous[r]);
                for(int first=0;first<rig.Parts.Count;first++)
                    for(int second=first+1;second<rig.Parts.Count;second++)
                        outcomes.Add(Analyze(rig,first,second));
            }
            Check(outcomes.Count==227,"expected 14C2+14C2+10C2 = 227 pair cases");
            Check(outcomes.Select(x=>x.BlueprintId+"|"+string.Join("|",x.RemovedPartIds)).Distinct().Count()==227,"unique pair identity");
            CounterexampleChecks(rigs,outcomes);
            File.WriteAllText("dualbreak_output.json",JsonSerializer.Serialize(outcomes,Json));
            Console.WriteLine("B01-61_W02_ORIGINAL_CSHARP=PASS | ORIGINAL_SINGLE_CASES=38/38 | NEW_PAIR_SCENARIOS=227/227 | ASSERTIONS="+assertions+" | NEGATIVE_INVALID_IDS=REJECTED | NEGATIVE_CELL_OVERLAP=REJECTED | UNITY_PHYSX=NOT_RUN | GAME_INTEGRATION=NO");
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("B01-61_W02_ORIGINAL_CSHARP_RED="+e);
            return 2;
        }
    }
}

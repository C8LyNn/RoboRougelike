// B01-23 P17 WEAKPOINTS-001: exhaustive one-PHYSICAL-owner break prediction.
// Uses actual frozen source logical occupied-cell geometry. NOT Unity PhysX,
// not collision narrowphase, not damage application, not enemy AI or garage code.
using System;
using System.Collections.Generic;
using System.Linq;
using B01.PARTSIZE001;
using B01.P17Rigs;

namespace B01.P17Weakpoints;

public sealed class WeakpointCase
{
    public string RemovedPartId {get;set;}="";
    public string RemovedKind {get;set;}="";
    public bool CoreLost {get;set;}
    public List<string> DetachedPartIds {get;set;}=new();
    public int DetachedCount {get;set;}
    public bool SelectedDriveOperable {get;set;}
    public int ConnectedGunCount {get;set;}
    public string NativePhysicalStatus {get;set;}="NOT_RUN";
}
public sealed class WeakpointDataset
{
    public string Schema {get;set;}="B01-P17-WEAKPOINTS-001-v1";
    public string OriginBlueprintSha {get;set;}="0978497561c8b6400490235d3e5a401ac32bea2d";
    public string OriginalPartsizeSha {get;set;}="be2be0e4802bc3f450179901f9ae91c6d84bcb5b";
    public string EvidenceClass {get;set;}="SOURCE_LOGICAL_ONLY_UNVERIFIED_PHYSX";
    public List<WeakpointRig> Rigs {get;set;}=new();
}
public sealed class WeakpointRig
{
    public string BlueprintId {get;set;}="";
    public string SelectedDrive {get;set;}="";
    public int OriginalOwnerCount {get;set;}
    public List<WeakpointCase> SingleBreakCases {get;set;}=new();
}

public static class WeakpointEngine
{
    private static readonly Cell3[] Faces={
        new(1,0,0),new(-1,0,0),new(0,1,0),new(0,-1,0),new(0,0,1),new(0,0,-1)
    };

    public static WeakpointRig Analyze(P17Rig rig)
    {
        if(RigValidator.Validate(rig) is string fail)
            throw new ArgumentException("INVALID_OR_UNTRUSTED_BASE_BLUEPRINT:"+fail,nameof(rig));
        return new WeakpointRig{
            BlueprintId=rig.BlueprintId,
            SelectedDrive=rig.SelectedDrive,
            OriginalOwnerCount=rig.Parts.Count,
            SingleBreakCases=Enumerable.Range(0,rig.Parts.Count).Select(i=>Simulate(rig,i)).ToList()
        };
    }

    public static WeakpointCase Simulate(P17Rig rig,int removedPartIndex)
    {
        if(RigValidator.Validate(rig) is string fail)
            throw new ArgumentException("INVALID_OR_UNTRUSTED_BASE_BLUEPRINT:"+fail,nameof(rig));
        if(removedPartIndex<0||removedPartIndex>=rig.Parts.Count)
            throw new ArgumentOutOfRangeException(nameof(removedPartIndex));

        var removed=rig.Parts[removedPartIndex];
        var alive=Enumerable.Range(0,rig.Parts.Count).Where(i=>i!=removedPartIndex).ToArray();
        var cellOwner=new Dictionary<Cell3,int>();
        foreach(int i in alive)
        {
            var p=rig.Parts[i];
            var physical=PartSizeBaselineCatalog.ForSourceKind(p.Kind);
            foreach(var cell in PartSizeGrid.Occupied(physical,p.Cell.ToCell(),
                         p.RotationQuarterTurns.ToCell()))
            {
                if(!cellOwner.TryAdd(cell,i))
                    throw new ArgumentException("DUPLICATE_LOGICAL_CELL_OWNER");
            }
        }

        var coreIndex=Array.FindIndex(rig.Parts.ToArray(),p=>p.Kind=="Core");
        var reachable=new HashSet<int>();
        var queue=new Queue<int>();
        if(coreIndex>=0 && coreIndex!=removedPartIndex)
        {
            reachable.Add(coreIndex);
            queue.Enqueue(coreIndex);
        }

        while(queue.TryDequeue(out int current))
        {
            var part=rig.Parts[current];
            var def=PartSizeBaselineCatalog.ForSourceKind(part.Kind);
            foreach(var cell in PartSizeGrid.Occupied(def,part.Cell.ToCell(),
                         part.RotationQuarterTurns.ToCell()))
                foreach(var d in Faces)
                {
                    var next=new Cell3(cell.X+d.X,cell.Y+d.Y,cell.Z+d.Z);
                    // P17 schema demands 63/all-faces mounting contacts, but
                    // these are still LOGICAL ports, never trusted typed sockets.
                    if(cellOwner.TryGetValue(next,out int owner) &&
                       owner!=current && reachable.Add(owner))
                        queue.Enqueue(owner);
                }
        }

        var orphans=alive.Where(i=>!reachable.Contains(i))
                         .Select(i=>rig.Parts[i].LocalId)
                         .OrderBy(id=>id,StringComparer.Ordinal).ToList();
        var connected=reachable.Select(i=>rig.Parts[i]).ToArray();
        var kind=rig.SelectedDrive switch {
            "Wheels"=>"Wheel","Tracks"=>"CrawlerTrack","Hover"=>"HoverEngine",
            _=>throw new ArgumentException("UNSUPPORTED_DRIVE")
        };
        bool drive=rig.SelectedDrive=="Hover"
            ? connected.Any(p=>p.Kind==kind)
            : connected.Any(p=>p.Kind==kind&&p.Cell.X<0) &&
              connected.Any(p=>p.Kind==kind&&p.Cell.X>0);
        return new WeakpointCase{
            RemovedPartId=removed.LocalId, RemovedKind=removed.Kind,
            CoreLost=removed.Kind=="Core",DetachedPartIds=orphans,
            DetachedCount=orphans.Count,SelectedDriveOperable=drive,
            ConnectedGunCount=connected.Count(p=>p.Kind=="Gun"),
            NativePhysicalStatus="NOT_RUN"
        };
    }

    public static WeakpointDataset Build(IEnumerable<P17Rig> rigs)
    {
        if(rigs is null)throw new ArgumentNullException(nameof(rigs));
        var array=rigs.ToArray();
        if(array.Length!=3 || array.Select(p=>p.BlueprintId).Distinct().Count()!=3)
            throw new ArgumentException("EXPECTED_THREE_DISTINCT_ORIGINAL_BLUEPRINTS");
        return new WeakpointDataset{Rigs=array.Select(Analyze).ToList()};
    }
}

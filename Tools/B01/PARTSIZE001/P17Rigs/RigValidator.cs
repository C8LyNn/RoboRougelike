// B01-23 / PASS17: REAL SPECIALIST RIG CONTENT validation/preview against original PARTSIZE C#.
// No UnityEditor, no canonical R3/Garage/AI writes, no collision/typed socket approval.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.PARTSIZE001;

namespace B01.P17Rigs;

public sealed class P17Rig
{
    public string Schema {get;set;}="";
    public string BlueprintId {get;set;}="";
    public string DisplayName {get;set;}="";
    public string Description {get;set;}="";
    public string SelectedDrive {get;set;}="";
    public string RecommendedBaseProfile {get;set;}="";
    public string SourcePartSizeCommit {get;set;}="";
    public string ContentStatus {get;set;}="";
    public string NativePhysicalStatus {get;set;}="";
    public List<P17Part> Parts {get;set;}=new();
}
public sealed class P17Part
{
    public string LocalId {get;set;}="";
    public string Kind {get;set;}="";
    public P17Point Cell {get;set;}=new();
    public P17Point RotationQuarterTurns {get;set;}=new();
    public int ContactMask {get;set;}
    public string? Weapon {get;set;}
}
public sealed class P17Point
{
    public int X {get;set;}
    public int Y {get;set;}
    public int Z {get;set;}
    public Cell3 ToCell() => new(X,Y,Z);
}

public static class RigValidator
{
    public const string ProducerSha="be2be0e4802bc3f450179901f9ae91c6d84bcb5b";
    private static readonly Cell3[] Directions={
        new(1,0,0), new(-1,0,0), new(0,1,0),
        new(0,-1,0), new(0,0,1), new(0,0,-1)};
    private static readonly HashSet<string> Profiles=new(StringComparer.Ordinal)
        {"Light","Standard","Heavy"};
    private static readonly HashSet<string> Drives=new(StringComparer.Ordinal)
        {"Wheels","Tracks","Hover"};
    private static readonly JsonSerializerOptions JsonOptions=new()
        {PropertyNameCaseInsensitive=true,ReadCommentHandling=JsonCommentHandling.Disallow,AllowTrailingCommas=false};

    public static P17Rig Read(string path)
    {
        if(!File.Exists(path))throw new InvalidDataException("BLUEPRINT_MISSING");
        try { return JsonSerializer.Deserialize<P17Rig>(File.ReadAllText(path),JsonOptions)
                     ?? throw new InvalidDataException("BLUEPRINT_JSON_NULL"); }
        catch(JsonException error) { throw new InvalidDataException("BLUEPRINT_INVALID_JSON",error); }
    }

    public static string? Validate(P17Rig? rig)
    {
        if(rig is null||rig.Schema!="B01-PARTSIZE-P17-RIG-BLUEPRINT-v1" ||
           rig.SourcePartSizeCommit!=ProducerSha ||
           rig.ContentStatus!="GAME_CONTENT_CANDIDATE_NOT_INTEGRATED" ||
           rig.NativePhysicalStatus!="UNMEASURED_COLLIDER_AND_SOCKET" ||
           string.IsNullOrWhiteSpace(rig.BlueprintId) ||
           string.IsNullOrWhiteSpace(rig.DisplayName) ||
           string.IsNullOrWhiteSpace(rig.Description) ||
           !Drives.Contains(rig.SelectedDrive) ||
           !Profiles.Contains(rig.RecommendedBaseProfile))
            return "INVALID_BLUEPRINT_HEADER";

        if(rig.Parts is null || rig.Parts.Count<4 || rig.Parts.Count>40)
            return "INVALID_PART_COUNT";

        var ownerByCell=new Dictionary<Cell3,int>();
        var owners=new List<List<Cell3>>();
        var ids=new HashSet<string>(StringComparer.Ordinal);
        var cores=0;
        var weapons=0;
        int left=0,right=0,hover=0;
        foreach(var part in rig.Parts)
        {
            if(part is null || part.Cell is null || part.RotationQuarterTurns is null ||
               string.IsNullOrWhiteSpace(part.LocalId)||!ids.Add(part.LocalId))
                return "INVALID_OR_DUPLICATE_PART_ID";

            if(Math.Abs((long)part.Cell.X)>12 || Math.Abs((long)part.Cell.Y)>12 ||
               Math.Abs((long)part.Cell.Z)>12 ||
               part.RotationQuarterTurns.X!=0 || part.RotationQuarterTurns.Z!=0 ||
               part.RotationQuarterTurns.Y<0 || part.RotationQuarterTurns.Y>3 ||
               part.ContactMask!=63)
                return "UNSUPPORTED_R3_CELL_ROTATION_OR_SOCKET_MASK";

            PartSizeDefinition definition;
            try { definition=PartSizeBaselineCatalog.ForSourceKind(part.Kind); }
            catch(ArgumentException) { return "UNKNOWN_CANONICAL_PART_KIND"; }

            if(part.Kind=="Gun")
            {
                if(part.Weapon!="Basic")return "INVALID_OR_UNAPPROVED_WEAPON";
                weapons++;
            }
            else if(part.Weapon is not null)
                return "NON_GUN_HAS_WEAPON";

            if(part.Kind=="Core")cores++;
            if(part.Kind==("Wheels"==rig.SelectedDrive?"Wheel":
                          "Tracks"==rig.SelectedDrive?"CrawlerTrack":"HoverEngine"))
            {
                if(part.Cell.X<0)left++;
                if(part.Cell.X>0)right++;
                if(part.Kind=="HoverEngine")hover++;
            }

            Cell3[] occupied;
            try { occupied=PartSizeGrid.Occupied(definition,part.Cell.ToCell(),part.RotationQuarterTurns.ToCell()); }
            catch(ArgumentException) { return "INVALID_NATIVE_PARTSIZE_DEFINITION"; }

            var cells=new List<Cell3>();
            foreach(var cell in occupied)
            {
                if(ownerByCell.ContainsKey(cell))return "TWO_PARTS_OCCUPY_SAME_LOGICAL_CELL";
                ownerByCell[cell]=owners.Count;
                cells.Add(cell);
            }
            owners.Add(cells);
        }
        if(cores!=1)return "REQUIRE_EXACTLY_ONE_CORE";
        if(weapons<1 || weapons>2)return "WEAPON_LAYOUT_NOT_SUPPORTED";
        if(rig.SelectedDrive=="Wheels" || rig.SelectedDrive=="Tracks")
        {
            if(left<1||right<1)return "SELECTED_DRIVE_REQUIRES_BOTH_SIDES";
        }
        else if(hover<1)return "SELECTED_HOVER_HAS_NO_ENGINE";

        int core=rig.Parts.FindIndex(p=>p.Kind=="Core");
        var visited=new HashSet<int>{core};
        var queue=new Queue<int>();
        queue.Enqueue(core);
        while(queue.Count>0)
        {
            var current=queue.Dequeue();
            foreach(var cell in owners[current])
                foreach(var d in Directions)
                {
                    var neighbor=new Cell3(cell.X+d.X,cell.Y+d.Y,cell.Z+d.Z);
                    if(ownerByCell.TryGetValue(neighbor,out int other)&&
                       other!=current&&visited.Add(other))queue.Enqueue(other);
                }
        }
        if(visited.Count!=owners.Count)return "DISCONNECTED_MODULE_FROM_CORE";

        // Critical: logical geometry never authorizes true physical clearance.
        // All wheel/gun/track/hover envelopes are explicitly UNMEASURED by frozen
        // PartSizeBaselineCatalog. Runtime test is still needed in Unity.
        foreach(var part in rig.Parts.Where(p=>p.Kind!="Armor"&&p.Kind!="Core"))
        {
            if(PartSizeGrid.DefinitionError(PartSizeBaselineCatalog.ForSourceKind(part.Kind))
                !="UNMEASURED_PHYSICAL_ENVELOPE")
                return "SOURCE_PHYSICAL_STATUS_CHANGED_REQUIRE_REVIEW";
        }
        return null;
    }

    public static string Describe(P17Rig rig)
    {
        string? error=Validate(rig);
        if(error is not null)return rig.BlueprintId+" INVALID "+error;
        return rig.BlueprintId+" PASS_LOGICAL_SOURCE_ONLY parts="+rig.Parts.Count+
            " guns="+rig.Parts.Count(p=>p.Kind=="Gun")+
            " drive="+rig.SelectedDrive+" native=NOT_RUN";
    }
}

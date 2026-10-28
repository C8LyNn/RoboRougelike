using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using B01.PARTSIZE001;

namespace B01.P17Rigs;

internal static class Program
{
    private static readonly string Dir=Path.Combine("Assets","PARTSIZE001","Blueprints","P17");
    private static readonly string[] Files={
        "P17_Flanker_Wheels.json","P17_Bulwark_Tracks.json","P17_Skirmisher_Hover.json"};
    private static int checks;

    private static P17Rig Copy(P17Rig value) =>
        JsonSerializer.Deserialize<P17Rig>(JsonSerializer.Serialize(value)) ??
        throw new InvalidOperationException("CLONE_FAILED");

    private static void Check(string id,bool passed)
    {
        if(!passed)throw new Exception("P17_SOURCE_CONTENT_FAIL:"+id);
        checks++;
        Console.WriteLine("PASS "+id);
    }

    private static void Reject(string id,P17Rig blueprint,Action<P17Rig> mutate,string expected)
    {
        var bad=Copy(blueprint);
        mutate(bad);
        Check(id,RigValidator.Validate(bad)==expected);
    }

    private static void RunTests(P17Rig[] all)
    {
        Check("01_three_distinct_source_compatible_blueprints",all.Length==3 &&
              all.Select(p=>p.BlueprintId).Distinct(StringComparer.Ordinal).Count()==3);
        Check("02_three_different_drive_topologies",
              all.Select(p=>p.SelectedDrive).OrderBy(s=>s).SequenceEqual(new[]{"Hover","Tracks","Wheels"}));
        Check("03_exact_original_source_mapped_geometry",
              PartSizeGrid.GridMeters==.55 &&
              PartSizeGrid.LegacyBlockMeters==.52 &&
              PartSizeBaselineCatalog.ForSourceKind("Core").Dimensions.Equals(new Cell3(1,1,1)));
        Check("04_flanker_14_individual_owners_and_4_wheels",
              all[0].Parts.Count==14 && all[0].Parts.Count(p=>p.Kind=="Wheel")==4);
        Check("05_bulwark_14_owners_and_2_tracks",
              all[1].Parts.Count==14 && all[1].Parts.Count(p=>p.Kind=="CrawlerTrack")==2);
        Check("06_skirmisher_10_owners_and_2_hover",
              all[2].Parts.Count==10 && all[2].Parts.Count(p=>p.Kind=="HoverEngine")==2);
        foreach(var p in all)
            Check("07_logical_valid_"+p.BlueprintId,RigValidator.Validate(p)==null);
        Check("08_no_unmeasured_physical_claims",
              all.All(p=>p.NativePhysicalStatus=="UNMEASURED_COLLIDER_AND_SOCKET"));
        Check("09_wide_kind_footprint_uses_original_4_cells",
              PartSizeGrid.Occupied(PartSizeBaselineCatalog.ForSourceKind("BonusModule"),
                    new Cell3(0,0,0),new Cell3(0,1,0)).Length==4);
        Check("10_mountable_unmeasured_wheel_not_physical_pass",
              PartSizeGrid.DefinitionError(PartSizeBaselineCatalog.ForSourceKind("Wheel"))==
                  "UNMEASURED_PHYSICAL_ENVELOPE");
        var wheel=all[0];
        Reject("11_duplicate_owned_id",wheel,p=>p.Parts[1].LocalId=p.Parts[0].LocalId,
               "INVALID_OR_DUPLICATE_PART_ID");
        Reject("12_occupied_voxel_overlap",wheel,p=>p.Parts[1].Cell=new P17Point(),
               "TWO_PARTS_OCCUPY_SAME_LOGICAL_CELL");
        Reject("13_disconnected_gun",wheel,p=>p.Parts[^1].Cell=new P17Point{X=9,Y=9,Z=9},
               "DISCONNECTED_MODULE_FROM_CORE");
        Reject("14_no_core",wheel,p=>p.Parts.RemoveAt(0),
               "REQUIRE_EXACTLY_ONE_CORE");
        Reject("15_duplicate_core",wheel,p=>p.Parts[1].Kind="Core",
               "REQUIRE_EXACTLY_ONE_CORE");
        Reject("16_unknown_kind",wheel,p=>p.Parts[1].Kind="ImpossiblePart",
               "UNKNOWN_CANONICAL_PART_KIND");
        Reject("17_wrong_footprint_yaw",wheel,p=>p.Parts[1].RotationQuarterTurns.X=1,
               "UNSUPPORTED_R3_CELL_ROTATION_OR_SOCKET_MASK");
        Reject("18_wrong_contact_mask",wheel,p=>p.Parts[1].ContactMask=64,
               "UNSUPPORTED_R3_CELL_ROTATION_OR_SOCKET_MASK");
        Reject("19_missing_left_drive",wheel,p=>p.Parts.RemoveAll(q=>q.Kind=="Wheel"&&q.Cell.X<0),
               "SELECTED_DRIVE_REQUIRES_BOTH_SIDES");
        Reject("20_wrong_source_sha",wheel,p=>p.SourcePartSizeCommit=new string('f',40),
               "INVALID_BLUEPRINT_HEADER");
        Reject("21_invented_physical_pass",wheel,p=>p.NativePhysicalStatus="G1_PASS",
               "INVALID_BLUEPRINT_HEADER");
        Reject("22_non_gun_has_weapon",wheel,p=>p.Parts[1].Weapon="Basic",
               "NON_GUN_HAS_WEAPON");
        Reject("23_not_existing_weapon",wheel,p=>p.Parts[^1].Weapon="Imaginary",
               "INVALID_OR_UNAPPROVED_WEAPON");
        Reject("24_unarmed_build",wheel,p=>p.Parts.RemoveAll(q=>q.Kind=="Gun"),
               "WEAPON_LAYOUT_NOT_SUPPORTED");
        Reject("25_wheel_drive_selected_but_only_hover",all[2],
               p=>p.SelectedDrive="Wheels","SELECTED_DRIVE_REQUIRES_BOTH_SIDES");
        Reject("26_hovers_without_engine",all[2],
               p=>p.Parts.RemoveAll(q=>q.Kind=="HoverEngine"),
               "SELECTED_HOVER_HAS_NO_ENGINE");
        Reject("27_track_requires_both_sides",all[1],
               p=>p.Parts.RemoveAll(q=>q.Kind=="CrawlerTrack"&&q.Cell.X<0),
               "SELECTED_DRIVE_REQUIRES_BOTH_SIDES");
        var first=RigValidator.Validate(wheel);
        Check("28_original_untouched_after_all_adversarial_cases",first is null);
    }

    private static string Render(P17Rig rig)
    {
        string? error=RigValidator.Validate(rig);
        if(error!=null)throw new InvalidDataException("UNSAFE_TO_PREVIEW:"+error);
        var glyphs=new Dictionary<(int x,int y,int z),char>();
        foreach(var p in rig.Parts)
        {
            char code=p.Kind switch {
                "Core"=>'C',"Armor"=>'#',"Wheel"=>'W',
                "CrawlerTrack"=>'T',"HoverEngine"=>'H',"Gun"=>'G',_=>'?'
            };
            var original=PartSizeBaselineCatalog.ForSourceKind(p.Kind);
            foreach(var cell in PartSizeGrid.Occupied(original,p.Cell.ToCell(),p.RotationQuarterTurns.ToCell()))
                glyphs[(cell.X,cell.Y,cell.Z)]=code;
        }
        var buf=new StringBuilder();
        buf.AppendLine(RigValidator.Describe(rig));
        int minX=glyphs.Keys.Min(c=>c.x),maxX=glyphs.Keys.Max(c=>c.x);
        int minY=glyphs.Keys.Min(c=>c.y),maxY=glyphs.Keys.Max(c=>c.y);
        int minZ=glyphs.Keys.Min(c=>c.z),maxZ=glyphs.Keys.Max(c=>c.z);
        for(int y=maxY;y>=minY;y--)
        {
            buf.AppendLine("y="+y+" (+z at top)");
            for(int z=maxZ;z>=minZ;z--)
            {
                for(int x=minX;x<=maxX;x++)
                    buf.Append(glyphs.TryGetValue((x,y,z),out char mark)?mark:'.');
                buf.AppendLine();
            }
        }
        return buf.ToString();
    }

    private static int Main(string[] args)
    {
        try
        {
            if(args.Length!=1 || (args[0]!="--selftest"&&args[0]!="--preview"))
            {
                Console.Error.WriteLine("Use --selftest or --preview. This program does not import anything into Unity.");
                return 2;
            }
            var all=Files.Select(f=>RigValidator.Read(Path.Combine(Dir,f))).ToArray();
            if(args[0]=="--preview")
            {
                foreach(var p in all)Console.WriteLine(Render(p));
                Console.WriteLine("P17_PREVIEW_SOURCE_ONLY_NOGAME_NATIVE_NOT_RUN");
                return 0;
            }
            RunTests(all);
            Console.WriteLine("B01_23_P17_SPECIALIST_RIGS_PASS "+checks+
                "/"+checks+" ACTUAL_FROZEN_PARTSIZE_CSHARP; UNITY_PHYSX_NOT_RUN; CODEx_GAME_UNTOUCHED");
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("B01_23_P17_SPECIALIST_RIGS_FAIL: "+e);
            return 1;
        }
    }
}

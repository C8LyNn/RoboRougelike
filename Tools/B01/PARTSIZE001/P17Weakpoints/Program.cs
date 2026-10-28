// Executable real-source regression of weak-point prediction and precomputed content.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.P17Rigs;

namespace B01.P17Weakpoints;

internal static class Program
{
    private static readonly string[] Names={
        "P17_Flanker_Wheels.json","P17_Bulwark_Tracks.json","P17_Skirmisher_Hover.json"};
    private const string Blueprints="Assets/PARTSIZE001/Blueprints/P17";
    private const string Output="Assets/PARTSIZE001/Weakpoints/P17/P17_SingleBreakOutcomes.json";
    private static readonly JsonSerializerOptions Json=new(){
        PropertyNameCaseInsensitive=true,PropertyNamingPolicy=JsonNamingPolicy.CamelCase,
        WriteIndented=true
    };
    private static int count;
    private static void Pass(string id,bool condition)
    {
        if(!condition)throw new Exception("WEAKPOINT_REAL_SOURCE_TEST_FAIL:"+id);
        count++;
        Console.WriteLine("PASS "+id);
    }
    private static void Reject(string id,Action action)
    {
        bool rejected=false;
        try{action();}catch(ArgumentException){rejected=true;}
        Pass(id,rejected);
    }
    private static P17Rig Clone(P17Rig rig)=>
        JsonSerializer.Deserialize<P17Rig>(JsonSerializer.Serialize(rig,Json),Json)!;
    private static P17Rig[] Load()=>
        Names.Select(n=>RigValidator.Read(Path.Combine(Blueprints,n))).ToArray();
    private static WeakpointCase Case(WeakpointDataset d,int rig,string suffix)=>
        d.Rigs[rig].SingleBreakCases.Single(c=>c.RemovedPartId.EndsWith(suffix,StringComparison.Ordinal));
    private static void VerifyDataset(WeakpointDataset expected,WeakpointDataset actual)
    {
        Pass("dataset_schema_source_pin",
             actual.Schema==expected.Schema&&
             actual.OriginBlueprintSha==expected.OriginBlueprintSha&&
             actual.OriginalPartsizeSha==expected.OriginalPartsizeSha&&
             actual.EvidenceClass==expected.EvidenceClass);
        Pass("three_original_blueprints",actual.Rigs.Count==3&&
             actual.Rigs.Select(r=>r.BlueprintId).SequenceEqual(expected.Rigs.Select(r=>r.BlueprintId)));
        for(int i=0;i<3;i++)
        {
            var a=actual.Rigs[i];var e=expected.Rigs[i];
            Pass("exact_blueprint_owner_count_"+i,
                 a.OriginalOwnerCount==e.OriginalOwnerCount &&
                 a.SelectedDrive==e.SelectedDrive &&
                 a.SingleBreakCases.Count==e.SingleBreakCases.Count);
            for(int j=0;j<e.SingleBreakCases.Count;j++)
            {
                var x=a.SingleBreakCases[j];var y=e.SingleBreakCases[j];
                Pass("source_grounded_removal_"+i+"_"+j,
                     x.RemovedPartId==y.RemovedPartId&&x.RemovedKind==y.RemovedKind&&
                     x.CoreLost==y.CoreLost&&
                     x.DetachedCount==y.DetachedCount&&
                     x.SelectedDriveOperable==y.SelectedDriveOperable&&
                     x.ConnectedGunCount==y.ConnectedGunCount&&
                     x.NativePhysicalStatus=="NOT_RUN" &&
                     x.DetachedPartIds.SequenceEqual(y.DetachedPartIds));
            }
        }
    }
    private static void RunTests(P17Rig[] rigs,WeakpointDataset computed)
    {
        Pass("all_three_input_rigs_original_valid",rigs.All(x=>RigValidator.Validate(x) is null));
        Pass("original_source_count_38",
             rigs.Sum(x=>x.Parts.Count)==38&&computed.Rigs.Sum(x=>x.SingleBreakCases.Count)==38);
        Pass("cores_are_one_physical_owner",
             computed.Rigs.All(x=>x.SingleBreakCases.Count(c=>c.CoreLost)==1));
        Pass("all_native_collision_measurements_unknown",
             computed.Rigs.SelectMany(x=>x.SingleBreakCases).All(c=>c.NativePhysicalStatus=="NOT_RUN"));
        Pass("flanker_upper_armor_is_structural_weakspot",
             Case(computed,0,"-006").DetachedPartIds.Contains(rigs[0].Parts[12].LocalId));
        Pass("skirmisher_upper_armor_severs_top_gun",
             Case(computed,2,"-006").DetachedPartIds.Contains(rigs[2].Parts[8].LocalId));
        Pass("flanker_four_wheels_single_wheel_still_drives",
             Case(computed,0,"-009").SelectedDriveOperable);
        Pass("bulwark_single_left_track_loss_disables_tracks",
             !Case(computed,1,"-011").SelectedDriveOperable);
        Pass("hover_twin_engines_one_loss_keeps_hover",
             Case(computed,2,"-007").SelectedDriveOperable);
        Pass("flanker_gun_loss_leaves_one_gun",
             Case(computed,0,"-013").ConnectedGunCount==1);
        Pass("destroyed_core_disables_vehicle_and_orphans_all_survivors",
             computed.Rigs.All(x=>{
                 var c=x.SingleBreakCases.Single(s=>s.CoreLost);
                 return c.DetachedCount==x.OriginalOwnerCount-1 &&
                        !c.SelectedDriveOperable&&c.ConnectedGunCount==0;
             }));
        Pass("no_removal_has_self_in_detached_set",
             computed.Rigs.SelectMany(x=>x.SingleBreakCases).All(c=>!c.DetachedPartIds.Contains(c.RemovedPartId)));
        Pass("all_detached_identifiers_are_surviving_real_owners",
             computed.Rigs.Select((rg,i)=>rg.SingleBreakCases.All(c=>
                 c.DetachedPartIds.All(id=>rigs[i].Parts.Any(p=>p.LocalId==id)))).All(v=>v));
        Pass("all_detached_sets_are_deduplicated_ordered",
             computed.Rigs.SelectMany(x=>x.SingleBreakCases).All(c=>
                 c.DetachedPartIds.SequenceEqual(c.DetachedPartIds.Distinct().OrderBy(s=>s,StringComparer.Ordinal))));
        Pass("negative_removed_index_rejected",ThrowsIndex(rigs[0],-1));
        Pass("oversize_removed_index_rejected",ThrowsIndex(rigs[0],rigs[0].Parts.Count));
        Reject("invalid_duplicate_original_part_id_fail_closed",()=>{
             var bad=Clone(rigs[0]); bad.Parts[1].LocalId=bad.Parts[2].LocalId;
             WeakpointEngine.Analyze(bad);
        });
        Reject("invalid_overlap_original_part_fail_closed",()=>{
             var bad=Clone(rigs[0]);bad.Parts[1].Cell=bad.Parts[0].Cell;
             WeakpointEngine.Analyze(bad);
        });
        Reject("invalid_canonical_part_name_fail_closed",()=>{
             var bad=Clone(rigs[2]);bad.Parts[1].Kind="DOES_NOT_EXIST";
             WeakpointEngine.Analyze(bad);
        });
        Reject("untrusted_socket_mask_fail_closed",()=>{
             var bad=Clone(rigs[1]);bad.Parts[4].ContactMask=0;
             WeakpointEngine.Analyze(bad);
        });
        Reject("unapproved_source_fork_fail_closed",()=>{
             var bad=Clone(rigs[0]);bad.SourcePartSizeCommit=new string('f',40);
             WeakpointEngine.Analyze(bad);
        });
        Reject("wrong_count_rigs_rejected",()=>WeakpointEngine.Build(rigs.Take(2)));
        Reject("duplicated_rig_rejected",()=>WeakpointEngine.Build(new[]{rigs[0],rigs[0],rigs[1]}));
        VerifyDataset(computed,ReadOutput());
    }
    private static bool ThrowsIndex(P17Rig rig,int index)
    {
        try{WeakpointEngine.Simulate(rig,index);return false;}
        catch(ArgumentOutOfRangeException){return true;}
    }
    private static WeakpointDataset ReadOutput()
    {
        if(!File.Exists(Output))throw new InvalidDataException("MISSING_COMMITTED_WEAKPOINT_GAME_DATA");
        return JsonSerializer.Deserialize<WeakpointDataset>(File.ReadAllText(Output),Json) ??
            throw new InvalidDataException("BROKEN_WEAKPOINT_JSON");
    }

    private static int Main(string[] args)
    {
        if(args.Length!=1||!new[]{"--selftest","--check","--preview"}.Contains(args[0]))
        {
            Console.Error.WriteLine("Use --selftest, --check, or --preview; NOT a Unity editor command.");
            return 2;
        }
        try
        {
            var rigs=Load();
            var computed=WeakpointEngine.Build(rigs);
            if(args[0]=="--selftest")RunTests(rigs,computed);
            else if(args[0]=="--check")VerifyDataset(computed,ReadOutput());
            else foreach(var rig in computed.Rigs)
            {
                Console.WriteLine("=== "+rig.BlueprintId+" / "+rig.SelectedDrive+" ===");
                foreach(var c in rig.SingleBreakCases.Where(c=>
                         c.DetachedCount>0||!c.SelectedDriveOperable||c.ConnectedGunCount<2))
                    Console.WriteLine(c.RemovedPartId+" ["+c.RemovedKind+
                      "] detached="+c.DetachedCount+" drive="+c.SelectedDriveOperable+
                      " guns="+c.ConnectedGunCount);
            }
            if(args[0]=="--preview")Console.WriteLine("P17_WEAKPOINT_PREVIEW_SOURCE_ONLY_UNITY_NOT_RUN");
            else Console.WriteLine("B01_23_WEAKPOINTS_PASS "+count+"/"+count+
                " REAL_ORIGINAL_PARTSIZE_AND_RIG_CSHARP; UNITY_NOT_RUN; G2_HOLD");
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("B01_23_WEAKPOINTS_FAIL: "+e);
            return 1;
        }
    }
}

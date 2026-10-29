// Real executable test of the EXACT core compiled by the Unity Editor adapter.
// Synthetic prefab binding GUIDs here test only the required fail-closed syntax.
// They DO NOT certify Unity asset existence, native Collider, typed socket, or G1.
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.PARTSIZE001;
using B01.PARTSIZE001.P17Probe;

internal static class Program
{
    private static readonly string[] FilePaths={
        "Assets/PARTSIZE001/Bracing/P17/P17_Flanker_Reinforced.json",
        "Assets/PARTSIZE001/Bracing/P17/P17_Bulwark_Reinforced.json",
        "Assets/PARTSIZE001/Bracing/P17/P17_Skirmisher_Reinforced.json"
    };
    private static readonly JsonSerializerOptions Json=new(){
        IncludeFields=true,PropertyNameCaseInsensitive=true,WriteIndented=true};
    private static int checks;
    private static void Pass(string name,bool result)
    {
        if(!result)throw new Exception("P17_NATIVE_PROBE_SOURCE_FAIL_"+name);
        Console.WriteLine("PASS "+name);checks++;
    }
    private static T Copy<T>(T value)=>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value,Json),Json)!;
    private static Intake Input()
    {
        var input=new Intake{
            schema=BracingPhysicalContract.IntakeSchema,
            originalBracingSha=BracingPhysicalContract.OriginalProducerSha,
            nativeEvidence="NOT_RUN",
            rigs=FilePaths.Select(path=>JsonSerializer.Deserialize<PhysicalRig>(
                File.ReadAllText(path),Json)!).ToArray(),
            // Deliberately synthetic: only schema positive control. Not Unity prefab proof.
            bindings=BracingPhysicalContract.Kinds.Select((k,i)=>new Binding{
                kind=k,assetPath="Assets/FAKE_SOURCE_SCHEMA_ONLY/"+k+".prefab",
                assetGuid=new string((char)('a'+i),32)
            }).ToArray()
        };
        return input;
    }
    private static void Reject(string name,Action<Intake> mutate)
    {
        var data=Input();
        mutate(data);
        Pass(name,BracingPhysicalContract.InputError(data)!=null);
    }
    private static int Main(string[] args)
    {
        if(args.Length!=1 || args[0]!="--selftest")
        {
            Console.Error.WriteLine("Use --selftest; native Unity API is NOT executed here.");
            return 2;
        }
        try
        {
            var input=Input();
            Pass("exact_original_P17_game_json_has_three_rigs",input.rigs.Length==3);
            Pass("all_three_original_owner_source_validated",
                BracingPhysicalContract.InputError(input)==null);
            Pass("authoritative_original_grid_is_550mm",
                PartSizeGrid.GridMeters==.55);
            Pass("actual_original_physical_owners_43",input.rigs.Sum(r=>r.parts.Length)==43);
            Pass("five_added_structural_armor_owners",
                input.rigs.SelectMany(r=>r.parts).Count(p=>p.localId.StartsWith("P17-BRACE-"))==5);
            Pass("six_kind_binding_syntax_required",
                input.bindings.Length==6);
            Pass("original_source_truth_remains_unmeasured",
                input.rigs.All(r=>r.nativePhysicalStatus=="UNMEASURED_COLLIDER_AND_SOCKET"));
            Pass("synthetic_binding_is_only_schema_not_native",
                input.nativeEvidence=="NOT_RUN" &&
                input.bindings.All(b=>b.assetPath.Contains("FAKE_SOURCE_SCHEMA_ONLY")));
            Reject("false_native_pass_blocked",d=>d.nativeEvidence="PASS");
            Reject("missing_prefab_binding_blocked",d=>d.bindings=d.bindings.Take(5).ToArray());
            Reject("duplicate_kind_binding_blocked",d=>d.bindings[0].kind=d.bindings[1].kind);
            Reject("duplicate_unity_guid_blocked",d=>d.bindings[0].assetGuid=d.bindings[1].assetGuid);
            Reject("empty_asset_path_blocked",d=>d.bindings[2].assetPath="");
            Reject("directory_path_traversal_blocked",d=>d.bindings[2].assetPath="Assets/../somewhere.prefab");
            Reject("external_source_prefab_path_blocked",d=>d.bindings[2].assetPath="Packages/Another.prefab");
            Reject("untrusted_non_prefab_blocked",d=>d.bindings[2].assetPath="Assets/mything.scene");
            Reject("fake_short_guid_blocked",d=>d.bindings[2].assetGuid="bad");
            Reject("uppercase_guid_cannot_be_assumed_original_blocked",d=>d.bindings[2].assetGuid=new string('A',32));
            Reject("wrong_original_producer_commit_blocked",d=>d.originalBracingSha=new string('f',40));
            Reject("unknown_content_schema_blocked",d=>d.rigs[0].schema="something");
            Reject("wrong_frozen_partsize_commit_blocked",d=>d.rigs[2].sourcePartSizeCommit=new string('f',40));
            Reject("forged_rig_native_status_blocked",d=>d.rigs[2].nativePhysicalStatus="PASS");
            Reject("duplicated_part_id_blocked",d=>d.rigs[0].parts[3].localId=d.rigs[0].parts[4].localId);
            Reject("two_physical_owners_same_cell_blocked",d=>d.rigs[0].parts[3].cell=Copy(d.rigs[0].parts[4].cell));
            Reject("invalid_core_count_blocked",d=>d.rigs[0].parts[0].kind="Armor");
            Reject("missing_author_owned_bracing_part_blocked",d=>d.rigs[2].parts=d.rigs[2].parts.Skip(1).ToArray());
            Reject("wrong_fixed_armor_variant_count_blocked",d=>d.rigs[1].parts=d.rigs[1].parts.Take(15).ToArray());
            Reject("unapproved_kind_blocked",d=>d.rigs[1].parts[4].kind="NewWeaponPartKind");
            Reject("unsafe_movement_undefined_yaw_blocked",d=>d.rigs[2].parts[8].rotationQuarterTurns.y=1);
            Reject("empty_physical_contact_mask_blocked",d=>d.rigs[1].parts[6].contactMask=0);
            Reject("unconnected_original_armor_blocked",d=>{var p=d.rigs[0].parts.Last();p.cell.x=80;});
            Reject("fake_bracing_part_material_blocked",d=>d.rigs[0].parts.Last().kind="Gun");
            Reject("bracing_owner_id_wrong_blocked",d=>d.rigs[2].parts.Last().localId="P17-BRACE-INVENTED");
            Reject("untrusted_extra_fake_rig_blocked",d=>d.rigs=new[]{d.rigs[0],d.rigs[1],d.rigs[2],d.rigs[2]});
            Reject("unapproved_contact_kind_missing_blocked",d=>d.bindings[0].kind="Foo");
            Pass("baseline_unmeasured_wheel_never_claims_physx",
                PartSizeGrid.DefinitionError(PartSizeBaselineCatalog.ForSourceKind("Wheel"))
                =="UNMEASURED_PHYSICAL_ENVELOPE");
            Pass("original_brace_armor_source_has_52cm_cube",
                PartSizeGrid.DefinitionError(PartSizeBaselineCatalog.ForSourceKind("Armor"))==null);
            Console.WriteLine("B01_23_P17_NATIVE_PROBE_ORIGINAL_CORE_PASS "+checks+"/"+checks+
                " ACTUAL_ORIGINAL_CSHARP_SOURCE; UNITY_NATIVE_NOT_RUN; SYNTHETIC_PREFAB_IDS_NOT_APPROVED");
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("B01_23_P17_NATIVE_PROBE_SOURCE_FAIL "+e);
            return 1;
        }
    }
}

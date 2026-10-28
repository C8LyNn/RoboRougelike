// Original source-backed executable P17 W02 bracing variants and mutation gate.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.P17Rigs;
using B01.P17Weakpoints;

namespace B01.P17Bracing;

internal static class Program
{
    private const string Root="Assets/PARTSIZE001";
    private const string KitsPath=Root+"/Bracing/P17/P17_StructuralBracingKits.json";
    private static readonly string[] Original={"P17_Flanker_Wheels.json","P17_Bulwark_Tracks.json","P17_Skirmisher_Hover.json"};
    private static readonly string[] Variant={"P17_Flanker_Reinforced.json","P17_Bulwark_Reinforced.json","P17_Skirmisher_Reinforced.json"};
    private static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive=true};
    private static int passed;
    private static void Check(string name,bool outcome)
    {
        if(!outcome) throw new Exception("BRACING_SOURCE_TEST_FAIL:"+name);
        passed++;
        Console.WriteLine("PASS "+name);
    }
    private static T Read<T>(string path)
    {
        if(!File.Exists(path))throw new InvalidDataException("SOURCE_FILE_MISSING:"+path);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path),Json) ??
            throw new InvalidDataException("INVALID_JSON_FILE:"+path);
    }
    private static (BracingKits packs,P17Rig[] original,P17Rig[] variants) Load()
    {
        var kits=Read<BracingKits>(KitsPath);
        var original=Original.Select(n=>RigValidator.Read(Root+"/Blueprints/P17/"+n)).ToArray();
        var variant=Variant.Select(n=>RigValidator.Read(Root+"/Bracing/P17/"+n)).ToArray();
        return (kits,original,variant);
    }
    private static string Fingerprint(object data)=>JsonSerializer.Serialize(data,Json);
    private static bool Rejects(Action action)
    {
        try{action();return false;}
        catch(ArgumentException){return true;}
    }
    private static void Prove(BracingKits pack,P17Rig[] originals,P17Rig[] variants,bool selftest)
    {
        Check("original_source_unchanged_three_base_rigs",
            originals.Length==3 && originals.All(r=>RigValidator.Validate(r) is null));
        Check("exact_kit_schema_and_source_sha",BracingEngine.DataError(pack) is null);
        Check("three_unique_game_content_bracing_variants",
            variants.Length==3&&variants.Select(v=>v.BlueprintId).Distinct().Count()==3);
        Check("five_new_real_armor_modules",
            pack.Kits.Sum(k=>k.MandatoryParts.Count)==5);
        Check("six_explicit_vulnerability_targets",
            pack.Kits.Sum(k=>k.ExpectedMitigations.Count)==6);
        Check("original_partsize_source_550mm_grid",
            B01.PARTSIZE001.PartSizeGrid.GridMeters==.55);
        for(int i=0;i<3;i++)
        {
            var kit=pack.Kits[i];
            var old=originals[i];
            var present=variants[i];
            var recomputed=BracingEngine.Apply(old,kit);
            Check("variant_exact_original_generation_"+i,Fingerprint(recomputed)==Fingerprint(present));
            Check("original_owners_preserved_exact_"+i,
                old.Parts.SequenceEqual(present.Parts.Take(old.Parts.Count),
                    new OriginalPartEqual()));
            Check("new_armor_individual_owners_"+i,
                present.Parts.Count==old.Parts.Count+kit.MandatoryParts.Count&&
                kit.MandatoryParts.All(p=>p.Kind=="Armor"&&p.Weapon is null));
            Check("source_derived_connected_game_layout_"+i,
                RigValidator.Validate(present) is null);
            foreach(var target in kit.ExpectedMitigations)
            {
                Check("removed_weakpoint_target_"+i+"_"+target.RemovedPartId,
                    BracingEngine.MitigationVerified(old,present,target));
            }
            // Each surviving original physical part must stay core-connected
            // at least whenever it was core-connected before reinforcement.
            for(int oldIndex=0;oldIndex<old.Parts.Count;oldIndex++)
            {
                var before=WeakpointEngine.Simulate(old,oldIndex);
                var after=WeakpointEngine.Simulate(present,oldIndex);
                Check("never_degrade_existing_survivors_"+i+"_"+oldIndex,
                    before.DetachedPartIds.All(id=>old.Parts.Any(p=>p.LocalId==id))&&
                    after.DetachedPartIds.Where(id=>old.Parts.Any(p=>p.LocalId==id))
                         .All(id=>before.DetachedPartIds.Contains(id)) &&
                    (!before.SelectedDriveOperable||after.SelectedDriveOperable) &&
                    after.ConnectedGunCount>=before.ConnectedGunCount);
            }
            Check("native_unknown_never_claimed_"+i,
                present.NativePhysicalStatus=="UNMEASURED_COLLIDER_AND_SOCKET");
        }
        if(!selftest)return;
        var dummy=BracingEngine.Copy(pack);
        dummy.NativePhysicalStatus="PASS";
        Check("fake_native_physics_pass_rejected",BracingEngine.DataError(dummy) is not null);
        dummy=BracingEngine.Copy(pack);
        dummy.Kits[0].MandatoryParts[0].Kind="Gun";
        Check("unapproved_weapon_in_brace_rejected",BracingEngine.DataError(dummy) is not null);
        dummy=BracingEngine.Copy(pack);
        dummy.Kits[1].MandatoryParts[0].ContactMask=0;
        Check("unapproved_socket_mask_rejected",BracingEngine.DataError(dummy) is not null);
        dummy=BracingEngine.Copy(pack);
        dummy.Kits[2].ExpectedMitigations.Add(dummy.Kits[2].ExpectedMitigations[0]);
        Check("duplicate_mitigation_rejected",BracingEngine.DataError(dummy) is not null);
        dummy=BracingEngine.Copy(pack);
        Check("wrong_source_rig_identity_rejected",Rejects(()=>BracingEngine.Apply(originals[2],dummy.Kits[0])));
        dummy=BracingEngine.Copy(pack);
        dummy.Kits[0].MandatoryParts[0].Cell=new P17Point();
        Check("overlapping_extra_armor_rejected",Rejects(()=>BracingEngine.Apply(originals[0],dummy.Kits[0])));
        dummy=BracingEngine.Copy(pack);
        dummy.Kits[0].MandatoryParts[0].LocalId=originals[0].Parts[0].LocalId;
        Check("duplicate_physical_owner_id_rejected",Rejects(()=>BracingEngine.Apply(originals[0],dummy.Kits[0])));
        dummy=BracingEngine.Copy(pack);
        dummy.OriginalWeakpointCommit=new string('f',40);
        Check("source_sha_forgery_rejected",BracingEngine.DataError(dummy) is not null);
        dummy=BracingEngine.Copy(pack);
        dummy.Kits[0].MandatoryParts[0].Cell.X=9;
        Check("disconnected_bracing_module_rejected",Rejects(()=>BracingEngine.Apply(originals[0],dummy.Kits[0])));
        Check("original_base_rigs_still_valid",originals.All(r=>RigValidator.Validate(r) is null));
    }
    private sealed class OriginalPartEqual:IEqualityComparer<P17Part>
    {
        public bool Equals(P17Part? a,P17Part? b)=>
            a is not null&&b is not null&&Fingerprint(a)==Fingerprint(b);
        public int GetHashCode(P17Part obj)=>StringComparer.Ordinal.GetHashCode(Fingerprint(obj));
    }
    private static int Main(string[] args)
    {
        if(args.Length!=1||!new[]{"--selftest","--check","--preview"}.Contains(args[0]))
        {
            Console.Error.WriteLine("Use --selftest / --check / --preview (no Unity integration).");
            return 2;
        }
        try
        {
            var (kits,originals,variants)=Load();
            if(args[0]=="--preview")
            {
                if(BracingEngine.DataError(kits) is string err)
                    throw new InvalidDataException("SOURCE_KIT_UNTRUSTED:"+err);
                for(int i=0;i<3;i++)
                {
                    var before=WeakpointEngine.Analyze(originals[i]);
                    var after=WeakpointEngine.Analyze(variants[i]);
                    Console.WriteLine("RIG "+kits.Kits[i].VariantId+" original="+originals[i].Parts.Count+
                        " reinforced="+variants[i].Parts.Count);
                    foreach(var goal in kits.Kits[i].ExpectedMitigations)
                    {
                        var old=before.SingleBreakCases.Single(c=>c.RemovedPartId==goal.RemovedPartId);
                        var now=after.SingleBreakCases.Single(c=>c.RemovedPartId==goal.RemovedPartId);
                        Console.WriteLine("  DESTROY "+goal.RemovedPartId+
                            " -> previously lost "+goal.PreviouslyOrphanedPartId+
                            " ; old_orphan_count="+old.DetachedCount+
                            " new_orphan_count="+now.DetachedCount+
                            " rescued="+!now.DetachedPartIds.Contains(goal.PreviouslyOrphanedPartId));
                    }
                }
                Console.WriteLine("B01_23_BRACING_PREVIEW_SOURCE_ONLY_UNITY_PHYSX_NOT_RUN");
                return 0;
            }
            Prove(kits,originals,variants,args[0]=="--selftest");
            Console.WriteLine("B01_23_W02_BRACING_PASS "+passed+"/"+passed+
                " ACTUAL_FROZEN_SOURCE_CSHARP; NATIVE_UNITY_NOT_RUN; CODEX_NOT_INTEGRATED");
            return 0;
        }
        catch(Exception e)
        {
            Console.Error.WriteLine("B01_23_W02_BRACING_FAIL: "+e);
            return 1;
        }
    }
}

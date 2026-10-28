// B01-23 P17 W02. New GAME CONTENT: structural redundancy bracing kits for 3 original rigs.
// Compiles the unchanged original PartSizeContract and P17 single-owner loss engine.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using B01.P17Rigs;
using B01.P17Weakpoints;

namespace B01.P17Bracing;

public sealed class BracingKits
{
    public string Schema {get;set;}="";
    public string PackageId {get;set;}="";
    public string OriginalRigBlueprintCommit {get;set;}="";
    public string OriginalWeakpointCommit {get;set;}="";
    public string OriginalPartSizeCommit {get;set;}="";
    public string NativePhysicalStatus {get;set;}="";
    public string GameIntegrationStatus {get;set;}="";
    public List<BracingKit> Kits {get;set;}=new();
}
public sealed class BracingKit
{
    public string KitId {get;set;}="";
    public string BaseBlueprintId {get;set;}="";
    public string VariantId {get;set;}="";
    public string Title {get;set;}="";
    public List<P17Part> MandatoryParts {get;set;}=new();
    public List<Mitigation> ExpectedMitigations {get;set;}=new();
}
public sealed class Mitigation
{
    public string RemovedPartId {get;set;}="";
    public string PreviouslyOrphanedPartId {get;set;}="";
}
public static class BracingEngine
{
    public const string RigSHA="0978497561c8b6400490235d3e5a401ac32bea2d";
    public const string WeakpointSHA="098ccdbe8f129253fa4ada52318436b7cc2b4dc4";
    public const string PartsizeSHA="be2be0e4802bc3f450179901f9ae91c6d84bcb5b";
    public const string Suffix=" Dodatkowe panele pancerza tworzą alternatywne połączenia między modułami.";
    private static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive=true};

    public static T Copy<T>(T obj) where T:class =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj,Json),Json) ??
            throw new ArgumentException("CLONE_FAILED");

    public static string? DataError(BracingKits? data)
    {
        if(data is null || data.Schema!="B01-P17-W02-STRUCTURAL-BRACING-KITS-v1" ||
            data.PackageId!="PARTSIZE001-P17-W02-STRUCTURAL-BRACING-R1" ||
            data.OriginalRigBlueprintCommit!=RigSHA ||
            data.OriginalWeakpointCommit!=WeakpointSHA ||
            data.OriginalPartSizeCommit!=PartsizeSHA ||
            data.NativePhysicalStatus!="NOT_RUN" ||
            data.GameIntegrationStatus!="NOT_INTEGRATED" ||
            data.Kits is null || data.Kits.Count!=3)
            return "WRONG_PROVENANCE_OR_KIT_COUNT";
        var names=new HashSet<string>(StringComparer.Ordinal);
        var bases=new HashSet<string>(StringComparer.Ordinal);
        var variants=new HashSet<string>(StringComparer.Ordinal);
        int extras=0,mitigations=0;
        foreach(var kit in data.Kits)
        {
            if(kit is null ||
               string.IsNullOrWhiteSpace(kit.Title) ||
               string.IsNullOrWhiteSpace(kit.KitId) ||
               !names.Add(kit.KitId) || !bases.Add(kit.BaseBlueprintId) ||
               !variants.Add(kit.VariantId) ||
               kit.BaseBlueprintId==kit.VariantId ||
               kit.MandatoryParts is null || kit.MandatoryParts.Count<1 ||
               kit.MandatoryParts.Count>2 ||
               kit.ExpectedMitigations is null || kit.ExpectedMitigations.Count<1)
                return "INVALID_KIT_IDENTIFIERS_OR_SIZE";
            extras+=kit.MandatoryParts.Count;
            mitigations+=kit.ExpectedMitigations.Count;
            foreach(var part in kit.MandatoryParts)
            {
                if(part is null || string.IsNullOrWhiteSpace(part.LocalId) ||
                   part.Kind!="Armor" || part.Cell is null ||
                   part.RotationQuarterTurns is null ||
                   part.RotationQuarterTurns.X!=0||part.RotationQuarterTurns.Y!=0||
                   part.RotationQuarterTurns.Z!=0 ||
                   part.ContactMask!=63 || part.Weapon is not null)
                    return "UNAPPROVED_PART_OR_CONTACT_SETTINGS";
            }
            if(kit.ExpectedMitigations.Any(m=>m is null ||
                string.IsNullOrWhiteSpace(m.RemovedPartId) ||
                string.IsNullOrWhiteSpace(m.PreviouslyOrphanedPartId) ||
                m.RemovedPartId==m.PreviouslyOrphanedPartId))
                return "MALFORMED_MITIGATION";
            if(kit.ExpectedMitigations.Select(m=>m.RemovedPartId+"\u0001"+m.PreviouslyOrphanedPartId)
                .Distinct(StringComparer.Ordinal).Count()!=kit.ExpectedMitigations.Count)
                return "DUPLICATE_MITIGATION";
        }
        if(extras!=5||mitigations!=6)
            return "CHANGED_AUTHORED_BRACING_SCOPE";
        return null;
    }

    public static P17Rig Apply(P17Rig baseline,BracingKit kit)
    {
        if(RigValidator.Validate(baseline) is string error)
            throw new ArgumentException("INVALID_BASE_RIG:"+error);
        if(kit is null || baseline.BlueprintId!=kit.BaseBlueprintId ||
           string.IsNullOrWhiteSpace(kit.VariantId) || kit.VariantId==kit.BaseBlueprintId)
            throw new ArgumentException("KIT_SOURCE_MISMATCH");
        var next=Copy(baseline);
        next.BlueprintId=kit.VariantId;
        next.DisplayName+=" / Reinforced";
        next.Description+=Suffix;
        next.Parts.AddRange(kit.MandatoryParts.Select(Copy));
        if(RigValidator.Validate(next) is string invalid)
            throw new ArgumentException("INVALID_BRACED_RIG:"+invalid);
        return next;
    }

    public static bool MitigationVerified(P17Rig original,P17Rig variant,Mitigation planned)
    {
        var removedIndex=original.Parts.FindIndex(p=>p.LocalId==planned.RemovedPartId);
        if(removedIndex<0) return false;
        var variantIndex=variant.Parts.FindIndex(p=>p.LocalId==planned.RemovedPartId);
        if(variantIndex<0)return false;
        var before=WeakpointEngine.Simulate(original,removedIndex);
        var after=WeakpointEngine.Simulate(variant,variantIndex);
        return before.DetachedPartIds.Contains(planned.PreviouslyOrphanedPartId) &&
            !after.DetachedPartIds.Contains(planned.PreviouslyOrphanedPartId) &&
            !after.CoreLost;
    }
}

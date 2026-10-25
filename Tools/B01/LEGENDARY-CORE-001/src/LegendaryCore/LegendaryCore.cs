using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RoboRougelike.B01.LegendaryCore;

public sealed record Footprint(int Width, int Height, int Depth);
public sealed record LegendaryDefinition(
    string Id, string DisplayName, string BasePartKind, string? WeaponFamily,
    Footprint Footprint, double MassKg, string MountTag, string WeakSide,
    string ProposedEffect, bool EffectImplemented);

internal static class Serialization
{
    internal static readonly JsonSerializerOptions Options = new()
    { PropertyNameCaseInsensitive = true, WriteIndented = true };
}

public sealed class LegendaryCatalog
{
    private static readonly HashSet<string> Kinds = new(StringComparer.Ordinal)
    {
        "Armor","Wheel","Gun","Core","Suspension","Booster","JumpDrive","Blink",
        "Gyroscope","OmniDrive","MagneticGrapple","HoverEngine","CrawlerTrack"
    };
    private readonly Dictionary<string, LegendaryDefinition> definitions = new(StringComparer.Ordinal);
    public IReadOnlyList<LegendaryDefinition> Definitions { get; }

    private LegendaryCatalog(LegendaryDefinition[] input)
    {
        if (input.Length == 0) throw new InvalidDataException("CATALOG_EMPTY");
        foreach (var d in input)
        {
            if (d is null || string.IsNullOrWhiteSpace(d.Id) ||
                !Regex.IsMatch(d.Id, @"^LEG-[A-Z]{3}-[0-9]{3}$") ||
                string.IsNullOrWhiteSpace(d.DisplayName) ||
                !ValidKind(d.BasePartKind ?? "") ||
                (d.BasePartKind == "Gun") != !string.IsNullOrWhiteSpace(d.WeaponFamily) ||
                d.Footprint is null ||
                d.Footprint.Width < 1 || d.Footprint.Height < 1 || d.Footprint.Depth < 1 ||
                d.Footprint.Width > 16 || d.Footprint.Height > 16 || d.Footprint.Depth > 16 ||
                !double.IsFinite(d.MassKg) || d.MassKg <= 0 ||
                string.IsNullOrWhiteSpace(d.MountTag) || string.IsNullOrWhiteSpace(d.WeakSide) ||
                string.IsNullOrWhiteSpace(d.ProposedEffect))
                throw new InvalidDataException("CATALOG_INVALID_DEFINITION");
            if (!definitions.TryAdd(d.Id, d))
                throw new InvalidDataException("CATALOG_DUPLICATE_DEFINITION_ID:" + d.Id);
        }
        Definitions = Array.AsReadOnly(input);
    }

    public static LegendaryCatalog FromJson(string json) =>
        new(JsonSerializer.Deserialize<LegendaryDefinition[]>(json, Serialization.Options)
            ?? throw new InvalidDataException("CATALOG_NULL"));

    public LegendaryDefinition? Find(string? id) =>
        id is not null && definitions.TryGetValue(id, out var value) ? value : null;

    public static bool ValidKind(string kind) => Kinds.Contains(kind) && kind != "Core";
}

// PhysicalPart.InstanceId maps to R3PartState.Id. One owned dictionary, no parallel
// production inventory: RobotId==null represents the existing R3 inventory concept.
public sealed record PhysicalPart(
    string InstanceId, string? LegendaryDefinitionId, string BasePartKind,
    string? WeaponFamily, double Health, string? RobotId);
public sealed record GarageSnapshot(
    string[] RobotIds, PhysicalPart[] Parts, string[] AppliedRewardEventIds);

public sealed class LegendaryGarage
{
    private readonly LegendaryCatalog catalog;
    private readonly HashSet<string> robots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PhysicalPart> parts = new(StringComparer.Ordinal);
    private readonly HashSet<string> rewardEvents = new(StringComparer.Ordinal);

    public LegendaryGarage(LegendaryCatalog catalog) =>
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    public IReadOnlyList<PhysicalPart> Owned =>
        parts.Values.OrderBy(p => p.InstanceId, StringComparer.Ordinal).ToArray();
    public IReadOnlyList<PhysicalPart> Inventory =>
        Owned.Where(p => p.RobotId is null).ToArray();
    public IReadOnlyList<PhysicalPart> Equipped(string robotId) =>
        Owned.Where(p => p.RobotId == robotId).ToArray();

    public static PhysicalPart NewLegendary(LegendaryDefinition d, string id) =>
        new(id, d.Id, d.BasePartKind, d.WeaponFamily, 1, null);
    public static PhysicalPart NewOrdinary(string kind, string? weaponFamily, string id) =>
        new(id, null, kind, weaponFamily, 1, null);

    public bool RegisterRobot(string robotId) =>
        !string.IsNullOrWhiteSpace(robotId) && robots.Add(robotId);

    // Atomic batch+event: duplicates of TYPE are legal, duplicates of physical ID are not.
    public bool TryAcquire(string eventId, IEnumerable<PhysicalPart> batch, out string error)
    {
        if (string.IsNullOrWhiteSpace(eventId) || batch is null)
        { error = "INVALID_REWARD"; return false; }
        if (rewardEvents.Contains(eventId))
        { error = "REWARD_ALREADY_APPLIED"; return false; }
        var incoming = batch.ToArray();
        if (incoming.Length == 0 || incoming.Any(p =>
                ValidatePart(p) is not null || p.RobotId is not null ||
                parts.ContainsKey(p.InstanceId)) ||
            incoming.Select(p => p.InstanceId).Distinct(StringComparer.Ordinal).Count() != incoming.Length)
        { error = "INVALID_OR_DUPLICATE_PHYSICAL_INSTANCE"; return false; }
        foreach (var p in incoming) parts.Add(p.InstanceId, p);
        rewardEvents.Add(eventId);
        error = ""; return true;
    }

    public bool CanEquip(string robotId, string instanceId, out string error)
    {
        if (!robots.Contains(robotId ?? ""))
        { error = "UNKNOWN_ROBOT"; return false; }
        if (!parts.TryGetValue(instanceId ?? "", out var p))
        { error = "PART_NOT_OWNED"; return false; }
        if (p.RobotId is not null)
        { error = "PART_ALREADY_EQUIPPED"; return false; }
        if (p.Health <= 0)
        { error = "PART_DESTROYED"; return false; }
        if (p.LegendaryDefinitionId is not null &&
            parts.Values.Any(q => q.RobotId == robotId &&
              q.LegendaryDefinitionId == p.LegendaryDefinitionId))
        { error = "LEGEND_DUPLICATE_EQUIPPED"; return false; }
        if (p.BasePartKind == "Gun")
        {
            var families = parts.Values.Where(q => q.RobotId == robotId && q.BasePartKind == "Gun")
                .Select(q => q.WeaponFamily!).ToHashSet(StringComparer.Ordinal);
            families.Add(p.WeaponFamily!);
            if (families.Count > 2)
            { error = "WEAPON_FAMILY_LIMIT_2"; return false; }
        }
        error = ""; return true;
    }

    public bool TryEquip(string robotId, string id, out string error)
    {
        if (!CanEquip(robotId, id, out error)) return false;
        parts[id] = parts[id] with { RobotId = robotId };
        return true;
    }

    public bool TryUnequip(string robotId, string id, out string error)
    {
        if (!parts.TryGetValue(id ?? "", out var p) || p.RobotId != robotId)
        { error = "PART_NOT_EQUIPPED_HERE"; return false; }
        parts[id] = p with { RobotId = null };
        error = ""; return true;
    }

    public bool TrySetHealth(string id, double health, out string error)
    {
        if (!parts.TryGetValue(id ?? "", out var p) || !double.IsFinite(health) ||
            health < 0 || health > 1)
        { error = "INVALID_PART_OR_HEALTH"; return false; }
        parts[id] = p with { Health = health };
        error = ""; return true;
    }

    public bool TryRepair(string id, out string error)
    {
        if (!parts.TryGetValue(id ?? "", out var p))
        { error = "PART_NOT_OWNED"; return false; }
        // State only: R3 RepairCost, affordability and rebuilding remain authoritative.
        parts[id] = p with { Health = 1 };
        error = ""; return true;
    }

    public string SaveJson() => JsonSerializer.Serialize(
        new GarageSnapshot(robots.Order(StringComparer.Ordinal).ToArray(),
            Owned.ToArray(), rewardEvents.Order(StringComparer.Ordinal).ToArray()),
        Serialization.Options);

    public static LegendaryGarage LoadJson(LegendaryCatalog catalog, string json)
    {
        var snapshot = JsonSerializer.Deserialize<GarageSnapshot>(json, Serialization.Options)
            ?? throw new InvalidDataException("SAVE_NULL");
        if (snapshot.RobotIds is null || snapshot.Parts is null ||
            snapshot.AppliedRewardEventIds is null)
            throw new InvalidDataException("SAVE_MISSING_FIELDS");
        var state = new LegendaryGarage(catalog);
        foreach (var id in snapshot.RobotIds)
            if (!state.RegisterRobot(id)) throw new InvalidDataException("SAVE_DUPLICATE_ROBOT");
        foreach (var id in snapshot.AppliedRewardEventIds)
            if (string.IsNullOrWhiteSpace(id) || !state.rewardEvents.Add(id))
                throw new InvalidDataException("SAVE_DUPLICATE_REWARD_EVENT");
        foreach (var p in snapshot.Parts)
            if (state.ValidatePart(p) is not null || !state.parts.TryAdd(p.InstanceId, p))
                throw new InvalidDataException("SAVE_INVALID_OR_DUPLICATE_PART");
        if (!state.CheckInvariants(out var error))
            throw new InvalidDataException("SAVE_INVARIANT:" + error);
        return state;
    }

    public bool CheckInvariants(out string error)
    {
        foreach (var p in parts.Values)
        {
            var reason = ValidatePart(p);
            if (reason is not null) { error = reason; return false; }
            if (p.RobotId is not null && !robots.Contains(p.RobotId))
            { error = "UNKNOWN_EQUIPPED_ROBOT"; return false; }
        }
        foreach (var robotId in robots)
        {
            var mounted = parts.Values.Where(p => p.RobotId == robotId).ToArray();
            if (mounted.Where(p => p.LegendaryDefinitionId is not null)
                .GroupBy(p => p.LegendaryDefinitionId, StringComparer.Ordinal)
                .Any(g => g.Count() > 1))
            { error = "LEGEND_DUPLICATE_EQUIPPED"; return false; }
            if (mounted.Where(p => p.BasePartKind == "Gun").Select(p => p.WeaponFamily)
                .Distinct(StringComparer.Ordinal).Count() > 2)
            { error = "WEAPON_FAMILY_LIMIT_2"; return false; }
        }
        error = ""; return true;
    }

    private string? ValidatePart(PhysicalPart? p)
    {
        if (p is null || !Guid.TryParseExact(p.InstanceId, "N", out var parsed) ||
            p.InstanceId != parsed.ToString("N") ||
            !double.IsFinite(p.Health) || p.Health < 0 || p.Health > 1 ||
            !LegendaryCatalog.ValidKind(p.BasePartKind ?? ""))
            return "INVALID_PHYSICAL_PART";
        if ((p.BasePartKind == "Gun") != !string.IsNullOrWhiteSpace(p.WeaponFamily))
            return "INVALID_WEAPON_FAMILY";
        if (p.LegendaryDefinitionId is null) return null;
        var def = catalog.Find(p.LegendaryDefinitionId);
        return def is not null && def.BasePartKind == p.BasePartKind &&
            def.WeaponFamily == p.WeaponFamily ? null : "UNKNOWN_OR_MISMATCHED_LEGEND";
    }
}

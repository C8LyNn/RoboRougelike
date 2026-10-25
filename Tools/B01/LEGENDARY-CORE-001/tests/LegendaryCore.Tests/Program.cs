using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using RoboRougelike.B01.LegendaryCore;

internal static class Program
{
    private sealed record Result(string Name, string Status, string? Error);
    private static readonly List<Result> Results = new();
    private static int serial;
    private static LegendaryCatalog Catalog = null!;
    private static LegendaryDefinition Weapon = null!;
    private static LegendaryDefinition Hover = null!;

    private static string Id() => (++serial).ToString("x32");
    private static void Require(bool value, string description = "assertion failed")
    {
        if (!value) throw new Exception(description);
    }
    private static void Reject(bool value, string error, string expected)
    {
        Require(!value, "operation incorrectly accepted");
        Require(error == expected, "expected " + expected + " got " + error);
    }
    private static void Test(string name, Action body)
    {
        try { body(); Results.Add(new Result(name, "PASS", null)); Console.WriteLine("PASS " + name); }
        catch (Exception e)
        {
            Results.Add(new Result(name, "FAIL", e.GetType().Name + ": " + e.Message));
            Console.Error.WriteLine("FAIL " + name + ": " + e.Message);
        }
    }
    private static void Invalid(Action action)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new Exception("Expected InvalidDataException");
    }
    private static LegendaryGarage Garage(params string[] robotIds)
    {
        var g = new LegendaryGarage(Catalog);
        foreach (var robot in robotIds) Require(g.RegisterRobot(robot));
        return g;
    }
    private static PhysicalPart Legend(LegendaryDefinition def) =>
        LegendaryGarage.NewLegendary(def, Id());
    private static PhysicalPart Gun(string family) =>
        LegendaryGarage.NewOrdinary("Gun", family, Id());
    private static void Grant(LegendaryGarage g, PhysicalPart part)
    {
        Require(g.TryAcquire("reward-" + part.InstanceId, new[] { part }, out var error), error);
    }
    private static void Equip(LegendaryGarage g, string robot, PhysicalPart part)
    {
        Require(g.TryEquip(robot, part.InstanceId, out var error), error);
    }
    private static void Safe(LegendaryGarage g)
    {
        Require(g.CheckInvariants(out var error), error);
        Require(g.Owned.Select(p => p.InstanceId).Distinct(StringComparer.Ordinal).Count() == g.Owned.Count);
    }
    private static string CatalogPath => Path.Combine(AppContext.BaseDirectory, "LEGENDARY_CATALOG_R1.json");

    private static int Main(string[] args)
    {
        string report = "test-results.json";
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--report" && i + 1 < args.Length) report = args[++i];
        string data;
        try
        {
            data = File.ReadAllText(CatalogPath);
            Catalog = LegendaryCatalog.FromJson(data);
            Weapon = Catalog.Find("LEG-WPN-001") ?? throw new Exception("missing Przebijacz");
            Hover = Catalog.Find("LEG-MOV-001") ?? throw new Exception("missing Serce Zawisu");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("SETUP FAIL: " + e);
            return 2;
        }

        Test("catalog_two_stable_ids", () =>
        {
            Require(Catalog.Definitions.Count == 2);
            Require(Weapon.BasePartKind == "Gun" && Weapon.WeaponFamily == "Railgun");
            Require(Hover.BasePartKind == "HoverEngine" && Hover.WeaponFamily is null);
            Require(Weapon.MassKg > 0 && Hover.Footprint.Width >= 3);
            Require(!Weapon.EffectImplemented && !Hover.EffectImplemented);
        });
        Test("catalog_duplicate_definition_rejected", () =>
            Invalid(() => LegendaryCatalog.FromJson(data.Replace("LEG-MOV-001", "LEG-WPN-001"))));
        Test("catalog_invalid_mass_rejected", () =>
            Invalid(() => LegendaryCatalog.FromJson(data.Replace("\"MassKg\": 220.0", "\"MassKg\": 0.0"))));
        Test("first_legend_mount", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); Grant(g, a);
            Require(g.CanEquip("A", a.InstanceId, out _));
            Equip(g, "A", a); Require(g.Inventory.Count == 0 && g.Equipped("A").Count == 1);
            Safe(g);
        });
        Test("duplicate_same_legend_on_one_robot_atomic", () =>
        {
            var g = Garage("A"); var first = Legend(Weapon); var second = Legend(Weapon);
            Grant(g, first); Grant(g, second); Equip(g, "A", first);
            var before = g.SaveJson();
            Reject(g.TryEquip("A", second.InstanceId, out var error), error, "LEGEND_DUPLICATE_EQUIPPED");
            Require(before == g.SaveJson() && g.Inventory.Single().InstanceId == second.InstanceId);
            Safe(g);
        });
        Test("different_legend_types_together", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); var b = Legend(Hover);
            Grant(g, a); Grant(g, b); Equip(g, "A", a); Equip(g, "A", b);
            Require(g.Equipped("A").Count == 2); Safe(g);
        });
        Test("same_definition_other_robot_allowed", () =>
        {
            var g = Garage("A", "B"); var a = Legend(Weapon); var b = Legend(Weapon);
            Grant(g, a); Grant(g, b); Equip(g, "A", a); Equip(g, "B", b);
            Require(g.Equipped("A").Count == 1 && g.Equipped("B").Count == 1);
            Safe(g);
        });
        Test("inventory_can_store_extra_copies", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); var b = Legend(Weapon);
            Grant(g, a); Grant(g, b);
            Require(g.Inventory.Count == 2 && a.InstanceId != b.InstanceId);
            Equip(g, "A", a); Require(g.Inventory.Count == 1); Safe(g);
        });
        Test("zero_hp_mounted_still_occupies_slot", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); var b = Legend(Weapon);
            Grant(g, a); Grant(g, b); Equip(g, "A", a);
            Require(g.TrySetHealth(a.InstanceId, 0, out _));
            Reject(g.TryEquip("A", b.InstanceId, out var error), error, "LEGEND_DUPLICATE_EQUIPPED");
            Require(g.Equipped("A").Single().Health == 0); Safe(g);
        });
        Test("repair_preserves_physical_instance_and_type", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); Grant(g, a); Equip(g, "A", a);
            Require(g.TrySetHealth(a.InstanceId, 0, out _));
            Require(g.TryRepair(a.InstanceId, out _));
            var repaired = g.Equipped("A").Single();
            Require(repaired.InstanceId == a.InstanceId && repaired.LegendaryDefinitionId == Weapon.Id);
            Require(repaired.Health == 1 && g.Owned.Count == 1); Safe(g);
        });
        Test("destroyed_unmount_repair_remount_swap", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); var b = Legend(Weapon);
            Grant(g, a); Grant(g, b); Equip(g, "A", a);
            Require(g.TrySetHealth(a.InstanceId, 0, out _));
            Require(g.TryUnequip("A", a.InstanceId, out _));
            Reject(g.TryEquip("A", a.InstanceId, out var err), err, "PART_DESTROYED");
            Equip(g, "A", b); Require(g.Inventory.Any(p => p.InstanceId == a.InstanceId));
            Require(g.TryRepair(a.InstanceId, out _));
            Require(g.TryUnequip("A", b.InstanceId, out _)); Equip(g, "A", a);
            Require(g.Owned.Count == 2); Safe(g);
        });
        Test("regular_railguns_coexist_with_legendary_railgun", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); Grant(g, a); Equip(g, "A", a);
            for (int i = 0; i < 12; i++) { var gun = Gun("Railgun"); Grant(g, gun); Equip(g, "A", gun); }
            Require(g.Equipped("A").Count == 13); Safe(g);
        });
        Test("two_weapon_families_allow_second_block_third", () =>
        {
            var g = Garage("A"); PhysicalPart a = Legend(Weapon), b = Gun("Automatic"), c = Gun("Heavy");
            Grant(g, a); Grant(g, b); Grant(g, c);
            Equip(g, "A", a); Equip(g, "A", b);
            var before = g.SaveJson();
            Reject(g.TryEquip("A", c.InstanceId, out var err), err, "WEAPON_FAMILY_LIMIT_2");
            Require(before == g.SaveJson()); Safe(g);
        });
        Test("weapon_family_release_on_unmount", () =>
        {
            var g = Garage("A"); PhysicalPart a = Legend(Weapon), b = Gun("Automatic"), c = Gun("Heavy");
            Grant(g, a); Grant(g, b); Grant(g, c); Equip(g, "A", a); Equip(g, "A", b);
            Require(g.TryUnequip("A", b.InstanceId, out _)); Equip(g, "A", c); Safe(g);
        });
        Test("physical_instance_cannot_mount_on_two_robots", () =>
        {
            var g = Garage("A", "B"); var a = Legend(Weapon); Grant(g, a);
            Equip(g, "A", a);
            Reject(g.TryEquip("B", a.InstanceId, out var e), e, "PART_ALREADY_EQUIPPED");
            Require(g.Owned.Count == 1); Safe(g);
        });
        Test("duplicate_reward_event_is_idempotently_rejected", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); Grant(g, a);
            var before = g.SaveJson();
            Reject(g.TryAcquire("reward-" + a.InstanceId, new[] { Legend(Weapon) }, out var e),
                e, "REWARD_ALREADY_APPLIED");
            Require(before == g.SaveJson() && g.Owned.Count == 1); Safe(g);
        });
        Test("duplicate_physical_id_new_event_rejected", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); Grant(g, a);
            var before = g.SaveJson();
            Reject(g.TryAcquire("another-event", new[] { a }, out var e),
                e, "INVALID_OR_DUPLICATE_PHYSICAL_INSTANCE");
            Require(before == g.SaveJson()); Safe(g);
        });
        Test("batch_duplicate_rejected_atomically", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon);
            Reject(g.TryAcquire("event", new[] { a, a }, out var e),
                e, "INVALID_OR_DUPLICATE_PHYSICAL_INSTANCE");
            Require(g.Owned.Count == 0);
            Require(g.TryAcquire("event", new[] { a }, out _)); Safe(g);
        });
        Test("invalid_ids_and_metadata_rejected", () =>
        {
            var g = Garage("A");
            Reject(g.TryAcquire("bad-1", new[] { Legend(Weapon) with { InstanceId = "INVALID" } }, out var e),
                e, "INVALID_OR_DUPLICATE_PHYSICAL_INSTANCE");
            Reject(g.TryAcquire("bad-2", new[] { Legend(Weapon) with { BasePartKind = "Wheel" } }, out e),
                e, "INVALID_OR_DUPLICATE_PHYSICAL_INSTANCE");
            var a = Legend(Weapon); Grant(g, a);
            Reject(g.TryEquip("unknown", a.InstanceId, out e), e, "UNKNOWN_ROBOT");
            Safe(g);
        });
        Test("save_load_retains_ids_locations_health_events", () =>
        {
            var g = Garage("A", "B"); var a = Legend(Weapon); var b = Legend(Hover);
            Grant(g, a); Grant(g, b); Equip(g, "A", a);
            Require(g.TrySetHealth(a.InstanceId, 0, out _));
            var saved = g.SaveJson(); var loaded = LegendaryGarage.LoadJson(Catalog, saved);
            Require(saved == loaded.SaveJson());
            Reject(loaded.TryAcquire("reward-" + a.InstanceId, new[] { Legend(Weapon) }, out var e),
                e, "REWARD_ALREADY_APPLIED");
            Require(loaded.Owned.Count == 2); Safe(loaded);
        });
        Test("load_rejects_duplicate_instance_ids", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); Grant(g, a);
            var s = JsonSerializer.Deserialize<GarageSnapshot>(g.SaveJson())!;
            var corrupt = s with { Parts = s.Parts.Append(a).ToArray() };
            Invalid(() => LegendaryGarage.LoadJson(Catalog, JsonSerializer.Serialize(corrupt)));
        });
        Test("load_rejects_illegal_equipped_same_legend", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); Grant(g, a); Equip(g, "A", a);
            var s = JsonSerializer.Deserialize<GarageSnapshot>(g.SaveJson())!;
            var b = Legend(Weapon) with { RobotId = "A", Health = 0 };
            var corrupt = s with { Parts = s.Parts.Append(b).ToArray() };
            Invalid(() => LegendaryGarage.LoadJson(Catalog, JsonSerializer.Serialize(corrupt)));
        });
        Test("load_rejects_unknown_robot", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); Grant(g, a);
            var s = JsonSerializer.Deserialize<GarageSnapshot>(g.SaveJson())!;
            var corrupt = s with { Parts = s.Parts.Select(p => p with { RobotId = "ghost" }).ToArray() };
            Invalid(() => LegendaryGarage.LoadJson(Catalog, JsonSerializer.Serialize(corrupt)));
        });
        Test("repeat_transitions_are_safe", () =>
        {
            var g = Garage("A"); var a = Legend(Weapon); Grant(g, a);
            Equip(g, "A", a);
            Reject(g.TryEquip("A", a.InstanceId, out var e), e, "PART_ALREADY_EQUIPPED");
            Require(g.TryUnequip("A", a.InstanceId, out _));
            Reject(g.TryUnequip("A", a.InstanceId, out e), e, "PART_NOT_EQUIPPED_HERE");
            Require(g.Inventory.Single().InstanceId == a.InstanceId); Safe(g);
        });
        Test("seeded_2000_step_property_regression", () =>
        {
            var g = Garage("A", "B"); var rnd = new Random(224001);
            var items = new List<PhysicalPart>();
            for (int i = 0; i < 12; i++)
            {
                var p = i % 3 == 0 ? Legend(Weapon) : i % 3 == 1 ? Legend(Hover) : Gun("Railgun");
                Grant(g, p); items.Add(p);
            }
            for (int i = 0; i < 2000; i++)
            {
                var p = items[rnd.Next(items.Count)];
                var current = g.Owned.Single(x => x.InstanceId == p.InstanceId);
                var robot = rnd.Next(2) == 0 ? "A" : "B";
                if (current.RobotId is null) g.TryEquip(robot, p.InstanceId, out _);
                else g.TryUnequip(current.RobotId, p.InstanceId, out _);
                if (i % 7 == 0) Require(g.TrySetHealth(p.InstanceId, rnd.Next(2), out _));
                if (i % 11 == 0) Require(g.TryRepair(p.InstanceId, out _));
                Safe(g);
                if (i % 100 == 0) g = LegendaryGarage.LoadJson(Catalog, g.SaveJson());
            }
        });

        var passed = Results.Count(r => r.Status == "PASS");
        var failed = Results.Count - passed;
        var receipt = new
        {
            Schema = "b01.legendary-core.test-receipt.v1",
            Target = "net8.0",
            CatalogSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(CatalogPath))).ToLowerInvariant(),
            Seed = 224001,
            Passed = passed,
            Failed = failed,
            Total = Results.Count,
            Cases = Results
        };
        var output = Path.GetFullPath(report);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllText(output, JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine($"RESULT {passed}/{Results.Count} PASS; failed={failed}; report={output}");
        return failed == 0 ? 0 : 1;
    }
}

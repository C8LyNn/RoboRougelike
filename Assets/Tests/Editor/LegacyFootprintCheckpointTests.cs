using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using UnityEngine;

public sealed class LegacyFootprintCheckpointTests
{
    static R3ModelCheckpoint Legacy()
    {
        var data = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light)).MakeCheckpoint(0);
        data.energyVersion = 0;
        data.mounted = new[] {
            new R3PartState(Vector3Int.zero, R3PartKind.Core),
            new R3PartState(Vector3Int.right, R3PartKind.DirectionalShield, .8f),
            new R3PartState(new Vector3Int(3, 0, 0), R3PartKind.Armor, .55f)
        };
        data.inventory = new[] { new R3PartState(Vector3Int.zero, R3PartKind.Wheel, .23f) };
        foreach (var p in data.mounted.Concat(data.inventory)) p.DefenseRechargeAge = float.MaxValue;
        data.wheels = new R3WheelCheckpoint[0];
        data.groups = new R3GroupCheckpoint[0];
        data.hasA = data.hasB = false;
        data.scrap = 137;
        return JsonUtility.FromJson<R3ModelCheckpoint>(JsonUtility.ToJson(data));
    }

    [Test] public void OldWideModuleBridgeMovesOnlyDisconnectedPartToInventoryWithoutLosingData()
    {
        var data = Legacy();
        var before = JsonUtility.ToJson(data);
        var armor = data.mounted.Single(p => p.Kind == R3PartKind.Armor);
        var ids = data.mounted.Concat(data.inventory).Select(p => p.Id).ToArray();
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data, R3VehicleKind.Light, null, 0,
            out var restored, out var error), Is.True, error);
        Assert.That(restored.Parts.Count, Is.EqualTo(2));
        Assert.That(restored.LegacyPartsMovedToInventory, Is.EqualTo(1));
        Assert.That(restored.Inventory.Single(p => p.Id == armor.Id).Health, Is.EqualTo(.55f));
        Assert.That(restored.Inventory.Single(p => p.Kind == R3PartKind.Wheel).Health, Is.EqualTo(.23f));
        Assert.That(restored.Parts.Concat(restored.Inventory).Select(p => p.Id), Is.EquivalentTo(ids));
        Assert.That(restored.Scrap, Is.EqualTo(137));
        Assert.That(JsonUtility.ToJson(data), Is.EqualTo(before), "The input checkpoint must not be rewritten.");
    }

    [Test] public void InvalidOldDisconnectedConstructionIsNotAcceptedAsMigration()
    {
        var data = Legacy();
        data.mounted[2].Cell = new Vector3Int(8, 0, 0);
        var before = JsonUtility.ToJson(data);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data, R3VehicleKind.Light, null, 0,
            out var restored, out _), Is.False);
        Assert.That(restored, Is.Null);
        Assert.That(JsonUtility.ToJson(data), Is.EqualTo(before));
    }

    [Test] public void ModernDisconnectedConstructionCannotUseLegacyMigration()
    {
        var data = Legacy(); data.energyVersion = 1; data.energy = 100;
        var before = JsonUtility.ToJson(data);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data, R3VehicleKind.Light, null, 0,
            out var restored, out _), Is.False);
        Assert.That(restored, Is.Null);
        Assert.That(JsonUtility.ToJson(data), Is.EqualTo(before));
    }

    [Test] public void MigrationWithFullInventoryRejectsWithoutLosingAnything()
    {
        var data = Legacy();
        data.inventory = Enumerable.Range(0, R3VehicleModel.InventoryCapacity)
            .Select(_ => new R3PartState(Vector3Int.zero, R3PartKind.Armor, .5f) { DefenseRechargeAge = float.MaxValue }).ToArray();
        var before = JsonUtility.ToJson(data);
        Assert.That(R3VehicleModel.TryRestoreCheckpoint(data, R3VehicleKind.Light, null, 0,
            out var restored, out _), Is.False);
        Assert.That(restored, Is.Null);
        Assert.That(JsonUtility.ToJson(data), Is.EqualTo(before));
    }
}

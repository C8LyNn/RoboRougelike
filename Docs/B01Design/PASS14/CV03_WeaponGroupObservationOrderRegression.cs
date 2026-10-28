// B01-10 PASS14 / CV-03 / source-pinned independent DESIGN acceptance fixture.
// The canonical game code is owned by Codex. This source is intentionally outside
// Unity Assets/** and is NOT compiled or executed merely by committing this file.
//
// ORIGINAL GAME INPUT:
//   Codex #310 @ 231005c6d0351945a8ee6684f3856af118f0e94f
//   Assets/R3/R3VehicleModel.cs Git blob 9f1bf190c7535182c6ce375cbfe67f5b2862e811
//   Assets/R3/R3VehicleCatalog.cs Git blob f76de5bbdcc1dbdb7ddb9fe9859c36c232fd8584
//   Assets/Tests/Editor/WeaponGroupTests.cs Git blob f2f9d8cdc5f2fbb25a03b01d98a65d440d3e85bc
//
// To run: Codex (only owner) may copy this C# file into its actual Unity
// Assets/Tests/Editor test assembly; execute original NUnit EditMode on a new game
// SHA, collect full raw XML, Editor.log, exit code. Do not interpret this document
// as a gameplay fix, an executed test or permission to integrate.
// Expected at pinned #310: NO_READ is source-predicted RED, EMPTY_READ source-predicted
// GREEN; these predictions must be confirmed/rejected by an actual original-C# run.
//
// PRODUCT ACCEPTANCE DECISION REQUIRED FROM HQ:
// After all mounted weapons are removed, remounting the same inventory item should
// restore default A/LMB deterministically, regardless of whether any UI read the
// intermediate weaponless state. Explicit user Swap A/B may then move it to B/RMB.
// A 0HP *still-mounted* weapon plan retains its group pending repair.
//
// Related existing queue: CV-03 https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEngine;

public sealed class B0110WeaponGroupObservationOrderRegression
{
    private static R3VehicleModel PreparedSingleGunInB()
    {
        var model = new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard));
        Assert.That(model.WeaponGroupA, Is.EqualTo(R4WeaponKind.Basic),
            "Precondition: catalog Standard mounts exactly one Basic type in A");
        Assert.That(model.WeaponGroupB, Is.Null);
        model.SwapWeaponGroups();
        Assert.That(model.WeaponGroupA, Is.Null);
        Assert.That(model.WeaponGroupB, Is.EqualTo(R4WeaponKind.Basic));
        return model;
    }

    private static (string id, Vector3Int cell, int inventoryCount, int scrap)
        RemoveSoleGunViaPublicGarageModelApi(R3VehicleModel model)
    {
        var soleGun = model.Parts.Single(p => p.Kind == R3PartKind.Gun);
        string id = soleGun.Id;
        Vector3Int cell = soleGun.Cell;
        int originalInventory = model.Inventory.Count;
        int originalScrap = model.Scrap;
        Assert.That(model.Remove(cell), Is.True,
            "Removing the only gun must preserve edit freedom when remaining structure is connected");
        Assert.That(model.Parts.Any(p => p.Kind == R3PartKind.Gun), Is.False);
        Assert.That(model.Inventory.Count(p => p.Id == id), Is.EqualTo(1),
            "Same physical part, not a generated replacement");
        Assert.That(model.Inventory.Count, Is.EqualTo(originalInventory + 1));
        Assert.That(model.Scrap, Is.EqualTo(originalScrap));
        return (id, cell, originalInventory, originalScrap);
    }

    private static void RemountSamePhysicalItem(R3VehicleModel model,
        (string id, Vector3Int cell, int inventoryCount, int scrap) captured)
    {
        Assert.That(model.Mount(captured.id, captured.cell), Is.True,
            "Remount the original inventory GUID using actual R3VehicleModel.Mount");
        Assert.That(model.Parts.Count(p => p.Id == captured.id), Is.EqualTo(1));
        Assert.That(model.Inventory.Count(p => p.Id == captured.id), Is.Zero);
        Assert.That(model.Inventory.Count, Is.EqualTo(captured.inventoryCount));
        Assert.That(model.Scrap, Is.EqualTo(captured.scrap));
    }

    [Test]
    public void CV03_NoReadDuringFullUnmount_RemountResetsToPrimaryA()
    {
        var model = PreparedSingleGunInB();
        var captured = RemoveSoleGunViaPublicGarageModelApi(model);

        // Intentionally DO NOT inspect WeaponGroupA or WeaponGroupB here.
        // This mirrors a UI frame that mutates remove+mount before rendering.
        RemountSamePhysicalItem(model, captured);

        // At the pinned original SHA these assertions are expected to expose
        // a stale B-only mapping (RED). No test was run in B01-10.
        Assert.That(model.WeaponGroupA, Is.EqualTo(R4WeaponKind.Basic),
            "Full unmount must reset mapping, not resurrect stale B via an unread intermediate state");
        Assert.That(model.WeaponGroupB, Is.Null);
    }

    [Test]
    public void CV03_ReadDuringFullUnmount_RemountResetsToPrimaryA()
    {
        var model = PreparedSingleGunInB();
        var captured = RemoveSoleGunViaPublicGarageModelApi(model);

        // The getter currently calls SyncWeaponGroups and clears both slots.
        Assert.That(model.WeaponGroupA, Is.Null);
        Assert.That(model.WeaponGroupB, Is.Null);
        RemountSamePhysicalItem(model, captured);

        Assert.That(model.WeaponGroupA, Is.EqualTo(R4WeaponKind.Basic));
        Assert.That(model.WeaponGroupB, Is.Null);
    }

    [Test]
    public void CV03_ZeroHpStillMountedPlan_KeepsExplicitBForRepair()
    {
        var model = PreparedSingleGunInB();
        var mounted = model.Parts.Single(p => p.Kind == R3PartKind.Gun);
        mounted.Health = 0;

        Assert.That(model.WeaponGroupA, Is.Null);
        Assert.That(model.WeaponGroupB, Is.EqualTo(R4WeaponKind.Basic),
            "A retained 0HP mounted plan is NOT a full uninstall");
    }

    [Test]
    public void CV03_ExplicitSwapAfterRemount_CanStillAssignSingleTypeToB()
    {
        var model = PreparedSingleGunInB();
        var captured = RemoveSoleGunViaPublicGarageModelApi(model);
        Assert.That(model.WeaponGroupA, Is.Null); // observe cleared empty state
        RemountSamePhysicalItem(model, captured);
        Assert.That(model.WeaponGroupA, Is.EqualTo(R4WeaponKind.Basic));

        model.SwapWeaponGroups();
        Assert.That(model.WeaponGroupA, Is.Null);
        Assert.That(model.WeaponGroupB, Is.EqualTo(R4WeaponKind.Basic));
    }
}

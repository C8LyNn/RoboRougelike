using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.R4;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

// Runs against EXACT original live #877 R3 garage and its existing one installation gate.
// An authored test is NOT performance evidence until a real Unity Editor executes it.
// Mono/main-thread allocations are not native GPU/PhysX allocations.
public sealed class B0150W32RealGarageProfilerTests
{
    private const int SamplesPerCase = 64;
    private const int WarmupsPerCase = 8;
    private static readonly Vector3Int SafeSawCell = new Vector3Int(0, 1, 4);

    private static long Percentile(long[] values, double fraction)
    {
        var copy = (long[])values.Clone();
        Array.Sort(copy);
        return copy[Math.Max(0, Math.Min(copy.Length - 1,
            (int)Math.Ceiling(copy.Length * fraction) - 1))];
    }

    private static string Summary(string label, long[] ticks, long[] bytes)
    {
        double microseconds = 1000000.0 / Stopwatch.Frequency;
        return label + "_wall_us_p50=" +
            (Percentile(ticks, .50) * microseconds).ToString("F3", CultureInfo.InvariantCulture) +
            " " + label + "_wall_us_p95=" +
            (Percentile(ticks, .95) * microseconds).ToString("F3", CultureInfo.InvariantCulture) +
            " " + label + "_managed_thread_bytes_p50=" + Percentile(bytes, .50) +
            " " + label + "_managed_thread_bytes_p95=" + Percentile(bytes, .95);
    }

    [UnityTest]
    public IEnumerator ActualR3GarageOwnedSawPreviewHasNativeAllocationAndLatencyEvidence()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();

        float deadline = Time.realtimeSinceStartup + 15f;
        while (R1RunController.Instance?.Progress == null &&
               Time.realtimeSinceStartup < deadline)
            yield return null;

        var run = R1RunController.Instance;
        Assert.That(run, Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Standard), Is.True);
        Assert.That(run.OpenSandboxGarage(), Is.True);

        // Same original game fixture as GarageEditFreedomTests; nothing is
        // purchased, repaired, persisted, or changed in original gameplay code.
        var oldGun = run.VehicleModel.Parts.First(p => p.Kind == R3PartKind.Gun);
        string sawId = oldGun.Id;
        var centered = oldGun.Cell;
        Assert.That(run.GarageRemove(centered), Is.True);
        var stock = run.VehicleModel.Inventory.Single(p => p.Id == sawId);
        stock.Weapon = R4WeaponKind.RotarySaw;

        Assert.That(run.VehicleModel.OwnedPlacementError(sawId, centered, default),
            Does.Contain("tarcza piły"),
            "The real original centered saw must be rejected; do not profile a vacuous preflight.");
        Assert.That(run.VehicleModel.OwnedPlacementError(sawId, SafeSawCell, default),
            Is.Null, "The real exterior saw must enter the canonical physical gate.");

        int beforeScrap = run.VehicleModel.Scrap;
        int beforeParts = run.VehicleModel.Parts.Count;
        int beforeInventory = run.VehicleModel.Inventory.Count;
        var beforeIds = run.VehicleModel.Parts.Concat(run.VehicleModel.Inventory)
            .Select(p => p.Id).OrderBy(id => id).ToArray();

        // Warm up the same real R3 CreateProbe -> PhysX -> Destroy path.
        // Let Unity retire temporary probes between frames to avoid a fake
        // saturation profile caused by the test itself.
        for (int i = 0; i < WarmupsPerCase; i++)
        {
            Assert.That(run.VehicleModel.OwnedPlacementError(sawId, SafeSawCell, default), Is.Null);
            Assert.That(run.VehicleModel.OwnedPlacementError(sawId, centered, default),
                Does.Contain("tarcza piły"));
            yield return null;
        }

        long[] safeTicks = new long[SamplesPerCase];
        long[] safeBytes = new long[SamplesPerCase];
        long[] blockedTicks = new long[SamplesPerCase];
        long[] blockedBytes = new long[SamplesPerCase];

        for (int i = 0; i < SamplesPerCase; i++)
        {
            // ABBA ordering balances first/second-query effects over pairs.
            bool blockedFirst = (i & 1) != 0;
            for (int slot = 0; slot < 2; slot++)
            {
                bool blocked = slot == 0 ? blockedFirst : !blockedFirst;
                long preBytes = GC.GetAllocatedBytesForCurrentThread();
                long preTicks = Stopwatch.GetTimestamp();
                string issue = run.VehicleModel.OwnedPlacementError(
                    sawId, blocked ? centered : SafeSawCell, default);
                long elapsedTicks = Stopwatch.GetTimestamp() - preTicks;
                long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - preBytes;
                if (blocked)
                {
                    blockedTicks[i] = elapsedTicks;
                    blockedBytes[i] = allocatedBytes;
                    Assert.That(issue, Does.Contain("tarcza piły"));
                }
                else
                {
                    safeTicks[i] = elapsedTicks;
                    safeBytes[i] = allocatedBytes;
                    Assert.That(issue, Is.Null);
                }
                // No manual Physics.SyncTransforms, global GC.Collect, or frame timing
                // override: test samples existing original project behavior.
                yield return null;
            }
        }

        Assert.That(run.VehicleModel.Scrap, Is.EqualTo(beforeScrap));
        Assert.That(run.VehicleModel.Parts.Count, Is.EqualTo(beforeParts));
        Assert.That(run.VehicleModel.Inventory.Count, Is.EqualTo(beforeInventory));
        Assert.That(run.VehicleModel.Parts.Concat(run.VehicleModel.Inventory)
            .Select(p => p.Id).OrderBy(id => id), Is.EqualTo(beforeIds));

        string expectedSha = Environment.GetEnvironmentVariable("B0150_W32_EXACT_GAME_SHA");
        // Missing provenance does not invalidate the original correctness
        // assertions, but excludes this sample from a formal performance gate.
        string provenance = string.IsNullOrEmpty(expectedSha) ?
            "UNPINNED_NOT_ACCEPTABLE_FOR_G1" : expectedSha;
        UnityEngine.Debug.Log(
            "B0150_W32_REAL_R3_GARAGE_PROFILE native_measurement_only " +
            "game_sha=" + provenance +
            " unity=" + Application.unityVersion +
            " os=" + SystemInfo.operatingSystem +
            " samples_per_case=" + SamplesPerCase + " " +
            Summary("safe", safeTicks, safeBytes) + " " +
            Summary("blocked", blockedTicks, blockedBytes) +
            " note=managed_thread_allocation_not_total_gc_gpu_or_physx");
        yield return new ExitPlayMode();
    }
    // W33: same ORIGINAL R3 game garage, not a synthetic collider benchmark.
    // Light/Heavy provide the 20/139-part endpoints in #913; Standard 83-part
    // remains covered by the existing genuine owned-Saw legal/blocked fixture.
    [UnityTest]
    public IEnumerator ActualLight20PartR3GaragePlacementLatencyAndAllocations()
    {
        return ProfileCatalogScale(R3VehicleKind.Light, 20,
            new Vector3Int(0, 0, 3), "light20");
    }

    [UnityTest]
    public IEnumerator ActualHeavy139PartR3GaragePlacementLatencyAndAllocations()
    {
        return ProfileCatalogScale(R3VehicleKind.Heavy, 139,
            new Vector3Int(0, 1, 5), "heavy139");
    }

    private static IEnumerator ProfileCatalogScale(R3VehicleKind kind,
        int expectedParts, Vector3Int exteriorCell, string label)
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline = Time.realtimeSinceStartup + 15f;
        while (R1RunController.Instance?.Progress == null &&
               Time.realtimeSinceStartup < deadline)
            yield return null;
        var run = R1RunController.Instance;
        Assert.That(run, Is.Not.Null);
        Assert.That(run.SelectVehicle(kind), Is.True);
        Assert.That(run.OpenSandboxGarage(), Is.True);
        Assert.That(run.VehicleModel.Parts.Count, Is.EqualTo(expectedParts),
            "Real catalog R3 count changed: repeat the performance intake review.");
        var oldGun = run.VehicleModel.Parts.Single(p => p.Kind == R3PartKind.Gun);
        string originalGunId = oldGun.Id;
        Assert.That(run.GarageRemove(oldGun.Cell), Is.True,
            "Actual game garage must remove the original mounted gun legally.");
        var stored = run.VehicleModel.Inventory.Single(p => p.Id == originalGunId);
        stored.Weapon = R4WeaponKind.RotarySaw;

        // PhysX + exact original placement acceptance are mandatory: never
        // print fake numbers after only a cheap logical occupancy rejection.
        string preflight = run.VehicleModel.OwnedPlacementError(
            originalGunId, exteriorCell, default);
        Assert.That(preflight, Is.Null,
            "Physical original exterior Saw candidate is not legal: " + preflight);
        int scrap = run.VehicleModel.Scrap;
        int installedCount = run.VehicleModel.Parts.Count;
        int storedCount = run.VehicleModel.Inventory.Count;
        var ownedIds = run.VehicleModel.Parts.Concat(run.VehicleModel.Inventory)
            .Select(p => p.Id).OrderBy(id => id).ToArray();

        for (int i = 0; i < WarmupsPerCase; ++i)
        {
            Assert.That(run.VehicleModel.OwnedPlacementError(
                originalGunId, exteriorCell, default), Is.Null);
            yield return null;
        }
        var ticks = new long[32];
        var bytes = new long[32];
        for (int i = 0; i < ticks.Length; ++i)
        {
            long preBytes = GC.GetAllocatedBytesForCurrentThread();
            long preTicks = Stopwatch.GetTimestamp();
            string error = run.VehicleModel.OwnedPlacementError(
                originalGunId, exteriorCell, default);
            ticks[i] = Stopwatch.GetTimestamp() - preTicks;
            bytes[i] = GC.GetAllocatedBytesForCurrentThread() - preBytes;
            Assert.That(error, Is.Null,
                "The real physical gate must stay valid during measured trials.");
            yield return null; // Let the original probe Destroy complete.
        }
        Assert.That(run.VehicleModel.Scrap, Is.EqualTo(scrap));
        Assert.That(run.VehicleModel.Parts.Count, Is.EqualTo(installedCount));
        Assert.That(run.VehicleModel.Inventory.Count, Is.EqualTo(storedCount));
        Assert.That(run.VehicleModel.Parts.Concat(run.VehicleModel.Inventory)
            .Select(p => p.Id).OrderBy(id => id), Is.EqualTo(ownedIds));

        string envSha = Environment.GetEnvironmentVariable("B0150_W32_EXACT_GAME_SHA");
        string provenance = string.IsNullOrEmpty(envSha) ?
            "UNPINNED_NOT_ACCEPTABLE_FOR_G1" : envSha;
        UnityEngine.Debug.Log(
            "B0150_W33_REAL_R3_GARAGE_SCALE native_measurement_only " +
            "game_sha=" + provenance +
            " unity=" + Application.unityVersion +
            " os=" + SystemInfo.operatingSystem +
            " catalog_kind=" + kind + " baseline_parts=" + expectedParts +
            " selected_cell=" + exteriorCell +
            " samples=32 " + Summary(label, ticks, bytes) +
            " note=real_editor_only_not_player_fps_or_gpu_and_no_save_writer");
        yield return new ExitPlayMode();
    }

    // W34: existing real 139-part UI hover/ghost, not just the isolated model gate.
    // R3GarageView.Select -> RefreshMarker -> PreviewValid -> same authoritative
    // R3BuildValidation; this includes actual Renderer[] material refreshes.
    [UnityTest]
    public IEnumerator ActualHeavy139GarageRepeatedSameCellVisualPreviewHasMeasuredLatencyAndAllocation()
    {
        EditorSceneManager.OpenScene("Assets/scene 1.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        float deadline = Time.realtimeSinceStartup + 15f;
        while (R1RunController.Instance?.Progress == null &&
               Time.realtimeSinceStartup < deadline)
            yield return null;
        var run = R1RunController.Instance;
        Assert.That(run, Is.Not.Null);
        Assert.That(run.SelectVehicle(R3VehicleKind.Heavy), Is.True);
        Assert.That(run.OpenSandboxGarage(), Is.True);
        Assert.That(run.VehicleModel.Parts.Count, Is.EqualTo(139),
            "Real Heavy R3 catalog changed: performance case must be reviewed.");

        var original = run.VehicleModel.Parts.Single(p => p.Kind == R3PartKind.Gun);
        string id = original.Id;
        Assert.That(run.GarageRemove(original.Cell), Is.True);
        var stored = run.VehicleModel.Inventory.Single(p => p.Id == id);
        stored.Weapon = R4WeaponKind.RotarySaw;
        var cell = new Vector3Int(0, 1, 5);
        Assert.That(run.VehicleModel.OwnedPlacementError(id, cell, default),
            Is.Null, "Real physical exterior gun placement must pass first.");
        var view = run.Garage;
        Assert.That(view, Is.Not.Null, "Must profile original real R3GarageView, not an invented ghost.");
        view.ChooseInventory(id);
        view.Select(cell);
        Assert.That(view.HasTarget, Is.True);
        Assert.That(view.PreviewValid, Is.True,
            "Visual preview must use same real garage mount gate.");

        int scrap = run.VehicleModel.Scrap;
        int mountedCount = run.VehicleModel.Parts.Count;
        int stockCount = run.VehicleModel.Inventory.Count;
        var ids = run.VehicleModel.Parts.Concat(run.VehicleModel.Inventory)
            .Select(p => p.Id).OrderBy(value => value).ToArray();
        for (int i = 0; i < WarmupsPerCase; ++i)
        {
            view.Select(cell); // Repeated real same-cell hover, no fake marker.
            Assert.That(view.PreviewValid, Is.True);
            yield return null;
        }
        var ticks = new long[32];
        var bytes = new long[32];
        for (int i = 0; i < ticks.Length; ++i)
        {
            long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
            long beforeTicks = Stopwatch.GetTimestamp();
            view.Select(cell);
            ticks[i] = Stopwatch.GetTimestamp() - beforeTicks;
            bytes[i] = GC.GetAllocatedBytesForCurrentThread() - beforeBytes;
            Assert.That(view.HasTarget && view.PreviewValid, Is.True,
                "Original view must keep displaying a genuinely legal candidate.");
            yield return null; // Original per-frame Unity Destroy/probe lifecycle.
        }
        Assert.That(run.VehicleModel.Scrap, Is.EqualTo(scrap));
        Assert.That(run.VehicleModel.Parts.Count, Is.EqualTo(mountedCount));
        Assert.That(run.VehicleModel.Inventory.Count, Is.EqualTo(stockCount));
        Assert.That(run.VehicleModel.Parts.Concat(run.VehicleModel.Inventory)
            .Select(p => p.Id).OrderBy(value => value), Is.EqualTo(ids));

        string sha = Environment.GetEnvironmentVariable("B0150_W32_EXACT_GAME_SHA");
        string provenance = string.IsNullOrEmpty(sha) ?
            "UNPINNED_NOT_ACCEPTABLE_FOR_G1" : sha;
        UnityEngine.Debug.Log(
            "B0150_W34_REAL_R3_HEAVY_GARAGE_VISUAL native_measurement_only " +
            "game_sha=" + provenance +
            " unity=" + Application.unityVersion +
            " os=" + SystemInfo.operatingSystem +
            " original_parts=139 samples=32 " +
            Summary("heavy139_hover_ui", ticks, bytes) +
            " note=full_existing_R3GarageView_Select_RefreshMarker_not_Player_fps");
        yield return new ExitPlayMode();
    }

}

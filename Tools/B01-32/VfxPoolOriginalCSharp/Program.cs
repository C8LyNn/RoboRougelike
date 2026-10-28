// B01-32 original C# source differential witness. Run: dotnet run --project
// TEST_KIND=ORIGINAL_CSHARP_UNDER_UNITY_SHIMS; NOT Unity/PlayMode/ParticleSystem.
// Expected known-source RED is an OBSERVATION, not a passing product contract.
using System;
using RoboRougelike.VFX001;
using UnityEngine;

static class Program
{
    private static int checks;

    private static void Check(bool condition, string id, string detail)
    {
        if (!condition)
            throw new InvalidOperationException("FAIL " + id + ": " + detail);
        checks++;
        Console.WriteLine("POSITIVE_CONTROL_PASS " + id);
    }

    public static int Main()
    {
        try
        {
            Time.frameCount = 70;
            var host = new GameObject("Original VFX001 manager contract host");
            var pool = host.AddComponent<VfxPoolManager>();
            Check(pool.CreatedCount == 8, "P01_REAL_AWAKE_PREWARM", "Original awake must prewarm eight actors");

            var first = pool.Emit(new VfxEvent(VfxEventKind.Hit, Vector3.zero, Vector3.up));
            Check(first != null && pool.ActiveCount == 1,
                "P02_ORIGINAL_ENABLED_EMIT", "Enabled source manager must admit an ordinary hit");

            pool.enabled = false;
            Check(pool.ActiveCount == 0 && pool.PooledCount >= 8,
                "P03_ORIGINAL_DISABLE_CLEARS", "OnDisable must return active effect to pool");

            // The production safety acceptance is: no effect can be emitted while component disabled.
            // Original VfxPoolManager.Emit currently never reads isActiveAndEnabled; witness observes this.
            var late = pool.Emit(new VfxEvent(VfxEventKind.Explosion, Vector3.up, Vector3.up));
            bool sourceContractIsRed = late != null && pool.ActiveCount == 1;
            Console.WriteLine(sourceContractIsRed
                ? "ORIGINAL_CSHARP_REPRODUCED_KNOWN_RED disabled_pool_Emit_returned_actor"
                : "ORIGINAL_CSHARP_SOURCE_CHANGED_OR_NOT_REPRODUCED disabled_pool_Emit");
            if (!sourceContractIsRed)
                throw new InvalidOperationException(
                    "Pinned original source behavior changed: refresh QA before asserting a fix.");

            pool.ClearAll();
            Check(pool.ActiveCount == 0 && pool.PooledCount >= 8,
                "P04_POST_NEGATIVE_CLEANUP", "Manual ClearAll must restore original pool accounting");

            // Separate positive control: disabling entire host must clear, regardless of component flag.
            var secondHost = new GameObject("Original VFX001 inactive-host control");
            var secondPool = secondHost.AddComponent<VfxPoolManager>();
            var secondEffect = secondPool.Emit(new VfxEvent(VfxEventKind.CoreDestroyed, Vector3.zero, Vector3.up));
            Check(secondEffect != null && secondPool.ActiveCount == 1,
                "P05_CORE_EMIT", "Core destroyed event should admit actor");
            secondHost.SetActive(false);
            Check(secondPool.ActiveCount == 0,
                "P06_HOST_DEACTIVATION_CLEARS", "Original OnDisable must release actor");

            Console.WriteLine("TEST_KIND=ORIGINAL_CSHARP_SHIM_EXECUTION; positive_controls="
                + checks + "; source_security_contract_RED=1; Unity=NOT_RUN; renderer=NOT_RUN");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("QA_HARNESS_FAILURE: " + e);
            return 1;
        }
    }
}

# B01-50 / VISUALPOOL-001 R1 — prewarmed, bounded reusable cosmetic GameObjects

**Producer:** B01-50 Build Performance & Profiling. **Task:** [PASS17 WAVE11 #500](https://github.com/C8LyNn/RoboRougelike/issues/500). **Governance:** [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

This is a working opt-in pool for **dispensable visuals**. Distinct from FXPACE-001 (limits activations per frame), PARTICLELOD (scales emission by distance), SHADOWLOD (decorative shadows) and SFXVOICE (audio voices), VISUALPOOL pre-instantiates a fixed number of instances **once** and reuses those exact same objects during repeated visual bursts. No existing canonical Unity scene/prefab or Codex source is changed.

## Original mechanic

Core/OptionalVisualLeasePool.cs: independent C#9 fixed-slot pool (1–256). Register each distinct instance exactly once, then grant a tokenized VisualLease with finite lifetime (0–60000 ms) and monotonic time. Each live slot has exactly one owner and generation token. Stale/double release cannot deactivate a replacement owner. When full, DENY the optional request rather than steal an active object. Expired slots stay occupied until the host's deactivate callback succeeds. A throwing callback leaves the slot active and retryable. TryRelease, RetireExpired and ReleaseAll retain exact source diagnostics. Partial cleanup failures never silently claim the entire pool is safe.

Unity/B0150OptionalVisualPool.cs: opt-in Unity 6 MonoBehaviour that strictly refuses a submitted prefab carrying any Collider, Collider2D, Rigidbody, Rigidbody2D, MonoBehaviour or AudioSource even in child objects. This is defense-in-depth, not proof that a visual is nonessential. During Awake it prewarms N inactive clones and caches each slot's ParticleSystem components ONCE. It rejects any ParticleSystem with collision/trigger modules active, even if the prefab has no Collider. Burst requests do not search for child component arrays. Each TryShowOptional resets an existing clone's transform, activates it, and restarts its visual ParticleSystem components. Update retires expired objects; OnDisable deactivates all. It DOES NOT invoke Instantiate or Destroy in the repeated burst path, nor modify gameplay, collisions, damage or real save state.

Unity/B0150OptionalVisualPoolDemo.cs: manual PlayMode context-menu 80-request burst test, with an explicitly provided disposable visual prefab. There are no hooks into the active game. Each of the three runtime scripts has a fixed companion Unity .cs.meta GUID pair.

**SAFETY:** Use only optional dust or discardable decorative sparks. NEVER physics debris, actual projectiles, damage, hitboxes, weapon cooldowns, warning/aim/accessibility indicators, navigation, gameplay-state VFX, or required effects. A visual can be essential even without a collider or MonoBehaviour. Overflow intentionally drops an effect and must not silently compromise player information. An owner must select and approve each prefab, not infer approval from a filter.

## Real C# acceptance (source gate, not native Unity)

Run from repository root with .NET 8 SDK, zero NuGet packages:

    dotnet run -c Release --project Tools/B01/B01-50/VISUALPOOL001/tests/VISUALPOOL.Tests.csproj -- --receipt /tmp/new-visualpool-receipt.json

The same original Core source compiles with warnings-as-errors, and the test program executes 50 positive/negative cases covering registration, capacity overflow, generation tokens, stale callback safety, validity and monotonic time, exact lifetime boundaries, callback rollback/retry, scene reset with partial failures and long-run 2048 leases reusing only eight original Visual instances across 256 simulated cycles. Existing receipt paths refuse overwrite. A dedicated GitHub Actions workflow compiles source tests, scans original tracked Unity Assets for three script GUID conflicts, and uploads a physical original JSON/log/meta/SHA256 evidence artifact.

A true source C# PASS is not a licensed Unity EditMode/PlayMode pass, real allocation/CPU/FPS profiling or proof that the prefab safety filter covers all possible gameplay semantics.

## Separate native Unity 6 URP acceptance

In an **explicitly authorized disposable Unity 6000.3.14f1 URP sandbox only**:
1. Copy Core/OptionalVisualLeasePool.cs and Unity/B0150OptionalVisualPool.cs (and optional demo) together with their fixed paired .cs.meta files into a NEW independent Assets/B0150VisualPoolSandbox directory. Preserve GUIDs. Check against the current receiving project including uncommitted Codex sources.
2. Prepare a purely decorative prefab with a renderer or particle system, NO Collider, Rigidbody, AudioSource, MonoBehaviour, gameplay scripts or required cues. Configure prewarmed count 16 and lifetime 1.5 s; assign prefab only to sandbox adapter.
3. In PlayMode manually invoke BurstVisuals from the demo context menu (e.g. 80 attempts). Confirm exactly 16 clones exist after Awake and never more, active count never exceeds cap, excess requests are denied, and after lifetime visuals deactivate. Repeat burst; track Unity instance IDs and confirm reuse of the same 16 objects. Disable component and confirm all clones return inactive.
4. Measure original Unity Profiler memory allocations, Instantiate/Destroy calls, CPU p95, GC and FPS against a nonpooled decorative baseline under identical machine/build/camera. Record current receiver game SHA, version, logs and player clips. This native demonstration HAS NOT been executed by B01-50; true FPS/CPU/GC gains remain NOT_MEASURED.

## Receiver / rollback

**B01-40:** verify exact SHA, manifest blob hashes and file sizes, 11 isolated paths (10 nonmanifest including dedicated workflow + MANIFEST), three stable GUIDs, original Actions artifact, rollback and no Codex code overlap.
**B01-60:** independently execute all 50 source tests on frozen SHA, audit Unity adapter's prefab safety checks, prewarm, lifecycle, stale token and required visual exclusion, and request native Unity G1 separately.
**B01-00:** decide optional Codex routing through existing #79 without new CV ID, and only after independent QA. No Codex ACK or merge claimed.

**Rollback:** disable the sandbox adapter to deactivate all currently leased instances; delete ONLY the disposable sandbox GameObjects/scripts and their paired .meta. Discard only this Tools/B01/B01-50/VISUALPOOL001 directory and its dedicated .github/workflows/b01-50-visualpool001.yml from the Draft. Never touch original game Assets, gameplay sources, prefabs, scenes, saves, other swarms or the existing CI.

**Status:** real .NET tests need actual GitHub Actions verdict; Unity native/EditMode/PlayMode/FPS/GC = NOT_RUN/NOT_MEASURED; independent QA = PENDING; Codex ACK/Integration = PENDING.
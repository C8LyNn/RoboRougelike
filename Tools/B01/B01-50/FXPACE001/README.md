# B01-50 / FXPACE-001 R1 — Bounded, original cosmetic-effect frame pacer

**Producer:** B01-50, BUILD PERFORMANCE & PROFILING. **Task:** [#433](https://github.com/C8LyNn/RoboRougelike/issues/433). **Explicit scope:** independent `Tools/B01/B01-50/FXPACE001/**` and dedicated `.github/workflows/b01-50-fxpace001.yml` only. Does not mutate Codex's canonical Assets, camera, weapons, physics, part sockets, AI, save files, any existing asset, or B01-32 original VFX.

## Actual functionality

The standalone, Unity-compatible C#9 `Core/CosmeticActivationBudget.cs` queues **purely cosmetic activations** and bounds their count to a configurable number per frame (1–128). It has:

- Strict queue size (1–32768) and a finite TTL (0–30 seconds). Stale visual requests are dropped instead of animating well after the effect's event.
- `Important` visuals can displace the oldest `Regular` when the queue is full. Regular visuals get a fair activation every third slot while both queues are nonempty, avoiding perpetual starvation. Priority is only a visual ordering policy.
- Monotonic nonnegative timestamps; invalid data rejected before mutation. Explicit `CancelPending()` for scene transitions, no automatic processing after disable.
- Counter diagnostics for completed activations, overflow, expiry, activation failure and cancellation.
- Unity-only opt-in `Unity/B0150CosmeticFxPacer.cs`: `TryScheduleVisual(prefab,position,rotation,important)` and `Update()` instantiate only eligible visual prefabs subject to a per-frame cap. `OnDisable()` cancels pending requests. `Unity/B0150CosmeticBurstDemo.cs` is a manual inspector/context-menu demonstration — **no automatic hooks** into gameplay.

**CRITICAL safety contract:** Do **not** route actual projectile prefabs, damage, hit/impact detection, physical debris, AI, colliders, weapon cooldowns, economy or a required visual aiming indicator through this queue. Delayed visuals do not change gameplay state or real hit timing; only dispensable effects such as decorative sparks, dust or weld particles can use it. This scheduler bounds *activation count*, **not** CPU milliseconds or frame duration. It may intentionally drop cosmetic effects under overload; performance improvements remain **unmeasured** until a legitimate native Unity test.

## Actual .NET core acceptance and reproduction

The optional dedicated GitHub Actions workflow performs **real C# compile/execution** using `actions/setup-dotnet@v4`, .NET 8 SDK and the *same original core source* linked in the zero-NuGet `tests/FXPACE001.Tests.csproj`; treats C# compile warnings as errors. Original console acceptance includes positive/negative validation, overload, TTL boundaries, overflow priority, fair ordering, cancellation, exception behavior, repeated clocks and a 300-request stress burst:

```sh
dotnet run -c Release --project Tools/B01/B01-50/FXPACE001/tests/FXPACE001.Tests.csproj -- --receipt /path/to/NEW-test-receipt.json
```

Expected: all actual original C# tests pass, **300 purely cosmetic effects delivered in exactly 38 simulated frames at maximum 8 scheduled activations per frame** under configured max queue 400, 2s TTL, synthetic 16ms frame timestamps. This is executable scheduling logic, **not** Unity or real game FPS. The runner refuses to overwrite the JSON receipt. Workflow publishes original test stdout, SHA256 and receipt as an artifact. Actual executable test status and SHA are to be confirmed from Actions, not implied.

## Unity 6 installation (Codex-only or explicitly authorized isolated sandbox)

1. In an **authorized disposable Unity 6000.3.14f1 URP project**, copy `Core/CosmeticActivationBudget.cs`, `Unity/B0150CosmeticFxPacer.cs` and optionally `Unity/B0150CosmeticBurstDemo.cs` into a standalone folder e.g. `Assets/B0150FxPaceSandbox/`. Let Unity generate standalone .meta GUIDs there; do not touch canonical R1/R3/R4/R6/P12 or Codex's working scene.
2. In a disposable demo scene, create `B0150CosmeticFxPacer` and `B0150CosmeticBurstDemo` components on temporary objects, assign a **decorative-only, no-physics prefab**, run, manually invoke `ScheduleBurst` from component context menu and inspect queue/counters. Even in a gameplay-adjacent preview, **never** connect actual weapons or damage.
3. Capture exact Unity version, source SHA, player/editor logs, per-frame behavior and tracked visual count. Independent B01-60 confirms no duplicates, excess per-frame activations, geometry collisions or required VFX loss. Native EditMode and PlayMode not yet executed by B01-50.

**Rollback:** remove **only** the isolated sandbox files and any sandbox .meta created in that disposable project; delete the independent Tools package/workflow when discarding this PR. No canonical source, prefab, Unity scene or save state needs recovery because none was changed.

## QA / transfer
B01-40 checks original source Git HEAD/owned file hashes/workflow and isolated install/rollback; B01-60 independently compiles and executes actual linked original C# core and reviews the optional Unity adapter static source. B01-00 controls any optional Codex offer or use, subject to existing #276 ownership locks. Codex ACK/Unity native integration are never implied by producer .NET tests. This package implements an actual pacing mechanism, **not** a profiling dashboard or simulated collision benchmark.

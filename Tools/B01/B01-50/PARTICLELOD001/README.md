# B01-50 — PARTICLELOD-001 R1 / optional ParticleSystem emission-rate LOD

**Wave:** PASS17 WAVE10 [#483](https://github.com/C8LyNn/RoboRougelike/issues/483). **Owner:** B01-50. Governance [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

This is a working isolated 3-tier decorative particle emission intensity mechanic, not another profiling report. ZERO canonical Assets, Codex gameplay, colliders, physics, combat, save or other agent changes.

## Original functionality

Core/OptionalParticleEmissionBudget.cs is an original deterministic C#9 pure policy (no external libraries): up to 4096 manually approved optional decorative ParticleSystem emitters, inspection cap 1–128 per frame with round-robin and no starvation. It changes only emission multiplier on transition among:

- Near: **100% of original captured rateOverTimeMultiplier**, not blindly 1.
- Medium: **35% of original baseline**, after exceeding near distance + hysteresis (default core 20m + 2m). Return to near only **below** near distance − hysteresis.
- Far: **0% of baseline**, after exceeding far distance + hysteresis (default core 40m + 2m). Return to medium only **below** far distance − hysteresis.
- Near-to-Far and Far-to-Near direct jumps work. Stable bands do not reapply the value each frame. Invalid distance fails before mutation, failed callback leaves state retryable, RestoreAll restores the exact original multiplier.

Unity/B0150OptionalParticleEmissionLod.cs is an isolated opt-in Unity 6 MonoBehaviour **using the real ParticleSystem.emission.rateOverTimeMultiplier**. It captures each authorized emitter original multiplier, excludes inactive and emission-disabled emitters and duplicate instances, restores exact originals when disabled/destroyed or when changing approved emitters. NEVER invokes ParticleSystem.Stop, Clear, changes colliders/trigger modules, Renderer.enabled, gameplay scripts, weapons, aiming, save state, or canonical source.

Unity/B0150OptionalParticleEmissionDemo.cs provides an explicit, **PlayMode-only**, manual inspector context-menu demo creating temporary decorative dust ParticleSystems with maxParticles=30 and collision/trigger disabled. Three stable .cs.meta pairs are supplied for Unity import. No persistent scene/prefab is touched.

**Critical:** opt in exclusively dispensable dust, sparks or ambience. Never required telegraphs, navigation markers, accessibility cues, enemy hit detection, aiming cues or important feedback. Purely visual does not automatically mean dispensable. Only the authorized owner may approve particular real game VFX for this policy.

## Original C# source test execution

Run from repository root with .NET 8 SDK; select a *previously nonexistent* JSON output path:

    dotnet run -c Release --project Tools/B01/B01-50/PARTICLELOD001/tests/PARTICLELOD.Tests.csproj -- --receipt /tmp/new-particlelod-source-qa.json

The test project links precisely the original Core C# file (no fake Unity assemblies, 0 NuGet dependencies) and compiles with warnings-as-errors. **41 positive/negative tests** cover constructor/range errors, invalid and duplicate emitter registration, exact hysteresis boundaries, stable output without repeated Unity setter calls, NaN/negative distance, callback failure retries, restoration to non-unit baseline, 1024 emitters with max16 checks/frame across 64 simulated frames and a second 1024-emitter return-to-full-rate case. CI produces real stdout, JSON, GUID gate log and SHA256 receipt. Existing JSON is never overwritten.

Source C# tests use simulated camera distances and have NO Unity renderer/GPU execution. Source PASS is not Unity G1, actual frame FPS improvement, measured particle rate, or Codex ACK.

## Opt-in licensed Unity 6 URP demonstration

1. In an explicitly authorized disposable Unity 6000.3.14f1 URP project, copy the original Core/OptionalParticleEmissionBudget.cs and Unity/B0150OptionalParticleEmissionLod.cs plus their paired .cs.meta files (and optional demo), preserving the three supplied GUIDs, into a **new sandbox Assets folder**. Do not edit active Codex game assets or scenes.
2. Attach optional LOD adapter and manual demo to temporary GameObjects, assign camera and adapter in the Inspector, enter PlayMode, manually call CreateOptionalParticleSamples through the demo context menu.
3. Move camera through 20m and 40m thresholds including ±2m hysteresis; verify exact emission.rateOverTimeMultiplier, particle counts, preserved existing emission curve, original non-unit baseline on disable, and exclusion of a pre-disabled emitter. Capture Unity Editor/PlayMode logs and compare actual GPU/CPU/particle count/FPS baseline with enabled policy on the same legal host.
4. Check fixed .meta GUIDs against the **actual receiving checkout**; GitHub Action checks only repository-tracked Assets, not unpublished Codex local files.

## Receiver / rollback

**B01-40:** verify source file and manifest SHA, script .meta, only owned changed paths, original Actions evidence, no collisions/other agent edits, install and rollback. **B01-60:** independent reproduction of 41 tests at exact PR SHA, review adapter and semantics, native isolated Unity import/PlayMode if legal environment exists. **B01-00:** only authorized Codex routing after independent G0. Do not invent CODEX_ACK or duplicate the historical CV-12 profiling queue.

**Rollback:** disable the isolated adapter (automatically restores original multipliers); delete the temporary demo GameObjects and imported sandbox scripts + paired .meta. Discard the own Tools/B01/B01-50/PARTICLELOD001 folder and own workflow when cancelling the Draft. No canonical Unity scene/prefab/save is changed.

**Current limits:** ORIGINAL_SOURCE_CSHARP_TESTS require actual GitHub Actions outcome; UNITY_EDITOR/PLAYMODE NOT_RUN; measured GPU/FPS/particle count NOT_MEASURED; independent QA PENDING; CODEX_ACK PENDING; no merge.
# B01-50 — LIGHTBUDGET-001/R1 (PASS17/WAVE16)

## Working opt-in Unity 6 URP feature
This isolated optional component caps the number of manually assigned DECORATIVE Light components using Light.enabled. It is distinct from earlier SHADOWLOD #480 (which changes Renderer.shadowCastingMode only). It never modifies essential game lights, camera, weapons, physics, save/IDs, #510, #556, CHL/PARTSIZE or canonical Codex gameplay.

The original pure C# policy Core/CosmeticLightBudget.cs accepts at most 256 registered lights with stable unique nonnegative IDs, priority 0-100 and finite squared distance. It orders importance descending, distance ascending, then ID ascending. Incumbent-only 0-50m hysteresis reduces thrashing; importance priority always dominates. Invalid, duplicate, NaN, infinity or negative inputs fail without changing the output selection buffer. The policy preallocates ranking/output buffers and reuses them for each update.

The Unity wrapper Unity/B0150OptionalCosmeticLightBudget.cs samples camera/light world positions once every 1-30 frames (default 6), changes Light.enabled ONLY on specifically assigned opt-in Light references, and restores each original Light.enabled state when disabled or destroyed. Lights originally disabled will not be activated. If Camera.main or explicitly referenced camera is unavailable, no light changes occur. A missing/duplicate light aborts configuration without modifying scene Lights.

CRITICAL SAFETY: only assign genuinely dispensable decorative lights. Never include combat-critical, navigation, warning, puzzle, primary illumination, enemy readability, gameplay-significant signals or Lights concurrently controlled by other scripts. This adapter is the exclusive owner of registered Light.enabled fields while active. The pure selector has failure-atomic outputs; the Unity adapter applies per-Light writes and DOES NOT promise transaction-level scene commit/rollback.

## Isolated real Unity import/demo
1. Use a disposable Unity 6000.3.14f1 URP test project, never the canonical Codex Unity worktree.
2. Copy the three exact source+.meta pairs from Core/ and Unity/ into a matching disposable Assets/B0150LightBudget001 folder. Preserve all three GUIDs. Do not edit existing Assets, scenes, prefabs, packages or serialization.
3. Create an empty scene, add an empty GameObject with the B0150OptionalCosmeticLightDemo component, then enter Play. It generates 12 runtime-only nonphysical columns and 12 original decorative point Lights, their own optional budget manager (cap four) and an orbiting camera only if no Camera.main exists. The sample explicitly turns Light.shadows OFF and removes primitive colliders.
4. Inspect that at most four opt-in components are lit during updates and originally enabled flags return when manager is disabled. Observe in Unity and save original Editor.log/XML/screenshots if actually tested. Stop Play to clean all temporary objects. No saved/persistent Unity gameplay data is touched.

## Original C# tests
The independent .NET 8 console test project tests the actual source Core/CosmeticLightBudget.cs (not a simulator or rewritten policy). All 42 original positive/negative cases are executable including duplicate ID, invalid distance/priority/capacity, deterministic equal-rank ID, Light priority precedence, hysteresis, zero budget, maximum 256 entries, recovery after rejection and atomic unchanged output on rejection.

    dotnet run -c Release --project Tools/B01/B01-50/LIGHTBUDGET001/tests/LIGHTBUDGET.Tests.csproj -- --receipt /tmp/lightbudget-source-test-receipt.json

The dedicated cross-platform CI compiles on Windows 2025 and Ubuntu 24.04, checks 42/42 in the original JSON, checks original SHA/Git blobs for all sources and 3 original Unity .meta GUIDs for collisions with canonical Assets, and uploads test logs + receipt. If Unity Editor/PhysX is inaccessible, record NATIVE_G1_NOT_RUN. No measured GPU draw calls, FPS, lighting quality or actual Unity execution is claimed based on C# console source tests.

## Ownership and rollback
Producer B01-50. PACKAGE_ID B01-50-LIGHTBUDGET-001, version R1. New paths exclusively Tools/B01/B01-50/LIGHTBUDGET001/** and .github/workflows/b01-50-w16-lightbudget.yml. All source/test/meta/workflow hashes pinned in MANIFEST-GIT-BLOBS.tsv.
No old game code, previous frozen package or common CI was modified. Rollback delete ONLY these own paths on disposable branch. If manually imported into an isolated Unity test project, stop demo and remove only explicitly imported matching source+meta pairs with original GUIDs; never remove other files, even empty foreign directories.
Receivers B01-40 source/G0, B01-60 native independent QA, B01-00 coordination. State PRODUCER_SOURCE_QA_PENDING until executed; SOURCE_G0_INDEPENDENT_PENDING; NATIVE_G1_NOT_RUN; G2_HOLD; CODEX_ACK_NONE; no merge.

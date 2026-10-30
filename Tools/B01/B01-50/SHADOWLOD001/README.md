# B01-50 / SHADOWLOD-001 R1 — optional decorative shadow distance pacing

Producer B01-50. Dispatch [PASS17/WAVE09 #466](https://github.com/C8LyNn/RoboRougelike/issues/466), ownership [#276](https://github.com/C8LyNn/RoboRougelike/issues/276). Isolated *working source*, not a performance report or native game pass.

## What it implements

The dependency-free C#9 Core/ShadowDistanceBudget.cs implements a deterministic round-robin scheduler with **1–128 renderer checks per frame**, maximum 4096 explicitly registered *optional decorative shadow casters*, and hysteresis: shadow casting switches OFF only above 35m by default, and switches back ON only below 25m. The gap prevents rapid toggling at threshold boundaries. Callback only runs when desired shadow state changes. Null or invalid distance values fail closed; a failed state transition is retryable. There are explicit restoration and counter APIs.

The opt-in Unity 6000.3.14f1 adapter at Unity/B0150OptionalShadowDistancePacer.cs reads the actual renderer.bounds.center and camera Transform. It changes **only Renderer.shadowCastingMode**, retaining the original On/TwoSided setting for restoration (including on disable/destroy). It does NOT disable Renderer.enabled, meshes, colliders, Rigidbody, physics, input, weapons, AI, save state, or canonical Unity game assets. Preexisting Off and ShadowsOnly renderers are excluded. No global hookup or active game scene modification. The manual Unity/B0150OptionalShadowDemo.cs creates temporary cubes **only in PlayMode**, disables their colliders and registers those temporary renderer references through the adapter.

**Safety boundary:** register only decorations with dispensable shadows. Never register shadows essential to gameplay/navigation, hazard warning, enemy visibility, accessibility, telegraphs, or geometry that another owner controls. Changes in visual shadows can still alter scene readability; actual design/playtest and licensed Unity validation are required.

## Actual source test execution

The zero-NuGet .NET 8 test project links the **exact original core source**. Run the following command from the repository root with .NET 8 SDK (choose a previously nonexistent receipt path):

    dotnet run -c Release --project Tools/B01/B01-50/SHADOWLOD001/tests/SHADOWLOD.Tests.csproj -- --receipt /tmp/new-shadowlod-source-receipt.json

Original 38 cases cover constructor and registration input validation, invalid distances, exact near/far hysteresis boundaries, stable state, frame-budget round-robin without starvation, restored shadows, callback faults, 1024 casters / 16 checks per frame over 64 simulated frames, and complete reverse transition. Compiler treats warnings as errors. The dedicated GitHub Actions workflow also checks all three Unity script .meta GUIDs against the existing tracked Assets/** and uploads actual C# stdout, JSON, GUID gate and SHA256 evidence. An existing receipt is never overwritten.

**Evidence boundary:** these are real C# tests on *simulated distances*, not native Unity, GPU/URP draw-call optimization, audio, frame times or FPS. Do NOT report actual performance improvements or G1 until an independent licensed Unity Editor/Player run produces native proof.

## Optional isolated Unity smoke test

1. In an **authorized disposable Unity 6000.3.14f1 URP project**, copy original Core/ShadowDistanceBudget.cs and Unity/B0150OptionalShadowDistancePacer.cs and optionally Unity/B0150OptionalShadowDemo.cs to an isolated Assets/B0150ShadowLodSandbox/ folder. **Preserve each exact .cs plus .cs.meta pair**; all three stable GUIDs are included in this package. Do not touch existing game scenes or prefabs or use the active Codex worktree.
2. Attach B0150OptionalShadowDistancePacer to a temporary GameObject; assign an actual camera Transform. Attach B0150OptionalShadowDemo to another sandbox GameObject, assign both camera and pacer. In PlayMode, call CreateSandboxDecoration from the demo's inspector context menu. It generates **runtime-only** cubes, immediately disables their colliders and gives the pacer their renderers.
3. Move the camera beyond 35m, then into 20–30m, then within 25m. Verify shadow casting becomes Off / stays Off / is restored On while meshes remain visible; test a TwoSided original mode separately. Confirm max checked casters per Update equals the configured limit. Disable the adapter and confirm exact original modes restore.
4. If licensed, measure real shadow-caster count, CPU/GPU frame, draw calls and FPS using Unity Profiler and Frame Debugger; record baseline, source SHA and Unity version. Unity-native import, EditMode/PlayMode, FPS and performance impact are currently **NOT_RUN/NOT_MEASURED**. The GitHub source gate does not scan uncommitted Codex files; independently check .meta collisions in the actual receiver checkout before import.

## Receiver and rollback

**B01-40** checks exact SHA, file scope, manifest/GUIDs, real CI artifact, importability and rollback. **B01-60** independently replays the original C# 38-case source tests and verifies adapter semantic safety, requesting native Unity validation separately. **B01-00** decides optional Codex offer; nothing here implies actual Codex ACK, Unity integration, G1/G2 or release.

Copy-ready Codex note: "Review SHADOWLOD-001 only in an isolated Unity 6 URP sandbox, preserve the supplied three C# .meta pairs, run original .NET tests, enable culling only for explicitly approved decorative renderers. Do not edit canonical gameplay. Return actual recipient game SHA, native import/PlayMode logs, visual baseline, measured shadow draw calls, FPS and rollback result; no merge without B01-00 authorization."

**Rollback:** remove only this Tools/B01/B01-50/SHADOWLOD001 subtree and its single .github/workflows/b01-50-shadowlod001.yml from this separate Draft. In a disposable Unity scene disable the adapter (restores original modes), delete only imported sandbox files/.meta and PlayMode-created demo objects. Original Assets/prefabs/saves/Codex worktree were never modified.
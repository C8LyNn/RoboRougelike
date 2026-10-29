# B01-21 / MAP-BREAK-001 — CODEX INTAKE + FAIL-CLOSED RECEIVER

**Producer:** B01-21 | source-only independent research branch stacked on #362@454cd190639ba6ce3600ceed5f4fbd97fff298c2. **Owner changed paths:** `Research/B01-21/P17/BREAK001/**` and dedicated B01-21 workflow only. No `Assets/**`.

## NEW W03 receiver option: real standalone C# game-language module

**Prebuilt optional source, NOT an authorized game integration:** `Runtime/CoverDestructionRuntime.cs` contains pure C# `B01.MAPBREAK001.CoverWorld`, `SourceCoverLayout`, `SourceBox`, `SourceMarker` and `SourceCell`, with **zero UnityEngine, external runtime package, or JSON-library dependencies**. The existing original geometry-specific Python authoring scripts remain frozen.

On Windows/Linux receive the standalone C# module with:

```shell
dotnet run --project Research/B01-21/P17/BREAK001/RuntimeTests/CoverBreakRuntimeTests.csproj --configuration Release
```

The .NET8 test-only loader uses `System.Text.Json` to materialize **original Git-committed** 3 MAPGEN arenas (33 damage cases). Assertions prove the actual C# reproduces all source Python geometry/visibility/state hashes, not merely self-generated values, and tests multi-cover simultaneous destruction, undo, stable receipts, corrupted/wrong-map rejects and post-construction data mutation isolation.

A future **Codex-owned** canonical adapter must: (1) read/map the real `MapGenBuilder` active source `GeometryHash` and original Cover ordinals in source order, (2) create a **new** `CoverWorld` with a correct measured/current coordinate transform and actual original cover dimensions + three source enemy descriptors, (3) apply `Destroy/Restore` to actual live colliders/meshes only on Codex's side, and (4) invalidate any receipt if the current source map hash or original cover count mismatches. The exported receipt's SHA256 is for accidental damage/corruption only; it is **not** a signature or save-file anti-cheat. Data generator **does not** implement active Unity durability, inventory, projectile damage, NavMesh, Physics.Raycast or actual hull-driven traversal.

**Frozen SHA rule:** B01-40 only freezes/recommends an exact final W03 PR HEAD after original source tests and real C# dual-platform CI have run. B01-60 independently reviews the real C# against original static source JSON and the caller-provided geometry hash, not only Python's own expectations. Do not merge, install or mark G1/G2 from .NET8 success.

## Exact requirements for handoff

1. B01-40 checks the complete Draft PR exact final HEAD vs unchanged #362 base and verifies all own-path scope, original source Git blob gates, presence of real `cover_break.py`, 12 source test cases, exact regenerated three damage JSONs + interactive `cover_break_viewer.html` + manifest, byte-identical regeneration on Ubuntu/Windows. Do not bypass test failure or label source-only pass G1.
2. B01-60 independently tests 33 real source cases and their fail-closed properties: each destroys only one original Source Cover ordinal, original `geometryHash` stays pinned, per-state SHA changes, visibility when removing a source opaque box cannot decrease, and no Source Light/Heavy main-route softlock appears. Independently check the browser UI event/reset handling and source-cover index alignment; the author's tests alone do not constitute this gate.
3. Codex receives only an **optional authored-data and UI-prototype suggestion**, not a Unity package ready to import. If HQ explicitly authorizes Codex to implement, prefer mapping the original cover ordinal and original `MapElement` index to actual source `MapGenBuilder` cover GameObjects, with proper parent transform and a strict hash gate. A destruction state must be invalidated if generator seed/recipe/cover geometry differs. Only Codex edits real canonical Unity runtime.
4. Native acceptance requires *actual licensed* Unity 6000.3.14f1 with correct original game build SHA and measured current Light/Heavy rig. Test real destructible colliders/trigger interactions, line of sight from enemy/camera/muzzle in true 3D, damage/repair reset, original map generation/replay after destruction, no newly blocked R6 wheel-driven center route, and no save/run state contamination. Capture real build/Editor.log, EditMode + PlayMode NUnit XML and human/player movement evidence. **Unrun = NOT_RUN**. Static source heatmaps are never proof of dynamic physics.
5. If integration is unwanted or not authorized, stop at source G0/QA and leave the original game intact. **Rollback** without integration = drop isolated research diff/branch; all canonical game state unchanged. After a separately authorized Codex integration, revert *only the Codex receiver/destruction implementation commit*, preserving original source game MAPGEN/CHL/PART and user save files, and rerun exact native G1 checks.

**No merge to main/develop. No claimed Codex ACK/G1.**

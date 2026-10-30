# B01-32 SCAN-SWEEP-VFX-001/R1 · PASS17 WAVE08
**Original 3D cosmetic scanning beam:** a 60° transparent URP sector built from **128 original C#9 vertices / 64 triangles**, rotating once around Y, then fading over 0.42 seconds. Does **not** detect enemies, use Unity Physics, alter hazard AI, reveal map, raycast, apply damage, grant rewards, or modify game camera/aim. Distinct from B01-32 wheelwake, salvage beacon, repair welding and dual-barrel recoil; no overlap with Codex gameplay.

## Actual tangible deliverables
- `Runtime/ScanSweepCore.cs`: independent deterministic pure C#9 phase machine with finite checks, cancel/OFF semantics and actual reusable 3D sector geometry, mathematically bounded.
- `Runtime/B0132ScanSweepVisual.cs`: original Unity mesh generator + `MeshFilter`/`MeshRenderer`, `StartVisualScan(float headingRadians)`, `CancelVisualScan()`, `SetVisualsEnabled(bool)`. No collision or Rigidbody.
- `Shaders/B0132ScanSweepTransparentURP.shader`: authored URP ShaderLab transparent double-sided unlit pass with per-vertex RGBA and uniform opacity. Native GPU shader compilation NOT_RUN.
- Generated physically serialized `Assets/B0132ScanSweep/Resources/B0132_ScanSweep.prefab`, original actual script GUID, runtime/test asmdefs and source in a portable `B0132-ScanSweep-UnityImport.zip`. Root `.meta` and all siblings included. All producer files remain isolated from Codex.
- **Actual physical `scan-sector.obj`** created by compiled original C#9 geometry (128 vertices, 64 triangle faces) plus **eight physical RGBA PNG CPU visual proofs** rasterized from this exact OBJ and the C# deterministic timeline. Images are NOT Unity/URP screenshots.

## Tests and acceptance boundaries
CI executes original C#9 compiled tests on Windows2022 and Ubuntu24.04 (2048 deterministic trajectories x8 steps; actual geometry seams, triangles, bounds, cancel/OFF). Roslyn checks all three C#9 Unity + test sources for syntax only. Python checks original exact import ZIP bytes, prefab script binding, URP shader, unique GUID, rejects deliberately malformed inputs, validates real OBJ and eight preview frames. Binary ZIP must match raw byte-for-byte across OS.

**B01-40 independent G0:** fetch the exact generated nested ZIP and `SOURCE_RECEIPT.json`, independently verify SHA-256, imported `Assets/B0132ScanSweep/**` directory scope / casefold path + GUID against current Codex target, staging and deletion rollback. This is NOT covered by producer authority.

**B01-60 native G1:** authorized disposable Unity 6000.3.14f1 URP: import entire nested ZIP with `.meta`; load `Resources/B0132_ScanSweep.prefab`, test actual URP shader transparent pass and real 3D triangle visibility, execute **two authored PlayMode tests** with raw TestRunner XML and Editor.log, capture 720p/1080p screenshots/video including VFX OFF. Native shader/Editor tests **NOT_RUN** by producer CI.

**B01-00 / Codex:** optional later cosmetic trigger from a Codex-owned scan presentation event ONLY if one exists and HQ explicitly selects it. No gameplay detection data or ownership assumed. `CODEX_SENT=NO`, `ACK_PENDING`, `G2_HOLD`. Cannot merge this Draft into canonical code.

**Rollback:** remove only imported `Assets/B0132ScanSweep/` folder and sibling root `.meta`, plus any future Codex-owned opt-in adapter; leave gameplay/save data untouched.

**Gates:** `PRODUCER_SOURCE_G0` only after exact green Actions; `INDEPENDENT_G0=PENDING`, `UNITY_G1=NOT_RUN`, `CODEX_ACK=NO`, `INTEGRATED=NO`.
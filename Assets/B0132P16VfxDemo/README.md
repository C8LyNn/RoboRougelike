# B01-32 / PASS 16 — ISOLATED VFX DEMONSTRATOR R1

**PACKAGE_ID:** `B0132-VFX-P16-R1` · **PRODUCER:** B01-32 · **TARGET:** Unity 6000.3.14f1 URP · **SOURCE:** isolated new folder `Assets/B0132P16VfxDemo/`. No changes to canonical R6/VFX001, CHL, PARTSIZE, garage, HP or physics. Codex alone can integrate a selected effect into the main game.

## Actual Unity assets

- **Scene:** `Scenes/B0132_P16_VfxDemo.unity`, an importable Unity YAML scene containing a single `B0132Showcase` host. The **runtime** creates a 3-platform greybox display, light, camera and keyboard/IMGUI controls; there is no dependency on another game scene.
- **Prefab:** `Prefabs/B0132_ProjectileHit.prefab` — light-blue/amber impact flash and 10 sparks, 0.55 seconds; cosmetic objects have disabled colliders.
- **Prefab:** `Prefabs/B0132_PartDetached.prefab` — damaged orange dummy module moving outward/down, scorch flash and 8 fragments, 1.35 seconds; visual-only, no actual physical detachment or HP mutation.
- **Prefab:** `Prefabs/B0132_CriticalWarning.prefab` — pulsing red/orange core and warning circle, 2.1 seconds, no gameplay warning gate.
- **Runtime:** `Runtime/B0132Effect.cs`, `Runtime/B0132Showcase.cs`, separate stable Unity asmdef `B0132.P16VfxDemo.Runtime`.
- **QA:** `Tests/PlayMode/B0132EffectPlayModeTests.cs`, separate asmdef `B0132.P16VfxDemo.PlayModeTests` with THREE authored native tests.

All scene, prefab and script assets have committed `.meta` and unique GUIDs. Prefabs are editable; no new Blender or MODEL_JOB assets were created.

## Run in a disposable authorized Unity editor

1. Verify receiver source SHA and only copy `Assets/B0132P16VfxDemo` (including `.meta`) into a separate licensed Unity 6000.3.14f1 URP checkout. No integration/merge without Codex/HQ consent.
2. Open `Scenes/B0132_P16_VfxDemo.unity`, press Play. Press `1` (hit), `2` (visual detach), `3` (warning), `Space` (all), `X` (clear). UI buttons do the same. The scene auto-triggers all every 3.2 seconds by default.
3. Drag a prefab into a scratch scene: it auto-plays its distinct serialized `effectKind` during Play. Do not use these unpooled prototype objects in a production combat loop without profiling and owner integration.
4. Execute the named `B0132.P16VfxDemo.PlayModeTests` assembly and verify 3/3. Record actual NUnit XML, Unity Editor.log, build/source SHA, process exit, 720p/1080p screenshots and warnings. Native result is **NOT_RUN** until verified.
5. B01-60 independently validates native evidence and B01-40 provenance. Codex decides on any integration into VFX001 and specifically mapping from real hit / part detach / warning events.

## Hard boundaries and known limitations

These effects **never call** game damage, economy, storage or trigger APIs. They create transient primitive renderers, no enabled Collider, no Rigidbody forces and no real vehicle damage. There is no claim that a detached visual is a physically detached module. `B0132Showcase` is a sandbox, **not** a production gameplay level.

The demo creates runtime GameObjects/materials instead of using original VFX001 pooling; mass-production would require a Codex-owned conversion/adapter and real profiling. Shader fallback uses URP/Lit or Unlit; camera/HDR bloom, performance and real Unity import are **unverified**. The actual HQ policy for VFX OFF and Beam telegraph remains unresolved separately. Do not assume queue receipt means CODEX_ACK or native Unity access.

**Entry requirement:** original import compilation, correct prefab/scene MonoBehaviour script binding, 3/3 real PlayMode tests, no active physical colliders after spawn, no effects after disable/clear, renderer visible at 720/1080. **G0 source check** isn't G1 Unity and no release/merge is authorized.

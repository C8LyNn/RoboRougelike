# B01-32 / PASS17 follow-on — WHEELWAKE-001 R1

**Producer:** B01-32 (Animation/VFX). **New independent visual feature:** wheel contact dust and braking/drift sparking, not muzzle/hit/existing damage decals or a gameplay tyre model. Unity 6000.3.14f1 URP candidate. **Production game code owner: Codex.**

## Physically committed content
- `Runtime/WheelWakeProfile.cs`: **pure deterministic C#** acceptance profile, inputs = visual mode Off/Cruise/Drift/Brake, nonnegative speed in m/s, slip 0..1, real contact bool, VFX bool; output = per-second emission, life, size, speed and alpha/color. Invalid/nonfinite inputs / contact loss / VFX OFF fail closed. The profile never reads game physics or applies forces/damage.
- `Runtime/B0132WheelWakeEmitter.cs`: one isolated dual-wheel Unity component using two particle systems, max 128 each, sends real particles from authored wheel contact offsets, explicit `SetVisualInput(...)` and `ClearAll()`. VFX OFF, no contact or inactive owner stops and clears **both** systems.
- `Runtime/B0132WheelWakeDemo.cs` plus **actual** `Scenes/B0132_WheelWakeDemo.unity`: stand-alone greybox vehicle/ground/camera auto-created at Play. Toggle modes 1–4, X VFX OFF, GUI speed/slip/contact. No Rigidbody, no integrated garage, main run, weapon, damage or AI.
- `Prefabs/B0132_DualWheelWake.prefab`: serialized emitter with two adjustable wheel offsets. Own stable Unity `.meta` GUIDs and isolated asmdefs.
- `Tests/PlayMode/B0132WheelWakePlayModeTests.cs`: original Unity PlayMode **3 authored, NOT_RUN** cases (dual emission without physics, grounded stop, OFF+disable stale clear).
- `Tools/B01-32/P17/WheelWakeChecks`: **real .NET 8 executable** directly compiles the original `WheelWakeProfile.cs` via linked file. Positive/negative/2048-vector deterministic checks, not mocks.
- `Tools/B01-32/P17/verify_wake_unity.py`: verifies original Unity scene/prefab/asmdef structure, importer and unique GUIDs, C# lifecycle boundaries; YAML source check, not imported Unity.

## Stand-alone Unity review (no merge)
1. Pin Draft PR source SHA; copy **only** `Assets/B0132P17WheelWake` including all `.meta` into disposable licensed Unity 6000.3.14f1 URP. Do not modify Codex's integrated run or use as merge-ready until reviewer acceptance.
2. Open `Scenes/B0132_WheelWakeDemo.unity`; press Play. **1 Off; 2 Cruise; 3 Drift; 4 Brake; X VFX switch.** GUI speed/slip/contact toggles. Vehicle should emit distinct dust/sparks from left/right wheel offsets and clear instantly when grounded=false or OFF.
3. Open Test Runner, run exactly three `B0132.P17WheelWake.PlayModeTests` UnityTests and retain original XML, Editor.log, exit code, 720p/1080p screenshots and exact source/Unity version. Native renderer, actual shader, framerate, input-system compatibility and GPU are NOT_RUN.
4. Codex may adapt an actual game vehicle's normalized contact/speed/slip outputs to `SetVisualInput` only after explicit acknowledgment; **do not infer actual wheel slip** from this cosmetic profile, do not change current R6 vehicle physics or original VFX001 pooling without ownership approval. Independently check that effects don't obscure enemy weapon tells.
5. Rollback: remove copied folder and any Codex-owned visual adapter if made; no game save/schema or integrated mechanics require migration.

## Dependencies / known limitations
Runtime depends only on UnityEngine + URP-compatible particle material shader candidate; pure source uses only `System`. Exact C# compiled/tested in .NET 8 is **not proof of native Unity compilation**. Emitter creates its own 2 particle systems and material on Awake; max 128 particles per wheel, unpooled, not profiled in real vehicle gameplay. Shader Find fallback may require a URP-specific material chosen by Codex. The demo uses primitive geometry and immediate-mode GUI; it is not an actual garage interface. No player-controlled vehicle movement in this scene. All collision and damage interactions are absent. G0 owner source green does **not** mean G1 native PlayMode or G2 Codex acceptance.

## Receiver ownership
Owner B01-32 retains this package until a version-specific OFFERED/FROZEN handoff by B01-40; B01-60 independent QA; Codex only integrates/changes canonical sources. Do not upgrade claim to CODEX_ACK without explicit Codex reply for exact SHA.

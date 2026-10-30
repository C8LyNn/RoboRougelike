# B01-30 / PASS17 WAVE10 — REAL SAFE/BLOCKED DOCK 3D DIAGNOSTICS

**A new genuinely produced 3D visual fixture for the already owned B01-23 physical dock/safe reattachment stream**, not a competing safe reattach source repair. Distinguishes a physical-looking **SAFE** and **HIDDEN-LAYER-8-BLOCKED** target-mount volume for BOTH original light and heavy chassis. The output is 3D source data: **4 actual FBX, 4 native Blender .blend and 4 real rendered PNG** (960×720), produced by Blender from exactly pinned published B01-23 C# constants. No fake Unity asset checks and **NO collision safety, public API, Physics.ComputePenetration, or native Unity claim**.

## Immutable owner source, dimensions, case geometry

Read-only **B01-23 physical W07** [original #439 HEAD `cdcaea750fda1a9d7db6018e34970f4b9f206427`](https://github.com/C8LyNn/RoboRougelike/pull/439), exact original Git blob of `RigDemoDefinition.cs` `b7d1a503c89537316fe48dbb54f5997bb21d14ce`. It contains two genuinely different experimental rigs: Light 1.5 kg / chassis width 1.4m / part width 0.5m / root X -3.5m; Heavy 12kg / chassis width 2.4m / part width 0.7m / root X 3.5m, both with original 0.25m clearance. Parts mount at `RootX + (ChassisWidth + PartWidth)/2 + Clearance`, Y=1.2m, Z=0; part dimensions use original W07 `PartWidth × .65m × .65m`; chassis uses original `ChassisWidth × .8m × 1m`. **These are isolated demo scales, NOT canonical robot/PartSize dimensions.**

Read-only original B01-23 [safe-dock #471 HEAD `763748f1be0c61356fb4462144640bb359364226`](https://github.com/C8LyNn/RoboRougelike/pull/471), exact original Git blob `HardenedDockDemo.cs` `d46c6341db199d010f48de00f98250becd2d7c87`; the authored 3m max distance, 5mm penetration tolerance, all-layer query, real PhysX check, and legacy `Reattach` invocation are source pinned, **not simulated by this fixture**.

Each of **four real independently importable visual FBX** represents one *original target state*, 2 rigs × 2 states:
- SAFE: original chassis (navy), part at target position (green), small amber direction guide.
- BLOCKED_LAYER8: same shapes plus a **physically intersecting red box** centered in the original candidate part target volume. The red box is a deliberate **visual proxy for a foreign blocker on Unity physics layer 8**, NOT actually assigned a Unity GameObject layer in Blender/FBX. It exists to make visually obvious why a layer-filtered safe-dock check could miss a blocker. Safe variants do not contain red geometry.

The source contract verifies the original Git blob byte-for-byte, parses both original C# constructor definitions rather than inventing the values, and runs an intentionally mutated original-source SHA negative. It then exports **4 genuine Blender-authored FBX and 4 editable .blend**, with 4 physically rendered PNG screenshot assets and hashes. **Four live in-memory 3D geometry mutants** translate the dock target into the actual chassis to prove the AABB intersection detector rejects invalid source positioning. A **fresh second Blender process** actually reimports each generated FBX, checks original scene geometry dimensions, target/chassis positions and material class names, two deliberate blocker-vs-target 3D contacts and two intentionally removed blocker negatives. A third Windows runner independently downloads all 12 physical artifacts and checks exact bytes/SHA and that SAFE lacks the red blocker.

## How to use (manual receiver)

Run the dedicated `.github/workflows/b01-30-p17w10-safedock-visual.yml` Actions on the Draft branch or use exact original C# input and the two scripts:

```bash
xvfb-run -a blender --background --factory-startup --python-exit-code 1 \
 --python Tools/B01/TechArt/P17W10/SafeDockVisual001/produce_original_dock_visuals.py -- \
 --source build/B01-30/W10/original --out build/B01-30/W10/producer

blender --background --factory-startup --python-exit-code 1 \
 --python Tools/B01/TechArt/P17W10/SafeDockVisual001/verify_real_fbx.py -- \
 --original build/B01-30/W10/producer --out build/B01-30/W10/receiver
```

The run must have real outputs `Light-SAFE-ORIGINAL-DOCK-VISUAL.fbx`, `Light-BLOCKED_LAYER8-ORIGINAL-DOCK-VISUAL.fbx` and the Heavy counterparts, editable BLEND and PNGs, exact per-file original SHA and machine receipts. **Never promote the visual verdict to native G1.**

## Safety / receiver / scope / rollback

**B01-23** retains ownership of all actual safe-dock code, including fixing the **public API bypass identified in [#472](https://github.com/C8LyNn/RoboRougelike/pull/472) and [#476](https://github.com/C8LyNn/RoboRougelike/pull/476)**. New visualization does NOT fix that defect, block any runtime route, assign Unity Layer 8, calculate Collider/ComputePenetration or modify ledger/rearm. It also **does not certify #471 safe**. **B01-40** independently checks exact 2 C# source SHA, physical 4×3 FBX/BLEND/PNG binaries, 12 per-file receipts and no GUID/path collision before granting visual-art G0. **B01-60** independently import these original FBX into a licensed disposable Unity 6 URP project to inspect axes/scale/appearance and compare to actual B01-23 native physical tests; Unity native G1 **NOT_RUN** here. **B01-00** owns any Codex queue; there is no OFFERED/ACK/INTEGRATED.

Own paths only `Tools/B01/TechArt/P17W10/SafeDockVisual001/**` and the dedicated workflow; **zero Git `Assets/**`**, no merge, no changes to existing source #471/#439/#475/#423, no Codex worktree. **Rollback:** delete only generated `build/B01-30/W10/**`, or the 4 optional visualization FBX imported into a disposable Unity QA project after checking references; keep original physical B01-23 sources intact. This is an observational 3D diagnostic, **not** proof that source API is safe.

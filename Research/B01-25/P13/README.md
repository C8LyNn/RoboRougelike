# B01-25 PASS13 — three minimal source-shaped level fixtures + Codex handoff

**Current original source:** [#310](https://github.com/C8LyNn/RoboRougelike/pull/310) @ `231005c6d0351945a8ee6684f3856af118f0e94f`, MapDraft blob `b5f8f395f93972c0ede000701dbbada8fcfcdfdb`, MapWorld blob `8928908db123b35dcab4b324fe9d3a7a8fceee4e`.

Original PASS11/PASS12 LD01 is **not a reliable mandatory-obstacle challenge**: its ground corridor includes an object-free route from Spawn→Finish. The new LD01_MIN intentionally spans **all four ground lanes** at x=4 with four LowBarrier blocks (yaw=90°), requiring interaction with a barrier to cross the ground corridor. It may be impossible physically; mandatory `Light×3 + Heavy×3` licensed tests decide whether to keep, soften or reject the obstacle. This is **not** a certified playable level.

**LD02_MIN:** 2 Pillars, 1 LowBarrier, Spawn + Finish and GlobalFloor — **geometry sandbox only**. Editor does not spawn enemies or implement kill condition, cover LOS, timed reward, collision gate or risk scoring. If the player drives around all cover to Finish, it proves only the editor's touch trigger.

**LD03_MIN:** local Ground 31 cells, Spawn/Finish and LowBarrier(4,2), Ramp(5,2); short centerline x0..8,z2 = 8 grid transitions. Safe branch via x0,z3→z4, x1..8,z4, then x8,z3→z2 = 12 transitions and two wide supporting floor rows at z4/z5; short corridor has 1 Ground row and may reject Heavy. All measures are *static 4m grid*, not Collider.bounds or actual PhysX.

## Import and operator
1. Pin original file and blob hash from `manifest.json`, use fresh **disposable** Unity profile. `MapStorage` resolves `EditorName.Trim()+'.json'` within `Application.persistentDataPath/Maps`. Do not overwrite existing user maps; D02 is open.
2. Copy each original JSON to its `B01_25_LD0X_MIN_P13.json` destination, select that name in editor and press **Wczytaj**. Confirm Spawn/Finish geometry and available tools; current UI does not browse arbitrary files.
3. With a fully functional weapon and the selected functional mobility drive (otherwise current `VehicleModel.DeployError` blocks testing), press Testuj mapę. Record exact source/build SHA, original filename/blob, rig ID/dimensions, direct input and sampled Rigidbody trajectory, collisions/contacts, observed finish touch, recovery/fall/time, and raw logs. No teleport/robot replacement/sequence splicing.
4. **LD01:** one physical run each Light and Heavy on mandatory 4-segment 16m low-barrier line; extend to 3+3 independent attempts only after safe preflight. Failure of either rig is **real design feasibility FAIL** if observed, not a false Unity API FAIL.
5. **LD02:** drive around cover without counting two enemy kills; `COMBAT_OBJECTIVE_BLOCKED_BY_CODEX_AND_HQ`. Enemy LOS opaque policy pending.
6. **LD03:** independent reset between short and detour; observe physical traversability and return; Light vs Heavy may differ. Required before any claim of fastest branch: ≥3 repeated independent completed trials per supported rig/branch, real times and exact map/vehicle hash.

## FINISH vs actual challenge outcome
The current editor implements `MapWorld.Finished` after player-associated Rigidbody touches Finish, not completed route/contact evidence, enemy kill, timeouts or rewards. Keep labels `SANDBOX_TOUCH_ONLY` vs `DESIGN_TARGET_UNVERIFIED` until HQ's sandbox/route policy and Codex changes. CHL-003 #285 is a **different authored course** with CP0–CP3; its SOURCE G0 does not certify these fixtures or permit G1.

## Handoff
- **B01-21**: inspect ground geometry, Heavy turning width, expected route at exact SHA; no cloned map generator.
- **B01-10**: acceptance fairness of forced barrier and choice; whether garage weaponless should be allowed in map test is product policy for HQ.
- **B01-24**: separate enemy LOS/clear vs blocked cover check for combat after Codex encounter is approved.
- **B01-60 / HQ**: genuine authorized Unity Physics/Windows operator, player trace with original input; don't assume access.
- **Codex**: canonical game owner, only after HQ map policy/encounter; no suggested third producer package or merge.

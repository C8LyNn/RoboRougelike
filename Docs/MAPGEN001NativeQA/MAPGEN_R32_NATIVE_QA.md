# MAPGEN-001 R3.2 — native Unity QA

Dispatch: `HQ-B01-20261025-CODEX-MAPGEN-NATIVE-QA`. Executed 2026-10-25 on Windows, Unity **6000.3.14f1**. **Overall FAIL / integration HOLD.** Native compilation succeeds and all 19 real R6 route attempts finish, but the original native suites contain two failures and generated surfaces have a standalone URP rendering defect. This is QA evidence, not acceptance by B01-60 or G2 approval.

## Immutable inputs and isolation

| Input / execution snapshot | Exact SHA |
|---|---|
| Complete producer source, stacked PR99 + PR103 + PR211 | `003bced54480d509ffc0d49d26daf87160a8d609` |
| Target OBJ001 PR210, including PHY R1 / PR125 | `4c9236a4cdc27814ea2e7679872ec4a524150cc8` |
| Included PHY R1 source | `916654634d1bfdc2bbc14e64b9eeac856ede1fa8` |
| Imported original source / original native suites | `dbba5d000d9bcac98d3bf79983d611ce10ffc704` |
| Matrix build code | `18442559982c5a213ac86b7abd0ef817005afc2d` |
| Matrix effective source + saved QA scene / launch HEAD | `ebcdf7252b24a00add76e61796c6593191389292` |
| Optional probe build code | `994b193db82f0839cea4763208671d521ac20c69` |
| Optional probe launch HEAD | `4877957710a0098e3f42d787454d46cd1cc20cae` |
| Probe effective generated scene captured after build | `58d5d8b89cd1277a51dc03df244809512af2f475` |

QA branch: `qa/rrl-mapgen-r32-native`; base branch: `feature/rrl-obj001-integration`. Separate sibling checkout at `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-MAPGEN-R32-QA`. Existing worktrees were preserved. The native managed-worktree tool was unavailable from the projectless Desktop, so a new Git worktree was created manually. No Library was copied. No active-game scene, OBJ, PHY, R3/R6, ProjectSettings or Packages file was changed. Dedicated QA scene only; no automatic runtime initialization hook or gameplay entry point. CAMERA/BUILD/RAC packages remain owned by B01 and were not repaired here.

The complete source tree was imported, not PR211's small stacked diff. [Integrity record](source-integrity.json): **46 source/doc + 5 optional-tool declared SHA256/size entries PASS**, all **53 imported files byte-identical** to the pinned Git tree, **0 duplicate GUIDs**. `Assets/B01Packages/MAPGEN001.meta` is omitted from the producer hash/ZIP entry list; it was copied and independently compared to Git. The other entry outside the hash map is the manifest itself (intentional recursive-hash exclusion). The producer's downloadable ZIP hash was **NOT independently verified**; this audit verifies the imported exact Git files and declared manifest. B01 should include the owned root folder meta in its package gate/ZIP.

PR211 was checked again after native testing: still draft/open at `003bced...`; no new immutable producer revision was received. Original package source and tests remain unchanged.

## Actual native results

| Check | Result | Native process exit / evidence |
|---|---|---|
| Original package Unity compilation | **PASS** | Actual NUnit execution; no C# compiler errors |
| Original EditMode | **FAIL: 22 PASS / 1 FAIL / 0 skipped**, 23 total | **2**; [XML](OriginalUnity/EditMode.xml), [Editor log](OriginalUnity/EditMode.log), [process](OriginalUnity/EditMode-process.json) |
| Original PlayMode | **FAIL: 5 PASS / 1 FAIL / 0 skipped**, 6 total | **2**; [XML](OriginalUnity/PlayMode.xml), [Editor log](OriginalUnity/PlayMode.log), [process](OriginalUnity/PlayMode-process.json) |
| QA standalone build attempt 1 | **FAIL, own harness GUID error** | **1**, separately retained; not a producer defect |
| QA standalone build attempt 2 | **PASS** | **0**, 0 build errors; four existing prototype warnings |
| QA standalone build attempt 3, optional probes | **PASS** | **0**, 0 build errors / 0 build warnings |
| Real R3/R6 calibration | **PASS 2/2**, repeated Arena7 controls | **0**, no logged console errors |
| Real R3/R6 required + historical matrix | **PASS 19/19 attempts, 17 distinct configs** | **0**, no logged console errors; [all rows](PHYSICAL_TRIALS.md) |
| Extra optional side-ramp probes | **Light PASS exit / Heavy FAIL_PROBE_SPAWN** | **2**, intentional failed diagnostic status; no logged console errors |
| Standalone source rendering | **FAIL** | Magenta generated surfaces; runtime shader `Hidden/InternalErrorShader`, target `UniversalRenderPipelineAsset` |
| Live OBJ battle, garage/mode transitions, whole-game regression | **NOT_RUN** | No production integration authorized |
| Manual keyboard driving, independent B01-60, G2 | **NOT_RUN / HOLD** | API drive evidence cannot substitute for these gates |

All 23/6 counts are actual native NUnit cases, not the producer's 1013 surrogate-math cases. EditMode includes hundred-seed/determinism/clearance and extremes loops, both recipes, narrow-lane regression seeds 2/12/14/22/28, blocked/overlapping spawns, wall obstruction, slope, sharp-turn, non-finite and impossible configuration rejection. Five passing PlayMode cases exercise real generated colliders/fall-and-land, transformed host world metadata, repeated regenerate/clear, and rejected input preserving the prior live level.

## Reproduced source issues and producer actions

1. **Edit fixture diagnostic mismatch.** `MapGenRouteValidationTests.CentralPitCannotPassOnlyFourCornerFootprintProbe`, source line87: expected `SPINE_FOOTPRINT_BLOCKED`, actual `START_BLOCKED`. Start and finish are the same unsupported central pit between four small corner supports. The validator rejects the unsafe map earlier; this does **not** demonstrate unsafe acceptance. B01 should update the expected diagnostic or build a true middle-route pit fixture with supported start/finish. Original failed assertion retained.
2. **Original cube traversal test fails its first case.** `MapGenPhysicalTraversalTests.LightAndHeavyRigidbodyDriveThreeSeedsForBothRecipes`, line64: CombatArena / Light / seed7, waypoint0/1, position `(-0.11, 0.50, -24.47)` after 900 fixed steps. The remaining **11 cube cases were not reached**, not executed failures. Primitive cube/default friction and guidance need producer investigation; no root cause or fix is claimed. Real R6 results below do not turn this original assertion into PASS.
3. **Generated surface material fails in this standalone URP host.** Raw 720p and 1080p matrix images show magenta surfaces, while existing R3 vehicle materials render normally. Runtime reports the error shader above. Exact default-material initialization versus shader inclusion cause remains unseparated. B01 should provide a pipeline-compatible material path and validate the actual native host. The optional extra probe uses a clearly labelled test-only renderer material override; this is **not** a shipped fix or original visual certification.
4. **Root folder meta omitted from producer package manifest/ZIP list.** Import from Git preserves its original GUID here. Producer packaging should retain it rather than rely on a regenerated Unity GUID.
5. **Optional ramp/access envelope limitation.** Course7, segments4/density50/lane16/difficulty0.5, ramp width3m/pitch6deg: Light traverses the ramp; Heavy's actual width5.18m cannot fit the chosen side approach, which overlaps a boundary. This is an extra probe spawn failure, **not** a generated `Layout.Start` failure or failure of the protected Heavy route. Source describes these as optional Light ramps; B01/HQ should confirm the restriction or widen access if Heavy traversal is intended.

No competing generator implementation or producer fix was made. These findings/reproductions are routed to B01 through issue93 and copied to HQ issue79. Retesting a source fix requires a new complete immutable producer SHA and refreshed manifest.

## Real vehicle method and matrix

Actual `R3VehicleCatalog` / `R3VehicleModel` / `R3VehicleAssembly.Build` rigs, canonical mass/parts/COM, original `R6VehicleDrive.Step` and `R6WheelContacts`, natural FixedUpdate/PhysX/gravity and free Rigidbody rotation. The normal input controller is disabled; QA supplies throttle/steering through the existing force drive. Gameplay input capability is retained for canonical recovery, but recovery attempts are zero. No second solver, traversal teleport, frozen roll/pitch, or body velocity writes. Initial factory spawn/reset is per independent row. **These are API-steered physical Windows Player runs, not human gameplay.**

| Rig | Canonical mass | Parts | Wheels | Measured collider envelope W×H×L m | Planner profile used W×L m |
|---|---|---|---|---|---|
| Light | 192kg | 20 | 4 | 2.980×1.564×2.720 | 2.980×3.2 |
| Heavy | 1176kg | 139 | 12 | 5.180×2.114×4.920 | 5.180×5.8 |

Profiles use max(nominal, measured actual collider footprint); nominal source widths2.1/4.5 understate these target rigs. A future game adapter must calibrate envelopes rather than assume the source reference dimensions equal the actual assembled vehicle. Source geometry was not retuned or rerolled.

Required matrix: Arena + Course × Light + Heavy × seeds7/31/83 = **12/12 PASS**. Historical Heavy Course normal2/7/31 and hard0/2/7/19 = **7/7 PASS**. Normal7/31 are intentional repeats of two base configurations, so **19 attempts / 17 unique configurations**. Fixed segments4/density50/lane16; normal difficulty0.5, hard1.0. Each follows generated checkpoints then finish; horizontal finish tolerance2.4m, finite65s deadline and stall/fall/flip checks. [Full 19-row table](PHYSICAL_TRIALS.md), [raw trial JSON](Matrix/wheel-trials.json), [0.1s CSV trace](Matrix/wheel-trace.csv), [process](Matrix/process.json), [launch and assembly hash](Matrix/launch.json).

Arena distance48.14–48.16m, Light≈6.7s / Heavy≈10.8s. Course54.28–54.68m, Light≈7.9s / Heavy≈12.4s. Course crosses three floor seams. All rows retain20/20 or139/139 parts, no recovery, fall, flip, unresolved stall or non-floor collision events. Optional side ramps are avoided on this protected route and are **not covered by those PASS results**.

Contacts are actual wheel queries after warmup at configured0.02s cadence. Light Course7/31/83 each has **one zero-contact query** (337/338,335/336,336/337 grounded); all other rows have no zero queries. Raw `longestNoContact=0` undercounts a single-query episode, so it must not be read as continuous contact or a measured zero-duration airborne event. Substep contact duration was not measured. Maximum Light course body tilt≈10.56deg; Heavy contact minimum10–12/12. This evidence supports these tested routes, not arbitrary seeds/obstacles or continuous contact guarantees.

## Separate optional ramp probes

These start beside the optional ramp facing low→high, **not** at generated Layout.Start, and stop after the ramp exit, **not** the full course finish. No repositioning during drive. Existing URP vehicle material assigned only to generated renderers under `-mapgen-material-override`; colliders/physics unchanged. Images carry the diagnostic override label.

| Probe | Result | Measured evidence |
|---|---|---|
| Light / Course7 / normal | **PASS ramp exit**, full finish false | 10.564m / 4.223s; 8 ramp collision events; maximum body tilt16.297deg |
| Heavy / Course7 / normal | **FAIL_PROBE_SPAWN**, no drive | Actual `Physics.ComputePenetration` at the chosen approach against boundary; distance0, about1.224s warmup; 103 non-floor collision events |

Heavy has no sampled traversal trace after rejection: raw sentinel minimumContacts999 and zero screenshot dimensions in that row are **N/A**, not measurements. The PNG header itself is1920×1080. [JSON](RampProbes/wheel-trials.json), [CSV](RampProbes/wheel-trace.csv), [exit2](RampProbes/process.json). This failed optional probe is preserved, not added to the 19 successful full-route rows.

## Evidence, builds and reproduction

[Native summary, screenshot dimensions and SHA256](native-summary.json). **57 raw matrix PNGs + 4 extra probe PNGs** retained. Examples:

- [Original generated rendering, Light720p](Matrix/BASE-ObstacleCourse-Light-7-driving.png)
- [Original generated rendering, Heavy1080p](Matrix/BASE-ObstacleCourse-Heavy-7-driving.png)
- [Light optional ramp, labelled material override](RampProbes/RAMP_PROBE-ObstacleCourse-Light-7-driving.png)
- [Heavy blocked optional approach](RampProbes/RAMP_PROBE-ObstacleCourse-Heavy-7-end.png)

[Compressed native build/Player logs and native process results](native-build-and-player-logs.zip), plus uncompressed original NUnit XML/logs above. Public logs omit connection IDs/IP and machine ID only; full originals remain in ignored `Artifacts/Local`. Physical diagnostic XML uses `native-physics-diagnostic/trial` and **is not NUnit XML** or an invented PlayMode count. Calibration is repeated controls and not extra unique coverage.

The first own QA build failed because its Editor script meta had a33-character GUID and Unity could not find the execute method. Repository validator reproduced the bad GUID; fixing only the QA meta to32 hex digits made validation pass and build2 succeeded. Attempt1 is retained separately. No producer asset was changed to hide the error.

Matrix Assembly-CSharp SHA256: `9ccaca5f092b988504e981a5c7f9e740983730df7d725386a573d1442a89ce8e`.
Probe Assembly-CSharp SHA256: `8b60ecc5d7b2c468212f8b29648621e5426c3e288039bdab63c9a4530bf36e16`.
Build3 regenerated the owned QA scene before probe launch; that scene was uncommitted at launch HEAD4877957 and was captured unchanged in58d5d8b. Thus launch HEAD alone is not advertised as a clean full probe snapshot. The older matrix binary was overwritten by the probe build; original tested source, binary assembly digest, launch/process/logs and telemetry remain. No production release ZIP was requested or certified.

Reproduce in another **new checkout**, never the live game. Fetch this QA branch and checkout exact recorded test snapshot (or the published QA PR HEAD for the full evidence/tools), with Unity6000.3.14f1. Original package suites do not require the custom QA harness. The committed QA branch includes `Tools/B01Validation/Run-PackageTests.ps1`; run:

```powershell
python Tools/B01Validation/verify_mapgen_r32.py
powershell -NoProfile -File Tools/B01Validation/Run-PackageTests.ps1 -Filter B01.MAPGEN001.Tests -Mode Both -Label MAPGEN-R32-repro
# Expected original native outcomes: Edit22/23, Play5/6, exits2; no source repair in this branch.
```

To recreate the standalone diagnostics, use matrix snapshot ebcdf725 for the original matrix, or probe effective snapshot58d5d8b for the optional modes:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe' -batchmode -quit -projectPath (Get-Location).Path -executeMethod MapGenR32QABuild.Build -logFile build.log
# Wait for the actual Editor process and check its native exit before launching.
powershell -NoProfile -File Tools/B01Validation/Run-MapgenWheels.ps1 -Label MAPGEN-R32-repro
# Optional extra probes only (probe snapshot or later):
powershell -NoProfile -File Tools/B01Validation/Run-MapgenWheels.ps1 -Label MAPGEN-R32-ramp-repro -RampProbe -MaterialOverride
```

Local diagnostic executable currently at `Builds/MAPGEN-R32-QA/MapgenNativeQA.exe` under this isolated checkout. It is a finite QA runner, not an installed game activity. Build commands regenerate the QA scene; record resulting files and assembly hash as well as launch SHA. Windows graphical shell invocation can return before Editor completion: wait on its process/exit, not only `$LASTEXITCODE`.

## Gate decision

**Native source compilation PASS; original EditMode FAIL; original PlayMode FAIL; real protected-route matrix PASS19/19; optional ramp probe mixed; rendering FAIL. Overall FAIL / integration HOLD.** No merge, production scene install, new gameplay or package-source correction was performed. OBJ/PHY code is preserved; contact/actual-rig compatibility is tested only within the scope above, not a live reactor battle or complete game acceptance. B01 receives concrete failures and must provide revised immutable sources for retesting. Independent B01-60 reception and B01-00 G2 remain outstanding.

QA infrastructure checks executed after collecting evidence: Python CI tool tests **16/16 PASS**; repository validator **1915 tracked files, 0 errors**; diff whitespace check **PASS** with repository CRLF policy; exact source audit repeated **53/53 byte-identical, 0 GUID collisions**. These are infrastructure checks and do not change either native NUnit failure into PASS.

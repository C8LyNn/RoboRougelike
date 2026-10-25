# W12 — MAPGEN R3.4.1 native retest

2026-10-25. **PASS in the requested native QA scope; candidate ready for independent B01-60/B01-40 review. G2 remains HOLD.** The CS1061 fix works, all43 original Unity NUnit cases pass, both recipes render correctly in a minimal Windows URP host, material lifetime checks pass, and12 distinct real R3/R6 routes finish. No producer repair, game installation or merge was performed.

## Immutable source, history and target

| Item / actual execution snapshot | Exact SHA |
|---|---|
| Frozen producer R3.4.1, PR237 | `5be31039411f89f17654b1c332f8692883fa03cc` |
| Current game OBJ210 including PHY125 | `4c9236a4cdc27814ea2e7679872ec4a524150cc8` |
| Included PHY125 | `916654634d1bfdc2bbc14e64b9eeac856ede1fa8` |
| Original native compile/Edit/Play | `9a45352dbf857a9ee4293af57b6f59ca1b17ec90` |
| Own diagnostic code / minimal-material build | `28a75b9af3611bd2c2d29062860e4232bc8acbae` |
| Saved minimal scene / material Player / wheel build code | `4fd9c35d8b19559474b8c0d302b23d38c1d85a05` |
| Saved wheel scene/material / both actual wheel launches | `dd20d641621ae2bb816d1d97423a814feb67ec7d` |

[Exact history receipt](source-history.json) verifies all six frozen PR heads are ancestors of the tested source:99 `6dd21a72c6ec1590015042be6775a7bd0f57f3e1`,103 `ac7d957799588452f831a645162a0ab7c05246e0`,211 `003bced54480d509ffc0d49d26daf87160a8d609`,225 `7334f3b1d8cd3a976098faa4e683f4de3d9a592f`,235 `0024731e1de0c0814968bd91d860a6ac9bb45385`,237 `5be31039411f89f17654b1c332f8692883fa03cc`. Full owned tree imported, not just the final one-line diff.

New clean worktree: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-MAPGEN-R341-QA`, branch `qa/rrl-mapgen-r341-native`, directly from4c9236a. No Library copy. Native app worktree creation was already unavailable in this projectless chat (Not a git repository in earlier dispatches), so Git fallback created this requested separate checkout. All12 prior checkouts/branches and evidence remain preserved; no active Unity/QA Player at initial check, no AGENTS reservation file found.

`Assets/B01Packages.meta` was absent in this fresh target, checked before importing the original owner GUID; no existing parent GUID was overwritten. [Integrity receipt](source-integrity.json): **56 source/doc/meta +6 optional-tool declared SHA256/byte entries match;63 complete imported files byte-identical to the pin;0 GUID collisions.** Manifest itself is the intentional self-hash exclusion. All Resources shader/metas included. Producer ZIP was not independently downloaded/hashed. Native source files, Unity6000.3.14f1, URP/Packages/ProjectSettings and existing R3/R6/P12/game scenes are unchanged. Original meta/Markdown whitespace was retained; formatting checks apply to own QA additions.

## Actual results

| Stage | Actual result | Native evidence |
|---|---|---|
| Project compilation, Unity6000.3.14f1 | **PASS**, native0 | [Editor.log](Compile/Editor.log), [process](Compile/process.json) |
| Original EditMode | **24/24 PASS**,0 skipped, native0 | [NUnit XML](OriginalUnity/EditMode.xml), [log](OriginalUnity/EditMode.log), [process](OriginalUnity/EditMode-process.json) |
| Original PlayMode | **19/19 PASS**,0 skipped, native0 | [NUnit XML](OriginalUnity/PlayMode.xml), [log](OriginalUnity/PlayMode.log), [process](OriginalUnity/PlayMode-process.json) |
|12 independent physical cube cases | **12/12 PASS** | [Per-case table](CUBE_AND_FLAT_CASES.md), original NUnit XML |
| Flat actuator control | **PASS** | `CubeForceActuatorMovesOnFlatControlSurface` actually executed |
| Minimal Windows x64 material build | **PASS**, native0;0 errors/4 warnings | [Editor.log](MaterialBuild/Editor.log), [process](MaterialBuild/process.json) |
| Minimal Windows material Player | **4/4 PASS**, native0,0 logged errors | [JSON](Materials/material-results.json), [Player log](Materials/Player.log), [process](Materials/process.json) |
| Real-vehicle Windows x64 build | **PASS**, native0;0 errors/0 warnings | [Editor.log](WheelBuild/Editor.log), [process](WheelBuild/process.json) |
| First4 real R3/R6, seed7 | **4/4 PASS**, native0 | [JSON](WheelsFirst4/wheel-trials.json), [process](WheelsFirst4/process.json), [Player log](WheelsFirst4/Player.log) |
| Extension, only new seed31/83 cases | **8/8 PASS**, native0 | [JSON](WheelsRemaining8/wheel-trials.json), [process](WheelsRemaining8/process.json), [Player log](WheelsRemaining8/Player.log) |

**43 actual original NUnit cases**, not the producer's1015 surrogate-math cases; cube12 +flat1 +other native Play6 are included in19, not additional counts. Standalone4material observations and12real-wheel attempts are separately labelled diagnostics, **not extra NUnit cases**. Their XML roots explicitly say non-NUnit. One original compile/Edit/Play cycle, one minimal material build/Player and one vehicle build with4+8 distinct route attempts; no original-suite rerun, producer/source fix or additional agent.

## Did the previous fixes work?

**CS1061: yes.** The source now calls Material.GetTag. Unlike R3.4's actual native exit1/CS1061 before test execution, R3.4.1 compiles and runs every requested original test.

**Cube fixture stall: resolved for all12 specified cases.** R3.3 had12 failures before the first waypoint with about0.55m displacement. Current unchanged R3.4.1 assertions all pass, plus the independent flat control's >4m criterion. Producer low-friction material applies only to its cube fixture; forces, gravity, Rigidbody,900-step route cap and finish assertions are unchanged. No receiver teleport, assertion weakening or timeout increase. This confirms the current fixture works in these cases; original counteracting contact impulses were not measured in R3.3, so no stronger old root-cause claim is made.

Successful producer cube tests do not print their numeric position/velocity/support-ray diagnostics (those are failure-message fields). The XML proves their actual unchanged assertions; no numeric pass telemetry is invented. `floorRaySamples` is support-ray evidence, not PhysX contact-manifold count. Crucially, the independent real-wheel results below establish R6 passage separately.

**Standalone shader retention/rendering: yes.** R3.3's minimal Player failed both recipes withURP_SHADER_NOT_AVAILABLE. This minimal Player loads `Resources.Load<Shader>("MAPGEN001_BlocksURP")`, reports the actual `B01/MAPGEN001/BlocksURP` shader supported, and renders both recipes at both resolutions. No built-in URP material reference/vehicle template was present when building/running this minimal host; no material/shader override or GraphicsSettings change masked the result.

## Actual minimal Windows rendering and material lifetime

Graphics: Direct3D11 / NVIDIA GeForce RTX5080; unchanged `UniversalRenderPipelineAsset`. Four rows: Arena7 and Course7 at1280×720 and1920×1080, default config/Light reference profile. All generated renderers share one source-owned material, match the loaded Resources shader, and report `UniversalPipeline` tag.

Each row performs **three actual Regenerate/Clear cycles**, checks prior root deactivated immediately and destroyed after a frame, verifies no remaining owned child root after Clear, and confirms material identity reuse. Observed owned-material count stays1 at all seven sampling points per row. **Baseline0 → cache1 after Clear while builder lives →0 after builder destruction.** A single retained builder cache is intentional, not a leak. All four rows have generated/lifecycleRan/oldRootsRemoved/clearRemoved/sameMaterial=true and no errors.

- [Arena720p](Materials/CombatArena-1280-original-resource.png)
- [Arena1080p](Materials/CombatArena-1920-original-resource.png)
- [Course720p](Materials/ObstacleCourse-1280-original-resource.png)
- [Course1080p](Materials/ObstacleCourse-1920-original-resource.png)

Arena720p and Course1080p were visually inspected; all four PNGs also have0pixels under the diagnostic magenta thresholdR>200,G<70,B>200. Dimensions/hashes are retained. This is actual generated rendering, not a replacement scene or stale screenshot.

## Real R3/R6 passage, not cube substitution

[Full12-row matrix](REAL_R6_MATRIX.md) contains last worldXYZ, cumulative distance, seconds, contacts, zero queries, seams, parts and finish. First4 cases use seed7; the second process runs only31/83. **12 distinct configurations, no duplicated first4**, same exact Player assembly digest and source/scene snapshot for both processes. The account still had65% of the five-hour budget before extension, so no budget-driven omission was needed.

Actual `R3VehicleCatalog` + `R3VehicleModel` + `R3VehicleAssembly.Build`, original `R6VehicleDrive.Step` / `R6WheelContacts`, real free Rigidbody/gravity/colliders. Input controller disabled and QA supplies bounded throttle/steering through the canonical force solver. No replacement solver, body-velocity assignment, frozen roll/pitch or travel teleport. Factory initial spawn/reset is per independent case. **API-controlled physics, not keyboard/mouse play.**

| Rig | Parts/wheels | Actual mass | Measured collider envelope W×H×L m | Body COM localXYZ m |
|---|---|---|---|---|
| Light |20 /4 |192kg |2.980×1.564×2.720 |0 /0.06875 /0 |
| Heavy |139 /12 |1176kg |5.180×2.114×4.920 |≈0 /0.25816 /-0.03367 |

Caller profile uses max(reference, measured actual footprint): LightW2.980/L3.2, HeavyW5.180/L5.8. Reference widths2.1/4.5 alone understate these rigs. No planner or map geometry was retuned/rerolled. Config: segments4,density50,lane16,difficulty0.5.65s cap and2.4m horizontal checkpoint/finish tolerance are unchanged from the prior verified R6 diagnostic.

All12 reach all generated waypoints/finish. Accumulated movement48.138–54.544m; it includes initial settling, not a pure horizontal odometer. No parts lost, falls, flips, recovery attempts, unresolved stalls or non-floor collision events. Each Course row crosses3floor seams.

Contact detail matters: Light Course7 and83 each have **one zero-contact query**; Light31 minimum2/4, Heavy courses12/12. Own QA's previously disclosed single-query duration undercount was corrected in the versioned receiver harness: the counter now estimates consecutive unsupported queries×fixedDelta0.02s. The two episodes show0.020s query-time estimates; this is **not a measured continuous airborne duration** or a continuous-grounding guarantee. No R6 production code was changed.

Optional side ramps are avoided on this protected route. Earlier R3.2 optional Heavy side-ramp access limitation/design policy was not adjudicated here; that historical issue is not converted into a new PASS. Historical hard seeds0/2/7/19 and arbitrary player builds were not repeated beyond the requested matrix. No whole-game/OBJ combat/garage integration was authorized or certified.

## Evidence and reproduction

40actual PNGs:4minimal material captures +36real-vehicle captures; [all dimensions/hashes, raw cases, launches, process codes](native-summary.json). Both builds retained locally, never overwritten with each other:

- `Builds/MAPGEN-R341-Materials/MapgenQA.exe`
- `Builds/MAPGEN-R341-Wheels/MapgenQA.exe`

These are finite QA runners, not an integrated game release. Full raw logs remain under ignored Artifacts/Local; public copies omit machine/connection identifiers and trailing whitespace only. No UI automation was attached. Each build/Player launch records actual SHA and binary assembly digests; no dirty saved-scene ambiguity.

Original suite reproduction: checkout9a45352 (runner included), Unity6000.3.14f1:

```powershell
$qaPython='C:\Users\kubap\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
& $qaPython Tools/B01Validation/verify_mapgen_r341.py
powershell -NoProfile -File Tools/B01Validation/Run-PackageTests.ps1 -Filter B01.MAPGEN001.Tests -Mode Both -Label MAPGEN-R341-repro -Python $qaPython
# Recorded actual expected results:24/24 and19/19, native0/0.
```

Material reproduction: checkout4fd9c35 (minimal saved scene, no vehicle template yet), build Unity `-batchmode -quit -projectPath <checkout> -executeMethod MapgenR341QABuild.Materials -logFile <Editor.log>`, wait for native exit, run `Builds/MAPGEN-R341-Materials/MapgenQA.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11 -mapgen-material-output <directory> -logFile <Player.log>`.

Vehicle reproduction: checkoutdd20d64, build with executeMethod `MapgenR341QABuild.Wheels`, wait for native exit, then `powershell -NoProfile -File Tools/B01Validation/Run-R341Wheels.ps1`. Only after success run the same script with `-RemainingOnly`; it excludes seed7 and requests eight new cases. No source or assertion change is needed for reproduction.

## Remaining work and gate recommendation

**No new blocking producer defect was observed in the requested R3.4.1 scope. No producer correction was applied or is demanded by these passing results.** Previously blocked compilation/cube/minimal-Player issues now have native positive evidence. The two brief wheel-support query gaps are disclosed observations without route failure; optional Heavy side-ramp policy remains outside this acceptance matrix.

Recommend handing the complete frozen source and evidence to **B01-60 for independent reception and B01-40 for target/source/GUID/scope review**. This receiver report does not sign their gates. B01-00/HQ must decide G2 separately; active-game adapter selection, transitions/encounters, player input and full gameplay regression remain future authorized integration work. Current game, R3/R6/P12, other packages and shared branches are unchanged. Draft QA only, no main/develop merge. Task stops after publication.

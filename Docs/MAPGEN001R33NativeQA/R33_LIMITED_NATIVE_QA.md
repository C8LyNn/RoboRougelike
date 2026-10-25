# MAPGEN R3.3 — one limited native QA cycle

2026-10-25. **Overall FAIL / future integration HOLD.** No producer repair, game installation, merge, parallel agents, .NET1015 rerun or unrelated package work. Stopped after this bounded validation and publication. Independent B01-60/G2 approval is not granted.

## Inputs and isolated environment

- Full producer source: **`7334f3b1d8cd3a976098faa4e683f4de3d9a592f`**, PR225 containing complete99+103+211+225 tree, original parent/owned metas, docs and tools.
- Target game: OBJ210 **`4c9236a4cdc27814ea2e7679872ec4a524150cc8`**, includes PHY125 `916654634d1bfdc2bbc14e64b9eeac856ede1fa8`.
- Reused clean isolated checkout `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-MAPGEN-R32-QA`, new branch **`qa/rrl-mapgen-r33-native`** from R3.2 QA **`d799878ce8f814b74bd47f8c664c86a954a1a1bb`**. Previous branch/PR226 and ten other worktree HEADs preserved. No local changes or active Editor/QA Player at initial check; no AGENTS reservation file found. No current game source/scenes, ProjectSettings, Packages, main or develop changed.
- Native compile + original suites snapshot: **`78fea95be859b23037702c9aeee000a50ff25a18`**.
- Material diagnostic build code: **`0da17bb0d568b040ae6519ff1086fc1be86979b7`**; exact saved scene committed before Player launch: **`4c2074c53deee0a6f32e22c4d3eedf4635dff871`**. No dirty-scene provenance ambiguity at launch.
- [Manifest/byte/GUID verification](source-integrity.json): **50 source/doc/meta +5 optional QA tools SHA256/size PASS;56 complete imported files exactly match Git;0 GUID collisions**. Only manifest itself is outside its self-hash map. Previously omitted `Assets/B01Packages/MAPGEN001.meta` is now covered correctly. No claim of independently verifying the downloadable producer ZIP.
- Visible five-hour limit: 32% remained at start,27% remained after native tests and material Player, before report publication. No reset used. Four additional physical runs were skipped because of the native blocker, not exhaustion of the limit.

## Requested result table

| Stage | PASS / FAIL / NOT_RUN | Actual evidence |
|---|---|---|
| Unity6000.3.14f1 project compilation | **PASS**, native exit0 | [Editor.log](Compile/Editor.log), [process](Compile/process.json) |
| EditMode | **PASS 24/24**,0 skipped, native exit0 | [XML](OriginalUnity/EditMode.xml), [Editor log](OriginalUnity/EditMode.log), [process](OriginalUnity/EditMode-process.json) |
| PlayMode | **FAIL:6 PASS /12 FAIL**,18 total,0 skipped, native exit2 | [XML](OriginalUnity/PlayMode.xml), [Editor log](OriginalUnity/PlayMode.log), [process](OriginalUnity/PlayMode-process.json) |
| 12 independent cube tests | **FAIL:0 PASS /12 FAIL /0 NOT_RUN** | [12 separate case results/reproductions](CUBE_CASES.md), original NUnit XML above |
| URP materials | **FAIL in standalone Player**; Editor material assertionPASS | [actual Player result](MaterialPlayer/material-results.json), [Player log](MaterialPlayer/Player.log), [exit2](MaterialPlayer/process.json); native buildPASS/exit0/[log](MaterialBuild/Editor.log) |
| 4 targeted real R3/R6 drives | **NOT_RUN, all4** | Condition C not met: source PlayMode blocker; no R3.2 PASS results relabelled as R3.3 |
| Bugs requiring producer work | **FAIL / OPEN** | All12 cubes stuck before first checkpoint; both recipes throw missing-URP-shader exception in minimal standalone host |

## Actual native findings

**Diagnostic fixture fix works.** Both supported-endpoint central-pit rejection and the separate unsupported-start case pass natively. All24 EditMode cases pass. No assertions were altered by Codex.

**All12 cube cases actually executed independently.** Arena/Course × seeds7/31/83 × Light/Heavy. Each runs900 physics steps and fails at waypoint0 (Arena0/1, Course0/5). Typical final positions: Arena Light(-0.11,0.50,-24.47), Heavy(-0.12,0.50,-24.46); Course Light(-0.11,0.50,5.52), Heavy(-0.12,0.50,5.54). Maximum horizontal displacement≈0.555–0.559m; final speed≈0.003. Each full failure message and its seed/rig is retained in [the table](CUBE_CASES.md) and XML. Light mass100kg/Heavy600kg, primitive cube, linear damping0.35, force acceleration clamped±7, default map config and source profiles. This is cube-harness failure, not proof that the real R6 chassis cannot drive. Static project gravity is(0,-9.81,0); runtime traction/contact/friction cause has not been established. Producer should instrument and repair the actual physical test/controller rather than weaken finish assertions. No repair/retest loop was started here.

**Six other PlayMode cases pass.** Real collider generation/clear, fall-and-land, world metadata on transformed host, three regeneration cycles removing old roots, rejected generation preserving prior map, and new URP shader assignment. That material test verifies Editor-side shader/material objects and one frame; it does not prove shader inclusion/rendering in a Windows build.

## Standalone URP result and lifecycle limitations

A dedicated QA-only scene contains a camera, directional light and source `MapGenBuilder` diagnostic. It uses unchanged target URP settings. It does **not** include a vehicle template/extra material asset, force shaders into the build, alter Always Included Shaders, or replace `Renderer.sharedMaterial`. This is a minimal opt-in package host, distinct from the previous R3.2 vehicle scene that referenced an existing URP vehicle material.

Actual Windows x64 build: **Succeeded,0 errors/0 warnings, native0**. Actual D3D11 Player starts with `UniversalRenderPipelineAsset`; its source `Generate` calls fail for **CombatArena7/Light and ObstacleCourse7/Light**, default config:

```text
System.InvalidOperationException:
MAPGEN001_URP_SHADER_NOT_AVAILABLE: provide URP Lit or Simple Lit shader
at B01.MAPGEN001.MapGenBuilder.GetGeneratedBlockMaterial()
at B01.MAPGEN001.MapGenBuilder.Generate(...)
```

Thus both `Shader.Find` candidates are unavailable in this built host. Missing shader retention/reference is the likely packaging dependency; exact stripping cause is not proven. This result does not establish failure in every host containing a referenced URP material. B01-20 should provide/document a build-retained shader/material dependency and verify actual standalone rendering. Editor availability alone is insufficient. No test-only material injection was used to hide the result.

**Screenshots of generated maps: NOT_RUN / unavailable.** Both calls throw before a map can be rendered, so no valid Arena/Course screenshot was produced. No empty frame is advertised as a rendered-map PASS.

**Material regeneration/clear/destroy lifecycle in Player: NOT_RUN.** Planned counts cannot execute after generation fails. Baseline/after-clear/after-destroy owned material counts are all0 because no source material was created. Raw `sameMaterial`, `oldRootsRemoved` and `clearedRootsRemoved` default-true fields do not constitute measured PASS; arrays are empty. The final own diagnostic error `Unexpected builder cache count after Clear` is downstream of its unmet successful-generation precondition, not an additional producer leak. Editor collider-root regeneration/clear passed separately; no material-leak guarantee is claimed. The producer caches one material until builder destruction by design; a retained cache after Clear alone would not be a leak.

Player Assembly-CSharp SHA256: `5398414eb40f2631e72f7d326216675e99a4b04081f9a081bd328e75ab4eb7ff`.
[Launch](MaterialPlayer/launch.json) and [native exit2](MaterialPlayer/process.json) retained. No UI automation attachment or shutdown-C0000005 conclusion.

## Four real-vehicle cases left unrun

| Actual R3/R6 case | Result | Reason |
|---|---|---|
| CombatArena7 / Light | NOT_RUN | Prior native blocker; limited priority C condition |
| CombatArena7 / Heavy | NOT_RUN | Same |
| ObstacleCourse7 / Light | NOT_RUN | Same |
| ObstacleCourse7 / Heavy | NOT_RUN | Same |

No final vehicle position, driveability PASS or contact measurement is invented for these rows. Prior19 R3.2 physical attempts remain valid only for their recorded R3.2 source and configurations.

## Minimal reproduction and next actions

In a new checkout of this QA branch, use Unity6000.3.14f1. Exact original native capsule78fea95 contains the existing runner; rerun only if receiving a revised producer source or independently reproducing this failure:

```powershell
powershell -NoProfile -File Tools/B01Validation/Run-PackageTests.ps1 -Filter B01.MAPGEN001.Tests -Mode Both -Label MAPGEN-R33-repro
# Expected on recorded source: Edit24/24; Play6PASS/12FAIL, cube12FAIL, exits0/2.
```

Standalone material reproduction uses snapshot4c2074c (saved diagnostic scene and scripts). Build with Unity `-batchmode -quit -projectPath <checkout> -executeMethod MapGenR33MaterialBuild.Build -logFile <Editor.log>`; wait for the actual process/native exit. Run `Builds/MAPGEN-R33-MaterialQA/MapgenMaterialQA.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11 -mapgen-material-output <evidence-directory> -logFile <Player.log>`. Expected actual result on this host: missing shader for both recipes, native2. Do not add a template/material override during this reproduction.

**B01-20 actions:** fix/measure the12 cube physical cases; ensure runtime source materials/shaders survive a real Windows build; then return a new complete frozen source/manifest for targeted retest. Retain successful corrected diagnostic fixtures and independent case reporting. No generator algorithm change is justified by this cube evidence alone.

**Independent B01-60:** evidence can be handed over for independent reproduction/triage, but R3.3 is **not a green acceptance candidate**. Open cube-suite and standalone material failures justify blocking future integration until resolved or explicitly adjudicated by the owners. G2 remains HOLD.

Remaining: revalidate revised source natively, real visible Arena/Course rendering, material lifecycle after successful generation, then the4 targeted actual R6 drives and independent B01-60/B01-00 decisions. One original native cycle and one separate material build/Player were performed; no additional source revision or repair loop was attempted. Full raw logs remain under ignored `Artifacts/Local`; public copies omit machine/connection identifiers and trailing whitespace only.

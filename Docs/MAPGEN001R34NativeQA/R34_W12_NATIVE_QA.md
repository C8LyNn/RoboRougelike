# W12 — MAPGEN R3.4 native QA

2026-10-25. **FAIL / BLOCKED at project compilation.** A real Unity6000.3.14f1 invocation exited1 with CS1061 in the producer's PlayMode test. Later native tests and Player runs were not executed. This is a newly reproduced source/API blocker, not evidence that the cube/material runtime corrections failed. One bounded QA cycle, no source repair or integration.

## Exact inputs and environment

| Item | Exact value |
|---|---|
| Complete R3.4 source, PR99+103+211+225+235 | `0024731e1de0c0814968bd91d860a6ac9bb45385` |
| Target OBJ210 including PHY125 | `4c9236a4cdc27814ea2e7679872ec4a524150cc8` |
| Included PHY125 | `916654634d1bfdc2bbc14e64b9eeac856ede1fa8` |
| Actual compiled receiver snapshot | `3e157af2ed6cd7c20145303fcf7dda0d1e52d0ec` |
| Preserved previous R3.3 QA / PR232 | `b80faa121e4022d772caa3b93d8b2a482d9a53af` |
| QA branch | `qa/rrl-mapgen-r34-native` |
| Runtime | Windows, Unity6000.3.14f1, existing URP/Packages unchanged |

Source PR235 and target PR210 heads were independently queried and matched the above pins. Reused clean, inactive isolated checkout `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-MAPGEN-R32-QA` on a new QA branch. Existing ten other worktree HEADs, all old QA branches/builds and CAMERA R1.1 evidence remain preserved. No active Unity/QA Player existed at start; no AGENTS reservation file found.

Imported the full owned Assets/Docs/Tools tree, not the PR235 diff. Shared `Assets/B01Packages.meta` was byte-compared before import, GUID `b01a0001000000000000000000000001`, and **not overwritten**. [Integrity receipt](source-integrity.json): **55 source/doc/meta +6 optional-tool declared SHA256/byte entries PASS;62 complete imported files match exact Git;0 GUID collisions**. Manifest itself is the sole intentional self-hash exclusion. Resources directory, shader and both required metas are included and hashed. This is source integrity, not proof of native shader compilation or inclusion in a Player; producer ZIP was not independently re-downloaded/hashed.

## Actual results by phase

| Phase/check | PASS / FAIL / NOT_RUN | Actual evidence / reason |
|---|---|---|
| Source/manifest/metas/Resources presence | **PASS** | [Exact hashes/bytes/GUID receipt](source-integrity.json) |
| A — Unity project compilation | **FAIL**, actual native exit1 | [Editor.log](Compile/Editor.log), [process/SHA/times](Compile/process.json) |
| A — EditMode | **NOT_RUN** | Compiler error before NUnit |
| A — PlayMode | **NOT_RUN** | Same |
| A —12 separate cube cases | **12 NOT_RUN,0 executed** | [Every authored case](CUBE_CASES.md) |
| A — new flat actuator control | **NOT_RUN** | `CubeForceActuatorMovesOnFlatControlSurface` cannot execute |
| B — Windows x64 build | **NOT_RUN** | Project compilation blocker; no test-assembly bypass |
| B — Resources.Load / real shader rendering | **NOT_RUN** | Presence in source is not native render evidence |
| B —3× Regenerate/Clear/material lifetime | **NOT_RUN** | No R3.4 Player |
| B — Arena/Course720p/1080p captures | **NOT_RUN** | No compiled R3.4 Player or screenshots |
| C — first4 actual R3/R6 seed7 cases | **NOT_RUN** | A/B prerequisite not met |
| C — extension to12 actual R3/R6 cases | **NOT_RUN** | First4 were not started |
| Independent B01-60 / B01-40 / G2 | **PENDING / HOLD** | No acceptance/integration granted |

[Machine-readable status JSON](native-status.json) and [status XML](native-status.xml) retain explicit NOT_RUN entries. The XML root is `qa-compilation-status`: **receiver status metadata, not NUnit output**. No real NUnit XML was produced; no false test-count PASS/FAIL or old screenshot was substituted. Later test-run directories were not created. No physical position, speed, contact, displacement or goal progress was measured on R3.4.

## W12-MAPGEN-R34-01 — blocking producer C# API error

File: `Assets/B01Packages/MAPGEN001/Tests/PlayMode/MapGenPlayModeTests.cs`, **line37,column56**. Authored test: `GeneratedBlocksUseUrpShaderInsteadOfErrorMaterial`. This is producer source from the immutable tree, not receiver instrumentation.

```text
error CS1061: 'Shader' does not contain a definition for 'GetTag'
and no accessible extension method 'GetTag' accepting a first argument
of type 'Shader' could be found
```

Offending source:

```csharp
Assert.AreEqual("UniversalPipeline",
    renderer.sharedMaterial.shader.GetTag("RenderPipeline", false),
    "Shader lacks URP pipeline tag");
```

Expected: the complete package compiles against Unity6000.3.14f1 before its real shader, cube and R6 regressions run. Actual: C# compilation fails; Unity logs `Scripts have compiler errors` and terminates with return code1. The same compiler diagnostic appears multiple times in the log; it is one distinct source error. An earlier licensing refresh message is not the cause: entitlement resolves, then the C# compiler reports CS1061.

**Minimal producer correction suggested, not applied or claimed green:** use the material API for this tag assertion, retaining the expected value and assertion:

```csharp
renderer.sharedMaterial.GetTag("RenderPipeline", false)
```

[Official Unity6000.3 Material.GetTag API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Material.GetTag.html) and the installed engine's `UnityEngine.CoreModule.xml` both declare `Material.GetTag(string,bool)`. The installed API does not declare `Shader.GetTag`; [local API receipt](api-evidence.json) records the actual check. A documented shader tag API could also be used, but B01-20 owns that implementation choice.

Route this blocking finding **via B01-00 to B01-20**. Preserve the assertion's purpose, issue a new complete frozen source SHA/manifest, then rerun native compilation and the skipped phases. No producer C#/shader/test file was locally repaired, assertion weakened, test excluded, cached assembly substituted or runtime-only project built to hide the failure.

## Comparison with R3.3 and remaining uncertainty

| Observation | R3.3 / PR232 | R3.4 this cycle |
|---|---|---|
| Project compilation | PASS/native0 | FAIL/native1, new test API error |
| EditMode |24/24PASS | NOT_RUN |
|12 physical cubes |12 executed FAIL, each stopped before first goal | NOT_RUN; test-only low-friction change not validated |
| Flat actuator control | Absent | Authored but NOT_RUN |
| Minimal Windows URP | Both recipes failed shader lookup | NOT_RUN; retained Resources shader not yet validated in Player |
| Actual R3/R6 | NOT_RUN | NOT_RUN |

The new retained shader and fixture changes are present with matching hashes. They cannot yet be called fixes proven by native execution. R3.2's19 real-vehicle attempts are historical evidence only; they are not R3.4 PASS results. The source diagnostic `floorRaySamples` measures support-ray observations, not an actual PhysX contact-manifold count; any future physical failure needs appropriately labelled contact evidence. No .NET1015 or static contract test was rerun or treated as native Unity proof.

Previous CAMERA R1.1 findings CAM-R11-01/02 remain preserved in PR234. No camera code/test or BUILD/RAC/LEGENDARY package was touched or tested in W12.

## Minimal reproduction

Use a new checkout of this QA branch at recorded execution snapshot3e157af (full package and original metas included). Keep Unity6000.3.14f1 and its existing dependencies. From that checkout:

```powershell
$qaProcess = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe' -ArgumentList @('-batchmode','-nographics','-quit','-projectPath',('"'+(Get-Location).Path+'"'),'-logFile',('"'+(Join-Path (Get-Location) 'R34-compile.log')+'"')) -WindowStyle Hidden -PassThru
$qaHandle = $qaProcess.Handle
$qaProcess.WaitForExit()
$qaProcess.Refresh()
$qaProcess.ExitCode
# Expected on immutable0024731: native1, CS1061 at MapGenPlayModeTests.cs(37,56).
```

Retained raw run: `Artifacts/Local/MAPGEN-R34-compile-20261025T160823Z`, UTC16:08:23.885→16:08:33.896. Public log only omits machine/connection identifiers and trailing whitespace; full original remains local. Do not report a corrected source PASS until a new actual invocation succeeds.

## Handoff and next package recommendation

**Current readiness: BLOCKED before independent acceptance.** B01-60 can independently reproduce this negative packet; B01-40 receives source integrity and unchanged-parent-GUID evidence. Neither G1 acceptance nor G2/integration follows from these receipts. Required next source action is the API correction above. Required tests after that: all native suites,12 cubes plus flat control, minimal Resources-only Windows render/lifecycle in both resolutions, then first4 real R6 and optional extension12. Future material QA must use the new retained shader contract, not the old R3.3 helper's built-in-URP-name expectation.

**Between the requested next packages, recommend BUILD R1.2 first**: [PR215](https://github.com/C8LyNn/RoboRougelike/pull/215), current exact head `489676414a3eb45fe70124098e3778a81d0aaff2`. Its mount/contact rules underpin vehicle building and its TestRunner reference repair is the same failure class that passed CAMERA R1.1 native compilation. This priority is an inference, not native readiness proof. **RAC R3.1 second**: [PR216](https://github.com/C8LyNn/RoboRougelike/pull/216), current head `fd9a3f4dda08eaba56d9f74007bc35dc0809b67e`. Only read-only PR metadata was checked for this recommendation; neither package was imported or tested. A newly frozen MAPGEN correction can be prioritized by HQ to finish the blocked gate. No next task starts automatically.

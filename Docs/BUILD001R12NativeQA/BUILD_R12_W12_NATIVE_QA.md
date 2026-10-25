# W12 — BUILD-001 R1.2 native QA

2026-10-25. **Overall FAIL / integration HOLD.** The assembly-reference repair is confirmed in Unity: the complete package compiles and all13 original producer tests pass. Receiver requirements expose all four BUILD-W9 concerns; the original demo also renders magenta in Windows URP. No producer fix or game integration was performed.

## Exact source, target and isolation

| Input / execution | SHA or version |
|---|---|
| Full BUILD R1 + R1.2, PR149 + PR215 | `489676414a3eb45fe70124098e3778a81d0aaff2` |
| Current OBJ210 target including PHY125 | `4c9236a4cdc27814ea2e7679872ec4a524150cc8` |
| Included PHY125 | `916654634d1bfdc2bbc14e64b9eeac856ede1fa8` |
| Original-source compilation snapshot | `3bd52637ff3e9b47cdf3e480073a932d44736864` |
| Combined original + receiver NUnit execution | `079e462d98a38d1dfb2e23dda487dc4d58470de4` |
| Demo observation build code | `2786f61b3ea3abcf43d3b3dbbdd73e586f675284` |
| Saved demo scene / exact Player launch | `b2fff3fcf49d2a89037a9689481550df2f4df87b` |
| Unity / rendering |6000.3.14f1 / unchanged target URP |

New worktree: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-BUILD-R12-QA`, branch `qa/rrl-build-r12-native`, directly from approved current game4c9236a. Native app worktree tool returned Not a git repository from this projectless chat; Git fallback created the requested separate checkout. No Library copy. Existing11 worktree HEADs, old BUILD source capsule/PR212, MAPGEN/CAMERA reports and release files were preserved. No active Editor/QA Player at start; no AGENTS reservation file found. Source PR215 and target PR210 heads were checked before testing.

Complete Assets/B01Construction/BUILD001, parent/owned metas and Docs imported, not only PR215 diff. [Integrity receipt](source-integrity.json): **37 complete files byte-identical**,36 declared Git blobSHA1/size entries match, independent SHA256 receipts computed;0 GUID collisions/invalid metas. Producer manifest does not supply verified per-file SHA256, so no claim of comparing against an absent producer SHA256 table. New parent `Assets/B01Construction.meta` had no existing target file to overwrite. Runtime dependencies remain existing Unity/URP/Test Framework. No Package/ProjectSettings, R3/R6/P12 or other package source changes.

Receiver tests live only under `Assets/BuildNativeQA/Tests`, in separate asmdefs and namespace `B01.BUILD001.Tests.ReceiverQA`. They deliberately share the source test filter prefix, but are explicitly counted separately below. Producer assertions/asmdefs were not changed. Original README trailing Markdown whitespace was preserved for exact hashes; whitespace checks covered new receiver files.

## Actual native results

| Phase | Result | Evidence |
|---|---|---|
| A — source project compilation | **PASS**, native0 | [Editor.log](Compile/Editor.log), [process/SHA](Compile/process.json) |
| A/B/C — combined EditMode | **32 PASS /1 FAIL**,33 executed, native2 | [NUnit XML](NativeUnity/EditMode.xml), [log](NativeUnity/EditMode.log), [process](NativeUnity/EditMode-process.json) |
| A/B/C — combined PlayMode | **2 PASS /3 FAIL**,5 executed, native2 | [NUnit XML](NativeUnity/PlayMode.xml), [log](NativeUnity/PlayMode.log), [process](NativeUnity/PlayMode-process.json) |
| Original producer tests only | **13/13 PASS**:11Edit +2Play | [Every actual case and origin](NUNIT_CASES.md) |
| Additional positive receiver cases | **21/21 PASS** |18 typed-rotation cases +armor +four-cell footprint +rebuild |
| BUILD-W9-01..04 | **4 FAIL**; each observed twice before asserting | [Raw per-attempt diagnostics/failures](W9_REPRODUCTIONS.md) |
| A — Windows x64 demo build | **PASS**, native0,0 errors/4 warnings | [Editor.log](DemoBuild/Editor.log), [process](DemoBuild/process.json) |
| A — actual original demo controller | Logic10/10 observationsPASS; **renderingFAIL**, native2 | [JSON](DemoPlayer/demo-results.json), [Player log](DemoPlayer/Player.log), [process](DemoPlayer/process.json) |
| D — real R3/R6 mounting/mass/COM/drive/detachment | **NOT_RUN** | Four required placement invariants failed; no vehicle adapter installed |

**NUnit total:38 executed,34 PASS,4 FAIL,0 skipped.** Four failed cases contain eight sub-observations, not eight extra NUnit tests. Ten demo state observations are also not extra NUnit cases. [Native summary, case results and PNG hashes/dimensions](native-summary.json). The demo observation XML has its own non-NUnit root; actual NUnit XML is above.

One original-source compilation, one combined Edit/Play run and one demo build/Player were performed. No full-game regression, producer repair/retest loop, static-pass substitution or additional agents. Native2 reflects failed assertions/visual checks, not a C# compilation failure or a shutdown crash.

## Positive mounting scope

These are real native Unity policy/geometry APIs and actual Physics queries where described; they are **not yet canonical vehicle attachment or player input tests**.

| Requested positive scenario | Result and limit |
|---|---|
| Correct wheel socket | PASS: axle/hub admission, mirrored side and suspension path; ordinary tire/top/barrel rejection controls pass |
| Gun base | PASS: base admission, wrong side rejected |
| Armor to structure | PASS: source structural socket and receiver armor case |
|90/180/270degrees | PASS: explicit independent expected anchors, both Wheel and Gun acrossX/Y/Z;18 NUnit cases |
| Different sizes | PASS for one-cell fixtures and one owner spanning4cells (2×2×1); source occupancy/rotation checks pass |
| Remove/rebuild | PASS: removing logical bridge disconnects tail; valid bridge replacement restores graph reachability |
| Supporting part loss | PASS for source alive/dead logical graph and orphan-admission rejection; canonical P12 fragment physicsNOT_RUN |
| Actual Light/Heavy robot installation | **NOT_RUN**, covered by gated phaseD rather than relabelled logical fixtures |

The source API is explicitly a standalone placement policy and does not mount R3 parts, mutate inventory or update a Rigidbody. No conclusion about vehicle mass/COM or functional drive is drawn from arithmetic graph tests.

## Four earlier concerns — native reproduction and ownership

All fixtures invoke unchanged BUILD APIs. R3/R6/P12 are not used to produce these failures. They are BUILD contract/coverage issues, not newly demonstrated regressions in the active game. R1.2 repaired assembly references, not these runtime concerns. Full exact test setup and both observations per issue are retained in receiver source and NUnit output.

### BUILD-W9-01 — HIGH: decorative child becomes an authorized contact

Configuration: authoritative Core root at world(0,1,0), structural cube0.52m, decorative child box at(0.29,1,0) with size(0.04,0.20,0.20). Grid0.55m, Core pose0, default hit-gap tolerance0.075m. Native ray from(1,1,0) toward-X hits the decorative child's +Xface at(0.31,1,0). A separate ray atY1.18 hits the genuine structural collider and passes as a control.

Expected: a merely decorative collider does not authorize mounting, even if it is a descendant near a socket plane. Actual in **2/2 attempts**: `ValidateHit=Valid`, face(+1,0,0). The hit is genuinely the decorative collider, not the structural box.

Cause: ValidateHit accepts hierarchy membership, then patch proximity/normal; no authoritative mount-collider identity is supplied or checked. Minimal direction for B01-20: define/verify authorized structural colliders per owner and reject unregistered visual children. Tightening gap tolerance alone would also affect legitimate0.52m mesh/0.55m grid offsets. No active garage exploit claim; receiver-owner resolution must be part of the final contract.

### BUILD-W9-02 — HIGH: a loaded second Core seeds an unrelated island

Configuration: installed authorizedCore at cell(0,0,0), untrustedLoadedCore at(4,0,0), candidateArmor at(5,0,0). Directly placing that second Core through Check is correctly rejected withInvalidCore (control). The regression supplies it in an existing/loaded installed list.

Expected: the one authoritative Core governs reachability; malformed multi-Core input cannot create another foundation. Actual **2/2**: secondRootReachable=true, candidate resultValid, support=untrustedLoadedCore.

Cause: Reachable seeds every alive Core and Check does not reject multiple installed roots. Minimal direction: require an authoritative Core ID or validate exactly one allowed Core before graph/admission, including loaded state. Existing game's save validation was not exercised or modified.

### BUILD-W9-03 — HIGH / B01-10 continuity decision: empty material gap relays support

Configuration: Core atX=-1; a single Device owner declares only cellsX=0 andX=2, with structural sockets at the outer left/right faces; candidate tail atX=3. Physical0.52m boxes exist at worldX0 and1.1m, with **no geometry in between**. Native OverlapBox at(0.55,1,0), half-extents0.2m, confirms the empty gap; query is filtered to the owned fixture.

Expected: no unsupported relay across physically disconnected islands under one part ID. Actual **2/2**: definitionValid=true, bridgeValid, downstreamValid withowner=islands, despite empty physical gap.

Cause: definition validation checks socket placement/area but not occupied-cell connectivity; graph membership promotes the whole owner. Minimal direction: validate one connected occupied component or model component-specific structural reachability. B01-10 must settle supported hollow/irregular-shape semantics; no shape requirement was relaxed to mark this PASS. The fixture has two disjoint boxes, not a continuous arch with an empty centre.

### BUILD-W9-04 — HIGH integration blocker / explicit boundary decision needed

Configuration: Core collider0.52m, candidate declared as one Armor cell atX=1 (worldX0.55m), actual candidate collider(1.2,0.52,0.52)m. Native `Physics.ComputePenetration` confirms overlap **depth0.3100m** in both attempts. This is an actual Unity geometry query, not a hand-calculated collision result.

Expected: complete placement validation rejects a colliding physical envelope. Actual **2/2**: Check returnsValid/supportcore while overlap=true.

Cause/boundary: the package checks logical cells and has no actual collider-envelope input. Its known limitations already defer oversized-envelope tests to PARTSIZE/integration; this is a **FAIL against the required complete behavior**, not a claim that a pure cell API mysteriously read scene geometry. B01-10/B01-40 must designate authoritative volume validation before production admission; B01-20 should provide the agreed preflight/contract. Do not shrink real colliders or redefine the required rejection merely to obtain a green result. Current R3 collision behavior was not changed/tested.

## Additional demo issue — BUILD-R12-DEMO-01, MEDIUM

The exact source demo scene/controller is used in a saved QA copy with an observer only. The observer invokes the existing private Cycle method, equivalent to its Next-candidate action, and reads the controller's verdict. It does not replace materials, create another build system or alter geometry. Source scene/controller bytes remain unchanged.

For each resolution1280×720 and1920×1080, all five states were actually sampled: valid axle, invalid top wheel, valid gun base, invalid gun side and valid armor. **10/10 logic observations match the predeclared expectations.** However **all10 ghost materials use Hidden/InternalErrorShader**, and real screenshots show magenta geometry rather than the claimed green/red preview. Build succeeds; Player exits2 because the observer detects this visual failure, with0 logged errors/exceptions.

- [720p invalid wheel preview](DemoPlayer/Demo-1280-choice1.png)
- [1080p valid gun-base preview](DemoPlayer/Demo-1920-choice2.png)
- [1080p valid armor preview](DemoPlayer/Demo-1920-choice4.png)

Minimal direction for producer: reference/retain URP-compatible materials inside its own demo and verify color coding in a built Player. No MAPGEN shader or global rendering setting was reused/changed to mask the defect. These screenshots document the demo, **not the exact W9 negative fixtures**; those have native Physics/API logs and reproducible source but no dedicated rendered frame.

## Evidence, reproducibility and deferred scope

The original-source compile snapshot3bd5263 includes full package/metas and the runner. The combined tests snapshot079e462 includes separate receiver test assemblies. In a new checkout on Unity6000.3.14f1:

```powershell
$qaPython='C:\Users\kubap\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
& $qaPython Tools/B01Validation/verify_build_r12.py
powershell -NoProfile -File Tools/B01Validation/Run-PackageTests.ps1 -Filter B01.BUILD001.Tests -Mode Both -Label BUILD-R12-repro -Python $qaPython
# Expected measured source: Edit32PASS/1FAIL, Play2PASS/3FAIL, native2/2.
```

Demo build uses saved snapshotb2fff3f: Unity `-batchmode -quit -projectPath <checkout> -executeMethod BuildR12DemoBuild.Build -logFile <Editor.log>`; wait for actual process exit. Then `Builds/BUILD-R12-QA/BuildNativeQA.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11 -build-qa-output <directory> -logFile <Player.log>`. It samples both resolutions and exits2 for error-shader observations. [Launch SHA/assembly digest](DemoPlayer/launch.json), [diagnostic XML, not NUnit](DemoPlayer/demo-observations.xml).

**PhaseD and real Light/Heavy mounting areNOT_RUN.** No temporary vehicle adapter was installed after the four required invariant failures. Actual vehicle recognition, collider validity, Rigidbody mass/COM updates, wheel/support detachment effects, ordinary keyboard/mouse play, persistence and existing gameplay regression remain unverified for BUILD. No cube or arithmetic graph was substituted as real R3/R6 proof. Source demo itself only previews admission and does not construct a playable vehicle.

Full raw local evidence remains under ignored Artifacts/Local. Public log copies omit machine/connection identifiers and trailing whitespace only; original source whitespace remains intact. Ten PNG dimensions and SHA256 are in native-summary.json. One full controlled cycle ended; no further task or producer repair loop was started.

## Readiness and integration risk

**Not ready for a positive independent B01-60 acceptance gate; G2 HOLD.** B01-60 can independently reproduce this negative packet. B01-00 receives it for B01-20, with B01-10/B01-40 decisions required for continuity/envelope ownership. Integration risk is HIGH: admitting untrusted collider surfaces, extra roots, empty-space support or physically colliding envelopes would undermine the vehicle graph/garage contract. Preserve the currently working R3/R6/P12 and await a revised immutable package, then repeat targeted regressions and the deferred real-vehicle phase. No main/develop merge or active-game installation occurred.

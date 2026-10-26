import hashlib,json,pathlib,subprocess
r=pathlib.Path(__file__).resolve().parents[2];out=r/'Docs/CAMERA001R12NativeQA'
d=json.loads((out/'native-summary.json').read_text());w=d['windows']['WindowsFinal'];a=w['aimSummary']
table=[]
for x,aim in zip(w['rows'],a):
 table.append('| '+ ' | '.join([x['id'],str(x['parts'])+'→'+str(x['partsAfterDamage']),f"{x['distanceMoved']:.3f}",str(x['inputLeftShells']),str(x['finalRenderedSamples']),str(x['finalRenderedOwnHits']),f"{x['maxMuzzleConsumedAfterSettle']:.4f}",str(aim['firingFrameViolations']),x['status']])+' |')
allcounters=['ownReticleHits','finalOriginOwnHits','originInsideOwnSamples','speedCapViolations','worldSphereOverlapSamples','aimErrorViolations','finalRenderedOwnHits','finalRenderedWorldOverlaps']
assert d['cameraStatus']=='PASS_OBSERVED' and all(x[k]==0 for x in w['rows'] for k in allcounters)
peak=max(a,key=lambda x:x['peak']['consumedError'])['peak'];corridor=w['phases']['corridor']
maxexcess=max(x['maxStepExcess'] for x in w['rows']);maxaim=max(x['maxActualAimError'] for x in w['rows'])
scene=r/'Assets/CameraR12NativeQA/CAMERA_R12_QA.unity';assembly=r/'Builds/CAMERA-R12-QA/CameraR12QA_Data/Managed/Assembly-CSharp.dll'
prov={'producer':d['producer'],'base':d['base'],'nativeTestSnapshot':d['compile']['source'],'finalBuildCodeSnapshot':d['builds']['BuildFinal']['source'],'finalLaunchSnapshot':w['launch']['source'],'generatedSceneSHA256':hashlib.sha256(scene.read_bytes()).hexdigest(),'gameAssemblySHA256':hashlib.sha256(assembly.read_bytes()).hexdigest(),'note':'Build factory saves a new receiver-only scene; scene frozen in finalLaunchSnapshot. Producer source remains exact. Report-only commits do not alter built code.'}
(out/'build-provenance.json').write_text(json.dumps(prov,indent=2)+'\n')
report=f'''# CAMERA-001 R1.2 — native Unity / Windows QA

2026-10-26. **Original camera regressions PASS_OBSERVED. Overall functional run FAIL_OBSERVED; integration HOLD pending aim-tracking triage.** Both requested R1.1 defects are absent in the sampled R1.2 tests. This is receiver QA, not B01-60 independent G1, B01-40 target acceptance, or G2.

## Exact inputs and isolation

| Input / snapshot | SHA |
|---|---|
| Complete CAMERA #134 + #214 + #247 | `{d['producer']}` |
| Original CAMERA #134 fork ancestor | `894d3488dedb69e2b8d89683d750bbdbfedf4e55` |
| R1.1 parent #214 / prior failed QA #234 | `d48108f56d3fe0790ad715683e77b2de9219e4b1` |
| Game OBJ #210, includes PHY #125 | `{d['base']}` |
| Native compile / original 5 + 13 tests | `{d['compile']['source']}` |
| Initial Windows build / launch | `27dca30d3a5566cefd7e44911f14b55e8e13a907` / `2236e08fcadaf55cecb6e094b94a1691effc125a` |
| Synchronized aim diagnostic build / launch | `c4cd93565e002aef6e0135ba8b1d18810874afea` |
| Final post-LateUpdate build code | `{d['builds']['BuildFinal']['source']}` |
| Final generated scene / Player launch | `{w['launch']['source']}` |

New worktree `C:/Users/kubap/Desktop/Prototypy/RoboRougelike-CAMERA-R12-QA`, branch `qa/rrl-camera-r12-native`. Unity **6000.3.14f1**, URP17.3.0, Test Framework1.6.0, InputSystem1.19.0; unchanged packages/settings. No reserved AGENTS file found in the target/parents. Existing worktrees inspected read-only; no other dirty worktree or active Unity editor found. Existing RC1 retail PID46264 was left alone. [Preservation snapshot](preservation.json).

Both PR248 RC1 ZIPs retain their original hashes:

- BASE: `457ec2020375e18c4599d832dc028eed7900f8cf5d603765c9e417fd80a8ca90`.
- EXPERIMENTAL Final: `722be56b98db0abe1517ec1d018b6f4d6ecbf0a46ea172d198f8294a7edc8cca`.

Imported the full owned tree at the pin, not just #247's diff. #214 and the #134 fork ancestor are proven ancestors. The current #134 branch has later commits and is not substituted for the frozen source. [Integrity receipt](source-integrity.json): physical inner ZIP SHA256 `d00e3a3f4aea9ce15a8ecc4d95838fcc5a42329060568da63d0e2a72817cbd9d`, **46,544bytes,41entries**, all40 manifest SHA256/length entries and41 exact Git/ZIP blobs match. No GUID collisions, invalid metas or missing script/assembly/scene metas. Target OBJ has no `Assets/B01Packages.meta`, so the original CAMERA parent is imported here; **future MAPGEN-containing targets must retain their existing parent GUID**. Original demo scene/meta and all producer sources/tests remain byte-identical. No R1/R3/R6/Pack/active scene or producer edit. Runtime asmdef remains standalone; one CAMERA rig owns the QA camera, P12 camera absent.

## Actual native results

| Stage | Result | Evidence |
|---|---|---|
| Unity compilation | **PASS**, native0 | [Editor.log.gz](Native/Compile.log.gz), [exit/SHA](Native/Compile-process.json) |
| Original EditMode | **PASS5/5**,0skip,native0 | [XML](Native/EditMode.xml), [log.gz](Native/EditMode.log.gz), [exit](Native/EditMode-process.json) |
| Original + R1.2 PlayMode | **PASS13/13**,0skip,native0 | [XML](Native/PlayMode.xml), [log.gz](Native/PlayMode.log.gz), [exit](Native/PlayMode-process.json) |
| First Windows x64 build | **PASS**, native0;0errors/4warnings | [log.gz](BuildInitial/Build.log.gz), [exit](BuildInitial/Build-process.json) |
| Diagnostic / final builds | **PASS**, native0 | [diagnostic log.gz](BuildDiagnostic/Build.log.gz), [final log.gz](BuildFinal/Build.log.gz), [final exit](BuildFinal/Build-process.json) |
| Initial real Windows run | **FAIL**, native2;8rows, Light2fail | [JSON](WindowsInitial/functional-results.json), [CSV](WindowsInitial/camera-trace.csv), [Player.log.gz](WindowsInitial/Player.log.gz), [exit](WindowsInitial/process.json) |
| Synchronized aim follow-up | **FAIL**, native2;8rows | [JSON](WindowsDiagnostic/functional-results.json), [CSV](WindowsDiagnostic/camera-trace.csv), [exit](WindowsDiagnostic/process.json) |
| Final rendered-pose follow-up | **{w['overall']}**, native{w['process']['exitCode']};8rows | [JSON](WindowsFinal/functional-results.json), [XML diagnostic](WindowsFinal/functional-diagnostic.xml), [Player.log.gz](WindowsFinal/Player.log.gz), [exit](WindowsFinal/process.json) |
| CAMERA pose checks across8 real R3/R6 cases | **PASS_OBSERVED**, {w['samples']} ordinary camera samples | [CSV](WindowsFinal/camera-trace.csv), [full summary](native-summary.json) |
| Actual EndOfFrame rays after weapon LateUpdate | **PASS_OBSERVED**, {w['endOfFrame']['samples']} samples;0own/world hits | [final-rendered-pose.csv](WindowsFinal/final-rendered-pose.csv) |

Logs are preserved compressed, with connection/machine identifiers redacted; raw originals remain in this checkout's `Artifacts/Local/CameraR12-*`. Nonfatal license access-token refresh messages occur in Editor logs; entitlement resolves and actual compiler/test/build exits are0. No Player errors/assertions/exceptions recorded. The Windows XML is a conversion of standalone observations, **not extra NUnit tests**. No source/.NET/JS suites were repeated. Original Unity suites ran once; only receiver instrumentation was corrected after observed failures, as documented in [fixture diagnostics](RECEIVER_FIXTURE_DIAGNOSTICS.md). None of the producer assertions was changed or disabled.

## Eight final Windows cases

| Rig / width | Mounted parts before→after damage | Real R6 distance m | Identified LMB shells | Post-Late samples | Own hits | Peak settled tracking error deg | Firing-frame violations | Combined result |
|---|---|---|---|---|---|---|---|---|
{chr(10).join(table)}

All cases actually render at1280×720 or1920×1080. Each has pitch18 transition, full360 yaw, requested zoom3→18→8, wall,70sample corridor, original HP destruction/removal and five seconds of native R6 movement. Light20parts; Heavy139; Tall34 with9.160m armour tower; Wide149 with10.420m hull width. These are real mounted R3 parts/colliders/HP/Rigidbody/R6, not proxy cubes. Tall/Wide are extreme QA models, not certified garage designs. Static camera phases use a kinematic chassis to isolate the camera; input-drive phases restore dynamic Rigidbody and unchanged controller/R6 forces. Distance is measured from native Rigidbody positions; no teleport route or substitute vehicle drive. The damage flag accepts destroyed OR detached state; loss from operational mounted count and camera refit are observed. Fragment flight/trajectory is not separately certified.

**CAM-R11-01: PASS_OBSERVED** in this scope. Final world-space camera-origin rays, near-clip centre rays and inside-own-collider checks all have0 intersections, including pitch transition and parts removal. Camera-to-solver-aim error≤{maxaim:.8f}° (required≤0.05°). EndOfFrame repeats actual origin/near-plane/inside/world checks after weapon LateUpdate;0 violations. This is sampled physical evidence, not a proof for every possible construction/frame.

**CAM-R11-02: PASS_OBSERVED** in this scope. Corridor{corridor['samples']}samples, no9.1732m snap,0world overlap,0speed violations. Peak recorded corridor displacement{corridor['maxStep']:.4f}m at its actual dt. Every ordinary step is checked against15×dt, not a fixed0.25m assumption at variable frame times. Largest positive floating-point excess across the final run is{maxexcess:.9f}m, below the0.0001m numeric tolerance; the limit was not increased. Configure/ResetSmoothing/StepCamera(0) deliberate resets are excluded from ordinary movement measurements and baseline-reset explicitly. Conservative near-plane enclosing sphere includes default0.14m padding. No collider is disabled to hide an overlap.

## Aim, firing and LMB/RMB

InputSystem **queued virtual Mouse/Keyboard events**, consumed by unchanged P12 Update/FixedUpdate/LateUpdate; camera orbit remains the package's public API. Per case,300LMB frames,72RMB frames and300W frames were observed; idle/RMB produce0shots, LMB produces14active owned `P17PhysicalShell` objects with a matching `R4ProjectileOrigin.PartId`. No arbitrary fast debris is counted. Every case moves>1m through actual R6 forces.

This base has **LMB fire and no RMB weapon-group binding**. The result checks preservation of that base behavior, not completion of Issue41's separate two-group weapons backlog. No claim of OS mouse/human play, integrated mouse-to-CAMERA input adapter or RMB group-B acceptance. Camera source remains opt-in and does not change bindings.

All112 identified shots in the diagnostic run had firing-frame muzzle error≤0.05°. Final-run shot data and `aim-diagnostics.json` provide exact paired measurements; actual flight direction is **normalized recorded velocity**, not the primitive sphere's transform-forward field. Final peak velocity-to-aim error is{w['flightAimMaximum']:.8f}°, {w['flightAimViolations']} violations. A firing-frame check does not certify full ballistic impact accuracy or damage delivery at every range.

## Open observation — CAM-R12-INT-01 / medium

**FAIL** for continuous muzzle-to-reticle tracking after target-hit changes during native driving. This is not hidden by the passing camera checks or by checking only firing frames. Full provenance and per-gun peaks: [aim-diagnostics.json](aim-diagnostics.json). Raw per-frame aim/first-hit labels and per-shot velocity/owner/part: [aim JSONL.gz](WindowsFinal/aim-trace.jsonl.gz), [shots JSONL.gz](WindowsFinal/shots.jsonl.gz).

Reproduce on the exact producer SHA + receiver final code snapshot above: real Light at(0,1,0), real kinematic enemy Light at(0,1,30), FOV62, default CAMERA parameters, derived target-facing pitch, flat floor. After orbit/zoom/corridor and one armour module destroyed through original HP, enable the canonical controller and dynamic Rigidbody. Queue W+LMB for5seconds; don't invoke FireGuns directly. The initial/synchronized runs reproduced Light peaks15.439°/15.308° at720p and12.686°/12.906° at1080p. Synchronized Light720peak occurs at scene time15.96298s,3.60026s into LMB drive: first-hit changes from enemy barrel to floor, aim jumps from approximately(0.00391,1.914,30.8045) to(0.00269,0,34.8804), muzzle near(0.00401,1.5552,27.8219), while camera's own-ray/continuity/aim checks stay green. Pre-Late target and post-fire target agree at that sample, so a changed post-fire diagnostic target alone does not explain this transient. Tall1080 also exhibited a short acquisition transient in the synchronized follow-up.

Expected strict receiver contract: continuous target/muzzle/reticle error≤0.05°. Actual: short excursions above it; the unchanged R3 gun pivot rotates at170°/s. **Camera causation/regression is unproven**: no identical new input fixture was run against R1.1/BASE. Some aim queries can select active physical projectiles as well as enemy/floor; hit labels are supporting name/component descriptions (200m diagnostic query), while canonical consumed aim vectors come from the controller's160m query. No production camera/weapon code is modified. B01-20 via B01-00 should triage the acquisition/retarget contract, determine whether bounded turret slew is accepted, and coordinate any required source/integration fix with its owner. Do not reopen the two original fixed bugs without a new reproduction.

## Rendering, evidence and build

144true native screenshots across three retained runs (48each), dimensions/nonuniform pixels checked; selected actual images visually inspected. Representative final evidence:

![Light720 after real movement/fire](WindowsFinal/Light-1280-driveFire.png)
![Tall1080 target visibility](WindowsFinal/Tall-1920-framing.png)
![Heavy720 tight corridor](WindowsFinal/Heavy-1280-corridor.png)

Exact launch/hash record: [launch.json](WindowsFinal/launch.json); generated-scene/assembly provenance: [build-provenance.json](build-provenance.json). The build factory saves a receiver-only scene (local fileIDs vary), frozen separately at the listed launch SHA. Report-only commits do not alter built code. Local QA Player:

`C:/Users/kubap/Desktop/Prototypy/RoboRougelike-CAMERA-R12-QA/Builds/CAMERA-R12-QA/CameraR12QA.exe`

This finite QA Player auto-runs the fixture and exits2 when the strict tracking check fails. It is **not an RC1 release or gameplay integration**. Existing RC1 builds and their running window remain unchanged.

## Independent handoff, remaining work, safe future integration

- **B01-60: evidence ready for independent reproduction with the open FAIL attached**, not an unqualified green acceptance. Both original camera bugs are corrected in these tests; combined aim tracking remains to classify. No independent G1 is issued here.
- **B01-40: source integrity/vehicle evidence available**, target integration gate still pending. Preserve an existing `Assets/B01Packages.meta` (especially MAPGEN's GUID), import only complete owned CAMERA subtree, never overwrite scenes/prefabs. Current RC1 vehicle/weapon/physics source matches this OBJ base; actual RC1 camera integration was not attempted.
- Future authorized integration must choose one transform owner, bridge existing mouse/orbit/zoom without consuming RMB fire, retain canonical reticle/HP/weapons, and test garage/range/OBJ/MAPGEN transitions. Rollback only that future adapter/package import, restoring old P12 ownership/input; retain shared parent meta. This QA branch can be discarded/reverted independently; do not merge it into active game/main/develop.
- **NOT_RUN:** paired R1.1/BASE acquisition comparison, hardware LMB/RMB human play, full ballistic hit/damage accuracy matrix, production camera input adapter, garage/menu/full expedition with CAMERA, all legal1–180part designs, independent B01-60/B01-40 gates. Existing RC1 results are not relabelled as new CAMERA tests.
- **Decision:** hand off now with precise evidence and CAM-R12-INT-01; keep integration HOLD until the strict tracking observation is accepted or resolved by the appropriate owner. No G2, merge, release or next task.
'''
(out/'CAMERA_R12_NATIVE_QA.md').write_text(report,encoding='utf-8')
print('Report written with actual final measurements; overall',w['overall'])

# Full Codex integration — Issue #270

**IN PROGRESS — this document does not yet approve a release.**

The immutable game base is RC2 `056911a8284f7d61f2a8d13ed2669e453c3d9915` (PR #258). The isolated branch is `integration/codex-full-merge-270`; neither `main` nor `develop` is modified. Unity remains 6000.3.14f1 / the existing URP packages.

All previously implemented Codex mechanics are inventoried in [the package matrix](CODEX_PACKAGE_MERGE_MATRIX.md). The complete 224-PR inventory and frozen source manifests are in [Intake](Intake). The owner explicitly excludes the prototype ModelFactory visuals and W01/H01 visual pilot. MOV-010 never supplied a runtime package. Separate B01 producer-only proposals are distinguished from previously implemented Codex gameplay; no source smoke test is presented as an active feature.

## Integration

- Existing R1/R3/R4/R6 own progression, inventory, health, damage, mounting, economy and Rigidbody forces. WPN-001–006, DEF-001/002, MOV-001–009/011, COM/SEN, cover/hazards/VFX and OBJ remain on those owners.
- BUILD R1.4 (#260) now validates actual mounted geometry before canonical spending/mutation, including root rotation, typed structural socket authority, complete physical overlap checks, destroyed-part restoration and authentic factory-wheel provenance.
- MAP-R6 (#3) supplies its complete original road, curve, junction, state, challenge and evidence source. Four activities are reachable from the same garage: time attack, evacuation, survival and boss arena. RAC-001 (#13/#216) observes the real R6 runner and shares the original reward/repair/restart flow.
- MAPGENWORLD v1.1 (#261) replaces the earlier compatible world host source, including signed replay/drift checks. Existing MAPGEN R3.4.1 remains the generator; MAP-R6 is a separate activity source.
- CAMERA (#269) is the one delegated camera pose owner behind the existing input facade. Own projectiles/debris cannot steal targeting/camera collision authority. Actual final-pose rays, consumed target/muzzle provenance, world overlap, full-hull framing and movement speed are recorded separately.

## Actual verification so far

| Snapshot / stage | Actual result | Evidence |
| --- | --- | --- |
| Prefinal `3e650c1`, full Unity | Compile/build exit 0; EditMode 1183/1190, exit 2; PlayMode 474/474, exit 0 | `Artifacts/Local/FM270-FullRelease-20261026T180512Z` |
| Targeted repair/provenance `c5a4d4c` | Compile exit 0; 2/2 EditMode, exit 0 | Native XML/process receipts, retained by evidence collector |
| Final full snapshot `b97b434c8682bc26dc7a681ddd1d247079c59910` | Compile/build exit 0; full EditMode/PlayMode still running | `Artifacts/Local/FM270-FinalFullRelease-20261026T214843Z` |
| Initial real Race / Light / 720p | Exit 0; 54.42 m, ordered checkpoints → reward → garage → restart | Windows receipt `20261026T180719Z` |
| Initial real road / Heavy / 1080p | FAIL exit 1: 81.34 m, pilot drove off first curve | Windows receipt `20261026T180747Z` |
| Reduced-speed real road / Heavy / 1080p | FAIL exit 1: 97.65 m; stopped before upper checkpoint; genuine 180 s timeout | Windows receipt `20261026T215202Z` |
| Current camera, four actual constructions × 720p/1080p | Own-hull reticle intersections 0, camera/world overlaps 0, physical LMB shots and actual R6 movement observed. Heavy/Wide have cropped-hull frames; framing admission remains unresolved | Eight Windows receipts beginning `20261026T220043Z`; raw counters retained |

## Defects found and corrected

The evidence retains the original failing attempts. Corrections include duplicated legacy test-assembly references; RAC Rigidbody versus render-clock observation; physical BUILD API bypasses, invalid socket ownership and sockets lost after rebuild; race victory not reaching canonical settlement; stale post-road physical envelope bounds; road AI using the old origin-centred arena limits; short wheel capsules expanding into spherical overlaps; stale destroyed-collider cache reads; and factory design IDs differing from owned runtime IDs during restoration. Older placement fixtures were moved to physically valid free locations without reducing parts, prices or assertions.

Pending final-cycle findings: road QA pilot stalls at a sharp curve, a pre-assembly public activity call lacks a null-player guard, and Heavy/Wide automatic camera fit reaches the source distance cap. Changes to Unity-owned source are deferred until the current native process finishes.

## Interpretation and limits

Windows activity selection/rewards and combat settlement are API controlled. Traversal uses actual R3/R6 wheels, colliders, forces and checkpoints, with no teleports or velocity writes. Camera trials queue real Input System movement/LMB events; orbit values are injected into the existing facade and raw muzzle acquisition peaks are retained. These are automated acceptance trials, not a claim of human or unassisted completion.

The approved computer-use helper failed before attaching to a window (`failed to write kernel assets`, OS error 3). No alternative UIA/OS-input workaround was used. Historical C0000005 is not declared fixed; each current Player process has its actual native exit recorded. Earlier manual RC2 garage/Alt+F4 confirmation is separate from this candidate.

Final source SHA, admitted test totals, performance, ZIP/extraction/launch receipts, remaining limitations and the readiness decision will replace this in-progress section after actual execution. No G1/G2 acceptance or shared-branch merge is granted by this report.

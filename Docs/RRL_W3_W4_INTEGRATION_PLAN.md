# W3 + W4 Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Execute inline; one fresh whole-branch reviewer at the end.

**Goal:** A playable physical-bonus/rotatable-building integration on latest verified W2, published as an isolated Draft PR.
**Architecture:** Preserve W2 transaction/model/arena/debris fixes. Import PR18 once (including PR17), adapt its bonus/port/orientation data through R4RewardSettlement and the existing P12 graph. Mounted effects use baseline-derived runtime stats.
**Tech Stack:** Unity6000.3.14f1, C#, NUnit/UnityTestFramework, Windows x64, Git.
**Spec:** Docs/RRL_W3_W4_INTEGRATION_DESIGN.md; user attachment b46ea2b6-5851-4e91-a539-f9aab67a858c.

## Global Constraints
- BaseW2a0f0a6590b07ed2f5007361a7e761f133e52ff99; sourcePR17/18 unchanged; Draft PR targets feature/unified-rewards-debris-density. No main/develop merge/force push.
- Exactly1bonus from3 plus independently0–3parts, including final; single atomic settlement and no bonus before mounting.
- Bonus1high×2wide×2long,1ID/HP/physicalbody,4cells; connected/alive only; loss removes effect; rebuild never duplicates stacking.
- Quarter-turn XYZ rotations: Q/E=Y,R/F=X,Z/X=Z; physical geometry/colliders/ports/device direction agree. P12 is sole graph.
- Retain paid repairs, garage arena hiding, W2 dense roster and debris/projectile fixes, MOV001–009. Never edit Assets while this checkout's batch Editor runs.
- Full regression and both Windows builds; native QA1280×720/1920×1080; rawlogs/XML local ignoredArtifacts/Local; no fake PASS or activation workaround.

## Review Focus
1. Missing/invalid bonus or inventory overflow after salvage validation must leave ledger, progress, IDs, scrap and baseline stats unchanged; Task1 atomic rejection/replay cases.
2. Rotated multivoxel module losing its anchor/bridge must leave no ghost connector; Task2 physical projectile/group detachment cases.
3. Modifier removal/rebuild/map/range cycles must preserve damage ratios and baseline without stale or squared factors; Task3 live runtime/device cases.
4. Directional MOV devices must follow actual mounted orientation under XYZ rotations, including negative/combined turns; Task2/3 physical pose/force cases.
5. 0HP or disconnected inventory ghosts, occupied rotated cells and panel shortcuts must agree with mount validation at both resolutions; Task4 input/placement/native cases.

### Task 1: Import PR code and make independent rewards atomic
**Files:** R4RewardSettlement.cs,R4SalvageLedger.cs,R3VehicleModel.cs,R1RunController.cs,R1RunProgress.cs; new Assets/Tests/Editor/RRLW34RewardTests.cs; imported preliminary tests and metas.
**Interfaces:** Consume existing TryClaim(progress,ledger,model,IEnumerable<string>,IEnumerable<int>); produce same signature with exactly1physicalbonus plus0–3parts. Preserve ReceiveBonuses/rotation data imported from W3/W4.
- [x] Run clean W2 targeted baseline (models/physics/industrial garage), native exit0 and all cases PASS.
- [x] Write RRLW34RewardTests first against actual settlement: zero bonus rejected without mutation;1bonus+3parts accepted and stored as4items; no immediate stat effect; final3offers/required1bonus; malformed/capacity/replay atomicity.
- [x] Run new cases RED before merge; expected behavioral failures under old W2 reward rules.
- [x] Merge PR18 head without committing (containsPR17). Resolve conflicts preserving current W2; keep imported code and record actual provenance/conflicts. Relocate preliminary tests/metas to Assets/Tests/Editor and fix compilation issues.
- [x] Route independent rewards through R4RewardSettlement; preflight all mutations, finite values/capacity/IDs. No split controller settlement.
- [x] Run reward tests GREEN and appropriate repair/density/debris regressions; adapt old reward tests only to explicit new requirements; commit integrated reward deliverable.

### Task 2: One physical module, rotated ports and P12 detachment
**Files:** R3MountingGrid.cs,R3BonusModuleGeometry.cs,R3VehicleAssembly.cs,R3MountedPart.cs,R3VehicleModel.cs,P12_PipelineSafeVehicleStructure.cs; new RRLW34StructureTests.cs.
**Interfaces:** Consume OccupiedCells(part),Rotation(turns),Compatible(...),PartState.RotationQuarterTurns/ContactMask; produce identical model/liveP12 connectivity and one body per module.
- [x] Read imported structure/rotation tests and establish actual scene fixtures.
- [x] Add RED cases for4-cell bounds/overlap, all XYZ orientations, reciprocal restricted ports, last bridge destruction, non-anchor-face support, owner death and no surviving proxy connectors, whole connected chunk ejection.
- [x] Make smallest P12/shape changes supported by failures; deduplicate same physical item across cells, preserve W2 release/debris behavior and default six-port legacy blocks.
- [x] Prove actual mesh/collider rotation and projectile HP, capture/remove/repair/remount preserve one ID/HP/orientation; run structure/W2 physics/MOV graph tests GREEN; commit.

### Task 3: Live effects, lifecycle and MOV propagation
**Files:** R3MountedBonusEffects.cs,R1VehicleState.cs,R3VehicleAssembly.cs,R6RangeController.cs,MOV adapters only as required; new RRLW34EffectsTests.cs.
**Interfaces:** Consume baselineProgress.Stats and mounted identity/connectivity; produce EffectiveStats/ActiveStats and live controller/device parameters without modifying baseline.
- [x] Add RED tests for inventory-inactive, mounted-active, multiple-module stacking, destruction/detachment removal, no stacking on rebuild, health ratio and field repair, next fight/map/range round trips.
- [x] Assert actual weapon damage/interval, chassis/movement, core/hull maxHP and MOV device behavior rather than only copied stats.
- [x] Repair propagation/refresh timing and rebuild paths from failing evidence; no permanent stats or free healing.
- [x] Run effects+relevantMOV/garage/runtime regressions GREEN; commit.

### Task 4: Garage UX, full regression, Windows and gameplay
**Files:** R3GarageView.cs,R4RunHUD.cs,R1RunHUD.cs,RRLW2RuntimeQA.cs or dedicated conditional observer; existing Assets/Tests/Editor cases and new RRLW34GarageTests.cs.
**Interfaces:** Consume same occupancy/ports/rotation/eligibility as actual mount and ActiveStats; expose existing pointer flows and six90degree shortcuts without panel click-through.
- [x] Add input/ghost RED cases for Q/E,R/F,Z/X, occupied footprint/mismatched ports/0HP preview and unchanged money on invalid mount.
- [x] Fix UI and fixture assumptions only as new requirements demand; preserve every old case. Record before/after test names and intended changes.
- [x] Full Tools/CI/Run-Unity.ps1 EditMode,PlayMode,Build: nonempty allPASS,no skips,native0. Verify protected Unityversion/packages and source hashes.
- [x] Native1280×720 and1920×1080: required1bonus+3parts→garage→rotate/mount→activeeffect→nextfight→physicalmoduleloss→effectoff; repair/remount and bridge loss; range/map persistence. Label fixture versus input-pilot evidence accurately.
- [x] Record compile errors/fixes, actual transferred files, conflicts, test counts/build/native limits and READY/BLOCKED in Docs/RRL_W3_W4_INTEGRATION_QA.md; commit.

### Task 5: Fresh review and publication
**Files:** Review package/ledger and final report/evidence.
**Interfaces:** Consume all tested code and preserved source PR commit IDs; produce one Draft integration PR based on W2.
- [x] One fresh whole-branch review under requesting-code-review; most capable model, read-only, full diff versus W2 plus spec/plan/review focus/rulings.
- [x] Grade findings by player effect, repair Critical/Important RED→GREEN in one fix pass and full regression; ledger all decisions/deferred minors.
- [x] Validate repository/index/metas, test-case preservation, secrets/links, source/binary hashes, clean commits. No generated Library/build/rawlogs published.
- [x] Push isolated branch, create Draft PR targeting W2; attach it, inspect exacthead/checks and report local/cloud results separately. Leave PR1/17/18 untouched.

Execution record: all gates and the one-review fix pass are detailed in Docs/RRL_W3_W4_INTEGRATION_QA.md. Final source 5d4b61d; EditMode434/434, PlayMode176/176, Windows retail+QA PASS, native29+29+8 PASS. Draft PR #19 targets W2; no main/develop or source-PR merge. Known automation-close and input-pilot limits remain documented.

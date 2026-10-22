# W3 + W4 integration and gameplay QA

Status: IN PROGRESS. This report is authoritative for the integration; the imported W3/W4 reports describe their original source-only submissions.

## Provenance and isolation

| Input | Verified commit | State on 2026-10-22 |
|---|---|---|
| W2 PR #1 | `a0f0a6590b07ed2f5007361a7e761f133e52ff99` | Open Draft, unmerged |
| W3 PR #17 | `08503536c883f95dcdc927dca390b643eb695c8f` | Open Draft, unmerged |
| W4 PR #18 | `25a3c387a56fd31842e18cb18b482aedef66c501` | Open Draft, unmerged; includes W3 ancestry |

Integration branch: `feature/rrl-w3-w4-integration`, based on exact verified W2. W4 was merged once with W3 history included. Source PRs are unchanged. No main/develop merge, force push, license upload or engine upgrade.

Final tested source SHA: pending. Draft integration PR: pending; target `feature/unified-rewards-debris-density`.

## Transferred source files

All paths below are repository-relative. Existing `.meta` GUIDs were retained.

- `Assets/R1/R1RunController.cs`, `R1RunHUD.cs`, `R1RunProgress.cs`.
- `Assets/R3/R3BonusModuleGeometry.cs` and meta; `R3GarageView.cs`; `R3MountedBonusEffects.cs` and meta; `R3MountedPart.cs`; `R3MountingGrid.cs` and meta; `R3VehicleAssembly.cs`; `R3VehicleModel.cs`.
- `Assets/Pack/P12_PipelineSafeVehicleStructure.cs`.
- `Assets/R4/R4RunHUD.cs`, `R4WeaponDefinition.cs`.
- `Assets/Resources/R1/Upgrades/{armor,core,damage,mobility,reload,repair}.asset`.
- Preliminary `Assets/Editor/R3PhysicalBonusModuleTests.cs` and `R3ContactAndRewardTests.cs`, with their metas, relocated into canonical `Assets/Tests/Editor/`. Their cases and GUIDs remain; conditional guards were removed so the test assembly discovers them.
- `Docs/RRL_W3_PHYSICAL_BONUS_MODULES_QA.md`, `Docs/RRL_W4_REWARDS_CONTACTS_ROTATION_QA.md`.

Integration additionally extends W2's `Assets/R4/R4RewardSettlement.cs` and `R4SalvageLedger.cs`, adds W34 regression fixtures and fixes the rotated booster binding in `Assets/MOVSeriesIntegration/MOVSeriesDevices.cs`.

## Resolved merge conflicts and implementation

| Conflict | Resolution |
|---|---|
| `R1RunController` | Preserve W2's shared atomic reward settlement and controller lifecycle; add oriented mounting and physical bonus stats. |
| `R3VehicleModel` | Preserve W2's finite HP/identity validation, full-price wreck repair and paid emergency quotes; incorporate rotated footprints, reciprocal ports and physical reward metadata. |
| `R3GarageView` | Preserve arena visibility restoration, camera near plane and world frame; incorporate rotated previews/ports and reject zero-HP inventory previews. |
| `R4RunHUD` | Preserve W2 pagination, keyboard input and loot readability; require one bonus independently of zero to three parts. |

The transaction prepares one validated bonus and selected salvage, then stores them in a single validated inventory batch before paying scrap, closing the ledger and advancing progress. Missing/multiple/malformed bonuses, duplicate/invalid parts, capacity failure and replay leave inventory, currency and progress unchanged. Final victories also offer three bonuses and require one selection. No reward modifies baseline stats.

The original W3 geometry, physical HP and inventory identity remain. Its three HP-less structural helper objects were replaced by four cell entries referencing one physical owner in the existing P12 graph. Counts, disconnected components and release mass deduplicate that owner. Destruction and bridge loss remove the entire footprint; legacy six-port blocks keep their behavior.

The booster now binds thrust direction to its physical mount. Controller remount without an explicit new rotation preserves the item's stored orientation.

The garage preview cache includes the selected contact mask, so selecting an equal-shaped part rebuilds its own port markers. Inventory feedback uses the same eligibility message as the ghost, including zero HP and missing targets.

## Executed verification

Targeted suites overlap and are not summed as a unique test total. All evidence directories below are under ignored `Artifacts/Local/`.

| Gate | PASS / FAIL / skipped | Native exit | Evidence directory |
|---|---|---|---|
| Clean W2 model/physics/industrial garage baseline | 35 / 0 / 0 | 0 | `W34-W2-Baseline-5ad88413ed5a4cdc8964334c385a5694` |
| New reward contract before import, expected RED | 1 / 12 / 0 | 2 | `W34-Reward-RED-before-import-a4afaf66441f43cba61dc0d770132313` |
| Imported reward and preliminary tests | 24 / 0 / 0 | 0 | `W34-Rewards-GREEN-9dafd67b41ee427e8d1f949c3cb3f87e` |
| Atomic rewards plus W2 model/physics/garage regression | 49 / 0 / 0 | 0 | `W34-Task1-Regression-6741e5a76f4c4c9e8c2824bd7465d181` |
| Physical structure before helper removal, expected RED | 3 / 2 / 0 | 2 | `W34-Structure-RED-9250098f037a4f31bc5efa2edd81e3bb` |
| Physical structure and W2 combat/physics regression | 15 / 0 / 0 | 0 | `W34-Structure-GREEN-9f6c312ac4c94740b961c9f3ad39b9a9` |
| Live effects and existing MOV device regression | 14 / 0 / 0 | 0 | `W34-Effects-GREEN-8c5541adb334434191f5677182d5dd7c` |
| Garage before controller remount fix, expected RED | 5 / 1 / 0 | 2 | `W34-Garage-RED-d9c1817ba4bd4b5ebac42b71410061c0` |
| First full EditMode; one superseded direct-stat fixture | 427 / 1 / 0 | 2 | `20261022T102245Z-bc5c5c3cb8b14fc0a92bcb917c47c719` |
| Garage, physical weapon fixture and existing R41 regression | 18 / 0 / 0 | 0 | `W34-Garage-Weapons-GREEN-6c3aa3c6f9494466bf4d123491cad554` |
| Final full EditMode | NOT RUN | pending | pending |
| Full PlayMode | NOT RUN | pending | pending |
| Windows x64 retail and QA builds | NOT RUN | pending | pending |
| Native 1280x720 and 1920x1080 | NOT RUN | pending | pending |

Repository validation: 720 indexed files, zero errors at initial gate. CI validator self-tests: 16/16 passed with temporary fixtures inside the writable worktree. Default sandbox TEMP was unwritable; this was an environment failure, not a product test failure.

Actual scene assertions cover all 64 XYZ quarter-turn combinations, non-anchor support, reciprocal ports, whole-module bridge ejection and a physical projectile hitting the non-anchor cell for exactly 25 HP. Effects tests fire an actual weapon projectile with the damage multiplier, check real HP/core/reload/omni limits, six simultaneous bonuses and an inactive inventory module, preserve 37% HP over three rebuilds, stop field repair after module loss, and verify range/reset/return plus next-map/repair/remount identity.

Compilation fixes: preliminary W4 test used an illegal implicitly typed multiple declaration (CS0819); split the two locals. One integration test adaptation initially referenced the wrong local name (CS0103); corrected to the selected gun. Neither compilation failure is counted as a passing test run.

## Regression expectation changes

All prior regression cases remain, with changes only for superseded reward requirements. The baseline test source archive is retained locally for comparison.

- W2 reward compositions with zero/two/three bonuses now assert atomic rejection; one bonus plus three parts is added. Baseline stats stay unchanged, and inventory includes the physical bonus.
- Final reward cases now expect three offers and one mandatory physical bonus; six-stage acquisition count becomes six.
- W2's live reward case now rejects three bonuses, verifies inactivity in inventory, then mounts the selected damage module before asserting live damage.
- Inventory assertions identify recovered parts by ID, or include the additional one-per-victory bonus items. Paid garage damage persistence mounts the earned module explicitly.
- `R4CombatTests.AllWeaponsHaveIndependentCadenceUpgradesAndPhysicalDamage` now receives and mounts damage/reload modules before checking all three weapons' actual physical hits and independent cadence. Its old direct mutation of permanent progress stats is superseded; no damage/cadence assertion was dropped.

## Gameplay evidence, review and decision

Pending native screenshots/results, final fresh review and publication. No READY claim is made before these gates complete. Raw XML/logs, Library, binaries, local state and license data remain excluded from Git.

# W3 + W4 integration and gameplay QA

Status: READY for local integration / Draft PR review. This report is authoritative for the integration; the imported W3/W4 reports describe their original source-only submissions.

## Provenance and isolation

| Input | Verified commit | State on 2026-10-22 |
|---|---|---|
| W2 PR #1 | `a0f0a6590b07ed2f5007361a7e761f133e52ff99` | Open Draft, unmerged |
| W3 PR #17 | `08503536c883f95dcdc927dca390b643eb695c8f` | Open Draft, unmerged |
| W4 PR #18 | `25a3c387a56fd31842e18cb18b482aedef66c501` | Open Draft, unmerged; includes W3 ancestry |

Integration branch: `feature/rrl-w3-w4-integration`, based on exact verified W2. W4 was merged once with W3 history included. Source PRs are unchanged. No main/develop merge, force push, license upload or engine upgrade.

Final game/test source SHA: `5d4b61d1160e9cceac888e070147aead71dfb92b`. Only evidence/report changes follow this source commit. Draft integration PR: pending; target `feature/unified-rewards-debris-density`.

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

The booster now binds thrust direction to its physical mount. Controller remount without an explicit new rotation preserves the item's stored orientation. Track traction follows each module's forward axis; sideways/inverted track probes and hover nozzles use their physical mounting axes. Ordinary upright mounts retain the original gravity-stabilized support behavior.

The reward screen exposes owned inventory management, including explicit two-click dismantling and a return to the retained reward selections. It disables settlement when the selected batch will not fit. The controller permits this recovery only during an open salvage transaction; inventory disposal never settles the reward ledger. This prevents a full inventory from blocking the mandatory bonus, including the final victory. The garage counter now reports occupied cells against the 180-cell limit.

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
| Review fixes: rewards, core death, directional forces and existing MOV regression | 40 / 0 / 0 | 0 | `W34-Review-Fixes-Final-GREEN-26d1943fdaaa4b6c99e14624486da825` |
| Final full EditMode | 434 / 0 / 0 | 0 | `20261022T174447Z-6fc77cd005f944a78e76a5a5fb150176` |
| Full PlayMode | 176 / 0 / 0 | 0 | `20261022T180640Z-307118fef2f34476a9359389eaf452a1` |
| Windows x64 retail and QA builds on final source | Both succeeded; 0 errors, 4 existing warnings each | 0 | `20261022T174338Z-2c9df743417e4fd6a45a92847c558627` |
| Native graphical lifecycle, 1280x720 | 29 / 0 / 0 assertions | gameplay PASS; automation close `C0000005` | `W34-Native-Fixture-1280x720-0cbe4b3f09f845b9bbeb1205b1ddcbb2` |
| Native graphical lifecycle, 1920x1080 | 29 / 0 / 0 assertions | gameplay PASS; automation close `C0000005` | `W34-Native-Fixture-1920x1080-905e9f54ae8248e399ac2122a54fc417` |
| Native full-inventory recovery UI, 1280x720 | 8 / 0 / 0 assertions | gameplay PASS; automation close `C0000005` | `W34-Native-Fixture-1280x720-86984d4ce8354c82866bfa4e90f918d7` |

Repository validation: 725 indexed files, zero errors after the final source fix. CI validator self-tests: 16/16 passed with temporary fixtures inside the writable worktree. Default sandbox TEMP was unwritable; this was an environment failure, not a product test failure.

Actual scene assertions cover all 64 XYZ quarter-turn combinations, non-anchor support, reciprocal ports, whole-module bridge ejection and a physical projectile hitting the non-anchor cell for exactly 25 HP. Effects tests fire an actual weapon projectile with the damage multiplier, check real HP/core/reload/omni limits, six simultaneous bonuses and an inactive inventory module, preserve 37% HP over three rebuilds, stop field repair after module loss, and verify range/reset/return plus next-map/repair/remount identity.

Compilation fixes: preliminary W4 test used an illegal implicitly typed multiple declaration (CS0819); split the two locals. One integration test adaptation initially referenced the wrong local name (CS0103); corrected to the selected gun. Neither compilation failure is counted as a passing test run.

## Regression expectation changes

All prior regression cases remain, with changes only for superseded reward requirements. The baseline test source archive is retained locally for comparison.

A method-name audit of all 39 baseline test scripts finds exactly three old names replaced by new names, with no other methods removed:

| W2 method | Integrated method | Superseded expectation |
|---|---|---|
| `R1RunModelTests.ThirdVictoryEndsRunWithoutAnotherUpgrade` | `ThirdVictoryOffersOneLastPhysicalBonusBeforeCompletion` | Final victory also has a mandatory physical reward. |
| `RRLW2GameplayTests.UnifiedThreeBonusesChangeLiveStatsOnceAndOpenGarageDirectly` | `OnePhysicalBonusChangesLiveStatsOnlyAfterMountAndOpensGarageDirectly` | Exactly one physical bonus, active only when mounted. |
| `RRLW2ModelTests.FinalRewardsResolveToWonWithNoBonusOrGarage` | `FinalRewardsResolveToWonWithOnePhysicalBonusAndNoGarage` | Final settlement includes one bonus before Won. |

- W2 reward compositions with zero/two/three bonuses now assert atomic rejection; one bonus plus three parts is added. Baseline stats stay unchanged, and inventory includes the physical bonus.
- Final reward cases now expect three offers and one mandatory physical bonus; six-stage acquisition count becomes six.
- W2's live reward case now rejects three bonuses, verifies inactivity in inventory, then mounts the selected damage module before asserting live damage.
- Inventory assertions identify recovered parts by ID, or include the additional one-per-victory bonus items. Paid garage damage persistence mounts the earned module explicitly.
- `R4CombatTests.AllWeaponsHaveIndependentCadenceUpgradesAndPhysicalDamage` now receives and mounts damage/reload modules before checking all three weapons' actual physical hits and independent cadence. Its old direct mutation of permanent progress stats is superseded; no damage/cadence assertion was dropped.

## Gameplay evidence, review and decision

One fresh independent whole-branch review examined W2 `a0f0a65` through `e483aa0`. It found no Critical issues and three Important issues: no recovery from a full inventory at mandatory rewards, track/hover mounting direction, and duplicate physical owners in a core-failure chunk. Each received a behavior-first regression test and a fix in one follow-up pass. The minor occupied-cell counter was corrected as well. Legacy unexposed upgrade compatibility and intentionally chassis/gravity-oriented devices were retained. No second review cycle was requested.

Native graphical fixtures at both 1280x720 and 1920x1080 passed 29 assertions each on final source `5d4b61d`. The operator selected one damage bonus and three real parts, inspected the inactive inventory module and a destroyed gun, used all six XYZ rotation shortcuts at 720p and mounted a green preview at both resolutions. The fixture then exercised next map, physical module loss, exact paid repair, removal/remount, range/reset/return and stable identity. Damage follows `1.00` inactive → `1.25` mounted → `1.00` lost → `1.25` repaired, while baseline damage stays `1.00`. The repaired module has one physical owner, one collider and 250/250 HP. Victory and module destruction were explicitly seeded through damage APIs, so these are graphical lifecycle fixtures, not claims of manual combat wins. Historical pre-review runs each passed 28 assertions and are superseded by these final-source runs.

The final full-inventory graphical run passed 8 assertions. At 200/200 items, the selected one bonus plus three parts cannot settle. First-click dismantling only displays confirmation and leaves 200 items. Four operator-confirmed wheel disposals yield 12 scrap and reduce storage to 196. Returning preserves the chosen bonus and all three parts; settlement succeeds once, returns inventory to 200 and leaves the bonus inactive. The separate six-stage controller regression covers the same recovery on every encounter and on final victory.

Curated game-window screenshots, sanitized lifecycle samples and the managed-assembly SHA-256 hashes of both tested binaries are in [`Docs/Evidence/W3W4`](Evidence/W3W4/README.md).

An additional native input pilot on `e483aa0` defeated the first three active-AI encounters with physical projectiles and ordinary steering/aim/fire input, without damage/reward/currency APIs. The operator claimed one hull module plus three real parts, mounted the hull module (100→120 maximum core HP), paid 9 scrap for repair, later mounted a reload module and claimed a third bonus. The run reached encounter four and closed before final victory. It is not counted as a completed six-stage gauntlet. Evidence: `W34-Native-Pilot-1280x720-3a90c4eebc324e5e86c43b3adce942a2`.

A fresh input pilot on final source `5d4b61d` won four actual encounters: First Scrap (2 enemies), Open Convoy (3), industrial Crossfire (4) and elite Bastion (2). The operator claimed one hull module plus three real parts after the first victory, then independently chose one bonus and zero parts at subsequent rewards. Hull, reload, core and damage modules were physically mounted, including module-on-module support. Actual maximum core HP changed 100→120→180; baseline progress multipliers stayed 1. Paid repairs charged exactly 7, 11, 23 and 4 scrap. The run recorded physical debris (peak 10) and no gameplay errors. During encounter five the simple QA pilot drove directly into an industrial wall and remained blocked; the run was closed with four completed encounters and `won=false`. This is a pilot navigation limitation, not a six-stage victory or a claim of human manual play. Evidence: `W34-Native-Pilot-1280x720-adf481f1ee554209be79af3b04f76138`; sanitized observations are included with the native evidence. The six-stage progression/reward regression remains a separate Unity test gate.

Known native limitations: automation-driven Alt+F4 ended with `C0000005` after both successful fixtures and the pilot, consistent with the pre-existing Unity accessibility shutdown pattern. A normal human W3/W4 close has not been verified. One 1080p injected numeric reward shortcut did not select; mouse buttons and the six rotation shortcuts worked. This is recorded as unverified rather than treated as evidence of a product input defect. The exact required Unity version is unchanged.

Final EditMode and PlayMode contain 610 passing test cases in total, zero failures and zero skips; overlapping targeted cases and graphical assertions are not added to that total. The superseded EditMode run `20261022T105824Z-57a4031fb29d4b94ada9a763c9c702aa` was deliberately canceled when review found important issues; it is not counted as a pass or failure.

Final post-regression retail and QA builds both succeeded (native exit 0, zero errors, four existing CS0414 warnings each). Evidence: `20261022T180814Z-0cbb35b6b0fa4f0aa4249e7ed0b795ba`. Retail output is 102843460 bytes; QA output is 102871065 bytes. Both managed-assembly SHA-256 hashes exactly match the binaries used for native QA, as recorded in the evidence JSON. Assets, Packages and ProjectSettings are unchanged from the tested source commit.

Decision: READY for a Draft integration PR based on W2. No unresolved Critical or Important code-review finding remains; all local suite/build and required native lifecycle gates passed. Known automation-close and pilot-navigation limits above remain explicit. This is not approval to merge or a claim that cloud Unity tests ran.

The existing workflows target PRs into main/develop, while this integration must target the W2 feature branch. Cloud checks on this integration are therefore NOT RUN unless explicitly dispatched. W2 previously documented a Unity activation blocker; no activation workaround, secret or license upload was attempted. Publication is the remaining task. Raw XML/logs, Library, binaries, local state and license data remain excluded from Git.

# B01 UNIFIED RC2 — integrated Windows candidate QA

2026-10-26. Draft [PR258](https://github.com/C8LyNn/RoboRougelike/pull/258), branch `integration/b01-unified-rc2`, based on PR248 `integration/b01-rc1-experimental`.

**Decision: READY_FOR_INDEPENDENT_REVIEW — one experimental playable Windows candidate.** Compilation/build and seven finite Windows runs passed. The initial full Edit cycle had two stale-fixture failures; both corrected cases passed a targeted retest, with production unchanged. Full PlayMode passed424/424. Ordinary UI/hardware acceptance remains NOT_RUN because the tool could not initialize. This is not formal production acceptance, G1/G2 or main/develop merge authorization.

## Exact provenance and scope

- Canonical RC1 gameplay: `86eb7bb77e6bfc7787f781ea1ccda547b8a4f643`.
- Starting PR248/report checkout: `5c29e8e28e7b34fedf0c992fca49451828b02314`, identical Assets/Packages/ProjectSettings to that gameplay SHA.
- **Frozen final RC2 production/build source: `46b42f62d591ad4aeb9dbfdeda9997044c6b5e1f`.** Build, initial full native cycle and Player launch receipts retain this exact SHA and binary hashes. Post-cycle Editor fixture revision **`f6184d2254c0ff4c59bcd1e9f21db6a134a54097`** changes only two test bodies; production source, binary and archive remain identical. [Equivalence proof](production-equivalence.json). Later report/tool commits do not change production.
- Isolated checkout: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-B01-Unified-RC2`. All other worktrees, historical outputs and uncommitted work are preserved.
- Unity **6000.3.14f1**, existing **URP17.3.0**, Windows x64 / Mono / D3D11, RTX5080 host. No Unity/UPM change.
- Source admission and excluded packages: [PACKAGE_STATUS.md](PACKAGE_STATUS.md). Frozen archive/source file receipts: [Intake](Intake). Final [integrity](final-integrity.json): 851 unique GUIDs, zero duplicate/missing meta, original shared parent retained, MAPGENWORLD36 and KEEP325 source files unchanged, original reward port byte-identical. No BUILD/RAC/CAMERA/LEGENDARY/ECON runtime tree remains.

One ordinary menu now enters the existing preparation garage. Its route, generated arena, reactor defense and test range use the same R3 model, R6 physics, original damage and wallet. No parallel inventory, HP, economy, garage or progression was added. Existing six-stage gauntlet and R3 physical1x2x2/XYZ/contacts remain. The obsolete separate RC1 menu entry was removed; its public API remains compatible.

MAPGENWORLD1.0.1 is an in-place package upgrade. REWARDKEEP3 uses its unchanged original policy and original installation port, a narrow authenticated receiver host and **the original atomic R4RewardSettlement**. Real enemy equipment gets its canonical slot GUID before firing, effects or salvage; the same ID reaches inventory. One mandatory independent bonus plus0..3nonArmor/Core enemy parts remains the rule. Capacity refusal, retry, replay and direct raw-policy bypass are checked. Session event receipts are not durable Save/Load; the game does not provide a persistent run checkpoint.

## Native Unity results

Every result below has actual XML and native process receipts in [Native/native-summary.json](Native/native-summary.json). Surrogate .NET/CI tests are not counted. Historical RED and failed instrumentation are retained, not rewritten as success.

| Stage | Result | Exact evidence |
| --- | --- | --- |
| Final compilation | **PASS**, native0 | [Compile receipt](Native/RC2-ReleaseCandidate-20261026T115006Z/Compile-process.json), Editor log.gz alongside |
| Initial full EditMode | **FAIL:1066/1068 PASS,2FAIL**,0skipped, native2 | [Full XML](Native/RC2-FinalRegression-20261026T115307Z/EditMode.xml), [actual process](Native/RC2-FinalRegression-20261026T115307Z/EditMode-process.json) |
| Corrected two Editor fixtures, targeted rerun | **PASS:2/2**,0skipped, native0 | [XML](Native/RC2-LegacyFixturesRecheck-20261026T123558Z/EditMode.xml), [actual process](Native/RC2-LegacyFixturesRecheck-20261026T123558Z/EditMode-process.json); f6184d2254c0ff4c59bcd1e9f21db6a134a54097 |
| Full PlayMode | **PASS:424/424**,0skipped, native0 | [XML](Native/RC2-FinalRegression-20261026T115307Z/PlayMode.xml), [actual process](Native/RC2-FinalRegression-20261026T115307Z/PlayMode-process.json) |
| Windows x64 release build | **PASS**, native0; 0 errors / 4 warnings;103391191 built bytes | [Build receipt](Native/RC2-ReleaseCandidate-20261026T115006Z/Build-process.json), [provenance](build-provenance.json) |
| MAPGENWORLD source + existing game host | **PASS**, Edit11/11, Play4/4, native0/0; compile0 | [Edit XML](Native/RC2-World101-20261026T105030Z/EditMode.xml), [Play XML](Native/RC2-World101-20261026T105030Z/PlayMode.xml); source snapshot328706d6665feb0796e9a50b83e06d562563d906 |
| Reward host + old EMP/boss/W2 + unified entry | **PASS**, Edit57/57, compile0, native0 | [XML](Native/RC2-RewardHost-GREEN-20261026T112942Z/EditMode.xml); snapshotf5cae0da5a85e25a0b6949463506f2579bf3439b |
| Raw reward service boundary | **RED**1/1FAIL at0663d3ac5da17786bdc3fa95de71cd169df91071, then **GREEN**7host+3unified+6CAMdiagnostic+3LEG+3ECON=22/22; compile0, native0 | [RED](Native/RC2-RewardBoundary-RED-20261026T113405Z/EditMode.xml), [GREEN](Native/RC2-ConditionalSources-RewardBoundaryGREEN-20261026T113656Z/EditMode.xml); GREEN084a64292c0c0db1dd54151b30674d454319e8e9 |
| BUILD source native | Original18Edit/6Play **PASS**, but receiver geometry **2FAIL/1PASS** | [Blocked reproduction](Blocked/BUILD_R13_CONTACT_AREA.md), snapshot702cb3b2eeba1661f8ca16f6ff75de76ba4ea5e4; package excluded |
| RAC source native | Edit11/11 **PASS**, Play3/4 **FAIL**, native2 | [Blocked reproduction](Blocked/RAC_R31_NATIVE.md), snapshotc3629d86144ea07ea404c87709d86e598f035675; package excluded |

Receiver instrumentation history: initial unified-entry native RED exposed missing preparation entry. Later headless Editor keyboard fixtures wrote Editor-only input buffers; those failed input-readiness checks are not production failures. The final Editor-only probe routes test devices to GameView using supported InputSettings and restores both settings/device on teardown. A separate temporary `InputSystem.Update(InputUpdateType.Dynamic)` overload mistake produced one compile1 at `RC2-Keep3-SourceGate-20261026T110659Z`; no later phases ran for that attempt. It was confined to the receiver fixture and corrected without modifying producer code or weakening assertions. Mixed BUILD/RAC Edit groups also contain that unrelated unified-input failure; their source-only counts above are obtained from actual individual cases.

The four build warnings are existing CS0414 unused input fields in MOV009 `ActiveSuspensionPrototypeInput`, MOV006 `PrototypeGrappleInput` (two) and MOV004 `GyroPrototypeInput`. They were not broadened into an unrelated producer refactor.

Full-cycle failures were investigated, not suppressed. `R1PlayIntegrationTests.KeyboardEnterStartsRunWithoutPrematureDefeat` expected immediate Combat, contradicting the authorized RC2 preparation garage. The revised test checks Garage/no enemies first, then explicitly chooses a route and retains Combat/CoreAlive assertions, adding model/physical-ID preservation. `RRLW34ReviewRegressionTests.FullInventoryHasExplicitRecoveryForMandatoryRewardsIncludingFinalVictory` forcibly replaced real R3 GUIDs with invalid `full-inventory-i` strings, causing source policy's correct owner rejection. The revised fixture uses original constructor GUIDs, adds valid/unique200ID checks, and retains all capacity refusal, atomicity, dismantle/retry, counters and six-stage/final victory assertions. A fresh read-only review confirmed neither correction weakens safety. These two cases actually passed the22second rerun. **No second unfiltered Edit run is claimed:**1066unchanged cases passed in the full run; the two changed cases passed separately. No production change or rebuild was necessary.

## One actual Windows application

The same release EXE and Assembly-CSharp were used in all rows. Explicit CLI diagnostics are dormant during normal launch; they do not create separate delivered games. Original `Assets/scene 1.unity`, `BuildOptions.None`. [All launch/process/result/image receipts](Windows/windows-summary.json).

| Scenario | 1280x720 | 1920x1080 | Method and observed result |
| --- | --- | --- | --- |
| Input / ordinary controller | **PASS**, native0 | **PASS**, native0 | Queued native InputSystem keyboard/mouse; actual unchanged player Update/R6/fire,13distinct checks. W moves17.621m, max12grounded wheels;8owned projectiles; D turns51.979deg; RMB adds0LMB shots; Esc pause freezes/resumes actual body. Canonical activity selection uses API. Not OS/human control |
| Generated CombatArena / damage / reward / repair / next map | **PASS**, native0 | **PASS**, native0 | API-controlled original R3/R6 forces and real projectiles;20observations/19distinct checks.185/185samples with supported wheels, max12. Accumulated3Ddistance15.541/15.497m; final positions(-.00859,.65001,-9.84325)/(-.00907,.65001,-9.88716). Real hostile HP loss28.56/20.40; enemy40HP. Subsequent win/loss uses explicit Core damage |
| Reactor defense / three waves / rewards / defeat / restart / ordinary battle | **PASS**, native0 | **PASS**, native0 | Real AI, HP and original settlement.364observations are21distinct check names, not364independent tests. Controlled enemy positioning/Core kills complete waves; not an unassisted victory |
| Exact extracted final ZIP EXE, Input scenario | **PASS**, native0 | Not repeated | Assembly hash matches build. Same13distinct checks; extraction/CRC/all-byte gate also PASS |
| Retail ordinary clicks / hardware-held driving | **NOT_RUN — UI TOOL UNAVAILABLE**, manual check requested | **NOT_RUN** | Initialization fails before any app state/input: [tool failure](Windows/UI_TOOL_LIMITATION.json). Old RC1 UI evidence is historical, not a new RC2 PASS |

Generated-map checks include actual source Resources shader `B01/MAPGEN001/BlocksURP` without replacement material, canonical source spawns, old floor disabled during activity, actual projectile damage, victory→1bonus+enemy recovery→same garage, repair after explicit2HP fixture injury, actual recovered part mount, seed31regeneration, controlled player loss restoring terrain, restart removing old map/material. The attempted optional drive.csv is empty and is not used as evidence; actual positions/contact counters are in JSON. Distances include initial vertical settling and are not a lap/path completion claim.

All seven finite runs reported Passed, empty errors and native0. Player logs contain no matched exception/error line. All58native PNGs are true1280x720/1920x1080 and nonblack; gray ground/brown obstacles and normal robot colors are visible, with no pink error material. [Generated arena1080](Windows/RC2-Windows-Mapgen-1920-20261026T120127Z/02-generated-start.png), [garage720](Windows/RC2-Windows-Input-1280-20261026T115537Z/01-garage.png). Exact image dimensions/hashes and log.gz are archived alongside each run.

## Bugs, stability and limitations

Fixed receiver integration defects: ordinary entry now opens the canonical preparation garage; stable physical enemy slot IDs replace incompatible debug IDs without claim-time remapping; forged/stale/owned/over-capacity selection is refused; raw policy access cannot bypass the controller's reward accounting after a refused attempt. Native negative/positive tests and assisted Windows reward loops cover these boundaries. Producer source and shared authoritative systems were not rewritten.

New producer defects left visible and excluded: BUILD under-supported contact acceptance/incorrect nested area; RAC authored force demo does not reach its assertion. CAMERA aiming evidence remains unresolved. LEGENDARY/ECON actual dependencies are absent, so state-only native tests do not admit gameplay. Detailed expected/actual configuration, SHA and logs are in the blocked reports and were sent to B01-23/B01-00.

**C0000005 is unresolved, not fixed.** Previous RC1 controlled evidence retained in [RC1 report](../B01_RC1/RC1_INTEGRATION_QA.md): UI-client-attached exits could return-1073741819, while client-free visible scenarios returned0. This is correlation, not established cause. The seven current finite RC2 scenarios used Application.Quit and exited0 without a UI client; no source fix for shutdown was made. Current UI runtime cannot initialize, so it cannot provide a matched attached-client reproduction. Manual extracted-retail garage/Alt+F4 result is pending and must be recorded separately.

Not claimed: full six-stage human victory, hardware-held driving, arbitrary construction coverage, all weapon combinations in Windows, a complete FPS/PC1/PC2 benchmark, race admission, Course as a game activity, matched CAMERA solver comparison, persistent run Save/Load or formal G1/G2. None of these is silently labelled PASS. Formal production merge remains gated by independent B01-60/B01-40/B01-00 acceptance.

Fresh whole-branch read-only code review found no actionable defects in production adapters, canonical authority, IDs, entry, dormant hooks or packaging. Follow-up review confirmed both Editor fixture corrections preserve or strengthen their behavioral assertions. [Review](REVIEW.md). It did not execute tests and is not independent B01 gate approval.

## Delivery and rollback

**One final ZIP**,38,523,382bytes (36.74MiB):

`C:\Users\kubap\Desktop\Prototypy\Releases\B01_UNIFIED_RC2_20261026\RoboRougelike_B01_UNIFIED_RC2_Windows_x64.zip`

SHA-256: **`7d35cd1bc823499a8855258e24ed0a0ff1d47e49bd694adb94c031ef8c946de6`**.

Actual verified extracted EXE:

`C:\Users\kubap\Desktop\Prototypy\Releases\B01_UNIFIED_RC2_20261026\RoboRougelike_B01_UNIFIED_RC2_Windows_x64-VerifiedExtract\RoboRougelike.exe`

[Full file hashes and extraction receipt](release-receipt.json), [build provenance](build-provenance.json), [one user instruction](START_HERE.md). ZIP includes EXE, Data, MonoBleedingEdge, UnityPlayer, crash handler and all built dependencies plus START_HERE.txt. Every extracted byte and CRC were verified; the exact extracted EXE actually ran. This local location is accessible to the user; no remote binary publication is claimed.

**Rollback:** preserve current changes; launch the historical RC1 Final MAPGEN build, or create a new isolated checkout at `5c29e8e28e7b34fedf0c992fca49451828b02314` (game86eb7bb). Do not reset any active/dirty worktree. [Rollback proof](rollback-proof.json) rehashes both historical RC1 archives and confirms original hashes unchanged. No main/develop merge or shared branch modification occurred.

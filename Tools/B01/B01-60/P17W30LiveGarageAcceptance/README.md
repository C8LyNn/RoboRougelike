# B01-60 | PASS17 W30 | Real R3 Garage dynamic acceptance

PACKAGE: B01-60-P17W30-LIVE-GARAGE-G1-001/R1. Exact current Codex cumulative game parent PR #877 @ a0de194fd382830fab4b7d013d204b0697b25078 on PR #873 @ d537b7310979b91df27a08b14d0f1c74d0889089. Existing game/garage/EMP/JumpPad/Save and Codex active worktree are NOT modified. New files are strictly eight ADD-only B01-60 QA paths including one fresh, unique Unity .meta GUID.

## What actual gameplay source now covers

Two new genuine Unity [UnityTest] native regression sources in Assets/Tests/Editor/W30B0160GarageDynamicGateTests.cs. Both open the existing scene 1, actual Standard R3 vehicle and real sandbox Garage and real R3GarageView/Model/BuildValidation.

1. Dynamically owned obstacle AFTER an initially GREEN 3D saw ghost: the original owned saw part is selected at valid exterior grid cell (0,1,4), with 0.55m grid. A REAL Unity BoxCollider is attached under the actual vehicle, moved from far away into the physical ghost "Środek piły" disc after the user chose the location. Physics.SyncTransforms is used. Original #877 SAME R3 model installationGate must turn its live preview invalid (physical purple hard restriction). Three actual ClickTarget attempts must deny mount with unchanged Scrap/parts/unique original inventory ID. After disabling/deleting the obstacle, actual UI preview and commit must return to valid and mount the SAME part without cost. No parallel validator, fake physics, or forged success.
2. FOUR 90-degree Y turns through real garage RotateSelected: check preview live validity and error against SAME OwnedPlacementError for each orientation; whenever invalid, actual UI rejection preserves Scrap, installed part list and inventory IDs. A full 360-degree rotation returns to legal exterior mount and same ID commits.

These are source-authored native Unity tests, not executed PhysX proofs. Actual #877 current-pose hard zone is NOT complete swept yaw/pitch collision or full five-zone Player acceptance; colored informative zones and expensive multi-gun/performance must still be tested.

## Actual selected current-game native acceptance suites (NOT executed in this package)

- EditMode GarageEditFreedomTests: original 13 cases, including #877 canonical physical saw rejection
- EditMode W30B0160GarageDynamicGateTests: two new genuine Unity tests
- EditMode W29LiveArenaJumpPadTests: two real R5 SalvageYard spawn/impulse tests
- EditMode RRLW6EmpPhysicalTests: 12 actual R3 EMP lifecycle cases
- PlayMode RoboRougelike.Mechanics.EMP.Tests.EmpEmitterPhysicsTests: 22 original physics module cases

TOTAL = 51 native NUnit cases. Only actual Unity 6000.3.14f1 Editor.log, NUnit3 XML, process exits from ONE exact game+QA SHA and independent peer QA review can count as Unity G1. Historical #839/#851 passes are for an older producer SHA, not automatically the combined #877 candidate. Existing full Editor/Play suites, Windows Player ZIP, real player, Save cold-start, turret v5, #861 saw physical mount and #834 softlock remain independent and NOT tested by this suite.

## Safe source-only validation, NO Unity

Commands from a clean trusted W30 QA Git checkout:
    python Tools/B01/B01-60/P17W30LiveGarageAcceptance/verify.py
    python -m unittest discover -s Tools/B01/B01-60/P17W30LiveGarageAcceptance -p test_verify.py -v

The real original product blobs are all pinned; checker proves source ancestry #824→#873→#877, 8 ADD-only own paths, true R3 gate/preview/actual Buy source and R5 original physical JumpPad. CI runs 15 source/evidence-negative tests per Windows and Ubuntu. Source PASS DOES NOT prove Unity G1, playability, or Windows Player. CI NEVER starts Editor.

## Windows native owner opt-in

Requires independent B01-40/41/61 exact original #873/#877 + W30 source/GUID G0 acceptance first. Create a new, clean, disposable clone/worktree whose path contains B0160-W30-Disposable (never Codex active worktree or user's normal player Save); locally activated legitimate Unity Hub Editor6000.3.14f1. Source preflight only:
    pwsh -NoProfile -File .\Tools\B01\B01-60\P17W30LiveGarageAcceptance\Invoke-B0160W30Native.ps1 -RepoPath .

Actual real Unity after owner approval:
    pwsh -NoProfile -File .\Tools\B01\B01-60\P17W30LiveGarageAcceptance\Invoke-B0160W30Native.ps1 -RepoPath . -Run -SourceG0Approved -DisposableCheckoutAcknowledged

Script requires exact typed consent B0160 W30 LIVE GARAGE NATIVE. Five actual classes each get their own real Unity Editor process invocation. Local nonce folder receives 5 NUnit XML, 5 Editor.log, 5 process logs and unsigned SHA256 receipt; structural verifier checks 51 correct original cases and exact version/commit. Independent human QA must still inspect logs. No SSH, auto-fetch, auto-upload, admin, background user-PC work, merge, release, or foreign file deletion.

Selected original Saw product for W30 is #861 @ 8321131aac3e175255a357cf5a30b7716ef6e8fd, explicitly separate; do not co-install duplicate B01-60 #863. Selected Save sole writer #826 @ 99fb08619c78b3834bb4c0450519babf991f218a remains not wired in this game branch; no second raw Store. Loaner policy #834 pending owner decision; do not alter V7.

GATES: IMPLEMENTED=YES real Unity native test sources; TESTED=SOURCE only after exact CI; QA_ACCEPTED=NO independent physical G0/G1; READY_FOR_INTEGRATOR=NO; INTEGRATED=NO; RELEASED=NO. G1 native=NOT_RUN, Windows Player=NOT_RUN, G2 HOLD. Rollback discards only the 8 new B01-60 QA paths/branch, preserving original #873/#877 products and all user data. Receiver B01-40/41/61 independent QA, B01-00 coordination and Codex on future authorized game intake.

Latest #877 original W30 explicitly includes a 256-hit fail-closed NonAlloc physical sweep buffer and 260-body source-authored stress within existing GarageEditFreedomTests. Our two new cases complement it with post-preview collider motion, old part-ID/Scrap atomicity, and full yaw rotation. Source is NOT native G1.

The NEW exact #877 current producer HEAD also includes bidirectional ExistingMountedWeaponPlacementIssue for incoming non-gun parts and restoration FunctionalWeaponPlacementIssue before repair Scrap/HP. Source QA pins both actual .cs blobs. Independent real native game/Player must still establish behavior, including failing #883 prior witness; no false PASS from source.
Deconfliction: B01-40 #892 already authors a moving-owned-Rigidbody test, B01-60 #884 reverse-mount scenario and B01-23 #887 game patch exist. Only ONE overlapping moving-collider fixture should be integrated later. The distinct contributions here are live four-yaw UI parity and five-suite 51-case same-head original Unity owner opt-in. Do not offer duplicate #79 G2.

Latest Codex #877 adds two genuine additional in-game native cases in original GarageEditFreedomTests: new Armor against existing Saw and destroyed legacy Saw repair/emergency restoration. Full native reception now requires THIRTEEN original garage cases, total FIFTY-ONE across five suites on the same immutable source; native not executed by GitHub CI.

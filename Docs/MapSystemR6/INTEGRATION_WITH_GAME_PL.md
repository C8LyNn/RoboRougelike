# MAP-R6 — izolowana integracja z aktualnym RoboRougelike

Status: **SOURCE INTEGRATED ON ISOLATED BRANCH; UNITY QA NOT RUN**.
Base: `develop` `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`.
Branch: `feature/map-r6-parallel-sandbox`.
Do not merge without real Unity compilation and gameplay tests.

## What was imported
Complete MAP-R6 Unity runtime, samples, existing Editor demo, EditMode/PlayMode tests, matching .meta/GUID files, latest R6 documentation and offline Python scripts. This package supersedes earlier MAP R4/R5 and includes R6 planners, geometry and passive drive evidence. Historical R3-only reference remains in the separately distributed source ZIP, not in active Unity Assets.

All archive files pass SHA-256 manifest verification. Git imported objects were checked against 104 expected Git blob SHAs, zero mismatches. Git tree path conflicts with base: zero.

## Two small compatibility updates
1. `MapR4PrototypeDriver.cs`: demo rover now reads WASD/arrows through new Input System when enabled; previously read zero input with `activeInputHandler: 1`. Legacy fallback remains.
2. `RoboRougelike.MapSystemR4.Runtime.asmdef`: explicit reference to `Unity.InputSystem`, already present in the project manifest.

## Opt-in hook to the actual robot
`Assets/R6/MapR6ProductionEvidenceBridge.cs` is passive telemetry only: attaches MAP-R6 `MapR6DriveEvidenceRecorder` to the real robot Rigidbody and records displacement, tilt, suspected stuck intervals and driver demand.

**To activate on an existing running game without editing the main scene**:
- Start Unity Play and enter actual `Combat` or `TestDrive`.
- Use `Tools > RoboRougelike > MAP R6 > Attach Recorder To Live Player`.
- In the player Inspector, use `MapR6DriveEvidenceRecorder` context menu `MAP R6 / Export evidence JSON manually`. JSON saved in `Application.persistentDataPath/MapR6Evidence`.
- For more accurate metrics, pass actual resolved controller throttle through `SetExternalDemand01`; current keyboard fallback cannot infer gamepad/AI demand.
- The bridge does NOT spawn roads, modify garage/camera, drive the robot or claim that a course was completed.

## Separate map sandbox
- Make a NEW empty Unity scene in a disposable workspace; do not save the original `Assets/scene 1.unity`.
- Use `Tools > RoboRougelike > MAP R6 > Create Demo In Current Scene`, press Play.
- Run map EditMode & PlayMode tests. Compare actual vehicle geometry with R6 reference plans. R6's geometric turn candidates and Gizmos do not prove real driving.

## Explicit Codex follow-up responsibilities
- Unity compilation and new Input System rover check; any asmdef errors -> fix in this branch only.
- Run all MAP R4/R5/R6 EditMode and PlayMode tests with logs and exit codes.
- Add robust optional actual player controller demand signal. Validate Gauntlet input, no false positive while paused, gamepad, collisions and position samples.
- Finish production map mode integration only after verifying road mesh seams and real Rigidbody manoeuvres; do not silently replace `R5Arena` or `R6TestRange`.
- Full MOV001–009 baseline regressions, editor scene isolation, Windows build.
- Report PASS/FAIL/NOT RUN, exact commit SHA; no automatic merge.

## Parallel ownership
This PR owns only new `Assets/RoboRougelike/MapSystemR4/`, `Assets/R6/MapR6ProductionEvidenceBridge.cs`, `Assets/Editor/MapR6AttachRealVehicleEvidence.cs`, `Docs/MapSystemR6/`, `Tools/MapSystemR6/`.
Codex's current PR #1 owns rewards, repair and combat/debris: R1RunController, R1RunProgress, R3VehicleModel, R4RunHUD, R4SalvageLedger, R1EncounterManager, R6DebrisRelease and P12_* files. Do not edit those from this branch.

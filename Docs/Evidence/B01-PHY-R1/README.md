# B01 PHY R1 evidence

Gameplay/build source: `523046f5bf6ddab0caf62253615c962e808c3053`; base: `865932fd78ef936493bdc51ea9ca612fe9ec31eb`.

Final Editor-fixture checkpoint: `54f0192b89ffac9f6989da36e8f5d893e631d600`; runtime Assets are unchanged. The full EditMode run was992/999/native2. All five corrected classes then passed28/28/native0; `editmode-coverage-reconciliation.json` identifies every superseded failure and999 unique current cases covered/0 unresolved. This is not a fresh full green Unity run. Full PlayMode passed398/398/native0. Both raw runs remain inspectable.

`index.json` lists actual NUnit counts, native process records, local raw log paths and SHA256. `unity-results.zip` preserves the XML/process files from real RED/GREEN runs. Intermediate TDD runs are explicitly not immutable release builds. Do not sum their counts as unique tests. Full final XML is the acceptance count.

The native directories preserve JSON, process exits/hashes, selected original PNGs and bounded log excerpts. Original full logs remain under the recorded `Artifacts/Local` paths on the owner's machine. The preliminary physics run and interrupted first UI session are retained as incomplete/terminated results, never PASS.

`package.json` is the real package/CRC/extracted-file hash result. `human-close.json` records the owner's current retail Alt+F4 answer; `B01-extracted-human-close/process.json` independently records its actual native exit0 and DLL/EXE hashes. The operator smoke process instead exited C0000005 after automation-attached Alt+F4, despite its gameplay checks completing.

## Reproduce

Use licensed Unity6000.3.14f1/Windows Build Support and close this checkout's Editor. Run from the repository root in the normal user environment (sandbox licensing IPC is unavailable here):

```powershell
./Tools/CI/Run-Unity.ps1 -Task EditMode
./Tools/CI/Run-Unity.ps1 -Task PlayMode
./Tools/CI/Run-Unity.ps1 -Task Build
```

Targeted real physics classes are `B01GroundedDriveTests`, `B01CoreRecoveryTests`, `B01EnemyGroundingTests`, `R6DrivingTests`, `RRLW8SteeringTests`, `MOV009PhysicsAcceptanceTests` and `RRLW6EmpPhysicalTests`. The usual Unity `-runTests -testPlatform EditMode -testFilter 'Class;Class' -testResults PATH` selects them.

The QA build under `Builds/MOVSeries-QA-Windows` accepts explicit fixtures:

```text
-b01-phy-native-output ABSOLUTE_NEW_DIRECTORY
-rrl-w8-native-api-output ABSOLUTE_NEW_DIRECTORY
```

These are controlled API/physics fixtures, not manual gameplay. Run one at a time with `-screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11 -logFile ABSOLUTE_LOG`. Preserve the process handle/actual native exit. Do not attach UI Automation during the no-client shutdown control. Inspect screenshots and result JSON; launch success alone is insufficient.

For operator smoke the existing QA-only flags are `-rrl-w2-qa-output DIR -rrl-w2-run-id ID -rrl-w5-diagnostics-output DIR`. Menus, rewards and garage use the actual UI; combat uses ordinary virtual input, real projectiles and active AI. It does not grant HP/currency/rewards. Its combat camera is controlled by the input pilot. The ordinary range/garage camera was inspected separately.

For retail extract the entire ZIP and retain Data/Mono/DLL files beside the EXE. `release-manifest.json` identifies the tested source and all runtime-file hashes; the Unity EXE stub hash alone cannot identify gameplay. No merge or next iteration is authorized by these evidence files.

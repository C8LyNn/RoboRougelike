# B01-30 | PASS17 WAVE34 — independent actual R41 v5 fixed-rig receiver

**EXACT authoritative base:** B01-31 [Draft #922 @ `b61bbe0643a6547ccb3087ada44474e8b33550fd`](https://github.com/C8LyNn/RoboRougelike/pull/922), continuation of #914 → #904 → #893; not the original game integrator. **Original B01-30 prior QA** [Draft #909 @ `f8517d85549cdfbd2049a33d1881c843a68eee76`](https://github.com/C8LyNn/RoboRougelike/pull/909), remains immutable historical source-RED witness of #904.

## Delivery

- **NO duplicated producer code:** original B01-31 #922 Editor C#, CLAUDE external FBX, old AutomaticGun, R4 weapons, #873/#877, garage, Save, economy, main/develop all remain byte-identical. 10 ADD-only B01-30 QA paths only.
- **Port existing B01-30 native QA verbatim:** exact 7 Git-file blobs from #909, including 2 NUnit tests which require the ORIGINAL producer SHA-pinned `R41_Turret_v5.fbx`. No synthetic cube test posing as original FBX. Missing model leads to NUnit INCONCLUSIVE (NOT native PASS). The old #909 PR itself stays unchanged; this is the first QA receiver actually stacked on corrected #922.
- **Original independent source checker:** confirms exact SHA Git ancestry and 10 paths, unchanged current source blob, pre-Instantiate fail closed hierarchy, separated fixed/yaw/pitch render groups, cloned original yaw/pitch reparent, fixed base world-pose check, unchanged original SHA lock, original test-byte equality and 5 GUID unique among Assets. Eight source-negative mutants must fail. Runs GitHub Actions Ubuntu24/Windows2022. Source PASS does not mean Unity 6000.3.14f1 G1.

## Source risk newly identified

The source FBX input path was correctly made project-absolute in #904, but **candidate output collision guards still call `File.Exists(CandidatePath)` and `File.Exists(CandidatePath+".meta")` using relative paths** within `BuildFromSelectionWithExplicitConsent`. Their behavior can depend on the Editor process working directory; if `AssetDatabase.LoadAssetAtPath` yields null for an existing-but-unimported/invalid output file, output collision may go undetected. This is a **source-level risk** requiring producer-owned fail-closed correction and dedicated native negative test using a disposable project and altered current directory. It is **NOT** proof that overwrite occurred. No output file is created by this QA PR.

## Gates

`SOURCE_G0=CONDITIONAL_AFTER_EXACT_CI`; `ORIGINAL_W30_FULL_FBX_PITCH_SOURCE_BUG=FIXED`; `OUTPUT_PATH_CWD_RISK=P1_OPEN`; `NATIVE_G1=NOT_RUN`; `EXTERNAL_FBXSHA=fc83d677744d7e60b5f5f5f3ad3d9e72c97f84b66bbacf142c14959becfa5533_NOT_AVAILABLE`; `ACTUAL_STATIC_BASE_MESH/DUAL_MUZZLE_SHOTS=NOT_RUN`; `WIN64_PLAYER=NOT_RUN`; `GAME_PREFAB=HOLD`; `INTEGRATED=NO`; `RELEASED=NO`.

B01-31 owns correcting relative candidate output guard, plus actual imported rig/mesh compatibility; B01-60 owns Unity full-native tests (original W30 4, W31 4, W33 4, B01-30 2) and real installed R3/R4/Player. Codex/B01-00 alone may authorize canonical gun firing-origin change (old single `Wylot` +Z 1.8m vs v5 dual +Z 0.7m). Keep old AutomaticGun fallback.

Rollback: remove only 10 new B01-30 QA paths from an approved isolated branch. No merge/release.

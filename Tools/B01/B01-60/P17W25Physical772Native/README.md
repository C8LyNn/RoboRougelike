# B01-60 / PASS17 WAVE25 — physical Rig #772 genuine Unity Windows G1 staging

**PACKAGE_ID** `B01-60-P17W25-PHYSICAL772-UNITYG1-001/R1` | HQ [#731](https://github.com/C8LyNn/RoboRougelike/issues/731) | user authorization [#667](https://github.com/C8LyNn/RoboRougelike/issues/667). This new QA-only isolated receiver **reuses** frozen B01-60 original W22 source staging, SHA-pinned Unity original source, original W12 strict ZIP stage/rollback, private `Application.persistentDataPath` NUnit guard and original Unity Editor.log/XML test runner. It does NOT replace existing G1 suite: [W24 B01-60 #760](https://github.com/C8LyNn/RoboRougelike/pull/760) already covers **all 7 original #750 R5 g4→g5** cases; [#751/#757](https://github.com/C8LyNn/RoboRougelike/pull/757) cover different owner-selected #732 source version. Do not coinstall/mix #732 and #750 corrected same-test source.

## New real physical test target

Pinned **original #772 @ `ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1`** (21 original ADD-only owner files, nine real GUID, two original asmdefs), descendant solely of selected original V3 API **#729 @ `b8ca29c7707cd6b4338d0b9772d74dee676d49d2`**. Frozen original **#510 AtomicDockLedger** and #556 codec must be unchanged, no private reflection. Three ORIGINAL authored Unity6 PlayMode tests, source file Git blob `6827351e8f578030de932b2d36a5c0c55337a2f7`:
1. `Original510AtomicLedger_RealRigidbodyDetachedAndAttachedParentAndVelocity`
2. `InvalidOriginal556Binary_CannotCreateAnyUnityBodyOrConsumeId`
3. `QuiescenceRequired_BeforeRealPhysicalStage`

Each is independently staged in a NEW random `B22-*` directory outside repo, private unique company/product Unity PlayerSettings and checkpoint guard, source exact Git blob/Unity GUID provenance; optional operator-run uses real `Unity.exe -batchmode -runTests -testPlatform PlayMode` with exact filtered NUnit identity, exit0, original test-run XML/Editor.log version 6000.3.14f1 and source integrity after process. No fake 3/3 until actual owner Windows evidence. Native real source code still has **independent G0 pending** and is NOT installed over the real old #510 private live `Rig[]`; original #772 authors a replacement Unity physical owner, not a binding to the existing old private component.

## Canonical Save G1 hard stop

Selected original Save lineage **#749 @ `8634e71dfcf633323073fe584a6c0884f1ca5d23`** has a genuine [public raw-store multi-directory API vulnerability #767](https://github.com/C8LyNn/RoboRougelike/pull/767). Proposed **only** descendant [#773 @ `3aaaec4027ee246fbfb6f209179f0e9507363473`](https://github.com/C8LyNn/RoboRougelike/pull/773) repairs this in NEW Tools/.NET source. **#773 still adds zero Unity `Assets`/asmdef/MonoBehaviour session binding.** This exact source dependency is checked in preflight, but cannot be called a genuine Unity Save test. Do NOT source-overlay #773 over raw parent #749, or import alternate #753/#741, or claim actual private old #510 `Rig[]` SaveLoad. Only B01-00/23/62 may explicitly choose #773 after independent B01-41/61 and B01-40 positive original G0 and new approved Unity-facing bridge. `FULL_CANONICAL_SAVE_G1=NOT_RUN`, `G2=HOLD`.

## Manual user-local Windows operation

From reviewed W25 QA branch checkout; real Python3, Git, original exact #772/#749/#773 historical commits already in LOCAL Git history; legitimate Unity Hub Editor `6000.3.14f1` if native execution desired. No automatic fetching on operator PC.

```powershell
# Safe physical staging/rollback only; no Unity:
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W25Physical772Native\Invoke-B0160W25Port772.ps1 -RepoPath .

# ONLY AFTER independent new #772 SOURCE_G0 and deliberate owner opt-in:
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W25Physical772Native\Invoke-B0160W25Port772.ps1 -RepoPath . -Run -G0Reviewed
```

Second command additionally requires typing EXACT `B0160 PORT772 THREE REAL UNITY G0 REVIEWED` after locally verifying Editor SHA256 and license. `-G0Reviewed` is only **operator acknowledgement**, not independent G0 certification. Owner may set `-UnityPath 'D:\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe'`. Original runner preserves new private evidence directory, SHA256, native Editor.log/NUnit XML, no upload, admin, SSH, foreign-directory cleanup, game working-tree modifications or merge. Do NOT execute until actual G0.

## Source CI / evidence acceptance

```bash
python -m unittest discover -s Tools/B01/B01-60/P17W25Physical772Native -p test_port772_native.py -v
```

Original Git SHA, 16 directly executable source/scratch/negative guard checks (Windows2025/Ubuntu24.04), real PowerShell source-only plus expected negative wrong scratch/not-G0/missing-editor, selected #773 source lineage and absent Unity binding. Synthetic/project preflight cannot show actual Unity renderer/physics. Native PlayMode real PASS only when real owner PC **Editor.log + original NUnit XML + process return + source SHA** are reviewed independently, not on a GitHub Actions image without Unity.

**Gates:** `IMPLEMENTED=YES` | `TESTED=INITIAL_CI_PENDING` | `QA_ACCEPTED=NO_PENDING` | `READY_FOR_INTEGRATOR=NO` | `INTEGRATED=NO` | `RELEASED=NO`; `SOURCE_G0_772=PENDING_INDEPENDENT` | `UNITY_PHYSICS_G1=NOT_RUN` | `SELECTED_SAVE_749_773_G1=NOT_RUN_NO_UNITY_BINDING` | `FULL_GAME_510_SAVE=HOLD` | `G2=HOLD` | `CODEX_ACK=NONE` | feedback `NOT_MEASURED`. **Receiver B01-40/41/61** original #772 physical+GUID source and #773 Save safety; **B01-00/23/62** sole selected Save successor and canonical Unity session binding; **B01-60** owner-operable genuine local Unity test logs. Rollback discard only this new QA Draft branch/files.

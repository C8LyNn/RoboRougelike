# CAMERA-001 R1 — native validation, source compilation failure

Dispatch HQ-B01-CODEX-W9-20261025. Target OBJ001 PR210 `4c9236a4cdc27814ea2e7679872ec4a524150cc8`; isolated `qa/rrl-w9-b01-native`, source checkpoint `ee0aa45` (full SHA in process record). Source PR134 `894d3488dedb69e2b8d89683d750bbdbfedf4e55` imported byte-for-byte, including original asmdefs/metas. No production camera/R3/R6/P12/scene1 edits. Original checkout remains clean/frozen.

## Actual result
- Source integrity:35 manifest entries match original Git blob IDs/lengths and checkout bytes; SHA256 computed separately in CAMERA001-source-audit.json. No target GUID collisions. This is local integrity evidence, not independent B01-40 approval.
- **FAIL — Unity6000.3.14f1 script/assembly compilation**, actual retained process exit1, run CAMERA001-original-20261025T091404Z. No NUnit XML generated.
- **NOT RUN — EditMode5 test methods (including840 authored C# geometry proposals), PlayMode3 tests, native demo and720/1080 visual matrix.** None reached execution. The prior JS840 surrogate is not native evidence.
- Independent B01-60 G1 remains unconfirmed; G2 HOLD. Local failure does not promote any approval. No host integration or release.

## Minimal reproduction
1. Clean separate checkout of target4c9236a, licensed Unity6000.3.14f1 and unchanged project packages.
2. `git restore --source=894d3488dedb69e2b8d89683d750bbdbfedf4e55 -- Assets/B01Packages.meta Assets/B01Packages/CAMERA001.meta Assets/B01Packages/CAMERA001`.
3. Run `Tools/B01Validation/Run-PackageTests.ps1 -Filter B01.CAMERA001.Tests -Mode EditMode`.
4. Editor log reports twice for each test asmdef:

```text
Assembly has duplicate references: UnityEngine.TestRunner,UnityEditor.TestRunner
Scripts have compiler errors.
Exiting without the bug reporter. Application will terminate with return code 1
```

Both `Tests/EditMode/B01.CAMERA001.EditModeTests.asmdef` and `Tests/PlayMode/B01.CAMERA001.PlayModeTests.asmdef` explicitly list the two runner assemblies while also enabling `optionalUnityReferences: [TestAssemblies]`, which supplies those references on this Unity/test-framework combination. The base's working package tests use the automatic TestAssemblies route without duplicating the runners.

Producer action: B01-20 choose one test-reference mechanism, publish a new immutable source revision, then B01-60 rerun actual compile/Edit/Play and visual/physical matrix. This receiver did not edit the original package or claim the suggested adjustment passes. G2 and B01-00 target-scope approval remain required before live camera integration. Tall/wide/asymmetric actual rigs, smoothing/reticle correctness, world walls and dual-camera ownership are still unverified.

## Deconfliction
All170 open PRs and changed paths were inventoried with no unresolved remote branches. Existing #125 and earlier feature PRs account for inherited R3/R6 changes; #55 independently touches R3VehicleAssembly/R3GarageView and must be coordinated before future BUILD integration. No active PR diff currently changes P12_PipelineSafeCamera.cs. This QA scope reserves only the package capsule, Tools/B01Validation and Docs/B01Validation; future host edits are not started or authorized. No other worktree was modified.

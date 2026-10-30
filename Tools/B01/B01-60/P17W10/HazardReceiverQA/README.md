# B01-60 — PASS17/WAVE10 — Formal physical source-delivery receiver for HAZARDPULSE R2 #470

**Immutable original owner**: [B01-25 Draft #470](https://github.com/C8LyNn/RoboRougelike/pull/470) @ `ebc3d6162f4516147092960291e50e15fe6c4582`; original manifest blob `cacc292d6fc15587c604cac2ac23cb0b9c8c6d48`; freeze R1 [#430](https://github.com/C8LyNn/RoboRougelike/pull/430) @ `6095577ece27320b1a861a738cc23ba42d6793a6` remains RED. Already executed independent original R1 RED→R2 GREEN [#474](https://github.com/C8LyNn/RoboRougelike/pull/474), exact frozen #474 SHA `2538f60093d3e8732e3c7865a10ef81ac3a340d7`, [four cross-OS jobs #41054435212](https://github.com/C8LyNn/RoboRougelike/actions/runs/41054435212) success. This W10 package **does not duplicate C# frame-timing tests**: it independently performs the **previously missing physical source-delivery G0**.

## Real executable physical receiver

From a branch stacked on original owner #470, run:

```sh
python Tools/B01/B01-60/P17W10/HazardReceiverQA/receiver.py
```

Checks owner-only exact 11-file producer delta from immutable R1, validates the real original manifest's **10/10 pinned raw Git blobs**, requires all 11 original versioned files preserved byte-for-byte on QA branch, and builds from **original Git object bytes** a cross-OS canonical uncompressed ZIP with independently generated receipt containing original Git blob hashes, per-file byte counts and SHA256. Includes producer's own workflow and manifest; exactly 12 archive members, and independently verifies entire ZIP byte hash. Runs safe no-overwrite stage and strict rollback in an **external temporary receiver**, requiring exact bytes before deleting only owned entries. Runs **14 actual adversarial rejected corruption/status/rollback** scenarios (tampered original source, forged manifest/Git blob/status, unexpected Unity game file, traversal, missing source, mutating staged data and unrelated user file). The CI jobs preserve original inner ZIP + actual JSON verdict. This is not a theoretical install plan: GitHub Actions executes it in Python3.11 on both Windows and Ubuntu.

## Explicit producer G0 limitation

Original R2 is **source-only Research C#9**, not Unity assets. It contains **ZERO authored `Assets/**` Unity assets, `.meta` GUIDs, or `.asmdef`**. Therefore `G0_PHYSICAL_SOURCE_DELIVERY=PASS` may be given after original ZIP verification, but `G0_UNITY_IMPORT_PACKAGE=BLOCKED` (no Unity-ready loader/assembly/adapter); **do not claim** engine-native Unity, an installable prefab, collision safety or actual damage to HP. The source can be considered only as a future Codex-owned integration offer if HQ authorizes; Codex must own adapter/scene/HP integration. **Unity G1 NOT_RUN, G2 HOLD, CODEX_ACK PENDING, INTEGRATED NO, NO MERGE**. B01-40 owns formal final cross-package source/Unity-receiver gate; B01-00 alone may queue #79.

This intentionally does not touch the old #430 producer SHA, #474 QA SHA, or canonical Codex game code. New changes are exactly three B01-60 QA files and own workflow; rollback is to remove these four own paths/branch.

## Current open reattach safety blocker (outside this archive)

Separate read-only [#472](https://github.com/C8LyNn/RoboRougelike/pull/472) + [#476](https://github.com/C8LyNn/RoboRougelike/pull/476) confirm legacy public W07 `DebrisRigPlayground.Reattach(int)` can bypass W08 collision checks. New owner [#471](https://github.com/C8LyNn/RoboRougelike/pull/471) shields its opt-in demo GUI only; original public W07 method remains callable from any host script. Until B01-23 fixes **all external public call entrypoints** under new exact source revision and native licensed Unity PhysX is tested, global `SAFE_REATTACH_G0=HOLD` irrespective of #471 pure source policy tests. B01-60 will not override original producer ownership.

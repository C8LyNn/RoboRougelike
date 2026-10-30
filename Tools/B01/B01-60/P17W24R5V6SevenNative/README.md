# B01-60 — P17 W24 corrected R5 V6 SEVEN native Unity cases

**PACKAGE:** B01-60-P17W24-R5V6-SEVEN-NATIVE/R1. **Authorization:** #667; HQ #731. Reuses immutable source-scoped selected R5 #690, versioned NEW owner-authored ColdRestart fix [#750](https://github.com/C8LyNn/RoboRougelike/pull/750), and existing B01-60 manual runner [#718](https://github.com/C8LyNn/RoboRougelike/pull/718) under [#735](https://github.com/C8LyNn/RoboRougelike/pull/735). New B01-60 branch descendant of #735, new files only. NO frozen source edits, alternative R5 #693, live game project writes or merge.

## Why this is no longer the six-case runner

Original #690 ColdRestart fixture wrongly expected generation 4 after genuine original #556 binary Save advanced 4→5. B01-23 authored an isolated **fixture-only V6 #750 @ 60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb**, with actual SnapshotCodec decode, saved/restored generation 5, same consumed Light/Heavy IDs. W24 selects the **entire exact original author V6 Unity source closure** via original Git objects and verifies only original fixture changed versus R5. It **reuses** the existing hardened B01-60 #718 source packaging/staging, per-case private PlayerSettings guard, original test runner, exact test identity, Editor.log/exit/XML checks. Does NOT copy alternative implementation or fix an owner fixture inside a QA runner.

Preflight now stages and strictly rolls back **SEVEN independent original V6 private Unity projects**, each with its own QA-only persistentDataPath probe; optional REAL Unity run calls guard plus original PlayMode test for all **7/7** authored R5 V6 cases (14 independent -runTests process invocations). A green source preflight NEVER makes G1 PASS. A genuinely passed seven-case native receipt requires separate independent raw inspection/attestation and G0; it still doesn't validate actual frozen #510 private Rig[]/AtomicDockLedger nor V3 #729 + selected sole #749 Save bridge.

## Windows operator one-command (manual on owner's PC only)

Prerequisites: trusted local checkout of W24 Draft branch, Git and Python 3, PowerShell7 (or Windows PowerShell5.1), exact ORIGINAL Git commit object for #750 already present locally, original #690 reachable; legally installed and activated Unity Hub Editor **6000.3.14f1**. There is **NO automatic network fetch**, SSH, credentials, admin, services, Git protected writes, auto-upload, auto-delete or foreign scratch cleanup.

Safe SOURCE-only preflight (no Unity process):

~~~powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W24R5V6SevenNative\Invoke-B0160W24R5V6.ps1 -RepoPath .
~~~

Only after B01-40/41 independently approve the exact #750 source-G0, and the owner checks Unity activation, manual operator can opt in:

~~~powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W24R5V6SevenNative\Invoke-B0160W24R5V6.ps1 -RepoPath . -Run -SourceG0Approved
~~~

The script prompts for literal operator consent **B0160 R5V6 SEVEN NATIVE** before invoking any Unity. If the Editor is installed elsewhere, supply -UnityPath with the exact activated 6000.3.14f1 Editor/Unity.exe path. Without independently accepted G0, do NOT set -SourceG0Approved; don't run untrusted PR code on the PC. No attempt is made to bypass Unity licensing.

Each run creates a fresh GUID/nonce private scratch directory OUTSIDE the original checkout and unique Unity product for every case; original R5 authored source and test assembly GUIDs are pinned. Missing historical #750 Git object, wrong Editor, unsafe scratch or missing consent => FAIL_CLOSED. All evidence remains **local** (P24-receipt.json, SHA256 inventory, 14 original NUnit XML/Editor.log/process log sets for native); redact private paths before any user-authorized sharing. SHA256 is self-consistency, not cryptographic identity proof.

## Executable source QA, no Unity

~~~powershell
python -m unittest discover -s Tools\B01\B01-60\P17W24R5V6SevenNative -p test_r5v6_native.py -v
~~~

Dual OS CI source QA checks source pins, 7 authored PlayMode names, original binary generation correction, no owner product modifications, seven real disposable staged+rolled-back projects, wrong consent/G0 native refusal. Windows hosted PowerShell preflight and missing-Editor negative executed with **no Unity**. Any test count in CI is **SOURCE ONLY**, never fake G1.

**Sole canonical Save:** B01-62 selected candidate #749 (new .owner lock), #753/#741 remain separate alternatives, not included/co-installed; actual #510 Rig[]/ledger adapter and Unity Save native integration remain HOLD. Owner-approved economy/loaner policy is independent design, not tested as integrated game by this R5 suite. Full game G0/G1/G2 not implied.

**Six states:** IMPLEMENTED / TESTED / QA_ACCEPTED / READY_FOR_INTEGRATOR / INTEGRATED / RELEASED reported separately in #731. Receiver B01-41/61 independent fixture+native raw QA; B01-40 physical source G0; B01-00 manual operator handoff. Safe rollback: drop only own W24 additions/branch without touching #750, #735, player saves or foreign scratch. No merge.

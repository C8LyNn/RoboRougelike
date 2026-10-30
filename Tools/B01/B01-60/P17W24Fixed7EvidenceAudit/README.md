# B01-60 · PASS17 / WAVE24 · Corrected R5 seven-PlayMode evidence audit

PACKAGE_ID: `B01-60-P17W24-SELECTED732-FIXED7-EVIDENCE-001`. Main HQ [#731](https://github.com/C8LyNn/RoboRougelike/issues/731), owner-approved LOCAL Windows native operator [#667](https://github.com/C8LyNn/RoboRougelike/issues/667). This is an **additional read-only independent original seven-case artifact verification gate**, NOT a new Unity runner, not game installation, not native Unity execution and NOT cryptographic proof of execution. Original **B01-60 W23 seven-case operator** [Draft #751 @ `f0ab4b7f64d2f9e09cd4bd79def729b27829d913`](https://github.com/C8LyNn/RoboRougelike/pull/751) stays byte-identical. The earlier W22 six-case R5 evidence QA [#734](https://github.com/C8LyNn/RoboRougelike/pull/734)/[#735](https://github.com/C8LyNn/RoboRougelike/pull/735) is not a substitute for seven-case acceptance.

## Exact selected source / concurrent version policy

The only R5 runtime host is immutable original [#690 @ `cc7f73541517a18f4479bb155534bd4832d9985e`](https://github.com/C8LyNn/RoboRougelike/pull/690). W23 owner-selected corrected seven-case R5 fixture [#732 @ `0bee4ce9928354433ec719c45a8186378361d46e`](https://github.com/C8LyNn/RoboRougelike/pull/732) retains all seven exact original Unity method names, modifies the ColdRestart source to g4→g5, and has independent SOURCE transport G0 [B01-40 #755](https://github.com/C8LyNn/RoboRougelike/pull/755) at EXACT #732 SHA. This W24 independent artifact auditor pins that **corrected** fixture exact Git blob `d096f65eeb37393204a35445dd87cbdffa642ce0` and **unchanged R5 runtime/codec** source blobs in every native project's `Assets` tree, not just an untrusted receipt value.

Another B01-23 fixture [#750 @ `60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb`](https://github.com/C8LyNn/RoboRougelike/pull/750) implements similar g4→g5 semantics but is a **DIFFERENT SHA/file blob `2508a04f57b04447112d2777ce4e3cd7478ed40e`**. Its author source tests cannot inherit independent G0 of #732 and it MUST NOT be silently installed in the same seven-case receiver. Receiver B01-00/23 must explicitly declare the one selected fixture. A new versioned B01-60 harness will be necessary if #750 is selected; the current #751+W24 original selected #732 bundle remains valid only for #732.

Save/Load selected candidate is [#749](https://github.com/C8LyNn/RoboRougelike/pull/749) pending final B01-00 decision; mutually exclusive #753/#741 must not be mixed. **This package tests ONLY isolated R5 game-host seven-case native evidence, NOT V3 #729, #749, live private game #510 Rig[]/AtomicDockLedger physics Save/Load, economics, anti-softlock or G2.**

## Exact owner-side commands

Safe source-only from the reviewed checked-out W24 QA branch (with original owner #732 exact Git SHA in local history), Python3 and Git installed:

```powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W23FixedR5SevenNative\Invoke-B0160W23Fixed7.ps1 -RepoPath .
```

After independent selected #732 source G0 review (not an automatic code action), legitimate owner Unity Hub **6000.3.14f1** installed and activated, opt into actual local native PhysX+Unity PlayMode seven-case execution via the **unchanged W23 original runner**, with G0 acknowledgement and precise typed consent:

```powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W23FixedR5SevenNative\Invoke-B0160W23Fixed7.ps1 -RepoPath . -Run -SourceG0Reviewed
```

Typed phrase `B0160 R5 FIXED7 G0 REVIEWED NATIVE`. Original operator outputs exactly one private `B0160_W23_FIXED7_LOCAL_EVIDENCE_NO_UPLOAD=<full path>` folder and preserves files locally. Then run the NEW **read-only** independent verifier against that EXISTING folder:

```powershell
python .\Tools\B01\B01-60\P17W24Fixed7EvidenceAudit\audit_fixed7.py '<B22-folder-printed-by-operator>'
```

Verifier refuses fake `G1 PASS`/G2, missing SHA256 or unapproved source version, incomplete 7 distinct private Unity products, unreviewed G0 native claim, failed/incomplete/malformed/skipped/duplicated NUnit3 tests, extra logs, wrong Editor 6000.3.14f1, process exit nonzero, original source byte mutation and symlinks/foreign files. Exact cases: **7 original R5 physical tests + 7 unique private scope-guard tests = 14 separated `NUnit3 XML` + 14 `Editor.log` + 14 `process.log`**. If they are *structurally* consistent, verdict is `STRUCTURALLY_CONSISTENT_7_OF_7_ORIGINAL_R5`, `native_G1=UNATTESTED_FILES_ONLY` and `full_R5_G1=HOLD_PENDING_INDEPENDENT_REVIEW`. The verifier **CANNOT prove Unity.exe actually ran**: unsigned SHA256 evidence can be fabricated by someone who controls the directory. Genuine human operator confirmation and separate original log/source/consistency review remain mandatory. No automatic upload and do not share private user names/paths from logs unredacted.

## Executable checks

```bash
python -m unittest discover -s Tools/B01/B01-60/P17W24Fixed7EvidenceAudit -p test_audit_fixed7.py -v
```

26 source-byte-pinned and **MOCK synthetic** hostile native artifact tests; synthetic NUnit XML / Editor.log fixtures are NOT real Unity. New dedicated GitHub Actions CI Windows2025+Ubuntu24.04 retrieves correct #732 commit into ephemeral CI git, executes test cases, then reuses original W23 #751 real Git source-only seven-Unity-scratch-project stage/rollback and checks its receipt with NEW independent audit. An independent Windows PowerShell job executes existing W23 safe source-only operator and NEW audit; never Unity.

`IMPLEMENTED=YES / TESTED=PENDING_CI / QA_ACCEPTED=NO_PENDING / READY_FOR_INTEGRATOR=NO / INTEGRATED=NO / RELEASED=NO / G0=SELECTED732_755_SOURCE_TRANSPORT_ONLY / ACTUAL_NATIVE_G1=NOT_RUN / CANONICAL_749_RIG_SAVE_G1=NOT_RUN / G2=HOLD / CODEX_ACK=NONE / ACTUAL_INTEGRATOR_FEEDBACK=NOT_MEASURED`.

**Receivers:** B01-40 exact G0 (already source-transport accepted, not gameplay), B01-41/61 independent source security and corrected R5 release QA; B01-23/B01-00 select #732 vs #750; B01-62 #749 candidate but not covered; B01-60 independent review of genuine owner-run local Unity logs. **Rollback:** discard only new W24 B01-60 QA-owned files/branch, never change producer, protected Git refs or user game. No merge.

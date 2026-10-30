# B01-41 — CI41-W20-NATIVE-EVIDENCE-GATE-001/R1

**Owner:** B01-41 / P17 W20 | HQ [#668](https://github.com/C8LyNn/RoboRougelike/issues/668) | authorized scope [#667](https://github.com/C8LyNn/RoboRougelike/issues/667) and [#666](https://github.com/C8LyNn/RoboRougelike/issues/666).

This is a **new independent evidence authentication/preflight policy for the B01-60 user-operated native Windows Unity runner**; it DOES NOT launch Unity, does NOT SSH or operate owner's Windows machine, and does not modify any original game source or user data. Independent native Unity release cannot be certified by GitHub Actions .NET stubs or synthetic test data.

### Current W20 R4 unselected
Only frozen R3 base #642 SHA `22cec9888047994ae523cbe7bf1b0a483d64cec4`. NEW competing R4 owner source candidates: [#647](https://github.com/C8LyNn/RoboRougelike/pull/647) exact `09c1809b73324a6d31c13e5d0143de62ac1e3ca6`, original Unity host blob `82147492a9205f8ef5986ceb61aeb10d6ea24ff2`, and [#652](https://github.com/C8LyNn/RoboRougelike/pull/652) exact `ae5dbfa5e9c4689d2bf80fcabf8438e9c673c419`, original host blob `7b8354bc4cd846227b4bdf6d25d81bed34a325bc`. **Owner has not supplied signed final W20 selection in HQ when this package was written**: both are review candidates; neither authorizes G1; never co-install. Original #647 **retains checkpoint owner lease through OnDisable and releases OnDestroy**, but #652 original OnDisable `ReleaseSlot()` still permits stale/contending host entry. Both may have SOURCE producer checks GREEN: not equivalent to release validation. Original historical #604 and #644 ALT_HOLD, never stage. Frozen #510 `d94400d95d13db7f27e1533df05c9292d5380431`, #556 `33094f154c7b91119b36d58aab285e6e83c5de57`, #596 `efb2b06eafedab5469b57f163152d96aca3c5915` and #642 unchanged. No Codex edits or unauthorized merge.

### Independent new verifier: **not another Unity launcher**

`verify_native_evidence.py` accepts **only a user-delivered, locally generated evidence directory**; performs no network calls and no native execution. It checks:
- Explicit operator opt-in, Windows Unity.exe command containing `-batchmode -runTests -testResults -testPlatform`, Unity **6000.3.14f1**, true-zero integer exit codes, offset-aware start/end and actual EditMode/PlayMode platform names.
- EXACT ONE set of **EditMode (at least 2 original #532), PhysX PlayMode (at least 14 original #532), SaveBridge PlayMode (at least 6 original R4 #647)**; original authored NUnit name sentinels, unique actual `test-case fullname` IDs, XML result Passed with coherent total/passed/failed/skipped, no skipped/failed tests.
- Exact selected source candidate SHA in immutable allowlist; separately trusted **B01-23 owner-signed R4** is needed, and no R4 winner is preapproved by this QA package. Provenance of frozen #510/#556 and R3 #642, identical source commit SHA before and after real Unity import.
- Raw Editor.log, stdout, NUnit XML for each suite; no missing/empty data, no path traversal, symlinks or absolute evidence paths, SHA256 matches **each original raw byte sequence**, actual Unity version in Editor.log, no explicit license error. XML DTD/ENTITY rejected.
- Produce JSON receipt with **`STRUCTURALLY_CONSISTENT_NOT_PROVEN_NATIVE_EXECUTION`** when all fields are consistent, NEVER `NATIVE_G1_PASS`. Self-authored files/hashes can be forged and no local structure verifier can attest they came from an activated editor. A B01-60 / user-signed manual review and trusted external operator context are mandatory before G1 acceptance. **`--enforce` always exit3 HOLD** until *a new separately reviewed, owner-authorized policy revision* pins a sole R4 and explicit independent local attestation. Malformed files exit2 fail closed. No forgeable in-chat native PASS.

### Evidence schema

Create a private folder `evidence/` (not inside the live game project) containing `manifest.json` plus hashed original files. For a genuine operator run, the B01-60 runner should emit this schema (not synthetic fixtures):

```json
{
  "schema":"B01-CI41-UNITY-NATIVE-EVIDENCE-001/R1",
  "producer_pr":"647",
  "source_sha":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
  "source_sha_after":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
  "source_r3_parent":"22cec9888047994ae523cbe7bf1b0a483d64cec4",
  "frozen_game_sha":"d94400d95d13db7f27e1533df05c9292d5380431",
  "frozen_save_sha":"33094f154c7b91119b36d58aab285e6e83c5de57",
  "unity_version":"6000.3.14f1",
  "operator_invoked":true,
  "sessions":[
    {
      "suite":"EditMode",
      "exit_code":0,
      "actual_test_platform":"EditMode",
      "command":["C:/.../Editor/Unity.exe","-batchmode","-runTests","-testPlatform","EditMode","-testResults","EditMode.xml"],
      "started_at":"2026-10-30T16:00:00+01:00",
      "ended_at":"2026-10-30T16:01:00+01:00",
      "editor_log":{"path":"editmode.log","sha256":"<64 hex digits>"},
      "stdout":{"path":"editmode.stdout","sha256":"<64 hex digits>"},
      "nunit_xml":{"path":"editmode.xml","sha256":"<64 hex digits>"}
    }
  ]
}
```
Only one session is shown for brevity; real evidence MUST have 3 separate suites EditMode, PhysX, SaveBridge, each with honest original raw outputs. The example is documentation **not executed output**.

Offline syntax review, **never engine execution**:
```powershell
py Tools/B01/CI41/P17W20/verify_native_evidence.py --evidence C:/separate/ci41-evidence --output C:/separate/ci41-g1-receipt.json
```
Security/release block (expected exit3 with structurally consistent data):
```powershell
py Tools/B01/CI41/P17W20/verify_native_evidence.py --evidence C:/separate/ci41-evidence --output C:/separate/ci41-release-gate.json --enforce
```
Do not upload operator-specific local paths, logs or project files to public CI without human approval.

### Actual automated QA for THIS code
GitHub Actions executes `test_verify_native_evidence.py` on Windows2025 and Ubuntu24.04: **32 adversarial tests per OS on self-generated SYNTHETIC fixture bytes** that deliberately DO NOT represent real Unity runs. Additionally the workflow fetches actual original Git R4 #647/#652 source commit refs and verifies host Git blob exact; compares lifecycle `OnDisable` authority (source audit ONLY). Artifacts contain only static machine results and synthetic-verifier stdout, **not** real Unity evidence. **G1 NOT_RUN** always in this workflow.

### Receivers & six lifecycle states
B01-60 attaches this NEW source guard to the one-command LOCAL manual runner #667 once real owner R4 winner is selected, without a new redundant launcher; B01-23/62 explicitly sign ONE R4 winner, new canonical #510 Save API development remains separate under #666; B01-40 exact source G0; B01-61 independent actual original transaction/ID/rollback; B01-00 only authorized release decision/real Codex intake.

`IMPLEMENTED=YES | TESTED=PENDING_CI | QA_ACCEPTED=NO | READY_FOR_INTEGRATOR=NO | INTEGRATED=NO | RELEASED=NO`; `SOURCE_G0=R4_PENDING | NATIVE_G1=NOT_RUN | CANONICAL_LIVE_GAME_SAVE=HOLD | G2=HOLD | CODEX_ACK=NONE | INTEGRATOR_FEEDBACK=NOT_MEASURED`.
Rollback: leave own Draft unmerged; remove only this new CI41 W20 folder+own workflow from a disposable checkout. Never delete/overwrite user working files or external evidence roots.

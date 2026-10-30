# B01-60 — P17/W23 — Original R5 Native Evidence Audit (read-only)

PACKAGE_ID: `B01-60-P17W23-R5-RECEIPT-AUDIT-001`. [HQ #731](https://github.com/C8LyNn/RoboRougelike/issues/731) · [Owner-approved local Windows testing #667](https://github.com/C8LyNn/RoboRougelike/issues/667) · original immutable W22 native R5 partial launcher [#718 @ `481bfb1cf2804d94fb49d7e6a57134e96c535b86`](https://github.com/C8LyNn/RoboRougelike/pull/718).

**This is NOT another native runner**: it audits exactly the receipts emitted by #718 without launching Unity, replacing owner selected sources, touching source GUIDs or editing old CI scripts. Sole selected original host R5 [#690 @ `cc7f73541517a18f4479bb155534bd4832d9985e`](https://github.com/C8LyNn/RoboRougelike/pull/690). Original frozen #510/#556/#647/#690/#691/#712/#729 and previous B01-60 QA unmodified.

## Verified gap addressed

Original W22 partial R5 launcher audits each individual Unity XML/Editor.log *at execution time*, and writes a self-hashed `P22-sha256-manifest.json`. There was no separate **read-only receipt verifier** for its bundle. W23 verifies original exact SHA256 file inventory and full original receipt metadata, rejects symlink/traversal, content changes, missing native files or extra NUnit test processes, contradictory NUnit root/test counts and fabricated full G1, and enforces six unique private project PlayerSettings/guard bindings. Even a synthetically perfect set of logs and XML can only reach `STRUCTURALLY_CONSISTENT_R5_6_OF_7_ONLY`, **not real-engine attestation, full seven-of-seven G1, or game Save/Load acceptance**.

## Operator workflow: existing native runner + one audit

From a local checkout of the W23 Draft branch (the earlier W21/V2 and W22/R5 scripts remain available), with Python 3 and original R5 Git history locally present:

```powershell
# Safe source-only R5 preflight, NEVER launches Unity:
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W22R5PartialNative\Invoke-B0160W22R5.ps1 -RepoPath .

# Explicitly authorized actual native PARTIAL (only with real licensed Unity Hub 6000.3.14f1):
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W22R5PartialNative\Invoke-B0160W22R5.ps1 -RepoPath . -Run

# After either mode, copy the B0160_W22_LOCAL_EVIDENCE_NO_UPLOAD directory
# printed by the runner; audit it READ-ONLY:
python .\Tools\B01\B01-60\P17W23R5EvidenceAudit\audit_r5.py 'C:\<PRIVATE_TEMP_PATH>\B22-xxxxxxxxxxxx'
```

The `-Run` switch requires a separate typed explicit operator consent `B0160 R5 NATIVE PARTIAL`. This is a **six-case partial only** execution, because original R5 `ColdRestart_RestoresRealRigidbodyAndConsumedIds` still expects generation 4 after original Save encodes generation 5; new B01-23 fixture-only owner correction / independent exact-SHA G0 & native full 7/7 are separate dependencies. Do not use W22 partial R5 to accept #729 V3 or #669/#712 physical Save/Load. The W21 V2 suite 2 EditMode+14 PlayMode is a separate runner and receipt, not audited by this script.

The audit is offline, **read-only**, never accesses owner game checkpoint, never uploads artifacts, never calls GitHub/SSH, never deletes files, never starts Unity, never changes Unity license/seats. `SHA256` is consistency, **not a signed or externally authenticated execution record**. Trust actual execution only with operator-confirmed original machine details and independent inspection of real Editor.log/NUnit3 XML. The verifier rejects impossible native promotion as a matter of policy, not proof of machine identity.

## Exact QA scope and local tests

```bash
python -m unittest discover -s Tools/B01/B01-60/P17W23R5EvidenceAudit -p test_audit_r5.py -v
```

33 **synthetic/adversarial only** tests validate expected source and partial-native shapes and omissions, test case names, six unique private checkpoints, all original six guard results plus six R5 selected method results, strict exact Unity 6000.3.14f1 Editor.log/process codes, no foreign tests, bad hashes, errors, owner SHA drift, corrupt/skipped/faked XML, symlink rejection. Dual-OS CI also executes actual W22 original Git-SHA source-only preflight then independent W23 audit of the resulting physical scratch receipt. Real Windows PowerShell source-only operator separately invoked in CI, **never Unity**.

**GATES:** `IMPLEMENTED=YES`; `TESTED=PENDING_REAL_CI`; `QA_ACCEPTED=NO_PENDING`; `READY_FOR_INTEGRATOR=NO`; `INTEGRATED=NO`; `RELEASED=NO`. `SOURCE_G0=R5_699_SOURCE_ONLY`; `NATIVE_G1=NOT_RUN`; `R5_FULL_G1=HOLD_COLDRESTART`; `CANONICAL_729_712_669_G1=NOT_RUN`; `GAME_SAVELOAD=HOLD`; `G2=HOLD`; `CODEX_ACK=NONE`; `INTEGRATOR_FEEDBACK=NOT_MEASURED`.

**RECEIVERS:** B01-41/61 independent evidence and R5 source QA; B01-40 source/G0 physical integrity; B01-23 owner fixed ColdRestart fixture and new exact SHA; B01-62/#729 session authority source acceptance; B01-00 owner-facing local Windows opt-in instructions; B01-60 real native receipt only after owner independently runs and deliberately shares redacted logs. **Safe rollback:** discard ONLY new W23 QA-owned files/branch; no merge or destructive cleanup.

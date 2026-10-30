# B01-60 PASS17/W23 — exact R5 Native Evidence Audit (issue #731)

**PACKAGE_ID:** B01-60-P17W23-R5-NATIVE-EVIDENCE-AUDIT/R1.

Independent read-only verification for previously source-tested [#718](https://github.com/C8LyNn/RoboRougelike/pull/718) Windows Unity R5 native operator. Selected owner host **#690 @ cc7f73541517a18f4479bb155534bd4832d9985e**; immutable W22 original #718 head **481bfb1cf2804d94fb49d7e6a57134e96c535b86**. No changes to #718, R5, Unity Assets, frozen #510/#556, API #729, #712/#669 storage, or protected refs. No new alternate Save authority, owner lock or Unity launcher.

## W23 independent audit

The original R5 receiver checks Unity XML and source at runtime. W23 separately verifies original P22-receipt.json, SHA256 inventory, **all 12 XML, 12 Editor.log, 12 process logs**, exact six private Unity PlayerSettings products and source probes, original fixture/core/host/retirement Git-blob SHA1 values, and six selected NUnit identities. It rejects absent, duplicate, skipped, contradictory, failed cases, nonzero process exit, wrong Editor version, staged source drift, false G0/G1, and absence of original ColdRestart HOLD.

**Important:** SHA256 is self-consistency, not execution attestation. Real raw Editor.log/NUnit XML and operator confirmation still need independent manual inspection. All W23 CI fixtures are SYNTHETIC and no Unity executable is launched by this package.

The six #718 PlayMode cases are explicitly PARTIAL. The seventh original R5 ColdRestart_RestoresRealRigidbodyAndConsumedIds incorrectly expects generation 4 after TrySaveBytes emits generation 5 from generation 4; new owner-versioned corrected fixture required from B01-23. Frozen original source is untouched; FULL_R5_G1 remains HOLD even after six real PASS.

## One-command user-side Windows operator

From a trusted checkout of this W23 branch, with exact original historical R5 #690 Git commit available locally, run existing inherited **source-only** preflight:

~~~powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W22R5PartialNative\Invoke-B0160W22R5.ps1 -RepoPath .
~~~

Source-only never launches Unity. To **manually opt in** on owner's PC, after original G0 review, legally activated Unity Hub 6000.3.14f1 and explicit approval, append -Run:

~~~powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W22R5PartialNative\Invoke-B0160W22R5.ps1 -RepoPath . -Run
~~~

The original launcher requires typed B0160 R5 NATIVE PARTIAL before six isolated PlayMode tests. No administrator account, SSH, passwords, services, installation into canonical game, network fetch, upload or foreign-directory cleanup.

Use the original runner printed B0160_W22_LOCAL_EVIDENCE_NO_UPLOAD local directory as the following -evidence value:

~~~powershell
python .\Tools\B01\B01-60\P17W23R5NativeEvidenceAudit\audit.py --evidence "C:\your-actual-B22-unique-evidence-folder"
~~~

W23 is **read-only**. Source-only result is SOURCE_PREFLIGHT_ONLY_NATIVE_NOT_RUN; even consistent native evidence says STRUCTURALLY_CONSISTENT_6_R5_CASES_UNATTESTED_FULL_G1_HOLD. Review/redact machine-specific paths before any owner-authorized sharing.

Execute 20 synthetic independent source tests (never Unity):

~~~powershell
python -m unittest discover -s Tools\B01\B01-60\P17W23R5NativeEvidenceAudit -p test_audit.py -v
~~~

Historical V2 2 EditMode+14 PlayMode is separately covered by #711/#717; DO NOT coinstall alternative suites. V3 #729 has distinct .owner lock from original #712/#669 .lease: DO NOT co-install until owner releases single-authority bridge. W23 does NOT validate live Save/Load.

**Gates:** SOURCE_G0=R5_699_SOURCE_TRANSPORT_ONLY; NATIVE_G1=NOT_RUN until genuine opt-in logs; FULL_R5_G1=HOLD_7TH_COLDRESTART; CANONICAL_V3_729_712_669_G1=NOT_RUN; LIVE_GAME_SAVELOAD=HOLD; G2=HOLD; CODEX_ACK=NONE; INTEGRATOR_FEEDBACK=NOT_MEASURED. Receiver B01-41/40 independent QA/source intake, B01-23 owner corrected fixture, B01-00 owner handoff. Rollback discards only W23 new own files/branch, no merge.

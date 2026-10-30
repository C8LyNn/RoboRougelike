# B01-60 — WAVE14 | Native G1 evidence must include actual independent test-case identities

Scope: **only existing B01-60 native tester** [#532](https://github.com/C8LyNn/RoboRougelike/pull/532), with its versioned rollback fix [#542](https://github.com/C8LyNn/RoboRougelike/pull/542). **Selected game source #510 is frozen** at `d94400d95d13db7f27e1533df05c9292d5380431`; #511/#494 are HOLD. This PR contains no changes to game `Assets`, Codex source, owner Save/Load adapters, source #510, or original frozen B01-60 #532/#542 branches. It is another *new version* of our QA-only runtime runner.

## Why this is a security/QA correctness issue

Old actual native runner trusted only `test-run result=Passed passed>=minimum failed=0`. An empty (fabricated, stale, or accidentally wrong-project) NUnit XML file `<test-run result="Passed" passed="14" failed="0"/>` would have been **accepted as real Unity PlayMode G1 PASS**, despite zero `test-case` entries and no physical test evidence. Similarly for EditMode counter `passed=2`. Original runner also did **not require existence of Editor.log**. A working real Unity editor is still needed: pure Python XML is never itself evidence.

## Corrected gate and retained real native tester

The existing `Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py` is corrected **only on this new W14 branch**. It now requires (a) a real nonempty `Editor.log` bearing original Unity Editor version `6000.3.14f1`, and (b) every single **original 14 independent B01-60 PlayMode and 2 EditMode test-case full names**, exact distinct occurrence 1, `result=Passed`, and aggregate suite status Passed/failed=0, before marking a native verdict. Exact native test names match the unchanged independently authored #532 fixtures; this gate does **not** rewrite the Unity tests. It also retains all source-pinning and W13 empty-folder rollback fixes. The log/version requirement is an additional basic sanity check, **NOT cryptographic attestation**; an adversarial forger can manufacture data and must be countered by retaining genuine host provenance. Acceptance still requires legally activated Unity and actual original on-host logs/exit codes.

Run the actual new **source-level** validator regression on Windows or Ubuntu using Python/Git, not Unity:

```sh
python Tools/B01/B01-60/P17W14NativeResultProof/test_native_result_gate.py
```

The test checks exact original Git blob `269aed3ede17bfe614455e6e0edd446f60ae3b0f` from frozen #542 (`ed0234d8...`), **executes the unchanged original** `parse_xml` twice on deliberately fabricated aggregate-only XML (both expected historical false-GREEN); executes corrected same function on two valid full-case *synthetic* XML fixtures and adversarial substituted/missing/duplicated/failing/skipped suite cases, wrong-editor-version and absent-log fixtures. It records a JSON receipt explicitly marked `SYNTHETIC_ADVERSARIAL_XML_NOT_UNITY`. Dual OS GitHub Actions run exact W14 HEAD; #542 previous real rollback suite is re-run for nonregression. This is real Python execution and source-only QA of **evidence trustworthiness**, not live PhysX.

## Actual native host (NOT_RUN here)

With a **licensed and activated Unity 6000.3.14f1** binary available, the same original runner from #532 is ready. `--mode native --unity "<absolute Unity executable>" --out "<new/empty evidence path>"`; it creates original exact #510 + W06 project, applies own NUnit fixtures, and executes real EditMode and PlayMode, saving two original Editor.logs, NUnit XML files, exit codes, ZIP and provenance. Only then can G1 be independently evaluated. On current isolated service/container there is no Unity Editor executable, no licensed activation, and no original native XML or Editor.log; `G1=NOT_RUN`. Missing-editor negative test returning exit 3 is NOT native execution.

## Rollback, owners, blocker

Delete the W14 files and revert only the OWN B01-60 runner.py delta on this new branch. No edits to original #510, #532, #542, Codex, main/develop, or merges. Formal original #510 source G0 accepted B01-40 [#521](https://github.com/C8LyNn/RoboRougelike/pull/521). Native G1 NOT_RUN, Save/Load remains HOLD pending B01-23/62 choosing one corrected #544/#549 after [#550](https://github.com/C8LyNn/RoboRougelike/pull/550), migration/rollback #536/#542 independently accepted only SOURCE, G2 HOLD. Receiver: B01-60 native authorized Unity host; B01-40 and B01-62 for source/migration; B01-61 for source Save/Load; B01-00 only for Codex queue #79; Codex ACK NONE, NOT_INTEGRATED.

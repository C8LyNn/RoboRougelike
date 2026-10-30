# B01-60 — PASS17 / WAVE15 — native G1 requires BOTH real Unity NUnit suites

**Issue:** [#571](https://github.com/C8LyNn/RoboRougelike/issues/571); governance [#276](https://github.com/C8LyNn/RoboRougelike/issues/276). This is a **new version of B01-60's OWN existing Unity native runner**, stacked on frozen [#567](https://github.com/C8LyNn/RoboRougelike/pull/567) original QA SHA `4303e62b0339c6cdc828478a06b6e2c9c79e6e13`. Gameplay source [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) **`d94400d95d13db7f27e1533df05c9292d5380431`** and ONLY Save/Load contract [#556](https://github.com/C8LyNn/RoboRougelike/pull/556) **`33094f154c7b91119b36d58aab285e6e83c5de57`** stay FROZEN. Alternatives #511/#494/#544/#549/#554 HOLD, no mixed sources, no Codex, no canonical game migrations, no merge.

## Reproduced genuine original-code W14 false G1 gate (new finding; not re-test of old XML-only bug)

The previously corrected W14 `parse_xml()` **correctly requires exact 2+14 NUnit case fullnames, and `require_editor_version_log()` requires Editor version**. However, in the actual frozen W14 `main()` `require_editor_version_log(log)` and `real[platform]=parse_xml(xml,min_tests)` were **mis-indented ONE level outside** `for platform,min_tests in (("EditMode",2),("PlayMode",14))`. Therefore the runner invoked **TWO** Unity subprocesses but accepted **ONLY the final PlayMode XML and log**; a bad/missing log, failing/partial/missing suite identities in EditMode went unchecked. The W14 source could write `native-verdict.json = {G1:PASS,results:{PlayMode:...}}`. This is a correctness/security problem of the QA receiver, not a proven defect of original #510 physics.

This W15 version moves **BOTH existing original log and exact-case validations inside the loop**, then adds `require(set(real)=={"EditMode","PlayMode"})` before publishing any success receipt. Final `native-verdict.json` now contains two independently verified original XML suites (2 real EditMode +14 real PlayMode) or it is never emitted. Keep same `runner.py` and unchanged C# NUnit fixtures from #532; no second competing runner, no reimplementing source-only ledger tests.

## Executable regression of REAL frozen old Python `main()` (synthetic subprocess!)

```sh
python Tools/B01/B01-60/P17W15NativePairGate/test_native_pair_gate.py
```

This test imports the exact frozen previous original W14 runner Git blob `919d8db76eb38d55e5b900f44692e23806f6900b` at SHA `4303e62b0339c6cdc828478a06b6e2c9c79e6e13`, and **really executes its `main()` Python source-control logic** under controlled I/O. The subprocess and its NUnit XML/Editor.log are **EXPLICIT SYNTHETIC STUB**, not a Unity binary and not evidence of G1. Native source/staging are also stubbed because this is exclusively a receiver *control-flow QA* test. Six deliberate EditMode bad states: missing Editor.log, wrong Unity version, failing suite, missing test-case, duplicate test-case, skipped test-case. Original W14 code demonstrably writes **six false `G1=PASS` receipts containing only `PlayMode`**. W15 fixed original runner code denies all six with no success receipt, then a separate positive synthetic two-suite fixture demonstrates W15 would require both distinct 2+14 independent results. Everything is deleted with the temporary QA directory; no fake native receipt is preserved outside the short-lived negative test. Original true Unity/PhysX remains `NOT_RUN`.

On GitHub Actions Windows2025 and Ubuntu24.04 the exact-W15-head workflow additionally **reexecutes original W14 `test_native_result_gate.py` (prior 26 adversarial parser inputs), W13 original physical directory rollback regression (12 negatives, original RED), and existing #532 runner's source preflight plus missing-Editor exit 3**; this is only regression of the modified shared B01-60 owned runner, not a duplicate test framework.

## Real native host execution, when authorized

Reuse **the same** native runner and original #532 Unity C# tests on an authorized, legally activated Unity 6000.3.14f1 editor:

```sh
python Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py --mode native --unity "/absolute/path/to/activated/Unity" --out "/absolute/path/to/new/evidence"
```

Real acceptance requires both **actual Unity process exit codes = 0**, genuine **EditMode XML 2/2 exact IDs PASS**, **PlayMode XML 14/14 exact IDs PASS**, original **separate** two Editor.logs with Unity version 6000.3.14f1, actual real Light+Heavy PhysX/Transform/ledger tests and source SHA. A synthetic CI test is explicitly NOT Unity execution. On this host **Unity executable/authorized activation absent**, real native `G1=NOT_RUN`; no original XML/Editor.log. The source project contains selected #510 only; it DOES NOT incorporate #556 host Save/Load bridge or prove native save/load. The new B01-23/62 adapter must be separately integrated/QA-accepted in authorized host before any native Save→Load claim.

**Gates:** `#510_SOURCE_G0_ACCEPTED_SOURCE_ONLY`, `#556_SOURCE_G0_ACCEPTED_SOURCE_ONLY` per B01-40 #560, `B01-60_W15_NATIVE_GATE_SOURCE_QA_PASS_IF_ACTUALLY_EXECUTED`, `NATIVE_G1_NOT_RUN`, `LIVE_SAVELOAD_HOLD`, `G2_HOLD`, `CODEX_ACK_NONE`, `NO_MERGE`.

**Rollback:** discard four NEW W15 B01-60 paths and revert ONLY the W15 delta in the B01-60-owned runner on the W15 branch, retaining original W14 #567, W13 #542, W12 #532 frozen source bytes. **Receiver:** B01-60 authorized real Unity host + B01-40 G0, B01-61/62 Save #556 adapter QA, B01-23/62 bridge owner, B01-00 sole Codex #79.

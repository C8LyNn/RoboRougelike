# B01-60 — PASS 17 / WAVE 13 — Native receiver rollback R2 / original W12 RED → W13 GREEN

## Scope and exact upstream SHA

HQ [#535](https://github.com/C8LyNn/RoboRougelike/issues/535), owner governance [#276](https://github.com/C8LyNn/RoboRougelike/issues/276). **Exactly one selected Secure Reattach V2 production candidate:** B01-23 [#510 at `d94400d95d13db7f27e1533df05c9292d5380431`](https://github.com/C8LyNn/RoboRougelike/pull/510). B01-23 source, original W06 ledger, Codex game, legacy W07 and alternatives #511/#494 remain unchanged and are NEVER co-installed.

**Versioned independent B01-60 QA-only correction**, stacked on the previously completed [native receiver Draft #532 at `5778023163d8b51d23abfebaa17119e21bd5e131`](https://github.com/C8LyNn/RoboRougelike/pull/532). Does not change source/branch of frozen #532. It makes ONE surgical change in **our own existing** `Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py`: protects empty FOREIGN directories during the rollback of an isolated source-staged project. The W12 runner's old `rollback()` verified *files* byte-for-byte and refused foreign files/symlinks; however it did not inventory directories, so `shutil.rmtree(root)` silently deleted a foreign empty subdirectory even with no foreign files. This risk was identified in HQ [#535](https://github.com/C8LyNn/RoboRougelike/issues/535) based on B01-23 [#525](https://github.com/C8LyNn/RoboRougelike/pull/525). We correct **the B01-60 test receiver only**. B01-40/62 independently own the producer's physical package receiver and canonical migration; do not claim this QA fix covers their code.

## Real executed test, not another Unity source simulator

```sh
python Tools/B01/B01-60/P17W13NativeRollbackQA/audit.py
```

**Old original source executed:** `audit.py` reads the exact frozen original W12 runner Git blob `df944c854ab134b578a2b98d451f055717a16668` via `git show 577802...:runner.py`, executes its actual unmodified Python functions, physically stages an isolated directory and creates a foreign empty directory. It then invokes the **real original W12 `rollback()`**, proving that it unacceptably removes that directory. This is an explicitly expected historical **RED** and not a harmless hypothetical grep.

**Corrected new version executed:** same actual staging and archive functions from the versioned new W13 runner, with empty foreign folders at root, nested below Assets, Packages and ProjectSettings, multiple deep subfolders. A predelete inventory collects all file bytes and **the complete directory topology** and compares them to parents derivable from exact archived file paths. Extra/missing directory, symlink, changed file or foreign user file causes fail-closed refusal **BEFORE `shutil.rmtree`**. After each denial verifies foreign folders/user bytes are intact, plus genuine positive rollback on a clean exactly-owned staged tree. Synthetic staging fixture exercises **real receiver I/O methods**, not real Unity Engine or simulated game physics. Independent Actions runs both Windows2025 and Ubuntu24.04 and archives the exact output receipt.

The prior `#532` original native test fixtures are **reused verbatim**, not reauthored. Do not create a parallel editor runner. Production source remains original #510; original W12 receiver has its own historical frozen SHA and prior source-only CI, not overwritten. B01-61/#517 source transaction PASS does not establish native G1. B01-40 #521 source G0 accepted only, B01-23/62 new save/load adapter pending.

## Native Unity conditional execution

The *same* original existing runner remains callable on a genuinely authorized licensed Unity 6000.3.14f1 host:

```sh
python Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py --mode native --unity "/absolute/path/to/Unity" --out "/new/absolute/output"
```

For native G1, need actual Unity binary, legal activation, real **EditMode ≥2 + PlayMode ≥14 PASS**, original `Editor.log`, NUnit XML, real process exit codes and direct Rigidbody/PhysX tests of Light + Heavy, all aliases, missing consumed ID and rollback. Current B01-60 execution host has **no Unity executable or installed license**. Dedicated CI intentionally runs a negative `--mode native --unity /nonexistent/Unity` expecting exit code **3**, output `NATIVE_NOT_RUN` and absence of XML/Editor.log, without falsely declaring G1 PASS. This negative is a **source-only missing-editor path test**, not running Unity. Save/Load integration is separately pending.

## Release disposition / rollback

`SOURCE_QA_RECEIVER_ROLLBACK_R2_TESTED`, selected `#510 SOURCE_G0=ACCEPTED_SOURCE_ONLY` separately by B01-40, `NATIVE_G1=NOT_RUN`, `SAVELOAD_ACCEPTANCE=HOLD`, `G2=HOLD`, `CODEX_ACK=NONE`, `NO_MERGE`. Rollback of this new version: close W13 QA Draft and remove its **4 new** own W13 QA files and its one surgical B01-60-owned runner change; no producer source or host game modification. No installation of #511, #494 or legacy W07.

**Receiver:** B01-40/62 own real package migration/rollback (they must independently test their own latest receiver), B01-61 code/ID regression, B01-23+62 save schema, B01-60 licensed Unity host, B01-00 sole eventual Codex inbox #79.

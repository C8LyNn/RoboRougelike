# B01-50 PERFRUN-001 R1 — real Windows Player capture operator

**Producer:** B01-50 / Build Performance & Profiling. **Output:** executable Python CLI, original positive/negative tests, immutable hashes and local test log. **Scope:** only `Tools/B01/B01-50/PERFRUN001/**`. It does not change Codex's `Assets/**`, game build, saves, CI workflow, CHL/PARTSIZE, VFX or scene configuration.

## Why this is not another synthetic benchmark

`PERFRUN-001` is the **missing safe execution/reception operator** for the real opt-in Unity Player recorder already implemented in frozen [PERFSCALING-001 PR #328](https://github.com/C8LyNn/RoboRougelike/pull/328) at `b5fc00428f6f663016e1525da556afbfe775facb`:

- It checks **the EXE and every extracted runtime file byte-for-byte against the supplied original Windows build ZIP**, not just the EXE. The archive must contain exactly one EXE member, required UnityPlayer.dll and `<PlayerName>_Data` configuration/manager files, one safe archive root, no duplicate paths/symlinks, and matching per-file SHA-256. It independently hashes both to SHA-256 and records their identities. The `--game-sha` is operator supplied and **not** independently proven by that binary hash; verify actual build provenance externally.
- It pins the real `validate_live.py` to original immutable Git blob `7060aa3e364b2bc69de99501faa99bba57e84f82`; rejects any other code, even if named the same.
- It starts the actual Player with existing `-b0150-live-*` flags, a dedicated `Player.log` path and a unique output directory. There are no fake runtime counters or invented Unity APIs.
- It gives the recorder its fixed 30 s warmup + 120 s sampling window, then shuts down a still-running Player after the set timeout. It **never replaces game controls**; the operator must configure each legal in-game 2/6/12-gun, map, enemy count or MOV009 scenario manually **in the actual player**. If such a configuration cannot be achieved legitimately, record it as BLOCKED; don't force parts or fabricate that scenario.
- It calls the exact pinned receiver over real recorder `receipt.json + frames.csv + objects.csv`, writes raw SHA-256 and an `operator_receipt.json` marked **`ORIGINAL_PLAYER_TRACE_SCHEMA_VALIDATED_NATIVE_REVIEW_REQUIRED`**, *not* G1, not measured display FPS/Unity CPU or GPU profiler acceptance. Crash, bad ZIP, incomplete/wrong receipt or validator failure yields **`CAPTURE_FAILED_NOT_NATIVE_EVIDENCE`** and preserves original logs.

No prior RoboRougelike 120 s original trace is included. This package is **not evidence of an actual Windows Player session**. Native Unity and original Windows Player execution were **NOT_RUN** on the B01-50 host; the only executed tests are the source-level harness tests using **explicit simulated fake process/receipt fixtures**.

## Prerequisites

- Authorized, isolated **Windows** machine with permission to execute the original RoboRougelike build and inspect its logs. Python 3.10+ with no dependencies.
- A genuinely built Windows Player with `B0150_LIVE_PROFILING` compile define **enabled** and optional [B0150LiveFrameCapture.cs](https://github.com/C8LyNn/RoboRougelike/blob/b5fc00428f6f663016e1525da556afbfe775facb/Tools/B01/B01-50/PERFSCALING001/Runtime/B0150LiveFrameCapture.cs) compiled **by Codex only** in his permitted worktree. Without this, the operator fails closed because the trace files never appear.
- The unchanged original [validate_live.py](https://github.com/C8LyNn/RoboRougelike/blob/b5fc00428f6f663016e1525da556afbfe775facb/Tools/B01/B01-50/PERFSCALING001/validate_live.py); the script verifies its actual Git blob, not merely its filename. A complete original build ZIP **fully extracted** into a dedicated directory; the operator checks every archived file in that directory, rejects mixed/absent assets and requires original Unity runtime core files. Do not run from a partially copied or edited distribution.
- An existing empty parent directory for runs and the **real source commit of that build** (40 hex chars). First record native scene/weapon setup, run same scenario route and stable hardware conditions; side-by-side quantitative conclusions remain conditional.

## Run (PowerShell; operator chooses all paths)

```powershell
$root = 'C:\B01-PerfEvidence'
New-Item -ItemType Directory -Force $root | Out-Null
$operator = 'Tools\B01\B01-50\PERFRUN001\capture.py'
$validator = 'Tools\B01\B01-50\PERFSCALING001\validate_live.py'
$player = 'C:\RoboBuild\RoboRougelike.exe'
$zip = 'C:\BuildArchives\RoboRougelike-original.zip'
$sha = '<40-CHAR-ACTUAL-LOWERCASE-GAME-COMMIT>'  # REPLACE with actual SHA

python $operator --player $player --build-zip $zip --validator $validator `
  --game-sha $sha --scenario '2gun-map-a' --out-parent $root --dry-run

# Only after validating the build, provenance and manual scene setup:
python $operator --player $player --build-zip $zip --validator $validator `
  --game-sha $sha --scenario '2gun-map-a' --out-parent $root --run-seconds 165
```

Separate distinct sessions use unique scenario IDs (e.g. `6gun-map-a`, `12gun-map-a`, `12gun-mov009`); **never rerun a previous ID into the same folder**. An invalid character, unsafe path, stale/missing build or output collision fails *before process execution*. The parent output directory must already exist; no network, server, credentials, localhost or external API calls.

The real player's **30 s warmup starts when Unity loads the recorder**, not when manual player setup ends. The operator cannot automatically ensure the requested stage lasts 120 s. Independent reviewers must verify Player.log, build/game SHA provenance, run configuration/physical weapon mounts, display/GC distinction, frame trace, process stop reason and scene state. Do not call this benchmark a gameplay FPS improvement.

## Reproduce actual source tests

```sh
cd Tools/B01/B01-50/PERFRUN001
python -m unittest discover -s tests -v
python -m py_compile capture.py
```

The **20/20 source tests are performed using explicitly FAKE executable ZIP members and a MOCK original receiver**, proving process orchestration and fail-closed behavior only. The production CLI **refuses this mock** because it pins `validate_live.py` to the original Git blob. The same tests check a wrong Player SHA, wrong/malformed ZIP, duplicate binary members, directory reuse, invalid scenario/SHAs, wrong validator blob, shorter-than-capture timeout, missing/partial receipt, nonzero Player exit, validator exit failure, `dry-run` side effects, no external network and absence of native approval. The test `test_13_fake_capture_with_mock_validator_still_not_native_pass` exercises a synthetic *branch* but never claims real game capture.

## Receiver / rollback / ownership

Codex can use this **standalone** tool without changing his game source. If an opt-in native build is not available, keep `NATIVE_NOT_RUN` and return missing build/legal host as a blocker. Do not request direct writes on the user's own Windows computer through another agent. **B01-40** checks file/manifest/ZIP provenance; **B01-60** independently reruns the exact operator code and tests; **B01-00** controls conditional CV-12 handoff through #79. `SOURCE_READY` does not mean `CODEX_CLAIMED` or `INTEGRATED`.

To roll back, delete only the `Tools/B01/B01-50/PERFRUN001/` standalone folder and the operator-owned **run output directory you chose**. It never changes source assets, gameplay stats, save files or main/develop. No merge or release authorization implied.

### Historical *real archive* provenance-only smoke check

The existing **historical** `RoboRougelike_SuspensionUX_Windows_x64.zip` supplied earlier to this conversation (ZIP SHA256 `ec3193bb3ac24398c496ea65ad12aa653321db160a4dc74e8f618e3cc7ad6d1d`) was actually opened and its archived `RoboRougelike.exe` extracted to a disposable directory. Running the operator's original `verify_exe_zip_member` on those two physical artifacts **PASS**: original EXE SHA256 `17ad8b27359b94eac8d2f44a0052d8789dc3ff5328f44ccb8c2740be4a375eb8`; full original result in `evidence/historical_zip_binary_match.json`. **The EXE was NOT executed**, the archived game may not be built from current Codex source, and the 38 MB ZIP is **not distributed inside this package**. In R2 the original archive was fully extracted to disposable storage and **212/212 original runtime files (103,854,568 uncompressed bytes) matched their ZIP member SHA-256**; see `evidence/historical_full_distribution_receipt.json`. This does not establish any frame time or G1.

## R2 / PASS17 WAVE04 — original source fix before independent QA

The original R1 source at `e544f4a00c2ac149dfe118293929667559e7d521` only checked the EXE. **Confirmed RED** with five additional original regression cases: `20 PASS / 4 FAIL / 1 ERROR`, exit 1. A matching EXE could be paired with different `UnityPlayer.dll` or missing/corrupted `<PlayerName>_Data` assets; ZIP members could carry traversal or unexpected roots; and a JSON-list capture receipt raised uncaught `AttributeError` instead of leaving a structured failure receipt.

The exact R2 fix verifies **every archive distribution file** against its extracted sibling, its size and SHA-256, expected Unity core files, a single ZIP root, path safety and duplicate/path case conflicts. Archive contents are only read, **never extracted or modified by the operator**. New operator receipts include the verified file count and an aggregate distribution fingerprint. A non-object JSON receipt fails with a preserved negative operator receipt. Original unchanged positive cases and 5 new negatives: **25/25 PASS**, Python `unittest`, no native Unity execution.

**Real historical fixture:** the supplied old `RoboRougelike_SuspensionUX_Windows_x64.zip` was fully extracted to a disposable directory; the new actual original-source function checked **212/212 archive files (103,854,568 uncompressed bytes)** with byte-identical SHA-256. This is physical file-verification evidence, **not Player execution** and **not proof that the requested game SHA is actually embedded in the binary**. Source SHA is still supplied by the operator and requires independent build provenance. The distributable ZIP and native Windows game runtime are NOT included.

**Receiver acceptance:** B01-40 must check exact Git HEAD and updated per-file SHA256 manifest and replay the historical ZIP receipt if authorized; B01-60 independently executes `python -m unittest discover -s tests -v` against that exact SHA and checks RED→GREEN. Only B01-00 can prioritize Codex CV-12 after G0; the game owner Codex alone can build/install the optional recorder into the game. A source-test green or ZIP-match green is not G1 or Codex ACK. Rollback remains delete the operator-only Tools folder and explicitly chosen evidence outputs, not shared game code.


## WAVE 05 / original validator integration receipt (same R2 operator, no separate prototype)

The previous 25 owner tests use **fake validators**. These are useful for subprocess and error handling but do not establish that the real recorder receipt flows through the actual original validator. This is now separately covered by `tests/test_original_validator_integration.py`:

- It **executes the untouched real** `validate_live.py` from original #328 using its subprocess/CLI, not a mock. Every execution verifies the original **Git blob SHA-1** `7060aa3e364b2bc69de99501faa99bba57e84f82`.
- The positive case builds explicitly **SYNTHETIC** format-faithful `receipt.json`, `frames.csv` (7200 rows), and `objects.csv` (120 scans) marked 120.04 s and passes them through original `run_and_validate`. It must return `LIVE_RAW_REVIEW_REQUIRED_NOT_PERFORMANCE_ACCEPTANCE` and the operator receipt must keep `native_performance_acceptance=false`.
- Three negative cases corrupt the Unity version, truncate scans or corrupt the CSV header, and require actual `validate_live.py` exit nonzero and preserved negative operator receipts.
- Exact original-source producer run on Linux Python 3.13: **29 PASS, 0 FAIL, 0 SKIP** (25 existing + 4 original-validator integration). Full log SHA256 `8b8e8c6561041cf2da735b33f4278a35babd8d642fd6828d7ad199a54d162afe` and source result in `evidence/W05_original_validator_interop_receipt.json`.
- The 4 integration tests **explicitly SKIP when the original validator path is absent**. An ordinary run without `B0150_ORIGINAL_VALIDATOR` is therefore **25 PASS / 4 SKIP**, not 29 PASS. An incorrect or modified validator with that variable set **FAILS**, not a silent skip. Do not distribute a copied/retyped validator as if it were pinned.

**Bash / authorized independent QA** (requires locally checked-out original Git commit from #328):

```sh
git show b5fc00428f6f663016e1525da556afbfe775facb:Tools/B01/B01-50/PERFSCALING001/validate_live.py > /tmp/b0150-exact-validate-live.py
cd Tools/B01/B01-50/PERFRUN001
B0150_ORIGINAL_VALIDATOR=/tmp/b0150-exact-validate-live.py python -m unittest discover -s tests -v
```

**PowerShell / Windows**: export original `validate_live.py` from immutable #328 into an operator-owned file; set `$env:B0150_ORIGINAL_VALIDATOR = 'C:\B01QA\validate_live.py'` before `python -m unittest discover -s tests -v`. Restore/clear the environment variable when finished. No network or actual Player is required for these source tests.

**Acceptance boundary:** this explicitly tests interoperability of *actual original Python components* with simulated Player data, not original Unity runtime execution, not external independent QA, not FPS/display-frame measurements, not current Codex SHA and not a Codex ACK. Native acceptance still requires Codex's authorized rebuilt Player and B01-00 approval under existing CV-12 #79. Rollback and permissions unchanged.

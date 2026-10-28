# B01-60 / W12 — QA-NATIVE-001

Independently owned QA tools only; no producer/game code or Codex worktree edits.

**Executable source**: `qa_native.py` (original parser) plus `qa_gate_v2.py` (strict profile/receipt gate) (stdlib Python 3.10+) + `Run-NativeQA.ps1` (Windows). Distinct `test_qa_native.py` exercises synthetic negative receipts. Both native tests and manual playtest are **NOT_RUN** in the B01-60 Linux environment. **G2 HOLD**.

## Why this is not fake native PASS

Run receipt `Compile/process.json` must include actual `exit_code`, `source_sha`, `target_sha`, `unity_version`. `EditMode` and `PlayMode` additionally need original `results.xml`, nonempty Editor.log and exact required test cases; `Player720`/`Player1080` require real PNGs at native resolutions, Player.log, telemetry.json, actual native exit codes. Missing files never grant PASS. Manual input never inherits API-driver PASS. Tool results cannot prove their own authorship; B01-60 witness + runner attestation remain separate.

## One-run usage

Prepare an **independent, disposable** checkout at the exact approved target git HEAD, including full source from the frozen producer SHA with preserved .meta GUIDs. Populate `plan.example.json` copy with verified full source-file SHA256 paths/hashes obtained from the immutable producer source. Empty `source_files` deliberately blocks execution. Create a local marker file `.b01-60-disposable-qa` in the disposable checkout only (never commit).

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools\B01\QA-NATIVE-001\Run-NativeQA.ps1 `
  -ProjectPath 'C:\QA\RRL-isolated' `
  -UnityExe 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe' `
  -PlanPath 'C:\QA\plan.json' -EvidencePath 'C:\QA\Evidence\run-001' `
  -PlayerExe 'C:\QA\RRL-isolated\Builds\NativeQA.exe'
```

The optional Player must implement `-qa-output <dir>`, generate *actual* named PNGs and telemetry per the plan. Without it the Player phases are NOT_RUN. Source/target SHA mismatches, live Editor locks, outside-worktree Player, reused evidence directory, invalid source file hashes and incorrect Unity version fail early. For evidence-only review: `python qa_gate_v2.py --plan plan.json --evidence Evidence/run-001 --out verdict.json`. V2 requires explicit nonempty Edit/Play NUnit cases and full-profile coverage. `--validate-only` rejects incomplete plans before Unity launches. CAMERA receipt needs finite aim/speed; R6 requires measured wheel queries/contact, distance, checkpoints and event counts. A structural receipt `PASS` is NOT independently witnessed native B01-60 acceptance.

**Exit:** `0 PASS`, `2 FAIL`, `3 BLOCKED/NOT_RUN`. Capture raw original Editor.log/XML/Player.log/telemetry, never forge output for producer receipts. Remaining API-driven R6 wheel tests do not imply manual keyboard play.

## Rollback and ownership

Stop all runner-owned test processes; archive evidence outside checkout; delete only the disposable QA checkout after confirmation. Restore no production scenes because this tool changes none. No merge/G2. New producer SHAs always require a new plan/evidence folder. Owned path: `Tools/B01/QA-NATIVE-001/**` and its QA-specific CI workflow.

**Scope of native verification:** MAPGEN R3.4.1 #237 `5be31039411f89f17654b1c332f8692883fa03cc`, target example OBJ #210 `4c9236a4cdc27814ea2e7679872ec4a524150cc8` (must revalidate before run). CAMERA R1.2 requires a newly frozen source SHA; previous Codex #234 R1.1 reticle transition/corridor failures are *regression targets*, not independent B01-60 test execution.


## Reproducible GitHub ZIP (exact tracked PR sources)

`python Tools/B01/QA-NATIVE-001/package.py --repo . --out Artifacts/B01-60/QA-NATIVE-001.zip --report Artifacts/B01-60/SHA256_REPORT.json` validates every current committed source file against `manifest.json` before writing a deterministic ZIP, SHA256 report and status `nativeUnity=NOT_RUN`. GitHub Actions uploads both as a CI artifact. The offline 22/22 ZIP available from ChatGPT is a **distinct** fuller source revision and is not assumed identical to this PR's ZIP. Prefer the GitHub artifact for byte-identical PR-source delivery.

# LD02-COHORT-001 / R1 — exact-source multi-session technical playtest funnel

**B01-13 HUMAN PLAYTESTING, PASS17/WAVE07.** An executable, offline, source-pinned Python stdlib analyzer for three or more **actual operator-recorded technical** sessions of the previous [LD02-OBS-001/R1 Draft #426](https://github.com/C8LyNn/RoboRougelike/pull/426). Its new, distinct capability is **group-level metrics and time-to-task across sessions**, not single-session observation, screenshot comparison, Unity gameplay, a mock browser or a new consent database. Every session file is read from physical disk with the **original unmodified #426 verifier**. The analyzer does not play or control the game.

## Exact source dependencies and use

- Source parent: frozen #426 HEAD `5a2420b479f2e8afad3d1f6cf3fff059243ec39f` / `Research/B01-13/P17D/ld02_observer.py` actual SHA256 `61f1e65deaaac3f670850a18e1a7011d7b8cb22a7640a383b5103fcfffa1a516`, Git blob `010faa232cdb311af634f2248092fbe0891154ea`. Source loaded ONLY after an exact physical SHA256 check; modified original fails closed.
- This W07 PR is a **stacked draft** onto `research/b01-13-pass17-wave06-ld02-observer` at the frozen parent SHA. It changes ONLY new `Research/B01-13/P17E/**` files plus its dedicated workflow. Never mutate or squash into parent #426 or edit `Assets/**`.
- Requires **Python 3.11+ stdlib only**, a valid original executable to hash, its **source SHA claim**, and **3–1000 unmodified, completed, technical JSONL** session files in a **private non-Git directory**. Directory may have only `session_name.jsonl` files; junk files, symlinks, incomplete/tampered logs, mixed source SHA, mixed binary SHA, dry-run/human mode, and filename spoofing are rejected. Full game build provenance and real operator observation are not cryptographically attested by this tool; verify externally.
- Sessions must all be collected from **the same Player binary and game source snapshot**, and `source-sha` must match each header. The Player executable SHA256 pins **one physical EXE only**; it does **not** verify other Unity build data or proves gameplay occurred.

### CLI — real local data, never commit it

```bash
python Research/B01-13/P17E/ld02_cohort.py \
  --sessions-dir /private/LD02/sessions \
  --player-file /private/LD02/Player.exe \
  --source-sha 0123456789abcdef0123456789abcdef01234567 \
  --output /private/LD02/new_cohort_summary.json
```

Exit **0** only after all original session files pass the unchanged parent verifier and group JSON is created exclusively (no overwrite) with `fsync`. Exit **2** is FAIL_CLOSED. Output contains group-level FIGHT, GARAGE, REPAIR and CHECKPOINT reach/completion/abandon/interruption, number of hints/blocked events, full four-stage completion count/rate, and the median completed duration **only when at least three sessions actually completed that stage**. Otherwise that median is `null`. The report includes group sample size, operator-supplied source and actual EXE SHA256; it never includes individual session IDs, paths, free text, consent refs or raw event timelines. No screenshots or network calls.

**Important interpretation:** the counters are operator observations rather than verified gameplay telemetry. With just three sessions, aggregate rates are not inherently anonymous; therefore **human mode is deliberately rejected**, even when the source contains consent references. The tool cannot independently verify whether a `technical` session was actually conducted; supervisors must check original game build, logs and operator attestations. `human_participants_verified=0` is a capability limitation, not a claim that all real-world studies have zero people.

## Executable QA, limits, rollback

```bash
python -m py_compile Research/B01-13/P17E/ld02_cohort.py Research/B01-13/P17E/test_ld02_cohort.py
python Research/B01-13/P17E/test_ld02_cohort.py -v
```

**30 executable original test cases** create and modify real synthetic-file fixture sessions using the exact #426 `add_row` and `read_rows`, then run actual W07 aggregation. Positive stages, cohort medians, group missingness, fail-closed tamper/truncation, mixed source/exe, human refusal, test-only dry-run exclusion, no-overwrite physical JSON, filesystem privacy and full subprocess CLI smoke. Test fixture EXE is expressly **synthetic non-Unity data**. Native Unity/Player/PhysX and genuine participants **NOT_RUN**. Windows NTFS ACL and junction end-to-end security **NOT_ATTESTED**; do not use this data-processing tool on actual participant evidence.

**B01-40 receiver:** source hash / 5-file scope / parent dependency / rollback and no conflict with Codex. **B01-60 independent QA:** rerun *original* 30 tests on both Ubuntu/Windows exact SHA, independently inspect negative cases, SKIPs and output privacy. **B01-00:** only after acceptance consider offline use for non-human technical facilitator tests; no `CODEX_SENT`, no game code import. **Rollback:** leave draft unmerged and discard isolated P17E branch; private JSONL and cohort reports require separate operator retention/withdrawal handling, not automatically deleted by git. Original #426 and #363/#389 frozen sources remain unmodified.

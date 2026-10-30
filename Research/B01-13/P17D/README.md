# LD02-OBS-001 R1 — timed operator observation of an actual LD02 playtest

B01-13 Human Playtesting / PASS17 WAVE06. This is a **working offline event recorder and strict replay validator**, not a browser simulation, not a game mechanic, not a screenshot verifier, and not a Unity patch. Its new purpose (different from AIM-OBS/DELTA/VAULT and HPT16 GBC01) is **per-task timing and intervention measurement for fight → garage → repair → checkpoint** in a *real* Player operator session. The program never starts or controls Unity. All progression signals are human/operator-entered observations, not game telemetry.

## Run — private operator computer

Requires Python 3.11+ (stdlib only) and a local actual Player executable file. Use a **private directory outside every Git worktree**, with real Player executable bytes to hash. An EXE hash alone does not authenticate its entire build or prove Unity runtime.


```bash
python Research/B01-13/P17D/ld02_observer.py capture \
  --root /PRIVATE_LD02_OBSERVATIONS --session technical_001 \
  --player-file /path/to/REAL_GAME.exe \
  --source-sha 0123456789abcdef0123456789abcdef01234567 \
  --mode technical
```

During an actually witnessed run the operator types `start FIGHT`, `hint FIGHT`, `blocked FIGHT`, `complete FIGHT`; then `start GARAGE` and complete/abandon, then `start REPAIR`, then `start CHECKPOINT`, and finally `end`. Use only when actually observed. Any task can be `abandon`ed and subsequent tasks are `NOT_OBSERVED` until started; `end` with active stage records `INTERRUPTED`. Invalid transitions are rejected. Each accepted action is immediately fsynced in an exclusively-created private `session.jsonl` with a SHA256 hash chain and monotonic millisecond clock. No free text, participant name, audio, pictures or screenshot storage.


```bash
python Research/B01-13/P17D/ld02_observer.py validate \
  --log /PRIVATE_LD02_OBSERVATIONS/technical_001.jsonl
```

Validator exits `0` only if the hash chain, stage ordering, schema and explicit `end` validate. `2` means FAIL_CLOSED. JSON output includes task status, measured timings, hint/blocked counts and actual local executable SHA-256. **Counts are operator-coded observations**, not validated gameplay success. SHA256 hash chaining detects accidental/unsophisticated edits, **not an adversary who recomputes the chain**. No upload or network activity; keep actual study logs private, never commit them. Never export participant-specific evidence into CI.

## Gates, limitations and source ownership

`--mode dry-run` (default) is strictly for artificial records; `--mode technical` is a real technical run without participants. `--mode human` requires `--approval-ref` and `--consent-ref`, which are opaque **local nonidentifying references**. This guard only checks that references are provided — **it cannot verify HQ approval, identity, or real informed consent**. Do not use human mode until HQ GATE D, a lawful approved study and actual participant consent are independently checked. All tests here use synthetic data and **zero real participants**.

The elapsed clock uses Python `time.monotonic_ns` in one capture process. A power failure or missing `end` creates a deliberately invalid/incomplete report. Never silently infer a completed run. Console input requires a separate facilitator; no background keylogger, player automation or shared gameplay controls. System time/clock cannot be independently attested by the program. Windows NTFS ACL/junction behavior is **NOT measured**, and Windows file creation is not a guarantee of private ACL; store human data only on an operator-approved volume with explicit secure ACL and retention/withdrawal SOP. No secure erase guarantee, no cloud backup deletion.

Pinned base: `main@5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`. Exclusive scope is `Research/B01-13/P17D/**` plus dedicated `.github/workflows/b01-13-p17w06-ld02-observer.yml`. **No Assets/**, canonical Codex, shared CI, game scripts or prefab edits. New Draft PR not automatically authorized for merge.

## Reproducible test and receiver


```bash
python -m py_compile Research/B01-13/P17D/ld02_observer.py Research/B01-13/P17D/test_ld02_observer.py
python Research/B01-13/P17D/test_ld02_observer.py -v
```

31 executable stdlib tests: 4-stage success, abandon/interruption, hinted/blocked counts, transition rejection, hash/sequence tampering, payload privacy, real physical JSONL fsync, actual dummy-executable SHA binding, interrupted writer, existing file no-overwrite, physical Git root and symlink denial, POSIX permissions, human approval-reference gate. A binary fixture in tests is explicitly fake; there is no Unity/Player test here. Windows symlink/ACL tests are not a substitute for native operator signoff.

**B01-40:** independently inspect exact source SHA256 manifest, path scope, base, rollback; report `G0`. **B01-60:** run source and CLI tests on immutable PR HEAD independently; evaluate negative/size/privacy failures and Windows skips accurately; report `G1-SOURCE` only. **B01-00:** decide whether an approved actual Player operator should use this tool; no game import or Codex ACK implied. **Rollback:** do not merge Draft; remove P17D tool from operator checkout; treat any private session JSONL under operator retention/withdrawal policy (not automatic deletion), leave frozen PR source unchanged after offer.

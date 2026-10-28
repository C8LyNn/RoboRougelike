# B01-50 — PERF-QA-01: PresentMon 120-second receiver fix

**Scope:** isolated QA-only Python. **NOT** a Unity gameplay package, native test, real FPS result, or Codex integration. Published by the profiling owner for repeatable use by the swarm. Never install into canonical `Assets/**`.

## Why this exists

The PASS 13 `presentmon_bridge.py` records **end-of-interval** times: 120 synthetic intervals of 1,000 ms produce `time_ms` values from 1000 to 120000. The PASS 11 analyzer treated `last - first` as duration and incorrectly rejected the **valid 120000 ms** handoff as only 119000 ms. `analyze_frametimes.py` now counts **all** `frame_ms` intervals for `tool=PRESENTMON`, verifies that `time_ms` is the cumulative sum to within 0.05 ms, and preserves the existing 120000 ms minimum. Other telemetry tools retain end-minus-first timestamp semantics.

## Execute exact boundary tests

Python 3.10+ and the standard library; no Unity, external downloads, Windows PC, credentials or real game data needed.

```sh
cd Tools/B01/B01-50/PERFQA01
python -m unittest -v test_receiver.py
```

Required synthetic regression outcomes:

| Intervals of 1,000 ms | Expected |
|---|---|
| 119 = 119,000 ms | `INSUFFICIENT_DISPLAY_DURATION` (FAIL-CLOSED) |
| 120 = 120,000 ms | accepted by bridge **and** analyzer; `runtime_pass=false` |
| 121 = 121,000 ms | accepted by bridge **and** analyzer; `runtime_pass=false` |

Also exercises missing display intervals, tampered cumulative times, zero-time forgery, non-PresentMon timing, missing GPU/GC and synthetic provenance. All rows in unit tests are **artificial controls**, not frame captures from RoboRougelike. `DUMMY_META.authorization_url` is explicitly a synthetic sentinel; it is not an HQ authorization.

## Raw capture conversion (when a genuinely authorized host exists)

```sh
python presentmon_bridge.py --raw ORIGINAL_PRESENTMON.csv --scenario G02 --run R01 --out normalized.csv
python analyze_frametimes.py --csv normalized.csv --metadata metadata.json --output inspection.json
```

Metadata must identify the immutable original **historical** release source SHA and ZIP SHA expected by this version of the analyzer. A later Codex game build needs a separately reviewed **source-pinned** receiver revision; never change pins silently or falsely mark a new build as the old one. The bridge requires a single actual process/swapchain and real `MsBetweenDisplayChange` intervals of at least 120 s. The analyst must independently verify captured raw hashes, provenance, authorized environment and platform logs before using any results. The generated `WINDOWS_CAPTURE_REVIEW_REQUIRED` or `SYNTHETIC_TEST_ONLY` classification is **not** a native PASS and never upgrades to `runtime_pass=true` automatically.

`MsBetweenDisplayChange` is displayed-frame pacing, **not** Unity Main Thread, Render Thread, GPU profiler marker, GC allocation or memory. Those unavailable fields are left null. A compliant profiling session and B01-50 independent review are tracked by the existing [Codex CV-12 queue](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6463896564), pending real environment acknowledgment; engine work remains `NOT_RUN` until authentic execution.

## Provenance and ownership

- Original B01-50 PASS14 repaired analyzer SHA-256: `9808a341e6dfecdf76fb5bb9de7683908013cf0e1f2ba6c55a14813c57811728`.
- Original PASS13 unmodified bridge SHA-256: `fcc5bd643b0d314711dfa78ce151ad18e669b4f1f1c9693b38a49aa4290b5538`.
- PASS14 source tool **RED→GREEN** report: https://github.com/C8LyNn/RoboRougelike/issues/297#issuecomment-6463916329
- PASS15 adds only the 121-second explicit positive to the published portable regression test, with historical inputs and failure behavior preserved.

**Source ownership:** B01-50 profiling QA only. Codex game code / CHL-003 / PARTSIZE sources are untouched; no new gameplay package, merge, release, G0/G1/G2 transition or Codex acknowledgment is implied.


## PASS15 continuation: original CLI intake & CSV integrity hardening

New actual CLI integration tests cover 119, 120 and 121 seconds, invoking both
`presentmon_bridge.py` and `analyze_frametimes.py` in a subprocess; results
remain synthetic QA test controls, **never measured game FPS**. A separate
original-source defect was reproduced RED: `csv.DictReader` silently accepted
a raw row with an undeclared extra cell, and duplicate required headers could
silently overwrite values. This version fails closed with
`MALFORMED_RAW_ROW` / `DUPLICATE_RAW_COLUMNS`, while accepting legitimate
additional *named* PresentMon columns.

Run on **either** Windows or Linux with Python 3.10+:

```sh
python -m unittest -v test_receiver.py test_cli_ingestion.py
```

Source-pinned local Linux test and artifact receipt:
[evidence/PASS15_LINUX_CLI_QA.json](evidence/PASS15_LINUX_CLI_QA.json).
**Windows run is NOT_RUN** and requires a separate genuine Windows runner;
the portable test itself is not a cross-platform PASS. Do not modify shared
GitHub Actions workflows from the B01-50 specialist scope.
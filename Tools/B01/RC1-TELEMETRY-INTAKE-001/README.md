# RC1-TELEMETRY-INTAKE-001 / B01-10 W13 — real source evidence before game-design claims

**Source-only read-only tool**, no Unity Assets, active Codex host code or ownership mutation. Complements [B01-10 GAMEPLAY-EXPERIMENT-001 #255](https://github.com/C8LyNn/RoboRougelike/pull/255); does not replace it. The original #255 schema cannot truthfully be filled from a short RC1 physics drive trace without inventing win rates, garages, salvage decisions, seed-paired human sessions and builds.

## Original external native evidence (NOT our own Unity test)
Pin to Codex [RC1 #248](https://github.com/C8LyNn/RoboRougelike/pull/248) HEAD `5c29e8e28e7b34fedf0c992fca49451828b02314`, actual target code `86eb7bb77e6bfc7787f781ea1ccda547b8a4f643`; original native Windows x64 `Docs/B01_RC1/FinalMapgen1080/drive.csv` (git blob `f486527f62feb552ee71e72cb03d7bf14bb3614b`) and `rc1-native-result.json` (git blob `97e4bbcde23e755747cb5efcf96bbca31f7dcf7a`). The original source says API-controlled, not human, with *real* Rigidbody physics, contact observations, shells and HP loss. Samples copied byte-for-byte and verified by Git blob hash inside this package.

## Run (Node 22, stdlib only)
```bash
node Tools/B01/RC1-TELEMETRY-INTAKE-001/test.mjs
node Tools/B01/RC1-TELEMETRY-INTAKE-001/ingest.mjs
node Tools/B01/RC1-TELEMETRY-INTAKE-001/ingest.mjs Tools/B01/RC1-TELEMETRY-INTAKE-001/manifest.json Tools/B01/RC1-TELEMETRY-INTAKE-001/samples/drive.csv Tools/B01/RC1-TELEMETRY-INTAKE-001/samples/rc1-native-result.json --output /tmp/rc1-observed.json
```
Code verifies original Git SHA-1 including blob length, expected exact pinned paths+source SHA, status/method/checks, monotonic finite CSV, realistic contacts, reported vs sampled displacement. Fail closed if modified, confused with human data or missing evidence. 21 explicit positive/negative tests; mutations of payload must be rehashed to prove checks are not merely cryptographic. CI produces receipt artifact containing derived *observations only*, not fabricated player event rows.

**Actual available evidence:** 33 sample rows around 3.66 seconds, full native check claims 185 support/contact samples, external Codex native Windows gameplay observation. Samples are **not** full-duration physics data or a measure of completed human challenge. No native playtest, B01-60 independent G1 or G2 here.

## Operational next gate
B01-60 or Codex can record real **human** runs in a future approved telemetry producer with authenticated build/rig/seed, event IDs, deterministic A/B budget and actual 3 build × 3 challenge × 100 seed matrix. A source file whose JSON claims `origin:native_player` alone is **insufficient**. Capture raw input/visual evidence or signed operator metadata, immutable source checksum and record actual control. Only then bridge to #255; no automatic conversion here.

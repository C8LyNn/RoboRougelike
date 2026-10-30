# B01-11 P17/W13 UXCAPTION-001/R2 — original source RED→GREEN

## Why this was a real bug in original R1
Original `CaptionTrack.PriorityOf(CriticalWarning)` and `PriorityOf(PartDetached)` both returned `CaptionPriority.Urgent`. With finite capacity 1-32 and newest-first tie sorting, new `PartDetached` events could replace an older live critical warning; or with `visibleSlots=1` it was omitted from `Visible()`. This is accessibility and safety signaling priority inversion, not a native audio engine failure.

## Reproducible test-first RED
- Historical R1 frozen SHA `b3e3c72991b4701fc6e08fa49c109ae0d7294418`.
- 12 adversarial C# tests introduced with source unchanged, final RED test-only SHA `c27aef4d9e0362a8cfe18a6c169a207600ea8b6e`.
- [Observed real GitHub Actions #41081844186](https://github.com/C8LyNn/RoboRougelike/actions/runs/41081844186): **153 PASS / 9 FAIL on Ubuntu and Windows**, logs verified. Includes dangerous priority eviction at capacity=1 and 3, 32+ event detachment flood and critical ranking below later part cues. No test mock: project directly includes original `Runtime/CaptionTrack.cs`.

## Fix and validation
- Original C# source fix `39e1e921d63e5e092652d5efc01944a0cd361318`: three stable priority tiers, eviction from lowest eligible priority only, critical-first visible output, both >=Urgent TTL=6000ms, explicit lower-priority rejected outcome and non-replay sequence recording.
- Old test previously expected a newer PartDetached before CriticalWarning in `Visible()`; intermediate run actually returned **166 PASS / 1 FAIL**. This stale assertion was replaced at `7c0c08a2cc84635772379d6a9254a853d7fac90a` to require Critical first.
- Test source at `7c0c08a2cc84635772379d6a9254a853d7fac90a`: **167 compiled original C# and locale validation assertions** per OS; includes prior 150 original PL/EN, sector and lifecycle tests plus 12 RED and 5 direct new priority contract tests. Record final exact-SHA source CI separately; never call CI-02/03 preflight-only attempts native PASS.
- Rollback: earlier SHA is intact; remove R2 from same isolated B01-11 branch or drop research subsystem. No canonical Unity gameplay changes.

## Gate truth
This source-only accessibility package **cannot prove visible captions, real Unity audio events, native Unity G1 or human playtest**, as no Unity adapter is installed and activation currently blocks generic CI. Independent B01-40/G0 and B01-60/source QA PENDING; Codex NOT_OFFERED/ACK_PENDING/NOT_INTEGRATED; no main/develop merge.

# B01-00 / P17 W13 — single source-only handoff to Codex Inbox #79

## Original external evidence read and offer actually posted

- Original immutable selected-only producer [Secure V2 R2 PR #510](https://github.com/C8LyNn/RoboRougelike/pull/510), `d94400d95d13db7f27e1533df05c9292d5380431`, B01-23 frozen.
- Original producer inner ZIP **35 members / 99,666 bytes / 14 GUID / 3 asmdef** and SHA256 `d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387`. Independently verified by [B01-40 G0 #521](https://github.com/C8LyNn/RoboRougelike/pull/521) SHA `a19724328cfda8a19f694cd84de899da214b3163`, original [physical CI #41073361172](https://github.com/C8LyNn/RoboRougelike/actions/runs/41073361172) SUCCESS Windows and Ubuntu, 13/13 physical receiver negative cases each OS. Exact G0 verdict `G0_ACCEPTED_SOURCE_ONLY`.
- Independently executed source transaction regression [B01-61 #517](https://github.com/C8LyNn/RoboRougelike/pull/517), QA SHA `12ceba268a3828d28dc9e99a0a36ec797599c8c1`, original [CI #41070443412](https://github.com/C8LyNn/RoboRougelike/actions/runs/41070443412) SUCCESS Windows/Ubuntu 167/167 original-source synthetic Unity API assertions each OS, **not Unity/PhysX G1**.
- **One original new source-only offer** actually posted to [Codex Inbox #79 comment #6526629723](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6526629723). It forbids integration; `#511/#494=ALT_HOLD`. Source is not modified by this offer; no real Codex ACK or integration reported.
- Independent [B01-62 save/ID preflight #527](https://github.com/C8LyNn/RoboRougelike/pull/527) identifies NO durable detached+consumed-ID persistent snapshot contract in frozen #510. `SAVE_RELOAD_HOLD`.
- Original #510 `package.py::rollback` reportedly checks owned files but may **delete foreign empty subdirectories** via `shutil.rmtree`: [B01-23 #525](https://github.com/C8LyNn/RoboRougelike/pull/525). Original unsafe rollback is NOT approved for an occupied receiver. Independent W13 B01-40/62 adversarial replay and safe receiver-only wrapper must be approved separately.

## Actual original W13 source tests and CLI — cross-OS

- Source/fixture/workflow commit `9e5aee6f2c11899263ee0187ad69043a995dcde0`.
- [GitHub Actions #41082151864](https://github.com/C8LyNn/RoboRougelike/actions/runs/41082151864) **COMPLETED SUCCESS**:
  - Windows 2025 original source job #124153770287: **172/172 PASS**, `Ran 172 tests`, `OK`.
  - Ubuntu 24.04 original source job #124153770616: **172/172 PASS**, `Ran 172 tests`, `OK`.
  - Original W13 independent `test_offer_protocol.py` adds **49** positive and adversarial tests to previous 123. Cases reject false native G1, Save/Load accepted, rollback allowed, canon migration done, CODEX_ACK, G2, integration or merge, mismatched source/G0/QA/offer/ZIP SHA and wrong recipient comment.
  - On both real hosts the original `offer_protocol.py ... --require-structural-offer` executed against the grounded offer record, returned `source_only_offer_record_consistent=true` and `verdict=SOURCE_ONLY_RECORD_FOR_MANUAL_VERIFICATION`, **but also `code_offer_authenticated_online=false`, `independent_g0_authenticated_online=false`, `native_unity_g1=NOT_RUN`, `codex_ack=UNCONFIRMED`, `g2=HOLD`, `integrated=false`**.
  - Original W12 real online GitHub PR #427 and Windows/Ubuntu workflow metadata check continues PASS on both OS.
- Exact final manifest HEAD CI must be separately confirmed before claiming the current final source HEAD GREEN; this evidence file records the preceding implemented functional source revision.

## Safety boundaries and rollback

The W13 CLI **validates only structure/consistency of a declarative JSON snapshot**, not any identity or GitHub metadata online. It has no network access and performs no GitHub/Unity writes, merge or transfer. Separate `github_live.py` verifies the earlier producer source workflow's read-only GitHub run/job labels; it also does NOT verify Codex ACK. No untrusted receipt can be promoted to authenticated approval.

**Owner scope:** only existing B01-00 `Tools/B01/B01-00/W09/RECEIPT-LATCH-001/**` and its source workflow. **DO NOT TOUCH** frozen producer #510, alternatives #511/#494, B01-40/60/61/62 QA files or Codex canonical `Assets/**`. Rollback of this coordination CLI: discard its own unmerged code checkout and disposable local output; keep already posted inbox, immutable source ZIP/G0 and QA receipts intact.

**QA receivers:** B01-40 provenance/physical migration and actual safe rollback, B01-61 independent source regressions, B01-62 Save/Load stable ID contract, B01-60 licensed native Unity/PhysX G1, B01-00 coordinator maintains source-only ACK inventory. **G2 HOLD / NO ACK / NO INTEGRATION / NO MERGE**.

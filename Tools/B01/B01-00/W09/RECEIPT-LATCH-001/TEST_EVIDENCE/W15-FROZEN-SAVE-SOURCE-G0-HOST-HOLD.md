# PASS17 / WAVE15 — selected #556 corrected source G0, NOT a Unity host Save/Load

## One selected source, not a third contract or duplicate adapter

- [Frozen gameplay #510](https://github.com/C8LyNn/RoboRougelike/pull/510) exact original SHA `d94400d95d13db7f27e1533df05c9292d5380431`, unchanged.
- [**Only frozen Save contract #556**](https://github.com/C8LyNn/RoboRougelike/pull/556) exact original SHA `33094f154c7b91119b36d58aab285e6e83c5de57`, v2.0.0 corrected #549 lineage. Original old #549 and #544 SECURITY_RED, competing #554 HOLD, alternative gameplay #511/#494 HOLD. No source blending.
- [Independent physical Save source G0 #560](https://github.com/C8LyNn/RoboRougelike/pull/560) exact QA SHA `1a218df9e287a8a254b7d84c228c962ab232b9b5`: **ACCEPTED_SOURCE_ONLY**, exact 67,581 B/17-member inner ZIP SHA256 `20c3c17c252f084014e9766010ab4651770a41f73ce534fc2aa7fd2edc697060`, source ZIP byte parity Win/Ubuntu and B01-40 rollback/symlink/foreign-empty-dir tests in disposable receiver.
- Independent source regression [#563](https://github.com/C8LyNn/RoboRougelike/pull/563) QA exact `42aa590a2f6b08e33886027ea452ef3392a2f2c0` was executed against the *original corrected #556* source SHA and original W06. Original #549 reentrant pointer swap RED → corrected #556 source GREEN. Additional [#570](https://github.com/C8LyNn/RoboRougelike/pull/570) QA SHA `413bee4c232145be4cdf5745088fe1538a5f421b` was **closed unmerged** to avoid duplicate streams; its actual tests of exact #556 source are retained as source QA, not integration.
- [Existing Codex Inbox #79 source-only offer #510](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6526629723) had no Codex ACK. B01-40 separately posted an exact #556 source-only G0 receipt to #79. No new B01-00 offer is required, and no assistant comment can be called Codex ACK.

## Actual B01-00 code change: update stale W14 intake to selected corrected v2

Existing original `save_admission.py` gains `--selected-v2` and `audit_selected_v2()`, not an additional Save codec or host bridge. New immutable source-only fixture `save_contract_w15_selected.json` tracks exact #510/#556 SHA, B01-23 owner choice, B01-40 #560 accepted G0 and exact ZIP, B01-61 #563/#570 independent source regressions and actual closed/unmerged history, excluded #494/#511/#544/#549/#554, old #550 vulnerable source blob, and required host-runtime block states. `test_save_selected_v2.py` supplies **62 real executable positive/adversarial tests**. Total original unit suite 287.

The gate rejects attempt to re-select #549/#554, stale source, zip SHA drift, false B01-40 G0, incorrect independent QA source SHA, fabricated earlier RED, fake adapter IDs, false Unity save/reload, native PhysX green, destructive rollback approval, Codex ACK, integration, merge or G2. A structurally coherent source-only result yields ONLY:
```json
{
  "selected_save_pr": 556,
  "source_g0_recorded": "ACCEPTED_SOURCE_ONLY",
  "source_claims_authenticated_online": false,
  "unity_host_adapter": "NOT_IMPLEMENTED",
  "unity_cold_save_load": "NOT_RUN",
  "native_physx_g1": "NOT_RUN",
  "codex_ack": "UNCONFIRMED",
  "g2": "HOLD",
  "integrated": false,
  "release_allowed": false,
  "verdict": "SOURCE_G0_RECORDED_HOST_GATE_HOLD"
}
```
This original Python gate merely checks a declared file offline: GitHub PR facts were examined externally by coordinator, not magically verified through this CLI. Original source tests or physical package acceptance **do not** prove the frozen #510 private `AtomicDockLedger` can be reconstructed in Unity. The actual atomic Unity-facing host Save/Load bridge is B01-23/62 scope, **NOT_IMPLEMENTED** by B01-00. If it needs a new public hydration interface, B01-23 must publish a new owned source version with consent; no reflection/private field writes or changes to frozen #510/#556.

## Actually executed original dual-platform source tests

[Original W15 source workflow #41091363949](https://github.com/C8LyNn/RoboRougelike/actions/runs/41091363949) COMPLETED SUCCESS on W15 implementation commit `cdcb0c489be1552b5e061bae9cf3df57294e5290`:
- **Windows2025** [job #124185787187](https://github.com/C8LyNn/RoboRougelike/actions/runs/41091363949/job/124185787187): **287/287 PASS**, 0 FAIL; actual W15 `--selected-v2` CLI PASS with exact selected #556 and explicit host HOLD.
- **Ubuntu24.04** [job #124185786730](https://github.com/C8LyNn/RoboRougelike/actions/runs/41091363949/job/124185786730): **287/287 PASS**, 0 FAIL; actual `--selected-v2` CLI PASS with host HOLD.
- [CI-01 #41091363986](https://github.com/C8LyNn/RoboRougelike/actions/runs/41091363986): SUCCESS. [Global CI-02 #41091363862](https://github.com/C8LyNn/RoboRougelike/actions/runs/41091363862) / [CI-03 #41091363869](https://github.com/C8LyNn/RoboRougelike/actions/runs/41091363869) CANCELLED on this intermediate SHA, so **native Unity Editor/PhysX NOT_RUN**, not native PASS or engine failure.
- Exact final manifest HEAD must separately be verified against original new Actions run after manifest commit before calling final source pass.

## Boundaries and receiver

No `Assets/**`, frozen #510/#556, old #544/#549/#554, B01-40/60/61/62 authored sources, Codex canonical host or main/develop mutation. Zero merge. The only files touched are B01-00 existing receipt-latch tool and its dedicated workflow. **Receiver:** B01-23/62 actual host bridge with public ownership interfaces, B01-61 exact new-bridge QA, B01-40/62 safe transport+rollback, B01-60 authorized licensed Unity/PhysX, B01-00 only claim/ACK coordination. Rollback discards the isolated unmerged B01-00 tool and disposable reports, not frozen source or accepted QA.

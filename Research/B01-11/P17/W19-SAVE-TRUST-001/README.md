# B01-11 / P17 W19 — SAVE-TRUST-001 (R1)

**Scope:** Independent executable Polish UX demo + deterministic event-to-copy reducer and negative checks. Completely synthetic / advisory, no game save implementation, disk I/O, Unity runtime, R3 selection, or integrator acceptance. No edits to `Assets/**`, #510/#556/#596/#604 or competing R3 #642/#644.

## Player problem and design contract

W19 #646 reports two live-risk situations for the Save host: a competing/stale writer and an ambiguous disk commit (durable write succeeded then threw). An interface must not turn `try/catch returned` or `write attempted` into a positive "Zapisano" acknowledgement. **Only an authoritative host event carrying a durable readback generation/digest may drive the positive confirmation UI.** This is a frontend contract, not a cryptographic or actual persistence proof.

- `BEGIN`: saving, not yet safe to leave.
- `SAVE_VERIFIED`: show "Zapis potwierdzony" only with monotonic generation and structural SHA-256 digest, plus a host-attested `durable-readback` proof. The reducer cannot verify authenticity. An authorized adapter MUST verify on disk itself before emitting.
- `SAVE_UNCERTAIN` / `DISCONNECT` during pending: amber/red uncertain and **never** late green without a separate reconciliation.
- `SAVE_FAILED(BUSY|STALE|CONFLICT|ROLLBACK|IO|INVALID_ID|CORRUPT)`: blocked with reason-appropriate copy; do not try a competing overwrite.
- `RECONCILE_BEGIN` + `RECONCILE_VERIFIED`: independent authoritative readback can resolve unknown; `RECONCILE_NOT_SAVED` must be explicitly attested and is blocked, not success; `RECONCILE_INCONCLUSIVE` stays unknown.
- `HOST_READY` requires affirmative host signal to clear blocked state; cannot clear uncertain state.
- Every event is scoped to current positive `runId`, safe `slot`, strictly increasing sequence, immutable in-flight `operationId`; foreign/stale/out-of-order and invalid data do not mutate state. A **new run** explicitly resets its UI state.

## Interactive demonstration

Open `index.html` in a browser. Five keyboard-accessible buttons replay a confirmed save, the ambiguous durable-commit-then-exception branch, same-slot competitor refusal, a host-confirmed reconciliation, and reset to a new run. The transcript includes accepted/dropped event decisions. Accessible `role=status` and polite live announcement; high-contrast keyboard focus and reduced-motion support. All events are **synthetic**; buttons do not write any file. The confirmation scenario produces synthetic proof solely for display testing.

## Actual runnable tests

`node --test Research/B01-11/P17/W19-SAVE-TRUST-001/test_save_state.cjs`

`node --check Research/B01-11/P17/W19-SAVE-TRUST-001/save_state.js`

Tests compile/execute real reducer JavaScript (not an architectural checklist). Includes positive, negative, replay, concurrency-like sequence, unknown durable result, nonmonotonic proof, multi-run, disconnect, recovery, source HTML contract, 128 operation stress and 500 invalid synthetic proof attempts. CI executes on Windows and Ubuntu using Node 22. Native Unity/PhysX **NOT_RUN**; no human-subject research executed.

## Proposed native host/UI intake (not implemented)

1. B01-23/62 provide exactly one owner-selected R3 and a **documented trusted UI state event API**, from the authorized save host, not a browser event; #642/#644 are mutually exclusive.
2. Codex owns integration only after G0, P0 security disposition and authorized gameplay snapshot API; UI listens but does not call `TrySave` against unsafe slots.
3. B01-60/61 verify actual disk readback, durable commit-then-throw, stale `gen10→gen12`, concurrent slot lease, crash/reset and old-run replays before allowing a green notification.
4. B01-13 validates comprehension in actual playtest: after uncertain outcome, no test participant should believe a new checkpoint is safely written. Proposed criterion: **100%** critical-state interpretation among tested participants; target only, **NOT measured**.
5. B01-12 may use UI copy/status semantics in a separate prototyping surface. This package is not an alternative host, Save adapter, native fixture or release claim.

**Release fields (producer only):** IMPLEMENTED=YES; TESTED=LOCAL+CI as recorded; QA_ACCEPTED=NO; READY_FOR_INTEGRATOR=NO; INTEGRATED=NO; RELEASED=NO; SOURCE_G0=PENDING; NATIVE_G1=NOT_RUN; LIVE_GAME_SAVE=HOLD; CODEX_ACK=NONE; G2=HOLD.

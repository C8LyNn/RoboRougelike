# B01-41 — P17/W29 real selected Save V3 + physical Rig[] + V7 original C# regression

**Scope:** This is a new **executable original-source composite QA** on a branch based on the actual current playable [Codex W27 #824](https://github.com/C8LyNn/RoboRougelike/pull/824) exact `ed8cf4bcc131688b9a93968a89683d2496586683`, **not** on stale main or an alternate player implementation. It adds only B01-41-owned tests/CI/runbook. It does NOT install another Save writer or any Unity gameplay package.

## Why this is useful and different from #847

- Corrected real #826 [`99fb08619c78b3834bb4c0450499babf991f218a`](https://github.com/C8LyNn/RoboRougelike/pull/826) already independently compiles alone for netstandard2.1 (B01-61 #847). W29 tests the cross-system boundary using the **original physical #792** (`16cb2a7b5158605b15191b6074f9e392e0d1ebd3`) and exact original #780 V7 economy together with the one chosen new #826 production writer/session.
- The original B01-62 #822 [physical two-case QA source](https://github.com/C8LyNn/RoboRougelike/pull/822) is recovered by **exact original immutable SHA and Git blob OID**; only its disposable *test fixture* substitutes the previous public type names `...002/V2` for the equivalent new `...003/V3`. Those are **not alterations to source under test**, and this harness is separate from the producer's own source tests.
- A separate original #792 UnityEngine managed shim is used for C# source-level Rigidbody staging. Its original test-only helper, also pinned at #822, repairs fake-parent Destroy behavior only in scratch. **This is NOT Unity PhysX**.
- Builds the selected **genuine producer source files simultaneously as C#9 `netstandard2.1`**, then executes two real combined OS-disk physical session scenarios under net8. Both original V7 binary section source files and frozen original #729/.owner, #556 world codec, #510 AtomicDockLedger/W06 are compiled from exact immutable originals, not invented stubs (except the declared UnityEngine synthetic shim).
- Pins the original **#750 R5 ColdRestart PlayMode fixture** exact source SHA, but does **not** run native Unity R5 tests. That stage remains `NOT_RUN`.

## Safety and limitations

This gate uses **one** selected #826 writer in its compiled runtime. `#797` is superseded, `#787` is reference/authority only (do not compile an independent raw Store), alternatives #749/#773/#753/#741 excluded. Save physical disk tests write only disposable CI temp directories; the Unity Editor and persistent user Save paths are never used. Preexisting output directory is refused even when empty. Old source SHA or any changed byte is fail-closed. Authoritative whole-game game-facing R3/Win/Loaner binder does not yet exist in this QA and is not claimed.

**Test classes:** 10 original Python mutation/selection controls per OS; true C#9 netstandard2.1 **cross-source ABI compiler**; two actual adapted original #822 managed/disk physical session tests per OS. **Full integrated source-G0, physical Unity G1, full Player/GUI/load regression and release remain pending.** This is additional independent evidence, not release authorization.

## Operator handoff

1. **B01-40/61**: review exact #826 → #792/#780/#750 original Git selection, full Unity .meta GUID/asmdef/YAML closure and source transport G0, including one writer and rollback/dirty path denial, independent of this narrowly compiled source corpus.
2. **B01-23/B01-20/B01-62**: supply and bind one real native R3/phys Rig[] world+economy Save V3 lifecycle, cold process restore, original #510 AtomicDockLedger authoritative ownership, real one-shot loaner.
3. **B01-60**: perform actual licensed Unity 6000.3.14f1 full Edit/Play, R5 physical collision/rollback and Windows x64 Player cold-Save/Win, collect Editor.log/NUnit XML/Player.log/ZIP SHA.
4. **B01-00**: approve integration/hand-off, never on the basis of a source-only green gate or an unresolved economy #812 product softlock.

`IMPLEMENTED=YES_CI41_QA`; `TESTED=AWAIT_ACTUAL_EXACT_SHA_CI`; `FORMAL_G0=HOLD`; `UNITY_NATIVE_G1=NOT_RUN`; `PLAYER=NOT_RUN`; `CODEX_ACK=NONE`; `INTEGRATED=NO`; `MERGED=NO`; `RELEASED=NO`. Rollback: close only this Draft and delete only its disposable CI artifacts/branch.

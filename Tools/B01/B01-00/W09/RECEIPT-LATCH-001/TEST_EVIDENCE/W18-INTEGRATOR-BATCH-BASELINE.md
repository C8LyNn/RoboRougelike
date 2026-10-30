# B01-00 / PASS17 WAVE18 — standard vs polished project-local handoff experiment baseline

## What actually happened / audit boundaries

This is a **versioned production deliverable**, not a claim that Codex reviewed/accepted anything. Coordinator [#468](https://github.com/C8LyNn/RoboRougelike/pull/468) owns only its standalone `Tools/B01/B01-00/W09/RECEIPT-LATCH-001/**` sources and original workflow, not gameplay or final bridge. HQ [#632](https://github.com/C8LyNn/RoboRougelike/issues/632), ownership [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

**IMMUTABLE selection:** gameplay #510 `d94400d95d13db7f27e1533df05c9292d5380431`, Save schema #556 `33094f154c7b91119b36d58aab285e6e83c5de57`, B01-23 single correct bridge [#596](https://github.com/C8LyNn/RoboRougelike/pull/596) `efb2b06eafedab5469b57f163152d96aca3c5915`. Unselected [#604](https://github.com/C8LyNn/RoboRougelike/pull/604) `1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a` ALT_HOLD, never installed, cherry-picked or included in a staged combination. Original selected 23-member 81,190-byte Git source INNER ZIP SHA256 `005ac910ca9476203bc3ff1748d9198d45835abad21d97e12ad6c1bfe50484e9`. [#596 owner selection](https://github.com/C8LyNn/RoboRougelike/pull/596#issuecomment-6529388456).

## Genuine original external evidence observed at W18 intake

| Slice | Real source / exact job result | Impact / policy |
| --- | --- | --- |
| [B01-23 #596](https://github.com/C8LyNn/RoboRougelike/pull/596) | [Producer CI #41097409278](https://github.com/C8LyNn/RoboRougelike/actions/runs/41097409278): source 96/96 C# and 25/25 ZIP cases per OS | Author source production PASS only |
| [B01-61 #607](https://github.com/C8LyNn/RoboRougelike/pull/607) | [Independent #41098564548](https://github.com/C8LyNn/RoboRougelike/actions/runs/41098564548): real original #596 C# transaction 82/82 per OS | Original exact core source QA GREEN, native G1 NOT_RUN |
| [B01-40 #625](https://github.com/C8LyNn/RoboRougelike/pull/625) | [Original physical run #41101915960](https://github.com/C8LyNn/RoboRougelike/actions/runs/41101915960) finished SUCCESS both Windows/Ubuntu, safe disposable rollback and ZIP original author tests | Strong physical source proof on chosen #596; **B01-40's explicit *formal G0 decision*** separate, do not infer by B01-00 |
| [B01-62 #627](https://github.com/C8LyNn/RoboRougelike/pull/627) | [Original #41102276399](https://github.com/C8LyNn/RoboRougelike/actions/runs/41102276399) FAIL both hosts at `SOURCE_QA_FAIL:selected_dependency_guid_counts`; own checker assumes 10+14+9 GUID | **QA checker count or input mismatch**, not proof of corrupt original #596 mesh/code. Owner B01-62 must inspect physical actual source GUIDs and fix own separate QA, reconcile B01-40's PASS |
| [B01-61 #629](https://github.com/C8LyNn/RoboRougelike/pull/629) | [Original #41102254555](https://github.com/C8LyNn/RoboRougelike/actions/runs/41102254555) FAIL *before host test* at `QA_EXCLUSIVE_OWNERSHIP_CHANGED`: own workflow rename mismatch | Multi-host fixed-slot stale owner is a serious **source hypothesis, unproven by this run**. B01-61 fixes its own guard, independently runs true original #596 MonoBehaviour CLR source and publishes exact RED or GREEN. Do not call this failure verified attack |
| [CI41 #631](https://github.com/C8LyNn/RoboRougelike/pull/631) and [B01-60 #621](https://github.com/C8LyNn/RoboRougelike/pull/621) | Same persistent filename used by multiple native PlayMode fixtures (run IDs differ), based on original selected MonoBehaviour public-source inspection. Actual activated Unity Editor XML/PhysX **NOT_RUN** | Per-test isolated slot and safe own-file-only cleanup mandatory before valid native G1. Never delete user's global persistentDataPath |

**Formal G0 remains `PENDING`** until B01-40 publishes a decision on **the selected exact #596 ZIP** and the #627/possibly #630 disagreement is resolved; CI pass is not implicit authority. Native G1 `NOT_RUN`; canonical original #510 private `Rig[]/AtomicDockLedger` hydrate cannot be performed without a *new, separately authorized* public interface. Crash-atomic two-file Save+checkpoint protocol not proven. **No CODEX OFFER/ACK, no integration or release.**

## New original production item: paired real-source *feedback baseline* code, not fabricated feedback

`integration_experiment.py` + `integrator_w18_batch.json` + `test_integration_experiment.py` are **new B01-00-owned executable source files**.

- Compare **two legitimate pinned, project-local packages**: selected source [#596](https://github.com/C8LyNn/RoboRougelike/pull/596) and the independent QA source [#607](https://github.com/C8LyNn/RoboRougelike/pull/607). Each package has **two presentation arms using the same identical real PR+SHA**: **standard** (`pr, sha, purpose`), **polished** (`pr, sha, purpose, evidence, dependencies, rollback, known_blockers, receiver`). Two packages × two arms = **4 presentation arms**. This is a **paired metadata-format baseline**, **NOT FOUR independent installed code changes** and not a randomized integrator outcome. Even choosing one arm for a later real offer needs G0.
- Every package independently tracks six lifecycle booleans: `IMPLEMENTED=true; TESTED=true; QA_ACCEPTED=false; READY_FOR_INTEGRATOR=false; INTEGRATED=false; RELEASED=false`. Source tests PASS does **not** flip independent acceptance.
- Each variant's **real integrator** `ACCEPTED | REWORK | REJECTED | BLOCKED` response is currently `NOT_MEASURED`, evidence URL/elapsed seconds/retries/conflicts/regressions all **null**. Resulting acceptance yield, median time, retry rate, regression rate are **null**, not fake 0% or 0-second. No baseline causal effectiveness yet.
- Explicit blockers for #629 source hypothesis, #631 checkpoint fixture, selected #596 G0 PENDING, native Unity NOT_RUN and canonical game host HOLD. A new authorized Codex release receipt must be separately captured/verified before any metrics are reported; the offline CLI **does not authenticate** GitHub reviewers, Codex ACK, or physical ZIP bytes.
- Executable policy tests reject fake 0 regressions, fake acceptance time, false `READY_FOR_INTEGRATOR`/G0/G1/G2/ACK, spoofed source SHA, alternative #604, missing dependencies, destructive rollback, mismatched arms, and write-over-existing evidence. Original unit test suite adds **61 W18 tests** (existing 406 + 61 = **467**).

Commands, from repository root:
```sh
python -m unittest discover -s Tools/B01/B01-00/W09/RECEIPT-LATCH-001 -p "test_*.py" -v
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/integration_experiment.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/integrator_w18_batch.json
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/integration_experiment.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/integrator_w18_batch.json --require-actual-feedback
```
On a valid **unmeasured** fixture, command 2 exits **0** (structurally consistent HOLD); command 3 exits **3** (no authenticated integrator feedback). Any tampered source/claimed ACK exits **2**. CI validates original Python source on Windows2025 and Ubuntu24.04 but those tests alone are **not integration performance**. New original source is own separate change, not a duplicate Save/Load adapter.

## Integration receiver instructions after future genuine acceptance

1. B01-40/62 reconcile #625 positive original #596 physical ZIP/GUID/migration receipts with #627 failed GUID assertion; B01-40 **formally** sign SOURCE_G0 or HOLD. Source QA cannot itself authorize live install.
2. B01-61/41 re-execute original public #596 host slot contention and stale-world generation; reproduce independent failing P0 with actual original C# + exact tested SHA or classify QA guard failure. B01-23 only makes a new **versioned**, owner-approved correction on confirmed RED; keep original #596 frozen.
3. B01-60 run native original Unity Editor+PhysX only after test slot isolation, gather genuine complete NUnit XML/Editor.log/source SHA; otherwise G1 NOT_RUN.
4. B01-00 may prepare **one** verified #596 source-only OFFER in Codex Inbox #79 only if owner/physical G0 and host security conditions allow, with full frozen #510/#556 IDs, original ZIP SHA256, QA, install/rollback and exact known HOLD. An OFFER is not Codex ACK, integration or playable Save.
5. When real Codex/authorized integrator actually reviews a paired envelope, independently capture **external** time, retries, reason, status and regression evidence. Only then calculate time-to-accepted, yield and regression; compare variants on **same package+SHA** and acknowledge evaluator order/learning bias. No blanket cap on PRs — assess throughput and backlog with observed numbers.

**Rollback:** discard only unmerged B01-00 tool/report and its own exclusive output; never write to protected main/develop, Codex or game Assets, old #510 recursive rollback or any foreign empty folder.

# B01-00 / PASS17 W19 — one selected R3 lineage #642, independently reproduced P0 RED

This is original executable B01-00 source-only governance/admission work. Never edit frozen #510/#556/#596, producer #642/#644, Codex gameplay, shared project, and never merge. HQ [#646](https://github.com/C8LyNn/RoboRougelike/issues/646), selection by [B01-23 on #642 comment 6530905217](https://github.com/C8LyNn/RoboRougelike/pull/642#issuecomment-6530905217), explicit [loser #644 ALT_HOLD comment 6530907715](https://github.com/C8LyNn/RoboRougelike/pull/644#issuecomment-6530907715). The selection is **BASE R3 line ONLY, NOT ACCEPTED FOR RELEASE**.

## Original physical producer receipts — two distinct incompatible R3s

| Real field | Sole owner-selected BASE [#642](https://github.com/C8LyNn/RoboRougelike/pull/642) | [#644](https://github.com/C8LyNn/RoboRougelike/pull/644) ALT_HOLD |
| --- | --- | --- |
| Full exact original HEAD | `22cec9888047994ae523cbe7bf1b0a483d64cec4` | `d9b95146e46a5d81d5a291d7887e456164f4d0c2` |
| Producer real Actions Windows & Ubuntu | [#41106902806](https://github.com/C8LyNn/RoboRougelike/actions/runs/41106902806) SUCCESS x2 | [#41105901634](https://github.com/C8LyNn/RoboRougelike/actions/runs/41105901634) SUCCESS x2 |
| Original C#9 source assertions per OS | 101/101 PASS | 103/103 PASS |
| Original physical Git/package tests per OS | 25/25 PASS | 27/27 PASS |
| Original INNER ZIP SHA256 | `2fd97465358bddb7c9aeee91af2d4a0b75dd9b86525150e6bf708403291aa60d` | `7f10a7e4ef5052d7b83010c07fff4b8eb653e62caf314ed0751345c108547315` |
| INNER ZIP measured bytes / members | 96,318 bytes / 23 entries | 103,942 bytes / 27 entries |
| Unity metas / asmdefs | 10 GUID / 3 asmdefs | 11 GUID / 3 asmdefs |
| Diff vs original frozen #596 | 7 modified original owner paths, including `AtomicWorldSwap.cs` | 10 original owner paths including new `HostSlotLease.cs/.meta`, original R2 core unchanged |
| Security treatment | Whole original `TrySave` checks physically published generation + SHA-256 digest and per-run slot; source PASS on stale-world positive/negative paths | New long-lived host-slot owner lease and fixture isolation; **does not change original vulnerable R2 source core**, cannot assert the same stale-writer security proof |
| Independent full release security | **RED** on #637 ambiguous durable Commit (see below) | Not selected; no basis for final independent G0 |
| Real native Unity/PhysX G1 | NOT_RUN | NOT_RUN |

The original frozen R2 #596 remains physically SOURCE_G0_ACCEPTED_SOURCE_ONLY via [B01-40 #625](https://github.com/C8LyNn/RoboRougelike/pull/625) + [#635](https://github.com/C8LyNn/RoboRougelike/pull/635) and corrected [B01-62 #627](https://github.com/C8LyNn/RoboRougelike/pull/627) new real 65/65 each OS [#41106250023](https://github.com/C8LyNn/RoboRougelike/actions/runs/41106250023). This approval **does not transfer** to new #642 R3 SHA. No mix/copy/merge from #644 or ALT #604; the two author original ZIPs differ fundamentally. Frozen game [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) exact `d94400d95d13db7f27e1533df05c9292d5380431`, sole frozen Save [#556](https://github.com/C8LyNn/RoboRougelike/pull/556) exact `33094f154c7b91119b36d58aab285e6e83c5de57`, frozen R2 [#596](https://github.com/C8LyNn/RoboRougelike/pull/596) exact `efb2b06eafedab5469b57f163152d96aca3c5915` unchanged.

## Independent selected #642 security RED is genuinely reproduced

B01-61 [Draft #648 @ `88e67451492e877032aa6a822f46a76449bf6aaa`](https://github.com/C8LyNn/RoboRougelike/pull/648), exact selected original core original Git blob `a0a1923216d88ff8f50c560196f82cdec694f15c` compiled with original #556 binary Save codec, W06 stable consumed-ID ledger and frozen #510 identity source. Actual [Actions #41110488050](https://github.com/C8LyNn/RoboRougelike/actions/runs/41110488050) **COMPLETED SUCCESS on both real Windows2025 and Ubuntu24.04**, with **16/16 independent security witness assertions per OS** and strict `--release` **exit 3 / HOLD_P0_RED**. This is a RED **release verdict**, not a failing test-run.

- **FIX VERIFIED SOURCE-PARTIAL:** #642 new published generation and snapshot hash stops original #629 old Light/g10 physical state being reexported after other Heavy/g11 physical checkpoint; legitimately published Heavy/g11 may save g12. Real original physical OS FileCheckpointStore CAS and original #556 Light/Heavy consumed ID tests pass in this portable source QA.
- **CONFIRMED P0 RELEASE RED on #637:** fault-injection at public original `ICheckpointStore.Commit` seam invokes **real original FileCheckpointStore.Commit** that physically persists new checkpoint generation 11, then intentionally throws (ack lost). Original #642 `TryLoad` catches generic `HostException` and returns `Quarantined=false`, restores old physical Light/g10 while durable disk checkpoint is g11. This is genuinely executed original source/fault-injection under OS test; **NOT** a claim native Unity/PhysX File.Replace actually threw in production. Required new B01-23 **separate versioned descendant** on #642 lineage with sticky `RollbackQuarantine` for uncertain durable Commit after attempt and independent exact-new-SHA security regression.
- **Additional open owner review:** #642 releases host slot lease `OnDisable` even if a disabled host retains live physical world; #644 retained lease until `OnDestroy`. Keep as **not independently resolved**; B01-23 owns safe lifecycle correction in new version, without copying original #644 files.
- **No canonical release:** #642 isolated host only owns new W06 ledger, original #510 private `Rig[]/AtomicDockLedger` cannot be exported/hydrated with an owner-authorized public main-thread contract; Save binary+checkpoint full crash-atomicity and recovery are not proven. B01-60 licensed activated real Unity 6000.3.14f1 XML and Editor.log missing: `NATIVE_G1_NOT_RUN`. Codex Inbox #79 no new R3 offer; no ACK, G2 HOLD or merge.

## New B01-00 original functional output and contract

`r3_decision_gate.py` + `r3_w19_unselected.json` (historical filename, **current contents encode owner-selected #642 and #644 ALT_HOLD**) + `test_r3_decision_gate.py`: executable *offline* exact-HEAD immutable-producer SHA, original INNER ZIP SHA256 and sizes, producer original CI job identities, selection decision URL, selected-base/loser policy, independent RED, six lifecycle stages, G0/G1/live game/ACK/G2 gate and false time/acceptance refusal. **66 new actual tests written**; previous W18 467 original cases => **533 total original source cases**. Original B01-00 source CI workflow runs all on Windows 2025 and Ubuntu 24.04; only actual original job logs qualify for W19 test PASS.

```sh
python -m unittest discover -s Tools/B01/B01-00/W09/RECEIPT-LATCH-001 -p 'test_*.py' -v
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/r3_decision_gate.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/r3_w19_unselected.json
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/r3_decision_gate.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/r3_w19_unselected.json --require-ready
```

Normal CLI exit **0** means only internally consistent **HOLD** receipt, not authenticated owner/GitHub artifacts or actual source acceptance. Strict `--require-ready` exits **3** (confirmed P0 release RED), tampering/forged G0/ACK/ZIP/out-of-scope owner/merge exits **2**; writes refuse existing/foreign files. Real GitHub producer and independent QA source evidence above were obtained separately; the offline JSON validator cannot itself fetch or authenticate comments, physical ZIP bytes or native Unity.

**Current result:** `SELECTED_BASE_642 / 644_ALT_HOLD / #648_CONFIRMED_P0_RED / R3_INDEPENDENT_RELEASE_QA=RED / R3_FORMAL_G0=HOLD_P0_RED / UNITY_G1=NOT_RUN / #510_GAME_SAVE=HOLD / CODEX_OFFER=BLOCKED / CODEX_ACK=NONE / G2=HOLD / INTEGRATOR_METRICS=NOT_MEASURED / NO_MERGE`.

**Receivers:** B01-23 new atomic-Commit exception/lease lifecycle correction as independent owner PR after #642; B01-61/41 repeat exact new SHA stale ID/slot/fault/reentry/concurrency, B01-40/62 new exact owner-descendant physical Git ZIP/GUID/YAML/alien EMPTY dirs/safe sandbox migration and explicit G0 after P0 cleared; B01-60 licensed native Unity or NOT_RUN; B01-00 source-only #79 offer only if all relevant independent release gates authorize; Codex remains canonical integrator. No arbitrary PR cap; actual standard/polished integrator acceptance/time still null until real external evidence.

Rollback **only** close unmerged own B01-00 Draft and discard own new tooling/disposable output; never delete foreign empty directory, run original unsafe #510/#532 recursive rollback or mutate protected code.

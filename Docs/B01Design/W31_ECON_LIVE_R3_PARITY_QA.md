# B01-10 / PASS 17 WAVE 31 — real R3 ↔ approved V7 economy differential

**Scope:** B01-10-owned *QA source + design diagnosis*, not an alternative economy or a gameplay patch. Pin selected live source [CODEX #877@56c874aeac19d7724d7cddeaabc7a6a4f82c08d4](https://github.com/C8LyNn/RoboRougelike/pull/877), parent playable source [#873@d537b7310979b91df27a08b14d0f1c74d0889089](https://github.com/C8LyNn/RoboRougelike/pull/873). Project owner authorized the separate V7 rebuilt-0HP policy in [#731](https://github.com/C8LyNn/RoboRougelike/issues/731): floor(65% of positive purchase), strictly cheaper than new, price1→0 **PAID, not FREE**; unchanged separate above-zero HP repair behavior. Approved source producer [#780](https://github.com/C8LyNn/RoboRougelike/pull/780). Current selected one whole-game Save successor [#885](https://github.com/C8LyNn/RoboRougelike/pull/885) is still source-only, not the real R3 binder.

## Actual source mismatch (read at exact #877 HEAD; not a Native Unity test)

The live `Assets/R3/R3VehicleModel.cs` original Git blob at #877 is `1d0f378dc598e08b3d7c16975f419d84231511bb` and currently does:

```cs
if(part.Health<=0&&purchase<=1)return 0;
int quote=Math.Max(1,Mathf.CeilToInt((1-part.Health)*purchase*0.35f));
return part.Health<=0?Math.Min(purchase-1,quote):quote;
```

`Repair` and `RepairInventory` immediately reject `price <= 0`, so price1 is *quoted* 0 but cannot actually rebuild as zero-PAID; `RepairAffordable` calls that same path. This is **not evidence** that #780 approved policy is faulty; the current gameplay source simply does not yet use the sole approved V7 economy for rebuilt destroyed parts.

| New item purchase | Existing R3 0HP RepairCost (source formula) | Approved ECON V7 destroyed rebuild |
|---:|---:|---:|
| 1 | 0 (but Repair rejects) | 0 PAID, valid |
| 5 | 2 | 3 |
| 15 | 6 | 9 |
| 30 | 11 | 19 |
| 100 | 35 | 65 |

This introduces a balance and UI expectations mismatch when a player uses real garage Buy/Repair rather than a separate source ECON V7 model. It does **not** authorize replacing partial (>0 HP) repair with V7, adding a second wallet/Store, or retroactively changing an approved Save.

## New original-game red-first NUnit source

`Assets/Tests/Editor/B0110W31EconomyParityTests.cs` with unique `.meta` GUID adds **5 authored EditMode unit-test invocations**: 4 verified price-pair cases and 1 price1/zero-Scrap paid-repair action with original owned ID retention. They call the **real `R3VehicleModel.Price`, `RepairCost`, `ReceiveSalvage` and `RepairInventory`** when run in native Unity. These tests are intended to FAIL on current old policy. **As authored, they are NOT_RUN in activated Unity, NOT compiler-PASS, NOT proof of a red NUnit result; do not import into release candidate as green tests.** A source inspection/CI may establish existence and numeric incompatibility only.

**Potential separate quote risk:** `AffordableRepairCost` uses `DeepCopy().RepairAffordable()`, but `DeepCopy` does not transfer the `restorationGate` delegate. Hence a displayed hypothetical affordable repair may not reflect the live R3 PhysX functional-sweep refusal now present in #877. Confirm an actual UI caller and true native Player before claiming player-visible misinformation. Keep physical authorization at the same **single** existing R3 gate, not a new one.

## Required receiver decision and acceptance

- **B01-00/Codex/B01-22:** reconcile real `R3VehicleModel` 0HP-priced rebuild transaction with actual original V7 `#780` through one trusted game-facing account/session adapter. Owner decision #731 is already approved for destroyed-parts pricing, but **#834 loaner conditional fast repair is NOT approved**. Do not edit frozen V7, bypass current physical `R3RestorationValidation`, or duplicate #885 Save writer.
- **B01-23/62:** ensure one paid-zero quote is distinguishable from FREE victory entitlement in the single full-world wallet/ID snapshot; no negative Scrap, duplicate paid-zero replay, or fabricated WinId. Exact native-world repair/cold Save/rollback required after G0.
- **B01-60/Codex native G1:** test **source-first red on exact #877 if safe**, then actual accepted integration green for these five cases + positive >0 HP partial repair unchanged, full 5-zone garage/preview authoritative safety, Windows Player extract and cold loaded wallet/IDs. If a fixture expectation is invalid because a new canonical paid-repair API supersedes R3, version the fixture to the *actual* transaction rather than weakening economic acceptance.
- **B01-40/41/61:** independently pin exact producer SHA, source changes, GUID, asmdef and enforce that no second Save authority appears.
- **B01-13:** future player UX: quote visibly distinguishes PAID 0 from earned FREE; pending repair cannot imply immediate restoration if V7 next-authentic-win rule applies.

**QA stages at authoring:** `DESIGN_FINDING=YES | NEW_GAME_CODE=NONE | REAL_R3_NUNIT_AUTHORED=YES | UNITY_EDITMODE=NOT_RUN | UNITY_PLAYMODE=NOT_RUN | WINDOWS_PLAYER=NOT_RUN | INDEPENDENT_G0=PENDING | GAME_ACCEPTED=NO | INTEGRATED=NO | RELEASED=NO | NO_MERGE`. Source-only script/test CI, if any, must not be called native Unity QA. Recovery: drop this isolated B01-10 QA-only Draft without altering original #877 branch or player save.

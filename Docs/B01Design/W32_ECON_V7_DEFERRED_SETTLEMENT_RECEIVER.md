# B01-10 / PASS 17 — WAVE 32 · REAL R3 / ECON V7 temporal parity (P0)

**Role:** B01-10 owns balance policy and independent acceptance contract, NOT Codex's canonical R3 game/Save writer.  
**Active original game at review:** [#877 @ 3bd9b159997924dcecb4e1cd1d41137896e5199e](https://github.com/C8LyNn/RoboRougelike/pull/877), stacked on [#873](https://github.com/C8LyNn/RoboRougelike/pull/873).  
**Owner-approved policy:** [HQ #731](https://github.com/C8LyNn/RoboRougelike/issues/731), [original frozen V7 #780](https://github.com/C8LyNn/RoboRougelike/pull/780).  
**Current sole Save successor:** [#896](https://github.com/C8LyNn/RoboRougelike/pull/896), source Windows+Ubuntu 42/42; original actual R3/Rig[]/loaner binder STILL ABSENT.  
**Anti-softlock owner decision:** [#834](https://github.com/C8LyNn/RoboRougelike/issues/834) remains open; conditional SAME-WIN claim has **NOT** been approved.

## Source audit: W32 reveals TWO independent product gaps

At current game HEAD the original `Assets/R3/R3VehicleModel.cs` Git blob remains exact `1d0f378dc598e08b3d7c16975f419d84231511bb`.

1. **QUOTE/PAID0**: `RepairCost(0HP)` uses old `ceil(35%×purchase)`, not V7 `floor(65%×purchase)`. For new prices `[1,5,15,30,100]`: original current quote `[0,2,6,11,35]`, approved V7 quote `[0,3,9,19,65]`. Both `Repair()` and `RepairInventory()` reject a legitimate 0-cost **PAID** order (`price <= 0` / `cost <= 0`). Zero `PAID` is NOT a FREE earned coupon and cannot auto-restore without a verified victory.
2. **TEMPORAL COMMIT / HP**: existing `Repair()` contains `Scrap -= price; part.Health = 1; return true;`; existing `RepairInventory()` contains `Scrap-=cost;part.Health=1;return true;`. Neither accepts/stages the V7 0HP paid booking in the authenticated pending queue. **Owner #731 requires 0HP owned-part booking to remain physically 0HP until a NEXT DISTINCT, AUTHENTIC win** (newly destroyed part is not restored in the same win); only above-zero HP partial repair retains its separate immediate 35%-based policy. Merely replacing 35% by 65% and deleting the `cost<=0` check would fix the arithmetic but create an unearned immediate restoration and violate the gameplay loop.

## W31 red-first native fixture correction

The W31 [#897](https://github.com/C8LyNn/RoboRougelike/pull/897) original real R3 NUnit fixture originally and incorrectly expected a destroyed inventory item to be back at `Health=1` IMMEDIATELY after quote0 payment. In W32 **we correct its expectation to still be `Health=0` pending authentic next victory**. It is deliberately RED on current R3; this is a genuine tightening, not a silent accepted behavior change.

Additional genuine original `Assets/Tests/Editor/B0110W32EconomySettlementParityTests.cs` authored:
- RED: mounted Core+Armor physically valid structure, destroyed Armor P100 (expect paid65), enough wallet, book via original `R3VehicleModel.Repair` on same GUID; **deduct exactly 65, keep 0HP until verified next win**. Current original R3 already fails quote and later immediate HP. This is a partial transaction oracle; no original real WinId exists in current R3.
- GREEN GUARD: damaged **above-zero HP** Armor100 at 50% quote18, real R3 `Repair` succeeds, HP rises to1, Scrap decreases once to82, original GUID unchanged; preserves authorized separate partial-repair policy.

**W32 native authored totals:** W31 prior 5 NUnit invocations + W32 two new NUnit methods = **7 targeted genuine Unity tests SOURCE-AUTHORED, NATIVE NOT_RUN**. Never call a green GitHub Python checker actual Unity PASS; it is a SHA-pinned **expected-source-RED witness** with deliberate mutants.

## Canonical product correction contract — owner/integrator

DO NOT add a B01-10 standalone economy runtime, second wallet/coupon, independent WinId generator, second Save file, V7 reducer rewrite, or bypass original R3 physical `R3RestorationValidation`. Integrator must extend the SAME existing canonical game transaction:

1. Physical authority checks actual owned `Part.Id`, 0HP, valid 0.55m required/connected geometry, live mount repair safety/stock identity, genuine sufficient Scrap incl `>=0`, and one active owner lease. Invalid action consumes no Scrap and leaves ids/HP/part positions unchanged.
2. Quote `floor(purchase*65/100)` using overflow-safe integer math, strictly below positive purchase; P1→0 records `PAID, Free=false`. Above-zero HP partial repair has distinct preserved policy, no temporal booking mistaken for immediate fix.
3. Debit paid rebuild quote ONCE and atomically create one pending `owned GUID + amount + required next distinct win` under trusted #896 successor's single full-world/V7 journal; **do not mutate 0HP to 1 at booking**. If action is denied/cancelled or journal commit fails/quarantines, no wallet debit, duplicate order, ghost part, or partially restored HP.
4. Only authentic committed host victory may settle an eligible prior pending order, and only once. Loss or no eligible encounter never settles; duplicate WinId, cold reload, lost ACK, simultaneous store owner, forged Win0 FREE, corrupted envelope must fail closed. Newly destroyed in winning battle waits for later distinct victory.
5. UI `R3GarageView` and potential #903 must clearly say **"Zlecono odbudowę — po kolejnym zwycięstwie"** for 0HP order; **"Naprawiono"** is correct only for an already >0HP instant partial repair. No invented accessible instant-free healing.
6. Existing #834 one-shot-loaner Win1 with 0 Scrap/multiple essentials can still end without a legal Win2: **PRODUCT_ANTI_SOFTLOCK=RED** until OWNER explicitly chooses A/B/A+B; no silent early claim or second loaner.

**Acceptance:** after exact-sha independent composition G0 (#877 + #780 + #855 + #896 + real R3 Rig[] binder), run full Unity6000.3.14f1 EditMode/PlayMode XML+Editor.log+exit and extracted Windows64 Player/Player.log. Actual normal bought PAID0/PAID65 and FREE single-win order must survive cold close/restart with original IDs, state and Scrap; 2 distinct fights vs loss/WinId replay, zero-quote no-coupon, physical Saw blocked restoration/no deduction, legal positive partial repair, and UI 720p/1080p. B01-60 performs independent G1, B01-22/62 economy/Save review, Codex owns real source intake, B01-00 #834 owner policy.

**Current truth at authoring:** `SOURCE_DEFECT_WITNESSED=YES`; `NATIVE_NUNIT_AUTHORED=7`; `NATIVE_TESTS_EXECUTED=NOT_RUN`; `SOURCE_CI=AWAIT_EXACT_HEAD`; `PRODUCTION_FIX=NOT_IMPLEMENTED`; `CURRENT_REAL_SAVE_BINDER=MISSING`; `UNITY_G1=NOT_RUN`; `WIN64_PLAYER=NOT_BUILT`; `MERGED=NO`; `RELEASED=NO`. **Rollback:** close isolated B01-10 QA branch, no original #877/#896 producer or user Save changed.

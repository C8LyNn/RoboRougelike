# B01-22 — PASS17 W28 — native current-game R3 recovery / ECON V7 integration witness

## Why this exists (not a new economy)
This **actual Unity NUnit EditMode source** exercises the existing playable Codex target runtime at **#824 @ e03d36f2b86f8c73a067e574b67237a8921f622a** via the REAL `R3VehicleCatalog.Create(Light)` and `R3VehicleModel` APIs. Tests are in a unique B01-22-owned file under the existing Assembly-CSharp-Editor NUnit project, not new mock economic code. They are an acceptance/regression gate for future Codex V7 integration and a product-factual contrast, **not a native executed QA report**. No author-owned game runtime, weapon/garage core, ECON, writer, game Save, current W28 Codex worktree or protected source was edited.

**Original targeted game source:** `Assets/R3/R3VehicleModel.cs` at Git blob `1d0f378dc598e08b3d7c16975f419d84231511bb`, `Assets/R3/R3VehicleCatalog.cs` Git blob `f76de5bbdcc1dbdb7ddb9fe9859c36c232fd8584`, `Assets/R1/R1RunController.cs` Git blob `09a6776db67662e8960d1f446ba5c5168cd3e4b2`. The actual `R1RunController.StartNextEncounter()` checks `VehicleModel.CanDeploy` and `Progress.LeaveGarage()`, not original ECON V7 or one-shot loaner; `R1RunCheckpointStorage` uses an older `checkpoint.json` garage-only save. This is a **live integration gap**, not an assertion that V7 is broken in game.

## Four authored real NUnit acceptance tests (Unity NOT_RUN)
1. Original Light vehicle, living Core, left-side wheel 0HP and wallet=0, real CanDeploy FALSE; garage Repair and EmergencyRepair fail, no Scrap debit, no identity changes or free HP.
2. Supply genuine TEST-ONLY Scrap to original R3 model, repair Wheel once, verify immediate HP restore and CanDeploy TRUE. THIS IS A DELIBERATE LEGACY-PRICE WITNESS: original R3 wheel purchase15, immediate repair6 (ceil 35%), whereas owner-approved V7 paid floor 65% is **9 Scrap**, and destroyed module must await next distinct authentic victory. The current model's recovery is NOT the selected V7 rule.
3. Optional Armor HP0 with healthy Core, weapon and drive must NOT block normal CanDeploy; ID stable, no autoheal.
4. Actual 0HP Core blocks deployment even if weapons/wheels alive.

These tests use actual Unity `Vector3Int`, `Mathf`, NUnit and original R3 model with no CLR shim. They have NOT executed because native Unity Editor/test artifacts are unavailable from this context. First owner-operated Unity 6000.3.14f1 EditMode run must produce Editor.log and NUnit XML, exact game SHA, exit code. Do not call `SOURCE_G0` or `NATIVE_G1` PASS without independent review / evidence. W27 #817 contains actual Unity targeted 20/20 and #824 native individual suites including 6/6, 5/5; those do NOT cover these new tests.

## P0 softlock / owner decision
Original B01-22 [#812](https://github.com/C8LyNn/RoboRougelike/pull/812) actual source CI Windows/Ubuntu 1564 direct V7 assertions green, but detects an economy-design-trap: living Core + broken expensive essential Drive + zero Scrap + one-shot loaner Win1 -> coupon only then reserved, remains 0HP until Win2, loaner now Spent. Needs an owner-approved conditional pre-win rescue claim OR explicit honest terminal run outcome. Cannot prove this inside current game until V7 and a real borrowed prefab enter the R1 gameplay loop. Do not fake a Win, use caller-asserted combat attestations, copy source-only V7 writer or change the owner's one-free-per-win rule.

**Selected integration source dependencies (READ-ONLY):** #780 V7, #787 sole strict canonical owner source, #792 actual Rig[] stage, #797 full-game Save (known Unity API RED: SHA256.HashData, Convert.ToHexString, File.Move(,,true)), #750 ColdRestart, #812 W27 source witness; latest actual Codex gameplay #824. No install or merge authorized; alternative writers ALT_HOLD. B01-62 alone repairs #797 Unity API; B01-00 makes policy choice; B01-23/20 real game binder and prefab; B01-40/41/61 independent source G0; B01-60 native G1. Codex owns current live game source. **Do not offer to Codex #79 before G0.**

## How to run in owner-authorized Windows native Unity editor
Select exact game PR branch checkout (or after Codex approved cherry-pick of this **test-only** source), Unity 6000.3.14f1 URP. In Test Runner → EditMode select `B0122W28LiveGarageRecoveryRegression`, run four tests. Record full `Editor.log`, NUnit XML, test count, exact Git SHA, Unity exit status. Do not open or modify the real player persistentDataPath. This test only creates in-memory R3VehicleModel and does not access disk. G1 NOT_RUN pending operator evidence.

**Gate:** IMPLEMENTED=YES_TEST_SOURCE | TESTED=NATIVE_NOT_RUN | QA_ACCEPTED=NO | G0=INDEPENDENT_PENDING | INTEGRATED=NO | RELEASED=NO | PRODUCT_SOFTLOCK_QA=P0_RED_DESIGN | ROLLBACK=discard isolated Draft; no disk or old Save changes.

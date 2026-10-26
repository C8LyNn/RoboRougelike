# RUNSAFETY S04 DESIGN — RECEIVER HANDOFF (DIAGNOSTIC ONLY)

**DO NOT INTEGRATE THIS SOURCE-ONLY DESIGN.** Read after B01-00 approves policy C and packages pass independent B01-60 G1 / B01-40 G0_TARGET / B01-00 G2. Source of truth must be pinned to approved future BUILD/ECON R1/R3 and current main game integration SHA.

1. Verify GitHub issue #152 authorizes this policy; if only `POLICY_C_RECOMMENDED` then STOP. Do not alter live `R1RunController`, `R3VehicleModel`, save, store, `EmergencyRepair`, loot or run-loss.
2. Ask B01-20 for an approved isolated RUNSAFETY R1.3 package implementing cases `EL-02/03/05/06/16` with trusted live R3 owner/HP/graph evidence and activity capability. Ask B01-22 for canonical ECON queue with strict 0HP-on-win and reward event idempotence.
3. On *disposable* exact SHA target, verify loader snapshot, event-level reward/lose conditions, UID persistence, correct reset. Keep the loaner lease outside owned inventory; never auto-grant loaner parts or currency.
4. Run real Unity EditMode/PlayMode/Windows manual tests, 720/1080 captures and 100 seed physical challenges per activity profile; retain exact hashes, XML and video with a tested player vehicle, not just a cube.
5. On failed tests, do not hide a blocking signal; return failure, rollback adapters and notify B01-00. Do not merge without separate authorization.

The fixture runner `node run.mjs` can serve as a **portable behavioral oracle** during adaptation, not as a substitute for engine tests. Option A violates the accepted one-win cost of destroyed repair; option B is a new activity needing separate product authorization; recommended C is **not** HQ approved.

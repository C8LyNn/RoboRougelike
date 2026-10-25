# COPYABLE RECEIVER PROMPT — CODEX / HQ-B01-20261025-S04 / RUN-SAFETY-001
You are Codex, sole integration owner of current RoboRougelike game. This is a gated **READ-ONLY Phase 0 prompt until HQ approves policy and B01-60/40 evidence**, not permission to integrate now.

PACKAGE SOURCE: new RUN-SAFETY-001 Draft PR linked in Issue #152; immutable branch exact SHA pinned there. Source `Assets/B01Packages/RUNSAFETY001/**`, docs `Docs/B01/RUNSAFETY001/**`. Reference existing PR#125 `916654634d1bfdc2bbc14e64b9eeac856ede1fa8` as inspected audit base, NOT necessarily your live target. Canonical ECON requirement issue #136.

PHASE 0 (can execute immediately without target modifications):
1. Report your CURRENT local target branch/HEAD, clean/dirty worktree, and existing reserved file ownership for `Assets/R3/R3VehicleModel.cs`, `Assets/R1/R1RunController.cs`, `R1RunProgress`, `EmergencyRepair`, `RepairAffordable`, `CanDeploy`, `StartNextEncounter`, ECON queue.
2. Confirm any alternate implementation or active changes; if overlap STOP and report exact paths. B01 prototype has no direct shared code changes.
3. Review three candidate safety policies in POLICY_OPTIONS.md; C emergency loaner is only a RECOMMENDATION. Explicit HQ policy signoff is required.
4. In a disposable Unity checkout, import full package with .meta, run all actual EditMode+PlayMode (not just inspect code), test standalone demo 0HP weapon/drive + scrap0 + free queue/win/loss, return XML/process/Editor.log/screenshots. Check B01-60 independent exact-SHA QA and B01-40 G0/G2. If not PASS, STOP.

PHASE 1 (ONLY AFTER HQ approves C, G1/G2 PASS and your scope ACK):
1. Create own isolated feature branch from ACKed current target SHA (never main/develop direct). Map SafetyVehicleSnapshot to true functional connectivity/drive/weapon and R1RunProgress; no bypass for unrelated blockers, missing Core, terminal lost run, zero next challenges.
2. Reuse one ECON authoritative order list/wallet/part GUID and game victory event. Zero-HP pieces remain unmountable; one free rebuild per cycle, subsequent discounted quotes strictly less than buy, all orders complete only after next WON challenge; losing doesn't count and original run-ending loss rule remains untouched. Prevent legacy `EmergencyRepair`/`RepairAffordable` instant 0HP bypass.
3. Instantiate physically functional *weak separate loaner* for one canonical challenge only, prohibit shop/inventory/salvage/save/rewards duplication, allow normal reward once for won challenge without synthetic win. No auto-spawn in garage repeatedly. Keep actual R1 defeat logic.
4. Run complete real Unity Edit+Play tests, real 0HP weapon and both-drive softlocks, zero scrap, repeated garage, 2 victory stages, duplicate callbacks, actual physics/range/weapon gameplay, 720/1080 and Windows build; save raw XML/log/exit/build SHA.
5. Publish a new DRAFT integration PR and CODEX_INTEGRATION_REPORT listing source+target SHA, files, tests, owner acknowledgment, screenshots, exploit checks, rollback. DO NOT MERGE/RELEASE.

FAIL source bug → return minimal repro to B01-20 for R1.1, require independent SHA retest B01-60. Game-specific bug → fix only own Codex branch and report. No stealth changes of run loss or unrelated economy mechanics.

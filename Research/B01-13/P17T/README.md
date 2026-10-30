# B01-13 — PASS 17 / WAVE 22 — SOFTLOCK-RECOVERY-ROUTES-001/R1

Owner: B01-13 HUMAN PLAYTESTING · HQ dispatch #713. Paths: Research/B01-13/P17T/** plus dedicated B01-13 workflow. No canonical game source, Codex source, Save/Load, Unity scenes, ECON, main/develop or external producer edits.

## New function: shortest legal recovery route generator
This is an executable offline, bounded breadth-first search over concrete garage repair choices, creating two real downloadable files: human-readable Markdown playtest action sheet and machine-readable JSON witness. It is NOT a duplicate of W20 passive affordability simulator #676 or W21 garage interactive tutorial #706; the new feature AUTOMATICALLY DISCOVERS minimal routes (or their absence under specified constraints) for future test operators instead of expecting them to guess choices.

Original scenarios: Light with core and drive operable, 3 OPTIONAL 0HP modules; Heavy with core operable and ESSENTIAL 0HP drive plus 2 optional 0HP modules. Compare temporary hypothesis one owner-global free reservation per unique victory vs one per run, and strictly no rescue vs ONE UNAPPROVED borrowed vehicle eligible only when essential 0HP is pending and normal fight blocked. Targets are normal deploy eligibility (CAN_DEPLOY) or restoring all destroyed parts (FULL_RESTORE). Search budget at most 9 actions and 3 synthetic victories. Sample 100 initial Scrap, 70 paid quote are invented parameters, NOT game settings. Search never invents infinite wins, real Scrap rewards or fake physical ownership. Pending rebuild completes ONLY after a later simulated genuine distinct win, not the damage-producing fight; losses don't complete. Local DEMO-xxx IDs do not grant real combat privileges.

Illustrative expected model results at sample cost 70 and Scrap 100:
- Heavy STRICT: normal fight blocked by essential drive; no possible modeled path even with 500 Scrap; HOLD_WITHIN_MODEL, NOT proven Unity softlock.
- Heavy loaner / can-deploy: QUEUE H-DRIVE, then HYPOTHETICAL LOANER_WIN. Loaner is explicitly NOT APPROVED by project owner (#154).
- Light one free per victory / full restoration: five steps, two synthetic model victories. Light one per run: insufficient Scrap for all three destroyed parts; HOLD_WITHIN_MODEL, NOT proof of live game unplayability.
- Heavy loaner, one free per victory / full restore: five steps involving one hypothetical loaner win and one later simulated normal win. This is not gameplay or human test evidence.

## Operator usage
Node 20+ stdlib only. Use a local PRIVATE existing directory OUTSIDE Git worktree; files are exclusively created without overwriting (O_EXCL), no network, credentials, user data, persistent storage in game, or remote upload. For illustration:

    node Research/B01-13/P17T/recovery_routes.mjs --format md /private/w22-playtest-routes.md
    node Research/B01-13/P17T/recovery_routes.mjs --format json /private/w22-playtest-traces.json

Both generated files can also be downloaded as actual GitHub Actions artifacts after successful original-source CI. The Markdown document has 8 combinations with exact step-by-step route witness, cautionary hold messages and consent-first playtest handoff. The JSON lists each modeled route with scenario and parameters, including zero-actual-human, NOT_RUN Unity and unapproved loaner flags.

Programmatic Node API: import {make,route,matrix,apply} from recovery_routes.mjs. Example route(make("HEAVY","PER_VICTORY","ONE_UNAPPROVED_LOANER"),"CAN_DEPLOY").

## Test plan and evidence boundaries
One dedicated workflow on Ubuntu24.04 / Windows2025 × Node20/22, using the ORIGINAL 58 Node test cases, not just a textual CI check. Tests include exact path/immutable pins, all action transitions, retained 0HP when pending, defeat, local replay denial and invalid IDs, strict no-start, loaner once, price exhaustion, shortest deterministic BFS routes, 8 combinations, actual physical CLI output and no overwrite/Git output, and both human and machine-readable outputs. Before source tests, physical manifest validates Git SHA1 blobs + byte lengths and prints SHA256 for five owner files. Afterwards runner generates both reports outside Git and uploads them, SHA256 logged. Only exact-SHA completed logs qualify as PASS; do not claim based on authored tests.

    python Research/B01-13/P17T/verify_package.py
    node --test Research/B01-13/P17T/test_recovery_routes.mjs

This is source-level Node test only. There is no native C#/Unity/PhysX build, genuine browser test, actual human recruited, direct ECON V3 CAS or authenticated combat source. No real Save/Load or overwrite or auto-merge; local event IDs are NOT a durable/cryptographic watermark. Missing modeled route means hold WITHIN limited model, not a true engine-level softlock.

## Frozen baseline, independence and receiving roles
Read-only frozen refs: selected original gameplay #510 d94400d95d13db7f27e1533df05c9292d5380431, Save #556 33094f154c7b91119b36d58aab285e6e83c5de57, historic host #596 efb2b06eafedab5469b57f163152d96aca3c5915, owner R4 #647 09c1809b73324a6d31c13e5d0143de62ac1e3ca6, economy #252 12572117a3dce369257feabc57021cde58023369. #652/#644/#604/#682/#693/#695 alternatives remain ALT_HOLD, no imported or mixed implementation. Owner selected R5 #690, API #691, ECON V3 #694, storage #712 are other teams' work and NOT integrated or certified here.

B01-22/B01-10: assess player-facing economic feasibility and critical mobility, question whether the tentative loaner should be adopted; #154 owner decision required. B01-40: exact Git/manifest source G0 and safe rollback. B01-60: independently verify paths and prepare real Unity/human testing when available. B01-00: coordinate informed and consented future sessions and integrator handoff. No participant data collected; G1 NOT_RUN; G2 HOLD; Codex ACK NONE; real live Game Save NOT_VERIFIED; integrator feedback NOT_MEASURED. Rollback simply abandon isolated unmerged Draft and discard user-owned synthetic report files, never remove foreign data. Six lifecycle states kept independent.

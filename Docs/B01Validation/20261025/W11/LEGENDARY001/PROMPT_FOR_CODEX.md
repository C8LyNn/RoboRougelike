# LEGENDARY-CORE-001 R1 — PHASE 0 READ-ONLY REVIEW DRAFT

**NOT SENT; DO NOT RUN GAME INTEGRATION. G2 HOLD.** Recipient: Codex only if later approved by B01-00/HQ. The present pure .NET8 prototype has no Unity installer, adapter, prefabs or native effects.

Exact source: Draft [PR #230](https://github.com/C8LyNn/RoboRougelike/pull/230) `7e9d9ca0a0ef6d4c9bf1ce39fdb187b0d960958d` (7 files / 38,928 B). Separate owner GDD [PR #229](https://github.com/C8LyNn/RoboRougelike/pull/229) `d5f6bfb9e6c019aadcaec2ae70ca3390c3923f61`. Historical published game-only comparison [OBJ #210](https://github.com/C8LyNn/RoboRougelike/pull/210) `4c9236a4cdc27814ea2e7679872ec4a524150cc8` is not live target ACK. Producer CI [40187129527](https://github.com/C8LyNn/RoboRougelike/actions/runs/40187129527): **25/25 .NET8 PASS (2000 seeded transitions)**, B01-40 independent source 7/7 SHA256 MATCH, **native Unity NOT_RUN**.

## Only authorized PHASE 0 content

1. Confirm exact live branch/HEAD, dirty/unpushed files and reserved R3/R4/R6/garage/economy/weapon-system owners. Treat all live sources as read-only. Do not modify host files or switch live worktree.
2. Review `Tools/B01/LEGENDARY-CORE-001/README.md`, catalogue, source, 25 test names and W11 B01-40 `INSTALL_FOR_CODEX.md`. Crosswalk GDD R0 schema/definitions to R1 C# model and canonical R3 part model; report field-by-field mismatch.
3. Flag unapproved mass conflicts: LEG-WPN-001 **650 vs 220 kg**, LEG-MOV-001 **780 vs 150 kg**; do not choose values silently. Flag 0HP remove losing ownership, weapon family cap 2 vs current game gun count/family mapping, R3 single authority, physical instance vs stable legend type, target sockets and save migration.
4. Independently reproduce .NET8 isolated 25/25 if available with actual logs/exits, and request B01-60 consumer negative QA: rewards idempotency, 0HP removal/save, interrupted transfer, duplicate physical ID, two robots, orphan mounted weapon families. Results without native Unity must be labelled .NET domain only.
5. Return an **API compatibility matrix and proposed adapter boundary**, not gameplay code. Include exact source SHA/target SHA ACK, field mapping, versioned serialization plan, risk list and test plan. Note absent B01-10 balance arbitration and blocked native G1.

**STOP.** No Unity Assets import, no game system edits, no new parallel garage inventory, no actual implementation, merge, build, deployment or claim of Legendary effects. Only a future new signed B01-00/HQ dispatch after design arbitration, target scope ownership, independent QA and G2 may authorize Codex integration.

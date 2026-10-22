# RRL-W2 — Unified rewards, repair economy, enemy density, debris ejection

Status: **implementation candidate only**. Authoring was performed through the GitHub connector. Unity compilation, EditMode, PlayMode, Windows build, and manual gameplay have **not been executed** for this branch. Do not merge on the strength of the baseline's 522/522 PASS.

## Intended player-facing behavior

After each completed encounter there is ONE reward screen:
- Up to **3 total** selections, in any combination, among the 3 offered stat bonuses and available enemy parts.
- 0, 1, 2, or 3 selections are legal. A reward may not be selected twice.
- Armor and cores are excluded. Any not-selected loot becomes scrap automatically.
- All retained equipment, including destroyed (0% HP) parts, goes to inventory; a destroyed part is **not mountable until repaired**.
- Intact (100% HP) repair: 0 scrap. Damaged (0–100% HP) repair: ceiling of 35% of part purchase price times missing durability, minimum 1 scrap. Destroyed (0% HP) rebuild: full purchase price. Repair is a separate garage expense.
- There is **no forced subsequent 1-of-3 bonus screen**. For the final battle, bonuses are not generated (only recovered parts), as there is no following encounter.
- Enemy reinforcements: non-boss encounters spawn one extra normal enemy, up to four in total; the Siege Boss remains alone.
- Destroyed parts and detached chunks receive outward/upward, mass-independent velocity. During the initial 0.45-second ejection, their colliders are disabled to prevent shielding the still-live chassis; physics collisions and gravity resume afterward.

## Boundaries

This is not a redesign of MAP-SYSTEM or MOV001–009 and does not alter Unity version, package manifest, scenes, or .meta GUIDs. The current opponent equipment source exposes guns and wheels; additional tech modules will appear only if they are actually present in future salvage pools. It does not fabricate an unmounted enemy component.

## Mandatory validation in Unity 6000.3.14f1

1. Clean-checkout import and compilation (no new Console errors), then EditMode and PlayMode baseline test suites. Reconcile existing tests which expected the two-step salvage/bonus workflow, or explicit old enemy counts, with new intended gameplay.
2. Win a non-final encounter, select three upgrades; verify all three stat modifiers apply once, none of the equipment is retained, unselected parts yield scrap, and the next screen is the garage.
3. Win with zero selected rewards; ensure progression to the garage, correct scrap amount, and no duplicated claim.
4. Select three damaged/intact/destroyed parts and no upgrades; verify IDs, health percentages, costs, warehouse entries, and total <=3.
5. Select one upgrade and two parts, then two upgrades and one part; verify only chosen rewards apply, no fourth selection, no unselected rewards in inventory.
6. Kill enemies with 0-HP wheels/guns; confirm they can be retained, show full rebuilding cost, cannot mount before repair, can be repaired if affordable, and can mount after repair.
7. Confirm intact part cost 0, damaged part cost >0 but below full part value, destroyed full value, and unaffordable repairs fail without spending scrap.
8. Select and deselect rewards across salvage pages; ensure selected IDs remain consistent and counts stay <=3. Ensure armor/cores cannot be selected.
9. Encounter counts: first stage 2 enemies; 2-base stages 3; 3-base stages 4; final Siege Boss 1. Check that extra enemies do not spawn into walls or cause pathfinding deadlocks, and re-balance difficulty if needed.
10. Damage front/side/end of gun/wheel assemblies in play mode. Confirm debris visibly moves away, projectiles can hit the living vehicle rather than being caught behind dead pieces, and colliders become enabled again after ~0.45 sec.
11. Stress-test large chunks and heavy chassis, pause/unpause during ejection, and verify debris cleanup doesn't leave colliders disabled or bodies floating.
12. Windows retail + QA builds, then compare runtime visuals and movement. GitHub Unity CI-02/03/04 remain blocked until supported Unity activation; don't treat CI-01 as Unity PASS.

## Merge decision

Keep as a DRAFT PR against `develop` until tests and gameplay QA succeed. Do not merge into `main` or publish a release automatically.

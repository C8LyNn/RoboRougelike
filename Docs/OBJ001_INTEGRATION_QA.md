# OBJ-001 — Reactor Defense Integration QA

Status: implementation and targeted Unity GREEN; final review/full regression/build/native verification IN PROGRESS. Not a merge approval.

## Sources and isolation
- BASE `codex/b01-phy-grounding-core-r1@916654634d1bfdc2bbc14e64b9eeac856ede1fa8`, Draft PR125; includes gameplay523046f and Editor fixture54f0192. Its independent PHY-QA-001 acceptance remains pending, not silently promoted to an approved release.
- WORK `feature/rrl-obj001-integration`, dedicated checkout `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-OBJ001`; existing worktrees, main/develop and W7/W8/B01 release archives untouched.
- ORIGINAL OBJ package: Draft PR14 `cdc36875efe4b5661a7c41c0b7b60d0010850e2b`, issue9. No host objective integration found in current gameplay/DEF22/26; DEF packages are shields/point defense. Model M05-011 is visual only. MAPGEN99/103/109 and other model branches excluded.
- Unity6000.3.14f1/URP and package versions unchanged. Native worktree creation returned Not a git repository in projectless Desktop context; manual git fallback created the isolated sibling checkout. No other checkout's Library was copied.
- Runtime candidate `7a2e901e1f7732d33a4f7f7ff07a9ea9c60b950c`; final source/PR/artifact pending remaining gates.

## Reuse of original swarm code
Six original C# files imported with their original `.meta` GUIDs. Five remain byte-identical: phase enum, standalone demo UI, editor demo builder and both test files. The actual in-game `ReactorDefenseObjective.cs` retains117/121 original lines, measured by exact line matching; it is extended for external HP binding and explicit wave-goal completion. Original timed standalone/demo behavior is preserved and tested; the demo UI/editor are not claimed to be the live game's HUD. The host uses the original objective's validation, start/reset/terminal states and events; its timer is explicitly disabled for wave completion. Canonical HP and all real weapon damage remain P12, read directly through `IReactorIntegritySource`, not a second combat HP store. Source/hash/line counts are recorded in `OBJ001-original-code-reuse.json`.

New work is integration: a static reactor damage bridge/team member, an R1 partial wave coordinator, HUD hooks, target retargeting/fallback in existing P12 AI, tests and QA-only native fixture. Existing spawner, all weapon hit routes, grounded R6/Core recovery, salvage/economy/reward settlement and garage remain authoritative. Friendly reactor hits are filtered by the existing team router using team1; NPC attacks use team2.

## Player instructions
1. Choose a vehicle in the menu, then **Obrona reaktora • przygotuj pojazd**.
2. Inspect/edit the real preparation garage, starting with0 scrap; click **Broń reaktora**.
3. Keep the600HP reactor alive through three waves of2,2,3 enemies, with3 simulation seconds between waves. Nearby players can draw an attacker's attention. HUD shows HP, wave and remaining enemies.
4. Victory opens existing1-of-3 bonus and up-to-3 enemy-part rewards, then the standard garage/next route. Vehicle damage and shared loot/cooldowns persist across waves. No intermediate-wave reward is granted.
5. Reactor or player destruction loses. Enter on the loss screen restarts through the main menu. Esc pauses; the defense pause also exposes return to menu. Restart clears activity objects/shots.
6. Defense is also an optional next activity from a normal expedition garage. Sandbox workshop cannot transfer its600 free scrap into rewarded defense. The final Siege Boss slot cannot be replaced; ordinary six-stage routes remain available.

## Actual targeted results
| Run | PASS/FAIL/SKIP | Native exit | Meaning |
|---|---|---|---|
| Python CI baseline, normal user |16/0/0 |0 | Existing tool unit suite. First sandbox attempt failed Temp ACL cleanup; not a product failure/PASS |
| OBJ001-original-and-red-2eef03ced0a145ff88fbd47e1a7c7aad |11/3/0 |2 | All original EditMode tests PASS; missing host entry, canonical bridge and explicit completion reproduced RED |
| OBJ001-health-green-260359ac9f504c48a0448e22ab942dc3 |13/0/0 |0 | Original11 plus real canonical damage/buff/death/reset bridge and live explicit completion |
| OBJ001-entry-green-c8ebe0eb031a411a9cda199f42ed9102 |14/0/0 |0 | Actual menu API→preparation garage→600HP first wave2NPC |
| OBJ001-gameplay-009eb2c381d84dd49ed00dd597025de8 |28/0/0 |0 | Original/entry, seven new lifecycle/physical cases plus ordinary W2/range regression |
| OBJ001-original-play-2809f8abce334376889e1e272f14a446 |3/0/0 |0 | Original package timed completion, disabled pause and independent reactor states |

These are separate runs, not summed unique tests. API lifecycle tests deliberately damage enemy cores to inspect progression/reward semantics; they are not claimed as ordinary-input victories. Physical tests use real P17 rounds: friendly25 damage leaves reactor600, hostile25 reduces it to575. Actual turret fire at controlled positions reduced reactor600→575.5199 and player part HP13780→13755.52, with distinct reactor/player target roots. Tests also cover3wave win, one ledger/unique IDs, preserved player damage, one reward, garage→normal fight, fatal reactor hit+final clear loss priority, restart cleanup, pause/intermission, duplicate entry and sandbox budget isolation.

## Pending gates / limitations
- Whole-branch fresh review: PENDING.
- Full current EditMode / PlayMode: NOT_RUN.
- Windows retail/QA build and native API/operator tests: NOT_RUN.
- Archive/extraction/source hashes and final Draft PR: PENDING.
- Known inherited UIA-attached Alt+F4 C0000005 remains unresolved; current ordinary close/native proof must be distinguished from B01 baseline. No shutdown fix or independent B01 acceptance is claimed.

Stop at a Draft PR with actual outcomes and limits, targeting the PR125 branch; no main/develop/shared integration merge and no next package implementation.

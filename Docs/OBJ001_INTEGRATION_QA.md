# OBJ-001 — Reactor Defense Integration QA

Decision: **READY for OBJ-001 gameplay review**, with current full Unity and native Windows evidence. Draft integration only; PR125 dependency acceptance and automation-attached shutdown remain open. This is not approval to merge a shared branch.

## Sources and isolation
- BASE `codex/b01-phy-grounding-core-r1@916654634d1bfdc2bbc14e64b9eeac856ede1fa8`, Draft PR125; includes gameplay523046f and Editor fixture54f0192. Its independent PHY-QA-001 acceptance remains pending, not silently promoted to an approved release.
- WORK `feature/rrl-obj001-integration`, dedicated checkout `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-OBJ001`; existing worktrees, main/develop and W7/W8/B01 release archives untouched.
- ORIGINAL OBJ package: Draft PR14 `cdc36875efe4b5661a7c41c0b7b60d0010850e2b`, issue9. No host objective integration found in current gameplay/DEF22/26; DEF packages are shields/point defense. Model M05-011 is visual only. MAPGEN99/103/109 and other model branches excluded.
- Unity6000.3.14f1/URP and package versions unchanged. Native worktree creation returned Not a git repository in projectless Desktop context; manual git fallback created the isolated sibling checkout. No other checkout's Library was copied.
- Final runtime/build/test source `e0ea2d155fca2a6190b38c184d3399104373e4df`. Subsequent evidence/report commits do not change Assets. Exact PR head is available in the Draft PR and delivery message.

## Reuse of original swarm code
Six original C# files imported with all16 original `.meta` files byte-identical, preserving their GUIDs. Five C# files remain byte-identical: phase enum, standalone demo UI, editor demo builder and both test files. The actual in-game `ReactorDefenseObjective.cs` retains117/121 original lines, measured by exact ordered line matching; it is extended for external HP binding and explicit wave-goal completion. Original timed standalone/demo behavior is preserved and tested; the demo UI/editor are not claimed to be the live game's HUD. The host uses the original objective's validation, start/reset/terminal states and events; its timer is explicitly disabled for wave completion. Canonical HP and all real weapon damage remain P12, read directly through `IReactorIntegritySource`, not a second combat HP store. Exact original/current hashes and counts: [original-reuse.json](Evidence/OBJ001/original-reuse.json).

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
| OBJ001-review-red-cc7cbb8a3e904b50b887445e0f186ee6 |0/1/0 |2 | External canonical HP0 incorrectly produced Won on manual Tick |
| OBJ001-review-green-c41dcc6cdab8454ca74b96b5faa39f23 |29/0/0 |0 | Review fix plus previous28; disabled/NaN/zero ticks remain inert, valid fatal deadline loses |

These are separate runs, not summed unique tests. API lifecycle tests deliberately damage enemy cores to inspect progression/reward semantics; they are not claimed as ordinary-input victories. Physical tests use real P17 rounds: friendly25 damage leaves reactor600, hostile25 reduces it to575. Actual turret fire at controlled positions reduced reactor600→575.5199 and player part HP13780→13755.52, with distinct reactor/player target roots. Tests also cover3wave win, one ledger/unique IDs, preserved player damage, one reward, garage→normal fight, fatal reactor hit+final clear loss priority, restart cleanup, pause/intermission, duplicate entry and sandbox budget isolation.

## Windows and actual play results
Build run `20261024T235249Z-a46fd24747f9429ca8fd488a5fa9cc1b`: retail103294717 build-report bytes, QA103386406, both0errors/4 existing unused prototype-input field warnings, actual Editor exit0. Unity6000.3.14f1/D3D11; no scene, prefab, GUID or package replacement. Cecil assembly inspection confirms the original runtime objective and host actor in both builds, all five QA harness types absent from retail, and no test assemblies shipped. See [binary audit](Evidence/OBJ001/binary-audit.json).

- **PASS — native controlled API/physics,1280×720:**21 unique assertions,0fail/0console errors, native exit0. The fixture logged364 evaluations because the no-intermediate-reward assertion is checked repeatedly during intermissions; this is not364 distinct tests. Actual enemy projectiles reduced reactor600→571.4399 and aggregate player partHP13780→13751.44. It then deliberately cleared enemy cores through canonical damage APIs to verify3waves,7kills, one authored reward, bonus/loot→garage→standard fight, reactor fatal loss/no reward, two real scene restarts and shot/object cleanup. Explicitly **not ordinary-input gameplay**.
- **PASS — operator menus plus ordinary-input combat pilot,1920×1080:** Heavy starter,0scrap prep garage, actual3waves against active AI. Pilot uses virtual WASD/LPM and aiming, with no HP/damage/reward/currency shortcuts. Reactor finished493.9197/600HP. Standard reward screen credited80scrap; operator chose one armor bonus and one enemy wheel, reached garage with183scrap/2inventory items, and launched normal stage2 with no reactor.239 physical rounds across defense plus subsequent fight, peak12shells/11debris. No console errors. This is a native input-driven test, **not a human full expedition**.
- **PASS — HUD inspection:** captured720p and1080p menu/preparation/combat/wave/reward/garage/normal-route frames show readable reactorHP/wave/enemy counts and reward title, without overlap with the weapon panel. Full-resolution PNGs retained.
- **PASS — extracted retail launch and human close:** all204 runtime-file SHA256s and ZIP CRC verified after fresh extraction; actual extracted EXE started. User reported normal manual Alt+F4; retained process handle confirms native exit0 at2026-10-24T23:58:25Z, with no UI automation attached. This is current OBJ001RC1 evidence, distinct from previous B01.
- **FAIL / unresolved — automation-attached shutdown:** after the successful input test, automated Alt+F4 reached OnApplicationQuit but process exited`-1073741819`/`C0000005`. It reproduces the inherited Unity/accessibility shutdown problem. No shutdown fix or independent B01 acceptance is claimed. API fixture exit0 and current human retail exit0 bound the observation; they do not prove an engine-level cause.

At60FPS cap on Ryzen9950X/RTX5080, defense42.47s/2548 sampled frames, peak3NPC/226parts: median16.667ms,p9516.670ms,p9916.719ms,max18.674ms,peak allocated142216627bytes. Next ordinary fight peaked259parts,p9516.670ms. Max observed transition64.4ms; no stuck transition. These are bounded QA frame-time observations, not an uncapped benchmark or proof for other hardware. JSON/process records and selected PNGs: [evidence directory](Evidence/OBJ001/).

## Fixed problems and review
- Original bootstrap had no actual host entry, game HP bridge, waves, target policy or reward/garage lifecycle. Added opt-in integration using existing systems, without replacing the standard expedition.
- Explicit wave completion now requires a live original objective. Canonical reactor death beats simultaneous final enemy clearance, so no posthumous victory/reward is granted.
- Manual Tick review regression: synchronize external HP before advancing a valid clock; retain disabled/invalid tick inertness. Observed RED then29/29GREEN.
- Single shared salvage ledger and per-wave unique enemy IDs prevent lost/duplicate recovery records; player part damage/cooldowns persist. Existing reward settlement runs once after wave3.
- Sandbox entry is rejected to prevent spending its free workshop budget in rewarded defense. Final boss slot remains unchanged. Actor/projectile cleanup is verified on win, loss and restart.
- One fresh whole-branch reviewer:0Critical/1Important/0Minor. Important fixed with regression; no deferred minor findings. Native UI, binary/ZIP correspondence and full-suite questions were explicitly deferred to execution, with actual evidence above and full-suite status below. No second review cycle.

## Full regression and release
- **PASS — full current EditMode:1021/1021,0FAIL/0SKIP, actual native0**, `20261024T235608Z-7f607d714df1449092cca7e1d59bb9b2`,34min30s. Includes original package and corrected inherited physics fixtures; this is a fresh full green run, not reconciliation of previous partial results.
- **PASS — full current PlayMode:401/401,0FAIL/0SKIP, actual native0**, `20261025T003101Z-a29cce66a0f346a99176431bb323e04d`.
- Raw full-suite XML/process records are compressed in [unity-results.zip](Evidence/OBJ001/unity-results.zip); exact counts, hashes and process times: [full-unity-results.json](Evidence/OBJ001/full-unity-results.json). Assets remain identical to the tested runtime source, tree`f419cfec0d65eb512c62213ce509265da0861319`.
- Repository validator:0errors; final file count retained in evidence. Final Python CI unit suite16/16PASS, actual normal-user exit0, after the release-helper change; the separate initial16-test baseline is not added to this count.
- Fresh archive: `C:\Users\kubap\Desktop\Prototypy\Releases\RoboRoguelike_OBJ001_RC1_Windows_x64.zip`,38496536bytes; SHA256`a44718196aa3b4eeb4d57274fd910d45b538f21b6735a0ef2453ae4f5cde13ba`. EXE/data/Mono/runtime dependencies included. [Manifest/extraction result](Evidence/OBJ001/release-package.json).
- Extracted EXE: `C:\Users\kubap\Desktop\Prototypy\Releases\OBJ001-RC1-extracted\RoboRoguelike_OBJ001_RC1_Windows_x64\RoboRougelike.exe`.
- Final [Draft PR #210](https://github.com/C8LyNn/RoboRougelike/pull/210), targeting `codex/b01-phy-grounding-core-r1`; no shared merge.
- Fresh human six-stage expedition, exhaustive vehicle/build/balance matrix, other GPUs and independent PHY acceptance: NOT_RUN in this iteration. Existing targeted/whole suites establish only their actual covered cases.

## Integration decisions
User authorized implementation without additional approval menus. PR125 is used as an explicit stacked dependency because grounded physics was required; its draft/independent acceptance remains visible. Native managed-worktree creation could not find Git at projectless Desktop, so a new sibling git worktree was created and retained for review. An optional existing hit-router team interface protects reactor friendly fire consistently. The static reactor uses canonical P12HP, one authority, and no new Rigidbody solver. Original timer/demo remains available while the game's waves use explicit completion. Transient build scripting defines were restored; no generated ProjectSettings change is committed. MAPGEN and independent model branches remain excluded. If upstream changes or balance assumptions change, rebase and targeted/native retesting are required. Final execution ledger is preserved with the evidence.

Stop at a Draft PR with actual outcomes and limits, targeting the PR125 branch; no main/develop/shared integration merge and no next package implementation.

# W5 Release Candidate QA

**Decision: READY for playable Windows RC1, with the unresolved UI Automation shutdown limitation below.** This is a candidate for review, not an approval to merge. Verified on 2026-10-23 (Europe/Warsaw). No new mechanics, models, production gameplay changes, engine upgrades or package upgrades were introduced in W5.

## Source and release provenance

| Role | SHA |
|---|---|
| develop / starting point | `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1` |
| W2, PR #1 | `a0f0a6590b07ed2f5007361a7e761f133e52ff99` |
| Verified W3/W4 game/test source | `5d4b61d1160e9cceac888e070147aead71dfb92b` |
| Integrated W3/W4, PR #19 head (later documentation only) | `916bf6d195bb7647f159a4dc50a4d6b2ab18b063` |
| W5 history-preserving integration | `4eba68b3a79e749adeb7cfc7d6ecaf9d60cb440f` |
| **Final tested playable/build source** | **`2d98906b47d76364c37a875527257b03d40e4ec9`** |

Release branch: `release/rrl-w5-candidate`, based on develop, merged W2 and then PR19 with `--no-ff`, without conflicts. PR19 already includes PR17/18; neither was imported again. The initial Assets/Packages/ProjectSettings tree exactly matched PR19. W5 adds only `RRLW5RuntimeDiagnostics.cs` and its meta to Assets, entirely inside `MOVSERIES_VISUAL_QA`; retail excludes that class. Later packaging/report/evidence commits do not change the tested Unity source. Source PRs and develop/main have not been merged or altered by this release.

Unity remains **6000.3.14f1**, Windows x64 Mono. The repository validator checks indexed metas/GUIDs, serialized references/scenes, required configuration and package lock. No missing/orphan/duplicate meta, unresolved reference or dependency error was found. Source, tests and generated evidence remain separate from ignored builds/logs/dumps.

## Fresh W5 verification

| Gate | Result | Local artifact directory under Artifacts/Local |
|---|---|---|
| EditMode | **434/434 PASS**, 0 failed/skipped, native 0 | `20261022T224846Z-fd35d8e482184b5aaee4987bd9856e36` |
| PlayMode | **176/176 PASS**, 0 failed/skipped, native 0 | `20261022T231034Z-4cd5374b08c647be874b431c20276c7a` |
| CI Python validator self-tests | **16/16 PASS** | writable isolated CI-Python-Temp |
| Windows retail + QA build | **Both Succeeded**, native 0, 0 errors; 4 existing CS0414 warnings each | `20261022T224222Z-69ac396d861f428ca0a9cc73887e30e4` |
| ZIP CRC + exact members + extracted SHA256 | **PASS**, all 186 runtime files | `W5-ZipSmoke-b8c6253f7c62404c83585439c38a585a` |
| Extracted retail startup + human Alt+F4 | **PASS**, ready/responding, 0 detected errors, **native exit 0** | `W5-Retail-1280x720-a19a78e86c514ec2ac42a6766cba6885` |

These are fresh W5 executions (610 Unity cases total), not the inherited W3/W4 baseline. The single clean W5 build predates the diagnostics commit but its Assets/Packages/ProjectSettings contents exactly match that commit. Final full suites ran against that same source; subsequent changes only package and document it. There was no reason to rebuild or repeat unchanged full suites. Retail build report: 102,843,460 bytes; QA: 102,875,835 bytes (Unity report totals, not ZIP size).

Retail Assembly-CSharp SHA256: `c0de470062169fe99b75205936088f58815986462fada52b5cdc7450512492de`. The extracted copy matches. Retail contains none of the four automation classes `RRLW5RuntimeDiagnostics`, `RRLW2RuntimeQA`, `RRLW34NativeQA`, `MOVSeriesVisualQA`; the separate QA assembly contains them. Evidence: [test summaries](Evidence/W5/unity-test-summary.json), [retail smoke](Evidence/W5/retail-smoke.json).

## Actual native gameplay — 1280×720

**PASS: menu → Heavy vehicle → all six real fights → final reward → Won → restart.** Seed 2026; 139 initial physical parts, 12 wheels, 180 core HP. The existing input pilot supplies ordinary virtual steering/aim/fire input. Physical projectiles and enemy AI resolve combat; the operator chooses routes/rewards and performs garage actions through native UI. **No damage, reward or currency APIs were used for this campaign.** This is input-pilot gameplay, not a claim of a human manually driving the entire campaign.

| Stage | Chosen encounter | Enemies defeated |
|---|---|---:|
| 1 | First Scrap / scrap yard | 2 |
| 2 | Open Convoy / desert | 3 |
| 3 | Industrial Crossfire | 4 |
| 4 | Elite Bastion / scrap yard | 2 |
| 5 | Relic Raiders / scrap yard | 3 |
| 6 | Siege boss / desert | 1 |

Outcome: **6/6 victories, 15 defeated enemies, 847 physical rounds, 6 bonus acquisitions**; final mandatory bonus claimed. The fifth encounter uses a valid open route for the simple pilot; no navigation/game code was changed and no fight was skipped. The observer recorded 219.707 seconds / 13,182 combat frames. Parts visibly detached and physical debris peaked at 9.

Verified native garage/reward actions:
- First reward: exactly one hull bonus from three choices plus three enemy parts (destroyed automatic gun + two wheels); inventory 4. Later fights accepted zero recovered parts, preserving the independent 0–3 choice.
- Paid hull repair charged 9 scrap (99→90). The recovered 0-HP gun could not be mounted; paid rebuild charged 36 (90→54), restored it to 100% HP, and its condition persisted through subsequent maps and final inventory.
- Rotated/mounted the hull bonus through a green preview: core HP 180→216. Mounted reload on another free plate, then core bonus onto the reload module's attachment port: 216→324. Inventory-only rewards remained inactive. Three mounted bonuses persisted without repeated stacking across maps.
- Further paid repairs charged 8, 32, 18 and 54 scrap. Final balance 686, six inventory items, three mounted bonus modules / 142 physical parts.
- Final Won screen showed 6/6, 15 enemies and 6 bonuses. Operator selected “Zagraj ponownie”; Ready/menu restored the default Standard selection, completed stages/currency/inventory/acquisitions all 0 and baseline effects all 1. Diagnostics confirm `sawVictory=true`, `restartVerified=true`, errors empty.

No hang occurred across combat, salvage, garage, route/map, Won, restart or menu. The diagnostic Transitioning-flag duration peaked at **66.956 ms**; this is that flag's duration, not a total scene-load benchmark. Observed automated closing after the campaign returned C0000005 as classified separately below, after the successful restart.

Evidence: [native observations](Evidence/W5/native-gameplay.json), [reward](Evidence/W5/ui-one-plus-three.png), [rebuilt wreck](Evidence/W5/ui-recovered-wreck-rebuilt.png), [final victory](Evidence/W5/ui-final-victory.png), [restart](Evidence/W5/ui-restart-menu.png).

## Separate API/native graphical fixture — 1920×1080

**29/29 PASS** using the existing W3/W4 lifecycle fixture. It explicitly seeds victory/module loss with damage APIs and later uses repair/range APIs; these are not additional actual campaign victories. Operator UI selected one damage bonus plus three parts, rotated the four-cell module (final green Y270 preview), and mounted it. R/F, Z/X and Q were exercised at 1080p; Q/E were also exercised in the 720p campaign.

Next-map persistence, module loss, paid 60-scrap repair/remount, range and workshop restoration passed. Effect sequence: baseline 1 → mounted 1.25 → destroyed 1 → repaired/remounted 1.25. Same module ID, one structural owner/collider and 250 HP were verified. No game errors. [Fixture samples](Evidence/W5/native-api-fixture-1080.json), [1080p rotation](Evidence/W5/ui-rotated-1080.png).

The fresh Unity suites additionally cover bridge/attachment-point loss, disconnected parts, 64 XYZ rotations, physical ownership/forces, recovery of destroyed modules and the original MOV001–009 systems. Those fixtures are API/automated evidence, not additional manual gameplay claims. The two native runs cover 720p and 1080p; this is not an exhaustive human usability test of every route/loadout.

## Performance during the actual six fights

Direct3D12, NVIDIA RTX 5080 / AMD Ryzen 9 9950X. QA pilot cap: **60 FPS**. Combat samples use `Time.unscaledDeltaTime`; enemy/part/debris/memory peaks sampled at 1 Hz. “Parts” means active P12 damageable components. A batch Unity regression ran concurrently; QA/background overhead is included. These are observed frame times on this machine, not isolated GPU headroom or minimum-spec claims.

| Stage | Frame samples | Peak enemies / parts / debris | Median ms | P95 ms | P99 ms | Max ms |
|---|---:|---|---:|---:|---:|---:|
| 1 — Pierwszy złom | 669 | 2 / 187 / 9 | 16.667 | 16.670 | 16.725 | 16.824 |
| 2 — Otwarty konwój | 2900 | 3 / 259 / 8 | 16.667 | 16.670 | 16.723 | 16.977 |
| 3 — Ogień krzyżowy | 2261 | 4 / 265 / 8 | 16.667 | 16.670 | 16.705 | 16.939 |
| 4 — Elita: Bastion | 1183 | 2 / 238 / 5 | 16.667 | 16.670 | 16.742 | 16.947 |
| 5 — Łowcy reliktów | 2794 | 3 / 262 / 6 | 16.667 | 16.671 | 16.732 | 17.076 |
| 6 — OBLĘŻNIK | 3381 | 1 / 259 / 6 | 16.667 | 16.671 | 16.778 | 17.362 |

All six fights held the observed 60-FPS cap with combat max **17.362 ms**, P95 no greater than **16.671 ms**, peak 4 enemies / 265 parts / 9 debris. Peak sampled Unity allocated memory: **161.62 MiB** (not process working set). Frame sampler recorded 13,188 frames; the independent input observer recorded 13,182 because their sampling boundaries differ. No measured combat stall or game error justified a production optimization. [Full measurements](Evidence/W5/combat-performance.json).

## C0000005 shutdown investigation — unresolved engine/UIA limitation

All controlled comparisons use Unity 6000.3.14f1 / Direct3D12. A live UI Automation client differs from merely making no new calls. Application.Quit is reached before native exit in these controlled QA probes.

| Controlled case | Result before native shutdown | Native exit |
|---|---|---|
| Previously verified QA binary, existing API fixture, no UI observation | 49/49 assertions | 0 |
| Same binary/fixture, explicit Sky UIA observation; self-closes without Alt+F4 | 49/49 assertions | `C0000005` |
| W5 empty scene, prior UIA client retained, no new per-window call | 0 controllers/cameras, 0 game errors | `C0000005` |
| W5 menu after releasing UIA client; no observation | 1 controller/camera, 0 game errors | 0 |
| W5 empty scene after releasing UIA client; no observation | 0 controllers/cameras, 0 game errors | 0 |
| W5 empty scene, fresh explicit UIA observation, timed self-close | 0 controllers/cameras, 0 game errors | `C0000005` |
| Extracted retail, no UIA client, human Alt+F4 | Ready/responding; human confirmed correct close | **0** |

Windows own-app fault metadata identifies UnityPlayer.dll `6000.3.14.35903`, RVA `0xb1b031`. The matching bundled PDB's public symbol places it at `RuntimeStatic<PlatformAccessibilityManager,0>::StaticDestroy +81`; a second recorded RVA `0xb29715` lies at `PlatformAccessibilityNodeRoot::get_BoundingRectangle +37`. PDB GUID/age match the DLL's CodeView signature. These are nearest public functions, not a full reconstructed stack; no game C# frame is asserted. [Sanitized symbol evidence](Evidence/W5/shutdown-symbols.json).

**Classification:** reproducible native Unity accessibility/UIA-client lifetime interaction in this test environment, independent of vehicles, cameras or run state. Self-issued Application.Quit also triggers it, so Alt+F4 alone is not the cause. Releasing the test client permits clean menu/empty-scene shutdown, and the extracted retail human close exited 0. This is a QA isolation procedure, **not a game/engine fix**. Accessibility remains enabled; engine/packages unchanged. Other machines/assistive clients and a general Unity fix are not proven. The known limitation does not prevent this RC's observed normal gameplay/startup/close, but remains open for subsequent engine/environment investigation.

## Delivered Windows artifact

- File: **`RoboRoguelike_W5_RC1_Windows_x64.zip`**.
- Accessible location: **`C:\Users\kubap\Desktop\Prototypy\Releases\RoboRoguelike_W5_RC1_Windows_x64.zip`**.
- Size: **38,301,521 bytes / 36.53 MiB**.
- SHA256: **`1f7f2cd735b67680fd2cf26c15081061d5cc8d215bc059ca05a1135274f9eaa8`**.
- Contains EXE, Unity data/managed assemblies, UnityPlayer/CrashHandler, MonoBleedingEdge, D3D12/runtime libraries; **186 runtime files**, Polish README and a per-file SHA256/source manifest. PDB/DoNotShip/backup files excluded.
- `Tools/Release/package_windows.py` created the archive, verified CRC/exact safe members, extracted to a new directory and matched every runtime file's bytes/hash plus generated README/manifest.
- Fresh extracted EXE: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike-W5-RC1\Artifacts\Local\W5-ZipSmoke-b8c6253f7c62404c83585439c38a585a\RoboRoguelike_W5_RC1_Windows_x64\RoboRougelike.exe`.
- Extracted retail launched visibly at 1280×720, logged game readiness, responded with 0 detected errors; human Alt+F4 confirmation and process **exit 0** recorded. No QA client attached for this control.
- No binary-release upload capability is available through the connected GitHub tools; the complete local ZIP above is the delivered artifact. No ZIP/log/dump/license material is committed. [Package metadata](Evidence/W5/package.json).

The first local packaging attempt had an incorrectly supplied placeholder source SHA; it was quarantined, never published and is not the artifact counted here. The delivered archive was regenerated with the actual Git-resolved source SHA and reverified.

## Changes, limits and publication

W5 repaired no production gameplay defect: the integrated systems passed the complete observed campaign and fresh regression. Changes are QA-only shutdown/performance/restart instrumentation, a redistributable packaging helper, release history, report and curated evidence. Existing CS0414 warnings remain. The unresolved native UIA shutdown crash is not relabelled as fixed.

One fresh, read-only final review of repository candidate `22fc4aa0d2c89ccd26065aefc219cf40141769fb` found **no Critical or Important issues**. Its one Minor finding was a report balance typo (688 instead of the recorded 686), corrected directly against the Won observation without changing game source or repeating gameplay. Reviewer independently checked ZIP/extracted files, assembly/source provenance, original Unity XML/build/native-exit evidence, six-stage/restart observations and the bounded shutdown/performance claims. [Execution decisions and review limits](Evidence/W5/execution-record.md).

Published **[Draft PR #20](https://github.com/C8LyNn/RoboRougelike/pull/20)** from `release/rrl-w5-candidate` into **develop**. Reviewed repository candidate SHA is above; final playable source remains `2d98906b47d76364c37a875527257b03d40e4ec9`. Subsequent reporting-only commits close the QA/publication record and do not alter the runtime source or delivered archive. Exact current publication HEAD is available in the PR and the final handoff. No merge to develop/main or source-PR changes occurred.

Remote CI on reviewed candidate `22fc4aa0d2c89ccd26065aefc219cf40141769fb`:
- **[CI-01 PASS](https://github.com/C8LyNn/RoboRougelike/actions/runs/39871626712)**: Python regression tests and indexed Unity repository integrity both succeeded.
- **[CI-02](https://github.com/C8LyNn/RoboRougelike/actions/runs/39871626889) / [CI-03](https://github.com/C8LyNn/RoboRougelike/actions/runs/39871626822): BLOCKED by missing required Unity activation**. Repository validation passed; the activation preflight failed; both Unity execution steps and result gates were skipped. **Cloud Unity tests NOT RUN**, not failed gameplay tests. Fresh local 434/176 results above remain valid.
- CI-04 does not trigger for this release-branch PR; local Windows build/extraction/startup passed. No activation workaround, secret inspection or license publication was attempted.

**Final decision: READY for the delivered playable RC1 with the documented UIA shutdown limitation. Cloud Unity CI remains BLOCKED on activation; the PR stays Draft and no merge is approved.** Reporting-only pushes may produce new CI runs; the linked outcomes are explicitly tied to their exact SHA rather than claimed for another revision.

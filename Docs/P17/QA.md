# PASS 17 — LD02, garage checkpoints and native QA

Game/build source `e605d4a874117ab9f5315b96bcf490e22e0589d5`; branch `feature/pass17-run-save`; draft #382 based on #310 at `be2c4b6ac63e04eff5b046e50641ec8d1da4837f`; Unity 6000.3.14f1, Windows x64. No merge to main/develop. Report/evidence commits made after this code do not alter the build.

## LD02

Commit `682dc0c5c13615577334375ffb6c27be5cdebf4a`. The exact approved MapDocument `Docs/CX08/Fixtures/LD02_ARENA.json` (SHA-256 `50e5b7e84bc51425080e91199abab96266a5615b84aaa919caa9cfff2fa3c115`) is packaged in Resources. Garage selection uses the existing encounter, damage, salvage, reward and AI systems; it hides and restores the legacy floor, clears map and returns through the canonical state machine. The authored arena is not MAPGEN and has no seed requirement.

Native Unity Editor 6/6 passed for light/heavy physical spawn clearance, AI projectile, physical wall blocking a shell, loss, and two successive victory transitions including one-time payment, part IDs and repair. Victory/loss were applied by API damage, not unassisted gameplay. Earlier native LD02 8-ray and light/heavy R6 straight traversal passed and MAPGEN regression passed. Additional native physical tests on gameplay base `be2c4b6a` with a QA-only fixture: hover, omni and tracks each drove horizontally across the arena, 3/3 PASS; hover pods detected the ground in 150 fixed steps. The first attempt failed to install tracks in invalid cells; its test configuration was corrected using real garage mounting rules, with both attempts retained in Evidence/Drive. These are scripted tests and do not establish every custom construction or manual camera visibility. A cover test initially waited one physics step and failed; fixed-step diagnostic demonstrated the wall stops the shell; no LOS policy was added.

## Save

Commits `1167874997d8c494abd9e17bb1962e2394bb47f2`, `2e424cf9dd3292e61e5996b340a602204e76585a`, `e605d4a874117ab9f5315b96bcf490e22e0589d5`. Save is explicitly offered in the ordinary expedition garage; the main menu offers load into a fresh run. It uses a versioned payload, SHA-256 corruption detection, atomic same-directory replacement and bounded/validated model data. It preserves parts, IDs, condition, inventory, resources, progress, settings, saved cooldowns and weapon-group assignment; it refuses a second load over an active run and safely rejects unsupported or corrupt data. Map-editor saves are separate. This is a single garage checkpoint, not mid-combat autosave or a migration of older unknown formats.

- Native targeted EditMode: 5/5 passed, including post-victory save/restart/load, owned IDs, damage, balance, duplicate load rejection, remount of gun/factory wheel, next combat, corrupted data, version rejection and weapon-group preference.
- Related LD02 / weapon-group / map-editor regression: 22/22 passed before the final batch-only QA harness change.
- Windows Player from extracted final ZIP: 3/3 LD02/save scenarios and 3/3 existing garage/map-editor/suspension regression scenarios passed, each process exit 0. Separately, group-aware aiming with seven weapon types at 10, 30, 60 and 100 m passed 28/28 stationary-target checks (16 A/LMB, 12 B/RMB), exit 0; weapon-specific range expectations were preserved. This is not a moving-target or human test. Light two encounters, enemy physical shell and two one-time payouts; Heavy defeat; save/restart/load preserving HP, IDs, balance and actual next combat. The success/defeat outcomes use controlled core damage. Manual GUI/gameplay, visual camera and player-controlled wins remain NOT_RUN.
- Initial test-harness error: Unity domain reload discarded the temporary path, and one test wrote a checkpoint to `C:\Users\kubap\AppData\LocalLow\DefaultCompany\RoboRougelike\Run\checkpoint.json`. The identified test-generated file was moved intact to `Artifacts/Local/P17Save/misdirected-test-checkpoint.json`; whether an earlier file existed and was overwritten is unknown. Tests now set and assert a temporary path after entering Play Mode; Player QA requires an explicit separate test slot.

## Native suite and build

- Unity compile and Windows x64 build: PASS, exit 0 on `e605d4a8`.
- Full PlayMode: PASS 474/474, 0 skipped, exit 0 on `e605d4a8`.
- Full EditMode on the prior LD02 base `be2c4b6a`: FAIL 1320 passed / 3 failed / 1 skipped, exit 2. Camera scroll zoom, MAPGEN heavy wheel contact, and MOV009 raised wheel test each passed isolated rerun (3/3); causes not established. The skipped policy-conditional test was intentionally not enabled. Do not reclassify the full run as PASS.
- Full EditMode on `e605d4a8`: INTERRUPTED without XML when a checkpoint reload defect was found; no PASS/FAIL claimed. Full tests have been restarted on corrected source `203a0805251ed28293888e43f2d49f0df63767c6` in Draft PR #383.
- The `e605d4a8` test ZIP below has a confirmed checkpoint defect: an unmounted railgun whose reload expires in inventory may remain empty after save/load and remount. Fix `203a0805` passed native targeted tests 6/6 and a corrected build is being prepared. Do not classify this ZIP as the final stable build.
- ZIP [Google Drive](https://drive.google.com/file/d/18dC9TNG5QbxXQ5n9SuaaLeqsRT8N5S8b/view?usp=drivesdk), 39,634,604 bytes; SHA-256 `54794deec766e647455b4efd3d645f98a6e9376671a7866f88b7d81ec5c3c0cd`. All 212 extracted files matched staging SHA-256. Drive size matched; connector did not expose a remote hash.

## Five-minute playtest

Extract the whole ZIP; run `RoboRougelike/RoboRougelike.exe`. Choose a vehicle and `Przygotuj pojazd`; choose `Arena LD02 • walka` in the garage. Fight, select rewards, inspect damage and repair. In the garage select `Zapisz wyprawę`, quit, restart and choose `Wczytaj wyprawę`. Verify parts, balance and progress, then start another fight. Check camera/aiming and physical cover. A garage checkpoint does not save the middle of a fight.






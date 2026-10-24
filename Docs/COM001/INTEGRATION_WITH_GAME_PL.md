# COM-001 — izolowana implementacja skanera konstrukcji w rzeczywistym Gauntlet

Status: **source implemented, Unity compilation / gameplay QA NOT RUN**.
Branch: `feature/com001-parallel-readonly-scan`, based on `develop` `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`.
The original package had no Unity .meta files. This branch adds deterministic unique GUIDs to the imported package; these are NEW assets only.

## New components
- `Assets/RoboRougelikeMechanics/COM001StructuralScan/**`: original standalone source, tests and asmdefs.
- `Assets/R6/COM001LiveScanPartAdapter.cs`: maps real P12 Damageable, collider identity, attachment and current HP into an IStructuralScanPart view; no duplicate HP/structure.
- `Assets/R6/COM001ProductionScanBridge.cs`: opt-in scanner bound to real player sensor and registered enemy parts; does not cause damage or modify enemy AI.
- `Assets/Editor/COM001ScanLiveEnemyMenu.cs`: manual test action, only while Editor Play is active.
- `Docs/COM001/**`: source integration instructions and original static QA reports.

## Manual smoke test
1. Open existing `Assets/scene 1.unity`, press Play and start an encounter. Get near enough to a living enemy to see the exposed blocks.
2. Use `Tools > RoboRougelike > COM-001 > Scan Visible Enemy Parts (Play Mode)`.
3. Inspect the Console: up to 16 actually visible part contacts, each with an existing HP fraction and heuristic priority.
4. Verify that blocks behind obstacles are not exposed, destroyed/detached parts are excluded and scanning again during cooldown does not produce new results.
5. No permanent scene changes or player keybinds are introduced. This is only an opt-in diagnostic UI, not the final feature or HUD.

## Outstanding / Codex follow-up
- **Unity source compilation and full package EditMode/PlayMode NOT RUN**. Project has no working cloud activation credentials.
- Confirm imported Unity `.meta` consistency and any test asmdef references; CI-01 validator is not a Unity compile.
- Check collider geometry, LOS and target selection in real Gauntlet. Multiple body colliders may cause occlusion or fail-closed results.
- Test four-enemy wave from PR #1 after that PR is independently validated; do not merge branches blindly.
- Add final HUD, energy usage / mechanic item, input binding only after passing real tests, avoiding modifications to W2 R1/R3/R4 files while Codex is working.
- Scan currently picks nearest visible foreign owner; does not guarantee selection of the vehicle under cursor.
- Run all 522 baseline MOV regression tests and report PASS/FAIL/NOT RUN with SHA and logs.
- Keep this PR draft and isolated; do NOT merge to `develop` or `main`.

## Ownership and conflict guard
COM-001 owns only NEW `Assets/RoboRougelikeMechanics/COM001StructuralScan/**`, `Assets/R6/COM001*.cs`, `Assets/Editor/COM001*.cs`, `Docs/COM001/**`.
No current PR #1 reward/combat, PR #3 map runtime nor PR #2 documentation files are modified.

# CODEX DISPATCH — integration of archived RoboRougelike packages

**START NOW / SOURCE GATED / NO AUTO MERGE**

You have the up-to-date GitHub repo `C8LyNn/RoboRougelike` and a separate, verified kit `RRL_CODEX_READY_ALL_PACKAGES_R1.zip`. Without access to kit source bytes stop: `WAIT / PACKAGE_UNAVAILABLE`.

1. Inspect actual `develop` HEAD, existing PR #1, `README.md`, `Docs/DEVELOPMENT_WORKFLOW.md`, classes R1/R3/R4/R5/R6 and existing Unity `Assets/scene 1.unity`.
2. Read the kit's `README_START_HERE_PL.md`, `docs/CODEX_MASTER_DISPATCH_PL.md`, `docs/CURRENT_GITHUB_CONTRACTS_PL.md`, `docs/INTEGRATION_ORDER_PL.md`, `docs/QUALITY_GATES_PL.md` and `packages/<ID>/CODEX_TASK_PL.md`.
3. Verify outer and internal SHA-256 with `python verify_and_stage.py --verify-all` before extracting. Recognize MAP R6 as the single successor to R3/R4/R5, VFX v5 as the single successor to v2/v3/v4, and MOV-001–009 as ALREADY IN REPO. MOV-010 remains WAIT.
4. Create independent feature branches/worktrees from verified clean `develop`. **One package, one PR**, no mass import. Run dry-run staging first; `--apply` only with verified paths/GUID and clean feature branch. Never overwrite existing scenes/code, renumber enums, regenerate current .meta, or modify root Rigidbody physics for convenience.
5. Compile source in Unity 6000.3.14f1. Run original module EditMode/PlayMode + full baseline MOV001–009. Build actual production adapters: real damage, inventory, 3-slot salvage, garage mount, AI, gameplay UI, detach, repair, projectiles, owner/team, state changes. Independent demo PASS is not Gauntlet PASS.
6. Run actual gameplay: garage → equipped module → Gauntlet / target → hit, death, salvage, repair → next fight, including AI and 4 enemies. Validate Windows build if available, capture actual stdout/XML/SHA and distinguish PASS / FAIL / NOT RUN.
7. Submit a scoped Draft PR to `develop` with G0–G5 and accurate evidence. No direct `main` or `develop` changes, no merge, no release, no force push, no Unity version upgrade.

Recommended first production implementation after W2 fixes: WPN-001 + one shared weapon/damage integration pattern; then DEF-001, WPN-002, WPN-003, WPN-004, DEF-002, WPN-005, WPN-006, COM-001, MOV-011, MAP-R6, VFX-001-v5. MAP/VFX source QA may proceed independently in isolated checkout without production integration.

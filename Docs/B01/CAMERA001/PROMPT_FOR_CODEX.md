# PROMPT_FOR_CODEX — CAMERA001 R1 (copy-paste, gated)

You are CODEX, RoboRougelike's sole target-game integrator. Scope: one isolated B01 CAMERA001 visibility package. Do not implement alternative wheel physics, MAPGEN, RAC, weapons or economics.

SOURCE: https://github.com/C8LyNn/RoboRougelike/pull/134
SOURCE BRANCH: feature/b01-camera001-s02-r1-isolated
IMMUTABLE SOURCE SHA: read the exact pin posted by B01-00 in Issue #66 / PR after source freeze; reject moving heads or mismatched content.
SOURCE FILES: Assets/B01Packages/CAMERA001/** plus original parent .meta as needed.
B01 QA GATE AT CREATION: G1 UNITY NOT_RUN / G2 HOLD / NOT SENT. THIS PROMPT DOES NOT AUTHORIZE GAME INTEGRATION.

PHASE 0 — READ-ONLY ACK:
1. Report current target branch/HEAD SHA, clean/dirty worktree, active camera physics tasks and reserved files. Inspect current P12_PipelineSafeCamera, R3VehicleAssembly, W8 scene and Codex PHY PR#125.
2. If camera work is already in progress or source overlaps, STOP and post path-by-path conflicts to Issue #66. Do not edit existing worktree.
3. Check B01-60 independent Unity QA and B01-40 gate reports for exact pinned source SHA. If they are not PASS and B01-00 has not written READY_FOR_CODEX, STOP BEFORE INTEGRATION. You may inspect or test source in a disposable checkout without host changes.

PHASE 1 — ONLY AFTER G1/G2 AND EXPLICIT SCOPE ACK:
1. Create YOUR OWN isolated Codex integration branch from the confirmed game target SHA (never main/develop direct).
2. Import entire CAMERA001 directory with .meta and asmdefs, preserve existing parent GUIDs. Do not use only a PR diff or copy untested helper files selectively without documenting hashes.
3. Run standalone CAMERA001 EditMode and PlayMode including 840 framing proposals and world collision cases. Open CAMERA001_Demo at 720p/1080p, Light/Heavy, pitch extremes, full orbit, zoom and walls; save NUnit XML/Editor.log/screenshots and exact exit codes.
4. Choose ONE camera ownership strategy: adapt Camera001Framing into P12 controller OR explicitly replace active Camera transform driver with Camera001Rig. Do not let both LateUpdate scripts write Camera. Preserve existing player mouse/cursor/zoom, weapon reticle/world-ray, projectile origins, game pause/menus and collision policy.
5. Run complete relevant existing Unity EditMode/PlayMode tests, garage/range/combat smoke, tall/wide robots, camera near walls/cover, detached parts, 2 weapon types and PHY#125 contact stability; capture 720p/1080p. Build Windows x64 and record SHA256 only if actually produced.
6. Create isolated Draft PR; no merge, release or deployment. Report actual tests (PASS/FAIL/NOT_RUN), exact integration commits, source SHA, target SHA, logs/PNG/video, regressions, conflicts and rollback.

FAILURE PROTOCOL: On package-originating failure, STOP, attach minimal repro to Issue #66 and return to B01-20 for B01 CAMERA001 R2. New source SHA must be re-tested by B01-60/40, not silently patched in live Codex worktree. On target-specific failure Codex owns its own integration branch fix and reports it. Never claim tests executed unless logs exist.

ROLLBACK: revert only YOUR isolated camera integration commit(s), restore previous P12 camera ownership and prefabs, delete only CAMERA001-owned imports and retain shared .meta, rerun garage/range camera smoke. NO MERGE without owner decision.

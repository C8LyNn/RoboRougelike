# CI41-ASMGRAPH-001 v1.0.0 — Unity asmdef dependency graph (real original Git source)

**B01-41 / PASS17 WAVE06.** Executable read-only CI module. Exactly isolated Tools/B01/CI41/P17W06/ and own .github/workflows/b01-41-p17w06-asmdef-graph.yml. No canonical Assets, Codex, main or develop modifications.

## Function

asmdef_graph_gate.py reads committed original Git object bytes from an exact SHA: all Assets/**/*.asmdef and matching .asmdef.meta. Validates JSON, unique/case-insensitive assembly names, GUID uniqueness, references by local name or GUID, missing original RoboRougelike assembly links, assembly reference cycles and self links, runtime → Editor-only dependencies, EditMode/PlayMode platform contracts, NUnit constraints and meta integrity. Reports deterministic JSON. Unexpected package/external references become warnings rather than invented closure claims. Strict CLI returns **exit 0 only for PASS_SOURCE_GRAPH** and exit 2 for any source defect. No Unity Editor invocation, compilation or game G1 inference.

## Actual original repo scan: two existing REDs

The 27 actual MOV001–MOV009 runtime/EditMode/PlayMode assemblies and 27 .meta GUIDs were scanned on the original repository. The strict scanner found **exactly 2 policy deviations** and returned FAIL_SOURCE_GRAPH, exit 2:

- Assets/RoboRougelikeMechanics/MOV005OmniDrive/Tests/EditMode/RoboRougelike.MOV005OmniDrive.EditModeTests.asmdef — Editor test assembly with includePlatforms=[]; exact original blob c0495aeba249d032a62b923717bd8dd94ee56858.
- Assets/RoboRougelikeMechanics/MOV006MagneticGrapple/Tests/EditMode/RoboRougelike.MOV006MagneticGrapple.EditModeTests.asmdef — same deviation; exact original blob b042546b04fabdab2fd5d0c28957c11cdbccab95.

Both are **source policy findings, not proven native Unity compiler failures**. They are in Codex-owned game files; this package does NOT fix or edit those files. Unity package/TestAssemblies context and native compilation require independent examination before declaring runtime impact.

The separate verify_known_baseline.py + known_baseline.json explicitly check that the **same exactly pinned two original errors, and no new errors**, remain. The original scan result stays FAIL_SOURCE_GRAPH. The baseline status is KNOWN_BASELINE_DEFECTS_OPEN and game_source_g0 HOLD_KNOWN_DEFECTS (never source green). If someone modifies either original blob, adds/removes any error or changes the number of assemblies, the baseline fails and manual QA review is required. This avoids falsely claiming the game is fixed while still detecting new regressions in CI.

## Use

From checkout of the original repository (or a separate utilities copy), pass the real exact 40-hex source SHA and new report paths:

1. python Tools/B01/CI41/P17W06/asmdef_graph_gate.py --repo . --source-sha YOUR_40_HEX_SHA --require-count 27 --out asmgraph-original.json
2. python Tools/B01/CI41/P17W06/verify_known_baseline.py --repo . --source-sha YOUR_40_HEX_SHA --report asmgraph-original.json --baseline Tools/B01/CI41/P17W06/known_baseline.json --out baseline-result.json

For the original committed source, step 1 **returns exit 2**, and step 2 returns 0 only with explicit status KNOWN_BASELINE_DEFECTS_OPEN / G0 HOLD. The GitHub workflow correctly expects strict exit 2 and fails on any unpinned deviation; its own Actions status may be success for *baseline containment*, not healthy gameplay.

## Original tests

- 22 physical real Git init/commit/ls-tree/cat-file/subprocess regression tests (in test_asmdef_graph_gate.py), including invalid JSON, reference cycles, GUID reference/duplicates, test constraints, dirty worktree versus original immutable Git blobs, count drift and exact-commit CLI.
- 7 fail-closed known baseline tests (test_known_baseline.py): two original REDs remain RED, wrong source SHA, altered original blob, added or missing errors, source fixes requiring review and baseline schema changes.
- Both Windows 2025 and Ubuntu 24.04 Actions runners repeat 29 physical tests and inspect the **27 actual original repo asmdefs**, preserving JSON and logs.
- Native Unity EditMode/PlayMode/Player, source compilation and gameplay G1 by B01-41: **NOT_RUN**.

## Receiver and rollback

B01-40 independently verifies exact manifest SHA, scope, original objects and known RED; B01-60/61 independently test source controls and, only if authorized, inspect Unity Editor/compilation implications for MOV005/MOV006. B01-00 controls any CODEX INBOX #79 offer; CODEX_ACK is pending. This package is an optional CI source gate with no asset/save migration. Rollback by leaving Draft unmerged or removing only own Tools/B01/CI41/P17W06 and isolated workflow paths.

Prior CI41-REPLAY-001 v1.1.0 Draft #409 remains frozen, unchanged, and source-design reviewed by B01-61; Unity G1 still NOT_RUN.

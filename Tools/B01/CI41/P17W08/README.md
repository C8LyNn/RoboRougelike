# CI41-TESTIMPACT-001 v1.0.0 — exact Git-diff Unity test impact routing

**B01-41 / PASS 17 WAVE 08.** Executable Python 3 standard-library + Git source-only QA utility. It is **different** from CI41-NATIVE-TRIAGE (#388), repeat execution (#409), .asmdef graph (#431) and UPM lock graph (#447). It plans a targeted *first* test pass from actual Git diff and exact original Unity test assembly metadata, with a fail-closed **FULL_UNITY_SUITE_REQUIRED** on global, unknown, multi-module, test-assembly and missing-test changes. It does **not** execute Unity or grant G1.

## Original repo facts independently examined by producer

Git main source `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`: **9** MOV001–MOV009 modules, **29** C# test files in module `Tests/EditMode` and `Tests/PlayMode`, **33** shared `Assets/Tests/Editor/*.cs` tests. Actual source includes both EditMode and PlayMode assembly definitions per module. All metadata/paths are read directly from exact original Git commit using `git ls-tree` and `git cat-file`; the tool does not trust an uncommitted filesystem inventory. The source reader checks original .asmdef JSON and test assembly names; it does not assert native Unity assembly import succeeds. Previous MOV005/MOV006 source policy exceptions in #431 remain independent blockers, unaffected.

## Functional modes and fail-closed contract

Run inventory on exact immutable original source SHA:

```sh
python3 Tools/B01/CI41/P17W08/test_impact_plan.py --repo . --head-sha YOUR_EXACT_40_HEX_SHA --inventory-only --expect-module-count 9 --out unity-test-inventory.json
```

To make an actionable, deterministic test plan from actual original Git commits, provide **base and descendant head**:

```sh
python3 Tools/B01/CI41/P17W08/test_impact_plan.py --repo . --base-sha BASE_COMMIT_SHA --head-sha HEAD_COMMIT_SHA --out test-impact-plan.json
```

The `changed_paths()` function invokes real `git diff --name-status -z -M` with rename support; requires **40-character lowercase SHAs** and verifies the base commit is an ancestor. The target inventory is taken from exact head commit, including actual committed module test C# files, test assembly names and counts. It handles additions, edits, deletions and renames including their source and destination paths. JSON has `mode`, `changed_path_count`, `modules_affected`, actual `selected_precheck_assemblies` (assembly name, EditMode/PlayMode, evidence paths), `reasons`, actual source inventory counts and SHA provenance.

Routing criteria: a *single* MOV module runtime/test source change with existing runtime .asmdef and EditMode+PlayMode test files produces `TARGETED_PRECHECK_PLUS_FULL_G1_LATER` and **both real assembly names** for an initial receiver precheck. Multiple MOV modules, global `Assets/**`, `Assets/Tests/**`, `Packages/**`, `ProjectSettings/**`, assembly-definition or meta changes, deleted/renamed tests, unknown project root files or missing test inventory **force `FULL_UNITY_SUITE_REQUIRED`**. Only `Tools/**`, `Docs/**` or `.github/**` source modifications yield `SOURCE_ONLY_NO_GAME_ASSETS_CHANGED` and explicitly require their own source CI. They are **not** certified as gameplay-safe. Head with zero changes reports `NO_CHANGE`. Unknown statuses/invalid commits/malformed `.asmdef` fail closed (`exit 2`).

**This plan NEVER waives a full native regression suite** for G1/integration. An initial targeted precheck is helpful to catch local failures sooner but cannot prove cross-assembly/gameplay regression absence. The only real native game acceptance criterion is an independently executed native Unity full-suite/Player result under B01-60/61/40 approval and Codex owner acceptance. `native_unity: NOT_RUN`, `game_g1: NOT_ASSESSED`, `release_gate: INDEPENDENT_FULL_NATIVE_SUITE_ALWAYS_REQUIRED_FOR_G1` are emitted for every generated plan.

## Tests and original evidence

`test_test_impact_plan.py` runs **24 physical Git process tests** per platform, using temporary real `git init`, original-looking MOV source/test assembly files, real commits, diffs and renames, and standalone Python CLI executions. Cases: single and multi-module, global scene, central integration tests, package/project settings, assembly and meta, deleted or renamed files, missing PlayMode coverage, new module without tests, unknown gameplay asset, Docs/Tools-only work, unknown root, exact ancestor/SHA checks, standalone command and drift. These are not mocked Git nor a native Unity Editor. GitHub Actions on Windows 2025 and Ubuntu 24.04 runs these checks **plus the real original 9-module / 29 module test C# / 33 central C# inventory**, preserving real JSON and test transcripts. The initial producer CI exposed missing explicit reason for a new testless MOV source, and a versioned source correction was made before the final frozen package.

## Scope, receiver and rollback

All changes are confined to `Tools/B01/CI41/P17W08/{test_impact_plan.py,test_test_impact_plan.py,README.md,manifest.json}` and `.github/workflows/b01-41-p17w08-test-impact.yml`. No `Assets/**`, `Packages/**`, `ProjectSettings/**`, Codex camera/MAPGEN/game code, shared CI02/03, or main/develop changed. No merge.

**B01-40** reviews and accepts original exact five-path scope, Git file/blob hashes, actual Windows/Ubuntu logs, release-gate semantics and rollback. **B01-60/61** independently reproduce the Git fixture tests and test actual original Unity test inventory, then only with authorization run actual Unity-native precheck/full suite. **B01-00** decides optional CodeX inbox handoff. Codex is busy; nothing here implies ACK, installation or G1. Rollback: leave this optional Draft unmerged; if adopted, delete only isolated CI41 source/tool files after HQ authorization. No asset/save migrations.

**Status:** PRODUCER_SOURCE_VERIFIED / INDEPENDENT_G0_PENDING / UNITY_NATIVE_NOT_RUN / CODEX_ACK_PENDING / NOT_INTEGRATED.

# CI41-ASSETIMPACT-001 v1.0.0 — Unity asset impact trace

**Producer:** B01-41 CI/CD & Test Automation. **Independent CI-only package:** source script, test suite, GitHub Actions, original-Codex JSON impact receipt. Does not touch Codex-owned gameplay, build, physics, scenes, prefabs, CHL, PARTSIZE or shared CI-02/03.

## The new function

Given two **exact original Git commits in one ancestor lineage**, [`asset_impact.py`](./asset_impact.py) identifies changed Unity asset GUID roots, reads Git-tracked `Assets/**/*.meta` and serialized Unity YAML in the newer commit, builds an inverse GUID-reference graph and computes each affected prefab/scene's shortest dependency chain.

**Example:** modifying `Assets/Scripts/Fighter.cs` can produce `Fighter.cs → Fighter.prefab → Arena.unity` even when the prefab and scene have no Git diff. Use the resulting `affected_scenes` and `affected_prefabs` to prioritize scene loads, Prefab import and smoke tests in the **authorized Codex Unity environment**. A cycle cannot loop indefinitely. Missing original objects, non-ancestry, ambiguous or deleted assets, and changed assets without local `.meta` return a blocking exit code.

This is **not** a second orphan GUID validator: [CI41-UNITYREF-001, separate frozen Draft PR #345](https://github.com/C8LyNn/RoboRougelike/pull/345) detects dangling references after asset GUID retirement. This package instead calculates the **impact radius of edits to existing assets**, which the prior tool does not compute.

## Reproduce using real Codex Git source

```bash
git fetch origin '+refs/pull/310/head:refs/remotes/origin/ci41-p17b-codex'
python Tools/B01/CI41/P17B/asset_impact.py --repo . \
  --base a2ccd055070a963805cba0779b79f18489f4794f \
  --head 16c2668104952f1b8db304cc7817b897fb48a9b1
```

Exit `0` means **the graph was fully computed** (not that the game passed Unity). Exit `2` means partial/incomplete input, deleted assets, missing local meta, unavailable Git, invalid SHA/ancestry or resource budget exceeded — fail closed. JSON includes the original Git SHA pair, changed roots, `affected_scenes`, `affected_prefabs`, exact `via` chains, inspected YAML count, `native_unity=NOT_RUN`, `game_integrated=false`.

Execute tests without Unity (Python 3 + Git; standard library only):

```bash
python -m unittest discover -s Tools/B01/CI41/P17B -p test_asset_impact.py -v
```

Test cases include direct scene changes; transitive script/material→prefab→scene; cycles; unaffected scenes; deletion, unmapped metadata, folder meta; invalid Git SHA/ancestry; exact CLI exit codes; and a real original immutable Codex source diff. Evidence is in the branch-scoped `.github/workflows/b01-41-p17b-asset-impact.yml` dual-OS run, whose uploaded artifact contains original Python test logs **and the actual original Codex impact JSON**.

## Import / rollback

Codex/CI receiver: **B01-40 source audit → B01-60 independent source verification → B01-00 scheduling → Codex optional adoption**. This source is *OFFERED*, not installed. Codex/HQ must explicitly authorize any global CI or project integration. When used on a PR, exact SHA inputs must come from a trusted CI/GitHub event, not user-chosen convenient SHA values. The optional tool can be copied to a sandbox or CI job; run it read-only before native Unity/Player test selection. Rollback: do not merge the Draft PR, or remove the optional CI tooling if separately adopted. There are no scene, gameplay, GUID or database migrations.

## Known limitations (hard requirements for interpreting results)

- Only Git-tracked `Assets/` Unity `.meta` and common **text serialized YAML** extensions are scanned; dynamic runtime GUID loads, packages, addressables, UXML, binary asset payloads, `fileID`-only subasset links, and semantic C# dependencies are outside the graph.
- A script changed in a runtime-instantiated scene might produce **no static YAML affected scenes**; this is not proof no gameplay regression exists. Retest full gameplay on any relevant C# change according to Codex/HQ policy.
- Deleted assets and missing `.meta` require **separate CI41-UNITYREF-001 or Unity resolver acceptance**; fail-closed exit `2`, never silently ignored.
- There is a 15,000 file and 256 MiB scan budget with 24 MiB per serialized asset; exceeding any budget causes fail-closed, not false green.
- No editor import, native Unity 6000.3.14f1 Play/Edit NUnit, Player build, game G1/G2, independent B01-60 acceptance or merge is included.

**Package state:** original source-only CI `TEST_EXECUTED` upon successful Actions; `GAME_INTEGRATED=NO`, `G1=NOT_RUN`, `G2=HOLD`, `CODEX_ACK_PENDING`.

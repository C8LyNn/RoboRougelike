# CI41-PATHPORT-001 / PASS 17 WAVE 02 — portable Unity asset path gate

**Producer:** B01-41 — CI/CD & Test Automation. **Scope:** only `Tools/B01/CI41/P17W02/**` and `.github/workflows/b01-41-p17w02-asset-path-portability.yml`. No `Assets/**`, Codex gameplay, CHL, PARTSIZE, or shared CI02/03 edits. Python 3 + Git only; Unity 6000.3.14f1/Player **NOT RUN**.

## Working code and why it is nonduplicative

[`pathport.py`](./pathport.py) reads the **actual immutable Git tree** for one exact, fully specified 40-hex commit, without depending on the checked-out Windows filesystem. It checks all Git-tracked Unity `Assets/**` paths, including their implicit directories, for source-portability hazards:

* casefold aliases (`Assets/Parts/Hull.prefab` vs `.../hull.prefab`, or folder `Robots` vs `robots`);
* Unicode NFC/NFD aliases (different Git names resolving to the same normalized name);
* Windows device names, including file extensions and variants (`CON.asset`, `NUL`, `COM¹.prefab`, `LPT9.mat`);
* reserved Windows characters and ASCII control characters;
* trailing spaces/dots in any component, 255 UTF-16-unit component-length violations.

A failing report includes `path,component,kind,detail` for each conflict and returns **exit 2**. A passing report returns **exit 0** with the exact source commit, number of checked file paths and `native_unity=NOT_RUN`. It is **not** an asset importer, an automatic rename tool, a branch protection gate or a native G1 receipt.

Existing [CI41-UNITYREF-001, Draft #345](https://github.com/C8LyNn/RoboRougelike/pull/345) checks retired GUIDs referenced by Unity YAML; existing [CI41-ASSETIMPACT-001, Draft #352](https://github.com/C8LyNn/RoboRougelike/pull/352) derives scene/prefab regression impact after a modified asset. Neither checks Windows/Linux filesystem *path identity*. This is a distinct CI source gate.

## Run directly on a trusted original Codex commit

```bash
git fetch --no-tags origin '+refs/pull/310/head:refs/remotes/origin/ci41-w02-codex'
python Tools/B01/CI41/P17W02/pathport.py --repo . \
  --commit be2c4b6ac63e04eff5b046e50641ec8d1da4837f > codex-paths.json
python -m json.tool codex-paths.json
```

Only an event-bound/trusted source SHA is authoritative for a PR. The command must not accept an arbitrary user-provided "green" branch ref as a required-check exemption.

Unit/integration regression without Unity:

```bash
python -m unittest discover -s Tools/B01/CI41/P17W02 -p test_pathport.py -v
```

The original suite includes **12 cases per OS**; among them one actually creates a Git tree/commit containing the otherwise un-checkoutable-on-Windows `CON.asset`, `Hull.prefab`, `hull.prefab` names using `git hash-object`, `git mktree -z`, `git commit-tree`, then scans the real immutable Git object and verifies the CLI returns nonzero. This is not a mock Windows checkout. Other negatives cover path casing, implicit directory aliases, Unicode normalization, forbidden characters, reserved devices, trailing punctuation, overlong UTF-16 and invalid SHA; positives and read-only scans run against the real original `main@5f06af81...` and `Codex@be2c4b6...` trees.

Branch-only dual-OS Actions [#40759904072](https://github.com/C8LyNn/RoboRougelike/actions/runs/40759904072) runs original Git/Python suites **plus** a source-only JSON scan on each OS; each artifact carries the original Python test trace and original source JSON. These are not Unity-generated logs.

## Handoff/rollback

**B01-40**: verify exact file-scope, Git blob hashes and both CI artifacts; inspect JSON. **B01-60**: independently re-run source-only original Git-object negatives and the real Codex source scan; this is not author QA. **B01-00**: decide any queue position. **Codex**: optional source-only CI check after explicit acceptance, with test checkout of the original commit; retain full native EditMode/PlayMode/Player required checks. No game release, merge or cherry-pick is implied by this Draft PR.

**Rollback:** leave Draft PR unmerged or delete the optional script/workflow from a future authorized adoption. There are no scene, asset, GUID or save migrations.

## Limitations and acceptance semantics

The gate is conservative about Unicode aliases; it can flag names that technically coexist on a particular filesystem. Human review is required before renaming canonical assets; Codex alone owns such edits. It doesn't validate binary asset import, Unity serialization, GUID links, Git LFS pointer contents, packages/assets outside `Assets/`, Windows path length/prefix policy, or runtime behavior. A **PASS_PORTABLE_ASSET_PATHS** verdict means only that this set of tracked paths avoids the modeled path hazards — never Unity G0/G1/G2, successful Windows Player import or independent QA.

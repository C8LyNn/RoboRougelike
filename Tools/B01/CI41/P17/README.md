# CI41-UNITYREF-001 v1.0.0 — Unity serialized GUID retirement gate

**Role:** B01-41 CI/CD & Test Automation. **Purpose:** prevent broken Unity scene/prefab/material/asset references during Codex integration. **Source-only package; Unity 6000.3.14f1 / URP compatibility by text format, NOT native tested.**

## Executable result

`unity_ref_gate.py` reads two immutable 40-character Git commits, asserts ancestor relation, discovers changed or deleted `Assets/**/*.meta` and calculates *retired local GUIDs*. It follows these GUIDs through the complete newer commit's serialized Unity YAML (`.unity`, `.prefab`, `.asset`, `.mat`, `.controller`, `.anim` and related suffixes). A deleted or modified script/material metadata GUID still referenced by an **unchanged** scene raises a blocking error with exact file, line, GUID and former `.meta` path. Moving an asset with its GUID unchanged and deliberately clearing an obsolete reference do not fail.

The current standard `Tools/CI/validate_repository.py` checks missing/duplicate `.meta` and configuration but does **not** cross-reference references inside existing serialized assets. This tool extends that read-only coverage. It does not edit Codex game files, rewrite GUIDs, import assets or bypass native tests.

## Run / expected output

Obtain both original commits from the same Git lineage. Example pinned published Codex pair:

```bash
git fetch origin '+refs/pull/310/head:refs/remotes/origin/ci41-p17-codex'
python Tools/B01/CI41/P17/unity_ref_gate.py \
  --repo . \
  --base 231005c6d0351945a8ee6684f3856af118f0e94f \
  --head 682dc0c5c13615577334375ffb6c27be5cdebf4a
# exit 0 if no known retired LOCAL guid is referenced by Unity YAML
# exit 2 if dangling known local GUID OR unavailable/untrusted Git history
```

JSON fields include `retired_local_guids`, `dangling_references` with file+line, `inspected_unity_yaml_files`, `native_unity=NOT_RUN`, `g1=NOT_RUN`.

Run automated tests (Python 3, Git, no dependencies):

```bash
python -m unittest discover -s Tools/B01/CI41/P17 -p test_unity_ref_gate.py -v
```

Actual GitHub Actions [Windows 2025 + Ubuntu 24.04 run #40727786472](https://github.com/C8LyNn/RoboRougelike/actions/runs/40727786472): **7/7 tests on each OS** on original temporary Git commits + original Codex Unity source. First intermediate run #40727548716 failed due to two B01-41 test setup mistakes (empty commit, unrelated main-vs-Codex ancestry). Both were fixed in the B01-41 test, no game code modified.

## Import / receiver / rollback

**No direct integration authorized.** B01-40 receives the exact pinned package as an independent source-only staging check, B01-60 independently verifies this tool on new source when needed; B01-00 establishes Codex order. Codex may copy the optional script and test under its own authorized CI/tooling scope only after **explicit claim/approval**. For a pre-merge read-only check of an asset-bearing PR, pass the trusted original PR event base/head Git SHAs, not self-supplied arbitrary SHA values. Do not change `.github/workflows/ci-02-editmode.yml`/`ci-03-playmode.yml` or classify skipped Unity as passing. **Rollback:** remove the copied optional CI41 tool/workflow only; there are no data migrations, changes in `Assets/**`, or side effects.

## Limits / known risks

- The gate detects references to GUIDs that **previously existed locally** and have subsequently disappeared from `Assets/*.meta`. It deliberately does not call never-seen GUIDs broken: many are Unity built-in / installed package references. Those require full Unity resolver tests or a separate package-aware audit.
- It scans textual Unity YAML with common serialized suffixes, not arbitrary binary Unity assets, external `Packages/**`, sub-assets identified only by fileID, or runtime-generated references. `inspected_unity_yaml_files=0` can be normal when no known local GUID retired.
- A green source-only result does not imply valid scene rendering, editor import, package compatibility, gameplay functionality, G0 global integration, native G1, or HQ G2. Real Unity/Player still needs Codex/HQ authorized environment.
- The script only reads Git object bytes; it creates no Git commits or gameplay files. The CI workflow is scoped to this QA branch and is **not a global required check**.

## Ownership and handoff

Own-only paths: `Tools/B01/CI41/P17/**`, `.github/workflows/b01-41-p17-unity-retired-guid.yml`. No edits in original Codex, CHL-003 or PARTSIZE producer/operator paths. Immutable source blobs in `manifest.json`, Draft PR targets main for **review only**, NO MERGE.

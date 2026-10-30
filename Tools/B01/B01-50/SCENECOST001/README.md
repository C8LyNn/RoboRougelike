# B01-50 / SCENECOST-001 R1 — real Unity scene/prefab static-cost inventory

Standalone **executable Python 3.10+ asset auditor**, zero external dependencies. Reads the existing, original Unity text-serialized `.unity` and `.prefab` YAML files **without modifying them**, counts GameObjects, MonoBehaviours, MeshRenderers, colliders and other components, groups script GUIDs, detects broken local links and flags structural collision/configuration hazards. Writes deterministic JSON and CSV receipts with the input file SHA256 for Codex/QA.

**Why distinct:** not another frame profiler, synthetic projectile benchmark, GPU timing viewer or GitHub CI replayer. It analyzes the *real authored content* before costly native testing. It does **not** measure FPS, CPU cost, draw calls, GPU time, actual triangles, dynamic spawns, physics, URP draw batches, or runtime memory, and cannot imply a game performance regression. Component count review budgets are **editable triage thresholds, not hard frame-time limits**. No modifications to gameplay, scenes, prefabs, assemblies, saves or Codex branches.

## Execute on actual repo contents

```sh
# from repo root, using full verified game checkout
python Tools/B01/B01-50/SCENECOST001/scenecost.py 'Assets/scene 1.unity' 'Assets/Scenes/SampleScene.unity' --out /tmp/b01-scenecost-actual.json --csv /tmp/b01-scenecost-actual.csv
# Allow only if the output files do not already exist; uses exit 2 on invalid input/overwrite
python -m unittest discover -s Tools/B01/B01-50/SCENECOST001/tests -v
```

For Windows, replace `/tmp` paths with an existing operator-owned directory and use PowerShell quotes. The tool never opens the Unity Editor. `--budgets path.json` accepts an exact JSON object mapping documented component class names to nonnegative integer limits; `--strict` returns exit `4` on advisory or structural issues **after writing evidence**. No `--strict` means return `0` even with advisory flags (but never on invalid source). Inputs cannot be symlinks, Unicode/invalid headers/duplicates are rejected, and reports are not overwritten. `.unity`/`.prefab` YAML is analyzed as serialized documents; variant/nested prefab references and conditional generated components may need a human Unity-native check. MonoBehaviour type identity is shown by GUID, not resolved to assembly without the Unity asset database.

## Verified actual source content, separate from performance metrics

Main's original `Assets/scene 1.unity` Git blob SHA: `a39a5e15482bbe1e114f1c304ceabb2227fcaa04`, observed from GitHub content (October 30). Contains **171 GameObject**, **155 MeshRenderer**, **136 BoxCollider**, **291 MonoBehaviour** YAML objects. Those source-derived counts are **not** frame-time measurements or evidence of inefficient runtime behavior. In the default configurable budget, 150 MeshRenderer /128 BoxCollider /256 MonoBehaviour would be review flags; tuning or optimization is Codex-owned only after native profiling and independent QA.

Main original `Assets/Scenes/SampleScene.unity` Git blob `1c63aa8ded8c325623e162675b13bfcd0b50aa5a`, with 3 GameObject and 3 MonoBehaviour entries. The dedicated optional GitHub Actions workflow `b01-50-scenecost001.yml` executes this exact script on both original source files and uploads JSON/CSV. Its workflow success does NOT prove Unity build/import, native perf or actual FPS.

## QA, import, rollback

**Receiver B01-40:** verify exact `SCENECOST001/**` path scope, own workflow, each source SHA256, JSON/CSV receipt, no destructive write, source provenance. **B01-60:** independently run the same 20 positive/negative Python tests against exact PR SHA and sample real main Unity assets. Logs and artifact hashes must be independently recorded. **Codex:** optional analysis only; no write to gameplay. A budget flag prompts Unity Profiler investigation, not automatic scene optimization or permission to change existing prefab/components. **Rollback:** delete only `Tools/B01/B01-50/SCENECOST001/` and the own dedicated workflow; remove operator-created reports only after preserving QA evidence. No Unity scene, script/meta or game saves to restore.

**Status:** producer-tested original Python code, native Unity and Windows Player **NOT_RUN**, independent QA **PENDING**, Codex ACK **PENDING**. This is standalone useful functionality while the last `PERFRUN-001` package remains separately blocked in independent QA, and does not mutate that frozen source.

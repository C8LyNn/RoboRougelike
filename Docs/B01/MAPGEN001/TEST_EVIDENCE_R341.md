# B01-20 — MAPGEN R3.4.1 / W12 urgent native CS1061 repair
Dispatch: [Global HQ PR #235 comment 6400298995](https://github.com/C8LyNn/RoboRougelike/pull/235#issuecomment-6400298995). **Producer source only; G2 HOLD.**

## Native RED (Codex external, exact R3.4 prior source)
- Frozen R3.4 original [Draft PR #235](https://github.com/C8LyNn/RoboRougelike/pull/235) SHA `0024731e1de0c0814968bd91d860a6ac9bb45385`. **Do not rewrite** this commit.
- Native Codex [QA PR #236](https://github.com/C8LyNn/RoboRougelike/pull/236) `5d410e11b5d4f330a52096fdab63e4a26a8a5610` (actual separate compile snapshot `3e157af2ed6cd7c20145303fcf7dda0d1e52d0ec`), Unity **6000.3.14f1**, compile **FAIL process exit 1, CS1061** at `Assets/B01Packages/MAPGEN001/Tests/PlayMode/MapGenPlayModeTests.cs(37,56)`. The source calls `renderer.sharedMaterial.shader.GetTag("RenderPipeline", false)`: `Shader` has no `GetTag`.
- Native EditMode/PlayMode, twelve CUBEs+flat ground test, bare Windows Player shader/rendering, actual R3/R6 **ALL NOT_RUN because compile blocked**. No NUnit XML is produced. Do not count R3.4 existing 1015 headless C# surrogate as native Unity compilation.

## ROOT CAUSE / minimal FIX
Unity's documented `Material.GetTag(string,bool)` queries the active shader's SubShader tags. R3.4 accidentally called this method on `Shader` instead of `Material`. **Exactly one-line source fix**, preserving test and semantic contract:
```diff
- renderer.sharedMaterial.shader.GetTag("RenderPipeline", false),
+ renderer.sharedMaterial.GetTag("RenderPipeline", false),
```
The `Assert.AreEqual("UniversalPipeline", ...)` expected value, no-error shader assertions, and all existing 6 PlayMode + 13 cube/flat UnityTest definitions remain intact. No planner/runtime/shader/PhysicsMaterial, other package, Codex worktree or game source changes.

## Unity API static audit (not native compile)
Changed/new R3.4 API sites audited against Unity 6 documentation and source contracts:
- `Material.GetTag("RenderPipeline", false)` is supported; `Shader.GetTag` is not.
- `Resources.Load<Shader>(...)` / `Shader.isSupported` are UnityEngine Resources/shader members.
- `PhysicsMaterial`, `PhysicsMaterialCombine.Minimum`, `Collider.sharedMaterial`, `Physics.Raycast`, `Rigidbody.AddForce`, `Rigidbody.AddTorque` are Unity 6 physics API members.
- Regression script `Tools/B01/MAPGEN001/check_w12_contract.py` keeps a negative in-memory copy of the old incorrect call, rejects it, scans all above APIs, verifies six original PlayMode [UnityTest] names, twelve separate CUBEs and flat controller. This is a **static source contract only**. It cannot detect all C# compiler, Unity package or GPU issues.
- Unity docs: https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/material/gettag, https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/physicsmaterial, https://docs.unity.com/en-us/engine/6000.7/manual/assets-and-media/assets-managing-runtime/assets-resources-system/load

## Executable evidence and gates
New producer SHA, final per-file manifest and deterministic ZIP SHA256 should be taken from this R3.4.1 branch's final **successful GitHub Actions run**, not old R3.4 receipt. `Tools/B01/MAPGEN001/package_gate.py --verify-zip` verifies real checkout bytes. CI compiles and exercises exact source planner/validator under explicitly *substituted* Unity math structures; it is NOT Unity/PhysX. New R3.4.1 native Unity compilation, Edit/Play, physical CUBEs and bare Windows Player are **NOT_RUN by producer**, awaiting Codex on exact frozen SHA.
- B01-60 independent G1 pending; B01-40 target G0/sha/retention pending.
- **No merge, no host import, G2 HOLD, CODEX delivery = QA ONLY**.

## Mandatory Codex native retest
In fresh disposable Unity6000.3.14f1 QA worktree, import **FULL stacked #99+#103+#211+#225+#235+#237** at R3.4.1 exact frozen SHA. Verify GUID/.meta/Resources and byte-SHA manifest. First native C# compile with raw Editor.log + exit code, STOP and report any new errors. Then EditMode and PlayMode NUnit XML, twelve individually reported CUBEs+flat control, bare Windows Player (no shader override) render both Arena/Course in 720p+1080p and 3x regen/clear, and real PR125/OBJ210 R6 physical drives (4 initial ->12 matrix subject to QA rules). Preserve original FAIL evidence. Send per-step source/target SHA, XML, logs, process and screenshots back to B01-20/#93 and B01-00. Do not integrate or merge.

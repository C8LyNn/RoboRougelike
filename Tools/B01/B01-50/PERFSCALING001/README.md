# PERFSCALING-001 R1 — B01-50 / Unity Physics3D proxy scaling

**Owner:** B01-50 BUILD PERFORMANCE & PROFILING. **Scope:** own isolated `Tools/B01/B01-50/PERFSCALING001/**`; code integration/active Codex worktree unchanged. **Status:** real C# source and receipt validator published, native Unity compile/Physics performance **NOT_RUN** by B01-50. It is a performance **test instrument**, not a completed optimization/game FPS finding.

## Actual workloads

Eight **renderless proxy** cases × **three repetitions** each; **30** warmup + **200** measured calls to `PhysicsScene.Simulate(0.02f)` per repetition. Robot: one rigidbody with **24,72,144** attached box colliders. Projectile proxy: **0,32,128** independent rigidbodies with continuous sphere colliders. Enemy physics proxy: **0,8,32** rigidbodies with six box colliders each. Baseline/isolated variations and a combined **144+128+32** case. Exactly **4800** recorded simulation steps and **720** warmup steps per native execution. Per-step wall-clock duration CSV, p50/p95/p99, mean, managed allocations and GC0 deltas.

**No real R3 mounted chassis, R4 shooting, P12 enemy AI, VFX, rendering, native Player FPS or actual game bottleneck has been measured.** This benchmark estimates only the scaling pressure of isolated PhysX primitives. Do not infer that a 144-collider proxy passes R3 build geometry or that a projectile proxy executes gun code.

## Codex operator: execute only in a legitimately licensed DISPOSABLE Unity 6000.3.14f1 project

1. Check source state and permissions; the latest public Codex source observed when authored: `a2ccd055070a963805cba0779b79f18489f4794f`, **not automatically the newest integrated source**. Record the **actual tested** game SHA before running.
2. Copy `Editor/B0150PhysicsScalingBench.cs` into the disposable clone under `Assets/B0150PerfSandbox/Editor/B0150PhysicsScalingBench.cs`. No automatic import from `Tools/**`, no edits to canonical `Assets/R1/R3/R4/R6`, CHL, PARTSIZE, default game scene or Codex active worktree.
3. The fixture uses `EditorSceneManager.NewPreviewScene()` and **fails before measuring** unless `scene.GetPhysicsScene()` is both valid **and different from** `Physics.defaultPhysicsScene`. It closes each preview scene. If Unity 6000.3.14f1 cannot provide a private PhysicsScene in Edit mode, record original `Editor.log` and status **FAILED / NO MEASUREMENT**, never simulate global game physics.
4. Execute (PowerShell; choose a path that does **not** exist; use actual SHA for both game and this tool's GitHub HEAD):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe' -batchmode -nographics -quit `
  -projectPath 'C:\DISPOSABLE_RoboRougelike' `
  -executeMethod RoboRougelike.B0150.PerformanceQA.B0150PhysicsScalingBench.RunBatch `
  -b0150-output 'C:\B0150_EVIDENCE\RUN001' `
  -b0150-source-sha '<EXACT_GAME_SHA40>' `
  -b0150-fixture-sha '<EXACT_TOOL_COMMIT_SHA40>' `
  -logFile 'C:\B0150_EVIDENCE\Editor.log'
```

5. **Original evidence:** exact source/game SHA, Unity and Windows host CPU/GPU/driver, legal activation status (no secrets), process exit code, `Editor.log`, `RUN001/receipt.json`, `summary.csv`, `simulations.csv`, original SHA256 all files. The fixture refuses an existing output directory. `FAILED.txt` means exception: **no native PASS**.
6. Check raw receipts using the included receiver, and keep original CSV unaltered:

```sh
python validate_receipt.py --folder RUN001 --source-sha EXACT_GAME_SHA40 \
  --fixture-sha EXACT_TOOL_COMMIT_SHA40 --out RUN001_validation.json
python -m unittest discover -s tests -v
```

The receiver checks 8 ×3 ×200 rows, unique keys, finite times, exact expected rigidbody/collider counts, recomputes p50/p95/p99, provenance SHA claims, null/unmeasured real-game fields. Its return **`STRUCTURAL_REVIEW_REQUIRED_NOT_NATIVE_ACCEPTANCE`** never means that Unity actually ran or the original Editor.log was inspected. Unit fixtures are *synthetic numbers*, not measured FPS.

## Native acceptance, known limitations and next owner

- **Native PASS (only after actual execution):** authentic Unity 6000.3.14f1 Editor compile and opt-in execution exit 0, private PhysicsScene guard, all 4800 finite records and 24 summaries, accurate counts/statistics, no gameplay scene writes, original logs/host/hash provenance independently checked.
- **Native FAIL:** any compile error, missing private physics scene, leaked scene, invalid count, CSV mismatch, missing data, exception or overwriting prior output. Preserve original RED.
- **Finding threshold:** only compare repeatable same-host/same-version sample sets, with p95/p99 distributions; separate rendering/gameplay costs are outside the proxy's scope. Real R3/R4/P12 frame/CPU/render/GPU/GC work remains the existing [CODEX CV-12](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6463896564).
- **Dependencies:** UnityEngine Physics3D, EditorSceneManager preview scenes, C# Editor compile in a disposable copy. In local B01-50 Linux **Unity/.NET runtime unavailable**, therefore the Unity fixture is **UNCOMPILED/NOT_RUN** and no real PhysX time is claimed. An independently authorized B01 G1 receiver remains separate from Codex diagnostics.
- **Rollback:** discard the disposable Unity checkout or remove only `Assets/B0150PerfSandbox/` and generated .meta there; remove evidence only by deliberate operator decision, never by the script. No main/develop merge, G2, ACK or gameplay code package is implied.

**Actual earlier local QA:** B01-50 executed 12/12 Python original portable schema-negative tests on an equivalent locally authored validator, with complete synthetic fixture ZIP included in conversation. **The GitHub-published rewritten receiver/tests have not yet been independently executed on this exact blob**, so don't borrow old 12/12 to claim source-verified green for this Git SHA.

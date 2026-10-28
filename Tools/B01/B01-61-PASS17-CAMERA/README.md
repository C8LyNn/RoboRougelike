# B01-61 — PASS17 CAMERA-STATE R1 (QA-only)

This is a **NEW** execution package independent of PASS17 `INPUT-LATCH` ([Draft #332](https://github.com/C8LyNn/RoboRougelike/pull/332)), the previously authored but unrun UI camera transition NUnit tests in #315, and B01-13 screenshot observation tools.

## Real implementation

[`run_original_camera_contract.py`](run_original_camera_contract.py) pins actual Codex #310 game source `be2c4b6ac63e04eff5b046e50641ec8d1da4837f`, `Assets/Pack/P12_PipelineSafeCamera.cs`, original Git blob `8519938f5734487d0350634cb6d81679a6434d62`. Extracts **without changing their contents** six original C# methods: `EnsureRig`, `SetGameplayInputEnabled`, `Configure`, `Update`, `LateUpdate`, `ApplyCursor`. Copies them into temporary .NET8 projects with minimal simulated Unity (including a Unity-compatible mathematical SmoothDamp approximation) and stubbed Camera001Rig, both original C# branches `ENABLE_INPUT_SYSTEM` on/off. No game source or files under `Assets/**` are modified.

Actual compiled C# operations include scroll zoom min/max 3..45m, 160-frame smooth zoom in/out with variable delta, direct orbit pitch clamp -5..58, pause/disabled motion blocking, resume reset exactly once, missing-pivot step block, target root identity preservation, rig frame step exactly once, and **2,400 deterministic frames of original camera state** for each input branch. An intentionally broken in-memory camera zoom clamp is compiled separately: it must be rejected by the same real C# test, **not** accepted as a successful gameplay run.

This is not real Unity rendering or native PhysX/aim performance. Only the original controller methods are genuine and unchanged; Unity dependencies are intentionally disposable shims. The intermittent visual camera zoom issue from #275 cannot be marked fixed from these results.

## Reproduce

Requires Python 3.11+, Git and .NET8 SDK. From a Git checkout of this QA branch with fetched producer commit:

```bash
git fetch --no-tags origin be2c4b6ac63e04eff5b046e50641ec8d1da4837f
python Tools/B01/B01-61-PASS17-CAMERA/run_original_camera_contract.py --repo .
```

Exit `0` only when both positive C# source variants pass and both negative mutations reject. Exit `2` for source drift, original method compile/runtime failure, false negative acceptance or missing dependency. Each C# run creates then removes a temporary isolated project. The workflow [`.github/workflows/b01-61-pass17-original-camera.yml`](../../../.github/workflows/b01-61-pass17-original-camera.yml) runs both Windows and Ubuntu and uploads unmodified command stdout/exit receipts as artifacts.

## Codex reception, limits and rollback

**No direct game import is required.** For optional adoption of this check, copy only the package into a disposable Codex checkout and run the Python command; **do not** copy extracted stubs or mutate the original P12 camera class in `Assets/**`. Keep this frozen input version if the Codex head has moved; a new source revision requires new pins and actual rerun.

For genuinely playable-camera verification, Codex / independent B01-60 must separately execute Unity 6000.3.14f1 in an authorized licensed environment, verify real camera objects, line-of-sight, framewise FPS and 720p/1080p GUI, record original NUnit XML, Editor.log, original game/test SHA, Player process exit and screenshots. **NATIVE_UNITY=NOT_RUN; REAL_CAMERA_RENDERING=NOT_RUN; G1/G2=HOLD.** No automatic merge/release.

Rollback: delete `Tools/B01/B01-61-PASS17-CAMERA/**` and `.github/workflows/b01-61-pass17-original-camera.yml`. No canonical game edits to reverse. [Manifest](manifest.json) pins all dependencies and known limitations. Proposed receivers: B01-40 (scope/hash), B01-60 (independent QA), B01-00 (routing to existing CODEX INBOX #79).

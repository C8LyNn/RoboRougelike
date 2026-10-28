# B01-61 / PASS17 WAVE02 — WHEELWAKE-RECEIVER R1

**Independent QA receiver for the new B01-32 producer [Draft #366](https://github.com/C8LyNn/RoboRougelike/pull/366), NOT a new competing VFX emitter.** Owner B01-61; Codex retains integration ownership. B01-32 original source is never modified. No assets or Canonical Unity gameplay changes by this receiver.

## Actual executable QA

[`run_original_emitter_qa.py`](run_original_emitter_qa.py) insists on pinned producer commit `005e177a6742634add0e3e434ca34647d590f2fd` and **exact** original Git blobs:

- `Assets/B0132P17WheelWake/Runtime/WheelWakeProfile.cs`: `45949c3c550f048ba8bccfe1071a29edf45d2807`
- `Assets/B0132P17WheelWake/Runtime/B0132WheelWakeEmitter.cs`: `debb9f0fb7803102cfdaa78babb5de0e9a47fba8`

It writes both **unchanged** original source files into a fresh temporary .NET8 C# console application with a separate, intentionally limited UnityEngine API surrogate. It compiles and invokes the actual emitter, including **both** particle systems, from `Awake`, through 3 positive display modes, OFF/contact loss/NaN and invalid input, 600 seeded transitions, `OnDisable`, reactivation and `OnDestroy`. It asserts exact two-wheel rate splitting, 128 particles cap, local wheel placement and rear orientation, real original emitted parameters, instant clearing of stale surrogate particles and owned material/renderer behavior.

Two **disposable synthetic mutated variants** are then independently compiled against the **same tests** and MUST be behaviourally rejected with source test error `WHEELWAKE_ORACLE_FAIL:`: (1) producer's `StopEmittingAndClear` in `ClearAll` changed to `StopEmitting`, leaving stale particles (2) producer's half rate changed to full per-wheel rate, doubling visual output. These mutations exist only in temp source files and **never** alter producer Git or game.

**Status distinction:** This verifies integration of actual producer C# policy and emitter under mocked `UnityEngine.ParticleSystem` lifecycle semantics. It **does not** execute Unity Editor or native ParticleSystem, actual URP shader, rendering, human visual quality, collision or true vehicle wheel physics. Actual B01-32 3 authored Unity PlayMode tests remain **NOT_RUN**. No G1/G2 or game integration inference.

## Reproduce / dependencies

Required: `git`, `python3.11+`, `dotnet SDK 8`. No third-party Python or NuGet packages, no Unity installation needed.

```bash
git fetch --no-tags origin 005e177a6742634add0e3e434ca34647d590f2fd
python Tools/B01/B01-61-P17-W02-WheelWake/run_original_emitter_qa.py --repo .
```

Exit **0** only if actual original C# behavioural positive passes and **both** intentionally malformed C# negatives are rejected with the expected original oracle marker. Any Git source drift, C# compile error, unexpected failure, unexpected negative success returns **2**. All temp C# projects and mutated copies are removed after execution. Dedicated cross-platform workflow `.github/workflows/b01-61-p17-wave02-wheelwake-receiver.yml` checks manifest SHA and both original Git blobs before test, uploads genuine per-OS log artifacts.

## Codex / B01-40 / B01-60 handoff and rollback

**B01-40:** verify only `Tools/B01/B01-61-P17-W02-WheelWake/**` and owned workflow were changed, exact original producer SHA, source manifest, CI logs and that the positive source is unchanged. **B01-60:** independently reproduce genuine Windows/Linux source-only receiver, *then separately* request authorized licensed Unity 6000.3.14f1 native run: import the original producer [#366](https://github.com/C8LyNn/RoboRougelike/pull/366) in disposable Unity workspace, execute its 3 original PlayMode tests against the exact producer SHA, verify left/right live particles, URP color/shader visibility at 720p and 1080p, VFX OFF and disable clear, and record original NUnit XML, Unity log, Player/rendered visual evidence and process exit. Native G1 **NOT_RUN** until evidence exists.

**Codex integration** only after B01-00/B01-40/B01-60 handoff; no ACK/merge presumed. Source receiver is optional QA tool and must **not** be imported into actual game Unity `Assets/**`. **Rollback** remove only owned QA folder and single workflow from scratch copy/branch. Never overwrite the producer's frozen source or producer's GUIDs. Exact pins and blockers in [manifest](manifest.json).

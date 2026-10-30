# B01-61 / PASS17 WAVE07 — independent original B01-23 debris Unity adapter source QA

**PACKAGE_ID:** `B01-61-P17-W07-DEBRIS-ADAPTER-QA-001`, **VERSION:** `v1.0.0`.

**Actual original producer under review:** [B01-23 Draft #421](https://github.com/C8LyNn/RoboRougelike/pull/421), full original immutable producer SOURCE SHA `cb2b3a76e84266e2730fffc4ab048c1039f0652f`. The producer compiled and tested its original **pure impulse math core** with 33/33 unit cases and 12 archive tests, but **did NOT compile or execute its original `DebrisRepulsionEmitter.cs` Unity adapter** in that unit runner. This independent QA has a different scope: actual end-to-end *managed call boundary* from the ORIGINAL Unity adapter to the original C# impulse ledger to the `Rigidbody.AddForce` API.

## Deliverable — executable source testing, not another gameplay prototype

- `run_original_adapter.py`: fetches/validates **eight exact original Git blobs** (both real original C# files + asmdef + five Unity `.meta` files) at #421 immutable SHA, and checks original five different Unity GUIDs and original assembly contract `autoReferenced=false`.
- `Program.cs`: 47 original independent positive/negative calls into the **original** `DebrisRepulsionEmitter` type (not its reimplementation), testing real call paths for zero/invalid/kinematic body, owner centre and COM, signed impulse axis, desired/cap fields, mass/velocity checks, duplicate stable IDs, rearm/new run, zero impulse, and ForceMode. The real full untouched original `DebrisImpulseCore.cs` and `DebrisRepulsionEmitter.cs` are compiled alongside it under C#9 using `.NET 8`.
- `UnityEngineBoundaryStub.cs`: **QA-only minimal simulated host API**, with a bookkeeping Rigidbody exposing the Unity 6 properties/method the original adapter uses. It is NOT UnityEngine, actual physics, Rigidbody runtime, source-matched binary API, collision, or native compile. Only assures that calls and values reach the expected stub surface.
- Five deliberately corrupted **independently recompiled original-source mutants** are rejected by the 47 tests on each OS: removing the kinematic guard, one-shot reservation check, incorrect ForceMode, incorrectly calling AddForce for zero impulse, and ignoring original Rigidbody.velocity. A mutant is validly rejected **only when original tests actually compile and run, print a failing assertion and exit nonzero**; compile failure or runner silence is NOT treated as a negative-test PASS.
- Actual Windows + Ubuntu Actions jobs archive distinct raw test logs and independently computed case receipts. The genuine original source passes only when **47/47 tests** execute and return exit 0.

This runs in a disposable external OS temp directory. It DOES NOT modify B01-23 original #421 `Assets/**`, Codex original source, PARTSIZE, CHL, global Unity CI, G1/PhysX tests, or main/develop. Does not download arbitrary third-party code; original Git blobs are strictly SHA pinned.

## Reproduce

Requires locally installed Git, Python 3.11+ and .NET 8 SDK. From a checkout with our QA-only files:

```sh
git fetch --no-tags origin cb2b3a76e84266e2730fffc4ab048c1039f0652f
python Tools/B01/B01-61-P17-W07/run_original_adapter.py --repo . --output b0161-w07-adapter-receipt.json
```

The command writes only the JSON receipt to the specified caller-owned output and temporary original C# build files into a disposable external temp directory; it refuses to overwrite an existing original receipt. The original C# game code is copied **byte-exact from `git show immutableCommit:path`** into a new isolated process project. No original game Unity `Assets/**` or adapter is modified. Rollback: close/unmerge this QA Draft, remove only `Tools/B01/B01-61-P17-W07/**` and `.github/workflows/b01-61-p17-w07-original-debris-emitter.yml`, delete disposable receiver receipt and temp files. Producer #421 stays frozen.

## Receiver and acceptance boundary

**B01-40:** independently verify #421 source/physical 34,472 B ZIP, original SHA, 5 new GUIDs and asmdef opt-in reference, original Codex game source collision / dependency chain, and this verified manager boundary receipt. **B01-60:** native licensed Unity 6000.3.14f1 with real Rigidbody/PhysX, actual detached-part owner, collider/force and save/reattach scenarios (especially kinematic bodies, simultaneous detach, momentum/collision, the true AddForce error path). **B01-00:** alone can queue any accepted package to Codex #79 after gates. Producer B01-23 retains sole source/adapter ownership; if QA finds defect, report it; DO NOT silently patch source.

**Truth limits:** compiled untouched real original C#9 adapter and original C# core **with QA fake Unity API**, **NOT** a native Unity assembly compile or real PhysX simulation; `UNITY_G1=NOT_RUN`, `PLAYER=NOT_RUN`, `CODEX_ACK=NOT_OBSERVED`, `GAME_INTEGRATION=NO`, `MERGE=NO`. The original asmdef `autoReferenced=false` requires explicit Unity receiver assembly dependency/wiring; no host adapter exists in this PR. The known #431 MOV005/006 Editor-asmdef strict graph RED is separate and must remain RED; this receiver does not override it.

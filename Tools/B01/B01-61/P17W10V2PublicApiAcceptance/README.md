# B01-61 — PASS 17 / WAVE 10 — Secure reattach V2 original-source public-API acceptance

**Purpose:** validate the *new separately owned V2 replacement* for B01-23, not repeat B01-62 #476 W07 route inspection or B01-61 #472's original W07 RED. Exact new candidate parent at time of QA: `59479716140b0e2682f5a979e210e0f2709d4a31` on `b01-23/p17w10-secure-reattach-v2`. Three original C# source Git blobs are pinned in `verify_source.py`. Zero producer/Codex/shared code edits.

## Independent executable tests

`OriginalSourceQA.csproj` links the ORIGINAL unchanged B01-23 W10 `SecureDockPolicy.cs` directly into C#9/.NET8, with our separate runner (not a Python imitation of engine logic). It executes nine independent regression groups against the original `GuardedDockCommand` and `SecureDockPolicy`:
- All three **public** entrypoints `Reattach`, `TrySafeReattach`, `ReattachFromGui` must all call the same inspector before any commit.
- Five explicitly denied verdicts on every route must never invoke the mutating callback.
- Bad index values cannot invoke inspector or commit.
- Reentrant calls from inspector and commit callbacks cannot bypass the guard or double-commit.
- Injected inspector exception cannot commit and must release recursion guard for a later attempt.
- Retry must repeat inspection; never use a prior approval.
- Original all-layer, saturation, out-of-reach, nonfinite penetration and unsupported-collider policy explicitly fail-closed.
- Null inspector/committer are rejected.

Independent `verify_source.py` pins exact original Git objects, verifies ONLY six new QA files and that the actual Unity host routes all public methods and GUI via the guarded command, keeps physical mutation private, and checks unfiltered all-layer Physics + ComputePenetration. Six negative **source-contract mutations** must be detected. CI runs actual .NET source on Windows2025+Ubuntu24.04 and archives logs.

## Strict limitation

This is *SOURCE G0 API-contract QA*, NOT real Unity6 Editor/PhysX/GUI execution. The original W07 `DebrisRigPlayground.Reattach(int)` still exists in the old separate component; **do not interpret V2 safety as magically securing an existing W07 host**. A Codex-controlled scene/receiver must explicitly select V2 instead of W07; no dual legacy controls, no install in canonical game yet. Native `SecureReattachV2PlayModeTests.cs` is authored by B01-23 but NOT_RUN here; requires licensed Unity 6000.3.14f1 and independently executed real collider/ID state tests.

**Acceptance:** B01-23 owns immutable V2 production (any correction only via separately versioned branch); B01-40 formal source/asmdef/GUID/ZIP G0 intake; B01-60/61 independent licensed Unity native G1; B01-00 controls #79 and Codex ACK. The receiver must check current exact producer SHA before treating this pinned QA as current. Prior frozen W07 #439, W08 #455, W09 #471 and QA #472/#476 remain as historical risk evidence.

**Rollback:** close this Draft and drop only `qa/b01-61-p17w10-v2-public-api-acceptance` (six own QA paths). No source files or existing game content are changed.

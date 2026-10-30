# B01-23 / PASS17 WAVE06 — DEBRIS-REPULSE-001 v1.0.0

**Real mechanic:** optional one-time outward impulse for a genuinely detached robot part. Original Unity C#9 source, not VFX or a substitute for canonical vehicle physics. Independent from Codex R3/R6, garage, combat and save.

- Inputs: positive stable part ID; owner and detached world centres; current linear velocity; Rigidbody mass; desired outward speed; max added speed.
- Outward speed added = max(0, desired - dot(currentVelocity, radialDirection)), capped by max added speed. ForceMode.Impulse equals mass (kg) * added speed (m/s), in N*s.
- Coincident centres use deterministic, eight-way fallback in XZ keyed by stable part ID.
- Invalid/non-finite inputs are rejected without consuming the part ID. Valid detach event consumes the ID even when zero impulse is needed. Repeated events never cause stacked impulses.
- Only explicit RearmAfterReattach or ClearForNewRun resets the ledger. No auto event from OnCollision/OnDisable.
- Defaults: 2.5m/s outward target, 4m/s max added. Guard rails on mass, position, velocity and finite inputs.

## Executable source tests

dotnet run --project Tools/B01/B01-23/P17W06DebrisRepulse001/DebrisRepulseTests.csproj --configuration Release

Links the actual package DebrisImpulseCore.cs (no translated mock); enforces C#9; 33 positive, negative and determinism cases. See GitHub Actions Windows/Ubuntu results for actual execution. Unity Editor/PlayMode/PhysX is **NOT_RUN**, even when standalone source tests pass.

## Install / rollback

Only on an isolated, Codex-authorized Unity 6000.3.14f1 checkout: verify that the entire Assets/B0123DebrisRepulse001 folder and its five GUIDs are unoccupied. Copy that folder plus its .meta. Instantiate DebrisRepulsionEmitter as an opt-in runtime component. On a real detached Rigidbody (not each collision), call TryApplyDetachImpulse(stablePartId, body, ownerCenter). Codex keeps responsibility for generating stable ID, detach timing, actual mass and whether a body is kinematic. Explicitly invoke RearmAfterReattach on real reattachment, ClearForNewRun on a new run. **Rollback:** remove only the optional emitter invocation and the isolated added Assets/B0123DebrisRepulse001 subtree in the disposable target branch. Never touch canonical R3/R6 or existing PARTSIZE/Bracing source.

## Status and evidence rules

Producer source CI may prove G0 candidate only. **Native Unity compile/PlayMode, PhysX, colliders, 720p/1080p, full game integration = NOT_RUN** without authorized licensed Unity. Independent B01-40 path/meta/GUID review and B01-60 native QA are pending; **G2 HOLD, CODEX ACK NO, NOT_INTEGRATED**, no merge. B01-00 owns handoff scheduling. Receiver: B01-40 then B01-60. Owner B01-23. The package contains genuine gameplay C# and tests; it does not claim to be already wired into the game.

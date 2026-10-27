# B01 / CHL-003 R1 — Complete obstacle-course candidate, QA pending

**Exclusive producer** B01-20 Unity Development, [issue #91](https://github.com/C8LyNn/RoboRougelike/issues/91). Authorized top-level governance [#276](https://github.com/C8LyNn/RoboRougelike/issues/276). Single approved CHL source-production stream, separate from PARTSIZE-001 B01-23. No game/Codex edits or main merge.

## Actual isolated Unity demo
Open `Assets/RoboRougelikeChallenges/CHL003/Demo/CHL003_Demo.unity` in **Unity 6000.3.14f1** and Play. One serialized bootstrap creates primitives and **real MeshRenderer, Collider, trigger/physics geometry**, non-kinematic Light or Heavy Rigidbody with wheel/hull colliders, four ordered checkpoints and finish. Terrain includes solid low barrier, tilted solid ramp, physically narrow elevated bridge and out-of-bounds fall failure. This is a fully local scene/physics/GUI code path; it is **not** the canon R3 vehicle/garage/world integration. The camera follows the demo rig only.

W/S forward/back, A/D steering with the old Input Manager if enabled; in a new-only Input System build use GUI HOLD DRIVE/REVERSE/LEFT/RIGHT buttons. TAB switches Light/Heavy, R retries, P pauses; GUI buttons provide all actions. A Rigidbody force/torque motor drives the vehicle; no manual transform movement advances checkpoints. Light 270kg, 1.45×0.52×2.3m hull; Heavy 1150kg, 2.5×0.65×3.4m hull (additional sphere collider wheels). Parameters are **demo fixtures**, not approved PARTSIZE dimensions or canonical R3 physics. Exact PlayMode/manual obstacle crossing and physical rig completion in Unity remain **NOT_RUN**, so scene/vehicle gameplay is candidate, NOT CODEX_READY.

## Narrow API
`CHL003CourseCore`: `Configure(count=3..5, actorToken, optionalTimeLimit=0)`, `Begin()`, `EnterCheckpoint(actorToken,index)`, `EnterFinish(actorToken)`, `Fail(actorToken,reason)`, `SetPaused(bool)`, `AdvanceTime(dt)`, `Retry()`, `Cancel()`, events CheckpointReached/Completed/Failed. `CHL003CourseController`: bind exact non-kinematic Rigidbody and ordered physical triggers; ReceiveGate validates `collider.attachedRigidbody` against that actor, ignoring foreign/multi-collider duplicates; fail below world Y threshold. No reward/inventory/gameplay host mutation.

## Status and hard limits
G0 SOURCE: pending final receipt/independent B01-40; actual headless C# tests and importer static suite through GitHub CI (see exact run and evidence from PR). 10 authored EditMode and 6 authored PlayMode **NATIVE_NOT_RUN**; no Unity editor activated in Actions. G1 independent B01-60 **PENDING**, G2 **HOLD**, CODEX_READY **NO**, SENT **NO**, CODEX_ACK **NO**. The source version remains **IN_PROGRESS**, not OFFERED/FROZEN until gate decision.

Required before readiness: native project compile and Edit10/Play6 XML/raw exits; manual actual Light/Heavy successful traversals (at least three runs each), native fall/retry/pause tests and screenshots/video; independent B01-60 reproduction; B01-40 SHA/GUID/ZIP/target & installation rollback gate. Do not conflate working source or simulated tests with completed physics QA.

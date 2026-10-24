# R1 limitations and risk gates

- Real Unity 6000.3.14f1 compilation and test execution NOT RUN at initial source submission; no real vehicle traversal or screenshot proof, so this is **not** READY_FOR_GPT_OSS.
- Map plan has deterministic variable cover/slalom/ramp positions and collision primitive construction, but does **not** provide a true continuous random road graph or terrain heightfields. Narrow scope is intentional; later recipes depend on empirical driving QA.
- Map vehicle envelope is a simple rectangle and turn radius scalar. Geometric centre corridor clearance is not a full PhysX traversal certification for a long robot with overhang or spinning weapons.
- CombatArena has **no AI, no win logic**, only metadata for three potential enemy spawn points. ObstacleCourse emits waypoint descriptors **only**, not CHL003 scoring.
- Demo uses a simple Rigidbody cube with on-screen held buttons; it is not the physical wheel-driven robot from W8. After import, GPT OSS must test the actual vehicle separately.
- Source branch is based on historic `develop` SHA because no Codex/GPT OSS unpublished worktree is visible; no host integration authorized.
- MAP-R6 #3 remains independent and not tested with this source. Blender/models are irrelevant for graybox R1.

## R2 validation limits
- Route validation is conservative **axis-aligned XZ rectangular envelope** (vehicle forward along course Z). It includes all-corner floor support, blocked-body dilation, a 1m 4-connected BFS and a 0.5m sampled certified centreline with waypoint circumradius checks. It is not a continuous swept-polygon turn simulation and cannot establish real suspension or wheel grip.
- Ramps are treated as solid blockers for the safe flat bypass, and their nominal pitch is constrained by `MaxSlopeDegrees`; an optional ramp is not certified physically climbable without Unity driving evidence.
- The new 12-case physics traversal UnityTest uses a *simple Rigidbody cube with forces and torque*, not the final W8 vehicle; the integrated vehicle/controller still needs separate GPT OSS driving QA.
- Source/test additions have not yet been executed in licensed Unity; `UNITY_NOT_RUN`, `B01_60_QA_PENDING`, `G2_HOLD`.

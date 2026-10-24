# R1 limitations and risk gates

- Real Unity 6000.3.14f1 compilation and test execution NOT RUN at initial source submission; no real vehicle traversal or screenshot proof, so this is **not** READY_FOR_GPT_OSS.
- Map plan has deterministic variable cover/slalom/ramp positions and collision primitive construction, but does **not** provide a true continuous random road graph or terrain heightfields. Narrow scope is intentional; later recipes depend on empirical driving QA.
- Map vehicle envelope is a simple rectangle and turn radius scalar. Geometric centre corridor clearance is not a full PhysX traversal certification for a long robot with overhang or spinning weapons.
- CombatArena has **no AI, no win logic**, only metadata for three potential enemy spawn points. ObstacleCourse emits waypoint descriptors **only**, not CHL003 scoring.
- Demo uses a simple Rigidbody cube with on-screen held buttons; it is not the physical wheel-driven robot from W8. After import, GPT OSS must test the actual vehicle separately.
- Source branch is based on historic `develop` SHA because no Codex/GPT OSS unpublished worktree is visible; no host integration authorized.
- MAP-R6 #3 remains independent and not tested with this source. Blender/models are irrelevant for graybox R1.

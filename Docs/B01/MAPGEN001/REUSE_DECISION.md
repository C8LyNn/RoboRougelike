# G0 / Reuse MAP-SYSTEM R6 rather than rewrite

Existing [MAP-R6 PR #3](https://github.com/C8LyNn/RoboRougelike/pull/3) HEAD `79b9605e07864b60fb38892c9e38f1cbb4072218`: `MapR4GraphValidator.cs` blob `28a16036526fa9717526becdbf26f8a494f7f665`, `MapR4GeometryBuilder.cs` blob `28101c17d9ceba5b1f762a00cdba88685f322a83`, `MapR5RoadMesh.cs` blob `3e521976891b402b1d0db06f1396930096bc39a4`, `MapR6RouteValidator.cs` blob `2d32b4b6389ba02d217a4c4ab4116d94dd00e658`, `MapR6JunctionPlanner.cs` blob `3d6be2f152a0ce572f87e74fb997dcfaf8894f0a`.

- **Reuse later through separate opt-in adapter**: MAP-R6 graph/turn/slope validation and road mesh when old PR #3 is compiled/tested with real Unity; the present minimal R1 uses self-contained C# primitives and direct safe corridor so an unmerged PR is **not a hidden compile dependency**.
- **Create new here**: seeded deterministic combat-cover/course-layout recipes, repeatable geometry signatures, lifecycle `Generate/Clear`, public result descriptors, isolated scene, two-rig graybox demo, isolated tests.
- **Explicit non-reuse**: MAP-001 #67 manual editor, R5 Gauntlet, RAC-001 #13 racing activity, CHL-003 #91 checkpoint gameplay. Not implemented, copied or rewritten.
- **Reason**: MAP-R6 source has 34/34 actual offline Python PASS but Unity compile, EditMode, PlayMode, real Rigidbody traversal NOT RUN; its node-junction seams are unproven. Importing it unreviewed would turn a small isolated producer task into an unsafe branch integration task.
- **Future R2**: optional source-sha-locked MAP-R6 adapter for turn-aware network maps, not part of R1. One geometry producer, no competing checkpoint state machine.

**Codex/GPT OSS active local worktree state:** UNKNOWN (no scope ACK observed). No shared files touched. B01-40 still must confirm G0 conflicts and recipient target SHA before release.

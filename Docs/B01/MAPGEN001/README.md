# MAPGEN-001 / R1 — isolated procedural map generator for Unity 6

One small actual Unity 6000.3.14f1 URP package, **no changes to RoboRougelike runtime**. Two modes: `CombatArena` (seed-variable cover grid, protected centre route, three enemy spawn descriptors) and `ObstacleCourse` (seed-variable floor/corridor, side slalom, inclined ramp, 3–5 ordered **checkpoint descriptors only**). No enemy AI, win condition or CHL-003 checkpoint state machine.

**Open** `Assets/B01Packages/MAPGEN001/Demo/MAPGEN001_Demo.unity` and press Play. GUI: switch seed, recipe, light/heavy reference rover, reset, press-and-hold movement. Generated geometry has BoxColliders, a physics Rigidbody rover with actual mass/gravity; this rover is **demo-only**, not the game's player vehicle. The layout planner is independent of Unity scenes and of MAP-R6 PR #3.

**Validation gate:** R1 is still a Draft PR and must pass Unity compilation/EditMode/PlayMode, manual physical runs and B01-60 independent QA before READY_FOR_GPT_OSS. Offline checks, where run, do **not** imply Unity PASS.

**Boundary:** MAPGEN makes geometry and metadata; CHL-003 #91 owns challenge rules/checkpoint trigger progression; Codex remains host game developer, GPT OSS is proposed owner of later controlled MAPGEN integration only.

## R3 (B01-20 production revision)
This isolated branch builds on R2 route BFS, adding stronger spawn/footprint validation, conservative corner fillet rejection, improved cover/ramp variability, full precommit cleanup handling and a rover-following camera. Unity compile/driving are NOT_RUN. Review changelog, updated test plan and per-file provenance manifest before any use. The user-approved integration recipient is GPT OSS **after** independent B01-60/B01-40 QA and B01-00 G2; no merge/deploy.

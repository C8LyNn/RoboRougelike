## W17 new explicit physical restrictions
Current CHL demo is rigidbody hull + spheres, NOT WheelCollider with rotating axle motors. Air control is **not** allowed: only supportive physical wheel-ray ground contacts permit drive/steering; no traction in midair. Gate crossing requires forward whole-body footprint and 0.25m lateral clearance; tight yaw may fail closed under conservative world collider AABB envelope. Author/test source exists but whether each rig can complete all six physical crossings and negative samples is **NOT_RUN** in licensed Unity; run real PlayMode13 and EditMode18 before any gameplay claims.


# CHL-003 R1 actual limitations
1. No licensed Unity6.3.14f1 runner available to this producer; native Editor compile, Edit15, Play9, physical game traversal, screenshots and Windows Player are NOT_RUN. .NET core PASS is not Unity PASS.
2. Demo uses runtime-created primitive graybox and physical Rigidbody boxes/spheres. Ramp ↔ bridge interfaces and actual Light/Heavy traffic cannot be certified until native driving G1. Visual readability requires B01-30 independent review.
3. Time limit disabled by default. No built-in competition opponent AI, economy, inventory, save/damage host mutation, difficulty/rewards, scoring persistence or canonical R3 mounts.
4. Legacy keyboard axes work only when ENABLE_LEGACY_INPUT_MANAGER; hold-to-drive UI buttons remain fallback for newer input config. No specialized gamepad bindings.
5. Demo masses/footprints not sanctioned PARTSIZE grid tiers; B01-23 owns physical part scale. Never auto-copy them into R3.
6. Original source base main 5f06af81e2dd9d877e4300259bb3dd008cb9c9d1 is a published repository baseline, not Codex's latest live game. Recipient target SHA/ownership unknown.
7. No claims that every physics trigger crossing is crash-free or that the environment supports Unity WebGL/Windows. G2 HOLD.

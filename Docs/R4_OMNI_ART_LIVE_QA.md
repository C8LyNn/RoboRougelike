# R4 Omni appearance

The R4 Omni model from #941 `c6b655d67c90ef91d7799460a0c0101756c4b975` decorates existing player OmniDrive blocks. Models on the vehicle's positive-X side use the source's requested X mirror. The original physical collider and Omni module/controller remain authoritative. Visual hub and roller rotation is an approximation derived from Rigidbody point velocity, not a separate solver or source animation clip. The primitive remains a fallback.

Unity 6000.3.14f1 **3/3 targeted tests PASS**, exit 0 (`r4-omni-first.xml`): actual light robot with four Omni modules strafes while a roller turns, source motor pods mirror across vehicle sides, real colliders and mounts are retained, all three classes strafe and yaw with the existing controller, and the Hover visual regression passes. The pre-implementation test failed because the model was absent (`r4-omni-red.xml`).

**NOT RUN:** Windows Player and human gameplay/visual review at this SHA, full Unity suites, source animation clips, all rotated mounts, and enemy/garage ghost appearance. Hub/roller direction and their approximate rate need visual review; no claim of mathematically exact Omni roller kinematics is made. The `11501df7` ZIP predates this change. Automated shutdown C0000005 and source FBX axis issues remain unresolved.

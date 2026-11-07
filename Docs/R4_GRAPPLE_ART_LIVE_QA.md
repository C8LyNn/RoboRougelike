# R4 magnetic grapple appearance

The supplied R4 grapple from #941 `c6b655d67c90ef91d7799460a0c0101756c4b975` is attached to the existing player MagneticGrapple block. Its mount and launch directions are aligned to the block, while the existing physical emitter position, collider, raycast, forces, energy and LineRenderer rope remain unchanged. The FBX cable and magnet glow are hidden; the game's real rope remains visible. The old primitive remains a fallback if required nodes or valid axes are unavailable.

Unity 6000.3.14f1 **4/4 targeted tests PASS**, exit 0 (`r4-grapple-first.xml`): supplied model mount and launch axes, existing emitter position and collider, actual attach/release, static and moving targets, force interaction, detached emitter release, Blink guard and Blink visual regression. The pre-implementation test failed because the new model was absent (`r4-grapple-red.xml`).

**NOT RUN:** Windows Player or human gameplay/visual review at this SHA; full Unity suites; source spool, claw and cable animations; enemy and garage ghost appearances; all rotated mounts. The art nozzle's position relative to the original physical emitter and rope needs manual review; the gameplay emitter was intentionally not moved. The most recent Player ZIP `bc866560` predates this integration. Automated shutdown C0000005 and source FBX axis defects remain unresolved.

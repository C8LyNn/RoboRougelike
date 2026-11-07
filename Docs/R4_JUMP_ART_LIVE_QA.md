# R4 jump drive appearance

The R4 JumpDrive model from #941 `c6b655d67c90ef91d7799460a0c0101756c4b975` now decorates the existing R3 JumpDrive block. The original primitive collider and MOV002 physical impulse, energy, ground checks, cooldown and damage remain in charge; the original appearance is a fallback if the imported asset is missing. A brief visual blast ring is driven by the real `JumpDrive.Activated` event and does not generate force. Piston articulation and source animation clips are not wired.

Unity 6000.3.14f1 targeted **3/3 PASS**, process exit 0 (`r4-jump-final.xml`): real jump activates the blast, preserves horizontal momentum and existing collider, rejects an immediate repeat, preserves existing jump/booster/gyro behavior, and keeps MOV009 visual regression passing. The initial pre-implementation test failed because the new model was absent (`r4-jump-red.xml`).

**NOT RUN:** Windows Player and human gameplay/visual review at this revision; full Unity suites; piston animation, enemy/garage ghost appearance and every rotated mount. The smoke-tested ZIP at code SHA `d1a33a74` predates this change. The automated Windows shutdown C0000005 remains unresolved and no raw FBX axis fix is claimed.

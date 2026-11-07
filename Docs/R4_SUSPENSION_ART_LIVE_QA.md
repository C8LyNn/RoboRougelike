# R4 suspension appearance

The R4 suspension from movement PR #941 `c6b655d67c90ef91d7799460a0c0101756c4b975` is attached to the player's existing suspension block. This adds no collider or force; MOV009 still owns the support module, settings, force and damage. The previous piston renderer is hidden only if the imported model and required nodes exist, and remains the fallback otherwise. The visible model is currently static: its arm and damper do not yet follow actual travel.

Unity 6000.3.14f1 targeted **3/3 PASS**, exit 0 (`r4-suspension-first.xml`): real module and original block collider retained, mount position/normal checked, no new art collider, destroyed support stops working, actual suspension force/pause behavior, and previous gyroscope visual regression. The initial pre-implementation test failed due to the model being absent (`r4-suspension-red.xml`).

Windows x64 retail build **PASS** at code SHA `d1a33a74f17b49715a01f716dee4cc6569e3cce6`, Unity exit 0. ZIP `RoboRougelike_R4_Movement_d1a33a74_Windows_x64.zip`, 43,508,126 bytes; SHA-256 `f3b45574318e0eb71b2c9c86cf226c3c75718d831ea7b423db490e2e3ca8fb82`. The extracted EXE matched and reached menu bootstrap in a hidden Player. Automated `WM_CLOSE` again exited `-1073741819` (C0000005); the cause is unresolved. Logs: `r4-suspension-d1a33a74-build.log` and `r4-suspension-d1a33a74-player-smoke.log`.

**NOT RUN:** human visual review and manual gameplay at this revision, dynamic model articulation/animation, full Unity suites, enemy/garage ghost appearance, and mount orientation for every possible rotated construction. No claim is made that raw FBX axis defects or automated Windows shutdown C0000005 are fixed.

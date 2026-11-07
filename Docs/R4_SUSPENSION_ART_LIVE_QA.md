# R4 suspension appearance

The R4 suspension from movement PR #941 `c6b655d67c90ef91d7799460a0c0101756c4b975` is attached to the player's existing suspension block. This adds no collider or force; MOV009 still owns the support module, settings, force and damage. The previous piston renderer is hidden only if the imported model and required nodes exist, and remains the fallback otherwise. The visible model is currently static: its arm and damper do not yet follow actual travel.

Unity 6000.3.14f1 targeted **3/3 PASS**, exit 0 (`r4-suspension-first.xml`): real module and original block collider retained, mount position/normal checked, no new art collider, destroyed support stops working, actual suspension force/pause behavior, and previous gyroscope visual regression. The initial pre-implementation test failed due to the model being absent (`r4-suspension-red.xml`).

**NOT RUN:** Windows Player at this revision, human visual review, dynamic model articulation/animation, full Unity suites, enemy/garage ghost appearance, and mount orientation for every possible rotated construction. No claim is made that raw FBX axis defects or automated Windows shutdown C0000005 are fixed.

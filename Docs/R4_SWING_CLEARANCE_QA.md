# Swing tire self-clearance

Based on Draft PR #1056. During live compression, the moving physical tire
checks owned colliders and stops at the last safe angle before penetrating
other parts on the same Rigidbody. This uses the existing collider geometry;
no forces or extra physics body are introduced. Collider lists are reused for
the lifetime of the rebuilt vehicle and detached parts are excluded.

- A targeted Unity reproduction at `4076078a8ff91f5531d531f055cd72ba262cebb5`
  showed tire penetration: `RC2-SwingClearanceRepro-20261108T100250Z`.
- Native Unity 6000.3.14f1 EditMode runner, tests entering Play mode:
  **3/3 PASS**, exit 0, at `d17d93b601f1a733167b7d8d0b30beba44fb0b0f`.
  Covers self-clearance, unobstructed ground response and real R6 drive with
  existing MOV-009; evidence `Artifacts/Local/RC2-SwingClearance-20261108T100409Z`.
- Complete freeform builds, repeated disconnections, rough terrain and large
  vehicle performance require additional testing. Windows Player at this
  commit is not yet built.

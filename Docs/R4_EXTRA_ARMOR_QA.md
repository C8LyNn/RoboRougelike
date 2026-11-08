# Additional one-cell armor

Production source: `cfd3f8d118ce03b027f4b1c20ecc13108a1751e2`, stacked on
Draft PR #1042. Imports the existing #963 half-block and pyramid FBX visuals
and their pinned convex meshes. Both reserve one 0.55 m cell, keep their own
inventory identity, use only verified full mount faces and preserve shape in
checkpoints. The current 1×1×1 rule and the original damage and repair systems
remain in effect. No new multi-cell part is enabled.

Unity 6000.3.14f1 EditMode runner: **16 passed, 0 failed, 0 skipped**, exit 0.
The tests enter Play mode and cover real purchases, mounting, physical bounds,
authored renderers, rotation, checkpoint restore, old wedge and wheel/catalog
regressions. XML and Editor log:
`Artifacts/Local/RC2-MoreArmorShapes-20261108T000714Z/`.

Windows Player for these two additional shapes: **NOT RUN**. The earlier build
`53d6d4c0` contains the cube and wedge, but predates these two shapes.

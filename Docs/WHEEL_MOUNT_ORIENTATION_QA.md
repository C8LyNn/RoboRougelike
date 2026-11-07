# Wheel mounting orientation — QA

Earlier player/ghost art mirrored from absolute grid X, which could face the wrong side after a wheel was rotated or mounted away from a starter layout. The adapter now examines the rotated local axle and compatible live neighbors; ambiguous placements retain a deterministic fallback. Existing colliders, vehicle forces and wheel stats are untouched.

Unity 6000.3.14f1 EditMode at `038f70a5235c72629884a1a332a7ddd997fc235c`: 23/23 passed, exit 0, including wheel drive, independent steering, ghosts and save-backed variants. Actual model socket orientations at `7bda8ff1f74f04602eae476f2cf8da1646b2e451`: 5/5 passed, exit 0; eight actual model/mount combinations (both appearances, normal/rotated, both sides) checked against their support direction. Local evidence: `Artifacts/Local/RC2-WheelMount-Verify-038f70a5-20261107T232004Z/` and `Artifacts/Local/RC2-WheelSocketVisual-7bda8ff1-20261107T232208Z/`.
Windows Player after this commit: NOT RUN. Layouts with multiple possible neighbors use fallback and remain to be examined manually.

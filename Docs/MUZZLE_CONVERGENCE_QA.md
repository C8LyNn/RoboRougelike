# Muzzle convergence — QA

At `04bb0620` the expanded native tests found muzzle errors of 1.14 degrees at 10 m, with several downstream test failures. The turret was aiming from its pivot while the actual projectile originated at the laterally displaced art muzzle. Aiming from the actual current muzzle allows the existing rotational servo to converge without changing camera behavior, fire rate, range, damage, obstruction checks or physics.

Unity 6000.3.14f1 EditMode at `f53dc2946bd16e8db0013538e16b7434db760766`: 14/14 passed, exit 0. Real Basic projectiles damaged the target at 10, 30, 60 and 100 m; real Automatic damaged at 30 m; Beam correctly reported out of range at 100 m; weapon group and replacement art tests passed. Evidence: `Artifacts/Local/RC2-MuzzleConvergence-f53dc294-20261107T225304Z/`.

Windows Player and human assessment of aiming after this change: NOT RUN. Enemy shooting remains on its own existing logic. No full regression performed.

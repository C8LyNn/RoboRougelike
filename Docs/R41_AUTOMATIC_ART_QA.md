# Automatic R41 turret integration — QA

Source FBX: `R41_Turret_v6.1.fbx`, SHA-256 `03c60486a6af5890f1106bc090d70bb3c09a67f8278e02c2942cab8e77845d6a`, artist-delivered file on Desktop. Unity 6000.3.14f1 import exit 0 with `bakeAxisConversion: 1`. Required GunPivot, R41_Yaw, R41_Pitch and Muzzle nodes were inspected in the Unity import log.

Production commit: `3f709ae746663eb748dfd69486a1640a4208abc1`. The existing aiming adapter and firearm are used for the player and enemies; original physical collider is retained and projectiles use the visible muzzle.

- Red at `0b5aaeb1`: 5 tests, 3 passed, 2 failed: automatic art missing on player and enemy.
- Targeted native EditMode at `04bb0620`: 11/11 passed, exit 0, including real weapon groups and automatic fire. Local evidence: `Artifacts/Local/RC2-AutomaticArt-Groups-04bb0620-20261107T223406Z/`.
- Expanded suite at `04bb0620`: 11/16 passed, exit 2. `Camera275AimNativeTests` still has five failures, including muzzle convergence at 10 and 30 metres; no claim of camera/aim regression pass. Older fixture's incompatible mount order was corrected, but convergence remains to investigate.
- Windows Player/manual visual assessment: NOT RUN for this commit. Known Unity Player automated shutdown exception remains independently unproven.

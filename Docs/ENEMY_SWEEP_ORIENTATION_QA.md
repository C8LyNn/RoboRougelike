# Enemy obstacle sweep orientation

The enemy navigation box sweep used its non-square chassis extents in world-axis orientation. A 90-degree turned enemy could treat a wall outside its rotated footprint as a blocking obstacle. The sweep now follows chassis yaw while remaining upright; this changes obstacle detection only and does not add sideways propulsion, alter damage or teleport vehicles.

Unity 6000.3.14f1 native EditMode runner, tests entering Play Mode:

- Prior source: **FAIL**, physical side obstacle incorrectly blocked a 90-degree rotated enemy; `Artifacts/Local/EnemySweepOrientation-Red-20261108/`.
- Updated source: **7/7 PASS**, exit 0, `Artifacts/Local/EnemySweepOrientation-Focused-20261108/results.xml` and `Editor.log`. This includes side-clear and front-blocked casts, boundary movement with real R6 wheels, and the nearby/far self-destruct cases.
- The cast fixture uses a real enemy Rigidbody and physical obstacle; the sweep is invoked directly while the body is parked. This does not establish that an enemy can physically turn in a corridor narrower than its chassis; see `ENEMY_BOUNDARY_QA.md`.
- Windows Player and human combat for this change: **NOT RUN**. Logs and XML are local QA artifacts.

Separately, the preceding integrated Windows Player `1bea5602` was opened in a window. In the sandbox garage the tester removed both real guns; the action feedback and the missing-weapon deployment warning were visible together and the test-drive button was disabled. The session returned to the main menu and exited; process exit code was not recorded. Portrait layout and other resolutions were not checked. This observation predates the sweep change.

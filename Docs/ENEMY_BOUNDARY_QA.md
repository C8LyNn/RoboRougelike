# Enemy boundary and trapped fallback

Based on Draft PR #1057. An enemy close to an arena limit now loses only
outward velocity, preventing residual momentum escaping the limit while
preserving tangent and inward movement. No teleport or lateral motor is added.

- Initial targeted review at `4511478380356394cc1e181a86a18559ed148304`:
  **3/5 PASS**. Boundary ramming exceeded the limit by 0.0003 m and the
  blocked-lane retreat test failed. Evidence `RC2-DriveAndEnemyReview-20261108T100638Z`.
- Physical diagnostic `RC2-EnemyBoundaryGeometry-20261108T101138Z` shows
  the enemy requests inward movement, rotates about seven degrees, then its
  body contacts the corridor walls and cannot turn. The existing two-second
  retreat assertion remains **FAIL**, not silently disabled or loosened.
- Boundary ramming and two independent five-second warning/explosion cases,
  including a physically trapped unarmed robot near the player: **3/3 PASS**
  at `677ade5b4fae8513ccc770bc76351ab96e703c1f`, evidence
  `RC2-EnemyBoundaryFix-20261108T101323Z`. After avoiding redundant velocity
  writes, boundary ramming alone **1/1 PASS** at
  `7aa85856d45f5716d9215dfc593793f2addd2c37`, evidence
  `RC2-EnemyBoundaryFinal-20261108T101457Z`.
- All runs are Unity 6000.3.14f1 native EditMode runner with tests entering
  Play mode. Windows Player and human playtest at this SHA: **NOT RUN**.
- Remaining: a physically wedged enemy cannot retreat in that geometry;
  when the player is nearby the existing delayed blast resolves it. Behavior
  when trapped and the player stays outside blast range still needs design
  and testing. A full alternative recovery maneuver is not claimed fixed.

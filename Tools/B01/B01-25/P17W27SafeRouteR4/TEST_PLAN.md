# W27 SAFEROUTE R4 — acceptance criteria and actual environment evidence plan

AC-01: When two physical crates start at (3,8) and (11,8), both real colliders are sensed and every one of the Light/Heavy × gate closed/open × hazard active/inactive states has a valid BFS witness (C#9 source R4 suite + native `W27TwoOriginalRealPhysicalCratesOccupyDistinctCells`).

AC-02: When crate A is at (3,8), relocation of crate B to (5,5) is **REJECTED** before `Rigidbody.position` moves, though B at (5,5) alone would pass. Heavy closed-gate/hazard-active is NO_PATH if B is moved there externally. Light can still have a bypass. This prevents a composite softlock. Source tests plus native `W27TwoSoloSafeCratesCombinedHeavySoftlockIsRejected` and `W27UnexpectedSecondCrateMoveFailsClosedWithoutFalseGuidance`.

AC-03: Safe B→(4,4) with A stationary is allowed, actual collider occupancy updated, and Heavy route remains reachable. Native `W27SafeMoveOfSecondPhysicalCrateKeepsHeavyRoute`.

AC-04: External displacement of either physical crate within the same cell (e.g. 0.14m) increments route revision and obtains a new physical occupancy snapshot; subcell movement must not preserve stale guidance. Native `W27SubcellObstacleMoveTriggersRealPhysicalReplan`.

AC-05: Saturated physics `OverlapBoxNonAlloc` (256 returned) or disabled/unobservable physical crate returns NO_PATH, not false route. Source inspection checker verifies fail-closed code; **native high-contact overflow simulation required for acceptance** (not covered in authored 17 runtime test cases).

AC-06: Original R1/R2/R3 regression source tests remain valid; no changed R3 Git bytes. Portable C#9 original executable tests on Windows/Ubuntu, physical Git SHA+GUID+rollback on both, third job downloads original 24-file Unity SOURCE ZIPs and uses actual sha256sum+cmp. Results must be tied to exact HEAD SHA. No native Unity claims without true XML/Editor.log/exit.

RISK=MEDIUM/HIGH: bad waypoint could softlock Heavy, overflow/serialization or false collider information can present fictitious safe traversal. Mitigate by fail closed, independent G0, real native physics testing and explicit rejection rather than silent teleport. SOURCE tests cannot validate actual PhysX collision or game feel.

**STATUS:** source CI pending until Action completes; Unity G1 `NOT_RUN`, manual playtest `NOT_RUN`, formal G0 `PENDING`.

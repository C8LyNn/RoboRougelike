# R4 gun mounting-port orientation tests

Follow-up: [native QA on a3d37d30](R4_BACKLOG_NATIVE_QA_20261107.md) passed
these targeted Unity checks; in-game wiring remains pending.

Prepared native checks for all 24 distinct orientations: an existing wedge's
partial structural patch may overlap an attached gun geometrically, but the
module-specific definition must reject it; a full cube face should allow it.
The offline identity-only fixture has matching checks. These tests exercise the
staged adapter and BUILD001 geometry, not actual garage integration.

**NOT RUN:** Unity compilation/test execution, the offline fixture, actual
weapon clearance, physics, save/load and player build. No runtime behavior is
changed. The staged caller must still select module-specific support for guns
while retaining structural connectivity; that wiring is not implemented.

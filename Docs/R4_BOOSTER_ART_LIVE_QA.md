# R4 booster appearance

The supplied movement model from #941 `c6b655d67c90ef91d7799460a0c0101756c4b975` is connected to the player's existing R3 Booster block. The old visible cylinder is hidden only when all required art nodes exist; its collider remains enabled. Existing `VehicleBooster` remains responsible for energy, thrust, duration and cooldown. Its actual state controls the visual flame and turbine; these do not affect physics. The existing primitive remains the fallback.

The imported socket's `up` axis is parallel to the exhaust despite the source manifest's declared normal. Native Unity inspection showed nozzle forward `(0,0,-1)` and socket forward `(0,-1,0)` on an unrotated block. The instance adapter aligns the exhaust with the negative thrust direction and the socket's imported negative-forward normal with block up. The source FBX and gameplay force were not changed. This is an instance correction, not a claim that the underlying FBX axis defect is fixed.

## Results

- Unity 6000.3.14f1 targeted tests: **4/4 PASS**, exit 0 (`r4-booster-final.xml`). They cover the model's exhaust and mount axes, preserved collider, flame following activation, physical boost with mass sensitivity and persistent cooldown, single-body module integration, and the preceding wheel visual regression.
- The first attempted run had 3/4 passing. The new test revealed that the source axes differed after import, and the safety fallback correctly discarded the model. Diagnostic and corrected XML/logs remain available as `r4-booster-first`, `r4-booster-debug`, `r4-booster-axes`, and `r4-booster-final` under the local Prototypy directory.
- **NOT RUN:** Windows Player at this revision, manual visual/gameplay review, full Unity suites, imported animation clips, enemy or garage ghost appearance. Flame proportions, art placement and visual quality require human review. The catalog also contains other movement models; only the wheel and booster have been connected so far.
- Earlier automated Player shutdown returned C0000005; root cause is unresolved and this visual change does not claim to address it.

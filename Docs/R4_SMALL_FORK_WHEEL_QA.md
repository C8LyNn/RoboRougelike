# Small fork wheel and underbody view

Source: #1018 `R4M_WheelS_Fork_Road.fbx`, SHA-256
`f67c3da798c533180e2ec1813cbf515c50f09d680c5bd1d1cfcb66a1ef604b7c`.
Based on Draft PR #1046. It uses the authored upper port and 0.205 m tire
radius, a compact physical hub, 6 kg mass and 0.65× grounded traction. Its
single grid cell has only the local upper port. The existing R6 and damage
systems remain authoritative; incompatible saved or recovered port masks are
rejected. Price currently uses the existing wheel price.

The garage camera can look beneath the vehicle, hiding the decorative floor
while below it. `U` toggles this view, and the normal orbit also permits it.

- Unity 6000.3.14f1 EditMode runner, tests entering Play mode: **21/21** at
  `69eed2785c4f54e26ff5e729d4f10ffe8367c28b`, including actual R6/Rigidbody
  drive around 9 m in 1.6 s, grounding, attachment, rotations, checkpoint,
  and wheel/armor/catalog regressions. Evidence: `RC2-SmallFork-20261108T004358Z`.
- Broader garage run: **35/36**, with one stale assertion still expecting the
  pre-replacement wheel and automatic-gun art. Those exact expectations were
  updated to the current supplied assets; the corrected model test and the
  underbody socket/shortcut tests subsequently passed **3/3** at `c4f98ee1`.
  Evidence: `RC2-SmallForkUnderside-20261108T004825Z` and
  `RC2-SmallForkUnderbodyRetest-20261108T005217Z`.
- Small-wheel recovered-part validation and corrected garage model test:
  **2/2**; existing salvage suites: **12/12**, native exit 0 at `2296b344`.
  Evidence: `RC2-SmallForkSalvage-20261108T005609Z` and
  `RC2-SmallForkSalvageRegression-20261108T005647Z`. Evidence paths are under
  `Artifacts/Local/`.
- Windows Player source `874f3a8e`: Unity build exit 0, actual garage purchase,
  U view, underside mounting and return to normal view all observed, followed
  by normal menu exit. Later text and salvage validation changes are not in
  this particular Player build. Full campaign, terrain and standalone PlayMode
  suite: **NOT RUN**.

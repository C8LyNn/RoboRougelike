# Automatic rocket reload status — QA

The weapon HUD now displays rocket ammunition and whether the launcher is ready, cooling down or automatically reloading, instead of implying that a manual reload is required. This is display-only; shooting and reload logic are unchanged.

Unity 6000.3.14f1 EditMode at `ceb204a48135dc223e9313e308644b6650e4807f`: 26/26 passed, exit 0, including automatic reload, actual rocket physics and HUD state cases. Before implementation, the four new status cases failed at `73cabe12`. Local evidence: `Artifacts/Local/RC2-RocketHUD-ceb204a4-20261107T234056Z/`.
Windows Player and readability at all target resolutions after this commit: NOT RUN.

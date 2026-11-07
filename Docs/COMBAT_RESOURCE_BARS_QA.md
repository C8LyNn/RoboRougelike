# Combat resource bars — QA

Compact HUD presents core HP and available energy as labelled numeric values with small colored bars; objective and repair status remain visible. Input and mechanics are unchanged. Values are clamped and non-finite/zero maxima render empty bars.

Unity 6000.3.14f1 EditMode at `47282137f65a7c19cb9fdd3773b0c57d0a4972f2`: 20/20 passed, exit 0. Evidence: `Artifacts/Local/RC2-CombatResourceBars-47282137-20261107T232409Z/`.
Windows Player `50d67bc7` build exit 0; navigated menu → garage → route → combat, observed labels, bars and objective; expanded details with I; paused and exited. Player test used one window size. Other resolutions, controller input and full UX/combat regression remain NOT RUN.

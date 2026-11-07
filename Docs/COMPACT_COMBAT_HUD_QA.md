# Compact combat HUD — QA

The default combat panel keeps encounter, enemies, scrap, core health, objective, energy and repair status visible. Construction, wheel/weapon counts, mass and speed remain available under `I` / Szczegóły. Victory pickup timing remains visible when active. No combat or input systems were replaced.

Unity 6000.3.14f1 EditMode at `6e7d2987f890b1a39dfc38c41a6eaad3d5759011`: 29/29 passed, exit 0. After adding the keyboard shortcut, at `0043b398b7fd825bb388e3fd982427c7a7b2283b`: 15/15 passed, exit 0, including a shortcut test checking movement and mouse events are not consumed. Evidence: `Artifacts/Local/RC2-CombatHUD-6e7d2987-20261107T231207Z/` and `Artifacts/Local/RC2-CombatHUDShortcut-0043b398-20261107T231459Z/`.
Windows Player `3923504e` build exit 0; actual menu → garage → route → combat, and keyboard I opened and closed the details while enemies were active. Paused and exited. Controller shortcut and the five requested output resolutions have not been verified; full UX acceptance and full regression remain NOT RUN.

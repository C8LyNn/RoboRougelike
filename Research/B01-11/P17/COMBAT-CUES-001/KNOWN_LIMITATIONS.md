# Known limitations

- Research prototype only: no gameplay Unity code, real 3D viewport, real colliders, R3 part IDs, actual combat damage, true 2-group weapon policy, scoring, or economy. No Codex gameplay integration.
- Five fixed parts, one sample weapon group and three synthetic event fixtures. Hit classification is educational; the host must remain authoritative.
- Uses live text and semantics, but has **not** been tested with actual screen-reader users, color vision conditions, keyboard-only participants or official game UI.
- Full-screen mobile at width 320 is tested for **horizontal overflow**, not subjective content density or player comprehension.
- First-event replay and all-events batch deterministic; not a timed combat system. No audio, VFX, physics, AI, balance or multiplayer.
- Browser permissions blocked file:// automation; Playwright set_content verified the exact HTML bytes. Different Chromium executable path on Windows requires local adjustment to test runner.

# PASS 16: LD02 arena native preliminary validation

Source fixture: `Docs/CX08/Fixtures/LD02_ARENA.json`, producer commit `691c8cb6b0be086d455e530f2531de7f3753a0ba`, Git blob `e39e2f174984832bd97311c050e3c22fd6fb1175`. Game source under test: `16c2668104952f1b8db304cc7817b897fb48a9b1` plus this QA test. Unity 6000.3.14f1, URP, native EditMode with actual Play Mode physics. See `Evidence/native.xml` and `native-exit.txt`; full Editor log is `Artifacts/Local/CX03/p16-ld02-native.log` in this checkout.

- PASS: schema loaded by the game's MapDraft, spawn and finish exist.
- PASS: eight independent rays find actual MapWorld wall/corner colliders.
- PASS: standard Light R3 rig, driven by actual R6 wheel forces, traveled from (8,20) to (34.05,20.00), with 158 grounded frames.
- PASS: standard Heavy R3 rig similarly reached (34.07,20.02), with 221 grounded frames.
- NOT_RUN: other routes, active enemies, combat cover, collision-free enemy spawn envelopes, reward transitions, Windows Player rendering, and manual play. These three tests cannot establish the arena's full combat readiness.
- PARTIAL: the LD02 data is not offered as a combat arena. No producer package or existing player activity was changed. Additional enemy-spawn and full encounter validation is required before exposing it.

# R4 hover appearance

The supplied R4 Hover model from #941 `c6b655d67c90ef91d7799460a0c0101756c4b975` decorates the existing player HoverEngine block. Its rotor and thrust glow respond to the real pod's measured positive lift force. The existing collider, hover controller, shared energy and movement physics remain authoritative. The primitive is retained as a fallback, and the supplied pulse/louvre animation is not wired.

The FBX nozzle empty's imported `forward` pointed upward even though the source describes its positive axis as exhaust. The adapter interprets its negative-forward direction as the exhaust axis and also validates that the nozzle position is below the top-face socket. This is a per-instance interpretation; it does not fix the underlying source FBX axis issue.

Unity 6000.3.14f1: the initial grouped run was **3/4 PASS, 1 FAIL** (`r4-hover-first.xml`); the new test exposed the nozzle-axis discrepancy. After correcting the axis interpretation, the targeted Hover test was **1/1 PASS** (`r4-hover-retest.xml`, exit 0): actual lift activates the glow, switching back to wheels removes real lift and glow, and the collider and mount remain intact. The earlier passing group also covered alternative-drive modifiers, speed/turn limits and grapple regression; those tests were not rerun after the visual-axis-only correction. The pre-implementation test failed because the model was absent (`r4-hover-red.xml`).

Windows x64 retail build **PASS** at code SHA `11501df77a88bc5ddb674020f5976a1f1c53556c` (Unity exit 0). ZIP `RoboRougelike_R4_Hover_11501df7_Windows_x64.zip`, 43,510,112 bytes, SHA-256 `6e1b79998394e8b56d88dcb72fa6b511298976b95c4aeadcfe710c6d85d0da9f`. The extracted EXE matched and reached menu bootstrap in a hidden Player. Automatic WM_CLOSE returned `-1073741819` (C0000005); cause unresolved. Logs: `r4-hover-11501df7-build.log` and `r4-hover-11501df7-player-smoke.log`.

**NOT RUN:** human gameplay/visual review at this SHA; full Unity suites; source clips, enemy and garage ghost appearance, and every rotated mount. Rotor axis, proportions and effects need human review. Automated shutdown C0000005 and source FBX axis issues remain unresolved.

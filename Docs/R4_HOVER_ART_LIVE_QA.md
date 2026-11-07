# R4 hover appearance

The supplied R4 Hover model from #941 `c6b655d67c90ef91d7799460a0c0101756c4b975` decorates the existing player HoverEngine block. Its rotor and thrust glow respond to the real pod's measured positive lift force. The existing collider, hover controller, shared energy and movement physics remain authoritative. The primitive is retained as a fallback, and the supplied pulse/louvre animation is not wired.

The FBX nozzle empty's imported `forward` pointed upward even though the source describes its positive axis as exhaust. The adapter interprets its negative-forward direction as the exhaust axis and also validates that the nozzle position is below the top-face socket. This is a per-instance interpretation; it does not fix the underlying source FBX axis issue.

Unity 6000.3.14f1: the initial grouped run was **3/4 PASS, 1 FAIL** (`r4-hover-first.xml`); the new test exposed the nozzle-axis discrepancy. After correcting the axis interpretation, the targeted Hover test was **1/1 PASS** (`r4-hover-retest.xml`, exit 0): actual lift activates the glow, switching back to wheels removes real lift and glow, and the collider and mount remain intact. The earlier passing group also covered alternative-drive modifiers, speed/turn limits and grapple regression; those tests were not rerun after the visual-axis-only correction. The pre-implementation test failed because the model was absent (`r4-hover-red.xml`).

**NOT RUN:** Windows Player and human gameplay/visual review at this SHA; full Unity suites; source clips, enemy and garage ghost appearance, and every rotated mount. Rotor axis, proportions and effects need human review. The `bc866560` ZIP predates this change. Automated shutdown C0000005 and source FBX axis issues remain unresolved.

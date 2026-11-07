# R4 Blink appearance

The R4 Blink model from #941 `c6b655d67c90ef91d7799460a0c0101756c4b975` decorates the existing Blink block. Its socket uses the **bottom face**, as required by its model manifest. The original collider, `BlinkDrive`, collision and landing checks, energy and cooldown remain authoritative. A short phase ring and flash are signaled only after the existing vehicle coordinator receives `BlinkResult.Activated`; failed attempts do not produce them. Original primitive visual remains a fallback. The supplied petal, charge and recovery animation clips are not connected.

Unity 6000.3.14f1 **4/4 targeted tests PASS**, exit 0 (`r4-blink-first.xml`): actual 8 m Blink triggers the effect, cooldown blocks another activation, original collider and socket position remain valid, all three vehicle classes preserve their assembly and momentum, unsupported destination is rejected, and the earlier 32-combination movement mount test remains green. Before implementation the new test failed because the model was absent (`r4-blink-red.xml`).

**NOT RUN:** Windows Player and human gameplay/visual review at this SHA; full Unity suites, source clips, enemy and garage ghost appearances, and every combined rotation. The ZIP `bc866560` predates this change. Automated shutdown C0000005 and the source FBX axis issues remain unresolved.

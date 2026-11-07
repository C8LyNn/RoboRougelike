# R4 wheels and independent steering

The initial R4 wheel integration left the imported model outside the MOV011 visual steering pivot. MOV011 also created an additional visible clone of the old wheel, so an equipped robot could display a duplicated primitive while the new wheel did not steer. This change reparents the imported model under the existing yaw pivot, preserving its world pose; when the imported model is unavailable, MOV011 retains the original primitive fallback. The physical tire collider, R6 drive and independent steering solver are unchanged.

Unity 6000.3.14f1: a targeted pre-fix test **FAIL** reproduced the imported wheel outside the steering pivot (`r4-steering-red.xml`). After the fix **5/5 targeted tests PASS**, exit 0 (`r4-steering-first.xml`): four real wheels use the imported visual under the yaw pivot without an old-mesh duplicate, the real steering solution turns that pivot, original tire colliders remain enabled, existing crab/counterphase R6 movement works, and wheel/track visual regressions pass.

**NOT RUN:** Windows Player and human gameplay/visual review at this revision, full Unity suites and all vehicle builds. The latest ZIP `11501df7` predates this fix and the Omni/track integrations. Automated Player shutdown C0000005 and source FBX axis issues remain unresolved.

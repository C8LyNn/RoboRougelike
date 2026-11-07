# R4 independent steering appearance

The R4 IndependentSteering model from #941 `c6b655d67c90ef91d7799460a0c0101756c4b975` decorates the existing player module. The existing one-cell footprint, root collider, MOV011 binding, R6 solver and wheel steering remain authoritative. The original block renderer is a fallback if the model is unavailable. The supplied servo and knuckle are currently static; source clips are not wired.

Unlike the older #963 intake document, this game's current `R3VehicleModel.UsesWideFootprint` returns false: independent steering already occupies one cell. This visual change does not alter the footprint or migrate saves.

Unity 6000.3.14f1 **4/4 targeted tests PASS**, exit 0 (`r4-independent-first.xml`): source model and socket on the actual one-cell part, original physical collider, real steering solution, imported wheel yaw without duplicated primitive, crab/counterphase R6 motion and crawler visual regression. The pre-implementation test failed because the model was absent (`r4-independent-red.xml`).

**NOT RUN:** Windows Player and human gameplay/visual review at this SHA, source servo/knuckle animation, all rotated mounts, full Unity suites, enemy/garage ghost appearance and performance with many high-poly models. All eleven supplied movement models now have player-side appearance hookups on this stacked branch, but this does not imply that every supplied animation, fallback, visual state or gameplay combination is verified. The `11501df7` ZIP predates Omni, tracks and this change. Automated shutdown C0000005 and source FBX axis issues remain unresolved.

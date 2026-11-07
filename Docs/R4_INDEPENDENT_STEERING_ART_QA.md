# R4 independent steering appearance

The R4 IndependentSteering model from #941 `c6b655d67c90ef91d7799460a0c0101756c4b975` decorates the existing player module. The existing one-cell footprint, root collider, MOV011 binding, R6 solver and wheel steering remain authoritative. The original block renderer is a fallback if the model is unavailable. The supplied servo and knuckle are currently static; source clips are not wired.

Unlike the older #963 intake document, this game's current `R3VehicleModel.UsesWideFootprint` returns false: independent steering already occupies one cell. This visual change does not alter the footprint or migrate saves.

Unity 6000.3.14f1 **4/4 targeted tests PASS**, exit 0 (`r4-independent-first.xml`): source model and socket on the actual one-cell part, original physical collider, real steering solution, imported wheel yaw without duplicated primitive, crab/counterphase R6 motion and crawler visual regression. The pre-implementation test failed because the model was absent (`r4-independent-red.xml`).

Windows x64 retail build **PASS** at code SHA `6711673fa389d2a2aa1b8ab101ff6d9cab366135` (Unity exit 0). ZIP `RoboRougelike_R4_Movement_6711673f_Windows_x64.zip`, 43,517,866 bytes, SHA-256 `4283ba16b55438663e2d9e5affe85e26e6df67710ebab23fd37da655a1878339`. The extracted EXE matched and reached menu bootstrap in a hidden Player. Automatic WM_CLOSE exited `-1073741819` (C0000005); cause unresolved. Logs: `r4-movement-6711673f-build.log`, `r4-movement-6711673f-player-smoke.log`.

**NOT RUN:** human gameplay/visual review at this SHA, source servo/knuckle animation, all rotated mounts, full Unity suites, enemy/garage ghost appearance and performance with many high-poly models. All eleven supplied movement models now have player-side appearance hookups on this stacked branch, but this does not imply that every supplied animation, fallback, visual state or gameplay combination is verified. Automated shutdown C0000005 and source FBX axis issues remain unresolved.

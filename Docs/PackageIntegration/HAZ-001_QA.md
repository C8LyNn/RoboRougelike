# HAZ-001 — original telegraphed arena hazards

Pinned source PR#12: `d102a27bb7cb56349a4f125ee828712825221922`, additive delta from `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`. Import verified27 exact Git blobs and15 new unique GUIDs without overwriting any existing file. Runtime integration commit `c30a6517012d9563c3f4fa285d3c4356db450f32`; final targeted source `32653684fd208be11bb0344f7c9a353791ef2deb`, based on COV `b166b8e843ab761c6358d6526967716829efc8d9`.

Every existing arena layout has two zones at (-6,0,-6) and (6,0,8), away from spawns and industrial halls. Original warning1.5s/active2s/cooldown3s/radius3m/interval0.5s/damage5 parameters and original Update/FixedUpdate clock are retained. The original root-deduplicated request resolves through the existing hit boundary to one actual living attached P12 part inside the radius per vehicle. There is no second HP pool, clock or physics force. The indicator has no collider. Pausing freezes the clock and explicitly blocks damage; leaving combat disables and resets the owned zones and hides warning rings.

Initial original compilation failed because both test asmdefs declared runner references already supplied by TestAssemblies. Only duplicate explicit runner references were removed. True integration RED2/2 then demonstrated missing actual arena zones. Initial GREEN10/10 and original Play4/4 passed. A subsequent real-density RED1/1 found that the original64-collider overlap buffer truncated two actual139-part Heavy vehicles. An Idle-only opt-in capacity API now configures1024 reusable entries in the live adapter; the package default64 and original timing/damage algorithm remain unchanged. Final GREEN11/11 (8 original +3 integration) proves automatic warning/active damage, one part hit per vehicle, no truncation for both dense vehicles, pause and transition cleanup. Existing COV/R5 arena/run regression7/7 passes, including all three safe layouts and six-stage API reward/restart flow. Every final process exited0.

These are Editor API/physics fixtures. Actual Windows telegraph readability, driving avoidance and performance remain W7 closure gates with COV/VFX. Mounting, salvage and repairs are not applicable to an arena zone. Existing GUIDs, scenes, prefabs and packages were preserved.

**Decision: INTEGRATED, targeted tests PASS; BLOCKED pending W7 native Windows and full-wave closure.**

[Machine-readable cases](HAZ-001_test_summary.json). API fixtures are distinct from native input gameplay.

Integration [Draft PR #30](https://github.com/C8LyNn/RoboRougelike/pull/30); no shared-branch merge.

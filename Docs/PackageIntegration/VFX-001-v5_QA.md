# VFX-001-v5 — settled combat feedback

Pinned source PR#6 `43010f6ac3aecff24d2a84d116ee686faeecd667`, additive delta from `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`. Import preflight verified171 exact Git blobs and86 unique new GUIDs, no overwritten files. Integration source `e2dd53bef859e819cc3896748b3fd92286c12f70`, based on HAZ checkpoint `605adaf` / Draft#30. Index validation:1400 tracked files,0 errors. New metadata whitespace normalization retains every GUID. Existing GUIDs/scenes/prefabs/packages were preserved. Original imported Docs/VFX001 are historical source material; this report is the current integration evidence.

The original v5 pool/orchestrator and six resource materials/shaders now run in the real Gauntlet and range. Accepted P12 damage, designated core destruction, original rocket detonation after blast resolution, actual R3 fragment creation and accepted COV durability hits emit classified cosmetic events. No HP, damage resolver, force or solver was added. Optional camera/jolt/heat receivers are not bound to physical/control roots. The original48-active/12-per-frame/prewarm8 budget and core priority are retained.

The supplied bridge polled transient R6 sphere names and inferred explosion by size, causing muzzle flashes to become hits and missing nonlethal rocket explosions. Six real integration RED cases reproduced missing immediate hit feedback, false muzzle hits, wrong core feedback, missing cover/fragment events, stale round effects and absent rocket explosion classification. The bridge now observes settled notifications; legacy hit spheres are replaced, while muzzle flashes and module-loss notices remain. Cosmetic callbacks cannot interrupt original death or rocket cleanup. Leaving combat clears particles and old muzzle flashes; guarded Run.Start wiring installs one fresh bridge on every scene restart. Disabling/re-enabling detailed effects clears the pool and never replays history.

Source compatibility fixes were reproduced before correction: duplicate runner references in both test asmdefs; configuring a newly added ParticleSystem while already playing during restart; native MaterialPropertyBlock allocation in ArmorScar/Heat controller field initializers. Fixes remove only duplicate test runner refs, stop new particle systems before configuration, and lazily allocate the two affected property blocks. Core pooling/preset algorithms remain original.

Final targeted Edit61/61 =55 original +6 integration, original Play20/20, all native exit0. Shared R6/W6/W7/R5 regression initially73/74: one old test expected the replaced legacy sphere object. Its HP70 after30 damage, persistent tint, bounded/no-collider/expiry contract is retained against the actual v5 pool; corrected target1/1 passes. The final full W7 run will cover all shared cases together. Tests assert immediate single hit, unchanged body pose/mass/velocity, no visual colliders/bodies, distinct core/explosion presets, actual surviving-target rocket flight/damage, cover/fragment notifications, disabled-effects history, round cleanup and fresh restart subscription. These are Editor API/physics fixtures.

An explicit MOVSERIES_VISUAL_QA Windows fixture was added for COV/HAZ/VFX and is excluded from retail. Native fixture results and ordinary-input campaign/performance are not claimed yet. Mounting/salvage/repair are not applicable to cosmetic effects. Windows/full-wave/ZIP gates remain pending.

**Decision: INTEGRATED, targeted tests PASS; BLOCKED pending W7 native Windows and full-wave closure.**

[Machine-readable cases](VFX-001-v5_test_summary.json). API fixtures are distinct from native input gameplay.

Integration [Draft PR #31](https://github.com/C8LyNn/RoboRougelike/pull/31); no shared-branch merge.


W7 build source e2dd53bef859e819cc3896748b3fd92286c12f70: Windows builds succeeded0errors; native720 API/physics23/23 checks/errors[]/actualexit0. Full regression and ordinary-input campaign/visual/performance gates remain pending. See [current W7 wave QA](W7_WAVE_QA.md) for evidence, release status and API/gameplay distinction.

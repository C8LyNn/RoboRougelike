# W8 integration and Windows candidate QA

**Decision: READY for playable W8 RC1, with the unresolved automation-attached shutdown limitation.** COM-001, SEN-001 and MOV-011 are integrated and accessible in the game. W9 road/race/reactor packages remain unimported. No main/develop/W5 merge or Unity/package upgrade.

## Source and PR review

Frozen W5 PR20 head `2154b4341dd6b6fefb135187dc07b4b12d44f908` preserves W2 and final W3/W4 history. W6 RC2 gameplay source is `8cf4ff8c80178f04f8d60dc6f57d03a2e7f7f32f`. Stable W7 RC1 gameplay source is `e2dd53bef859e819cc3896748b3fd92286c12f70`, documentation checkpoint `04695f9abcfeb43b7bb84d37d9d1e2f7cc27af0a`.

Reviewed [Draft32 COM](https://github.com/C8LyNn/RoboRougelike/pull/32) at `af92a0725f5f7e940183c582b5b6b47eecae0821` and [Draft33 SEN](https://github.com/C8LyNn/RoboRougelike/pull/33) at `83b188071f3a692144aa40a266f93403f8ab6bf7`: both open, mergeable, with no review comments at inspection. They form a stack on W7, with no duplicate reimport. COM exposes visible structural HP in a 60m/85-degree cone; SEN reveals typed 20m signals through obstacles without HP. They share the owned-device/world boundary but retain their original clocks and distinct outputs.

[Draft34 MOV-011 and W8 closure](https://github.com/C8LyNn/RoboRougelike/pull/34) is stacked on SEN. Initial MOV source `f0ab1bd11f4474ede0250b344f7e2fef716c58f9`; final gameplay/build/full-tested/packaged source **`40173ee60a481c07ccc005fe94c517c524aef0fa`**. Subsequent commits change documentation/evidence only. Original source hashes and import/GUID manifests are in the three per-ID reports. Unity remains **6000.3.14f1**; existing metadata, scenes, prefabs and package configuration are retained.

MOV-011 computes original four-corner steering angles once within the existing R6 step. R6 remains the sole force solver. Exactly four unique rectangular unrotated nominal wheel positions are supported; Light is supported, Standard/Heavy twelve-wheel and irregular/rotated layouts visibly retain ordinary R6. Lost wheels retain nominal roles. There is one HP, Rigidbody, mount, salvage and repair authority. M selects front/rear/counter-phase/crab; identity and mode survive canonical part copying and repair.

## Verified gates

| Gate | Actual result | Evidence under Artifacts/Local |
|---|---|---|
| COM original and initial integration | Edit62/62, Play22/22, shared10/10; native exits0 | COM-001 per-ID report |
| SEN original and integration | Edit16/16, Play3/3, COM7/7, GUI1/1; native exits0 | SEN-001 per-ID report |
| MOV original and initial integration | Edit55/55, original Play14/14, shared34/34; native exits0 | MOV-011 per-ID report |
| Final physical W8 fixes/composition | 21/21 = 6 COM + 7 SEN + 8 MOV, native0 | W8 physical green d86e2f6d; composition9255313f |
| Full EditMode | **976/976 PASS**, failed0/skipped0/native0 | 20261024T094131Z-0da4d1c3c0724002a092708735efc426 |
| Full PlayMode | **398/398 PASS**, failed0/skipped0/native0 | 20261024T101056Z-9f8f054a53eb48a1a1d8734aec1dbec9 |
| Windows x64 retail/QA builds | Both Succeeded,0errors/4 existing warnings, native0; Unity totalbytes103266335/103339265 | 20261024T093729Z-206a18fed60545eba53ecd71d7784242 |
| Native API/physics1280x720 | **47/47**, errors[], actual process0; D3D11/RTX5080, no Sky attached | W8-Native-API-6ef87d4ecc68410a9b49fd892da0b21b |
| Native UI1920x1080 range | Three actual surface mounts,600→485scrap/20→32cells; M4modes, K10HPcontacts, L1typed signal, pause guard, actual AI rocket damage80→52HP, reset and garage return | W8-Native-Input-f601dc760366409189bccd4a2a61ff4c |
| Native1920x1080 full campaign | **6/6 wins,15 foes,6 bonuses,final victory and restart**, observer errors[]; actual menus/routes/rewards/garage controls, ordinary combat-input pilot | W8-Native-Input-c6cbba340d744db48a00cb143a6de3b3 |
| Extracted retail1280x720 | Exact extracted EXE menu→Light garage→MOV purchase/mount→206kg four-wheel range→M front/rear HUD | W8-Native-Retail-b9e663d6dbac4db0bc499a4d21fb6037 |
| ZIP/CRC/extraction/dependencies |203 runtime files; every fresh extracted hash matches; six QA classes absent retail DLL | Docs/Evidence/W8/release-package.json and retail-qa-exclusion.json |

Counts refer to separate runs and are not summed as unique coverage. Complete Unity tests ran in the local licensed Editor. The built and full-tested Assets trees are identical; later documentation does not require another build. No successful cloud Unity CI run is claimed. The four existing CS0414 warnings concern unused prototype input fields in MOV004/006/009.

The native47 fixture uses controlled API placement/damage and real physics. It proves actual scanner LOS/pulse behavior, canonical cooldown and retained COM reactivation, EMP without erased momentum, destruction/Capture/paid repair/removal/remount, mode persistence and workshop isolation. Default planar R6 and actual MOV009 four-support/gravity/unconstrained-body composition are separate cases. The MOV009-supported crab moves(.76,0,1.78)m with approximately.0001819-degree yaw and2766.4N lift. Both rendered crab HUD screenshots were inspected. The earlier36-check fixture proved planar motion only; its inaccurate suspension label is corrected and superseded.

## Native gameplay and performance

The uninterrupted Heavy/seed2026 route was FirstScrap→OpenConvoy→IndustrialCrossfire→EliteBastion→RelicRaiders→SiegeBoss, covering all three existing layouts. Native window clicks controlled vehicle choice, routes, bonus/loot selection, paid repairs, purchases, physical mounting, final confirmation and restart. Combat used the existing ordinary virtual aim/fire/driving input pilot with active AI and physical shells. No HP, currency, reward or victory API was used. This is **input-pilot gameplay, not six human-driven fights**; the separate47-check fixture remains API/physics testing.

Recovered one destroyed automatic gun at0HP, paid36scrap to rebuild it and mounted it onto the physically installed core bonus. Occupied cells139→143→144, core180→270HP and two working guns prove activation. Bulk repairs13/6/14/16/39scrap persisted actual condition between fights. SEN was bought/mounted after fight1 for30scrap (144→148cells), COM after fight2 for40scrap (148→152). Actual L during fight2 showed9typed signals, including two hazards and a wreck. Actual K during fight5 showed9structural parts, including armor93%HP, with6.6s cooldown visible. Earlier K attempts without a visible cone target left it ready. Both scanners survived subsequent transitions. Other acquired bonuses remained inactive in inventory.

The native final screen records **6/6 wins,15 defeated foes,6 bonuses,1 recovered module,704scrap and143 logical parts**. Actual restart returned to menu,0scrap/0inventory/0bonuses, one controller and one camera. Observers reported errors[]; Player.log contains no managed exception/error/assertion. Sparks, smoke, physical fragments and yellow/red hazard telegraphs were observed during active enemy fire. [Final victory](../Evidence/W8/native-final-victory.png), [operator results](../Evidence/W8/native-1080-operator-result.json), [native observations](../Evidence/W8/native-campaign-observations.json).

1920×1080, D3D12/RTX5080, Ryzen9 9950X,60FPS cap, no Editor running. Combat116.594s,1227physical rounds, peak21shells/12debris; peak269parts/4foes,171.22MiB allocated, longest transition70.19ms. Worst per-stage **p95 16.672ms, p99 16.854ms, maximum17.142ms**. The prior W7 frame outliers did not recur in this single run; their root cause is not established and no fix is claimed. These measurements include QA observers/captures and do not establish performance on weaker hardware. [Full per-stage diagnostics](../Evidence/W8/native-1080-diagnostics.json), [performance summary](../Evidence/W8/native-performance-summary.json).

## Corrections and limitations

One whole-W8 review found no Critical issue. Fixed the missing MOV mode HUD without an unrelated MOVSeries adapter, corrected the misleading native suspension evidence, and reproduced/fixed retained COM view cooldown resynchronization (RED expected7.69969/actual7.99975, then targeted/full/native GREEN). Original MOV rear-angle assertion and colliderless demo-test inertia were corrected in test fixtures; production steering mathematics and demo force code were unchanged. [Review findings and rulings](W8_REVIEW.md).

**Automation-attached shutdown C0000005 remains unresolved.** Both actual extracted-retail Sky/Alt+F4 and the completed campaign's post-restart Sky/Alt+F4 returned -1073741819. The latter was independently confirmed using two retained process handles. The separate no-Sky47-check process exited0. Earlier human R6/W2/W5 closes and existing accessibility/empty-scene evidence support an automation/Unity lifetime limitation; they do not establish a W8 human-close PASS or a proven game-code fix. The previous input run ended after3/6 wins during an interruption: it is explicitly partial, not a full victory or an established in-game crash. No errors were reported by its gameplay observer. The menu still carries the historical W6 cosmetic label; the archive manifest identifies W8 RC1.

## Candidate

`C:\Users\kubap\Desktop\Prototypy\Releases\RoboRoguelike_W8_RC1_Windows_x64.zip`

**38,482,493 bytes**, SHA256 **`2d40f42e223580b8dda84ffb5b57af6bdb53ab016b2915e01754b4624a83da6b`**,203 runtime files. Complete EXE, Unity data, Mono and DLL dependencies are included. Exact extraction and actual extracted retail launch were verified. [Package proof](../Evidence/W8/release-package.json), [full tests](../Evidence/W8/full-test-summary.json), [retail observations](../Evidence/W8/native-retail-observations.json).

W7 RC1 remains **38,447,241 bytes**, SHA256 `41de715507c1493fc9ef5bbcce3fe3a772305641dd7935b6b84639650608d5d5`, independently preserved and freshly hash-checked. No shared-branch merge.

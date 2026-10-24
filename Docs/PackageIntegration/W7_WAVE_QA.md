# W7 wave release QA

**Decision: READY for playable RC, with the unresolved automation shutdown and measured frame outliers below.** COV-001, HAZ-001 and VFX-001-v5 run in the real Gauntlet. W8/W9 remain separate, unimported work. No main/develop/W5 merge.

Frozen W5 `2154b4341dd6b6fefb135187dc07b4b12d44f908`. W6 RC2 `8cf4ff8c80178f04f8d60dc6f57d03a2e7f7f32f`. COV source integration `871a6dbb257480d3365f3df82923fa531c3200e1`, HAZ final targeted source `32653684fd208be11bb0344f7c9a353791ef2deb`, final W7 gameplay/build/ZIP source `e2dd53bef859e819cc3896748b3fd92286c12f70`. Subsequent commits contain reports only. Drafts [#29](https://github.com/C8LyNn/RoboRougelike/pull/29), [#30](https://github.com/C8LyNn/RoboRougelike/pull/30), [#31](https://github.com/C8LyNn/RoboRougelike/pull/31) are stacked. Unity6000.3.14f1/packages retained; no existing GUID/scene/prefab overwritten. Source SHAs and import/GUID evidence are in each package report.

| Gate | Actual result | Local evidence under Artifacts/Local |
|---|---|---|
| COV original/integration | Edit9/9, Play2/2, arena/run5/5; exit0 | Per-package report |
| HAZ original/integration | Edit11/11, Play4/4, COV/arena/run7/7; exit0 | Per-package report |
| VFX original/integration | Edit61/61 (55 original+6 integration), Play20/20; exit0 | Per-package report |
| Shared regression | Initial73/74; obsolete sphere assertion adapted to actual pooled visual contract, corrected target1/1 exit0; full suite below covers it | W7-shared-regression-1a811e70294244ed8d2786c7997a4239; W7-feedback-contract-green-ba130eca7f90496d8034910f6921bd2e |
| Fresh full EditMode | **841/841 PASS**, native0 | 20261024T023411Z-8f62d2b8147b4cbc8f6adff9d8a0087b |
| Fresh full PlayMode | **359/359 PASS**, native0 | 20261024T030116Z-c71b3239107b4b9b94d44f9329539710 |
| Repository/GUID/config index | 1400 tracked files,0errors at source checkpoint | Validator at source commit |
| Windows x64 builds | Retail103189170 /QA103247099 Unity totalbytes; both Succeeded,0errors/4 existing warnings, native0 | 20261024T023223Z-a39428f558cd4f4693be8dc3a2945701 |
| Windows720 API/physics | 23/23 checks, errors[], actual exit0; D3D11 RTX5080 | W7-Native-API-720-726ccc4618094057aa94040c23ea3373 |
| Windows1080 ordinary-input campaign | **6/6 victories,15 foes,6 bonuses,final screen+restart**, errors[] | W7-Native-1080-fe338affb01f4b06b0ed8385e48b7aab |
| Extracted retail720 | Actual menu→garage→range, native mouse shot/cooldown observed; no QA classes/flags/pilot | W7-Extracted-Retail-c79ec76a4ef843829a996d1027f1269c |
| ZIP/CRC/full extraction hashes | PASS199 runtime files; actual extracted EXE launched | W7-RC1-package.json |

Counts describe distinct runs, not summed unique coverage. Builds ran before the final full suite to catch native/rendering defects early; the Assets tree is identical to the full-tested source. Documentation updates required no rebuild/retest. Local licensed Editor supplied the evidence; no successful cloud Unity run is claimed.

## Native gameplay

Native screenshots/mouse operated vehicle choice, routes, rewards, garage purchases/repairs/mounts, final confirmation and restart. Combat used the existing ordinary virtual aim/fire/driving input pilot with active AI and physical shots. It used **no HP, currency, reward or victory APIs**. This is not a claim of six fights driven by a human. The separate23-check Windows fixture uses controlled placement/damage APIs and actual P17/rocket flight and is explicitly not manual gameplay.

Heavy/seed2026: Pierwszy złom→Otwarty konwój→Ogień krzyżowy→Elita:Bastion→Łowcy reliktów→OBLĘŻNIK. All three existing layouts ran with new covers/hazards. Result15 defeated foes,1 recovered module,6 bonuses,770 final scrap,141 physical logical parts/144 occupied cells. The destroyed enemy automatic gun was rebuilt0→100%HP for36scrap and mounted onto the physical core bonus. Core180→270HP, two working weapons and unchanged inventory bonuses proved activation/mounting/persistence. Paid bulk repairs6 and86scrap restored actual condition before later fights; rewards supplied all currency. Other bonuses remained inactive in inventory. Final victory and restart returned to one game controller and one camera.

Combat: **135.944s**,1439 physical shells, peak23 shells/12 debris. Native visuals show layered spark/flash/smoke/fragments and readable yellow/red hazard rings; garage transitions leave no frozen old muzzle effects. [Actual destruction and active hazards](../Evidence/W7/native-destruction-and-active-hazards.png), [final victory](../Evidence/W7/native-final-victory.png). Native API additionally proves actual cover50→10 P17 impact/break/reset, automatic warning→one5HP part hit/pause, original core and nonlethal rocket explosion presets, no cosmetic colliders/bodies/physics changes, disabled-effects history and fresh restart subscription. Scheduled API transient filenames alone are not used as proof of a fleeting rendered effect.

## Performance and limits

1920×1080, Direct3D12 / NVIDIA GeForce RTX 5080, AMD Ryzen 9 9950X 16-Core Processor ,60FPS cap, no Editor CI running. Worst per-stage p95 **16.673ms**, p99 **16.821ms**; peak267 parts/4 simultaneous foes, 170.67MiB allocated, longest transition 68.97ms. There was a **114.30ms first-stage outlier** and40.95ms boss-stage maximum. Their cause is not established and they are not claimed fixed; remaining stage maxima16.78–17.02ms. Measurements include native QA observers and screenshot capture and do not establish performance on weaker hardware. Complete per-stage metrics are in [diagnostics](../Evidence/W7/native-1080-diagnostics.json).

**C0000005 remains unresolved.** Both client-attached Alt+F4 closes returned−1073741819 after otherwise successful gameplay. The no-Sky23-check process exited0. Existing W5 empty-scene/accessibility evidence and previous human R6/W2/W5 closes support an automation/Unity lifetime limitation; there is no proven game-code fix or fresh W7 human close result. No in-game exception or transition hang occurred. The menu still displays its previousW6 label (cosmetic); the candidate identity and exact source are in the archive manifest.

## Corrections and candidate

Reproduced/fixed: COV Editor missing#endif; HAZ/VFX duplicate runner references; HAZ64-collider truncation with two139-part Heavy vehicles (live opt-in1024 reused entries); VFX marker polling/muzzle misclassification, missing settled cover/rocket/fragment events and restart/transition cleanup; particle configuration while already playing; two native property-block field initializers. Existing HP/damage/mount/salvage/physics remain authoritative. Details and RED→GREEN cases are in the per-ID reports.

`C:\Users\kubap\Desktop\Prototypy\Releases\RoboRoguelike_W7_RC1_Windows_x64.zip`

**38,447,241bytes**, SHA256 `41de715507c1493fc9ef5bbcce3fe3a772305641dd7935b6b84639650608d5d5`, source `e2dd53bef859e819cc3896748b3fd92286c12f70`. CRC, all extracted file hashes, complete EXE/data/Mono/DLL dependencies and actual extracted launch PASS. [Package proof](../Evidence/W7/release-package.json), [retail observations](../Evidence/W7/native-retail-observations.json), [full test summary](../Evidence/W7/full-test-summary.json). W6 RC2 stays preserved independently. Earlier frozen per-device branches rely on this tested aggregate for shared closure fixes; no shared merge was performed.

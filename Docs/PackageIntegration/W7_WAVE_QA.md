# W7 wave release QA — IN PROGRESS

**Decision: BLOCKED pending the running full regression and native input gameplay/performance.** COV-001, HAZ-001 and VFX-001-v5 are connected to the real game; targeted and Windows API/physics gates below passed. W8/W9 are not imported. No shared-branch merge.

Frozen W5 `2154b4341dd6b6fefb135187dc07b4b12d44f908`. W6 RC2 source `8cf4ff8c80178f04f8d60dc6f57d03a2e7f7f32f`. COV integration `871a6dbb257480d3365f3df82923fa531c3200e1`, HAZ final targeted source `32653684fd208be11bb0344f7c9a353791ef2deb`, W7 build/gameplay source `e2dd53bef859e819cc3896748b3fd92286c12f70`. Drafts [#29](https://github.com/C8LyNn/RoboRougelike/pull/29), [#30](https://github.com/C8LyNn/RoboRougelike/pull/30), [#31](https://github.com/C8LyNn/RoboRougelike/pull/31) are stacked. Unity6000.3.14f1 and package versions retained; no existing GUID/scene/prefab overwritten.

| Gate | Actual result | Local evidence under Artifacts/Local |
|---|---|---|
| COV original/integration | Edit9/9, original Play2/2, arena/run5/5; exit0 | Per-package report |
| HAZ original/integration | Edit11/11, original Play4/4, COV/arena/run7/7; exit0 | Per-package report |
| VFX original/integration | Edit61/61 (55 original+6 integration), original Play20/20; exit0 | Per-package report |
| Shared regression | Initial73/74; old sphere-specific assertion adapted to new bounded pool with HP/tint/collision/expiry checks retained, corrected target1/1 exit0 | W7-shared-regression-1a811e70294244ed8d2786c7997a4239; W7-feedback-contract-green-ba130eca7f90496d8034910f6921bd2e |
| Repository/GUID/config index | 1400 tracked files,0errors | Validator at source checkpoint |
| Windows x64 builds | Retail103189170 bytes / QA103247099 bytes; both Succeeded,0errors/4 existing warnings, native0 | 20261024T023223Z-a39428f558cd4f4693be8dc3a2945701 |
| Windows720 API/physics | 23/23 checks, errors[], native exit0; D3D11 RTX5080 | W7-Native-API-720-726ccc4618094057aa94040c23ea3373 |
| Full EditMode | RUNNING — no final result claimed | 20261024T023411Z-8f62d2b8147b4cbc8f6adff9d8a0087b |
| Full PlayMode | Queued after full EditMode | Read actual subsequent run directory when complete |
| Native input campaign /1080p performance | NOT RUN yet | Pending full regression |
| ZIP/CRC/full extraction hash | PASS199 runtime files; extracted EXE not launched yet | W7-RC1-package.json |

The23 Windows checks used controlled placement, damage APIs and actual P17/rocket flight; **this is not manual or ordinary-input campaign gameplay**. They verified six real yard covers; P17 contact50→10 durability, break/reset collision and renderer; yellow harmless telegraph, automatic5HP active hit, pause guard; immediate single VFX hit without mass/pose/velocity changes, no cosmetic physics, no muzzle misclassification, designated core preset, nonlethal original rocket440.3187HP/explosion preset, disabled-effects history, reward cleanup and fresh restart subscription. The720 warning image shows a readable yellow ring. Scheduled transient capture filenames are not independent proof that a fleeting core/rocket effect was visible in the captured frame; visual input validation remains pending.

Source defects were reproduced before correction: COV Editor builder missing#endif; HAZ/VFX duplicate test runner references; HAZ64-collider buffer truncating two139-part Heavy vehicles; VFX marker polling/classification/restart/round cleanup; particle initialization while already playing; two native property-block field initializers. Full details and RED→GREEN evidence are in each package report.

Prepared, **not release READY yet**: `C:\Users\kubap\Desktop\Prototypy\Releases\RoboRoguelike_W7_RC1_Windows_x64.zip`,38,447,241bytes, SHA256 `41de715507c1493fc9ef5bbcce3fe3a772305641dd7935b6b84639650608d5d5`. Source `e2dd53bef859e819cc3896748b3fd92286c12f70`. CRC and every extracted file hash match. Existing W6 RC2 remains available independently.

Known Unity/UI Automation shutdown C0000005 is **unresolved**. This new no-Sky native API process exited0; that does not establish a fix for client-attached shutdown. A fresh W7 human Alt+F4 success is not claimed.

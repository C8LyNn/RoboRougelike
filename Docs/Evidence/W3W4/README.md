# W3/W4 native evidence

Tested game source: `5d4b61d1160e9cceac888e070147aead71dfb92b`.

Final Unity gates: 434/434 EditMode and 176/176 PlayMode passed, no skips, native exit 0. Both post-regression Windows builds succeeded with zero errors. Their managed-assembly hashes match the native-tested binaries exactly; counts, build summaries and protected configuration hashes are in the JSON below.

[`native-results.json`](native-results.json) contains sanitized outcomes and lifecycle samples from three Windows QA runs. It excludes raw logs, machine paths and licensing information. Both graphical lifecycle fixtures passed 29 assertions; the full-inventory fixture passed 8. Fixture victories/module loss are seeded with damage APIs. Operator reward selection, XYZ rotation and the first mount are real UI actions. Inventory recovery is entirely operated through the new UI.

The two images named `actual-combat-*` come from a separate active-AI input pilot, with physical projectiles and ordinary control input, without damage/reward/currency APIs. They show the first victory's one-bonus-plus-three-parts selection and the mounted hull module. The pilot won four encounters and mounted four earned modules, then became blocked by a wall during encounter five and was closed without final victory. Sanitized observations preserve that limit. Automated fixture screenshots must not be interpreted as manual combat wins.

| Evidence | Image |
|---|---|
| One bonus plus three independently selected parts, 1280x720 | [Rewards](rewards-720.png) |
| Rotated green footprint and physical mounting ports, 1280x720 | [Garage 720](garage-720-rotated.png) |
| Same preview and usable controls, 1920x1080 | [Garage 1080](garage-1080-rotated.png) |
| Full owned inventory: first click asks for confirmation, count stays 200 | [Confirmation](capacity-confirmation-720.png) |
| After four confirmed disposals: selection preserved, count 196, settlement enabled | [Retained selection](capacity-retained-selection-720.png) |
| Rewards after active-AI combat | [Actual combat rewards](actual-combat-rewards-720.png) |
| Earned hull module mounted after actual combat | [Actual combat module](actual-combat-mounted-hull-720.png) |

Known limitation: automation-driven close returns `C0000005` after the successful scenarios. Gameplay assertions and process-exit outcomes are recorded separately. A normal human close of this W3/W4 build is not claimed as verified.

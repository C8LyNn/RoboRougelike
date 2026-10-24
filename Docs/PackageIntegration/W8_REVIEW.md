# W8 integration review

One fresh read-only review covered frozen W7 `04695f9abcfeb43b7bb84d37d9d1e2f7cc27af0a` through W8 source `f0ab1bd11f4474ede0250b344f7e2fef716c58f9`. No reviewer modified the checkout or ran Unity. The current user request closes W8; W9 remains unimported. There was no second review.

The reviewer confirmed canonical R3/P12/R6 ownership/HP/physics, bounded read-only sensor markers, distinct COM LOS/HP and SEN area signals, and original MOV011 geometry consumed by the existing R6 force path. No Critical ownership, damage, duplicated solver or cleanup issue was found.

| Finding | Effect | Resolution / evidence |
|---|---|---|
| Important: MOV011 HUD required the unrelated MOVSeriesVehicleAdapter | A Light carrying only W8 devices could change modes without seeing the selected mode or unsupported-layout reason | Reproduced in initial native screenshots. Status now renders independently of the old adapter. Final native6ef87d4e screenshots03/05 visibly show the M/Crab status; ordinary-input verification remains a separate closure gate. |
| Important: native fixture called planar driving "normal suspension" | Its first passing motion assertion did not establish MOV009 compatibility | The initial36-check result remains preserved and is interpreted as default-planar R6 only. Added an explicit planar assertion and a separate four-support MOV009/gravity/unconstrained-body native case. Editor composition run9255313f passed8/8; final native6ef87d4e passed47/47 including actual supported crab travel(.76,0,1.78)m/yaw.0001819°/lift2766.4N. |
| Minor functional bug: retained COM view did not resynchronize canonical deadline on reactivation | Disabled view resumed an old remaining cooldown although R3 DeviceReadyAt had advanced | Actual RED d5290f19: expected7.69969s, actual7.99975s. Added narrow OnEnable restoration matching the existing owned deadline; final physical suite d86e2f6d passed21/21 including this regression. Final native47/47 also verifies reactivation. Addressed under the user's request to repair detected bugs, rather than deferred as cosmetic work. |

## Review boundaries and rulings

- W9 roads, racing and reactor/objective routing were not judged: no W9 implementation is in this range, and the current request is W8 only. Cost: their integration remains a later task.
- Imported standalone demos and editor-only free-scanner tooling are optional reference/test material and are not installed in the production player flow. Their presence does not establish gameplay readiness; real device/R6/native gates remain required. Cost: their optional editor interactions are not a player acceptance claim.
- Source-component-only enable/disable manipulation has no production path here. The original source reset contract remains; production lifecycle uses the physical binding and canonical state. Cost: future custom scripts toggling only ScannerPulse.enabled must explicitly honor that boundary.
- The read-only reviewer did not certify native visual quality, full-suite results, performance or packaging. The executor must supply actual commands/process exits/results separately. Cost: targeted review alone cannot produce READY.
- The existing four-role original package supports exactly four rectangular, unrotated nominal wheel mounts. Other layouts retain visible standard R6 fallback; destruction never remaps remaining roles. Cost: advanced steering for12-wheel or irregular layouts is not provided.
- New mesh pivots reuse existing wheel meshes and keep original HP blocks/colliders. Source steering supplies angles only; R6 applies the one existing force/torque path. Cost: visual alignment and real motion require native inspection.
- Supplied COM production profile60m/85° remains distinct from source defaults24m/75°. SEN keeps original20m/5s/3s/128-budget logic, using one layer2 non-solid trigger per actual world actor. Garage prices/masses are documented integration tuning. Cost: balance tuning is not original-package calibration.
- The original MOV011 rear-angle assertion confused axle angle with driver input; corrected the assertion without changing steering mathematics. The original250kg colliderless demo fixture had yaw inertia1 and unstable grip feedback; added its supplied hull geometry to the test fixture. Cost: neither correction substitutes for actual R6/MOV009 motion tests.

No cosmetic Minor was deferred by this review. The functional COM issue enters the same single fix pass because the user explicitly requested detected bug repairs. The overall W6–W9 scratch plan is retained for its unfinished future tasks; this report does not declare W9 complete or delete its evidence.

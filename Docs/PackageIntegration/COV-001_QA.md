# COV-001 — original destructible cover integration

Pinned source PR#16: `b2d22c4f7ed7dd9e07fec0fa6137272c541138a6`, additive delta from `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`. Import preflight verified25 exact Git blobs and15 unique incoming GUIDs with no overwritten files. Runtime integration commit: `871a6dbb257480d3365f3df82923fa531c3200e1`, on W6 aggregate `5a466bf8f5ddbc926562d1fcac86a5bd4d795aa2`.

Actual salvage-yard covers, wasteland outcrops and industrial connectors use the original50-durability COV001 component. Border walls and large industrial halls retain their existing collision. The existing W6 RRLHitRouter resolves real collisions to the cover adapter without a second vehicle HP pool. Original phase transitions disable the actual blocker once; the adapter tints damaged/critical surfaces and hides a broken mesh. Reset/rebuilding restores the view and blocker. The existing rocket receiver contract is attached for blast routing; beam/rail/saw already use the same hit boundary. No new model, solver, scene or prefab was created.

Initial source compilation failed CS1027: the supplied Editor demo builder opened UNITY_EDITOR without its closing #endif. Only the missing directive was appended; original runtime C# is unchanged. Source metadata whitespace was normalized without changing GUIDs; original README's intentional Markdown hard break is preserved.

RED integration2/2 observed no real arena cover binding. GREEN Edit9/9 (seven original plus two integration): actual P17 damages exactly the contacted cover, nearby covers remain untouched, original durability has no P12 mirror, broken collider/renderer turn off and reset/rebuild restore them. Original Play2/2 verifies real blocker restoration and no double break. Existing arena/run regression5/5 verifies all three safe layouts, industrial AI traversal and six-stage API/reward/restart flow. Every final test process exited0. These are Editor API/physics fixtures, not Windows native gameplay.

Module mounting, salvage and repair are not applicable to an arena obstacle. Source and gameplay integration are complete; native Windows and full regression execute once at W7 closure with HAZ/VFX. The frozen W6 RC2 ZIP remains available independently.

**Decision: INTEGRATED, targeted tests PASS; BLOCKED pending W7 native Windows and full-wave closure.**

[Machine-readable cases](COV-001_test_summary.json). API fixtures are distinct from native input gameplay.

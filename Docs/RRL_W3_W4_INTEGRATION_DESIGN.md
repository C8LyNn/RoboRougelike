# W3 + W4 integration design

The user requests autonomous implementation and QA from latest verified W2, importing actual PR17/18 code and preserving newer W2 fixes. New integration branch only; Draft PR targets the W2 branch. No merge or edits to source PRs/main/develop.

## Inputs and selected approach
- W2 base: a0f0a6590b07ed2f5007361a7e761f133e52ff99.
- W3 PR17 head:08503536c883f95dcdc927dca390b643eb695c8f.
- W4 PR18 head:25a3c387a56fd31842e18cb18b482aedef66c501, includes W3 ancestry.
- Merge W4 once into isolated W2 checkout; this preserves both PR histories and their real code. Replaying every intermediate commit creates avoidable conflicts; replacing whole W2 files would discard verified fixes.

## Reward transaction
R4RewardSettlement.TryClaim remains the only live transaction boundary. Exactly one of three offered upgrades becomes one inactive BonusModule inventory item; independently0–3 real enemy parts. All preconditions, IDs, finite HP/effect amounts and inventory capacity must validate before mutation; failure/replay changes no state, money, stats or inventory. Apply salvage, generated bonus and progress once. Final victory also offers three upgrades and resolves one bonus+0–3 parts to Won. Armor/Core remain unselectable. Progress.Stats is the unmodified baseline, not a reward accumulator.

## Physical identity, orientation and connectivity
Use imported R3PartState/R3MountingGrid: one ID/HP, four rotated cells for1high×2wide×2long, normalized90degree XYZ orientation and reciprocal opposing face ports. Single logical parts keep their identity/rotation through inventory/repair/remove/remount. Placement, ghost and P12 share the same occupancy/port semantics; reject disconnected islands, overlaps and bounds violations before payment. Existing180cell budget and weapon/device limits remain.

P12 remains the sole integrity graph. Test the imported footprint socket representation against destruction, rotation and chunk detachment before choosing the smallest repair. Any helper cell must disappear with its physical owner and must never remain as an indestructible connector. If necessary P12 maps several cells to one real block and deduplicates physical processing; no second integrity graph.

## Effects and devices
Derive active multipliers from mounted, alive, core-connected modules against the original baseline. Refresh on mounting, loss, destruction and rebuild; no persistent reward effects or repeated stacking. Propagate to live weapon damage/interval, mobility, hull/core HP and MOV device adapters. HP modifier changes preserve damage ratios and do not heal on rebuild. Repair modules affect only live connected parts during simulation.

## Garage and QA
Q/E rotateY, R/F rotateX, Z/X rotateZ by90degrees. Mesh/collider, occupied cells, contacts and directional payload agree. Show rotated footprint, port compatibility and actionable placement errors; damaged/0HP inventory preview must match actual mount eligibility. Preserve W2 arena hiding, framing, pointer/camera controls and all repair quotes.

Compile with exactUnity6000.3.14f1. Keep every existing regression case, adapt only superseded reward semantics, import preliminary W3/W4 tests into Assets/Tests/Editor. Full EditMode/PlayMode/retail+QA Windows build, real scene/projectile/graph/device tests, and native UI/gameplay1280×720+1920×1080. Distinguish fixture/API setup, combat input pilot and manual operator evidence. Raw evidence ignoredArtifacts/Local; reportDocs/RRL_W3_W4_INTEGRATION_QA.md. Cloud activation remains an external gate; do not bypass licensing or change engine.

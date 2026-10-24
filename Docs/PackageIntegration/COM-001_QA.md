# COM-001 — physical structural scanner

Pinned source PR#4 `cc9fbfbf895a2285a536ecfe70b9557c04921488`,51 exact additive Git blobs/24 unique new GUIDs, no overwritten files. Final targeted integration source `de07f62a9a2492da4a2aadd409cdbe4cb8cc9d6b`, based on W7 aggregate/report head `04695f9abcfeb43b7bb84d37d9d1e2f7cc27af0a`. Index validation1476 files/0errors. New metadata whitespace normalization retains GUIDs; old GUIDs/scenes/prefabs/packages unchanged. Imported Docs/COM001 are historical source reference; this report is current evidence.

COM and SEN share IRRLScanDevice, RRLDeviceBinding physical availability, activation scope and player controls. They keep distinct typed results: COM reports visible foreign structural HP/function snapshots; SEN will report area signal kinds without structural HP. SEN is not imported in this checkpoint.

COM is now an owned garage part, enum appended after existing kinds, four occupied cells with existing XYZ/contact geometry, one P12 HP component, existing R3 identity/condition/repair/DeepCopy and R6 mass. The paged shop exposes it; K uses ordinary InputSystem and the same guarded GUI activation fallback. The HUD shows original typed contacts and remaining snapshot lifetime. There is no free auto-installed scanner on the vehicle root and no second vehicle HP/solver. Source structural importance remains0; no graph analysis is invented.

Profile60m/85degrees comes from the supplied PR4 production bridge; package defaults remain24m/75degrees. Original8s cooldown,5s snapshot,16 contacts,512 registry budget and0 activation energy remain. Garage40scrap/16kg/100HP/one per kind are integration tuning, not package-defined values. Original controller Update is the sole scan clock. A minimal restore-cooldown API reconnects the existing clock to canonical R3 DeviceReadyAt after rebuild; only successful scans commit it. Failed/empty scans and cooldown retry preserve original semantics/snapshot.

The explicit activation-time registry uses real living Encounter enemies and the existing training NPC, with no per-frame scene search. Pinned live part adapters now reject an independent released Rigidbody even under the old hierarchy and read actual existing EMP/module status. These changes are read-only; tests reproduced both incorrect attached/function reports before correction. The shared device boundary separates hardware availability from active input availability so pause freezes a valid scan snapshot while destruction/EMP/encounter exit clears results. Existing weapon input/EMP behavior remains unchanged.

True physical RED1/1 at629e9372 demonstrated missing actual garage module; adapter RED2/2 at743265c3 demonstrated released-body/EMP status errors. Intermediate core60/62 included the deliberately missing input path and a fixture WaitForSeconds issue; the input path was implemented after its observed failure. Final62/62 Edit =55 original +5 physical/input +2 read-only adapter. Cases cover real player mounting/scanning, foreign target/LOS blocker, unchanged HP/mass/pose/velocity, snapshot-preserving cooldown retry, pause, EMP clear/automatic recovery without free activation, destroyed module, paid repair/rebuild/identity/cooldown persistence and GUI K owner/pause/loss guards. Original Play22/22 passes. Shared10/10 covers all integrated weapon EMP gates, normal/GUI reload and seven XYZ garage cases. All final processes exit0.

Initial test-harness attempts failed during domain reload because a captured local enum closure in UnitySetUp was lost; moving it to the fixture field produced the actual feature RED. These setup failures are not counted as mechanic failures. The existing COV control1/1 passed unchanged. The recovery fixture uses an explicit Time.time loop in Editor mode; original EMP runtime was not changed.

The targeted results above are Editor API/physics/input fixtures. Native and full-wave closure is recorded below; the verified W7 RC1 ZIP remains independent.

**Decision: READY in the tested W8 RC1 aggregate, with the unresolved automation-attached C0000005 shutdown limitation.**

[Machine-readable cases](COM-001_test_summary.json). API fixtures are distinct from native input gameplay.

Integration [Draft PR #32](https://github.com/C8LyNn/RoboRougelike/pull/32); no shared-branch merge.

## Final W8 aggregate closure

Final gameplay/build source `40173ee60a481c07ccc005fe94c517c524aef0fa` passes full976/976 EditMode and398/398 PlayMode, native exits0. The final Windows API/physics fixture passes47/47 with actual process0 and no Sky attached. Native UI verifies physical purchases/mounts, K/L/M controls and pause; the separate ordinary combat-input pilot completes all6 stages/15 foes, actual rewards/paid repair/garage assembly/final victory and restart. Actual extracted retail launches at720p and the input campaign runs at1080p. These methods are distinct, not a claim of six human-driven fights. Earlier frozen per-ID PRs rely on final Draft34 for the shared COM reactivation fix and aggregate closure.

The complete203-file W8 RC1 ZIP is38,482,493bytes with SHA256 `2d40f42e223580b8dda84ffb5b57af6bdb53ab016b2915e01754b4624a83da6b`; CRC/all extracted hashes and actual extracted launch verified. Sky-attached shutdown still returnsC0000005; no game-code fix or fresh human W8 close PASS is claimed. W7 RC1 is unchanged. [Full W8 wave report, performance, limits and evidence](W8_WAVE_QA.md). No main/develop/W5 merge.

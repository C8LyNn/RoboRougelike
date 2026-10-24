# SEN-001 — physical pulse scanner

Pinned source PR#15 `f70dc1612ef5293a5f6592dee0024f998bdb7239`,22 exact additive blobs/12 unique incoming GUIDs; no overwrites. Integration code `02e39111514068219745cd31a918caed8192ab7e`, based on COM final head `af92a0725f5f7e940183c582b5b6b47eecae0821`. Repository index1506 files/0errors. New metadata whitespace normalization preserves GUIDs; scenes/prefabs/Unity/packages unchanged. Imported source QA documents are historical; this report records current evidence.

The owned garage module occupies the existing four-cell1×2×2 footprint with XYZ rotation/contact geometry, canonical R3 identity/condition/repair/loot/copy, one P12 HP and existing R6 mass. It is exposed in the paged shop, costs30scrap, weighs12kg, has100HP and a one-per-kind limit; these are integration tuning. Original pulse20m/5s cooldown/3s reveal remains. No source demo is auto-installed.

COM and SEN share IRRLScanDevice/RRLDeviceBinding and guarded ordinary InputSystem/GUI controls. K remains structural LOS/HP snapshots; L reveals typed enemy/wreck/hazard signals through walls, without structural HP. HUD displays distance/bearing and live source TTL. Each panel shows up to3 rows plus total count, keeping both above the existing movement panel at logical y490.

Original ScannerPulse remains the sole clock/query/reveal implementation. A minimal operational flag clears results on EMP/loss without clearing committed cooldown; a restore API reconnects canonical DeviceReadyAt after rebuild. A valid empty pulse spends its original cooldown; rejected retry does not. Pause blocks activation and freezes time/visibility. OnDisable may reset the source view but never overwrites committed owned state.

One invisible trigger/tag belongs to each actual live actor root, actual R3Debris root or actual hazard root. The live original source uses configurable built-in IgnoreRaycast layer2; original128 query capacity and demo defaults stay unchanged. This avoids dense physical-part colliders without substituting a new query algorithm. Combat/LOS/hazards ignore triggers; markers have no Rigidbody, HP, solid geometry or forces. They are prepared on explicit activation, with no per-frame scene search. Core death disables enemy tag/trigger immediately; real detached wrecks are separate Salvage signals. No inventory is revealed. W9 objective tagging remains a dependency until the actual activity exists.

Observed RED confirmed absent physical garage kind; the intermediate core run passed14/15 with only deliberately absent L/controller path failing. Final16/16 Edit =9 original+7 integration, original Play3/3, COM/shared scan regression7/7, final HUD compilation/GUI L1/1; all process exits0. Tests cover real range enemy through a solid wall while COM cannot reveal HP, self exclusion, two139-part Heavy rigs without truncation, unchanged owner and marked enemy mass/inertia/pose/velocity, empty/retry timing, pause, EMP clearing/recovery without free activation, actual destruction/Capture/paid repair/DeepCopy preserving identity/cooldown, and owned GUI activation guards. An explicitly separate source-query API fixture verifies actual Gauntlet hazard tags, immediate dead-enemy removal and actual debris Salvage signals.

These are Editor API/physics/input fixtures, not native Windows gameplay. W8 Windows/native mounting, activation, damage/repair and complete regression are pending COM/SEN/MOV011 wave closure. W7 RC1 remains independent. No shared-branch merge.

**Decision: INTEGRATED, targeted tests PASS; BLOCKED pending W8 native Windows and full-wave closure.**

[Machine-readable cases](SEN-001_test_summary.json). API fixtures are distinct from native input gameplay.

Integration [Draft PR #33](https://github.com/C8LyNn/RoboRougelike/pull/33); no shared-branch merge.

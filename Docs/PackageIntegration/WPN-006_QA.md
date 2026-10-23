# WPN-006 EMP Emitter and W6 closure QA

Source: verified kit WPN006EMP SHA256 `19afc48aa519f093b476af2df779a52daf92766a787af54a1f8a9a22b0c342c0`. Feature feature/rrl-wpn006-integration; base WPN005 checkpointc0056e1 on immutable W5. Original dry-run/apply imported15 Assets files with no overwrite. Unity/package version remains6000.3.14f1.

Integration source checkpoint: `a63f8192fa5c223f47002b71c60e2b51eb49a8c3`. [Draft PR #28](https://github.com/C8LyNn/RoboRougelike/pull/28), stacked on WPN-005 #27. No shared-branch merge.

**Current decision: READY in the W6 RC2 aggregate; full757/333 and Windows gates passed, with the known Unity/UIA exit limitation. Editor API fixtures remain distinct from native input gameplay.**

Original EmpEmitter owns pulse overlap/deduplication, LOS, owner/team exclusion, distance/resistance duration and commit semantics. Defaults remain9m radius,3s maximum disruption,35% edge duration,12s cooldown,0 activation energy,1024/128 query budgets. Valid empty pulses consume cooldown. Existing cadence modifiers snapshot into cooldown; EMP causes0 HP damage and no direct velocity writes. Integration chassis tuning:30kg,140HP,65scrap. Physical garage guns and the Industrial four-enemy escort expose EMP; that escort approaches6m and waits for an8.5m target before pulsing. Other arenas retain railgun/saw/beam paths.

A receiver belongs to each actual electronic part: guns, wheel motors, active movement devices, suspension, shield and point defense. Armor/core/passive bonuses remain immune. The receiver keeps its original enabled recovery clock; a narrow Disabling event snapshots persistent state before its existing ClearEmp callback. The emitter alone opts into a controlled clock and bounded cooldown restore. Canonical R3 DisruptionRemaining/DeviceReadyAt survive capture, inventory, actual actor teardown, copy, paid repair and rebuild. Complete R4 snapshots also preserve the state of ordinary and elite weapons/wheels. No extra vehicle HP or Rigidbody solver is introduced.

Temporary operation is explicitly separate from physical survival. R3MountedPart.Operational remains physical so capture never marks EMP-disabled parts destroyed. P12 functional modules expose physical and effective queries; defeat checks use surviving wheels/weapons/drives, while torque/fire use effective availability. Enemy WeaponOnline remains physical so EMP cannot call DropTurret. Existing R6 wheel forces, enemy wheel counts, MOV-series availability and MOV009 support filters respect disruption while preserving mass and momentum. Shared IRRLDeviceLifecycle refreshes all package source modules synchronously on status changes; expiry cannot resurrect destroyed/detached parts.

## Targeted evidence and fixes

- Initial RED4/4: missing EMP catalog and electronics receivers. Additional RED caught active enemy wheel count4 despite EMP; effective count now0 without HP loss.
- Final Emp filter **50/50 PASS**:35 original EMP EditMode cases,10 integration/lifecycle cases and5 incidental matches from other existing tests (the substring also matches Empty). Final original Play **24/24 PASS**, native process0. Original OnDisable case was moved with unchanged assertion from invalid EditMode dispatch to real PlayMode.
- Actual original pulse disables an exposed enemy gun; production timer recovers it automatically; killing it during disruption then clearing EMP cannot restore operation. No HP/momentum change. Frontal armor correctly occluded the gun (5 detected,3 occluded,2 exposed wheels affected); the gun case deliberately emits above armor. Teleport fixture first had stale ClosestPoint132m despite near overlap/raycast until an actual PhysX step; no game query algorithm was changed.
- Actual expedition stays Combat when all electronics are temporarily disabled. Hover can have no effective drive while its physical drive survives. HP/colliders/vehicle mass/velocity remain intact; saved wheels remain healthy and disrupted after rebuild.
- Actor-teardown RED exposed canonical disruption3s becoming0 when old Unity objects were disabled. Lifecycle snapshot before receiver ClearEmp fixes it. Elite battery salvage RED exposed2s becoming0; complete state snapshots now retain it.
- W6 shared physics RED: dynamic shield mass stayed208kg instead of240kg due R6 Awake part cache. Explicit RefreshParts at mounting/final composition now includes actual new parts/materials. Destroyed armor carrying a PDS previously made an8kg fragment; live same-body subtree is43kg (8armor+35PDS), removed immediately from its owner once, with no duplicate solver. These shared fixes are dependencies for earlier shield/PDS feature checkpoints when using them in the aggregate.
- Practice transition **1/1 PASS**: sandbox budget uses existing600scrap; an existing Gunner/rocket model is reused as an actual electronic target, default fireOFF, optional UI toggle, pause-aware control, no loot/rewards, cleaned on return with workshop scrap unchanged. This makes EMP/shield/PDS testable through actual game UI; no new enemy model/mechanic is synthesized.
- Weapon inventory briefs now describe DPS/contact/penetration/EMP and real ammo rather than falsely displaying beam as35damage per.02s or EMP as a broken0damage gun. Defense HUD no longer overlaps suspension telemetry.

Earlier shared RRLW6 runs27/27 and Emp50/50 are API-driven only. The subsequent full W6 regression/build/native evidence is in the wave report; all eight mechanics are READY in the tested RC2 aggregate. Known W5 UIA-client shutdown crash is unchanged and is not called fixed.

**Current decision: READY in the W6 RC2 aggregate, with the documented Unity/UIA shutdown limitation.** API-driven physics is recorded separately from native player-input gameplay.

[Machine-readable test evidence](WPN-006_test_summary.json). Raw XML and logs: ignored Artifacts/Local paths listed there.

## W6 closure corrections

Release source checkpoint: `e9ba555e6fab1798c679fe90fa4a8d0e2ad5c8de`. The first full EditMode run was **750/755**, native2; it is not reported as passing. It exposed expired weapon references in enemy salvage, two obsolete loot-count expectations after defense mounts, and one reward-offer assertion that passed in isolation and in the subsequent affected suite without reward-logic changes. The new expiry regression reproduced the MissingReferenceException before the Unity-null guard.

The affected campaign/garage/W6 suite then passed **55/55**, native0, including six actual physical-projectile fights with active AI and ordinary virtual aim/fire input (227.382 seconds for that case). This is automated Editor input/physics evidence, not manual Windows gameplay. A separate finite-resource expiry regression reproduced spent shotgun ammunition11 becoming the initialization sentinel−1. Enemy equipment now retains references to the same canonical R3 data independently of disposable Unity views. Expired shotgun, shield and point-defense loot retains identity,0HP, ammo11/charge40/ammo7 through claiming, paid repair and DeepCopy. Final expiry/range targets **3/3 PASS**, native0. No second HP or resource store was introduced.

The optional practice enemy is now on the left in front of the starting position, within EMP range, still default fireOFF. The full757/333 run, both Windows builds,47 native API checks and six-stage native input campaign subsequently passed; see current wave evidence. [Wave evidence/status](W6_WAVE_QA.md).


Current aggregate source `8cf4ff8c80178f04f8d60dc6f57d03a2e7f7f32f`: all W6 gates and known limits are recorded in [W6 wave QA](W6_WAVE_QA.md). Native per-device checks use APIs/real physics; UI-driven garage/campaign flow and virtual-input combat are identified separately. Earlier frozen feature SHAs do not include all shared closure fixes.

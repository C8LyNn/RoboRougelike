# B01-24 / ENEMY-CHARGE-001 / W28 R2

Source-only continuation of W27 #804 @ 4dcec50d1a9a245393ea9f63d1f6925eb6566465.
No canonical Codex AI, weapon, Save, ECON V7, garage, protected branch, or active game worktree modification.

## Functional delta
- Existing 4-phase physics dash (Seeking / Telegraph / Charging / Recovery) is preserved.
- Collision with walls, props, unassigned enemies, or allied actors stops the dash but **never forwards damage** to incidental IChargeDamageSink implementations.
- Only the explicitly assigned target (or its child collider) may receive one damage callback per charge.
- Optional IChargeTeamIdentity on target/surroundings prevents friendly-target acquisition and allied damage even without an EnemyChargeMotor on the ally.
- OnCollisionStay handles contact already overlapping when the dash starts, while vertical-only ground/ceiling support contacts do not abort a horizontal charge. SetArmed(false) still cancels attacks.
- ChargeDemoHealth is a test-scene sink, not canonical HP or affiliation authority.

## Authored genuine Unity PlayMode source tests (not executed)
1. FreeLane_OneRealPhysicsHit
2. SolidBlockerWithHealth_IsNeverDamagedOrPassedThrough (R1 would hit the obstacle sink)
3. CancelTelegraph_NoDamage
4. FriendlyAssignedTarget_CannotBeAcquiredOrDamaged
5. MovingKinematicBlocker_StopsChargeWithoutDamagingProp
6. SecondAlliedRobot_InterruptsWithoutFriendlyFire
7. FloorSupportContact_DoesNotAbortCharge

## Acceptance and boundaries
- **Native G1 NOT_RUN**: Unity 6000.3.14f1 Editor/PhysX/NUnit/Windows Player unavailable in this environment.
- **Independent G0 PENDING**: B01-40/61 need exact blob/GUID/asmdef/source review, then B01-60 must run physical PlayMode and collect NUnit XML/Editor.log.
- The scene/prefab generator from W27 exists, but generated .unity/.prefab are **not checked in** and must be created and accepted in licensed Unity before CODEX intake.
- ObstructionLayers is optional and defaults to zero; collision-based stop works without it, but pre-charge line-of-sight must be configured by integrator.
- This package does NOT bind itself to production enemy AI / HP or prove enemy movement arbitration. CODEX intake is not authorized until G0 and native QA.
- All seven tests are source-authored; none are represented as PASS.
- Keep existing W27 #804 as rollback target. Integrate only when accepted individually.

**Receiving roles:** B01-40/61 for independent G0, B01-60 for genuine Unity G1, B01-00 for integration decision, Codex only after acceptance.

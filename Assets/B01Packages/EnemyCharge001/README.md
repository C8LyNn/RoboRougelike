# B01-24 / ENEMY-CHARGE-001 / W27 R1

Actual isolated Unity6 Rigidbody unarmed enemy actuator. AI assigns target by AssignTarget; no canonical AI, weapon, Save, garage or Codex worktree edits.
States Seeking -> Telegraph -> Charging -> Recovery; target validity and optional wall raycast check before charge; AddForce physics; stop on dynamic collision; ally collision stops without friendly damage; one IChargeDamageSink callback per charge; cancel via SetArmed(false).
A dedicated ChargeDemoHealth receives damage only in test/demo, NOT production canonical health.

**Generate physical prefab and sample test scene** through Unity Editor menu: B01 > Enemy Charge 001 > Generate Prefab And Demo Scene. This source revision includes generator only; assets are NOT yet generated and must be checked in by asset owner after native QA. Sample player must receive AssignTarget in scene; prefab does not retain a scene object reference.

Authored genuine three Unity PlayMode [UnityTest] cases: collision hit once, blocker stops charge, cancel telegraph. NATIVE G1 NOT_RUN: no Unity Editor or NUnit evidence available; group collision/manual review still needed. The script performs genuine Rigidbody movement but actual in-game integration is PENDING.

Ownership: B01-24 to review/extend, B01-40/61 independent source/GUID G0, B01-60 native Unity G1, B01-00 HQ. Do not offer to Codex until proper G0 and noncollision ACK. Rollback remove only exclusive EnemyCharge001 files in isolated copy; do not touch shared or user empty directories.

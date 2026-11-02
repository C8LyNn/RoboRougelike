# PASS17 / WAVE28 — Rotary Saw physical self-contact guard

Exact parent #824 @ e03d36f2b86f8c73a067e574b67237a8921f622a.

Product change: the actual WPN005 RotarySawModule now performs an independent PHYSICAL disc-volume owner obstruction query before motor energy/thermal progression, distinct from the projectile path. Only its own blade child is exempt. Both connected owner parts and registered detached owner colliders can block. A saturated query fails closed and stops damage/motor. If the own piece moves or is disabled, the existing requested motor resumes without resetting cooldown or inventing shots. Other entities retain existing contact damage rules.

Changed original gameplay source: Assets/RoboRougelikeMechanics/WPN005RotarySaw/Runtime/RotarySawModule.cs. Native PlayMode test additions in Assets/RoboRougelikeMechanics/WPN005RotarySaw/Tests/PlayMode/RotarySawPhysicsTests.cs: own armor blocked and later resumed, registered previously owned detached collider blocked and later resumed. Existing WPN005 and W27 suites still need a licensed native Unity 6000.3.14f1 run.

**Tests:** Unity compile NOT_RUN; EditMode NOT_RUN; PlayMode NOT_RUN; Windows Player NOT_RUN, profiler NOT_RUN. Source code only; no honest Unity PASS claimed. Known review risks: a broadphase buffer overflow conservatively stalls saw when very many contacts exist, and fast geometry transitions require active PhysX validation. No Canonical Save, ECON, garage, other weapon changes. Verify dynamic current parent and flags in full game before acceptance. Source-only PR not production accepted.

**Rollback:** revert only this PR on an isolated W27 child branch; retain #824 as-is, do not merge into main/develop. B01-61 independent test and B01-60 native G1 are required. No new Codex Inbox OFFER until G0.

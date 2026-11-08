# B01-41 | W28 — independent original Codex #824 Rotary Saw / EMP native tests

**Source:** W27 original Codex [#824 exact e03d36f2b86f8c73a067e574b67237a8921f622a](https://github.com/C8LyNn/RoboRougelike/pull/824). No gameplay code changed and no Codex active worktree accessed. Seven new CI41-only paths including 1 genuine Unity Test Framework .cs + unique .meta GUID, SHA pin source guard and Windows/Ubuntu CI.

## What it tests in the ORIGINAL GAME — four actual native UnityTest cases

1. RotarySaw real R3 Light assembled gun: own 3D collider physically intersects active rotating blade sweep; expected blocked motor RPM/heat stay zero, then unblocked resumes without stale cooldown. This is a **new W28 expected acceptance**, NOT guaranteed by original #824: its real saw binding currently spins whenever request is held, although underlying blade filter protects own damage. Native regression may RED and must be fixed by Codex in an owner-controlled gameplay branch.
2. Active real owned 3D collider under saw disc never suffers direct damage/self-hit; original R3 part health remains unchanged.
3. Original direct EMP binding under one actual R3 Light rig: real PhysX wall between own emitter and target protects target, owner's same-vehicle receiver remains immune, valid activation does spend cooldown (EMP differs from projectile blocked shot semantics).
4. Real EMP direct module, two colliders on one external receiver, owner in overlap: one affected module and no own disrupted state, second activation denied without double cooldown or bullet-ammo cost.

### What is NOT claimed
These four tests are **authored and committed** but not executed by real Unity in this chat. GitHub CI runs source provenance/anti-mock controls only; actual Unity G1=NOT_RUN until B01-60 or Codex executes licensed Unity 6000.3.14f1 and supplies Editor.log, exit code and NUnit XML. A green source workflow is NOT a W28 gameplay PASS. Saw fail is a test hypothesis based on current source, not a measured native failure.

Other open systems: missing five garage color zones, same-preview-validator and no-fee invalid purchase test in actual game; #812 one-time loaner deferred win softlock PRODUCT RED (policy decision to B01-00; no unauthorized V7 change), #797 Unity API incompatibility BLOCKED_BY_PRODUCER. W27 #824 targeted Unity tests 6/6 and 5/5 are separate, not replayed by this QA.

### How to execute after authorized staging
Use source branch exact commit; copy only new test .cs + .meta to already existing Assets/Tests/Editor in independently authorized Unity project based on Codex #824 exact source. Run `-runTests -testPlatform editmode -testFilter CI41W28SawEmpNativeRegressionTests -testResults CI41-W28-NUnit.xml -logFile CI41-W28-Editor.log -batchmode -nographics -quit`, evaluate XML counts, exit code and SHA. EnterPlayMode tests invoke actual original game assembly, not fake UnityEngine types. Use disposable saved-game location and no player data.

Only after B01-60 independent G0/owner authorization consider Codex #79 OFFER. Native RED is a real bug to fix, never auto-merge/release. Source exact artifacts and log receipts in own dedicated CI workflow, no Unity license preflight spoof.

Lifecycle: IMPLEMENTED=YES_NATIVE_TEST_SOURCE; TESTED=AWAIT_ACTUAL_GIT_CI; QA_ACCEPTED=NO_FORMAL; READY_FOR_INTEGRATOR=NO; INTEGRATED=NO; RELEASED=NO. G0_source pending; native G1 NOT_RUN; G2 HOLD; Codex NEW PACKAGE ACK NONE. Receivers: B01-60/61/40, B01-00. Safe rollback discard 7 new CI41 paths only.

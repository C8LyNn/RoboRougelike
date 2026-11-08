# B01-61 / PASS17 WAVE28 — independent **real Unity** dynamic self-occlusion regression

Original playable Codex W27 frozen source base: [#824 @ `e03d36f2b86f8c73a067e574b67237a8921f622a`](https://github.com/C8LyNn/RoboRougelike/pull/824), itself based #817. The existing original CodeX 6/6 special projectile tests + 5/5 Beam EditMode native targeted cases are real **prior tests**, not proof of W28 dynamic blocker coverage. Source tests W28 only.

## Implemented without game source edits

**Three new authentic `[UnityTest]` fixtures** in Unity's **existing** `Assets/Tests/Editor` NUnit assembly — no shadow input systems, no fake player attacks and no physical game source changes:

1. `MovingOwnedArmorBlocksBothDirectAndGroupSpecialGunsWithoutAmmoOrCooldown`: build original R3 Light for Shotgun, Railgun, Rocket. A real kinematic Rigidbody owner blocker moves from outside the path into the active path and away. Check original `ClearProjectilePath`, actual module direct `Fire()` denial, actual LMB `FireGroup(false)` denial, same ammo/device & group cooldown while blocked, then an actual successful round and cooldown after moving obstruction away. Three actual physically assembled weapon variants.
2. `RotatedGunMustTreatDifferentGunColliderAsOwnedButExternalCoverAsFirstHit`: actual R3 Basic Gun Pivot rotates 35 degrees. Move a second owned component's real Rigidbody collider into its *new* path and verify it blocks, and an external obstacle strictly closer to the muzzle than this owned component takes precedence; without external, self-block resumes. Actual `Physics.SyncTransforms` after repositioning.
3. `ContinuousBeamCoolsOrStaysStableWhileMovingOwnedArmorEntersLane`: actual BeamCutter attached to original R3; real FixedUpdate original binding's controlled fixed step. Heat rises when clear, must not rise while real kinematic owner collider blocks, resumes increasing after moving blocker away without releasing player trigger.

The tests are authored and submitted for **native Unity 6000.3.14f1 execution by Codex/B01-60**. No false PlayMode/EditMode PASS: CI only runs strict original source SHA/GUID + fixture structural readiness on Ubuntu/Windows; actual native G1 remains `NOT_RUN` until Editor.log, NUnit XML, exit code and exact corresponding HEAD are published. These tests do not cover Saw, EMP, garage five-zone UI or the whole Windows Player and cannot promote W28 completion.

## Safe test execution (receiver's existing licensed Unity Editor)

- In a separate disposable, source-compatible checkout based **exact #824 original SHA**, add ONLY this new test source and its `.meta`, preserving existing `Assets/Tests/Editor`, and verify per-file source SHA/GUID receipt. Do not overlay active Codex or user Save. Existing `[UnityTest]` suite runs under EditMode test assembly despite entering PlayMode for physics.
- Run EditMode `B0161W28DynamicOwnClearanceRegressionTests` with the existing project test runner; retain **genuine** Unity 6000.3.14f1 Editor.log and NUnit result XML with pass/fail/skip/total/exit.
- If a testcase fails due to actual own-rig 0.55m geometry blocking a supposed unobstructed fixture, report the exact collider geometry/stack and fix only QA setup if genuinely erroneous; do not modify production to make tests green. Confirm dynamic self-vs-foreign geometry and ammo/heat invariants before re-testing. In particular, no fake proxy game combat harness is a substitute.
- Do not inject these files into #817/#824's active branch or edit physics/shooting sources. B01-00 decides when to offer; B01-60 independent owner native G1, Codex resolves actual gameplay failures on a newly versioned candidate, B01-61 reruns and reviews.

**Scope of this PR:** source-G0 of **added test cases only**. No production gameplay fix, no unit/integration result for new tests until native run. No launcher/ZIP or new Player build, no altered Save. G0 full game `HOLD`, G1 `NOT_RUN`, G2 `HOLD`, `INTEGRATED=NO`, `RELEASED=NO`. No simultaneous tool/agent worktree write.

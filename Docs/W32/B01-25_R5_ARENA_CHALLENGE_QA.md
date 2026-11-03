# B01-25 — PASS 17 / WAVE 32 — canonical R5 arena challenge acceptance

## Exact ownership and source
- Existing real game receiver: Draft PR #877 @ `3bd9b159997924dcecb4e1cd1d41137896e5199e`, descendant of #873 @ `d537b7310979b91df27a08b14d0f1c74d0889089`.
- Own QA-only descendant: `research/b01-25-p17w32-r5-challenge-qa`.
- Only two new Unity test sources plus .meta, this document, owner-scoped source verifier and its Actions workflow. **Zero production C# changes.**
- Separate existing SAFEROUTE R5.1 #868 @ `49540536ee91695671142abe6089ae87a05b72e3` is **NOT INSTALLED**. Its real R3Debris event hook, physical route subsystem and 27 authored native tests remain independent and require G0/G1. Never co-install obsolete R1–R5 revisions.
- Independent multi-collider *real player impulse* test owner: B01-60 PR #901. No duplicate version of that test.

## New native acceptance hypotheses (AUTHORED_NOT_RUN)
1. `AllThreeExistingR5ArenasRetainWideCentralSpawnToCombatCorridor`: instantiate **actual production R5Arena.Build** for every `R5ArenaKind`; check 20 physical longitudinal samples of a 3.4m-wide virtual vehicle against the **real live generated solid Collider.bounds**. Trigger-only modules are ignored, not invented as solid walls. No simulated robot movement or AI win is claimed.
2. `RealSalvageYardJumpPadHasWideIngressAndPredictedClearTouchdown`: construct actual Salvage Yard, inspect exactly one real active MapJumpPad and trigger, sample actual spawn→pad ingress and predicted touchdown along configured 8.5m/s upward + 2m/s forward launch under actual Physics.gravity. Check *solid collision envelopes*, not human playability, real multi-collider launch, trajectory or landing dynamics.

These do **not** resolve the approved V7 one-shot-loaner policy issue #834. No gameplay economy, Save, active map generator, live R5 behavior, Rigidbody, weapon, garage or branch-owned files were changed.

## Run / evidence
Source static/scope verifier (no Unity, no gameplay PASS):
`python Tools/B01/B01-25/P17W32R5ChallengeQA/verify.py`

Unity 6000.3.14f1, activated Windows Editor, clean **isolated** exact commit checkout:
- Run EditMode Test Framework filter `B0125W32RealArenaChallengeTests`.
- Record exact PR head SHA, NUnit XML (2/2 if actually passing), Editor.log and Unity exit code.
- Follow with existing W29 arena tests, #901 real R3 vehicle launch cases, full EditMode/PlayMode, Windows x64 build, extracted Player run+Player.log. Any failures remain RED.
- Actual native G1 in current executor: **NOT_RUN** (no activated Unity Editor). Windows Player: **NOT_BUILT**. Human playtest: **NOT_RUN**.

## Risk / rollback
A 3.4m clearance probe is a bounded level-design envelope, **not** a guarantee for every buildable robot silhouette or physics motion. Predicted touchdown assumes free-flight constant gravity and no external push. Failures require scene/geometry diagnosis by B01-20/25 and owner-approved change to the same canonical game, not a second arena implementation. Roll back by discarding this isolated QA-only PR; original #873/#877 are untouched. No merge/release.

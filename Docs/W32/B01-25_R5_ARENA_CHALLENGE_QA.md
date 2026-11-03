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

## PASS 17 / W33 — update on the SAME QA-only PR #911

**Confirmed source-test oracle defect:** Original W32 test used a hard-coded 3.4 m wide envelope for all R3 vehicles, ignoring that production `R3VehicleCatalog.Create` has 20/83/139 parts (Light/Standard/Heavy) and `R3VehicleAssembly` lays out part centres on a **0.55 m** grid. Heavy reaches X cell ±4 at the wheels: wheel centre span alone is **4.4 m**, exceeding the W32 3.4 m proxy. W32 source-only green did **not** prove Heavy clearance.

**W33 correction, no product edits:** Both existing native Unity test cases now compute factory-grid bounding X/Z dimensions from **actual** `R3VehicleCatalog.Create(R3VehicleKind)` cells, plus 0.15 m on each side. Nominal conservative grid envelopes (not measured mesh/collider bounds): Light 3.05 m × 3.05 m; Standard 4.15 m × 4.15 m; Heavy 5.25 m × 5.25 m. First original test now checks **3 existing real R5 arena kinds × 3 factory vehicle designs × 21 central travel poses**. Second original test checks real Salvage Yard Pad active trigger, all three factory-size approach and estimated touchdown samples. Uses actual R5 colliders and the same game's original R3 factory; no new arena, no pathfinding authority, no second collision validation, no Move or material game source change.

**Important coverage limits:** These are conservative axis-aligned grid *proxies*; they do not account for chassis yaw/roll, arbitrary player-built appendages, attached extra weapons, dynamic debris, real AI driving or actual trajectory and live multi-collider JumpPad launch. Distinguish `ASSERTED_BY_AUTHORED_UNITY_TEST` from `NATIVE_EXECUTED`. An oversize build requires genuine physic collision/route validation after G1. B01-60 #901 owns actual compound vehicle impulse. Current selected game Save P0 #913 and economy #834 remain separate owners; this PR changes neither.

**Updated source guard:** The existing own Windows/Ubuntu workflow now also immutably pins original `R3VehicleCatalog.cs` and `R3VehicleAssembly.cs` against parent #877, verifies factory calls/0.55m grid and rejects deliberate missing factory and undersized grid mutants. Source-only receipts cannot certify Unity. Need exact new PR head GitHub Actions and independent B01-40/61 G0, then activated Unity6000.3.14f1 Test Framework 2/2 with Editor.log, NUnit XML and exit code, and extracted Player under owner-authorized G1.

**Rollback:** Discard own unmerged QA-only PR #911; protected project #877 remains unchanged. W33 additions supersede original 3.4m QA assumptions above; the W32 section remains a historical record.

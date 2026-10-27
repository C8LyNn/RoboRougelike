# B01-20 / Dependency-aware CHL C1: B01-21 measured gate–bridge mismatch

**TASK_ID:** C1-CHL003-W22-B01-21-YAW8-GATE-ALIGNMENT
**OWNER:** B01-20 exclusive Unity production, #276.
**INPUT_ARTIFACT_URI@SHA:** [B01-21 geometry QA Draft #295](https://github.com/C8LyNn/RoboRougelike/pull/295) @ `cadc35dca5e5e8cb2cd6595f7e860b0c898ef8ec`, independent original producer [CHL #285](https://github.com/C8LyNn/RoboRougelike/pull/285) QA source pin `9a947108928d4c2cdfdfbb25cbeb30464439da37`.
**REQUIRES:** B01-21 W17 QA receipt [HQ comment6439226800](https://github.com/C8LyNn/RoboRougelike/issues/71#issuecomment-6439226800) actual source geometry measurement, DONE, QA SHA above; B01-20 previous source HEAD `f4ca318577edbdf4d003ab3e104ac7ec71d83cfe` (existing C1 source CI PASS run [40486705906](https://github.com/C8LyNn/RoboRougelike/actions/runs/40486705906)) — verified unchanged source fields prior to this update.
**PRODUCES:** strictly in existing CHL Draft #285: correction of all 4 checkpoints + finish trigger apertures to 3.70m, visible post center ±1.97m; actual source C# RED(old gate width)→GREEN(new bridge-aligned) analytical cases with real Heavy original primitive geometry, native NUnit E19 plus real Physics P14 (authored only), static importer negative.
**OWNED_PATHS:** `Assets/RoboRougelikeChallenges/CHL003/**`, `Tools/B01/CHL003/**`, `Docs/B01/CHL003/**`; no R3, PARTSIZE, MAPGEN, Codex, or other lane.
**STATE:** producer candidate WORKING until exact final Actions source gate + ZIP manifest; then DONE-SOURCE-ONLY / native G1 BLOCKED.
**QA_KIND:** SOURCE actual original C# and source-param analytical cases, native real Unity = NOT_RUN.
**BLOCKED_BY:** `C3-G1-NATIVE_UNITY` actual licensed Unity6000.3.14f1 (CI-02/03 activation credentials unavailable), `C2-G0-B01-40` source/rollback for NEW HEAD. No G2.

## Why the dependency changes gameplay
B01-21 parsed actual authored Heavy source: body Box chassis 2.50×3.40m; four sphere wheel colliders radius0.410m with centers X ±1.00m and Z ±1.15m. At yaw8°, its conservative projected width is ~3.1205m. The physical bridge/ramp width is **3.70m** (clearance (3.70-3.1205)/2 ≈ **0.2897m**, passes game-policy minimum0.25m). The PREVIOUS checkpoint/finish BoxCollider is only **3.50m** (0.1897m/side), therefore CHL003GateCrossing's actual full-body clearance rule can reject a route physically passable on the bridge. This is an upstream source-level scenario, not an observed Unity failure.

Widen the gate trigger to **3.70m**, leaving the 0.25m rule *untouched*. Move visible gate side posts to ±1.97m (half3.70 + .12m). A yaw10° Heavy retains ≈0.2555m lateral clearance, yaw12° ≈0.2228m remains correctly RED under 0.25 threshold. Light at yaw30° fits. No widening of physical bridge, no deliberate reduction of safety margin, no changes to production car sizes. Importantly the physical bridge remains narrower than 4.20m final platform.

## Executable checks and native falsifier
Source headless net8 compiles **actual CHL003GateCrossing.cs** and tests upstream parameters: old 3.5m must FAIL under Heavy yaw8, new 3.7m must PASS, Heavy yaw12 must remain FAIL, both rigs yaw0 pass, measured marginal side clearance stays >=0.25 for yaw8 and <0.25 for yaw12. The Python importer/source gate reads original demo gate width, bridge width and post offsets and REDs if the gate is reverted to 3.5. Static negative mutants are not Unity physics.

Unity **E19** (authored) tests actual C# gate policy using source-observed wheel sphere/chassis yaw geometry. Unity **P14** (authored) does genuine physics-only CP0 old gate3.5 NEGATIVE → Retry → same Heavy Rigidbody yaw8 gate3.7 POSITIVE, without direct calls to checkpoint state. **NOT_RUN** until external license-backed Unity; if Unity shows new behavior differs, return original NUnit XML/Editor.log to B01-20, state REWORK rather than automatic offer.

**NEXT_UNBLOCKS:** when source CI exact HEAD and deterministic physical ZIP PASS: B01-40 independent **C2 current SHA** G0 + rollback; B01-60 native **C3 19 EditMode / 14 PlayMode and 3 Light + 3 Heavy** actual original demo traversals. B01-10 C3b scoreboard must wait for authentic six-trial telemetry; C4 HQ G2 remains HOLD until native + owner signoff. No Codex OFFERED_FROZEN/ACK, no main merge.

**Revision integrity:** NEVER reuse historical ZIP SHA256 `05670f8fa1daf1b19ec2cdf401b18db62f6e1a866d1113343ae949a264fd5138` or old B01-40 receipt for NEW HEAD. The final actual ZIP SHA is emitted by CI, not guessed.

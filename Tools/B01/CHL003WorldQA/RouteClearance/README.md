# B01-21 / N27-21 — original CHL-003 full-course longitudinal source audit

**TASK_ID** `N27-B01-21-CHL-SOURCE-ROUTE-20261027`; **OWNER** B01-21; **QA_KIND** actual source-parsed geometry, independent test; **NATIVE UNITY/PhysX/CONTACT=NOT_RUN**. No Unity implementation, no third package. Task from [HQ dependency queue #297](https://github.com/C8LyNn/RoboRougelike/issues/297), which contains a historical CHL reference. The actually verified producer input is [B01-20 #285](https://github.com/C8LyNn/RoboRougelike/pull/285) **exact `077e692785aff2990a2d72a80fd1db62ac57b0e8`**, source `CHL003DemoBootstrap.cs` Git blob `9308ceb9eab89d1ab483b560ff3ed58fa6d2d32e`, original gate policy blob `5c736da620487e48131f6d454bcf231ae368b513`. Producer latest source [Actions #40489108497](https://github.com/C8LyNn/RoboRougelike/actions/runs/40489108497) SUCCESS and real full ZIP SHA available. Previous gate-width issue was SOURCE_FIXED and confirmed with original C# in [B01-21 #295](https://github.com/C8LyNn/RoboRougelike/pull/295); **this audit DOES NOT re-report it.**

## New research axis (not another gate yaw duplicate)
This checks **LONGITUDINAL** course continuity and checkpoint position against exact physical Unity BoxCollider authored dimensions in the current demo, not just the old lateral width safety requirement. Parses five original source `Box(...new Vector3...)` statements and five gate `Gate(CP0–3, FINISH)` statements, compares original `CHL003GateCrossing` policy tokens; calculates top face of authored Euler pitched ramp mathematically; computes true authored Box + four Sphere *static rotated AABB* in both Z and X at yaw0/8, plus **an idealized flat-surface wheel-root height** based on radius0.41m/center local Y=-.52m. These are **hypotheses and conservative source projections**, not evidence a real vehicle touches or completes anything.

### Measured original course facts
- Ground top Y0m and Z interval `[-6.50,14.50]`. First CP Z5 and second CP Z11.5 both lie on ground. Real physical low barrier is at Z8.7, a possible mechanical hurdle between CP0 and CP1 (actual contact / traversal **NOT_RUN**).
- Ramp `center=(0,.54,16)`, box `3.7×.55×7m`, pitch -12°: **upper face** entrance approx `Z12.5193, Y0.0813`, exit `Z19.3663, Y1.5367`. Ground→ramp top-plan overlap approx **1.9807m**, entrance top rises **0.0813m** over ground.
- Bridge `Z[19.5,29.5]`, top Y1.68 and width3.70m: between last ramp upper edge and bridge start **0.13366m horizontal gap** plus **0.14332m rise in top surface**. Wheels radius.41m might negotiate it; real PhysX is unresolved. CP2 at Z21 and CP3 at Z29 are on bridge.
- Final platform `Z[29,40]`, top Y1.68, width4.20m; bridge overlaps platform **0.50m** in longitudinal world axis. CP3 Z29 lies at shared connection; Finish at Z38 lies 2m before final platform end (at its center).
- True 5 gate center spacings in order: CP0→CP1 **6.5m**; CP1→CP2 **9.5m**; CP2→CP3 **8m**; CP3→Finish **9m**. No actual co-located CP3+Finish issue.
- Source **static projected forward hull bound after finish center crossing**: Heavy yaw0 **0.30m** from front hull to end of platform, Heavy yaw8 approx **0.143m**; Light yaw0 approx **0.84m**. Heavy yaw8's remaining static longitudinal margin is shorter than the illustrative 12m/s at 50Hz single-step travel0.24m; this is **a repeatable boundary-case proposal, not proof of a fall/win loss**, because real contact wheel and actual speed/finite gate callback must be measured.
- At source yaw0°, the whole actor (max of hull Box and wheel Spheres) overlaps the gate plane for longitudinal distance ≈**3.56m Heavy /2.48m Light**, rather than the trigger box's own 0.16m alone. This would correspond to approx 14.8/10.3 fixed steps at the illustrative source speed cap 12m/s and standard 50Hz, **not** a claim any Unity `OnTriggerStay` callbacks occurred. P07 real wheel motor six-run must verify CP event delivery.

### This source-only test explicitly cannot prove
PhysX joints/contact, ramp climb, inability to pitch/roll (both rigid rigs freeze X/Z rotation), tire collision friction, maximum true speed, actual airborne wheel ray contacts, physics callback order, NUnit Unity Edit19/Play14, complete 3×Light+3×Heavy passage, screenshot/video or G1. A static source corridor consistency check **is not** `GAME_READY` or `G2`. Worst-case dynamic overshoot at finish is a *native falsification candidate* only.

## Executable and negative tests
```sh
# This B01-21-only QA PR deliberately retains an older Git base.
# ALWAYS fetch SOURCE BY ORIGINAL B01-20 producer SHA, not the QA worktree.
producer=077e692785aff2990a2d72a80fd1db62ac57b0e8
git show "$producer:Assets/RoboRougelikeChallenges/CHL003/Demo/CHL003DemoBootstrap.cs" > /tmp/n27-current-demo.cs
git show "$producer:Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003GateCrossing.cs" > /tmp/n27-current-gate.cs
export B01_21_CURRENT_CHL_DEMO=/tmp/n27-current-demo.cs
export B01_21_CURRENT_CHL_GATE=/tmp/n27-current-gate.cs
python3 Tools/B01/CHL003WorldQA/RouteClearance/route_source_qa.py --output /tmp/N27_CHL_ORIGINAL_SOURCE_ROUTE.json
python3 -m unittest discover -s Tools/B01/CHL003WorldQA/RouteClearance -p 'test_*.py' -v
```
The CI additionally verifies Git SHA-1 for **both** extracted producer blobs before testing. Passing the tool against the older B01-21 QA worktree's default `Assets/` paths is intentionally invalid: it is not the current CHL producer source and will reject mismatched gate width.
The tests explicitly mutate and reject: obsolete 3.5m gate, moving CP2 or Finish, ramp lowering/increased lip, shift of bridge by1m, final platform shortened, absence of real guard, and altered original source. CI **must** match original producer exact SHA and **original source Git blobs** before compiling source evidence. Emits JSON and sha256, source-only logs, no fake Unity XML.

## Receiver, dependency and rollback
**REQUIRES:** CHL.C1 exact `077e...SOURCE_PASS` already verified, previous B01-21 gate-QA source complete. **PRODUCES:** 20 original source gate×rig×yaw rows, 23 real source-backed Python positive/negative tests, source seam and final-runout indicators. **BLOCKED_BY:** CHL.C3 true Unity licensed runner for physical confirmation (not for this source audit), CHL.C2 B01-40 independent current-head G0 status pending. **NEXT_UNBLOCKS:** B01-20/B01-60 can target limited P07 original real-drive recording near ramp exit Z19.36..19.50, CP2 Z21, CP3 Z29 and Finish Z38..40, with original root position, wheel contact points/normals, yaw, current speed, exact fixed-step count, event timestamps and genuinely credited CPs. Hold G2 until independent real G0+G1.

**Exclusive ownership:** all source/prototype/test files here live only under `Tools/B01/CHL003WorldQA/RouteClearance/**` plus own B01-21 CI; no CHL/R3/PARTSIZE/Codex owner source edits. **Rollback:** remove those files and own CI only. This PR is independent QA, not a third Unity package or producer rework.

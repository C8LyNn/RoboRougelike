# B01-21 | Reactive Queue C-DQ | exact-original CHL-003 aperture regression

TASK_ID `B01-21-C-DQ-NEWHEAD-GATE-20261027` | owner `B01-21` | QA_KIND `SOURCE` only.
Pinned original producer B01-20 [CHL Draft #285](https://github.com/C8LyNn/RoboRougelike/pull/285) **`f4ca318577edbdf4d003ab3e104ac7ec71d83cfe`**. Prior [QA PR #295](https://github.com/C8LyNn/RoboRougelike/pull/295) is a different immutable upstream pin `9a947108...`, therefore its whole-suite acceptance may not be claimed current; only unchanged geometrical values can be carried forward after exact-blob comparison.

## REQUIRES (checked, not assumed)
- C1 producer source+original artifact [run #40486705906](https://github.com/C8LyNn/RoboRougelike/actions/runs/40486705906) `SUCCESS`, same SHA, **24,056/24,056** actual original C# source checks, 58 original ZIP source entries and 57/57 original source hashes, but **Unity native NOT_RUN**.
- C2 B01-40 exact-source independent ZIP and recipient G0 **NOT_RUN at this head** (historical older SHA G0 cannot carry).
- C3 B01-60 native 18 Edit/13 Play and 3×Light +3×Heavy authentic full-body motor runs **BLOCKED_BY licensed Unity runner**, not PASS.
- CHL ownership **B01-20**; no source edits or competing Unity package. This is small independent deterministic feedback before G1.

## Actually run against real original production C# (NO fake Unity)
`GateGeometryReceipt.csproj` directly **compiles** the existing real original `Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003GateCrossing.cs` C# without modifications. The analytic caller supplies **source-measured** box/sphere footprint corners (0.82m wheel diameters, Heavy chassis2.50×3.40m wheel centers X±1.0/Z±1.15, Light1.45×2.30m X±.59/Z±.75). The ORIGINAL function rejects/accepts **policy** for explicitly supplied gate-local bounds and plane-crossing signs. This does **NOT** invoke actual Unity `Collider.bounds`, non-kinematic motor, trigger messages or PhysX. CI pins the five original producer Git blobs and runs the real production C# verdicts on Ubuntu+Windows, with separate JSON consumer mutation negatives.

- Original physical bridge width **3.70m**, **CHL checkpoint trigger aperture width3.50m**, original `CHL003GateCrossing` margin .25m/side.
- At yaw0° Heavy actual outer envelope2.82m. **Checkpoint allowable lateral center offset = ±0.09m** with guard. Bridge physical allowable center offset at same .25m side reserve = **±0.19m**. Thus center offset **+0.10m on exact straight Heavy**: safe by bridge review reserve, but ORIGINAL production C# `IsForwardFullBodyCrossing` returns **false** with the provided measured geometry. A fixed original gate at CP0=Z5 on 5.40m wide approach ground makes even greater difference.
- At centered Heavy yaw5°: actual original cuboid+spheres source projection ~3.01285m, gate reserve 0.24358m < required0.25 and bridge reserve0.34358m; ORIGINAL production gate returns false. Heavy allowable angle **~4.65336° at CP aperture** vs **~10.32962° at bridge** for centered vehicle with 0.25m each-side reserve.
- At yaw8° Heavy bridge side margin ~0.28968m, gate side margin ~0.18968m (also original production predicate rejects). Light still has margin ~0.65136m at checkpoint.
- This is **a definitive SOURCE-level difference between accepted geometric predicates** and a **native falsifiable gameplay issue candidate**, not direct evidence an actual demo rig got blocked or softlocked. No assertion that the stricter gate is unintended. B01-20/HQ decide whether precision is deliberate, fair/visible and recoverable; if accidental, only producer changes policy/geometry.

## Reproduce on pinned immutable stacked QA branch
```bash
dotnet run --project Tools/B01/CHL003WorldQA/DependencyQueue/GateGeometryReceipt.csproj -c Release -- /tmp/B01_21_GATE_POLICY_SOURCE.json
B01_21_CHL_DQ_RECEIPT=/tmp/B01_21_GATE_POLICY_SOURCE.json python3 -m unittest discover -s Tools/B01/CHL003WorldQA/DependencyQueue -p 'test_*.py' -v
```
The exact-source Git SHA/blobs upstream provenance stage happens first in GitHub Actions. The output JSON contains every original policy input/verdict and explicit `actualUnity=NOT_RUN`. NO produced Unity package, no fake physical drive or G1 PASS.

## ONE native receiver action (after native licensing)
Only B01-20/independent B01-60: in disposable licensed Unity6000.3.14f1 on **same exact producer SHA**, drive real Heavy with yaw0° and lateral center offsets 0/+.08/+.10m through the actual CP0/CP2 forward plane, then yaw4°/5°/8° centered, and Light yaw8°; three repeats each condition, capture original CHL `fixedStep, previousDepth, currentDepth, rootX, yaw, actual world Collider.bounds min/max, local gate box, wheel support, CP counter, Retry recoverability, completion, CONTACT CSV, Editor.log/NUnit XML`. No direct `core.EnterCheckpoint` injection or forced Completion. Let genuine real results decide if correction needed; if source head changes, mark this receipt `STALE_INPUT` and re-pin before consuming.

NEXT_UNBLOCKS: concrete B01-20 acceptance decision & B01-60 targeted negative before C3; **does not** unblock C2/C3/G2 itself. Rollback delete only this QA directory+its own workflow; old CHL source intact.

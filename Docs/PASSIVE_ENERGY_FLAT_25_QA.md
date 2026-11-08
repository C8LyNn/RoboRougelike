# Passive energy reservation: 25 per module

Base: `2edbed45` (Draft PR #1125). The user specified that passive modules reserve 25 energy each and explicitly retained 25 when active-use costs were halved. The previous tier surcharge made T1/T2/T3 reserve 25/30/35. The same real R3 model reproduced these values before the change.

The tier surcharge is now zero while the existing per-module reservation remains 25. This affects the garage model, the live player energy bank and the garage descriptions through their shared tier rule. Tier-specific effects and active-use costs are unchanged. Destroyed passive modules continue to release their reservation. Enemy energy-bank rules already used the flat per-module reservation and were not changed.

Verification on 2026-11-08:

- Roslyn runtime and editor/test assemblies compiled against local Unity references: exit 0 / 0. This is not a Unity compilation.
- Offline checks against the compiled real R3 model: 9/9 combinations of motor, regenerator and gyroscope at T1/T2/T3 each reserved and released 25 after destruction. A mixed-tier model reserved 75, and its T2 regenerator retained its existing 20 energy/s total regeneration. These checks do not construct a Unity vehicle or exercise EMP.
- Added focused model tests and updated the existing real-vehicle T2 test to expect the explicitly requested 25 reservation. Unity tests: NOT RUN. Actual live `VehicleEnergyBank`, physics, visual UI and Windows Player: NOT RUN under the lightweight-work constraint.
- Local ignored logs: `Artifacts/Local/CSharp-PassiveEnergy25-20261108/`.

Follow-up: run the targeted tests listed in `PENDING_TARGETED_QA_20261108.md` and manually compare T1/T2/T3 passive modules, live mounting, destruction and EMP on an isolated profile. No build was prepared.

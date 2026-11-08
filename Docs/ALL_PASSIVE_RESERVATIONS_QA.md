# Passive reservation: all current module kinds

Base: `3bcea415` (Draft PR #1127). This follow-up extends the existing focused model test from three passive kinds to all seven recognized by `IsPassiveEnergyPart`: suspension, gyroscope, bonus module, directional shield, independent steering, wheel motor and energy regenerator.

Verification on 2026-11-08: 21/21 offline combinations of those seven kinds and tiers T1/T2/T3 reserved 25 energy in the actual compiled R3 model and released it when the model part was marked destroyed. This constructs model parts directly; it does not validate physical placement, connectivity, EMP, device adapters, or the live Unity energy bank. The editor/test assembly compiled with Roslyn and existing local references (exit 0), against the runtime assembly from #1127. The extended Unity tests and Windows Player were NOT RUN under the lightweight-work constraint. Local ignored evidence: `Artifacts/Local/CSharp-AllPassiveKinds-20261108/`.

Follow-up: run the targeted Unity tests from `PENDING_TARGETED_QA_20261108.md`, including physical live-bank and destruction behavior. No build was prepared.

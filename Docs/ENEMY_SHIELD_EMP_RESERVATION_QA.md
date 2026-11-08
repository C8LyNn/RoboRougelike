# Enemy shield passive reservation during EMP

Base: `33075217` (Draft PR #1128).

Player passive reservation already remains while EMP disables the device. The enemy energy bank instead counted shields using `HardwareAvailable`, which explicitly excludes EMP-blocked devices, so an EMP could temporarily return the shield's 25-energy capacity. The enemy count now uses the same physical/living/attached hardware conditions without treating temporary EMP as physical removal. `HardwareAvailable` still requires both physical presence and absence of EMP, so device operation remains blocked during EMP. Destruction, detachment, disabling, owner loss, and core loss still invalidate physical presence.

Verification on 2026-11-08: runtime and editor/test assemblies compiled with Roslyn against existing local Unity references (exit 0 / 0). The cause and gate equivalence were checked in source. A real-enemy Unity test was added for a mounted shield before/during/after EMP and destruction, but NOT RUN. Actual EMP behavior, Player, physics and native Unity compilation: NOT RUN under the current lightweight-work constraint. Local ignored logs: `Artifacts/Local/CSharp-EnemyShieldEMPReservation-20261108/`.

Follow-up: run `RRLW6ShieldPhysicalTests.EnemyShieldKeepsPassiveReservationDuringEmpUntilDestroyed` and nearby shield/EMP regression tests. Manually confirm that the shield cannot operate under EMP, its reservation persists, and destroying/detaching it releases the reservation. No build was prepared.

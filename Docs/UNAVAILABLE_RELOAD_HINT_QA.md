# Reload hint when a device is unavailable

Base: `11366dcb` (Draft PR #1123).

Shotgun, railgun and point-defense HUD rows no longer suggest that T can reload an unavailable device. Their text uses the same device-availability gate as the existing reload methods and otherwise retains the full / reloading / T-to-reload statuses. An EMP-blocked weapon's existing EMP overlay remains authoritative. No weapon or reload mechanics changed.

Verification on 2026-11-08: runtime and editor/test assemblies compiled with Roslyn and local Unity references (exit 0 / 0). Five offline checks of the actual compiled label function passed, including unavailable, active reload, full and partially filled states. These do not simulate real devices or EMP. Five targeted Unity test cases were added but NOT RUN. Unity, Windows Player and visual input/device behavior: NOT RUN under the lightweight-work constraint. Local ignored evidence: `Artifacts/Local/CSharp-UnavailableReload-20261108/`.

Manual follow-up: equip a shotgun, railgun and point defense, spend ammunition, apply EMP while the devices are mounted, and compare the status and T behavior before, during and after disruption. Verify reload and ammo are unchanged.

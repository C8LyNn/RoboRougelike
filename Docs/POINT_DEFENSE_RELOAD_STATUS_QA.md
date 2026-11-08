# Point-defense reload status

Base: `1437ff0f` (Draft PR #1119).

Point-defense rows now use the same reload-state wording as manual-reload weapons: full, T to reload, or reloading. The existing display of operational state is retained; the underlying defense and reload behavior are unchanged.

Verification on 2026-11-08: runtime and editor/test assemblies compiled with Roslyn and existing local Unity references (exit 0 / 0). The existing device-page Unity test was extended to cover full, partial and reloading states, but was NOT RUN. Unity, Player, actual device behavior and visual display: NOT RUN under the lightweight-work constraint. Local ignored logs: `Artifacts/Local/CSharp-PointDefenseHUD-20261108/`.

Manual follow-up: mount point defense, observe a full magazine, consume ammunition and reload using T; verify the displayed state and the device's actual behavior.

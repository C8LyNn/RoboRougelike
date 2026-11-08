# Pickup grace lifecycle and reward input

Base: `3202a2ec` (Draft PR #1122).

After the last post-battle energy pickup disappears, the game could enter rewards while a positive pickup-grace deadline remained. With simulation paused, the first Enter in rewards was intercepted by the expired context's shortcut. The grace deadline is now cleared when opening salvage, and the remaining-grace property reports zero outside combat. Combat's existing timer and Enter-to-skip behavior remain unchanged.

Verification on 2026-11-08: runtime and editor/test assemblies compiled with Roslyn and local Unity references (exit 0 / 0). Six offline reflection checks of the actual compiled getter passed for Ready, Salvage, Garage, Won, Lost, and Combat with no active deadline. They do not simulate active Unity time or keyboard input. A focused Unity test was added covering disappearance of the last pickup and the first Enter in rewards; NOT RUN. Unity, Windows Player, and interactive gameplay remain NOT RUN under the current lightweight-work constraint. Local ignored logs: `Artifacts/Local/CSharp-PickupGraceLifecycle-20261108/`.

Manual follow-up: defeat the final enemy, collect the final energy cube before the timer expires, select a bonus and basket, and press Enter once. Confirm that rewards are accepted. Also check Enter still skips the pickup window while cubes remain.

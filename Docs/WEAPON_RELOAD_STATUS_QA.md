# Manual-reload weapon status

Base: `c68a98b8` (Draft PR #1118).

The shotgun and railgun HUD now displays "pełny" for a full magazine, "T przeładuj" when ammunition can be topped up, and "ładowanie" during a reload. Their magazine capacities and the rocket capacity displayed by the HUD come from their existing bindings. Gameplay and reload behavior are unchanged.

Verification on 2026-11-08: runtime C# compilation with Roslyn and existing local Unity references succeeded (exit 0). Four offline reflection checks of the compiled formatting function passed for full, partial, empty and reloading states; they do not exercise actual weapons or rendering. Unity, Windows Player and visual testing were NOT RUN under the lightweight-work constraint. Local ignored logs: `Artifacts/Local/CSharp-ManualReloadHUD-20261108/`.

Manual follow-up: equip a shotgun and railgun; observe a full magazine, fire a shot, press T, and check the displayed status throughout the reload. Confirm the rocket still reloads automatically and its capacity is correct.

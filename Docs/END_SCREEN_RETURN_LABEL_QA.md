# Podpis przycisku po wyprawie

Baza `e1ca1745` (Draft PR #1148). Ekran zwycięstwa i porażki opisuje istniejącą operację `RestartRun` jako „Wróć do menu”, ponieważ metoda przeładowuje scenę do menu, a nie rozpoczyna natychmiast nowej walki. Sterowanie Enter i logika restartu pozostają bez zmian.

- Kompilacja runtime Roslyn: **PASS**, kod 0; `Artifacts/Local/CSharp-EndScreenLabel-20261108/runtime.log`.
- Rzeczywisty ekran, Enter, restart i Player: **NOT RUN**. Bez nowego buildu.

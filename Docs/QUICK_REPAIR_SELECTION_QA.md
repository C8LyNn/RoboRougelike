# Wybór części po szybkiej naprawie

Baza: `a0c4d0b9` (Draft PR #1139). Gdy na szybkiej liście naprawiono grupę, a gracz jest w trybie montażu i nie miał wybranego egzemplarza, pierwszy naprawiony egzemplarz tej grupy jest wybierany do montażu. Nie zmienia to kosztu ani fizycznego montażu. Wybór nie jest nadpisywany, gdy istnieje już wybrana część, otwarto katalog lub gracz używa innego narzędzia.

- Kompilacja Roslyn runtime i testów Editor: **PASS**, kody 0/0; logi `Artifacts/Local/CSharp-QuickRepairSelection-20261108/`.
- Przygotowano test Unity: odbudowa wraku i wybór montażu oraz zachowanie trybu zaznaczania. Wykonanie **NOT RUN**. Widmo, faktyczne kliknięcia, działanie w grze i Windows Player: **NOT RUN**.
- Nie przygotowano buildu. Przy przyszłym teście sprawdź też przypadek, w którym inna część była już wybrana, i odczytaj komunikat garażu.

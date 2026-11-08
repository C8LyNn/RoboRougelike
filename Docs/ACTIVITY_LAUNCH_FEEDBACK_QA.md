# Komunikaty uruchamiania aktywności

Baza `5f582288` (Draft PR #1145). Panel wyboru aktywności pokazuje aktualny powód odrzucenia wygenerowanej areny, wyścigu, trybu drogowego, areny LD02 lub obrony reaktora. Jeśli operacja nie podała szczegółu, wyświetlany jest komunikat ogólny; poprzedni komunikat garażu jest czyszczony przed próbą, aby nie został błędnie przedstawiony jako nowy powód. Przy powodzeniu poprzedni komunikat znika. Nie zmieniono kryteriów wyboru ani generatorów map.

- Kompilacja Roslyn runtime i testów Editor: **PASS**, kody 0/0, logi `Artifacts/Local/CSharp-ActivityLaunchFeedback-20261108/`.
- Skompilowana logika wyboru tekstu: **4/4** poza Unity. Przygotowany test z kontrolowaną funkcją zastępczą nie jest testem rzeczywistej aktywności.
- Testy Unity, faktyczne uruchomienie/odrzucenie aktywności, renderowanie obu układów oraz Windows Player: **NOT RUN**. Test GUI powinien objąć zbyt duży pojazd na LD02, niesprawną trasę, powtórzenie tej samej odmowy i udany start; użyć profilu QA.
- Bez nowego buildu.

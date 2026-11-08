# Czytelne przyciski naprawy zbiorczej

W ustawieniach garażu obydwa układy ekranu zamiast nieaktywnej ceny `0 Z` wyświetlają stan napraw: sprawny pojazd, brak złomu lub niedostępną naprawę. Odbudowa awaryjna pokazuje również, gdy jest niepotrzebna, niedostępna albo ile złomu brakuje. Dostępność przycisków, cena, ekonomia i sam algorytm naprawy pozostały bez zmian. Naprawa zbiorcza dotyczy zamontowanych części; stan magazynu nie jest przedstawiany jako stan całego pojazdu.

Kod gry i testów skompilowano lokalnie przez Roslyn: exit 0/0. Osiem wywołań skompilowanego kodu i modelu pojazdu w PowerShell/.NET: **8/8 PASS** — sprawny, uszkodzony bez złomu, brak dostępnej naprawy, cena oraz cztery stany odbudowy. Wyniki: `Artifacts/Local/CSharp-BulkRepairLabels-20261108/results.json`.

Unity, Windows Player oraz wizualne sprawdzenie szerokości napisów: **NOT RUN**, ponieważ bieżące zadanie ograniczono do lekkich prac bez przejmowania komputera. Build `c332e6a5` nie zawiera tej poprawki.

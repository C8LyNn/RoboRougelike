# Powody odrzuconego zakupu części

Baza `2057798b` (Draft PR #1143). Gdy zakup zostanie odrzucony, garaż i obie wersje katalogu wyświetlają rozpoznaną przyczynę: niedostępny garaż, bonus tylko z nagrody, zablokowaną technologię, pełny magazyn, brak złomu lub brak wymaganego modelu koła/pancerza. Jeżeli nie można ustalić przyczyny, pozostaje ogólny komunikat. Przy zakupie pięciu, gdy zawiedzie pojedynczy zakup, nadal podawana jest faktycznie zakupiona liczba i koszt. Nie zmieniono walidacji, cen ani zawartości magazynu.

- Kompilacja Roslyn runtime i testów Editor: **PASS**, kody 0/0; `Artifacts/Local/CSharp-PurchaseReasons-20261108/`.
- Poza Unity rzeczywista skompilowana logika podstawowych powodów: **7/7** przypadków.
- Odczyt modeli przez Unity Resources, rzeczywiste odrzucone zakupy, częściowy zakup pięciu, GUI oraz Windows Player: **NOT RUN**. Nowe testy Unity są przygotowane, nie wykonane.
- Bez nowego buildu. Przy przyszłym teście użyj profilu QA i sprawdź oba układy, brak środków, pełny magazyn oraz brakujący model wyłącznie w izolowanej kopii projektu.

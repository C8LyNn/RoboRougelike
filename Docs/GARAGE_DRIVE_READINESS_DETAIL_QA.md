# Dokładniejsze komunikaty gotowości w garażu

Baza `448f41f1` (Draft PR #1144). Przy braku wybranego napędu komunikat opisuje wymagania właściwe dla kół, gąsienic, Omni albo Hover, zamiast sugerować gąsienice również dla innych napędów. Gdy rdzeń jest zniszczony, widoczne ostrzeżenie podaje ten powód przed wtórnymi brakami niepołączonych elementów. Zasady dopuszczenia do aktywności i fizyka nie zostały zmienione.

- Roslyn runtime i kompilacja testów Editor: **PASS** 0/0; logi `Artifacts/Local/CSharp-DriveReadinessFinal-20261108/`.
- Skompilowane teksty wymagań czterech napędów: **4/4** poza Unity (`Artifacts/Local/CSharp-DriveReadinessDetail-20261108/offline-results.json`).
- Próba uruchomienia pełnej gotowości modelu poza Unity trafiła na zależność natywną (`ECall methods must be packaged into a system module`); nie jest to wynik testu gry. Testy Unity czterech napędów, rdzenia i istniejących ostrzeżeń: **NOT RUN**. Rzeczywisty GUI i Player: **NOT RUN**.
- Brak nowego buildu. Przy przyszłym QA sprawdź wszystkie wybrane napędy, brak broni i napędu jednocześnie, zniszczony rdzeń oraz czy komunikat mieści się na ekranie.

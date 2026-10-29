# B01-12-SNAPGRID-001 / R3 — dostępny demontaż (staged candidate)

## Cel i status

Minimalna poprawka kodu B01-12 w istniejącym prototypie R2. Historyczny **R2 Draft #376** HEAD `2c358806e38a6b5a19f03d97ad69d3484f6cc5ff` i HTML Git blob `1ee408a1532d07c3a83437780e2d87174a6b2656` pozostają nienaruszone. R3 jest oddzielną rewizją proponowaną w stacked Draft PR, **nie przyjętą przez Codexa**. R2 test producenta 152/152 PASS, ale zawiera udokumentowany błąd dostępności.

## Naprawa źródła

W R2 `aria-label` dla poprawnego demontażu opisuje akcję jako „montaż możliwy” (4/4 rzeczywiste szerokości 320/768/1280/1920). W R3 przy narzędziu **Demontaż** czytnik ekranu otrzymuje „demontaż możliwy”. Nieprawidłowa operacja otrzymuje „operacja zablokowana”. Po powrocie do narzędzia montażu komunikat brzmi „montaż możliwy”. Ponadto footer wcześniej wyświetlający R1 teraz pokazuje R3. Nie zmieniono renderingu kolizji, weryfikacji `connected`, schematu JSON ani istniejącej logiki Undo/Redo i unieważniania eksportu R2.

## Testy oryginalnych bajtów

Wymagania: Python 3, Playwright, Chromium `/usr/bin/chromium` (Linux). W katalogu paczki:

```bash
python3 test_a11y_r3.py
python3 test_snapgrid.py
```

`test_a11y_r3.py`: **52/52 PASS**, 0 JS exceptions. Oryginalne R2 jest odtworzone dokładnie z wersji R3 przez odwrócenie jednej poprawki, *jego Git blob SHA-1 jest weryfikowany*; na każdej z czterech szerokości test obserwuje wcześniejszy błędny komunikat, następnie właściwe ARIA/AX, klawiaturę, blokadę usunięcia mostu, poprawne ostrzeżenie przy montażu, brak poziomego scrollowania i R2 export-invalidation. `test_snapgrid.py` to **niezmieniony oryginalny skrypt R2**, jego Git blob `d86cb982c91f7842ffc31d48ccc9a159887b43e1`, odtwarzany na nowym źródle **152/152 PASS**. Oba skrypty sprawdzają pełny SHA256 źródła z manifestu i kończą się `exit 2` po zmianie danych przed uruchomieniem Chromium. To wyłącznie browser/source QA, **nie Unity/PhysX/Player**. Rzeczywisty screenshot `snapgrid-r3-1280x720.png` i JSON `a11y-results.json` są artefaktami lokalnymi, nie dowodem integracji.

## Granice własności i zależności

Zmiana wyłącznie we własnych plikach `Research/B01-12/P17W02/SnapGrid001/**`, w osobnym stacked Draft PR bazującym na niezmiennym R2; żadnych `Assets/**`, CHL, PARTSIZE, gałęzi `main/develop` i plików Codexa. Kontrakt 2D footprint i JSON to narzędzie ocen UX, nie rzeczywista mechanika socketów 3D, ekonomii ani save/load. Native Unity G1 **NOT_RUN**, niezależne B01-40/B01-60 **PENDING**, CODEX_ACK **PENDING**, G2 **HOLD**.

## Import i rollback

Odbiorcy: **B01-40** sprawdza 2-zmianowy diff HTML, test/manifest, scope/G0; **B01-60** niezależnie uruchamia oryginalne testy na nowym SHA i waliduje AX komunikaty; **B01-00** decyduje o kolejce #79. Codex może dopiero po ACK podjąć decyzję o adaptacji zmiany do interfejsu gry. Samodzielny podgląd: otwórz `snapgrid.html` w przeglądarce. Rollback: odrzuć stacked Draft PR R3, pozostawiając R2 bez zmian. Żadnych migracji, skryptów instalacyjnych, merge ani zmian w runtime.

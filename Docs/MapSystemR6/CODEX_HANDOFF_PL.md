# CODEX — MAP-SYSTEM R5 / TEST & INTEGRATION HANDOFF

**PROJECT:** RoboRougelike, Unity 6000.3.14f1 URP
**MODE:** isolated integration and evidence-gathering; NO merge to `main`; do not modify MOV packages

## Cel

Zweryfikuj i uruchom w lokalnej kopii Unity paczkę MAP-SYSTEM R5. Na początku wykonaj import i kompilację; NIE łącz jeszcze z produkcyjnym garażem i systemem Gauntlet.

## Krok po kroku

1. Sprawdź istniejące foldery `Assets/RoboRougelike/MapSystemR4`. Jeśli R4 już jest, podmień jego katalog na wersję R5 zamiast kopiować klasy równolegle.
2. Zarejestruj dokładny HEAD SHA i diff (jeśli repo jest już dostępne).
3. Zaimportuj sample Resources oraz kod C# z `.meta`.
4. Uruchom Unity EditMode tests: stare R4 i nowe R5; zapisz nazwy testów i rzeczywiste wyniki.
5. Uruchom PlayMode tests, w tym blokadę obwiedni pojazdu na warstwie przeszkód.
6. Na pustej scenie użyj `Tools > RoboRougelike > MAP R5 > Create Demo In Current Scene`.
7. Sprawdź TimeAttack/Survival/Escape/Boss — pełne AI i boss nadal NIE są zintegrowane.
8. Ręcznie przejedź zastępczym pojazdem trasy R5; zanotuj problemy na węzłach i pochyłościach.
9. Skonfiguruj `traversalObstacleLayers` na dedykowanej warstwie **tylko przeszkód**; uruchom kontekstowe `R5 / Audit obstacle layers`.
10. Następnie przeprowadź próbę rzeczywistym pojazdem w izolowanej scenie. Sprawdź promień skrętu, nierówności, spadanie z drogi, moment obrotowy i kontakt kół.
11. Dopiero po testach fizycznych zbuduj raport `MAP_R5_UNITY_QA_REPORT.md`.

## Raport obowiązkowy

- SHA źródeł i wersja Unity;
- liczba wszystkich uruchomionych testów; ich rzeczywiste PASS/FAIL/SKIP;
- log kompilacji, log PlayMode, screenshoty lub krótki opis ręcznej inspekcji;
- czy G1b była skonfigurowana (bez warstwy to NOT RUN);
- faktyczne miejsce utknięcia lub nieprzejezdności (jeśli wystąpiło);
- lista blokad dla G1c i kolejnej fazy R6.

**ZAKAZY:** bez force-push, bez merge do main, bez publikacji builda, bez zmian zasad obecnego Gauntletu.

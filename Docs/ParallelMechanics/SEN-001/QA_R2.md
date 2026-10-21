# SEN-001 — QA R2 / 2026-10-22

## Zmiany
- Dodano filtrowanie rodzaju sygnału `ScannerSignalMask` (None, Unknown, Enemy, Salvage, Objective, Hazard, All), niezależne od LayerMask.
- `ScannerPulse.ConfigureSignals` i `DetectableSignals` udostępniają konfigurację filtra; domyślnie All zachowuje zgodność R1.
- Skaner ignoruje nieprawidłowe wartości rodzaju sygnału; nigdy nie modyfikuje obiektów ani ich markerów.
- `IsRevealed` nie pokazuje nieaktywnego/wyłączonego markera; `PruneExpired` usuwa takie wpisy.
- Rozszerzono EditMode z 6 do 9 przypadków; PlayMode pozostaje 3. Brak zmian scen, istniejących prefabów i integracji.

## Weryfikacja
- Inspekcja zmian w przydzielonych katalogach: statyczna, nie jest testem Unity.
- Unity 6000.3.14f1 EditMode: **NOT RUN** — brak uruchomionego Unity i logów XML dla tego commita.
- Unity 6000.3.14f1 PlayMode: **NOT RUN** — brak uruchomionego Unity i logów XML dla tego commita.
- Build Windows: **NOT RUN**.
- Integracja z prawdziwym pojazdem: **NOT INTEGRATED**, czeka na rzeczywiste Unity QA.

## Ograniczenia i następny etap
- QueryCapacity=128 może obciąć wyniki; `LastPulseTruncated` sygnalizuje potencjalnie niekompletny wynik.
- `Revealed` to żywa lista aktualizowana w Update / TryPulse; po dezaktywacji markera `IsRevealed` reaguje natychmiast.
- Uruchomić EditMode, PlayMode i build w izolowanym checkout Unity 6000.3.14f1, przeanalizować XML/logi.
- Dopiero po PASS i sprawdzeniu kontraktu pojazdu rozważyć opcjonalny adapter wyłącznie w katalogu SEN-001.

Nie scalać do develop/main bez QA i zgody koordynacji.

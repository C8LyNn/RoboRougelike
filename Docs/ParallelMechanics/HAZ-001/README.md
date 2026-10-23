# HAZ-001 — telegrafowane zagrożenia areny (R1, isolated)

**Repo:** C8LyNn/RoboRougelike · **Issue:** #7 · **PR:** #12 (DRAFT → develop)  
**Unity:** 6000.3.14f1. **Status:** source implemented; Unity EditMode / PlayMode / Windows build **NOT RUN**; game integration **NOT INTEGRATED**.

## Zakres
- Ręczne `Begin()` uruchamia cykl **Idle → Telegraph → Active → Cooldown → Idle**. `ResetCycle()` cicho anuluje cykl, także po wyłączeniu obiektu.
- Parametry: czas ostrzeżenia/aktywności/chłodzenia, promień, interwał trafień, żądana wartość obrażeń, maska warstw i polityka triggerów.
- `DamageRequested(GameObject target, float amount)` to **zdarzenie**, nie rzeczywiste obrażenia. Brak subskrybenta = brak obrażeń. Brak zmian HP, AI, pojazdu, scen i systemu walki.
- Detekcja `Physics.OverlapSphereNonAlloc` z buforem 64 colliderów; deduplikacja po Rigidbody lub obiekcie Collider, limit częstotliwości na cel; `LastScanTruncated` sygnalizuje osiągnięcie limitu (możliwa utrata części celów).
- `HazardRingIndicator` to opcjonalny runtime'owy okrąg: żółty = Telegraph, czerwony = Active; tylko lokalny obiekt potomny, bez trwałego zapisu w scenie.
- `HazardDemoHarness` uruchamia cykle i wypisuje **REQUEST** w konsoli. Nigdy nie uszkadza celu.

## Demonstracja opt-in
W Unity Editor (po imporcie i kompilacji): **Tools → RoboRougelike → HAZ-001 → Create Additive Demo**. Tworzy nową **niezapisaną scenę addytywną**, bez modyfikacji `Assets/scene 1.unity`. Po wejściu w Play pojawia się widoczny okrąg i w konsoli prośby o obrażenia dla demonstracyjnej kuli na warstwie 8. Demo jest oddzielne i nie stanowi integracji gry. Można też dodać `TelegraphedHazardZone` i opcjonalny `HazardRingIndicator` do własnego obiektu w scenie testowej, a następnie wywołać `Begin()`.

## Testy (źródła, bez deklaracji PASS)
- `Tests/EditMode/HazardZoneEditModeTests.cs`: 8 przypadków (timeline, overshoot, reset, niepoprawny czas, disable, sanity).
- `Tests/PlayMode/HazardZonePlayModeTests.cs`: 4 przypadki (rate limit, warstwy, wiele colliderów, disable).
- Oddzielne asmdef dla runtime, Editor, EditMode i PlayMode. Każdy nowy asset ma własne `.meta`; zachowano oryginalny GUID pliku `TelegraphedHazardZone.cs`.
- Na oddzielnym checkout z aktywowanym Unity: `Tools/CI/Run-Unity.ps1 -Task EditMode`, `-Task PlayMode`, `-Task Build`, oraz `python Tools/CI/validate_repository.py`. W CI bez aktywacji Unity: **NOT RUN**, nie PASS.

## Granice i integracja
Wszystkie zmiany są w `Assets/RoboRougelikeMechanics/HAZ001TelegraphedHazards/**` i `Docs/ParallelMechanics/HAZ-001/**`. Nie edytowano `Assets/scene 1.unity`, R1/R3/R4/R6, MOV, ProjectSettings, Packages ani plików PR #1–#6. Brak automatycznej aktywacji, brak auto-merge. Integrator może podłączyć `DamageRequested` do autorytatywnego systemu obrażeń w **osobnym, uzgodnionym PR**, dopiero po testach Unity. W przypadku 64+ colliderów na skan należy rozszerzyć politykę buforowania i przetestować wydajność.

**Kolejny etap:** rzeczywisty compile/test Unity 6000.3.14f1, oględziny ostrzegania i kolizji w PlayMode, przegląd wydajności i dopiero później bezkolizyjny adapter gameplay.

# SEN-001 — Scanner Pulse Sensor (R2: signal filtering + reveal validity + tests)

Issue: https://github.com/C8LyNn/RoboRougelike/issues/10 · Draft PR: https://github.com/C8LyNn/RoboRougelike/pull/15

## Mechanika i kontrakt

Unity **6000.3.14f1**. Manualny, **opt-in**, read-only skaner 3D: promień, maska warstw, QueryTriggerInteraction, cooldown, tymczasowe wykrycia, deduplikacja colliderów jednego obiektu i ignorowanie colliderów własnej hierarchii. Wykrywane są wyłącznie obiekty z włączonym `ScannerDetectable` na obiekcie lub rodzicu. Typ sygnału: Unknown / Enemy / Salvage / Objective / Hazard. R2: `ScannerSignalMask` umożliwia niezależne od warstw filtrowanie typów sygnału (domyślnie `All`); `ConfigureSignals(mask)` zmienia filtr bez modyfikacji celów. Właściciel markera może skonfigurować rodzaj sygnału przez Inspector lub `ScannerDetectable.SetSignalKind(kind)`. Nieprawidłowe wartości enum są ignorowane. Nie ingeruje w HP, AI, nagrody, spawn, materiały, kolizje ani sterowanie pojazdem. Nie ma automatycznego keybindu.

`ScannerPulse.TryPulse(Vector3 worldPosition)` zwraca liczbę unikalnych oznaczonych obiektów albo **-1**, jeśli cooldown/disabled. `LastHits` zawiera reprezentatywny collider każdego obiektu z ostatniego skutecznego impulsu. `Revealed` zawiera aktualne oznaczenia; wygasłe usuwane są w `Update`, a `IsRevealed(marker)` zawsze sprawdza czas. `OnDetected` wywoływane dla każdego unikalnego wykrycia podczas impulsu. R2: `IsRevealed` natychmiast zwraca false dla nieaktywnego lub wyłączonego markera; `Revealed` usuwa takie markery podczas następnego `Update` lub `TryPulse`, bez zapisywania czegokolwiek do obiektu. `ResetScanner()` czyści cooldown, listy i oznaczenia. `Configure(radius, cooldown, reveal, layers)` pozwala konfigurować instancję bez prefabów.

**Granica wyników:** fizyka używa stałej tablicy 128 colliderów i `Physics.OverlapSphereNonAlloc`; `LastPulseTruncated == true` oznacza, że osiągnięto pojemność, a wynik **może być niekompletny**. Nie obiecujemy wyboru najbliższych 128 obiektów. Wiele colliderów jednego obiektu liczy się raz. Wyłączony marker lub brak markera jest ignorowany. Przez `Time.time` pauza przy `timeScale = 0` zatrzymuje cooldown i czas oznaczeń.

## Demonstracja (bez edycji głównej sceny)

1. Utwórz **osobną** scenę sandboxową w swoim checkout (nie commituj jej do PR bez zgody).
2. Utwórz pusty GameObject, dodaj `ScannerPulseDemo` (automatycznie dodaje `ScannerPulse`). Ustaw promień, cooldown i czas oznaczeń.
3. Dodaj 2–3 GameObjects z `SphereCollider` i `ScannerDetectable`; ustaw je wewnątrz promienia. Jeden umieść na wykluczonej warstwie.
4. W Play kliknij przycisk **Pulse at this object** w oknie Game. Zobacz liczbę wykryć, pozostały cooldown i aktywne oznaczenia. W Scene View zaznacz skaner, aby zobaczyć promień (Gizmos).

To **nie jest** podłączenie do istniejącego Gauntlet. Opcjonalny adapter do prawdziwego pojazdu wymaga oddzielnej weryfikacji kontraktów, testów i zatwierdzenia.

## Testy i dowody

`Tests/EditMode/ScannerPulseEditModeTests.cs` — 9 testów: odczytowość, deduplikacja, warstwy, własna hierarchia, cooldown/reset, wyłączony marker, filtrowanie typów, maska pusta/Unknown, natychmiastowe ukrycie i usunięcie wyłączonego markera.
`Tests/PlayMode/ScannerPulsePlayModeTests.cs` — 3 testy: wygasanie oznaczenia, cooldown, wyłączenie komponentu.

Weryfikacja na Windows w osobnym checkout Unity 6000.3.14f1: `Tools/CI/Run-Unity.ps1 -Task EditMode`, `-Task PlayMode` oraz `-Task Build`. Przed akceptacją wymagane XML/logi i rzeczywiste PASS. **Stan obecny: Unity EditMode/PlayMode/Build NOT RUN (również dla R2)** (kod i testy dodane, bez dowodu wykonania). Nie utożsamiać CI-01 ani kompilacji statycznej z testami fizyki.

## Granice integracji

Jedyna własność SEN-001: `Assets/RoboRougelikeMechanics/SEN001ScannerPulse/**` i `Docs/ParallelMechanics/SEN-001/**`. Bez zmian istniejącej sceny, R1/R3/R4/R6, MOV, ProjectSettings, Packages, PR Codexa #1 i MAP/COM/VFX. Branch `feature/rrl-sen001-isolated-pack` → **DRAFT PR do develop**, bez merge.

# Energia pojazdów i moduły 1×1×1 — raport testowy

Baza: PR #934, `e6e5aadd006b9f78350a3bc5d537b85b65cb2fe7`. Kod buildu: `2a6d5a4c2e8914857499126d08a113149d325867`. Gałąź: `feature/unified-energy-unit-parts`.

## Zasady

- Każda część zajmuje jedną komórkę konstrukcyjną siatki 0,55 m; obrót nie zwiększa zajętości. Ruchome elementy, takie jak lufy i koła, mogą wizualnie wystawać poza tę komórkę.
- Rdzeń daje 500 pojemności; każda sprawna, dołączona bateria dodaje 100. Nowa pojemność nie jest automatycznie wypełniana.
- Każde sprawne, zamontowane zawieszenie, żyroskop, moduł bonusowy, tarcza kierunkowa i niezależny skręt rezerwuje po 25 energii z maksimum. Zniszczenie lub odłączenie baterii ogranicza zapas do nowego maksimum.
- Regeneracja wynosi 5/s. Turkusowe pole na podłożu dodaje 25/s; ładuje każdy znajdujący się w nim pojazd, również wroga, tylko raz na krok fizyki.
- Przykładowe koszty: zwykły strzał 5, shotgun 10, railgun 18, rakieta 15, EMP 25, laser i piła 12/s, booster 15, skok 25, Blink 30, zaczep 12. Koszty i tempo regeneracji poza wartościami zadanymi przez użytkownika są wstępne i wymagają oceny balansu.
- Ostatni przeciwnik w starciu ma fizyczną baterię, którą można uszkodzić i odzyskać jako część. Wyświetlane wartości są wspólne dla broni i urządzeń danego pojazdu.

## Wyniki rzeczywiste

- Unity 6000.3.14f1, zbiorczy EditMode przed ostatnimi zmianami tekstów i koloru: **89/89 PASS**, exit 0. Obejmuje rzeczywistą konstrukcję, bronie, moduły ruchu, nagrody i zmieniony obrys.
- Unity, celowany EditMode po zmianach interfejsu: **14/14 PASS**, exit 0; obejmuje 500/+100/-25, zapis energii, baterię gracza i wroga, zniszczenie baterii, pole ładowania, zużycie różnych typów broni, lasera i piły.
- Unity, pełny PlayMode po zmianach interfejsu: **478/478 PASS**, exit 0.
- Windows x64, kompilacja **PASS**, exit 0, błędy 0, ostrzeżenia 2.
- Wypakowany Windows Player, test API czterech rodzajów broni i blokowania strzału przez własny pancerz: **PASS**, 4 zablokowane i 4 dozwolone, exit 0.
- Wypakowany Windows Player, test API: garaż, dwie walki, fizyczny strzał przeciwnika, dwie wygrane i nagrody oraz zachowanie ID części: **PASS**, exit 0.
- Test ręczny grafiki, odczucia gry i pełnej wyprawy: **NOT RUN**. Testy Playera były sterowane API w trybie bez okna, nie są ręczną rozgrywką.

Pliki XML, logi i kody zakończenia zapisano lokalnie w `Artifacts/Local/EnergyUnit/`. Stary build `Builds/FULL-MERGE270` zachowano i przywrócono po kompilacji.

## Build i instrukcja

`Builds/RoboRougelike_Energy_UnitCells_TEST_Windows_x64.zip` — 39 643 976 bajtów; SHA-256 `5d6ed76d983f0407c24c9989ef803354cd44ada32f4bf99cc831fa279d421d2c`.

Wypakuj całe archiwum, uruchom `RoboRougelike.exe`, wejdź do garażu, przejdź strzałką w katalogu części na stronę 5/5 i zamontuj baterię. Wskaźnik energii znajduje się w garażu i walce. Podczas aktywności znajdź turkusowe pole w pobliżu startu i wjedź na nie po zużyciu energii.

## Ograniczenia

Starsze konstrukcje, których połączenie opierało się wyłącznie na dodatkowych komórkach dawnego modułu 1×2×2, mogą nie przejść walidacji przy wczytaniu po zmianie na 1×1×1. Nie przygotowano automatycznej migracji takich układów; należy zachować kopię starego zapisu. Integracja kanonicznego systemu zapisu z wcześniejszych paczek pozostaje poza tym testem. Widoczność pola w każdym wariancie mapy, dokładny balans kosztów i pełna sesja gracza wymagają ręcznej weryfikacji.
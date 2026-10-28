# B01-25 / PASS 15 — LD02: wybór osłony (jeden nowy poziom)

**Produkt:** [LD02_COVER_CHOICE.json](LD02_COVER_CHOICE.json), oryginalny format `MapDocument v2` obsługiwany przez obecny edytor Unity. **Podgląd z tych samych danych:** [LD02_COVER_CHOICE.svg](LD02_COVER_CHOICE.svg). W pliku gry **nie ma** przyszłych wrogów, waypointów, nagród ani nieobsługiwanych pól metadanych.

## Co poprawiono wobec [PASS14 LD02_ARENA](https://github.com/C8LyNn/RoboRougelike/blob/eecdc5ab9f20cad2f47dd7a139c9cb96981d0c3e/Research/B01-25/P14/LD02_ARENA.json)

Zachowano 120 pól Ground, cały obwód 40 Wall, Spawn (2,5) i Finish (9,5). Usunięto sześć wcześniejszych elementów osłon (4 Pillar + 2 LowBarrier) i rozstawiono sześć nowych. **Dodano pięć stałych ścian wewnętrznych**: `Wall(x=4..8,z=4,rotation=0)`. W efekcie powstała taktyczna alternatywa:
- **Skrót odsłonięty:** `(2,5)→(3,5)→(4,5)→(5,5)→(6,5)→(7,5)→(8,5)→(9,5)` = **7** przejść między polami.
- **Obejście za osłoną:** `(2,5)→(2,4)→(2,3)→(2,2)→(3,2)→(4,2)→(5,2)→(6,2)→(7,2)→(8,2)→(9,2)→(10,2)→(10,3)→(10,4)→(10,5)→(9,5)` = **15** przejść; 8 więcej, nie jest to pomiar czasu jazdy.

**Kontrakt geometryczny widoczności (bez AI):** hipotetyczny punkt obserwacji (9,6), 36m ×24m w świecie. Na wysokości 1,5m, na podstawie dokładnie podanych przez oryginalny `MapWorld.CreateItem` wymiarów Wall `4×4×0.5m`, w teście przecięcia promienia z prostokątem XZ: pięć komórek (3..7,2) ma zasłoniętą linię do hipotetycznego punktu, sześć komórek (3..8,5) na skrócie jej nie ma. **To tylko dowód geometrii brył ścian**. Przeciwnik ani jego `LOS` nie jest elementem JSON; obecnie brak tu celu „dwa zabicia”.

## Jedna mapa, zachowana historia zmian

| Rodzaj | PASS14 | PASS15 | Delta |
|---|---:|---:|---:|
| Ground | 120 | 120 | 0 |
| Wall obwodowe | 40 | 40 | 0 |
| Wall wewnętrzne | 0 | 5 | +5 |
| Pillar | 4 | 4 | nowe pozycje |
| LowBarrier | 2 | 2 | nowe pozycje |
| Spawn / Finish | 1 / 1 | 1 / 1 | 0 |
| Łącznie | **168** | **173** | **+5** |

Dodatkowe pozycje: Pillar `(4,1),(7,1),(4,7),(7,7)`; LowBarrier `(5,7),(8,7)`. Każdy element ma `scaleX/Y/Z=1` i wszystkie współrzędne należą do obsługiwanego przez `MapDraft` zakresu.

## Faktyczne wykonane kontrole danych

**19/19 PASS w izolacie V8** na oryginalnych bajtach JSON z GitHuba i oryginalnych tekstach kodu `MapDraft.cs` (Git blob `b5f8f395f93972c0ede000701dbbada8fcfcdfdb`) oraz `MapWorld.cs` (blob `8928908db123b35dcab4b324fe9d3a7a8fceee4e`) w aktualnym publicznym Codex [#310](https://github.com/C8LyNn/RoboRougelike/pull/310) `231005c6d0351945a8ee6684f3856af118f0e94f`. Sprawdzono typy, liczby, granice, unikalność footprintów, zachowanie obwodu, obie kompletne trasy, brak przecięcia ich wnętrz, najkrótszą drogę i pięć zasłoniętych / sześć odsłoniętych punktów w przyjętej projekcji XZ. Nie wykonywano oryginalnego C#, Unity ani PhysX.

**Do przetestowania w istniejącym [CV-05](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223), bez drugiego zadania:** w izolowanym profilu skopiuj plik do `Application.persistentDataPath/Maps/B01_25_LD02_COVER_P15.json`; wpisz w edytorze nazwę `B01_25_LD02_COVER_P15` i wybierz `Wczytaj`. Nigdy nie nadpisuj rzeczywistych map (D02). Oczekiwane minimum:
1. `TryLoad=true`, `TestError=null`, 173 elementy (120 Ground, 45 Wall, 4 Pillar, 2 LowBarrier, 1 Spawn, 1 Finish), `globalFloor=false`.
2. Na naprawdę działającym Light i Heavy da się niezależnie przejechać obie trasy lub wydać prawdziwy **FAIL** wraz z pomiarem `Collider.bounds`, prześwitu, trajektorii, zderzeń i `Editor.log`. **Nie** zakładaj, że 4m grid dowodzi bezpiecznej szerokości.
3. Render/PhysX potwierdza ciągłość wewnętrznego muru, a hipotetyczne zasłonięcie odpowiada rzeczywistemu `Physics.Raycast` wyłącznie jeśli operator taki pomiar przeprowadzi. Nagroda, „2 kills”, faktyczny LOS/ogień i osobne zatwierdzenie trasy **UNSUPPORTED / NOT_RUN**.
4. Meta w obecnym `MapWorld.MarkFinish` oznacza **SANDBOX_TOUCH_ONLY**, nie combat victory, nie reward. Zachowaj źródłowe SHA, autentyczne NUnit XML/log, wyjście procesu, obrazy i oryginalne dane jazdy. Oddzielna niezależna bramka CHL/PARTSIZE G1 pozostaje nienaruszona.

**Owner:** B01-25 autor poziomu; B01-21 fizyczna topologia/clearance; B01-10 sprawiedliwość decyzji ryzyka; B01-24 combat LOS; Codex kanoniczna implementacja; B01-60 realny niezależny odbiór tylko po zgodzie HQ. Własność `Assets/**` bez zmian, **no merge / no G0-G1 upgrade**.

# B01-25 / PASS 15 — LD02: wybór osłony (jeden nowy poziom)

**Produkt:** [LD02_COVER_CHOICE.json](LD02_COVER_CHOICE.json), oryginalny format `MapDocument v2` obsługiwany przez obecny edytor Unity. **Podgląd z tych samych danych:** [LD02_COVER_CHOICE.svg](LD02_COVER_CHOICE.svg). W pliku gry **nie ma** przyszłych wrogów, waypointów, nagród ani nieobsługiwanych pól metadanych.

## Co poprawiono wobec [PASS14 LD02_ARENA](https://github.com/C8LyNn/RoboRougelike/blob/eecdc5ab9f20cad2f47dd7a139c9cb96981d0c3e/Research/B01-25/P14/LD02_ARENA.json)

Zachowano 120 pól Ground, obwód 36 Wall + 4 Platform z naprawionymi narożnikami, Spawn (2,5) i Finish (9,5). Usunięto sześć wcześniejszych elementów osłon (4 Pillar + 2 LowBarrier) i rozstawiono sześć nowych. **Dodano pięć stałych ścian wewnętrznych**: `Wall(x=4..8,z=4,rotation=0)`. W efekcie powstała taktyczna alternatywa:
- **Skrót odsłonięty:** `(2,5)→(3,5)→(4,5)→(5,5)→(6,5)→(7,5)→(8,5)→(9,5)` = **7** przejść między polami.
- **Obejście za osłoną:** `(2,5)→(2,4)→(2,3)→(2,2)→(3,2)→(4,2)→(5,2)→(6,2)→(7,2)→(8,2)→(9,2)→(10,2)→(10,3)→(10,4)→(10,5)→(9,5)` = **15** przejść; 8 więcej, nie jest to pomiar czasu jazdy.

**Kontrakt geometryczny widoczności (bez AI):** hipotetyczny punkt obserwacji (9,6), 36m ×24m w świecie. Na wysokości 1,5m, na podstawie dokładnie podanych przez oryginalny `MapWorld.CreateItem` wymiarów Wall `4×4×0.5m`, w teście przecięcia promienia z prostokątem XZ: pięć komórek (3..7,2) ma zasłoniętą linię do hipotetycznego punktu, sześć komórek (3..8,5) na skrócie jej nie ma. **To tylko dowód geometrii brył ścian**. Przeciwnik ani jego `LOS` nie jest elementem JSON; obecnie brak tu celu „dwa zabicia”.

## Jedna mapa, zachowana historia zmian

| Rodzaj | PASS14 | PASS15 | Delta |
|---|---:|---:|---:|
| Ground | 120 | 120 | 0 |
| Wall obwodowe | 40 (4 narożne szczeliny) | 36 | -4 |
| Wall wewnętrzne | 0 | 5 | +5 |
| Platform narożne | 0 | **4** | **+4** |
| Pillar | 4 | 4 | nowe pozycje |
| LowBarrier | 2 | 2 | nowe pozycje |
| Spawn / Finish | 1 / 1 | 1 / 1 | 0 |
| Łącznie | **168** | **173** | **+5** |

Dodatkowe pozycje: Pillar `(4,1),(7,1),(4,7),(7,7)`; LowBarrier `(5,7),(8,7)`. Każdy element oprócz czterech Platform narożnych ma `scaleX/Y/Z=1`; narożniki używają `scaleX=1,scaleY=4,scaleZ=1` i wszystkie współrzędne należą do obsługiwanego przez `MapDraft` zakresu.

## Faktyczne wykonane kontrole danych

**19/19 PASS w izolacie V8** na oryginalnych bajtach JSON z GitHuba i oryginalnych tekstach kodu `MapDraft.cs` (Git blob `b5f8f395f93972c0ede000701dbbada8fcfcdfdb`) oraz `MapWorld.cs` (blob `8928908db123b35dcab4b324fe9d3a7a8fceee4e`) w aktualnym publicznym Codex [#310](https://github.com/C8LyNn/RoboRougelike/pull/310) `231005c6d0351945a8ee6684f3856af118f0e94f`. Sprawdzono typy, liczby, granice, unikalność footprintów, zachowanie obwodu, obie kompletne trasy, brak przecięcia ich wnętrz, najkrótszą drogę i pięć zasłoniętych / sześć odsłoniętych punktów w przyjętej projekcji XZ. Nie wykonywano oryginalnego C#, Unity ani PhysX.

**Do przetestowania w istniejącym [CV-05](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223), bez drugiego zadania:** w izolowanym profilu skopiuj plik do `Application.persistentDataPath/Maps/B01_25_LD02_COVER_P15.json`; wpisz w edytorze nazwę `B01_25_LD02_COVER_P15` i wybierz `Wczytaj`. Nigdy nie nadpisuj rzeczywistych map (D02). Oczekiwane minimum:
1. `TryLoad=true`, `TestError=null`, 173 elementy (120 Ground, 41 Wall, 4 Platform, 4 Pillar, 2 LowBarrier, 1 Spawn, 1 Finish), `globalFloor=false`.
2. Na naprawdę działającym Light i Heavy da się niezależnie przejechać obie trasy lub wydać prawdziwy **FAIL** wraz z pomiarem `Collider.bounds`, prześwitu, trajektorii, zderzeń i `Editor.log`. **Nie** zakładaj, że 4m grid dowodzi bezpiecznej szerokości.
3. Render/PhysX potwierdza ciągłość wewnętrznego muru, a hipotetyczne zasłonięcie odpowiada rzeczywistemu `Physics.Raycast` wyłącznie jeśli operator taki pomiar przeprowadzi. Nagroda, „2 kills”, faktyczny LOS/ogień i osobne zatwierdzenie trasy **UNSUPPORTED / NOT_RUN**.
4. Meta w obecnym `MapWorld.MarkFinish` oznacza **SANDBOX_TOUCH_ONLY**, nie combat victory, nie reward. Zachowaj źródłowe SHA, autentyczne NUnit XML/log, wyjście procesu, obrazy i oryginalne dane jazdy. Oddzielna niezależna bramka CHL/PARTSIZE G1 pozostaje nienaruszona.

**Owner:** B01-25 autor poziomu; B01-21 fizyczna topologia/clearance; B01-10 sprawiedliwość decyzji ryzyka; B01-24 combat LOS; Codex kanoniczna implementacja; B01-60 realny niezależny odbiór tylko po zgodzie HQ. Własność `Assets/**` bez zmian, **no merge / no G0-G1 upgrade**.

## PASS15 CONTINUATION — OWNED SOURCE FIX FOR 4 CORNER GAPS

B01-61 independently reported the previous original 1.75m gaps in [P14](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6464183499). The same four yaw90 Wall cells `(0,0),(0,9),(11,0),(11,9)` were present in earlier P15 `LD02_COVER_CHOICE.json` Git blob `21d079d0fe5917554be526aeb0d0d89a35de4c7e`. This update replaces only those four items by **supported `Platform(kind6,rotation0,scaleY4)`**, to source AABB `4m×4m×4m`, without losing the established 7-step and 15-step centerline paths. The remaining 169 items are unchanged. Counts are now **120 Ground / 41 Wall (36 outer + 5 inner) / 4 Platform corner / 4 Pillar / 2 LowBarrier / 1 Spawn / 1 Finish = 173**. The SVG also depicts four full 4m square blocks.

**Actually executed SOURCE-SPACE checks:** original 2m boundary probes before: middle `4/4` and corners `0/4`; after: middle `4/4`, corners `4/4` on current exact Codex `MapWorld` AABB dimensions. These are **mathematical original-data source checks only**. Native PhysX/collider contact, Heavy clearance, enemy LOS and actual rewards still **NOT_RUN**. Independent B01-61 receiver must check **this new Git blob** and release the next content extension gate; do not reuse B01-21's prior P14 receiver success as P15 green.

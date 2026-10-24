# MAPGEN001_R2_DESIGN_QA — B01-10 / GAME DESIGN & BALANCE

**Priorytet:** P0 · **Data:** 2026-10-24 · **Rola:** B01-10 (niezależny audyt projektowy)  
**Issue:** https://github.com/C8LyNn/RoboRougelike/issues/93  
**Źródło:** Draft PR https://github.com/C8LyNn/RoboRougelike/pull/103  
**Dokładny badany commit (immutable):** `a2993a4cb09275864a04c6eaf8bc7b9f4dfb9946`  
**Werdykt:** **DESIGN FAIL / G2 HOLD**. Nie merge, nie integracja; odbiorcą po G2 jest GPT OSS.

## 1. Metoda i granice dowodów
- Przeczytano na powyższym SHA faktyczne źródła `MapGenPlanner.cs`, `MapGenRouteValidator.cs`, `MapGenData.cs`, `MapGenPhysicalTraversalTests.cs`, `MapGenRouteValidationTests.cs` i plan QA.
- Niezależnie odtworzono poza Unity deterministyczny generator geometrii (klasyczny algorytm `System.Random`), footprint rect XZ + margines 0,25 m, próbki spine co 0,5 m i BFS 4-neighbour w siatce 1 m. Obliczono **600 layoutów**: 2 receptury × 3 presety × 100 seedów (0–99), oraz **1200 prób walidacji** dla Light/Heavy. Dodatkowo wykonano analizę izolowanego `Difficulty` przy stałym `ObstacleDensity=50` dla 600 planów i obu profili (próbki spine).
- **To niezależny model offline, a nie uruchomienie oryginalnego C# w Unity.** Rezultaty seedów i wyniki graniczne mogą różnić się z powodu implementacji `System.Random`, precyzji float i `Mathf`. Każdy zgłoszony FAIL należy potwierdzić oryginalnym runtime na identycznym SHA. To mocne hipotezy poparte deterministyczną reprodukcją, nie fikcyjne Unity PASS.
- Brak potwierdzonych uruchomień Unity EditMode/PlayMode ani fizycznych przejazdów na SHA R2 w zbadanych dowodach; B01-60 nadal niezależnie musi wykonać G1. Sprawdzenie kodu i deklaracje testów nie są wynikami testów.

## 2. Wynik 100 seedów na recepturę i preset (model offline)
Presety do porównań: **easy** Difficulty=0, Density=25; **normal** Difficulty=0.5, Density=50; **hard** Difficulty=1, Density=75; Segments=4, LaneWidth=16 m. Light = 2.1 × 3.2 m / MinTurnRadius=3 m / MaxSlope=14°; Heavy = 4.5 × 5.8 m / MinTurnRadius=6 m / MaxSlope=10°.

| Receptura | Trudność | Light: spine+BFS | Heavy: spine+BFS | Unikalna geometria / 100 |
|---|---|---:|---:|---:|
| COMBAT_ARENA | easy | 100 | 100 | 100 |
| COMBAT_ARENA | normal | 100 | 100 | 100 |
| COMBAT_ARENA | hard | 100 | 100 | 100 |
| OBSTACLE_COURSE | easy | 100 | 100 | 97 |
| OBSTACLE_COURSE | normal | 100 | **79** | 100 |
| OBSTACLE_COURSE | hard | 100 | **46** | 100 |

Pozostałe **21 normal + 54 hard = 75** prób Heavy kończą się w modelu offline `SPINE_FOOTPRINT_BLOCKED` **przed** BFS. W kodzie `MapGenPlanner.Plan` analogiczny FAIL wywołuje `ArgumentException` zamiast generowania mapy. Nie ma zatwierdzonego bezpiecznego reroll/fallback.

## 3. Ocena COMBAT_ARENA — DESIGN FAIL (P1)
- Wszystkie layouty mają te same: 64×64 m podłoże, 12 m chroniony centralny korytarz, player spawn `(0,-25)`, enemy spawns `(-17,25),(0,25),(17,25)` w XZ. Seed zmienia tylko opcjonalne osłony w bocznych komórkach; centrum `ix==0` pozostaje wolne.
- **100/100 seedów na każdym poziomie:** bezpośrednia centralna linia widoczności player start → middle enemy spawn (bez przesłony w wygenerowanym układzie). Uwaga: to pomiar geometrii XZ, nie faktyczny renderer/AI/walka.
- Średnie liczby osłon: easy **4,26**, normal **8,21**, hard **12,03**; widocznych od gracza spawnów: odpowiednio **2,65 / 2,10 / 1,59**. Więcej osłon nie musi oznaczać większego ryzyka: może ułatwiać graczowi unik ostrzału.
- **Seed 7 / easy:** 0 osłon, 3/3 widoczne spawny — bezosłonowy układ pomimo „unikalności” innych seedów. **Seed 31 / hard:** 14 osłon, a nadal 3/3 spawny widoczne: przykład braku korelacji ilości osłon z ekspozycją startową.
- Dominująca strategia: centralny przejazd i starcie na otwartej osi, bez wymuszonej zmiany pozycji. Brak zmienności pozycji startowych i topologii flank. Ryzyko natychmiastowego ostrzału środka zależy od późniejszego AI — **nie twierdzimy, że damage już występuje**.

## 4. Ocena OBSTACLE_COURSE — DESIGN FAIL (P0/P1)
- R2 dodał BFS/obrys pojazdu, poprawił start Z z 2 na 5 i uzależnił dryf osi od Difficulty. To rzeczywisty postęp wobec MGD-01.
- Trasa nie ma istotnego wymogu zaliczania slalomu/ramp: wszystkie przeszkody są przy bokach, rampy są opcjonalne, a oznaczona spine omija te elementy. W 100 seedach na preset długość osi to około **56,02 / 56,11 / 56,25 m** (easy/normal/hard); rozpiętość normal: **56,00–56,21 m**, hard: **56,00–56,49 m**. Nie stanowi to samo w sobie zauważalnej progresji gry.
- Średnia liczba przeszkód **1,05 / 1,98 / 3,12**, ramp **1,20 / 1,45 / 1,73**. Przy stałej Density=50 dla wszystkich trudności liczba przeszkód pozostaje **1,98**, ramp **1,45**, a zmieniają się tylko dryf osi i rozmiar przeszkód.
- **Seed 7 / normal / Heavy:** model generuje rampę środkiem X=+3,3 m, Z=23 m; przed rampą obrys Heavy na osi toru w okolicy **X=-0,318, Z=16,394** nakłada się na obrys rampy. Preflight: `SPINE_FOOTPRINT_BLOCKED`. Light: OK. **Seed 2 i 31 / normal / Heavy:** ten sam kod błędu. **Seed 0, 2, 7 / hard / Heavy:** ten sam kod błędu. Wymagany natywny repro na dokładnym SHA.
- **Seed 83 / hard:** 4 przeszkody i 1 rampa, ale chroniona oś jest wolna w modelu; można je ominąć zamiast wykonywać wymagający manewr. To pokazuje brak gwarancji jakości wyzwania mimo zróżnicowanych hashy.
- Minimalne geometryczne promienie wyliczone z trójek waypointów w modelu: mediany około **274,66 m / 128,81 m / 84,72 m** (easy/normal/hard); znacząco przewyższają promienie 3/6 m pojazdów. Obliczenie circumradius nie jest symulacją sterowania i nie dowodzi trudności.
- **Dominująca strategia:** jazda środkiem z niewielką korektą kierunku, brak konieczności użycia rampy albo wybrania drogi zależnej od właściwości robota.

## 5. Rejestr problemów i weryfikacja R1
| ID | P | Stan | Problem / dowód | Wymaganie dla B01-20 |
|---|---|---|---|---|
| MGD-01 / R2-01 | **P0** | **FAIL offline; potwierdzenie Unity potrzebne** | Nowy validator BFS + footprint istnieje, lecz Heavy odrzucany w 21/100 normal i 54/100 hard; wycinek seedów 2/7/31 | Zmniejszyć kolizję spine z side ramp/obstacle lub przeliczać przeszkody względem swept spine; testować wszystkie 100 seedów × 2 rig × 3 trudności; ograniczony deterministyczny reroll lub jawne fail-closed wyłącznie dla konfiguracji faktycznie niewspieranych |
| MGD-02 / R2-02 | **P1** | **FAIL DESIGN** | Oś toru omija wszystkie przeszkody i rampy; rosną tylko małe przesunięcia i liczba side elementów | Dodać przejezdny lecz faktycznie istotny slalom, zwężenia/wybór alternatywnych przejazdów, ograniczenia zależne od gabarytu, mierzyć wysiłek i czas |
| MGD-03 / R2-03 | **P1** | **FAIL DESIGN** | Stałe centralne starcie 100/100; osłony zmieniają liczbę/skalę, nie taktykę | Zmienny dostęp do osłon, co najmniej dwie sensowne drogi podejścia, walidacja LOS i fair-spawnu; zachować bezpieczną mobilność |
| R2-04 | **P1** | **NOT_RUN Unity** | Testy PlayMode są zapisane w źródłach, ale brak XML/logów/wykonania; test używa prostej kostki Rigidbody, nie W8 | B01-60 uruchamia 12 scenariuszy i 3 ręczne seedy per receptura × 2 pojazdy, zachowuje logi i dowody |
| R2-05 | **P1** | **GAP** | Deklaracja 6+ różnych hashy nie mierzy różnych decyzji gameplayowych | Raport cech/topologii, LOS, liczb wymuszonych manewrów, dostępności flank, zapasów przejazdu |
| R2-06 | **P2** | **LIMIT** | Czworokątny axis-aligned footprint, BFS 1m, 4-neighbour i promień circumcircle mogą pomijać rotację/suspension/overhang | Po naprawie preflight dodać kinodynamiczne testy i rzeczywiste pojazdy; nie podnosić statusu do PhysX PASS |
| R2-07 | **P2** | **GAP** | `MapBlueprint.GenerationVersion` pozostaje 1, pomimo zmiany algorytmu R2 | Podnieść wersję kontraktu/generacji, ustalić zgodność zapisanych seedów |

## 6. Instrukcje naprawcze dla B01-20, bez ingerencji w grę
1. **Najpierw P0:** na SHA `a2993a4...` uruchomić `MapGenPlanner.Plan(ObstacleCourse, 7, Default, Heavy)` i analogicznie seedy 2,31; w trybie hard 0,2,7. Zachować wyjątek/log i obraz footprint vs ramp. Po fixie opublikować nowy SHA i *100 seedów na każdy preset/rig*; nie ukrywać FAIL przez pomijanie seedów.
2. **R2-02:** liczyć bezpieczny obrys pojazdu **wzdłuż rzeczywistej zadanej spine**, a nie względem nominalnego środka segmentu; przy każdej generacji wymusić ≥1 faktyczny wybór/manewr na normal oraz ≥2 na hard, jednak zachować przejazd Light i uzasadniony profil Heavy. Przeszkody nie powinny generować nieuzasadnionych softlocków.
3. **R2-03:** przygotować minimum trzy archetypy areny: rozdzielone osłony, flankowanie z przesmykami i półotwarta przestrzeń. Utrzymywać co najmniej dwie grywalne drogi podejścia i kontrolę ekspozycji spawnów; bez implementacji AI/win logic.
4. Nie zmieniać W8, ekonomii, CHL-003 ani scen gry. Kod wyłącznie w MAPGEN001, osobny Draft PR/commit; B01-60 oraz B01-40 niezależnie retestują nowy SHA.

## 7. Mierzalne kryteria akceptacji po fixie
- **Zakres:** dla **każdej receptury** seedy 0–99 × easy/normal/hard × Light/Heavy = 600 przypadków (łącznie 1200); dodatkowo 3 seedy manualne × 2 receptury × 2 pojazdy. Pełne CSV: seed, config, rig, hash, planner PASS/exception, route code, min clearance, min radius, LOS/spawns, czas generacji.
- **Geometry G1 (100%):** brak blokady startu/mety i nieprawidłowych nakładek na wymaganej trasie; sukces preflight dla konfiguracji deklarowanych jako obsługiwane, 0 nieoczekiwanych `IMPOSSIBLE_GENERATED_ROUTE`; 0 zawieszonych rerolli (limit prób jawny). Dwa niezależne profile zgodne z wymiarami testowymi.
- **Gameplay diversity:** minimum 6 różnych hashy i celowo przyjęte kryterium **≥25 taktycznych sygnatur na 100 seedów** na preset, gdzie sygnatura obejmuje LOS/spawn, położenia osłon względem startu, wybór trasy, wymagane manewry oraz minimalny clearance. Próg 25 jest propozycją designową, nie historycznym wynikiem.
- **Trudność:** na stałym seeddzie i stałej density mierzyć wymuszone zmiany toru, skręt/ryzyko, czas przejazdu, otwarcie linii ognia i utknięcia; zalecana kalibracja median wysiłku manewrowego **normal ≥ easy+10%**, **hard ≥ normal+10%**, przy braku niespodziewanych softlocków. Jeśli fizyka powoduje znaczącą wariancję, oddzielić pomiar trasy od kontroli AI/sterowania.
- **Arena:** 0 spawnów wewnątrz colliderów, 0 spawnów poza podłożem, co najmniej 2 alternatywne podejścia, sprawdzane seedowo; raport udziału odkrytej centralnej LOS zamiast obecnego sztywnego 100%, bez wymagania implementacji AI.
- **Fizyka:** Unity 6000.3.14f1 EditMode/PlayMode oraz rzeczywisty Rigidbody i 2 pojazdy co najmniej na 3 zadeklarowanych seedach na recepturę; XML, Editor.log, exit, screenshot/video, czas/mety/falle/stuck; po integracji także faktyczny W8. **Nie mylić statycznego BFS ze zdolnością do jazdy.**
- **Korekty:** każdy FAIL → drobna poprawka B01-20 → nowy exact SHA → ponowny audyt B01-10 i B01-60.

## 8. Stan bramek
`R2_CODE_PRESENT / MGD_01_PARTIAL_REWORK / MGD_02_FAIL / MGD_03_FAIL / OFFLINE_HEAVY_REJECT_75_OF_200_NORMAL_HARD / UNITY_NOT_RUN / G1_BLOCKED / DESIGN_FAIL / G2_HOLD / GPT_OSS_NOT_AUTHORIZED / NO_MERGE`.

Wyniki źródłowe tego audytu: niezależny skrypt `mapgen_r2_offline_audit.py`, 600-wierszowy CSV layoutów i 1200-wierszowy CSV route-check dostępne jako artefakty rozmowy B01-10; **nie są to pliki źródłowe Unity ani oficjalne wyniki Test Runnera**.

# M04-002 — wirnik podwójny przeciwsobny

**Kolejka:** M04 / mobilność specjalna. **Katalog:** Docs/ModelFactory/CATALOG_V1.md na feature/rrl-model-factory-queue-v1. **Rezerwacja:** Docs/ModelFactory/claims/M04-002.json, commit 023e3d12693bb7377639ffdc87c665dff20c244f.

## Model

Boczny moduł napędowy z dwoma osiowymi wirnikami w osłonie. Pięć łopat pierwszego wirnika i siedem drugiego obracają się przeciwnie wokół osi +X. Koncentryczne obręcze grafitowo-miedziane, ceramiczne pierścienie, cyjanowe i bursztynowe akcenty emisyjne. Siedem zdefiniowanych materiałów, sześć używanych w siatce; 62 części proceduralne.

Mocowanie: płaszczyzna X=0, oś na Y=0, Z=0, na zewnątrz pojazdu +X. Wymiary statycznego OBJ: 0.658 × 0.998 × 0.998 m. Jednostki: 1 = 1 m. Dwa pivots Spin_0_CW, Spin_1_CCW w X=0.280 i X=0.520 m. Animacja: klatki 1→49, 24 FPS, 360° w przeciwnych kierunkach; klatka 49 jest klatką zamykającą pętlę (dwie przeciwne pełne rotacje po 48 odstępów klatkowych); GLB zawiera wszystkie 17 pozycji kluczowych każdego wirnika.

## Uruchomienie

CPython bez Blendera, walidacja i statyczny OBJ/MTL:

    python Tools/ModelFactory/M04/M04-002/test_m04_002.py
    python Tools/ModelFactory/M04/M04-002/generate_m04_002.py --out build/M04-002

Blender 4.x, dodatkowo animowany GLB:

    blender --background --python Tools/ModelFactory/M04/M04-002/generate_m04_002.py -- --out build/M04-002

Generator produkuje OBJ/MTL i M04-002.manifest.json w obu trybach, a GLB wyłącznie w uruchomionym Blenderze. Nie używa zewnętrznych bibliotek do generowania OBJ. Nie dodano gotowego GLB ani OBJ do PR: artefakty są odtwarzalne po uruchomieniu generatora.

## QA i ograniczenia

- Lokalnie wykonano generator w CPython, py_compile, test stdlib oraz kontrolę OBJ przez trimesh: PASS.
- 62 zamknięte komponenty, 8528 wierzchołków, 8010 wielokątów w OBJ, 12 łopat. Testy kontrolują indeksy, materiały, brak otwartych krawędzi, rozdział grup i nominalne prześwity.
- Nominalna radialna różnica promieni wynosi 0.041 m, a osiowy odstęp bezpieczny 0.190 m. To ograniczenia projektowe, nie symulacja kolizji w czasie.
- Blender GLB export / wizualna inspekcja / Unity import i PlayMode: NOT RUN w środowisku wykonawczym. Nie deklarujemy gotowego, animowanego assetu Unity.
- GLB należy otworzyć w Blenderze/Unity, sprawdzić kierunek obrotu po konwersji osi, import materiałów emisyjnych i ruch pivots, a następnie wykonać test animacji w ekstremalnych klatkach.
- Moduł jest wyłącznie wizualny; nie modyfikuje fizyki, rozgrywki, scen ani ProjectSettings.

**Stan:** DRAFT_QA_HOLD do czasu rzeczywistego wykonania Blendera i importu Unity.

## Naprawa konstrukcyjna V2 — piasty, łopaty i animacja

**Znaleziono trzy problemy:**
1. Piasty `Hub_0/1`, `HubRing_0/1`, `RotorCore_0/1` były pełnymi walcami, przez które przechodzi nieruchomy wał. To błąd przenikania stałej i obracającej się geometrii.
2. Nasady łopat zaczynały się zbyt daleko od piast, tworząc widoczną szczelinę i wrażenie nieprzymocowanych łopat.
3. Klipy obracały każdy wirnik o 360° za pomocą wyłącznie dwóch klatek. Eksporter konwertujący krańcowe orientacje na kwaterniony może zinterpretować oba końce jako tę samą orientację, redukując obrót do bezruchu.

**Poprawiono:**
- Sześć elementów piast stało się zamkniętymi pierścieniowymi bryłami z **47 mm promienia otworu** przez całą długość osi. Wał ma 38 mm promienia, więc luz promieniowy wynosi **9 mm**. Siatki są manifold i mają prawidłowe normalne powierzchni wewnętrznej.
- Promień pierwszej stacji łopaty zredukowano z 102 mm do 76 mm; łopaty kończą się **wewnątrz zewnętrznego obrysu piast** i zachowują dodatni rozstaw kątowy między siedmioma łopatami.
- Animacja każdego wirnika ma teraz **17 klatek kluczowych co trzy ramki** (45° na interwał) ze znakiem przeciwnym dla każdego zespołu. Klatki 1–49 przy 24 FPS reprezentują pełne dwa sekundy i domknięcie pętli.
- Dodano `M04_002_ROOT_SIDE_X_POS` jako jeden korzeń mocowania bocznego (+X) dla statycznej osłony i obu obrotowych pivotów. Kontrola Blender sprawdza hierarchię i zachowanie world-space po ustawieniu parentów.

**Potwierdzone artefakty i QA:**
- [GitHub Actions 40097915025 — PASS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40097915025): składnia generatora, 62 zamknięte komponenty, 8528 wierzchołków, 8010 wielokątów, sześć pierścieniowych piast, prześwit wału, osadzenie 12 łopat, kontrola kluczy animacji.
- Skorygowany model **OBJ + MTL** oraz manifest powstały w CI bez Blendera i są dostępne jako artefakt `M04-002-hollow-hubs-corrected-static-OBJ`.
- **NOT RUN:** wykonywanie `bpy` w Blenderze, właściwy eksport animowanego GLB i import/runtime Unity. Test źródła i siatki nie zastępuje wizualnego QA pełnego klipu. Oryginalny PR #42 zawiera stary generator; poprawka znajduje się na oddzielnej gałęzi i wymaga review.

**Zakres bezpieczeństwa:** wyłącznie wizualny model `M04-002`, bez zmian scen, rozgrywki, ProjectSettings i cudzych modeli. Nie użyto nowego ID katalogowego ani nie przejęto cudzej rezerwacji.

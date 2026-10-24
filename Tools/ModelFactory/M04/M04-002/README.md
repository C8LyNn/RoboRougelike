# M04-002 — wirnik podwójny przeciwsobny

**Kolejka:** M04 / mobilność specjalna. **Katalog:** Docs/ModelFactory/CATALOG_V1.md na feature/rrl-model-factory-queue-v1. **Rezerwacja:** Docs/ModelFactory/claims/M04-002.json, commit 023e3d12693bb7377639ffdc87c665dff20c244f.

## Model

Boczny moduł napędowy z dwoma osiowymi wirnikami w osłonie. Pięć łopat pierwszego wirnika i siedem drugiego obracają się przeciwnie wokół osi +X. Koncentryczne obręcze grafitowo-miedziane, ceramiczne pierścienie, cyjanowe i bursztynowe akcenty emisyjne. Siedem zdefiniowanych materiałów, sześć używanych w siatce; 62 części proceduralne.

Mocowanie: płaszczyzna X=0, oś na Y=0, Z=0, na zewnątrz pojazdu +X. Wymiary statycznego OBJ: 0.658 × 0.998 × 0.998 m. Jednostki: 1 = 1 m. Dwa pivots Spin_0_CW, Spin_1_CCW w X=0.280 i X=0.520 m. Animacja: klatki 1→49, 24 FPS, 360° w przeciwnych kierunkach; klatka 49 jest klatką zamykającą pętlę, odtwarzanie 1–48.

## Uruchomienie

CPython bez Blendera, walidacja i statyczny OBJ/MTL:

    python Tools/ModelFactory/M04/M04-002/test_m04_002.py
    python Tools/ModelFactory/M04/M04-002/generate_m04_002.py --out build/M04-002

Blender 4.x, dodatkowo animowany GLB:

    blender --background --python Tools/ModelFactory/M04/M04-002/generate_m04_002.py -- --out build/M04-002

Generator produkuje OBJ/MTL i M04-002.manifest.json w obu trybach, a GLB wyłącznie w uruchomionym Blenderze. Nie używa zewnętrznych bibliotek do generowania OBJ. Nie dodano gotowego GLB ani OBJ do PR: artefakty są odtwarzalne po uruchomieniu generatora.

## QA i ograniczenia

- Lokalnie wykonano generator w CPython, py_compile, test stdlib oraz kontrolę OBJ przez trimesh: PASS.
- 62 zamknięte komponenty, 7952 wierzchołki, 7158 wielokątów w OBJ, 12 łopat. Testy kontrolują indeksy, materiały, brak otwartych krawędzi, rozdział grup i nominalne prześwity.
- Nominalna radialna różnica promieni wynosi 0.041 m, a osiowy odstęp bezpieczny 0.190 m. To ograniczenia projektowe, nie symulacja kolizji w czasie.
- Blender GLB export / wizualna inspekcja / Unity import i PlayMode: NOT RUN w środowisku wykonawczym. Nie deklarujemy gotowego, animowanego assetu Unity.
- GLB należy otworzyć w Blenderze/Unity, sprawdzić kierunek obrotu po konwersji osi, import materiałów emisyjnych i ruch pivots, a następnie wykonać test animacji w ekstremalnych klatkach.
- Moduł jest wyłącznie wizualny; nie modyfikuje fizyki, rozgrywki, scen ani ProjectSettings.

**Stan:** DRAFT_QA_HOLD do czasu rzeczywistego wykonania Blendera i importu Unity.

# M01-002 — Krótka haubica tłokowa (model wizualny)

**Katalog:** M01-002, top socket, ruch suwadła/odrzutu, gruby stożkowy hamulec wylotowy. **Status:** DRAFT / QA HOLD. **Zakres:** wyłącznie model 3D, bez mechanik strzelania.

## Pliki

- `M01-002.obj` + `M01-002.mtl` — rzeczywista statyczna geometria w pozycji spoczynkowej; siedem materiałów, w tym akcent emisyjny w Blenderze. OBJ nie przenosi animacji.
- `generate_m01_002.py` — pełny, idempotentny generator Blender bpy; tworzy ten sam zestaw części, hierarchię `MOUNT_TOP_SOCKET` i `RECOIL_Y_POSITIVE`, animuje suwadło i eksportuje `.blend`, `.glb`, `.fbx`.
- `validate_static.py` — walidator OBJ/MTL w Pythonie 3 bez zależności.
- `M01-002.static-qa.json` — wynik kontroli podczas generowania geometrii.

## Współrzędne i montaż

Jednostki: metry (1 Unity unit = 1 m). Blender: **+Z góra**, **-Y przód (wylot)**. Pivot mocowania (0,0,0), środek stopy montażowej na osi X=0. Animacja przesuwa cały zespół lufy i komory **+Y o 0,12 m** (klatki 1,4,8,14,20, 24 FPS). Dwie chromowane prowadnice wsuwają się w obudowy tłoków — to **zamierzona współosiowa teleskopowa interakcja**, a nie błąd przenikania. Maksymalny ruch pozostawia około 0,055 m odstępu od tylnej grodzi i około 0,165 m odstępu komory od bocznych policzków. Są to odstępy analityczne dla elementów obudowy, **nie pełny test kolizji mesh-to-mesh**.

## Odtwarzanie i eksport

```bash
python3 validate_static.py
blender -b --python generate_m01_002.py -- --output ./export
```

Po eksporcie w Unity: import GLB przez importer obsługujący glTF (lub FBX z Unity), potwierdź orientację przód/-Y i oś animacji, skalę oraz zakres ruchu; ręcznie sprawdź cieniowanie i przenikanie w klatkach 1,4,8,14,20. Dla FBX w Unity sprawdź ustawienia osi i skali (Blender +Z -> Unity +Y). Model nie jest prefabem ani zintegrowaną bronią.

## QA

Statyczny generator OBJ: 55 części, 2376 wierzchołków, 4568 trójkątów, 7 materiałów; wstępna walidacja indeksów i zakresu współrzędnych PASS. Blender generator runtime: **NOT RUN** (brak Blendera); eksport GLB/FBX: **NOT RUN**; Unity: **NOT RUN**. Pozostawić PR w DRAFT do pełnej kontroli.

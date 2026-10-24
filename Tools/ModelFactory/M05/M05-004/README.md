# M05-004 — Lidar kopułowy (MODEL-05)

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na gałęzi `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M05-004.json`, commit `b3fc5ff7e8fe73bf3a72254b4ece8568b90b68f9`, agent M05, 2026-10-24 13:44:27 UTC. Nowy wizualny moduł lidarowy, **bez nowego skanowania gameplayowego**, bez zmian w istniejących SEN-001/COM-001.

## Wygląd i geometria

Wymiary modelu **0,98 × 0,98 × 0,77 m**. Gniazdo mocowania `M05-004_MOUNT_TOP_SOCKET` w lokalnym `(0,0,0)`, oś pionowa Blendera **+Z**, w glTF z `export_yup=True` odpowiada osi **+Y**. Dolna stopa montażowa i grafitowa podstawa, ceramiczny kołnierz, pierścień ochronny i cztery zaciski, osiem oznaczeń ostrzegawczych oraz dwuwarstwowa półkulista osłona optyczna (grubość 12 mm). Wewnątrz cztery moduły optyczne z cyjanowymi soczewkami i emisyjnymi markerami. Osłona ma materiał `smoke_glass` (alpha 0,23); źródła światła nie są wymagane do odczytania sylwetki.

Geometria statyczna OBJ: **42 zamknięte bryły, 3956 wierzchołków, 4368 poligonów, 7776 trójkątów**. Sześć materiałów: graphite, titanium, ceramic, amber, scan_cyan (emisja), smoke_glass (półprzezroczysty). W generatorze wszystkie prymitywy mają kontrolę domknięcia krawędzi (dokładnie dwa sąsiadujące poligony na każdą krawędź); łącznie 8256 krawędzi topologicznych.

## Animacja

`M05-004_LIDAR_ROTOR` obraca **24 części** wewnętrznej głowicy wokół lokalnego punktu `(0,0,0.43)`, zgodnie z osią Z Blendera. Pozycje kluczowe: 1/31/61/91/121 klatek = 0/90/180/270/360°. Stosowany jest quaternion, a nie samodzielna interpolacja pomiędzy identycznymi orientacjami 0 i 360°, która mogłaby dać brak ruchu. Klucze liniowe; eksport GLB wymusza próbkowanie animacji.

**Analiza prześwitu:** rotor ma obwiednię promieniową do 0,302 m, pionową 0,385–0,600 m. W najbardziej ograniczonym promieniu kopuły jej wewnętrzna powierzchnia leży ok. 0,629 m nad podstawą; zachowano minimum ok. **29 mm prześwitu** nad ruchomymi częściami. Dolna krawędź rotora znajduje się 53 mm powyżej górnej powierzchni zewnętrznego pierścienia osłonowego. Sprawdzenie jest **analityczne**, nie symulacją silnika fizyki Unity. Styki obudów i obejm są celowymi miejscami montażu, nie niezależnymi animowanymi kolizjami.

## Reprodukcja

Wymagany Python 3.10+ do statycznego OBJ/MTL i Blender z `bpy` do animowanego GLB i .blend:

```bash
python3 generate_m05_004.py --self-test --output-dir /tmp/m05-004
blender --background --factory-startup --python generate_m05_004.py -- --output-dir /tmp/m05-004 --require-blender
```

Skrypt tworzy `M05-004.obj`, `M05-004.mtl`, `M05-004.static-qa.json`. Blender dodatkowo generuje `M05-004_animated.glb` i `M05-004.blend`. Eksport glTF jest ograniczony do obiektów własnej kolekcji `M05-004_ASSET`, a po eksporcie poprzedni wybór i aktywny obiekt zostają przywrócone. Wielokrotne uruchomienie usuwa wyłącznie obiekty, nieużywane siatki i akcje tego samego assetu. Dla czystego .blend stosuj `--factory-startup`; w przeciwnym razie zapis .blend zachowuje także inne obiekty otwartej sceny.

## QA — faktycznie wykonane

- **PASS**: Python `py_compile`, wykonanie `--self-test`, zamknięta topologia prymitywów, zakresy geometrii i prześwit, liczba materiałów, pivot, źródłowe klatki animacji.
- **PASS**: bajtowo identyczna regeneracja `M05-004.obj` i `M05-004.mtl` w GitHub Actions; [run 40099673115](https://github.com/C8LyNn/RoboRougelike/actions/runs/40099673115).
- **NOT RUN**: Blender `bpy` i rzeczywisty eksport animowanego GLB; animacja została przygotowana w kodzie, lecz nie zweryfikowana wizualnie w Blenderze.
- **NOT RUN**: Unity 6 import, EditMode, PlayMode, fizyka, animacja na prefabie, kontrola renderowania przezroczystości w URP.

To **nowy model wizualny**, nie implementacja gameplayu lidarowego. Nie edytowano `Assets`, scen, `ProjectSettings`, istniejących pakietów sensorów ani cudzych gałęzi.

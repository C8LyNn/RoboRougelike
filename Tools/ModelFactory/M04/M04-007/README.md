# MODEL-04 / M04-007 — Silnik hover z czterema emiterami

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.  
**Claim:** `Docs/ModelFactory/claims/M04-007.json`; CREATE commit `bcbfbd1b9ddeffe8b2060229dc321046e0d92eef`.  
**Cel:** wyłącznie nowy, niezależny model wizualny do RoboRougelike / Unity 6. Bez zmian napędu, gameplayu, scen, ProjectSettings ani cudzych modułów.

## Sylwetka i mocowanie

Cztery symetryczne okrągłe emitery hover w układzie 2×2, każdy osadzony w otwartej grafitowej obudowie z tytanową obręczą, ceramicznym wkładem, ośmioma miedzianymi radiatorami i dwiema świetlnymi liniami. Całość podtrzymuje prostokątna płyta mocująca z czterema ukośnymi ramionami oraz centralnym kontrolerem. W przeciwieństwie do wirników M04-001–003, **nie ma łopat ani fizycznego obrotu**; jest to silnik hover z czterema pulsującymi dyskami.

Blender Z-up; 1 jednostka = 1 metr. `M04-007_MOUNT_ROOT` leży w (0,0,0), na **dolnej płaszczyźnie montażowej ramy** Z=0. Wszystkie części znajdują się pod płaszczyzną montażową (Z≤0). Pozycje emiterów: X=±0,45 m, Y=±0,40 m. Siedem materiałów: graphite, titanium, ceramic, copper, rubber, ion_cyan, amber. Emisja jest wyłącznie akcentem, nie zastępuje geometrii.

## Animacja

Cztery niezależne `M04-007_EMITTER_PULSE_00…03` z pivotami w osiach dysków na Z=−0,282 m. Pulsacja skali XY `1±0,07` i przesunięcia Z `±0,012 m`, z przesunięciami fazowymi 0°, 90°, 180°, 270°. Jedna pełna fala od klatki 1 do 81 przy 60 FPS; dziewięć klatek kluczowych co 10 klatek. Każdy root porusza trzema połączonymi warstwami dysku. Nie ma ruchu obudów, ramy ani miedzianych radiatorów.

Najbliższa nieruchoma wkładka pierścieniowa zaczyna się na promieniu 0,207 m; największy ruchomy dysk ma promień 0,178 m, skalowany maksymalnie 1,07, co daje **co najmniej 16,54 mm luzu promieniowego**. Testy weryfikują pełny zakres klatek, nie tylko pozycje skrajne.

## Uruchomienie

```bash
python3 Tools/ModelFactory/M04/M04-007/generate_m04_007.py --self-test --output-dir /tmp/m04-007
python3 Tools/ModelFactory/M04/M04-007/qa_mesh_m04_007.py --asset-dir /tmp/m04-007
blender -b --factory-startup --python Tools/ModelFactory/M04/M04-007/generate_m04_007.py -- --output-dir /tmp/m04-007-blender --require-blender
python3 Tools/ModelFactory/M04/M04-007/qa_mesh_m04_007.py --asset-dir /tmp/m04-007-blender --glb /tmp/m04-007-blender/M04-007_animated.glb
```

Skrypt działa bez `bpy` w trybie generowania statycznego OBJ/MTL/QA. Z Blenderem generuje `M04-007.blend` (bibliotekę wyłącznie własnej kolekcji) i `M04-007_animated.glb` (cztery niezależne animowane grupy). Blender 3.4+ / Python 3.10+. Powtórne uruchomienie usuwa tylko własną kolekcję `M04-007_ASSET`; nie usuwa obcych obiektów ani scen.

**Testy:** `generate_m04_007.py` sprawdza zamknięte siatki, orientację normalnych, niedegenerację, animację we wszystkich 81 klatkach, konserwatywne AABB i analityczny luz pierścieni. `qa_mesh_m04_007.py` niezależnie sprawdza zapisany OBJ/MTL i opcjonalnie rzeczywisty GLB (w tym osiem kanałów translacji/skali czterech korzeni). Dedykowane GitHub Actions publikują statyczne oraz animowane artefakty. PASS wolno deklarować wyłącznie na podstawie wykonanych testów.

**Unity:** import OBJ jest statyczny; animowany GLB może wymagać importera glTF lub konwersji na FBX. Unity EditMode/PlayMode, import do gry i kontrola wizualna pozostają **NOT RUN**, dopóki nie ma logów i zrzutów z docelowego środowiska. Integracja pozostaje zadaniem Codexa. Nie scalać Draft PR bez osobnego QA.

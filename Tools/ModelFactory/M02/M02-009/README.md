# M02-009 — Trójkołowy wózek gąsienicowy (MODEL-02)

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`. **Agent:** MODEL-02 (M02, wyłącznie napęd mechaniczny).

**Claim:** `Docs/ModelFactory/claims/M02-009.json`, operacja CREATE potwierdzona 2026-10-24T18:50:39.279Z, commit `4da962900ca0ebca81a5ab36dddc4eb20ae9670d`. Claim jest na gałęzi operacyjnej, a ten model na oddzielnej gałęzi od `develop`. Nie zajmować ponownie ID.

## Projekt mechaniczny

Trójkołowa kaseta gąsienicowa o czytelnym trójkątnym układzie nośnym. Dwa koła jezdne znajdują się na dole przy X=±0,310 m, Z=-0,270 m; trzecie koło napędowe/napinające na górze przy X=0, Z=+0,170 m. Wszystkie trzy osie są równoległe do Y, oś obrotu kół to +Y. Promień zewnętrzny koła wynosi 130 mm. Tor środków 40 osobno animowanych ogniw to dokładne równoległe odsunięcie trójkąta o 147 mm, z trzema zaokrąglonymi narożami i odcinkami prostymi.

**Wyróżnik wizualny:** dwie mocne, odsunięte od bieżnika, trójkątne ramy boczne z trzema belkami na stronę, bursztynowe wzmocnienia, stalowe piasty z rzeczywistymi otworami na osie, miedziane tuleje, widoczne śruby, żebrowane szprychy, klinowe nakładki gumowe i cyjanowe oznaczenia indeksowe. Użyto siedmiu odrębnych materiałów PBR. Każde koło ma oddzielny obrót; wszystkie ogniwa rzeczywiście przemieszczają się po torze (nie jest to tylko przewijanie tekstury).

**Mocowanie:** pivot `RRL_M02_009_ROOT_MOUNT_PIVOT` oraz socket `RRL_M02_009_MOUNT_SOCKET` znajdują się w **(0,0,0)**, na zewnętrznej płycie mocującej. Normalna portu jest skierowana w **-Y**, w stronę ramy pojazdu. Model rozciąga się w kierunku dodatniego Y. Blender: X do przodu, Y oś kół, Z do góry. Eksport glTF konwertuje układ do osi pionowej Unity Y. Skala: **1 jednostka = 1 metr**.

Wszystkie osie nieruchome mają średnicę 32 mm, natomiast piasty mają rzeczywisty otwór średnicy 48 mm. Różnica promieni zapewnia 8 mm luzu osi/piasty. Wewnętrzna powierzchnia ogniwa jest oddzielona od koła promieniowo o około 4 mm w miejscach styku; jest to uproszczony model wizualny, bez rzeczywistej pracy zębów na łańcuchu.

## Wykonanie

```bash
python3 Tools/ModelFactory/M02/M02-009/generate_m02_009.py --self-test
python3 Tools/ModelFactory/M02/M02-009/qa_m02_009.py
python3 Tools/ModelFactory/M02/M02-009/generate_m02_009.py --static-obj --out build/M02-009-static
blender -b --factory-startup --python Tools/ModelFactory/M02/M02-009/generate_m02_009.py -- --out build/M02-009-blender
python3 Tools/ModelFactory/M02/M02-009/qa_m02_009.py build/M02-009-blender/M02-009_animated.glb
```

Generator w Blenderze usuwa i odtwarza wyłącznie własną kolekcję `RRL_M02_009_TRI_WHEEL_BOGIE`, więc powtórne uruchomienie nie dubluje obiektów. Statyczne OBJ/MTL oraz manifest QA powstają w zwykłym Pythonie. Animowane GLB i edytowalny projekt BLEND powstają **tylko gdy faktycznie uruchomiony jest Blender**. CI publikuje wygenerowane pliki jako artefakty przebiegu; binaria nie są deklarowane jako commitowane do repozytorium.

## Walidacja i ograniczenia

Niezależne QA sprawdza każdy zapisany obiekt OBJ (własność wierzchołków, zamknięcie manifold, dodatnią objętość, brak degeneratów), siedem materiałów, trzy koła, 40 ogniw, geometrię trójkątnych belek oraz deterministyczność eksportu. Skrypt źródłowy sprawdza 97 faz ruchu, wszystkie pary ogniw metodą SAT w 2D oraz koła względem ogniw przez konserwatywne obwiednie. Kontrola GLB weryfikuje glTF 2.0, grupy obrotowe, socket i kanały animacji.

**Ważne:** analityczne testy obwiedni nie zastępują pełnego testu kolizji 3D, przeglądu wizualnego ani testów Unity. Animacja przedstawia jeden obieg gąsienicy; przy ponownym zapętleniu kąt kół może wymagać sterowania runtime, bo długość toru nie jest całkowitą wielokrotnością obwodu koła. Nie zmieniono gameplayu, wspólnych scen, prefabów, `ProjectSettings`, `develop` ani `main`.

**Unity 6 import, EditMode, PlayMode, fizyka pojazdu i ręczna ocena wyglądu: NOT RUN** dopóki nie zostaną faktycznie wykonane.

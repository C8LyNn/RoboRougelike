# M02-008 — Pojedynczy moduł gąsienicy (MODEL-02)

**Źródło prawdy:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.
**Rezerwacja:** `Docs/ModelFactory/claims/M02-008.json` — atomowe CREATE potwierdzone commitem `508eddf8c244e7e61f50ff59c11226dfdccb564b` z 2026-10-24T17:45:41.770Z. Agent M02. Nie ponawiać rezerwacji.

## Konstrukcja

Kaseta gąsienicowa składa się z 32 ruchomych stalowych ogniw z klinowymi gumowymi nakładkami, dwóch kół napędowych z zębami, trzech rolek dolnych, sztywnej ramy bocznej i osobnej płyty mocowania. Każde ogniwo ma dwa boczne czopy zawiasów. Oznaczenia indeksowe i boczne elementy miedziane podnoszą czytelność geometrii; użyto sześciu materiałów: BlackenedSteel, BrushedTitanium, TreadRubber, SafetyOrange, Copper, SignalCyan.

Układ współrzędnych Blendera: **X** — kierunek jazdy, **Y** — oś rolek (bok pojazdu), **Z** — pion. Jednostki: metry. Pivot `RRL_M02_008_ROOT` = `(0,0,0)`. Port montażowy `RRL_M02_008_MOUNT_SOCKET` = `(0,-0.212,-0.080)` m, normalna na zewnątrz `-Y`. Dwa koła główne mają osie `(±0.340,0,-0.200)` m.

Łańcuch biegnie po zamkniętej obwiedni dwóch łuków i dwóch odcinków prostych. Każde ogniwo otrzymuje własny transform, a animacja obejmuje jeden pełny obieg 96 klatek; osobno animowane są dwa koła napędowe i trzy rolki. Geometria nie jest fizycznym symulatorem sworzni i kontaktu z podłożem; w grze może służyć jako wizualny moduł do odrębnego systemu napędowego. Rzeczywiste ruchy ogniw są transformowane, nie zastępowane UV-scrollowaniem.

## Generowanie

```bash
python3 Tools/ModelFactory/M02/M02-008/generate_m02_008.py --self-test
python3 Tools/ModelFactory/M02/M02-008/qa_m02_008.py
python3 Tools/ModelFactory/M02/M02-008/generate_m02_008.py --static-obj --out build/M02-008-static
blender -b --factory-startup --python Tools/ModelFactory/M02/M02-008/generate_m02_008.py -- --out build/M02-008-blender
python3 Tools/ModelFactory/M02/M02-008/qa_m02_008.py build/M02-008-blender/M02-008_animated.glb
```

Generator modyfikuje tylko kolekcję `RRL_M02_008_TRACK_CASSETTE`. Statyczny OBJ/MTL jest wytwarzany w standardowym Pythonie i nie zawiera animacji ani obiektów Empty. Animowany GLB zachowuje grupy transformacji i socket; `.blend` jest projektem źródłowym. Wszystkie wygenerowane pliki z CI są dostępne jako artefakty GitHub Actions, nie są deklarowane jako pliki repozytorium bez osobnego commita.

## QA / granice dowodów

`qa_m02_008.py` sprawdza manifold/winding każdej siatki, poprawność OBJ/MTL, unikalność obiektów, 32 ogniwa, sześć materiałów, zgodność skali i obwiedni oraz próbki pełnego obiegu. W generatorze przeprowadzane są testy SAT w 2D dla par ogniw oraz konserwatywne sprawdzenie odległości od kół na 97 fazach; test nie zastępuje fizyki 3D ani ręcznego przeglądu w Blenderze. Po faktycznym eksporcie GLB niezależny parser sprawdza format glTF 2.0, animacje i węzły.

**Unity 6:** import, PlayMode, EditMode, rendering w grze i kolizje Rigidbody = **NOT RUN** do czasu rzeczywistego wykonania. Nie wolno uznawać testów analitycznych za test Unity. Nie modyfikowano scen, ProjectSettings, gameplayu, cudzych assetów ani gałęzi `main`/`develop`.

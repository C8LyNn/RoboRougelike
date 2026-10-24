# M05-006 — Projektor bariery kierunkowej (MODEL-05)

**Źródło katalogowe:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`.
**Claim:** `Docs/ModelFactory/claims/M05-006.json` (utworzony przed generowaniem).
**Zakres:** wyłącznie nowy model wizualny. Bez skanowania, pola ochronnego, fizyki bariery, zmian gameplayu lub ingerencji w DEF-001/DEF-002.

## Projekt i montaż

Kompaktowy moduł o dwóch odchylanych ramionach i dwóch dużych płytach projektujących pole. Z tyłu znajduje się frontowe mocowanie ramowe, na bokach dwa miedziane pierścienie łożysk, a z przodu dwie osobne ceramiczne płyty z ciemnoszklanymi oknami, cyjanowymi paskami emitera, bursztynowymi znacznikami ostrzegawczymi i widocznymi żebrami chłodzenia. Sylwetka jest wyraźna również bez emisji. Konstrukcja nie powiela skanera ani istniejących sensorów.

- **73 zamknięte części**, **2768 wierzchołków**, **2162 poligony**, **5260 trójkątów**, **7 materiałów**.
- Materiały: `Graphite`, `Ceramic`, `Copper`, `Rubber`, `Glass`, `Cyan` (emissive), `Amber` (emissive).
- Skala: **1 Blender unit = 1 m**. Układ źródłowy **Z-up**, kierunek do przodu **-Y**.
- Punkt montażu `M05-006_MOUNT_ROOT`: **(0,0,0)**, środek tylnej płaszczyzny styku obudowy z przednim gniazdem pojazdu.
- Oś lewego ramienia `M05-006_HINGE_L`: **(-0.34,-0.22,0)**, obrót **Z**, `0 → -15 → -30 → -15 → 0` stopni.
- Oś prawego ramienia `M05-006_HINGE_R`: **(0.34,-0.22,0)**, obrót **Z**, `0 → +15 → +30 → +15 → 0` stopni.
- Klatki kluczowe: **1,16,31,46,61**. Brak animacji mechanik tarczy, jedynie rozstawienie dwóch płyt.
- Pierścienie łożysk są geometrycznie puste wewnątrz; promieniowy prześwit między otworem pierścienia a obracającym się wałem: **18 mm**.

## Pliki

`geometry.json` — pojedyncze źródło geometrii, materiałów, mocowań i ruchu.

`generate_m05_006.py` — deterministyczny generator standardowego OBJ/MTL oraz sceny Blender bpy, animacji i GLB. Ponowne uruchomienie usuwa wyłącznie własną kolekcję i osierocone własne datablocki, zachowując cudze obiekty i przywracając zaznaczenie oraz stan osi czasu.

`M05-006.obj` i `M05-006.mtl` — rzeczywiste, zapisane neutralne modele statyczne. OBJ nie zawiera animacji.

`blender_smoke.py` — test headless: dwukrotne uruchomienie, zachowanie obcego obiektu, test klatek animacji, analiza zawartości GLB i biblioteki BLEND.

`.github/workflows/ci-model-m05-006.yml` — statyczna kontrola geometrii oraz rzeczywisty test Blender na GitHub Actions.

## Uruchamianie

```bash
python3 Tools/ModelFactory/M05/M05-006/generate_m05_006.py --self-test
python3 Tools/ModelFactory/M05/M05-006/generate_m05_006.py --output-dir /tmp/m05-006
blender -b --python Tools/ModelFactory/M05/M05-006/generate_m05_006.py -- --output-dir /tmp/m05-006-blender --require-blender
blender -b --python Tools/ModelFactory/M05/M05-006/blender_smoke.py -- /tmp/m05-006-smoke
```

Eksport `M05-006_animated.glb` jest wykonywany wyłącznie w Blenderze. `M05-006.blend` jest biblioteką kolekcji do dołączenia przez **File → Append**, a nie plikiem zawierającym obce obiekty otwartej sceny. Eksport GLB ogranicza się do `M05-006_ASSET`.

## QA i ograniczenia

**Static QA — PASS:** każda z 73 brył jest zamknięta i orientowana na zewnątrz, bez niezorientowanych krawędzi; unikalne nazwy, prawidłowe materiały, odtwarzalny OBJ/MTL, kontrola prześwitu łożysk oraz **31 próbek animacji (0–30° co 1°)**. Kontrola AABB obejmuje wszystkie pary elementów nieruchomych z ruchomymi i pary lewego/prawego ramienia; wyjątek dotyczy tylko współosiowych wałów wewnątrz pierścieni, gdzie prześwit jest sprawdzony analitycznie.

**Blender bpy / animowany GLB / .blend:** wyniki rzeczywistego workflow należy sprawdzić w GitHub Actions; sam PASS statyczny nie dowodzi udanego eksportu. **Unity 6 import, URP, PlayMode i ocena artystyczna: NOT RUN** do czasu uruchomienia projektu z poprawną aktywacją i inspekcji.

**Integracja:** wyłącznie Draft PR do `develop`. Nie modyfikować `Assets`, `ProjectSettings`, scen, mechanik DEF ani pozostałych modeli bez osobnej decyzji. Import GLB do Unity 6 może wymagać zgodnego importera glTF; alternatywnie należy użyć FBX z Blendera.

# M04-005 — Dopalacz dwukomorowy (MODEL-04)

**Zadanie:** M04-005 z katalogu `Docs/ModelFactory/CATALOG_V1.md` na gałęzi kontrolnej `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M04-005.json`, commit `e93a0b96d572949e642aeb78cc83213528c94b0a` (2026-10-24 14:53:30 UTC).

## Koncepcja i montaż

Dwa równoległe dopalacze na **tylnej powierzchni ramy**. Każdy ma otwartą cylindryczną komorę spalania, stopniowane pierścienie termiczne, miedziane żebra, ceramiczne osłony, świecącą bursztynową gardziel i niebieski pierścień jonowy. Cztery ruchome ceramiczne klapy na każdą dyszę dają łącznie osiem elementów zmieniających profil wylotu. Całość ma **62 części, 8 materiałów, 3792 wierzchołki i 7400 trójkątów** (w neutralnym OBJ).

- Układ źródłowy: **Blender Z-up**, +Y do przodu pojazdu, wydech w stronę **−Y**. Eksport glTF/GLB: Y-up.
- Skala: **1 Blender unit = 1 m**.
- `M04-005_MOUNT_ROOT`: **(0,0,0)** na płaszczyźnie tylnego gniazda ramy, powierzchnia montażowa Y=0.
- Komory: X=−0,45 i +0,45 m, Z=0; gardziele otwarte, a nie zakryte pełnym dyskiem.
- Każda klapa: własny pivot na promieniu **0,255 m**, Y=**−0,827 m**, oś styczna do obwodu w płaszczyźnie XZ. Ruch: **0° → +20° → 0°** (klatki 1,25,49), otwieranie na zewnątrz.
- Czysto wizualny model. Bez gameplayu, paliwa, siły odrzutu, fizyki, skryptów Unity, scen ani ProjectSettings.

## Pliki

- `geometry.json` — parametry, kolory PBR i animacja (źródło prawdy).
- `generate_m04_005.py` — deterministyczny generator OBJ/MTL w Pythonie i Blender `bpy` dla animowanego GLB oraz biblioteki `.blend`.
- `qa_mesh_m04_005.py` — niezależny audyt OBJ/MTL i raportu QA.
- `M04-005.obj`, `M04-005.mtl` — rzeczywisty statyczny model neutralny (OBJ **nie** zawiera animacji); generowane i weryfikowane przez CI.
- `M04-005.static-qa.json` — wynik testu źródła i geometrii.
- `.github/workflows/ci-model-m04-005.yml` — walidacja źródła, deterministyczności, wykonanie Blendera i weryfikacja GLB w CI.

## Uruchamianie

```bash
python3 Tools/ModelFactory/M04/M04-005/generate_m04_005.py --self-test --output-dir /tmp/m04-005
python3 Tools/ModelFactory/M04/M04-005/qa_mesh_m04_005.py --asset-dir /tmp/m04-005
blender --background --factory-startup --python Tools/ModelFactory/M04/M04-005/generate_m04_005.py -- --output-dir /tmp/m04-005-blender --require-blender
```

Generator `bpy` tworzy własną kolekcję `M04-005_ASSET`, a przy ponownym uruchomieniu usuwa **wyłącznie tę kolekcję**; nie kasuje innych obiektów sceny. GLB eksportuje tylko swoje obiekty. `M04-005.blend` jest biblioteką kolekcji do użycia przez **File → Append**, a nie pełnym zapisem otwartej sceny. Zapisuje i przywraca wcześniejsze zaznaczenie, aktywny obiekt oraz ustawienia klatek.

## Weryfikacja ruchu

Test w czystym Pythonie oblicza rzeczywiste wierzchołki ośmiu klap w **21 próbkach** od 0° do 20° (co 1°). Sprawdza, że wszystkie klapy znajdują się **za** wszystkimi elementami stałymi względem osi Y, co dowodzi braku ich wzajemnych przecięć bez fałszywie dodatnich alarmów AABB. Weryfikuje też odseparowanie obu komór w osi X i brak nakładania się sektorów kątowych klap w obrębie jednej komory. Każda część musi być zamkniętą siatką 2-manifold, bez trójkątów o zerowym polu, z dodatnią objętością i prawidłowymi indeksami.

**Wynik lokalnego Python QA:** `STATIC_QA_PASS`; 62/62 zamkniętych części, 9072 kontroli odległości ruchomych i stałych brył, minimalny prześwit w osi Y **15,654 mm**, odstęp między komorami w osi X **163,976 mm**. Ten test nie zastępuje wizualnego renderu, realnego importu ani kontroli materiałów w Unity 6.

## Integracja z Unity 6

Wybierz artefakt `M04-005_animated.glb` z pomyślnego przebiegu GitHub Actions. Użyj odpowiedniego importera glTF/GLB dla Unity 6 (lub przekonwertuj z `.blend` na FBX). Zweryfikuj: pivot tylnego mocowania, skalę 1 m, oś Y, animację ośmiu klap, normalne, URP emission i brak przecinania ramy pojazdu. **Import/PlayMode Unity nie zostały wykonane lokalnie** i nie należy raportować PASS bez osobnego uruchomienia.

**Izolacja:** Draft PR do `develop` wyłącznie z własnymi plikami M04-005 i workflow CI; bez merge.

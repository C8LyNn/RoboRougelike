# MODEL-03 / M03-010 — Pancerz segmentowy schodkowy

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na gałęzi kontrolnej `feature/rrl-model-factory-queue-v1`. Pozycja **M03-010** w wyłącznej kolejce M03; **przód ramy**, brak wymaganej animacji, wyróżnik: **warstwowa płytka**. Model wizualny — bez ingerencji w mechanikę obrażeń, Unity prefabrykaty, sceny i ProjectSettings.

## Rezerwacja ID przed produkcją

- `Docs/ModelFactory/claims/M03-010.json` na gałęzi kontrolnej, utworzony przez **CREATE**.
- Agent `M03`, `claimed_at_utc=2026-10-24T19:45:19.293Z`, `run=2026-10-24T19Z`.
- Commit CREATE: `2ae03af5caee75f2b35acbb633474cc67cedbddf`.
- SHA pliku po potwierdzonym CREATE: `080ea5d2a42d932d3a3e9f437d80bb2034969120`.
- Poprzednie ID M03-001…009 zajęte; brak istniejącego M03-010 w `develop`, gałęzi kontrolnej i otwartych PR podczas przydziału.

## Geometria i wygląd

Układ współrzędnych: **X** szerokość pojazdu, **Y** do przodu / na zewnątrz, **Z** pion. Jednostka = **1 metr**. Root pivot `RRL_M03_010_ROOT_PIVOT` znajduje się w `(0,0,0)` na tylnej płaszczyźnie czterech gniazd montażowych; normalna montażu **+Y**. Maksymalny obrys **1.136 × 0.137 × 0.740 m** (X×Y×Z), czyli X ±0.568, Y 0…0.137, Z ±0.370.

Wizualna konstrukcja:
- Jedna grafitowa płyta nośna 940×740×20 mm, Y=30…50 mm.
- **Pięć stalowych stopni** o ściętych narożnikach (zamknięte, rzeczywiste siatki, nie tekstura) o środkach Z `[-280,-140,0,140,280] mm`; każdy ma wysokość 120 mm i szerokość 870 mm. Między segmentami jest 20 mm przerwy, a ich fronty leżą kolejno na Y=**76, 88, 100, 112, 124 mm** (wzrost o 12 mm na poziom).
- **Pięć osobnych ceramicznych płytek** na frontach stalowych segmentów, każda grubości 13 mm i z 10 mm ścięciami narożników; płytki są wsunięte do obrysu segmentu.
- **Pięć miedzianych listew** na niższych fragmentach segmentów, grubości 7 mm, odsuniętych od ceramiki o 11 mm w Z. Widać fizyczne uskoki, materiały i odstępy.
- Dwie grafitowe szyny boczne i sześć bursztynowych znaczników. Znaczniki są osadzone na szynach bez objętościowego przenikania.
- **Cztery prawdziwe przelotowe pierścienie montażowe** (promień zewnętrzny 38 mm, otwór promień 20 mm) o środkach `(±0.530,0,±0.280)`. Każdy pierścień ma mostek kontaktujący go z płytą nośną. Otwory pozostają wolne, bez zaślepiania dekoracją.
- **Cztery locatory SNAP** `RRL_M03_010_SNAP_00…03`, odpowiadające gniazdom, z metadanymi osi `-Y` i promienia otworu.

Materiały PBR: `GraphiteBacker`, `TitaniumStep`, `CeramicTile`, `CopperLip`, `AmberWarning`, `TitaniumMount` (**6**). Model jest **statyczny**, więc animacja i testy klatek skrajnych **NOT REQUIRED** zgodnie z katalogiem.

## Źródło, eksport i QA

- `generate_m03_010.py` — kompletny deterministyczny generator `bpy`, samowystarczalny statyczny eksporter OBJ/MTL, raport JSON. Czyści tylko własną kolekcję i własne nieużywane siatki przy ponownym uruchomieniu; nie dotyka innych obiektów sceny.
- `qa_m03_010.py` — niezależny od generatora parser rzeczywistego OBJ; kontroluje orientację normalnych, brak degeneratów, zamknięcie wszystkich siatek, dodatnie objętości, **496** par brył bez objętościowego nakładania AABB, pięć stopni, pięć płytek, pięć listew, styki montażowe i otwarte otwory. Dwukrotnie regeneruje OBJ/MTL/JSON i wymaga identyczności bajtowej.
- `.github/workflows/ci-m03-010.yml` — izolowane joby statycznego QA oraz realnego uruchomienia Blendera, eksport GLB/FBX/BLEND, inspekcja binarnego GLB, kontrola ponownego wykonania w jednej sesji. Nie używa sekretów, nie modyfikuje gry.

Polecenia:
```bash
python3 Tools/ModelFactory/M03/M03-010/generate_m03_010.py --self-test
python3 Tools/ModelFactory/M03/M03-010/qa_m03_010.py
python3 Tools/ModelFactory/M03/M03-010/generate_m03_010.py --static-obj --out build/M03-010
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M03/M03-010/generate_m03_010.py -- --out build/M03-010
```

Wyniki workflow, gdy joby zakończą się sukcesem: `M03-010-static-OBJ-MTL` i `M03-010-Blender-GLB-FBX-BLEND`. Nie commitujemy binarnych plików do `develop` ani `main`. **Unity EditMode/PlayMode oraz oględziny w Unity: NOT RUN** — potrzebne osobne środowisko Unity z aktywacją. Pełna kontrola wyglądu z kamerą gry pozostaje do integracji.

**Merge: NO-GO bez review.** PR jest Draft do `develop`; nie zmienia gameplayu, prefabów, scen, ProjectSettings ani istniejących ID.

# M05-007 — Akumulator kasetowy / MODEL-05

**Katalog źródłowy:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M05-007.json` utworzony atomowo przez GitHub CREATE 2026-10-24T16:44:39.574Z, SHA `cdbed7d0887b10dfe025950ca75484669d90b506`.

## Zakres

Nowy, wyłącznie **wizualny** moduł czteroogniwowego akumulatora kasetowego z dwiema niezależnie wysuwanymi szufladami (po dwa ogniwa). Nie dodaje logiki baterii, ładowania, energii, fizyki, sensorów ani interakcji w Unity. Nie zmienia istniejących mechanik.

## Design i parametry

Otwarta rama grafitowa z ceramicznymi belkami, miedzianymi stopami montażowymi i szynami, dwa wysuwane kasetowe podajniki, cztery turkusowe cylindry ogniw, miedziane zaciski, pierścienie izolatorów, cyjanowe świetlne obręcze i pomarańczowe oznaczenia polaryzacji. Emisja jest dodatkiem do rzeczywistej geometrii, a nie jedyną formą.

- **79 zamkniętych części**, **5464 wierzchołki**, **4786 poligonów**, **10660 trójkątów**, **7 materiałów**.
- **Wymiary neutralne:** X 1,105 m, Y 0,795 m, Z 0,576 m. Przy maksymalnym wysunięciu kaset skrajny uchwyt sięga do Y=+0,514 m, a tylna krawędź pozostaje na Y=-0,3975 m: **całkowita długość robocza Y = 0,9115 m**; pełną przestrzeń roboczą sprawdzać z animacją.
- **Mocowanie:** wewnątrz ramy pojazdu, `M05-007_MOUNT_ROOT` = **(0,0,0)** na dolnej płaszczyźnie modułu. **1 unit = 1 m**, Z-up.
- **Ruch:** `M05-007_DRAWER_L` i `M05-007_DRAWER_R` przesuwają się wyłącznie w lokalnym kierunku **+Y**, zakres **0–0,21 m**, bez obrotu. Pivots odpowiednio **(-0,255,0,0)** i **(0,255,0,0)**. Klatki **1, 16, 31, 46, 61**: lewa kaseta wysuwa się jako pierwsza, prawa z opóźnieniem; obie wracają do położenia spoczynkowego.
- Cztery cylindry pozostają osadzone we własnych szufladach. Brak animacji samych ogniw.

## Pliki

`generate_m05_007.py` — kompletny, deterministyczny generator Python 3.10+ z opcjonalnym bpy, testem topologii i 22-próbkowym testem przesuwu.

`M05-007.obj`, `M05-007.mtl` — zapisane w repozytorium, statyczne assety w pozycji neutralnej. OBJ nie zachowuje animacji.

`blender_smoke.py` — rzeczywisty smoke test uruchamiany w Blenderze: dwukrotne uruchomienie generatora, kontrola obcych obiektów, liczby datablocków, klatek ruchu, animowanych węzłów GLB i biblioteki BLEND.

`.github/workflows/ci-model-m05-007.yml` — niezależny test statyczny oraz Blender na GitHub Actions; wygenerowane GLB/BLEND są przechowywane jako artefakt workflow (nie są automatycznie importowane do Unity).

## Uruchamianie

```bash
python3 Tools/ModelFactory/M05/M05-007/generate_m05_007.py --self-test
python3 Tools/ModelFactory/M05/M05-007/generate_m05_007.py --out /tmp/m05-007
blender -b --python Tools/ModelFactory/M05/M05-007/blender_smoke.py -- /tmp/m05-007-blender
```

W Blenderze 4.x: `blender -b --python Tools/ModelFactory/M05/M05-007/generate_m05_007.py -- --out /tmp/m05-007 --require-blender`.

Generator usuwa wyłącznie kolekcję `M05-007_ASSET` i osierocone datablocki ze swoim prefiksem. Zachowuje inne obiekty sceny i przywraca wcześniejszy stan zaznaczenia oraz osi czasu.

## QA

**STATIC PASS:** uruchomienie generatora na GitHub Actions: 79 zamkniętych, poprawnie zorientowanych brył, 22 próbki ruchu (0–0,21 m co 0,01 m), **28 336 testów AABB** między ruchomymi elementami i nieruchomą ramą, brak wykrytych przenikań, deterministyczne OBJ/MTL. Kontrola AABB nie zastępuje dokładnej kontroli siatek w Blenderze/Unity.

**BLENDER:** wynik jest potwierdzony tylko wtedy, gdy `M05-007.blender-qa.json` ma `BLENDER_PASS`, a workflow weryfikuje ten plik i wyeksportowane GLB/BLEND. Sam kod wywołujący Blender ani jego kod zakończenia nie są wystarczającym dowodem (Blender może zakończyć proces kodem 0 mimo wyjątku w skrypcie).

**UNITY:** import, materiały URP, EditMode, PlayMode, wygląd w grze i ręczna kontrola artystyczna **NOT RUN**. Unity może wymagać importera glTF; GLB/BLEND nie stanowią dowodu udanego importu Unity.

**Integracja:** tylko osobny Draft PR do `develop`. Brak zmian scen, prefabów, ProjectSettings, gameplayu i pozostałych kolejek.

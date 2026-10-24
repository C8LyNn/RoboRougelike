# M02-001 — Segmented Off-road Wheel / Koło terenowe z segmentowym bieżnikiem

**Katalog:** M02-001 · **Agent:** M02 · **Źródło:** `Docs/ModelFactory/CATALOG_V1.md` na gałęzi `feature/rrl-model-factory-queue-v1`.

## Zawartość

- `generate_m02_001.py` — proceduralna siatka koła, 60 segmentów bieżnika (3 pasy × 20), 8 szprych na stronę, 16 śrub, wielomateriałowy wygląd.
- Pivot: środek osi `(0,0,0)`; obrót wokół lokalnej **osi +X**; mocowanie: boczna oś pojazdu.
- Promień korpusu: 0,500 m, promień z klockami: 0,534 m; szerokość nominalna: 0,340 m; 7 materiałów.
- `MOUNT_AXLE` → `WHEEL_ROTATOR` → części; animacja referencyjna: pełny obrót, 60 klatek / 60 FPS.
- Klocki mają przerwy kątowe i między pasami; połączenia szprych z piastą/obręczą oraz klocków z gumą są celowo zakładkowe, jak w fizycznej konstrukcji.

## Generowanie

**Blender 4.2+ (zalecane):**

```sh
blender --background --python Tools/ModelFactory/M02/M02-001/generate_m02_001.py -- --out build/M02-001
```

Powstają `.fbx` (Unity-native), `.glb` (portable) i manifest JSON. Przed importem do Unity zweryfikuj orientację, pivot i mapowanie materiałów w prefabie. Właściwą rotację koła podczas jazdy powinien sterować kod pojazdu; animacja to referencyjny turntable, nie mechanika gameplayu.

**Środowisko bez Blendera:**

```sh
python -m pip install trimesh
python Tools/ModelFactory/M02/M02-001/generate_m02_001.py --self-test
python Tools/ModelFactory/M02/M02-001/generate_m02_001.py --out build/M02-001
```

Fallback tworzy rzeczywisty **statyczny GLB** (bez animacji i FBX), co jest jawnie zaznaczone w manifeście. Blender nie jest wymagany do testu topologii ani generowania statycznej siatki.

## QA / integracja

- `--self-test` sprawdza unikalność nazw, brak degeneratów, materiały, granice, liczbę klocków i śrub.
- Zakres obrotu koła jest bezkolizyjny **wewnątrz modelu**, ponieważ wszystkie części są dziećmi jednego rotatora. Prześwit względem ramy pojazdu trzeba sprawdzić w docelowym prefabie.
- Collidery: rekomendowany prosty cylinder/koło, **nie** siatka z 60 osobnymi klockami.
- Nie zmieniono istniejącego sterowania, scen, prefabu ani systemów Unity. **Unity Play Mode: NOT RUN**.
- Ten pakiet jest wizualnym zasobem prototypowym, nie zastępuje istniejącej mechaniki koła.

## Rezerwacja

`Docs/ModelFactory/claims/M02-001.json` na gałęzi kontrolnej; model wolno aktualizować tylko w ramach tego ID/PR. 

# M02-001 — Segmented Off-road Wheel / Koło terenowe z segmentowym bieżnikiem

**Katalog:** M02-001 · **Agent:** M02 · **Źródło:** `Docs/ModelFactory/CATALOG_V1.md` na gałęzi `feature/rrl-model-factory-queue-v1`.

## Zawartość

- `generate_m02_001.py` — proceduralna siatka koła, 60 segmentów bieżnika (3 pasy × 20), 8 szprych na stronę, 16 śrub, wielomateriałowy wygląd.
- Pivot: środek osi `(0,0,0)`; obrót wokół lokalnej **osi +X**; mocowanie: boczna oś pojazdu.
- Promień korpusu: 0,500 m, promień z klockami: 0,534 m; szerokość nominalna: 0,340 m; 7 materiałów.
- `MOUNT_AXLE` → `WHEEL_ROTATOR` → części; animacja referencyjna: pełny obrót, 60 klatek / 60 FPS.
- Klocki mają przerwy kątowe i między pasami; połączenia szprych z piastą/obręczą oraz klocków z gumą są celowo zakładkowe, jak w fizycznej konstrukcji.

## Gotowe artefakty w repozytorium

W katalogu `generated/` zapisano **rzeczywisty model**:

- `M02-001_segmented_offroad_wheel.obj` + `.mtl` — statyczna siatka do importu w Unity bez dodatkowego importera; Y-up, obrót wokół +X.
- `M02-001_segmented_offroad_wheel.glb` — gotowy glTF 2.0 GLB z 7 materiałami PBR i **animacją referencyjną** `Wheel_Spin_60RPM` (1 sekunda, 5 klatek kluczowych, pełny obrót). Ten plik powstał przez bezpośrednie pakowanie geometrii OBJ do glTF, **nie przez uruchomienie Blendera**. Do importu GLB w Unity potrzebny jest kompatybilny importer glTF; bez niego użyj OBJ lub wyeksportuj FBX z Blendera.
- `M02-001_static_qa.json` — raport statycznego sprawdzenia OBJ.

Zapisano 100 części, 3360 wierzchołków, 6336 trójkątów i 60 klocków bieżnika. Obecność modelu GLB w repozytorium nie oznacza, że wykonano Blender/Unity Play Mode.

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

## Iteracja poprawkowa M02-001 — przelotowa piasta i weryfikacja (24.10.2026)

**Zidentyfikowany problem:** oryginalna piasta `Axle_Hub` i obie zaślepki `Hub_Cap_L` / `Hub_Cap_R` były pełnymi walcami. Zasłaniały rzeczywisty montażowy kanał osi pojazdu, mimo że logiczny znacznik `MOUNT_AXLE` był ustawiony prawidłowo.

**Zmiana modelu:** wszystkie trzy pełne walce zastąpiono zamkniętymi pierścieniowymi siatkami. Promień otworu wynosi **0,043 m** (średnica **86 mm**), a dla przyjętej referencyjnej osi o średnicy 68 mm pozostaje **9 mm** luzu promieniowego. Otwory są współosiowe wokół lokalnej osi +X. Zachowano 60 klocków bieżnika, kolorystykę, 7 materiałów i hierarchię `MOUNT_AXLE → WHEEL_ROTATOR`.

**Nowe QA w generatorze:** `validate_axle_bore()` sprawdza minimalny promień otworu, jego zgodność we wszystkich trzech częściach i to, czy każda krawędź trójkątów występuje dokładnie dwukrotnie (siatka zamknięta / manifold). Wywoływane także przez `--self-test`.

**Zweryfikowane w GitHub Actions:** [run 40095210640](https://github.com/C8LyNn/RoboRougelike/actions/runs/40095210640) — PASS: walidacja oryginalnej paczki, źródłowe testy samokontroli, odtworzenie z poprawionego generatora oraz archiwizacja wygenerowanego statycznego GLB jako artefaktu `M02-001-through-bore-GLB-static`.

**Ważne dla użytkownika:** pliki `generated/*.obj` i `generated/*.glb` dołączone we wcześniejszym PR #40 są nadal wersją **sprzed poprawki**, ponieważ nie zostały nadpisane. Poprawiony model pobierz z artefaktu powyższego przebiegu GitHub Actions albo wykonaj generator samodzielnie. Artefakt z CI to **statyczny** GLB; do uzyskania animowanego FBX/GLB z poprawioną piastą wymagany jest Blender. Blender, import/PlayMode Unity oraz kontrola wizualna: **NOT RUN**. Nie scalać na `develop` przed zatwierdzeniem importu i QA.

**Integracja:** przed przyjęciem poprawki do bazowego PR #40 należy podmienić stare pliki `generated/` na odpowiadające poprawionemu generatorowi, ponownie wygenerować manifest, zaktualizować `qa_assets.py` do nowych liczebności siatek i sprawdzić tę samą geometrię w Unity. Tego etapu nie oznaczono jeszcze jako ukończony.

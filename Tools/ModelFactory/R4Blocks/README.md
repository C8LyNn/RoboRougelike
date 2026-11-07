# R4Blocks — podstawowe klocki konstrukcyjne (30 kształtów)

Biblioteka art-only: zestaw klocków budulcowych w stylu R4/R41, na tej samej siatce i z tym samym portem co bronie z PR #939 i moduły ruchu z PR #941.
Biblioteka niczego nie podpina do gry. Nie zmienia `Assets/`, scen, prefabów, runtime, fizyki, Save/Load ani ekonomii.

**Status: BLENDER_PASS 30/30 · test zgodności PASS · UNITY_NOT_RUN 30/30.** Unity nie był dostępny, więc żaden import do Unity nie został wykonany.

Dane maszynowe są w [INDEX.json](INDEX.json), a sumy kontrolne w [SHA256SUMS.txt](SHA256SUMS.txt).

## Skąd te kształty

Klocki to standardowy zestaw kształtów gier voxel-builder, jaki znają gracze Robocraft i Trailmakers:
- sześcian,
- klin,
- narożniki,
- zaokrąglenia,
- połówki,
- belki,
- płyta,
- kratownica.

Geometria, faktura i detale są własne, w stylu R4: ciemny i jasny pancerz, wpuszczone panele, porty R4 i paleta R41. Nie ma tu tekstur, logo ani modeli skopiowanych z tamtych gier.

## Klocki

| Klocek | Kształt | Komórki | Porty (pełne + częściowe) | ContactMask kotwicy | Tri |
|---|---|---|---|---|---|
| R4B_Cube | sześcian 1×1×1, port na każdej ścianie | 1 | 6 + 0 | 63 | 5 036 |
| R4B_Wedge | klin (pryzmat), skos przód-dół → tył-góra | 1 | 2 + 2 | 43 | 3 252 |
| R4B_Tetra | narożnik czworościenny | 1 | 0 + 3 | 42 | 2 306 |
| R4B_InnerCorner | narożnik wewnętrzny (sześcian ze ściętym rogiem) | 1 | 3 + 3 | 63 | 4 826 |
| R4B_CornerWedge | narożnik piramidalny | 1 | 1 + 2 | 42 | 2 416 |
| R4B_RoundEdge | zaokrąglona krawędź (ćwierć walca) | 1 | 2 + 2 | 43 | 3 428 |
| R4B_RoundCorner | zaokrąglony narożnik (oktant kuli) | 1 | 0 + 3 | 42 | 2 782 |
| R4B_HalfBlock | półklocek 1×1×½ | 1 | 1 + 4 | 59 | 4 716 |
| R4B_HalfWedge | niski klin ½ | 1 | 1 + 1 | 40 | 1 652 |
| R4B_Cylinder | walec (oś pionowa) | 1 | 0 + 2 (łaty na dnie i wierzchu walca) | 12 | 3 202 |
| R4B_Beam2 | belka 2×1×1, jedna sztywna część | 2 | 10 + 0 | 62 | 8 184 |
| R4B_Beam3 | belka 3×1×1, jedna sztywna część | 3 | 14 + 0 | 62 | 11 296 |
| R4B_LongWedge | długi klin 1×2 (łagodny skos) | 2 | 3 + 2 | 43 | 4 030 |
| R4B_Plate2x2 | płyta 2×2×½ | 4 | 4 + 0 (spód) | 8 | 6 284 |
| R4B_TrussCube | ażurowa kratownica 1×1×1 | 1 | 0 + 6 (kwadratowe piasty ±0,17) | 63 | 7 140 |
| R4B_Pyramid | piramida o kwadratowej podstawie | 1 | 1 + 0 | 8 | 972 |
| R4B_Dome | kopuła na płycie (osłona sensora / kokpitu) | 1 | 1 + 0 | 8 | 3 270 |
| R4B_Cone | stożek (nos), okrągła podstawa | 1 | 0 + 1 | 8 | 1 700 |
| R4B_HexColumn | kolumna sześciokątna | 1 | 0 + 2 | 12 | 1 764 |
| R4B_OctColumn | kolumna ośmiokątna (ścięte pionowe krawędzie) | 1 | 2 + 4 | 63 | 4 844 |
| R4B_RoundTop | półklocek z półwalcem na górze | 1 | 1 + 4 | 59 | 4 190 |
| R4B_Arch | łuk (tunel r 0,17 m pod klockiem), mocowanie od góry | 1 | 3 + 2 | 55 | 4 564 |
| R4B_Step | stopień schodów | 1 | 2 + 4 | 63 | 4 966 |
| R4B_CoveEdge | wklęsła krawędź (odwrotność zaokrąglenia, r 0,40 m) | 1 | 2 + 2 | 43 | 3 344 |
| R4B_CornerL3 | narożnik L, 3 komórki | 3 | 14 + 0 | 46 | 11 764 |
| R4B_Tee4 | trójnik T, 4 komórki | 4 | 18 + 0 | 44 | 15 142 |
| R4B_Slab2x2 | blok 2×2×1 | 4 | 16 + 0 | 46 | 13 356 |
| R4B_BigCube2 | duży sześcian 2×2×2 | 8 | 24 + 0 | 42 | 20 012 |
| R4B_Plate1x2 | płyta 1×2×½ | 2 | 2 + 2 | 10 | 4 752 |
| R4B_WideWedge | szeroki klin 2×1×1 | 2 | 4 + 2 | 42 | 4 812 |

Dokładna lista portów (komórka, kierunek, rodzaj, offset i połowa łaty) jest w `R4B_*/R4B_*_QA.json` oraz w `INDEX.json`.

Zestaw 2 (15 kolejnych kształtów od Pyramid do WideWedge) zbudowano tym samym skryptem i tym samym QA. Arch nie ma portu od spodu (tunel), dlatego jego `Socket_Mount` stoi na górnej ścianie.

## Siatka i porty — zgodność z bronią i ruchem

- Klocki stoją na siatce 0,55 m. Ściana bloku leży 0,26 m od środka komórki, co daje BlockSize 0,52, tak jak `R41PartGeometry`. Szczelina między sąsiadami wynosi 30 mm.
- Port pełny ma ten sam wzór co port R4 w PR #939 i #941:
  - kwadrat 0,509 m, równo ze ścianą, wyśrodkowany,
  - wpuszczone złącze centralne, pierścień i 4 otwory ustalające.

  Moduł z portem R4 pasuje więc na każdą pełną ścianę klocka.
- Porty częściowe to ściany trójkątne lub połówkowe. Są opisane jako łaty `BuildSocket` (offset i half ≤ 0,5).
- `ContactMask` liczony jest dla komórki kotwicy, w kolejności `R3MountingGrid`: right, left, up, down, forward, back (bity 0–5, osie Unity).
- Klocki wielokomórkowe (Beam2, Beam3, LongWedge, Plate2x2, CornerL3, Tee4, Slab2x2, BigCube2, Plate1x2, WideWedge) mają listę komórek i `bonds` (`BuildCellBond`).
- Empties w każdym pliku:
  - `Port_<kierunek>_<komórka>[_part]`,
  - `Socket_Mount` (spód),
  - `R4B_*_ROOT` z właściwościami.

## Test zgodności (`compat/`)

Plik `scripts/compat_assembly.py` składa pojazd testowy z 24 elementów:
- klocki,
- prawdziwy wyeksportowany `R4W_Basic.glb` (PR #939),
- 4 × `R4M_Wheel.glb` oraz `R4M_Booster.glb` (PR #941).

Ścieżki i SHA-256 tych plików są zapisane w `compat_assembly.json`.

Sprawdzane są trzy rzeczy:
- każda para stykających się portów musi być dokładnie 30 mm od siebie wzdłuż normalnej i mieć 0 mm przesunięcia stycznego;
- każdy moduł broni i ruchu musi stać na porcie klocka;
- żadne dwa elementy nie mogą się przenikać.

**Wynik: PASS.** 38 par portów, odstęp 30 mm i przesunięcie 0 mm. Działo, 4 koła i booster stoją na portach. Przenikań: 0.

Rendery: `compat/compat_assembly_{34front,side,34rear}.jpg`. Statyczny model: `compat/R4_Compat_Vehicle.glb`.

## Co sprawdzono w Blenderze 5.2.3 (każdy klocek)

- Topologia: manifold, spójne normalne, brak zdegenerowanych ścian, UV.
- Geometria: geometria w zadeklarowanych komórkach.
- Porty: test łaty styku promieniami. Ściana pełna wymaga ≥ 80% płaskiej powierzchni, częściowa ≥ 65%. Nic nie może wystawać ani brakować.
- Paleta: materiały R41 (Armor_Dark, Armor_Gunmetal, Armor_Plate_Light, Steel_Light, Hazard_Yellow…).
- Eksport: FBX (Unity 6 URP, −Z forward, Y up, FBX_SCALE_UNITS, `bake_space_transform=False`) i GLB z ponownym importem. Odchyłka obrysu < 0,05 mm, PASS.
- Kolizje: `*_colliders.glb` (wypukłe UCX).
- Rendery: 3/4 przód, 3/4 tył, spód (`R4B_*/renders/`).

## Podgląd

Plik `preview/BLOCKS_PREVIEW.zip` zawiera podgląd. Po rozpakowaniu otwórz `index.html`:
- 30 klocków i pojazd testowy (prawdziwe GLB osadzone w `models.js`),
- orbit i zoom, komórka 0,55 m, porty,
- szereg wszystkich klocków.

three.js r128 (MIT) jest w `lib/`, więc internet nie jest potrzebny. Bez WebGL otwórz `offline.html`.

Źródła podglądu są w `scripts/preview_src/`, a generator to `scripts/build_preview_blocks.py`.

## Ryzyka / do integracji (poza zakresem tej biblioteki)

- `UNITY_NOT_RUN`: import, materiały URP i kolizje w Unity nie były testowane.
- Runtime zna dziś tylko klocek Armor (sześcian). Każdy nowy kształt wymaga osobnej pracy integracyjnej:
  - rejestracji rodzaju i prefabu,
  - per-kształtowego `ContactMask`,
  - łat portów częściowych.
- `R3BuildValidation` używa dziś łat 0,47. Porty częściowe (trójkąty, połówki) mają mniejsze łaty i będą wymagały obsługi `BuildSocket` offset/half.
- Klocki wielokomórkowe wymagają obsługi footprintu wielu komórek (`R3VehicleModel.OccupiedCells` zakłada dziś jedną komórkę na część).
- Rotacje i lustra klinów oraz narożników zostają po stronie runtime (transform). Biblioteka daje jedną orientację każdego kształtu.

## Odtworzenie

- `scripts/build_blocks.py` (z `r4lib.py`) buduje w Blenderze headless wszystkie klocki i QA.
- `scripts/compat_assembly.py` wykonuje test zgodności. Potrzebuje GLB z PR #939 / #941.
- `scripts/make_index_blocks.py` odświeża `INDEX.json` i `SHA256SUMS.txt`.

Ścieżki lokalne w skryptach są zapisane jako `%USERPROFILE%`. Dostosuj je.

# R4ConnectorRods1182 — 20 łączników 3D: pręty (ROD) i rozpory (STRUT) dla triforcingu (#1182)

To biblioteka wyłącznie art: 12 prętów i 8 rozpór. Działają w **istniejącym** standardzie R4, tym samym co:
- klocki z PR #963,
- bronie z PR #939,
- moduły ruchu z PR #941,
- warianty kół z PR #1018.

To nie jest nowy standard. Nic nie jest podpięte do gry: nie zmieniałem `Assets/`, scen, prefabów, `R3PartKind`, obrażeń, Save/Load, fizyki ani ekonomii. Wcześniejsze modele zostały bez zmian.

**Status:**
- BLENDER_PASS: 20/20.
- Obroty 24 + lustro: 20/20 PASS.
- Zgodność z 36 prawdziwymi modelami: 3 264 montaże, 0 błędów styku.
- Konstrukcje testowe: 5/5 PASS.
- Unity: **UNITY_NOT_RUN 20/20**, bo Unity nie był dostępny.

Pliki pomocnicze:
- tabela: [INDEX.md](INDEX.md);
- dane maszynowe: [INDEX.json](INDEX.json);
- sumy: [SHA256SUMS.txt](SHA256SUMS.txt);
- integracja: [CODEX_INTEGRATION_NOTES.md](CODEX_INTEGRATION_NOTES.md).

## Standard — zmierzony, nie założony

Wymiary wziąłem z **eksportowanych GLB** (osie Unity) wcześniejszych modeli (`compat/compat_all.json → standard_measured`), a nie z wyglądu:

| Model | Socket | Najdalszy punkt wzdłuż normalnej socketu | Pół-wymiar płyty portu |
|---|---|---|---|
| R4B_Cube | (0; −0,26; 0) | 0,26 m | 0,26 m |
| R4B_Wedge | (0; −0,26; 0) | 0,26 m | 0,26 m |
| R4W_Basic | (0; −0,26; 0) | 0,26 m | 0,2565 m |
| R4M_Wheel | (0; +0,26; 0) | 0,26 m | 0,2563 m |
| R4M_WheelM_Side_Offroad | (−0,26; 0; 0) | 0,26 m | 0,2563 m |

Wynika z tego:
- siatka ma 0,55 m;
- ściana bloku leży 0,26 m od środka komórki;
- między sąsiadami jest szczelina 30 mm;
- port R4 ma 0,509 m, a wzór portu (rowek, gniazdo, 4 otwory) jest ten sam co w `R4Blocks/build_blocks.py`;
- paleta R41 jest zablokowana;
- osie: Unity +Z przód, +Y góra; Blender = (x, −z, y).

## Budowa łącznika

Łącznik składa się z **węzłów** i **członów**:
- **Węzeł** to komórka z jawnie zadeklarowanymi portami.
- **Człon** to pręt albo belka (prosta lub ćwiartka łuku) między węzłami. **Człony nie mają portów.** Nic nie łączy się „po drodze” wzdłuż pręta, więc nie ma automatycznych portów.

| Rodzina | Węzeł | Port | Co można przyłączyć |
|---|---|---|---|
| ROD | mały hub, stub i płytka 0,23 m | `rod_mini`, tylko na końcach: na ścianach naprzeciw członu, bez portów bocznych; huby skrzyżowań nie mają portów | klocki i inne łączniki — **tylko konstrukcyjnie**. Broń, ruch i koła wymagają pełnego portu. |
| STRUT | węzeł wielkości bloku (0,52 m) z ciemnymi krawędziami | `r4_full`, ten sam port 0,509 m co klocki, na każdej wolnej ścianie | klocki, łączniki, broń, ruch, koła |

Każdy model ma:
- `R4C_<ID>.blend`, `.fbx` i `.glb`;
- `_colliders.glb` — propozycja: wypukłe kawałki UCX;
- `_QA.json`;
- `_connector_manifest.json` — zawiera:
  - part_id, family, version, grid, bbox;
  - `occupied_cells` jako jawną listę z rolą: node, span, crossing;
  - `connection_ports` z pozycją, normalną, typem, allow/deny i bitem ContactMask;
  - pivot, `Socket_Start` / `Socket_End`;
  - obroty i lustro, visual vs collision;
  - regułę połączenia;
  - masę, HP i koszt jako **PROPOSAL_ONLY**;
- `_model_manifest.json`;
- `renders/` z widokami front, side, 3/4 i 3/4 wireframe oraz kadrem planszy.

Pilot (#1182) zrobiłem i przetestowałem przed resztą: ROD_SHORT, ROD_LONG, ROD_DIAG_2D_LONG, STRUT_LONG, STRUT_ARC.

## Co sprawdzono (Blender 5.2.3, na prawdziwych plikach)

1. **Model** (`*_QA.json`):
   - topologia: manifold, normalne, UV, 1 powłoka;
   - paleta i budżet trójkątów (pręt 14 000, rozpora 22 000);
   - reimport FBX i GLB: odchyłka obrysu < 0,05 mm;
   - styk każdego portu: płaska łata, nic nie wystaje — r4_full ≥ 80% płasko, rod_mini ≥ 65%;
   - geometria nie wchodzi w komórki spoza `occupied_cells`.
2. **Zgodność z wcześniejszymi modelami** (`compat/compat_all.json`, `scripts/connector_compat.py`):
   - Na każdym porcie montowałem prawdziwe GLB:
     - na każdym porcie: klocki Cube, Wedge, Plate2x2, HalfBlock, Beam3;
     - na portach `r4_full` dodatkowo 8 broni, 11 modułów ruchu i 12 kół.
   - Każdy montaż w 4 obrotach i w 10 klatkach animacji.
   - Wynik: 3 264 montaże, **0 błędów styku**, szczelina 30 mm.
   - Przenikania: wszystkie porty są wolne poza 2 wewnętrznymi portami STRUT_ARC (`P_A_right`, `P_B_back`):
     - Plate2x2, Beam3, RotarySaw i MagneticGrapple pasują tam tylko w części obrotów;
     - koło XL Monster nie mieści się w żadnym obrocie.

     To ta sama klasa ograniczenia co wewnętrzne ściany CornerL3 i Tee4 w #963. Zgłaszam to, nie ukrywam.
3. **Triforcing, czyli sąsiad ≠ połączenie:**
   - Prawdziwy R4B_Cube postawiłem w każdej wolnej komórce sąsiedniej każdego łącznika — 468 przypadków.
   - 0 przenikań.
   - 132 przypadki to połączenia, bo port łącznika patrzy na kostkę.
   - 336 przypadków to stykanie bez połączenia (minimalna szczelina 0,131 m).
4. **Obroty i lustro** (`rotations.check` w manifeście):
   - Każdy łącznik sprawdzony w 24 obrotach i 24 obrotach po lustrze X.
   - Komórki pozostają unikalne, porty leżą na zajętych komórkach i patrzą na wolne.
   - 20/20 PASS.
   - U wszystkich 20 łączników lustro daje to samo co jakiś obrót, więc nie trzeba osobnych modeli lustrzanych.
5. **Konstrukcje ze starymi modelami** (`assemblies/`, `scripts/assemble.py`) — 5/5 PASS:

   | Scena | Co pokazuje | Części | Pary portów | Przenikania | Obok bez połączenia |
   |---|---|---|---|---|---|
   | `two_robots` | A: pancerz z klinów przykręcony do kadłuba · B: ściana pancerza 2 komórki dalej na dwóch ROD_SHORT (łącze redundantne), kostka między prętami styka się z nimi, ale nie łączy | 40 | 51 | 0 | 2 |
   | `long_mast` | maszt STRUT_LONG z railgunem; montaż pojedynczy, więc wykryty jako most | 4 | 3 | 0 | 0 |
   | `braced_mast` | maszt STRUT_STANDARD z zastrzałem ROD_DIAG_2D_LONG; zamknięta pętla, zastrzał mija belkę i maszt bez łączenia | 6 | 6 | 0 | 2 |
   | `arc_outrigger` | dwa STRUT_ARC (prawy i **lustrzany**) z kołem z piastą boczną i kołem skrętnym | 8 | 7 | 0 | 0 |
   | `cross_deck` | pokład STRUT_CROSS (4 łącza) na dwóch Beam3, zderzak ROD_FRAME, 2 × BeamCutter | 6 | 8 | 0 | 3 |

   Dla każdej sceny sprawdziłem:
   - styk 30 mm każdej pary portów, po promieniach z obu stron;
   - przenikanie BVH każdej pary części;
   - konflikty komórek;
   - regułę typów portów — testy negatywne, np. moduł na `rod_mini` zostaje odrzucony;
   - graf połączeń: spójność z kotwicą, łącza pojedyncze (mosty) i części krytyczne.

   Znaczniki: zielony = para połączona, pomarańczowy = wolny port, czerwony = sąsiedztwo bez połączenia.

## Podgląd

`preview/CONNECTORS_1182_PREVIEW.zip` — po rozpakowaniu otwórz `index.html`. Działa offline, z three.js r128 w `lib/`. Zawiera:
- 20 łączników z komórkami i portami;
- 5 konstrukcji z prawdziwymi wcześniejszymi modelami i znacznikami styku;
- planszę wszystkich 20 w jednej skali (`board_all20.jpg`).

## Znalezione niezgodności i propozycje

- **Części wielokomórkowe z komórkami „przelotowymi”.** Runtime R3 umieszcza dziś części jednokomórkowe. Łączniki zajmują do 15 komórek: węzły, przeloty i komórki muśnięte grubością skosu. Propozycja: rezerwować wszystkie `occupied_cells` (patrz notatki dla Codexa). Rezerwację komórek muśniętych można zluzować dopiero po decyzji projektowej.
- **`rod_mini` jest mniejszy od pełnego portu.** Styk z klockiem jest płaski i poprawny (BuildSocket ma 0,175 komórki², a minimum to 0,01), ale broni i ruchu tam nie wolno. Gra musi znać typ portu, nie tylko maskę ścian.
- **Wewnętrzne porty łuku STRUT_ARC** — duże moduły się tam nie mieszczą. Gra powinna sprawdzać kolizję montażu, a nie tylko zgodność ścian.
- **STRUT_SHORT** to dwa węzły bez widocznego członu (1×2). Jest czytelny po ciemnych krawędziach i kołnierzach, ale sylwetką przypomina 2 Cube. To decyzja do akceptacji.
- **Pręty o 4–5 węzłach** (CROSS, FRAME) przekraczały budżet 14 000 tri. Zmniejszyłem liczbę segmentów tylko we wzorze `rod_mini` (skala 0,45). Pełny port ma bez zmian te same segmenty co klocki.

## Odtworzenie

- `scripts/build_connectors.py -- <out> [ID,...]` — jeden generator parametryczny (katalog `CAT`) plus `r4lib.py` (kopia z #963).
- `scripts/connector_compat.py -- <wynik> <out.json> [ID,...]` — test z GLB z #963, #939, #941 i #1018.
- `scripts/assemble.py -- <wynik> <out> [scena,...]` — konstrukcje.
- `scripts/make_manifests.py` — manifesty, obroty, INDEX, SHA256SUMS.
- `scripts/build_viewer1182.py` + `viewer1182_template.html` — podgląd.

Ścieżki lokalne usunąłem. Pliki `.blend` są zapisane bez kompresji, a ścieżki zastąpiłem `//` z zachowaniem długości; SHA w `*_QA.json` są po tej zmianie.

# R4SixStyles1 - seria 1: cztery elementy × sześć stylów (przegląd artystyczny)

**Status:** propozycja do przeglądu artystycznego. Nic tu nie jest podpięte pod grę: domyślne prefaby, mechanika i publikacja
pozostają do osobnej decyzji. Wszystko leży w tym jednym katalogu, na osobnej gałęzi `art/r4-six-styles-s1` (od `main`).
Oryginały na innych gałęziach są nietknięte; modele zachowane bez zmian są tu kopiami bajt w bajt.

Elementy: działo `R4W_Basic`, koło `R4M_Wheel`, wyrzutnik `R4X_FlyingDroneDeployer`, dron `FlyingDrone`.
Style w stałej kolejności: 01 Clean Futuristic, 02 Brutal Industrial, 03 Arcade Low-Poly, 04 Gritty Industrial,
05 Comic Outline, 06 R41 Modular Tech.

## Plansze (`boards/`)

| plik | co pokazuje |
|---|---|
| `01_porownanie_6_stylow.jpg` | **plansza 1**: 6 stylów × 4 elementy, wspólna kamera (2,3 m, 50 mm), skala, światło i tło; ostatni wiersz to wycinek 2× drona z tego samego renderu; znaczniki ZACHOWANY / PRZEROBIONY / NOWY |
| `01g_szarosc_bez_podpisow.jpg` | ta sama siatka w glinie, bez podpisów i bez koloru; 05 bez konturu (test samej bryły) |
| `02_perspektywa_gracza.jpg` | **plansza 2**: ten sam prosty pojazd w 6 stylach (koła, działo, wyrzutnik na kadłubie, dron po starcie); klatka gracza 1920×1080, pojazd ~20 m od kamery, wycinek 1:1 pikseli i bliski widok 3/4 |
| `00_przed_i_po.jpg` | stan wyjściowy (najlepsze istniejące modele) nad dostarczonym modelem w każdej pozycji |
| `03_wyrzutnik_i_dron.jpg` | para wyrzutnik + dron: wspólna cecha konstrukcyjna, gniazdo z góry, start (Docked / Launch / Fly), szarość |
| `04_rodzina_R41.jpg` | stare modele R4 (działo #939, koło #941) obok nowego wyrzutnika i drona 06 |
| `05_kontur_z_i_bez.jpg` | 05 z konturem i cieniem, w kolorze bez konturu, w szarości bez konturu; pojazd 1:1 z konturem i bez |

Każdy obraz na planszach to render dostarczonych plików `.glb` (`scripts/s6render.py`, specyfikacje ujęć w `scripts/render_specs/`);
plansze tylko kadrują, skalują i podpisują (`scripts/boards.py`). Pojedyncze rendery: `renders/`.

## Od czego zacząłem (stan wyjściowy)

| źródło | gałąź | użyte jako |
|---|---|---|
| `R4StyleAtlas5/hero4` (Pass 4, PR #1294) | `art/1239-atlas5-hero4` | działa A/B/D zachowane; działo E + kontur; wirniki kół A/B/D/E; palety |
| `R4Weapons938/R4W_Basic` (PR #939) | `art/938-r4-weapon-models` | działo 06 zachowane; kontrakt działa |
| `R4Movement940/R4M_Wheel` (PR #941) | `art/940-r4-movement-models` | koło 06 zachowane; kontrakt koła |
| `R4ModulesSleek1x1` / atlas A-E wyrzutnika | `art/1239-atlas5-w4-modules-deployers` | kontrakt wyrzutnika; idea H-pada i przysłony (06) |
| `R4StyleAtlas5/hero1` v3 `R4B_Cube` | `art/1239-atlas5-hero1` | kostki pancerza pojazdu na planszy 2 (01-05) |
| `R4Blocks/R4B_Cube` (oryginalna kostka R4, kopia referencyjna na stacji) | - | kostki pancerza pojazdu 06 |
| `inventory/canonical_inventory.csv` | `art/1239-atlas5-hero4` | lista części i identyfikatorów |
| `Assets/R4_1/R41PartGeometry.cs`, render R41_Turret_v6.1 | `main` / renders 938 | trzy cechy stylu 06 |

**Brakujące źródła (konkretnie):**
- `FlyingDrone` - nie ma modelu w żadnej gałęzi ani klasy/prefabu w `Assets/`. Zbudowany od zera z kontraktem **PROPOSAL** (niżej).
- `RoboRougelike_atlas_odpowiednikow.html` - nie znalazłem w repozytorium ani w podłączonych folderach.
- Moodboard tldraw (`tldraw.com/f/WX7CwQhjEzvyq65kKtGEr`) - wymaga logowania; nie odczytany. Kierunki stylów wziąłem z opisu w zleceniu.
- Koła Pass 4 to inna część (`R4M_WheelM_Side_Offroad`, boczne mocowanie). Ich wirniki przeniosłem na oś `R4M_Wheel` z nowym mocowaniem.

## Kontrakty (sprawdzone na modelach z repo; osie Blendera: przód = −Y, góra = Z)

| element | węzły i puste obiekty | klipy | uwagi |
|---|---|---|---|
| `R4W_Basic` | ROOT, Base, GunPivot (0,0,0.135), Yaw, Pitch, Barrel, MuzzleFlash; Muzzle (0,−0.66,0.135), Socket_Mount_Bottom (0,0,−0.26) | Idle 1-25, AimYaw 27-75, AimPitch 77-113, Fire 115-124, Recoil 126-129, Return 131-139 | pitch +35/−10°, odrzut 45 mm, wylot bez zmian we wszystkich stylach |
| `R4M_Wheel` | Mount, Rotor (oś (0,0,−0.26)); Wheel_Axle, Socket_Mount (0,0,0.26), GroundProbe (0,0,−0.65) | Idle 1-25, Roll 27-51, Reverse 53-77, Brake 79-103 | port do góry z=0.26; Mount w obrysie ±0.2563 × z −0.312..0.26; L/P przez odbicie X |
| `R4X_FlyingDroneDeployer` | Body, Iris (obrót Z); Port_down_000 i Socket_Mount (0,0,−0.26), Deploy_Out (0,0,0.26) | Active 1-49 (pętla) | port w dół, styk ≥ 80 % |
| `FlyingDrone` **PROPOSAL** | ROOT, Body, Rotor_1..n (dzieci Body), Socket_Dock (0,0,0), Muzzle | Docked 1-25, Launch 27-51, Hover 53-77, Fly 79-103 | obrys ±0.23 × z 0..0.24; nazwy i klipy do zatwierdzenia po stronie gry |

Zachowane w każdym wariancie: ID, zajęte komórki (1×1), skala, pozycja w siatce, punkty montażu, osie obrotu, wymagane nazwy węzłów i klipy.
Wysokość dokowania drona (`dock_z`) jest zapisana w `meta` w QA każdego wyrzutnika.

## 24 pozycje

| | 01 Clean Futuristic | 02 Brutal Industrial | 03 Arcade Low-Poly | 04 Gritty Industrial | 05 Comic Outline | 06 R41 Modular Tech |
|---|---|---|---|---|---|---|
| działo | ZACHOWANY (Pass 4 A) | ZACHOWANY (Pass 4 B) | **NOWY** | ZACHOWANY (Pass 4 D) | PRZEROBIONY (Pass 4 E + kontur) | ZACHOWANY (oryginał #939) |
| koło | PRZEROBIONY | PRZEROBIONY | **NOWY** | PRZEROBIONY | PRZEROBIONY | ZACHOWANY (oryginał #941) |
| wyrzutnik | **NOWY** | **NOWY** | **NOWY** | **NOWY** | **NOWY** | PRZEROBIONY |
| dron | **NOWY** | **NOWY** | **NOWY** | **NOWY** | **NOWY** | **NOWY** |

Uzasadnienia (pełne w `status.json`):
- **Działo 03 i koło 03 od nowa** - poprzednie C_Arcade w szarości były bliźniacze z E_Toon (te same zaokrąglone skorupy). Nowe: heksagonalny cokół, klinowe policzki, trójkątna płetwa celownika; 12-kątna opona ze szprychami.
- **Koła 01/02/04/05 przerobione** - wirnik Pass 4 przeniesiony na oś `R4M_Wheel` (przesunięcie −23 mm w X), nowe mocowanie i półbłotnik.
  - 01: zamknięta pokrywa piasty.
  - 02: ciężka płyta, ramię skrzynkowe i amortyzator.
  - 04: kątowniki, resor, łata, a po przeglądzie w szarości także dokręcona tarcza boczna na wysięgniku, bo z boku 02 i 04 miały tę samą sylwetkę.
  - 05: rurowe ramię i gwiazda.
- **Wyrzutniki 01-05 od nowa** - wszystkie istniejące warianty A-E to ta sama rama 1×1 z czterema słupkami, różniąca się tylko kolorem.
  - 01: niski ceramiczny moduł.
  - 02: klatka startowa z siłownikami.
  - 03: sześciokątny pokład na nogach.
  - 04: pół-beczka na ramie z kątowników.
  - 05: „garnek” z bulajem.
  - 06 zachowuje ideę H-pada i przysłony z 1×1, przeniesioną na cokół R4.1.
- **Dron w każdym stylu dzieli cechę konstrukcyjną z wyrzutnikiem** (opisy na planszy `03`).
  - 01: wtopione wentylatory.
  - 02: rama z profili.
  - 03: sześciokąt z trójkątem.
  - 04: bęben.
  - 05: torus-bulaj i wielkie oko.
  - 06: ośmiokąt, czerwony pas i oko.

## 06 R41 Modular Tech - trzy powtarzane cechy z najwcześniejszych modeli R4.1

1. Gunmetalowe płyty fazowane/ośmiokątne ze szczelinami, żaluzjami i śrubami.
2. Poziomy czerwony pas drużyny i żółte znaczniki ostrzegawcze na czarnym kwadratowym cokole z narożnymi zaczepami.
3. Pomarańczowy toczony pierścień-„oko” na bocznej ścianie, piaście albo płycie mocowania.

Na planszy `04` oryginalne działo #939 i koło #941 stoją obok nowego wyrzutnika i drona.

## Co sprawdzono w Blenderze (5.3, w tle) - a czego nie

Sprawdzone dla każdego nowego i przerobionego modelu (szczegóły: `models/<STYL>/<CID>/<CID>_QA.json`):

**Kontrakt:**
- wymagane nazwy węzłów, rodzice i pivoty;
- obrys każdego węzła względem oryginału;
- styk portu - raycast BVH: wyrzutniki 99,3-100 % w dół, koła 99,3-100 % w górę, działo 03 99,3 %.

**Eksport i ponowny import:**
- GLB i FBX zaimportowane z powrotem;
- porównane nazwy, rodzice, obrysy (odchyłka ≤ 0,05 mm) i macierze świata w klatkach testowych (odchyłka animacji 0,0 mm);
- sprawdzona lista materiałów, skala 1,0 i zakresy klipów. Wynik: **BLENDER_PASS 19/19**.

**Modele zachowane (ZACHOWANY):**
- import GLB, wymagane węzły, obrysy węzłów, akcje - `contract_ok: true`;
- dodatkowe węzły mechanizmu w działach Pass 4 (Ring/Vent/Bolt/Gear/Crank...) pochodzą z zatwierdzonego Pass 4.

**Unity: `UNITY_NOT_RUN`.**
- Pliki nie były importowane do Unity.
- Nie zmieniono żadnego prefabu, materiału ani kodu gry.
- Ustawienia eksportu FBX: `axis_forward=-Z`, `axis_up=Y`, `FBX_SCALE_UNITS`. FBX ma osadzone tekstury, więc Unity może je wyciągnąć przy imporcie.

Trójkąty LOD0 / LOD1:

| | 01 | 02 | 03 | 04 | 05 | 06 |
|---|---|---|---|---|---|---|
| koło | 8552 / 5574 | 6112 / 4620 | 756 / 680 | 5700 / 3844 | 4304* / 4858 | oryginał |
| wyrzutnik | 2996 / 1740 | 1732 / 1648 | 260 / 260 | 1876 / 1646 | 2792* / 3644 | 1288 / 1268 |
| dron | 5024 / 3388 | 1684 / 1684 | 380 / 380 | 1804 / 1524 | 2448* / 3158 | 1544 / 1504 |
| działo | Pass 4 | Pass 4 | 280 / 268 | Pass 4 | Pass 4 + kontur | oryginał |

\* 05: liczba LOD0 bez powłoki konturu. Z konturem jest 2× tyle (np. koło 8608 w GLB). LOD1 05 zawiera kontur, dlatego ma więcej trójkątów niż LOD0 bez niego.
Działo 05: LOD1 to niezmieniony LOD1 E_Toon z Pass 4, bez konturu.

## Ocena po przeglądzie

- **Najbardziej rozpoznawalny: 05 Comic Outline.**
  - Rozpoznaje się go także w szarości bez konturu: okrągłe masy i przerośnięty element funkcji.
  - Konkretnie dron 05: kulisty bikopter z ogromnym okiem-bulajem. Ta sama gruba obręcz wraca jako pokrywa wyrzutnika-„garnka”.
  - W wycinku 1:1 z kamery gracza kształt drona 05 (kula z okiem) czyta się najlepiej ze wszystkich dronów, mimo że zajmuje tylko kilka pikseli.
- **Para wciąż najłatwiejsza do pomylenia: koło 01 vs koło 02.**
  - Oba mają wirnik z Pass 4 o tym samym profilu: duża opona z jodełką i tą samą średnicą.
  - Oba mają też ten sam układ: płyta + półbłotnik.
  - W kolorze różnią je biała zamknięta pokrywa piasty i czarno-żółta szprychowa obręcz. W szarości (`01g`, wiersz 2) zostaje tylko różnica piasty, która z kamery gracza ginie.
  - Proponowana poprawka w następnej serii: 01 dostaje gładką oponę niskoprofilową z osłoną boku, 02 - szerszą oponę i odsłonięty amortyzator po stronie zewnętrznej.
- **02 vs 04** (wskazane w zleceniu) rozdzielone bryłą:
  - wyrzutnik: kostka-klatka vs pół-beczka;
  - dron: rama H vs bęben;
  - działo: stożkowy cokół z żebrami vs rama z płyt;
  - koło: dodana tarcza boczna 04.
- **03 vs 05** rozdzielone bryłą:
  - 03: heksagony, kliny, szczeliny, niski poly (koło 756, wyrzutnik 260, dron 380 trójkątów);
  - 05: okrągłe, pękate formy.
  - Bez konturu 05 nadal nie wygląda jak 03 (plansza `05`).

## Struktura

```
boards/                    plansze (jpg)
renders/                   pojedyncze rendery użyte na planszach (el_*, pair_*, launch_*, veh_*, r41_*, before_*)
models/<STYL>/<CID>/       <CID>.glb, <CID>.fbx, <CID>_LOD1.glb, <CID>_LOD1.fbx, <CID>_QA.json, <CID>_manifest.json
                           (ZACHOWANE: kopie plików oryginału + <CID>_QA_source.json, tekstury w textures/)
source/<STYL>/<CID>_SOURCE.blend   edytowalne źródła: kolekcje SOURCE_highpoly / GAME_rig / LOD1 (działo 05: models/05_ComicOutline/R4W_Basic/R4W_Basic_SOURCE.blend)
textures/<CID>_<styl>_{base,orm,normal,emission}.png   2048 px, jeden zestaw na model
scripts/                   generator serii (s6*.py) + kopie modułów Pass 4 bez zmian (hf, hbake, hskin, hlod, h2defs, h2pal, h4defs, h4pal)
status.json                ZACHOWANY / PRZEROBIONY / NOWY + źródło i uzasadnienie dla 24 pozycji
```

## Jak odtworzyć

```
blender -b --factory-startup --python scripts/s6model.py -- OUT_DIR <CID> <STYL 1-6> 2048   # nowy / przerobiony model
blender -b --factory-startup --python scripts/s6outline.py -- SRC.glb OUT_DIR R4W_Basic 0.0075  # działo 05 (kontur)
blender -b --factory-startup --python scripts/s6keep.py -- SRC_DIR TEX_DIR OUT_DIR <CID> "notatka"  # kopia zachowanego modelu + QA
blender -b --factory-startup --python scripts/s6render.py -- scripts/render_specs/<spec>.json      # rendery
python3 scripts/boards.py renders boards                                                        # plansze
```

Ścieżki w `render_specs` wskazują na katalogi stacji roboczej (zastąpione `<WORKDIR>`); po przeniesieniu trzeba je podmienić.
Kopia robocza na stacji: `C01-Blender-Automation\R4_Six1\f1\` (modele, źródła, tekstury) i `R4_Six1\rf\` (rendery).

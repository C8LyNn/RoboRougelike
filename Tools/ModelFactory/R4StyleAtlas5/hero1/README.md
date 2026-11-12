# #1239 R4 Style Atlas 5 — HERO BENCHMARK PASS

Status: **propozycja do akceptacji właściciela**. Blender: 30/30 `BLENDER_PASS`. Testy GLB: 6/6 PASS. Unity: **UNITY_NOT_RUN**.
Zakres: tylko `Tools/ModelFactory/R4StyleAtlas5/` (`hero1/` + skrypty). Bez zmian w `Assets/`, kodzie gry, kolizjach, montażu, Save/Load.
Wcześniejsze dostawy (`atlas/`, `pilot2/`, `polish1/`, `polish2/`) są nietknięte.

Zmiana kierunku: pięć stylów ma się zbliżyć jakością i charakterem do nazwanych wzorców (żaden asset wzorca nie został skopiowany ani
pobrany — wzorce są celem jakości i charakteru). Zgodnie z poleceniem **nie** przerabiam biblioteki: na styl jest 1 hero robot, 1 broń
(Basic Gun), 1 napęd (Wheel) i 1 moduł energetyczny (Battery); do robota dochodzą Cube i ExplosiveTrapDeployer, bo jest z nich zbudowany.

## Co obejrzeć

| Plansza | Co pokazuje |
|---|---|
| `boards/01_hero_robot.jpg` | hero robot, BEFORE (Polish 2) / AFTER, ten sam blueprint, kamera i światło |
| `boards/02_hero_silhouette.jpg` | sylwetka z boku i z niskiej kamery, BEFORE / AFTER |
| `boards/03_weapon.jpg`, `04_drive.jpg`, `05_energy.jpg` | broń, napęd, moduł energetyczny, BEFORE / AFTER |
| `boards/06_materials.jpg` | zbliżenia materiałów: skos pancerza, dziób, mechanika broni |
| `boards/07_distance.jpg` | kamera rozgrywki 1920×1080 i wycinki 1:1 z 20 / 36 / 60 m, BEFORE / AFTER |

Pełne klatki: `renders/<Styl>/` (1080p; kadr dystansowy także 720p), parametry kadrów w `shots.json`, liczby w `renders/<Styl>/scene_stats.json`.
Modele: `variants/<Styl>/<Id>/` (FBX + GLB + LOD1 + .blend + QA), kit kadłuba: `hull_kit/<Styl>/R4H_HullKit.glb/.fbx`.

## Co konkretnie zmieniono

1. **Sylwetka robota — owiewki kadłuba (`scripts/hhull.py`).** Największa zmiana. Robot z klocków 0,52 m miał schodki i wcięcia, więc z
   dystansu czytał się jak zestaw kostek. Do warstwy kadłuba z Polish 2 doszły trzy rodzaje kosmetycznych fragmentów:
   `ramp` (skos 45° wypełniający schodek między dolnym pokładem a wyższym blokiem), `cheek` (ukośna ściana w narożniku wklęsłym w rzucie —
   dziób zamiast wcięcia) i `nose` (zderzak / taran na czole najniższego rzędu, wystaje 115 mm). Każdy styl ma własną wersję tych trzech
   brył. Robot czyta się teraz jako pojazd z glacisem i dziobem, a nie jako stos sześcianów.
2. **Prawdziwe fazowanie krawędzi.** Wszystkie 30 wariantów ma krawędzie sfazowane geometrią w Blenderze (szerokość per styl:
   A 4 mm ×2 segmenty, B 3,2 mm, C 14 mm ×3, D 2,2 mm, E 10 mm ×3). Krawędzie łapią światło — to główna różnica między „klockiem” a
   „odlewem / tłoczoną blachą”. 100 z 102 węzłów sfazowano; 2 zostały ostre, bo fazka psuła siatkę (Sleek: wirnik koła, Scrap: lufa).
3. **Palety pod wzorce** (`scripts/hstyle.py`) — opis niżej, per styl.
4. **Jedno światło sygnaturowe** na styl A (zimny błękit) i E (cyjan) na wszystkich częściach, zamiast kolorów per część.
5. **Look-dev Heroic Toon:** cieniowanie pasmowe (3 pasma), światło konturowe i czarny obrys (odwrócony kadłub). Istnieje tylko w scenie
   renderu — patrz „Uczciwie o dystansie do wzorców”.
6. **Scena:** światło kluczowe + kontrowe, głębia ostrości w zbliżeniach. Arena, kamera i światło identyczne dla BEFORE i AFTER.

Hero blueprint (`striker`, 32 części rozgrywki: 23 Cube, 4 Wheel, 2 Gun, 2 Battery, 1 Deployer) jest poprawny dla gry: unikalne komórki,
każda część ma klocek nośny, kadłub jest spójny. BEFORE to ten sam blueprint zbudowany z wariantów i płyt Polish 2, bez owiewek.

## Decyzje artystyczne per styl

**A — Sleek Futuristic → Destiny 2 / Armored Core VI.** Biały satynowy pancerz ceramiczny z lakierem bezbarwnym na ciemnej ramie
gunmetal, mechanika w tytanie, jedna cienka zimna linia światła na panel. Skosy mają wpuszczone ciemne „szkło sensorów”, dziób ma klin i
ostrze splittera. Zmiana względem Polish 2: szaroniebieski półmat → dwutonowy kontrast pancerz / rama, mniej świecących elementów, ale spójnych.

**B — Brutal Industrial → ciężka maszyna wojskowa / przemysłowa.** Malowana oliwkowoszara płyta, goła i oksydowana stal, żeliwo, żółć
ostrzegawcza tylko jako znak. Skosy to grube płyty w ramie ze śrubami i dwoma żebrami nośnymi; dziób to belka taranu na wspornikach.
Zmiana: jednolita czerń → czytelne klasy stali i masa (grubości 26 mm zamiast 18 mm, żebra, ramy).

**C — Arcade → klarowność Astroneera, ale pod czytelność walki.** Złamana biel, jeden nasycony akcent (pomarańcz), ciemny łupek na gumę i
ramę, turkus tylko na lufie / świetle. Mat, bez tekstur, duże promienie 14 mm, zero drobnego detalu. Zmiana: niebieski „plastik” →
jasna bryła z jednym akcentem; forma zostaje prosta, ale ma hierarchię (bryła → pas akcentu → ciemna rama).

**D — Scrap Wasteland → Crossout.** Wypłowiały khaki i tlenkowa czerwień, rdza, goła stal, sadza; szorstkie PBR, cienkie ostre krawędzie.
Skos to dwie różne, zachodzące na siebie blachy z przyspawaną taśmą, podparte widocznymi prętami (bez zamkniętej skrzynki); dziób to
taran z prętów i wyciętych zębów. Zmiana: zielonkawa blacha → przytłumiona, cieplejsza paleta i konstrukcja „z odzysku” widoczna w bryle.

**E — Heroic Toon → Overwatch + Roboquest.** Nasycony pomarańcz, złamana biel na oznaczeniach, ciemny granat jako „kombinezon” pod
pancerzem, cyjanowe światło; promienie 10 mm; pasmowe światło + kontur. Zmiana: koralowo‑granatowa „zabawka” → dwuwarstwowy pancerz
(jasna obwódka + wypukła płyta), jedno wyraźne oznaczenie na panel, reflektor w zderzaku jako „twarz” pojazdu.

## Owiewki a rozgrywka — do decyzji właściciela

Owiewki są **wyłącznie wizualne**, tak jak płyty z Polish 2: bez kolidera, bez HP, nic nie trafia do zapisu. Reguły (pełny opis w
nagłówku `scripts/hhull.py`, manifest w `hull_layer/hero_striker_<Styl>.json`):

- powstają tylko w **pustych** komórkach wewnątrz obrysu kadłuba (`ramp`, `cheek`) albo do 115 mm przed czołem (`nose`); nigdy w komórce koła ani nad nim;
- każda ma właściciela i listę `requires`; znika, gdy zniknie którakolwiek z tych części; zniszczenie nie tworzy nowych owiewek przed naprawą;
- zamontowanie części w komórce usuwa owiewkę (warstwa jest funkcją zajętości komórek);
- konsekwencja, którą trzeba zaakceptować albo odrzucić: pocisk, który „trafia” w skos, w logice gry trafia w klocek za nim (do pół komórki głębiej).

Test po stronie generatora (`hull_layer/fairing_destruction_test.json`, 7 scenariuszy × 5 stylów): PASS — żadna owiewka nie zostaje po
usuniętej części. To test numpy, **nie** test Unity. Runtime w grze pozostaje po stronie Codexa; tu jest tylko propozycja i dane.

## Liczby

| | A Sleek | B Brutal | C Arcade | D Scrap | E Toon |
|---|---|---|---|---|---|
| Hero robot, trójkąty BEFORE → AFTER | 53 426 → 110 730 | 42 816 → 79 784 | 12 080 → 38 946 | 24 658 → 54 172 | 41 890 → 82 060 |
| w tym warstwa kadłuba | 7 504 → 9 024 | 13 288 → 16 100 | 5 128 → 5 712 | 3 448 → 5 484 | 5 052 → 6 094 |
| Basic Gun (LOD0) | 17 364 | 9 570 | 3 094 | 4 134 | 13 182 |
| Wheel (LOD0) | 8 864 | 7 148 | 3 684 | 6 836 | 7 522 |
| Battery (LOD0) | 11 224 | 3 480 | 2 652 | 3 532 | 3 188 |
| ramp / cheek / nose | 272 / 280 / 144 | 504 / 512 / 276 | 96 / 104 / 88 | 424 / 248 / 268 | 140 / 148 / 326 |

Fazowanie mniej więcej **podwaja** liczbę trójkątów robota (C: ×3, bo fazki są 3‑segmentowe). To koszt do oceny przed przeniesieniem na
całą bibliotekę: tańsza droga to fazka 1‑segmentowa + normalne ważone albo fazka w normal mapie.

Testy: `tests/HERO1_TEST_SUMMARY.md`, `tests/hero1_glb_tests.json` (kontrakt węzłów, porty, obwiednia, klipy, COLOR_0 — na wyeksportowanych
GLB), logi Blendera w `tests/build_logs/`. Interfejsy, węzły kontraktowe, obwiednie i klipy są te same co w Polish 2.

## Uczciwie o dystansie do wzorców

To jest krok w stronę wzorców, nie ich poziom. Co nadal dzieli wynik od Destiny 2 / AC VI / Crossout / Overwatch:

- **Modele to nadal prymitywy z generatora** (pudła, bryły obrotowe, wytłoczenia) z fazkami — nie rzeźbione high‑poly z wypalonymi
  normalnymi. Brakuje złożonych, ciągłych powierzchni i detalu trzeciego rzędu, który niosą wzorce A, B i E.
- **Tekstury są generowane i kafelkowane** (A, B, D); nie ma unikalnego rozwinięcia UV z zabrudzeniem dopasowanym do bryły. W zbliżeniu
  Scrap rdza jest wzorem, nie historią konkretnej blachy — do Crossout brakuje tu najwięcej.
- **Toon (E):** pasma, światło konturowe i obrys są w skrypcie sceny (`scripts/hscene.py`), nie w assetach. Eksport FBX/GLB niesie zwykłe
  PBR. Bez odtworzenia shadera w URP styl E wróci do wyglądu „plastiku”. Na skosach widać też ząbkowanie pasma cienia — artefakt do poprawy w shaderze.
- **Brak bloomu** w renderach: światła A i E są jaśniejsze, ale nie „świecą”. Odbiór w grze zależy od post‑processu URP.
- **Kit kadłuba** (płyty i owiewki) nie ma fazek ani COLOR_0 — tylko części mają.
- **Tył i boki robota** są nadal płaską ścianą klocków (owiewki działają na schodki, narożniki i czoło). Widać to w wycinku 20 m.
- **Animacje, FX, dźwięk** — bez zmian względem Polish 2; „feeling” wzorców w ruchu nie był przedmiotem tego etapu.
- Sylwetka zależy od blueprintu: robot gracza bez schodków i narożników dostanie mniej owiewek niż hero.

## Odtworzenie

```
cd Tools/ModelFactory/R4StyleAtlas5/scripts
python3 hgen.py                      # mesh_source, kit_source, scene_source, shots.json, hull_layer (katalogi źródłowe są w .gitignore)
blender -b --factory-startup --python pbuild.py -- OUT_DIR MESH_SOURCE_DIR R4_REF_DIR [Id,Id..|*] [ABCDE]        # warianty (jak w Pilot 2 / Polish 2)
blender -b --factory-startup --python hscene.py -- OUT SCENE_SRC KIT_SRC SHOTS_JSON POLISH2_VARIANTS POLISH2_VARIANTS HERO_VARIANTS TEXTURES KIT_OUT [ABCDE]
python3 ptests.py hero1 && python3 hboards.py
```

## Pytania do właściciela

1. Czy kierunek pięciu stylów (palety, fazki, owiewki) jest akceptowany jako baza dla całej biblioteki?
2. Owiewki kadłuba: zostają jako część kosmetycznej warstwy (z konsekwencją dla czytelności trafień), czy tylko `nose`, czy wcale?
3. Budżet trójkątów: pełne fazki geometrią, czy tańszy wariant?
4. Heroic Toon: czy Codex odtwarza shader pasmowy + obrys w URP — bez tego styl E nie osiągnie pokazanego wyglądu.

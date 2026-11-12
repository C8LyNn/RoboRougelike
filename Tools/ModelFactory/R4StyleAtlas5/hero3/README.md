# #1239 R4 Style Atlas 5 — HERO BENCHMARK PASS 3 (wybór artystyczny właściciela)

Status: **bramka artystyczna — czeka na decyzję właściciela**. Testy wyboru na GLB: **PASS 5/5**. Unity: **UNITY_NOT_RUN**. Windows player: **WINDOWS_PLAYER_NOT_RUN**.
Zakres: tylko `Tools/ModelFactory/R4StyleAtlas5/` (`hero3/` + pięć skryptów `h3*.py` + wpis w README pakietu). Bez zmian w `Assets/`, kodzie gry,
gameplayu, kolizjach, montażu i Save/Load. `hero1/` (PR #1270) i `hero2/` (PR #1274) są **nietknięte** — sprawdza to test X5.

W tej iteracji **nie powstał żaden nowy model**. Robot HYBRID to ten sam blueprint co w obu poprzednich iteracjach (32 części gry) z wyborem
historycznego modelu dla każdego typu części. Rendery ładują niezmienione pliki `.glb` z `hero1/variants/` i `hero2/variants/`
(SHA-256 każdego użytego pliku jest w `renders/<Styl>/scene_stats.json` i w `report/selection.json`).

## Najpierw obejrzyj

**[`boards/01_hybrid_robots.jpg`](boards/01_hybrid_robots.jpg)** — pięć robotów HYBRID. To jest przedmiot decyzji.

| Plansza | Wymóg z zadania |
|---|---|
| `01_hybrid_robots.jpg` | pięć kompletnych robotów HYBRID |
| `02_old_new_hybrid.jpg` | porównanie OLD / NEW / HYBRID |
| `03a_close_top.jpg`, `03b_close_fgun.jpg`, `03c_close_wheel.jpg` | widoki z bliska: dach, działko, koło |
| `04_gameplay_distance.jpg` | kamera rozgrywki 1080p i wycinki 1:1 z 20 / 36 / 60 m |
| `05_geometry_clay.jpg` | spójność geometrii (sama siatka, jedna glina) |
| `06_materials_coherence.jpg` | spójność materiałów (kolory zmierzone z modeli) |
| `07a_zero_brutal_robot_wheel.jpg`, `07b_zero_arcade_battery.jpg` | osobna ocena trzech elementów z oceną 0 |
| `08_sleek_gun_analysis.jpg` | Sleek: dlaczego nowe działko wypadło gorzej |
| `09_sleek_light_proposal.jpg` | jedyna propozycja korekty (HYBRID+), tylko w scenie |
| `10_A…E_reference_hybrid.jpg` | referencja High Detail obok HYBRID |

## Jak czytam „robot”

Klocek (Cube), deployer i warstwa osłon kadłuba mają **tę samą geometrię** w obu iteracjach — Hero Pass 2 tylko je przemalował na swoją paletę.
Dlatego „stary / nowy robot” oznacza tu **paletę i materiał kadłuba** (klocki, deployer, osłony), a nie inną bryłę.
Jeśli oceniałeś w kolumnie „Robot” coś innego (np. całość z częściami), powiedz — wybór dla A, B i D zmieni się wtedy tylko w tej pozycji.

## Pięć robotów HYBRID

| Styl | Robot (kadłub) | Działko | Koło | Bateria | Trójkąty OLD / NEW / HYBRID |
|---|---|---|---|---|---|
| A Sleek | NOWY (+1) | STARE (−1) | NOWE (+2) | STARA (−2) | 110 730 / 63 242 / **107 954** |
| B Brutal | **STARY** (0 → decyzja) | NOWE (+1) | **NOWE** (0 → decyzja) | STARA (−1) | 79 784 / 58 364 / **61 556** |
| C Arcade | NOWY (+1) | NOWE (+1) | NOWE (+2) | **NOWA** (0 → decyzja) | 38 946 / 37 182 / **37 182** |
| D Scrap | STARY (−1) | STARE (−1) | NOWE (+1) | STARA (−1) | 54 172 / 37 720 / **46 668** |
| E Heroic Toon | NOWY (+2) | NOWE (+2) | NOWE (+2) | NOWA (+2) | 82 060 / 49 592 / **49 592** |

- **E** jest identyczny z NEW — nic nie zostało cofnięte ani przeprojektowane. Render HYBRID różni się od zatwierdzonego renderu Hero Pass 2
  średnio o 0,001 poziomu na 255, w pojedynczych pikselach o najwyżej 4 (szum próbkowania).
- **C** po rozstrzygnięciu baterii też jest identyczny z NEW.
- **A** wraca prawie do wagi Hero Pass 1: stare działko (17 364 trójkątów) i stara bateria (11 224) mają fazy z prawdziwej geometrii.
  To koszt wyboru, nie błąd.

## Trzy elementy z oceną 0

**Brutal — robot: STARY.** Plansza `07a`. Działko (nowe) i bateria (stara) są ustalone. Stara bateria jest pomalowana paletą starego kadłuba
(ta sama oliwkowoszara płyta i ta sama tekstura rys), więc na starym kadłubie dwa z trzech elementów dachu i kadłub są jedną rodziną materiałów,
a nowe działko jest ciemniejszą, obrobioną bryłą na jaśniejszym tle — czytelna hierarchia. Na nowym, prawie czarnym kadłubie robot zlewa się z
działkiem i kołami; z 36 m zostaje ciemna plama z żółtymi punktami. Pomiar (rozrzut jasności wewnątrz sylwetki z 36 m): stary kadłub 0,146–0,159,
nowy 0,133–0,141. Różnica jest niewielka — pomiar potwierdza kierunek, nie rozstrzyga sam.

**Brutal — koło: NOWE.** Kryterium było: spójność z nowym działkiem. Nowe koło powtarza jego motywy (kołnierz ze śrubami, bladożółta opaska,
to samo zużycie krawędzi liczone z geometrii), więc działko nie jest jedynym elementem z nowego potoku na robocie. Uczciwie: stare koło jest
bardziej kanciaste i w samym pomiarze czytelności wypada minimalnie lepiej (0,159 wobec 0,146); nowa opona jest miększa niż reszta stylu —
to jest na liście zmian. Wszystkie cztery kombinacje są na planszy, zmiana wyboru to jedna linia w `scripts/h3gen.py` (`ZERO`).

**Arcade — bateria: NOWA.** Plansza `07b`. Stara bateria wnosi na nowego robota trzy kolory spoza jego palety (łupkowa podstawa obok granatu,
blada morela obok nasyconego pomarańczu, morski kołpak obok cyjanu). Nowa ma paletę robota i formę z referencji. Jej niespójności wobec stylu
— do usunięcia w geometrii po bramce, bez dokładania detalu:
1. osiem małych kostek narożnych → cztery duże, sfazowane klamry (jak w referencji); styl ma „mało części, każda duża”;
2. ostre naroża sześcianu → faza tej wielkości co na działku i piaście koła;
3. pełny sześcian 0,49 × 0,51 × 0,50 m do wysokości działka — dwie baterie mają na dachu więcej masy niż broń; obniżyć korpus o ok. 15 %;
4. każda bateria ma tyle pomarańczu co działko (13–14 % powierzchni), a są dwie — po zmianie 1 akcent wraca do broni.

## Sleek: dlaczego nowe działko wypadło gorzej

Plansza `08`, liczby w `report/sleek_gun.json`. Oba modele mają ten sam obrys kontraktu, więc porównałem sylwetki siatek gry.

| Pomiar | Stare | Nowe | |
|---|---|---|---|
| pole sylwetki z boku | 2 322 cm² | 2 315 cm² | to samo — nowe działko **nie** jest mniejsze |
| udział podstawy w sylwetce | 48 % | 49 % | to samo |
| najwęższe miejsce podstawy | 239 mm | 301 mm | znikła „szyja”: ścięty stożek → dzwon, wieża zlewa się z cokołem |
| lufa widoczna przed osłoną | 285 mm | 217 mm | −24 %, do tego przewężenie i gruby wylot zamiast równej linii |
| powierzchnia świecąca | 8,7 % | 2,5 % | dwa ciągłe pierścienie i linia → drobne szewrony i kreski |
| trójkąty LOD0 | 17 364 | 4 064 | faza 2-segmentowa → 1-segmentowa + normal mapa |

Wniosek: nowe działko nie straciło wielkości, tylko **czytelność**: (1) kierunek — długą, ciągłą linię lufy z ciemnym stożkiem i tuleją w jednej
osi; (2) rozdział cokół / wieża; (3) ciągłe światło widoczne z dystansu; (4) zgodność języka form z kadłubem — stare jest z płaszczyzn i skosów
jak płyty robota, nowe jest obłe (kopuła, hełm) na kanciastym pojeździe; (5) dyscyplinę kolorów — pomarańczowy kołnierz przecina linię lufy.
Zyskało dwie rzeczy, które **są** w referencji High Detail i warto je przenieść na starą formę: szewrony światła na osłonie i mały pomarańczowy akcent.

## Spójność materiałów i geometrii

Plansze `05` i `06`, liczby w `report/palette.json` i `report/selection.json`.

- **Dwa potoki w jednym robocie.** Części Hero Pass 1 to farba stref × tekstura kafelkowa i fazy z geometrii; części Hero Pass 2 mają unikalne UV
  i wypalone mapy. A, B i D mieszają oba. W glinie (`05`) widać to jako różnicę gęstości krawędzi, w kolorze jako różnicę jakości zużycia.
- **A:** stare działko i bateria świecą lodową bielą, nowy kadłub i koła cyjanem (odległość koloru 0,55); stare części nie mają pomarańczu
  kadłuba. Biel starych części jest wyraźnie szarsza niż biel kadłuba (0,22).
- **B:** dwie żółcie — bursztynowa (stary kadłub, bateria) i blada, obita (nowe działko, koło), odległość 0,26–0,28; stal nowych części jest
  ciemniejsza niż oliwkowa płyta kadłuba (0,35–0,37), co tu działa na korzyść hierarchii.
- **D:** nowe koło jest blisko palety starego kadłuba (0,07–0,16) — najmniej problemów ze wszystkich mieszanych.
- **C, E:** jedna iteracja, odległości małe.

**HYBRID+ (tylko Sleek, plansza `09`).** Jedyna korekta, którą pokazuję poza samym wyborem: stare działko i bateria biorą kolor światła kadłuba.
To nadpisanie koloru emisji w scenie renderu, **nie nowy asset**; HYBRID na pozostałych planszach jest bez niej. Żółci Brutal i jakości zużycia
nie da się uczciwie poprawić samą paletą — wymagają przejścia starych modeli przez potok tekstur (niżej).

## Lista zmian w stronę referencji High Detail (po bramce)

Kolejność według priorytetów zadania: całość → język form → hierarchia → materiały → detal drugoplanowy. Żadna pozycja nie jest „więcej trójkątów”.

**Wspólne dla A, B, D** — stare modele wybrane do HYBRID (działko i bateria A, bateria B, działko i bateria D) przeprowadzić przez potok tekstur
Hero Pass 2 (unikalne UV, wypalenie, zużycie liczone z geometrii) **bez zmiany ich geometrii**. To usuwa mieszanie dwóch potoków i jest
warunkiem pozostałych poprawek materiałowych.

**A — Sleek**
1. Działko: zostaje stara forma; przenieść na nią szewrony światła na osłonie i mały pomarańczowy akcent na kołysce (nie na linii lufy).
2. Działko: druga warstwa skorupy nad ciemną ramą na osłonie i policzkach — referencja ma pancerz z warstw, stare działko z jednej.
3. Bateria: zostaje stara pozioma kapsuła; dodać dwa zaciski na końcach (w referencji pomarańczowe klamry trzymają ogniwo), rdzeń w kolorze światła robota.
4. Jedno światło na całym robocie (HYBRID+), łuki światła koła pogrubić tak, by były widoczne z 36 m.
5. Kadłub: pomarańczowy szew jest na każdej krawędzi klocka, w referencji akcent to kilka małych pól — zostawić go na krawędziach sylwetki.
6. Deployer: po przemalowaniu jest największą pomarańczową plamą na dachu; korpus biały / grafitowy, pomarańcz tylko jako znak.

**B — Brutal**
1. Jedna żółć: przepis obitej farby nowego działka na baterii i znakach kadłuba.
2. Koło: bieżnik o kwadratowym barku (kształt klocków ze starego koła), zachować nowy kołnierz ze śrubami i opaskę.
3. Kadłub: tekstura rys powtarza się na każdej płycie — wypalenie per płyta albo przesunięcie per obiekt w shaderze.
4. Bateria: zostaje stara bryła z izolatorami; brakuje jej świecących bursztynowych ogniw z referencji — dodać je bez zmiany sylwetki.
5. Działko: faktura odlewu w normal mapie; skala lufy zostaje decyzją o kontrakcie (Ø 80 mm).

**C — Arcade**
1. Bateria: cztery punkty z sekcji „Arcade — bateria”.
2. Większa faza na największych bryłach kadłuba (referencja jest „soczysta”, klocki mają 14 mm).
3. Koło: łuki światła grubsze i dłuższe — w referencji to główny znak koła.

**D — Scrap**
1. Kadłub, działko, bateria (stare): rdza jest wzorem kafelka, nie historią blachy — wypalenie per część; farba różna per płyta (referencja ma
   zieleń, żółć, gołą stal i pasy obok czerwieni tlenkowej), bez zmiany brył.
2. Spawy jako geometria LOD0 tam, gdzie stykają się blachy osłon; podpory i taśmy tylko w miejscach, które coś trzymają.
3. Nowe koło: maska błota i tlenkowy nalot w barwie kadłuba, żeby nie było czystsze niż reszta.
4. Bateria: lampy ogniw jaśniejsze (w referencji to jedyne światło pojazdu).

**E — Heroic Toon** — bez zmian w modelach. Do referencji brakuje shadera toon i szkła w URP oraz emblematu; to praca po stronie gry.

Największa różnica wobec ilustracji pozostaje poza grafiką: obrys lufy Ø 80 mm i kadłub z klocków 0,52 m (pytania 2 i 3 z Hero Pass 2).

## Testy i zgodność z kontraktem

`tests/HERO3_TEST_SUMMARY.md`, `tests/hero3_selection_tests.json` (numpy, na wyeksportowanych GLB, bez Blendera). Dla każdego stylu i typu
porównuję model stary z nowym i sprawdzam wybrany:

| | Wynik |
|---|---|
| X1 węzły kontraktu i hierarchia identyczne | 15/15 |
| X2 pivoty, gniazda, wylot lufy, porty i obwiednia identyczne (odchyłka 0,0000 mm) | 15/15 |
| X3 wybrany model w obrysie (poza obrysem 0 mm) | 15/15 |
| X4 klipy, zdarzenia, fps i długość identyczne; klucze węzłów kontraktu identyczne (odchyłka 0) | 15/15 |
| X5 wybrany plik jest bajt w bajt blobem gałęzi swojego PR; `hero1/` i `hero2/` bez zmian | 25/25 |

Stary i nowy model każdego typu są dla gry tą samą częścią, więc każda kombinacja jest dopuszczalna. To testy techniczne po stronie generatora —
**nie** test Unity.

## Uczciwie

- HYBRID to render z historycznych plików, nie nowe assety. Roboty są zdefiniowane danymi (`report/selection.json`), nie osobnymi plikami GLB.
- „Stary / nowy robot” to paleta kadłuba (patrz wyżej). Paleta jest nakładana w scenie renderu tak samo, jak robił to Hero Pass 2.
- OLD i NEW na planszach to te same prezentacje co kolumny BEFORE i AFTER w PR #1274 (średnia różnica renderów poniżej 0,01 poziomu na 255),
  więc OLD w stylu E jest bez shadera toon, a w stylu D bez przesunięcia rdzy per obiekt — tak jak na planszach, które oceniałeś.
- Pomiar czytelności z dystansu jest prosty (rozrzut jasności w wycinku) i rozróżnia warianty słabo; traktuj go jako pomoc, nie dowód.
- Kolory części Hero Pass 2 to średnie tekseli stref — dobre do porównań, nie do kopiowania jako wartości palety.
- Shader toon, kontur, światło kontrowe i głębia ostrości nadal są w skrypcie sceny, nie w assetach.

## Odtworzenie

```
cd Tools/ModelFactory/R4StyleAtlas5/scripts
python3 h3gen.py                                   # hero3/shots.json: wybór (SCORES, ZERO, PLUS) i kadry; kamery, arena i światło z hero2/shots.json
blender -b --factory-startup --python h3scene.py -- OUT hero2/scene_source hero1/kit_source hero3/shots.json hero1/variants hero2/variants hero1/textures [ABCDE]
python3 h3tests.py && python3 h3report.py && python3 h3boards.py
```
`hero2/scene_source` i `hero1/kit_source` są w `.gitignore` (powstają z `hgen.py` / `h2gen.py`); użyłem tych samych plików co rendery PR #1274.
Logi Blendera: `tests/build_logs/`.

## Pytania do właściciela (bramka)

1. Czy pięć HYBRID to właściwa baza? W szczególności: Brutal na starym kadłubie z nowym kołem i Arcade z nową baterią.
2. Czy „robot” w Twojej tabeli to paleta kadłuba, tak jak przyjąłem?
3. Sleek: czy przyjąć HYBRID+ (jedno światło)?
4. Czy następnym krokiem ma być przejście wybranych starych modeli przez potok tekstur Hero Pass 2 bez zmiany geometrii, a dopiero potem
   poprawki form z listy?

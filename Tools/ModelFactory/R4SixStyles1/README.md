# R4SixStyles1 - seria 1: cztery elementy × sześć stylów (przegląd artystyczny)

**Status:** propozycja do przeglądu artystycznego. Nic tu nie jest podpięte pod grę. Domyślne prefaby, mechanika i publikacja
pozostają do osobnej decyzji. Wszystko leży w tym jednym katalogu, na osobnej gałęzi `art/r4-six-styles-s1` (od `main`).
Oryginały na innych gałęziach są nietknięte. Modele zachowane bez zmian są tu kopiami bajt w bajt.

Elementy: działo `R4W_Basic`, koło `R4M_Wheel`, wyrzutnik `R4X_FlyingDroneDeployer`, dron `FlyingDrone`.
Style w stałej kolejności: 01 Clean Futuristic, 02 Brutal Industrial, 03 Arcade Low-Poly, 04 Gritty Industrial,
05 Comic Outline, 06 R41 Modular Tech.

Ta wersja to trzecia dostawa serii:
1. Poprawki po odczytaniu moodboardu `rr-style-directions` i atlasu odpowiedników.
2. Kontrola sensu każdego modelu (`scripts/s6sense.py`): czy każda część się czegoś trzyma i czy ruchome części mijają resztę.
3. Druga kontrola, funkcji: czy koło stoi na ziemi, czy strzał wychodzi z lufy, czy dron stoi na swoim gnieździe i może wystartować, czy LOD1 trzyma się w całości. Do tego naprawione dwa przypadki zostawione w drugiej dostawie (działa 05 i 06), oliwka w 04 i oznaczenia kierunku w 06.

Wynik końcowy: wszystkie 53 raporty kontroli (24 modele, 23 pliki LOD1, 6 par) są OK - `qa_sense/`.

## Plansze (`boards/`)

| plik | co pokazuje |
|---|---|
| `01_porownanie_6_stylow.jpg` | **plansza 1**: 6 stylów × 4 elementy. Wspólna kamera (2,3 m, 50 mm), skala, światło i tło. Ostatni wiersz to wycinek 2× drona z tego samego renderu. Znaczniki ZACHOWANY / PRZEROBIONY / NOWY |
| `01g_szarosc_bez_podpisow.jpg` | ta sama siatka w glinie, bez podpisów i bez koloru; 05 bez konturu (test samej bryły). To jest kontrola szarości dla par 02/04 i 03/05 |
| `02_perspektywa_gracza.jpg` | **plansza 2**: ten sam prosty pojazd w 6 stylach (koła, działo, wyrzutnik na kadłubie, dron po starcie). Klatka gracza 1920×1080, pojazd ~20 m od kamery, wycinek 1:1 pikseli i bliski widok 3/4 |
| `06_test_128px.jpg` | test z moodboardu: każdy element i pojazd przeskalowany do 128 px, w kolorze i w szarości |
| `07a…07d_inspekcja_*.jpg` | kontrola sensu w obrazach: każdy model z przodu, z boku, z tyłu i z drugiego boku |
| `00_przed_i_po.jpg` | stan wyjściowy (najlepsze istniejące modele) nad dostarczonym modelem w każdej pozycji |
| `03_wyrzutnik_i_dron.jpg` | para wyrzutnik + dron: wspólna cecha konstrukcyjna, gniazdo z góry, start (Docked / Launch / Fly), szarość |
| `04_rodzina_R41.jpg` | stare modele R4 (działo #939, koło #941) obok nowego wyrzutnika i drona 06 |
| `05_kontur_z_i_bez.jpg` | 05 z konturem i cieniem, w kolorze bez konturu, w szarości bez konturu; pojazd 1:1 z konturem i bez |

Każdy obraz na planszach to render dostarczonych plików `.glb` (`scripts/s6render.py`, specyfikacje ujęć w `scripts/render_specs/`).
Plansze tylko kadrują, skalują i podpisują (`scripts/boards.py`). Pojedyncze rendery są w `renders/`, a widoki kontroli sensu w `renders/inspekcja/`.

## Zgodność z moodboardem (`rr-style-directions`, tldraw)

Moodboard odczytałem po udostępnieniu. Użyłem strony „MOODBOARD — 6 stylów, R41 + 5 koncepcji” i strony „Części gry · odpowiedniki”, która jest atlasem odpowiedników.
Reguły wziąłem z kart DNA SYLWETKI, PALETA i GRANICA każdego stylu.

| styl | reguły z moodboardu | co zmieniłem |
|---|---|---|
| 01 | wydłużona, gładka sylwetka; panele ukrywają mechanikę; paleta biel / stal / cyjan / granat; bez rdzy, odsłoniętych tłoków i konturu; test 128 px: smukły kadłub i światło | koło 01 zbudowane od nowa: slick, zamknięta tarcza z cyjanowym pierścieniem, owiewka z linią światła (wg „świetlne obręcze kół przechodzą w cienkie linie owiewek”); kolor dodatkowy pomarańcz → granat |
| 02 | symetria, ogromny pancerz, widoczne siłowniki, duże nity; grafit + stal + BHP; bez łat, rdzy i brudu | zgodne - bez zmian stylu |
| 03 | kostki i kliny; matowe płaskie pola; błękit / turkus / limonka / żółty; bez cienkich śrub, rdzy i konturu | paleta przestawiona: kremowa baza i pomarańcz → turkus, błękit, limonka, żółty (pomarańcz należy do 05); dron 03 zbudowany od nowa jako klockowy śmigłowiec (ref. Trailmakers), bo stary trikopter z pierścieniami był w szarości jak dron 06 (granica 03 ↔ 06) |
| 04 | nieregularna, asymetryczna, łatana; rdza, wypłowiały metal; węgiel / metal / rdza / ziemia + oliwka, brudny pomarańcz; bez nowego gładkiego pancerza i fabrycznej symetrii | geometria zgodna; czerwono-tlenkowa baza z Pass 4 zostawiona (pasuje do działa 04); oliwka dodana jako kolor łat zastępczych: płyta i taśmy wyrzutnika, łata i taśma koła, bateria drona (wcześniej szary podkład) |
| 05 | mocne czarne obrysy, przerysowane bronie; czarny / pomarańcz / błękit / fiolet; test 128 px: kreska widoczna bez koloru | kontur 7,5 → 12 mm, bo 7,5 mm znikał w teście 128 px; kremowy kolor dodatkowy → fiolet (koło, wyrzutnik, dron). Cel shading i malowane plamy cienia to shader w Unity - nie robione |
| 06 | widoczne pojedyncze moduły; grafit + stal, czerwone paski, żółte BHP; pierścienie, porty, śruby; oznaczenia kierunku; bez owiewek 01, pancernych brył 02, rdzy 04 | zgodne; oznaczenia kierunku: żółta strzałka do przodu (−Y) na lądowisku wyrzutnika i na włazie drona (działo i koło 06 to oryginały R4) |

**Atlas odpowiedników - referencje elementów tej serii:**
- działo `R4W_Basic` ↔ Crossout Avenger 57mm / TerraTech GSO 3-Pound Cannon;
- koło `R4M_Wheel` ↔ TerraTech GSO Little Trekker Wheel;
- wyrzutnik ↔ Crossout AD-12 Falcon (alternatywa: Beyond All Reason Mantis);
- dron ↔ Beyond All Reason Legion Drone.

W atlasie `FlyingDrone` ma etykietę „Moduł pojazdu” i jest sparowany z wyrzutnikiem, czyli identyfikator pochodzi z kodu gry. Modelu 3D nie ma jednak nigdzie. W sprawdzonych gałęziach (`main`, `feature/catalog-cell-port-mapping-20261109`) nie znalazłem też kodu drona. Kontrakt drona zostaje PROPOSAL.

## Kontrola sensu (`scripts/s6sense.py`, wyniki w `qa_sense/`)

### Pierwsza kontrola: czy to się trzyma i czy się nie zderza

Każdy z 24 modeli i każda z 6 par wyrzutnik + dron przechodzi automatyczną kontrolę „czy to ma sens mechanicznie”:

1. **Nic nie wisi w powietrzu.**
   - W pozie spoczynkowej siatka jest dzielona na osobne części.
   - Części, które się stykają, przenikają albo siedzą jedna w drugiej, są łączone w grupy.
   - Każda grupa, która nie dochodzi do głównej konstrukcji, to błąd (z podaną szczeliną).
   - Tolerancja: szew do 2,5 mm w obrębie węzła, 4,5 mm luzu między ruchomymi węzłami. Sam kontrakt R4 ma 2 mm szczeliny pod obrotnicą.
2. **Ruchome części nie przechodzą przez resztę.**
   - Wszystkie klipy są przewijane klatka po klatce: koło Roll; działo AimYaw, AimPitch i Recoil; dron Hover; wyrzutnik Active.
   - Każde przecięcie trójkątów poza strefą przegubu (oś, czop, silnik) jest błędem.
   - Punkt przecięcia jest liczony dokładnie, nie ze środków trójkątów.
3. **Dron naprawdę leży na wyrzutniku.**
   - Dron w klatce Docked stoi na wysokości `dock_z`. Musi dotykać gniazda i nie może w nie wchodzić.
   - Skrypt liczy też, o ile dron może opaść w dół (`free_drop_mm`). Z tego ustawiłem `dock_z`.

Pierwsze przejście znalazło błędy w 19 z 24 modeli i we wszystkich 6 parach. Raporty z pierwszego przejścia nie zostały zachowane, bo każde przejście nadpisywało poprzednie. Poniżej jest opis znalezisk z przebiegu pracy i końcowy wynik z `qa_sense/`.

| model | co znalazła kontrola | poprawka | wynik |
|---|---|---|---|
| działo 01 (Pass 4 A) | odbiornik wisiał 18 mm od obu ramion jarzma na niewidocznej osi; boczne żaluzje przy Recoil odchylały się o 20° i wchodziły 2 cm w ramię jarzma; przy +35° tył odbiornika wchodził w przeciwwagę | łożyska czopów z jarzma do boku odbiornika (3 mm luzu); żaluzje nieruchome; blok przeciwwagi krótszy o 18 mm z przodu | OK |
| działo 02 (Pass 4 B) | - | - | OK |
| działo 03 | odbiornik wisiał między policzkami bez czopów; bieżnia obrotnicy 4 mm pod pierścieniem | heksagonalne czopy z policzków do odbiornika z limonkowymi kapslami; bieżnia podniesiona do 2 mm szczeliny z kontraktu | OK |
| działo 04 (Pass 4 D) | cała kołyska wisiała 40-70 mm między wspornikami bez niczego na osi pochylenia; kosz wylotu 5 mm od lufy; osłona termiczna kończyła się 20 mm przed odbiornikiem; paski nie dochodziły do osłony; osłona boczna 18 mm od szyny; lewy wspornik poza obrotnicą; luźna pokrywa 4 mm nad szynami | osie ze wsporników do czopa zamka; obejma kosza; opaska osłony; łączniki pasków; 4 przekładki; stopa kątowa; pokrywa położona na szynach (nadal grzechocze przy Fire) | OK |
| działo 05 (Pass 4 E + kontur) | przy pełnym podniesieniu (+35°, klatka 83) dolny przedni róg bocznej płyty odbiornika wchodził w policzek jarzma - odziedziczone z Pass 4 | w trzeciej dostawie: części płyt i listew wystające poza 129,5 mm od osi cofnięte do 132 mm (6 mm luzu), po obu stronach; strefa czopów nietknięta | OK |
| działo 06 (oryginał #939) | pasiasta płyta ostrzegawcza z tyłu głowicy wisiała 16 mm za tylną płytą i 10 mm od policzków | w trzeciej dostawie: dwie listwy dystansowe od tylnej płyty do płyty ostrzegawczej (kopia - oryginał na `art/938` nietknięty) | OK |
| koło 01 | silnik i oś piasty nie dochodziły do wirnika | piasta i oś dociągnięte do tarczy | OK |
| koło 02 | tarcza hamulcowa wisiała obok wirnika | tuleja piasty w wirniku + czop osi w mocowaniu | OK |
| koło 03 | oś i kołki obręczy wisiały | kołki wpuszczone w oponę; heksagonalna oś w mocowaniu | OK |
| koło 04 | resor wchodził w oponę; tarcza boczna wychodziła poza obrys `Mount` | resor przesunięty na zewnątrz ze strzemionami; tarcza przycięta do y −0,245 z obejmą, zaczepem i zastrzałem; błotnik i łata nad oponą | OK |
| koło 05 | tarcza hamulcowa i koniec ramienia wisiały | tuleja piasty; koniec ramienia na osi | OK |
| koło 06 (oryginał #941) | - | - | OK |
| wyrzutnik 01 | segmenty przysłony przechodziły przez korpus przy obrocie; światła i czujniki wisiały | przysłona nad korpusem, pierścień światła na niej; czujniki dosunięte | OK |
| wyrzutnik 02 | przysłona przechodziła przez klatkę | obrotnica ze szczękami i podkładkami nad klatką | OK |
| wyrzutnik 03 | płatki przysłony przechodziły przez pokład; trójkąt wisiał | płatki nad pokładem; trójkąt zlicowany z pokładem | OK |
| wyrzutnik 04 | - (tylko wysokość dokowania) | - | OK |
| wyrzutnik 05 | przechylona przysłona przechodziła przez obręcz „garnka” | przysłona bez przechyłu, płatki niżej | OK |
| wyrzutnik 06 | przysłona przechodziła przez prowadnice | pierścień prowadzący z prowadnicami nad przysłoną | OK |
| dron 01 | płozy wisiały pod kadłubem | zastrzały płóz | OK |
| dron 02 | - | - | OK |
| dron 03 | (nowy po moodboardzie) | - | OK |
| dron 04 | łopaty przechodziły przez bęben i pałąk | wirniki wyżej (co drugi +8 mm), niższy pałąk, mniejsze łopaty | OK |
| dron 05 | wspornik oka wisiał | wspornik przesunięty do kadłuba | OK |
| dron 06 | osłony wirników oderwane od ramion; łopata uderzała w osłonę | ramiona od kadłuba, silnik jako część korpusu, osłony na silnikach, łopata r 49 mm | OK |
| pary 1-6 | dron wisiał nad gniazdem albo w nie wchodził | `dock_z` ustawione z `free_drop_mm` (tabela niżej) | OK |

Poprawki dział 01, 04, 05 i 06 są zrobione na istniejących plikach (`scripts/s6fix.py`, operacje w `scripts/fix_gun1c.json`, `fix_gun4b.json`, `fix_gun5.json`, `fix_gun6.json`). Każda nowa część bierze UV z najbliższej ściany węzła, do którego jest dołączona, więc ma ten sam materiał.

### Druga kontrola: czy część robi to, do czego służy (`s6sense.py` v2) i LOD1

4. **Koło stoi na ziemi i toczy się równo.** Opona w pozie spoczynkowej dotyka ziemi w `GroundProbe` (z −0,65; ±3 mm), wirnik jest wyśrodkowany na osi (±2 mm), a przy toczeniu (klip Roll) opona nie skacze więcej niż 5 mm. Tyle daje opona z klockami, która przetacza się po klockach.
5. **Strzał wychodzi z lufy.**
   - `Muzzle` działa leży na osi lufy, 0-50 mm przed jej końcem (oryginał #939 ma 40 mm), a linia strzału jest wolna w spoczynku i w klipach celowania.
   - `Muzzle` drona leży 0-45 mm przed jego bronią, z wolną linią strzału, która nie przecina tarczy wirnika.
6. **Dron stoi na swoim gnieździe** (`Socket_Dock`, ±2 mm), a jego wirniki obracają się wokół własnych piast (±3 mm).
7. **Przysłona wyrzutnika obraca się wokół osi startu** (±3 mm).
8. **Dron naprawdę może wystartować.** Dron jest odtwarzany przez klip Launch i podnoszony pionowo 0-300 mm nad obracającą się przysłoną. Każde przecięcie z wyrzutnikiem to błąd.
9. **LOD1 też trzyma się w całości.** Plik LOD1 to jedna statyczna siatka bez rigu. Przechodzi test „nic nie wisi” z tolerancją ruchomych złączy (4,5 mm), a koło LOD1 dotyka ziemi.

| model | co znalazła druga kontrola | poprawka | wynik |
|---|---|---|---|
| koło 02 | bieżnik Pass 4 sięgał r 0,386, ziemia kontraktu jest na r 0,390 - opona wisiała 4,5 mm nad ziemią | wirnik przeskalowany promieniowo ×1,0097 (szerokość bez zmian) i obrócony tak, że w spoczynku na ziemi stoi klocek bieżnika | ziemia 0,0 mm, toczenie 2,4 mm |
| koło 04 | bieżnik kończył się na r 0,380 - koło stało tylko na trzech stalowych listwach: 10 mm nad ziemią w spoczynku, skok 10 mm przy toczeniu | bieżnik ×1,0171 do r 0,389, listwy cofnięte tak, że wystają 1 mm (r 0,390), klocek w dół | 1,0 mm, 1,8 mm |
| koło 05 | balon z siedmioma klockami stał 10 mm nad ziemią | wirnik ×1,0138, klocek w dół | 0,0 mm, 2,0 mm |
| koło 01 | w LOD1 pierścień światła, szew i szczeliny tarczy odstawały 6-18 mm (uproszczenie spłaszcza kopułę tarczy) | wpuszczone 10-15 mm w tarczę | OK |
| dron 01 | `Muzzle` leżał w środku płyty kadłuba | `Muzzle` 10 mm przed okiem w nosie, oko jest emiterem | OK |
| dron 02 | `Muzzle` był w oku czujnika - dron nie miał broni | ciężka lufa z kołnierzem i hamulcem wylotowym pod okiem. Pierwsza wersja (zasobnik pod kadłubem) była cięta przy starcie przez obracające się szczęki wyrzutnika, więc lufa jest 3 mm nad nimi | OK, start bez kolizji |
| dron 04 | `Muzzle` był na reflektorze; obejma bębna schodziła 2-3 mm pod `Socket_Dock` | rurowe działko przypięte dwiema obejmami z prawej strony bębna (asymetria 04); bęben 10 mm wyżej, dron stoi na płozach | OK |
| dron 05 | dron stał 4 mm nad `Socket_Dock` | cały dron 4 mm niżej; `Muzzle` na osi oka (oko jest bronią - promień) | OK |
| dron 06 | `Muzzle` przed okiem bez lufy | krótka lufa w pomarańczowym pierścieniu oka | OK |
| działa 01-06 | `Muzzle` 40-42 mm przed lufą we wszystkich (tak jak oryginał #939) | - (to punkt pojawienia się pocisku) | OK |
| LOD1 dział 01 i 04 | stare LOD1 z Pass 4 nie miały napraw sensu (3 i 4 części w powietrzu) | LOD1 odbudowane z naprawionych LOD0 (`scripts/s6lod.py`): 04 regułą generatora; 01 jako siatka gry bez uproszczenia, bo każde rozpuszczenie 14/9/5° odrywało cokół od stopy | OK |
| LOD1 dział 05 i 06 | - | odbudowane z naprawionych modeli: 05 regułą generatora bez konturu, 06 regułą oryginału (DECIMATE collapse 0,5) | OK |
| pary 4 i 5 | po zmianie dronów 04 i 05 | nowe `dock_z` | OK |

**Dokowanie (`dock_z` w `meta` QA każdego wyrzutnika):**

| para | 01 | 02 | 03 | 04 | 05 | 06 |
|---|---|---|---|---|---|---|
| `dock_z` [m] | 0,0230 | 0,0740 | 0,0245 | −0,0060 | 0,0345 | 0,0660 |
| swobodny opad drona [mm] | 0,2 | 0,0 | 0,5 | 0,1 | 0,5 | 0,0 |
| start (klip Launch + pionowo 0-300 mm) | bez kolizji | bez kolizji | bez kolizji | bez kolizji | bez kolizji | bez kolizji |

Geometria wyrzutników 04 i 05 się nie zmieniła; w QA tych wyrzutników zmieniło się tylko `meta.dock_z` (z notatką).

## Od czego zacząłem (stan wyjściowy)

| źródło | gałąź | użyte jako |
|---|---|---|
| `R4StyleAtlas5/hero4` (Pass 4, PR #1294) | `art/1239-atlas5-hero4` | działo B zachowane; działa A/D/E naprawione (E + kontur); wirniki kół B/D/E; palety |
| `R4Weapons938/R4W_Basic` (PR #939) | `art/938-r4-weapon-models` | działo 06 (kopia naprawiona, oryginał nietknięty); kontrakt działa |
| `R4Movement940/R4M_Wheel` (PR #941) | `art/940-r4-movement-models` | koło 06 zachowane; kontrakt koła |
| `R4ModulesSleek1x1` / atlas A-E wyrzutnika | `art/1239-atlas5-w4-modules-deployers` | kontrakt wyrzutnika; idea H-pada i przysłony (06) |
| `R4StyleAtlas5/hero1` v3 `R4B_Cube` | `art/1239-atlas5-hero1` | kostki pancerza pojazdu na planszy 2 (01-05) |
| `R4Blocks/R4B_Cube` (oryginalna kostka R4, kopia referencyjna na stacji) | - | kostki pancerza pojazdu 06 |
| `inventory/canonical_inventory.csv` | `art/1239-atlas5-hero4` | lista części i identyfikatorów |
| `Assets/R4_1/R41PartGeometry.cs`, render R41_Turret_v6.1 | `main` / renders 938 | trzy cechy stylu 06 |
| moodboard i atlas odpowiedników (tldraw `rr-style-directions`) | - | reguły stylów i referencje (wyżej) |

**Brakujące źródła (konkretnie):**
- `FlyingDrone` - nie ma modelu w żadnej gałęzi ani klasy/prefabu w `Assets/`. Zbudowany od zera z kontraktem **PROPOSAL** (niżej).
- Koła Pass 4 to inna część (`R4M_WheelM_Side_Offroad`, boczne mocowanie). Ich wirniki przeniosłem na oś `R4M_Wheel` z nowym mocowaniem.

## Kontrakty (sprawdzone na modelach z repo; osie Blendera: przód = −Y, góra = Z)

| element | węzły i puste obiekty | klipy | uwagi |
|---|---|---|---|
| `R4W_Basic` | ROOT, Base, GunPivot (0,0,0.135), Yaw, Pitch, Barrel, MuzzleFlash; Muzzle (0,−0.66,0.135), Socket_Mount_Bottom (0,0,−0.26) | Idle 1-25, AimYaw 27-75, AimPitch 77-113, Fire 115-124, Recoil 126-129, Return 131-139 | pitch +35/−10°, odrzut 45 mm, wylot bez zmian we wszystkich stylach |
| `R4M_Wheel` | Mount, Rotor (oś (0,0,−0.26)); Wheel_Axle, Socket_Mount (0,0,0.26), GroundProbe (0,0,−0.65) | Idle 1-25, Roll 27-51, Reverse 53-77, Brake 79-103 | port do góry z=0.26; Mount w obrysie ±0.2563 × z −0.312..0.26; Rotor w obrysie ±0.184 × ±0.39; L/P przez odbicie X |
| `R4X_FlyingDroneDeployer` | Body, Iris (obrót Z); Port_down_000 i Socket_Mount (0,0,−0.26), Deploy_Out (0,0,0.26) | Active 1-49 (pętla) | port w dół, styk ≥ 80 % |
| `FlyingDrone` **PROPOSAL** | ROOT, Body, Rotor_1..n (dzieci Body, obrót wokół lokalnego Z), Socket_Dock (0,0,0), Muzzle | Docked 1-25, Launch 27-51, Hover 53-77, Fly 79-103 | obrys ±0.23 × z 0..0.24; nazwy i klipy do zatwierdzenia po stronie gry |

Zachowane w każdym wariancie:
- ID i zajęte komórki (1×1);
- skala i pozycja w siatce;
- punkty montażu i osie obrotu;
- wymagane nazwy węzłów i klipy.

Naprawy sensu nie przesunęły żadnego pivota, osi ani punktu montażu. Wirniki kół 02/04/05 są większe o 1-1,7 % w promieniu (dalej w obrysie Rotor ±0,39) i obrócone wokół własnej osi. `Muzzle` dronów został przesunięty na ich broń (kontrakt drona to PROPOSAL).

## 24 pozycje

| | 01 Clean Futuristic | 02 Brutal Industrial | 03 Arcade Low-Poly | 04 Gritty Industrial | 05 Comic Outline | 06 R41 Modular Tech |
|---|---|---|---|---|---|---|
| działo | PRZEROBIONY (Pass 4 A + naprawy sensu) | ZACHOWANY (Pass 4 B) | **NOWY** | PRZEROBIONY (Pass 4 D + naprawy sensu) | PRZEROBIONY (Pass 4 E + naprawa sensu + kontur 12 mm) | PRZEROBIONY (kopia oryginału #939 + listwy płyty) |
| koło | **NOWY** | PRZEROBIONY | **NOWY** | PRZEROBIONY | PRZEROBIONY | ZACHOWANY (oryginał #941) |
| wyrzutnik | **NOWY** | **NOWY** | **NOWY** | **NOWY** | **NOWY** | PRZEROBIONY |
| dron | **NOWY** | **NOWY** | **NOWY** | **NOWY** | **NOWY** | **NOWY** |

Razem: **2 ZACHOWANE, 8 PRZEROBIONYCH, 14 NOWYCH.** Uzasadnienia są pełne w `status.json`.

Zmiany względem pierwszej dostawy:
- Działo 01 i 04: ZACHOWANY → PRZEROBIONY (naprawy sensu).
- Koło 01: PRZEROBIONY → NOWY (moodboard 01 i granica 01 ↔ 02).
- Dron 03: zbudowany od nowa jako śmigłowiec.

Zmiany w trzeciej dostawie:
- Działo 06: ZACHOWANY → PRZEROBIONY (listwy dystansowe płyty ostrzegawczej).
- Działo 05: naprawa policzków.
- Koła 02/04/05 stoją na ziemi.
- Drony 01/02/04/05/06 mają broń, z której wychodzi `Muzzle`.
- Oliwka w 04, strzałki kierunku w 06.

Pozostałe uzasadnienia:
- **Działo 03 i koło 03 od nowa.** Poprzednie C_Arcade w szarości były bliźniacze z E_Toon (te same zaokrąglone skorupy). Nowe działo ma heksagonalny cokół, klinowe policzki i trójkątną płetwę celownika. Nowe koło ma 12-kątną oponę ze szprychami.
- **Koła 02/04/05 przerobione.** Wirnik Pass 4 przeniosłem na oś `R4M_Wheel` (przesunięcie −23 mm w X), z nowym mocowaniem i półbłotnikiem:
  - 02: ciężka płyta, ramię skrzynkowe i amortyzator.
  - 04: kątowniki, resor, łata i dokręcona tarcza boczna na wysięgniku.
  - 05: rurowe ramię i gwiazda.
- **Wyrzutniki 01-05 od nowa.** Wszystkie istniejące warianty A-E to ta sama rama 1×1 z czterema słupkami, różniąca się tylko kolorem.
  - 01: niski ceramiczny moduł.
  - 02: klatka startowa z siłownikami.
  - 03: sześciokątny pokład na nogach.
  - 04: pół-beczka na ramie z kątowników.
  - 05: „garnek” z bulajem.
  - 06 zachowuje ideę H-pada i przysłony z 1×1, przeniesioną na cokół R4.1.
- **Dron w każdym stylu dzieli cechę konstrukcyjną z wyrzutnikiem** (opisy na planszy `03`).
  - 01: wtopione wentylatory.
  - 02: rama z profili.
  - 03: heksagon i duży limonkowy trójkąt.
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
- sprawdzona lista materiałów, skala 1,0 i zakresy klipów.
- Wynik: **BLENDER_PASS 22/22** (wszystkie nowe i przerobione).
- Odbudowane LOD1 dział: ponowny import GLB i FBX, trójkąty, materiały, obrys (`<CID>_LOD1_info.json`).

**Kontrola sensu i funkcji:**
- **53 z 53 raportów OK**: 24 modele, 23 pliki LOD1 (koło 06 ma LOD1 tylko w FBX), 6 par z dokowaniem i startem.

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
| działo | 3244 / 3212† | 2976 / 2912 (Pass 4) | 380 / 348 | 1834 / 1784 | 2576* / 1840 | 12776 / 6298 |
| koło | 3576 / 1720 | 6176 / 4658 | 776 / 700 | 5700 / 3846 | 4340* / 2594* | oryginał |
| wyrzutnik | 2996 / 1748 | 1732 / 1660 | 260 / 260 | 1876 / 1646 | 2792* / 1774* | 1308 / 1288 |
| dron | 5072 / 3438 | 1784 / 1784 | 288 / 288 | 1868 / 1564 | 2448* / 1524* | 1704 / 1672 |

Liczby z plików GLB, LOD0 działa razem z płaszczyzną błysku wylotu.

\* 05: bez powłoki konturu. Z konturem jest 2× tyle, np. koło 8680 / 5200. LOD1 działa 05 jest bez konturu.

† Działo 01: LOD1 = siatka gry bez uproszczenia. Rozpuszczenie płaszczyzn dawało tylko 2-4 % mniej trójkątów i odrywało cokół od stopy w kontroli LOD1.

## Pozostało do decyzji

1. **LOD1 to statyczna siatka bez rigu** (tak jak w Pass 4 i w oryginale #939). W Unity działo na odległości LOD1 nie będzie celować, a koło nie będzie się obracać. Trzeba zdecydować, czy LOD1 ma być podpięty pod węzły rigu, czy zostaje tak.
2. **Działo 06 to teraz kopia oryginału z dwiema listwami.** Jeśli oryginał #939 ma zostać wzorcem bez zmian, wystarczy wrócić do pliku z `art/938`; poprawka to tylko te dwie listwy.
3. **Koło 05 przy toczeniu podskakuje najmocniej w serii.** Ma siedem dużych klocków. Z rozkładu podpór wychodzi do 4,8 mm, a w klatkach klipu Roll zmierzone jest 2,0 mm. Mieści się w przyjętym progu 5 mm.
4. **Cel shading 05** - to shader w Unity, nie geometria.
5. **Kontrakt `FlyingDrone`** - PROPOSAL do zatwierdzenia po stronie gry, razem z nowymi położeniami `Muzzle` i bronią dronów.

## Ocena po przeglądzie

- **Najbardziej rozpoznawalny: 03 Arcade Low-Poly.**
  - Wszystkie cztery sylwetki są jednoznaczne w szarości, bez koloru i bez konturu (`01g`, `06_test_128px`): 12-kątna opona, sześciokątny pokład na nogach, klockowy śmigłowiec, działo z płetwą.
  - Trzyma się nawet w 128 px.
  - 05 jest rozpoznawalny głównie przez kreskę. Bez konturu jego okrągłe formy zostają, ale tracą charakter.
- **Para najłatwiejsza do pomylenia: koło 02 vs koło 05 bez konturu.**
  - Oba mają dużą oponę z bieżnikiem, płytę i półbłotnik.
  - Z konturem rozdziela je kreska, a bez konturu tylko ramię (skrzynkowe vs rurowe).
  - Poprzednio najłatwiejsza para, koło 01 vs 02, jest rozwiązana nowym kołem 01: slick z zamkniętą tarczą i owiewką.
- **02 vs 04** rozdzielone bryłą:
  - wyrzutnik: kostka-klatka vs pół-beczka;
  - dron: rama H vs bęben;
  - działo: stożkowy cokół z żebrami vs rama z płyt;
  - koło: tarcza boczna 04.
- **03 vs 05** rozdzielone bryłą:
  - 03: heksagony, kliny, szczeliny, niski poly (koło 776, wyrzutnik 260, dron 288 trójkątów);
  - 05: okrągłe, pękate formy.
  - Bez konturu 05 nadal nie wygląda jak 03 (plansza `05`).

## Struktura

```
boards/                    plansze (jpg)
renders/                   pojedyncze rendery użyte na planszach (el_*, pair_*, launch_*, veh_*, r41_*, before_*)
renders/inspekcja/         widoki kontroli sensu (przód, bok, tył, drugi bok) - plansze 07a-d
qa_sense/                  raporty kontroli sensu i funkcji: 24 modele (<el>_<styl>.json), 23 LOD1 (<el>_lod1_<styl>.json), 6 par
models/<STYL>/<CID>/       <CID>.glb, <CID>.fbx, <CID>_LOD1.glb, <CID>_LOD1.fbx, <CID>_QA.json, <CID>_manifest.json
                           ZACHOWANE: kopie plików oryginału + <CID>_QA_source.json, tekstury w textures/
                           działa 01/04/05/06: <CID>_SOURCE.blend obok (edytowalny plik naprawionego modelu), <CID>_LOD1_info.json (odbudowany LOD1);
                           textures/ i <CID>_QA_source.json = pliki źródła (Pass 4 / #939)
source/<STYL>/<CID>_SOURCE.blend   edytowalne źródła nowych modeli: kolekcje SOURCE_highpoly / GAME_rig / LOD1
textures/<CID>_<styl>_{base,orm,normal,emission}.png   2048 px, jeden zestaw na model
scripts/                   generator serii (s6*.py), naprawy (s6fix.py + fix_gun*.json), LOD1 (s6lod.py), kontrola sensu i funkcji (s6sense.py),
                           sonda części (s6probe2.py), plansze (boards.py)
                           + kopie modułów Pass 4 bez zmian (hf, hbake, hskin, hlod, h2defs, h2pal, h4defs, h4pal)
status.json                ZACHOWANY / PRZEROBIONY / NOWY + źródło i uzasadnienie dla 24 pozycji
```

## Jak odtworzyć

```
blender -b --factory-startup --python scripts/s6model.py -- OUT_DIR <CID> <STYL 1-6> 2048           # nowy / przerobiony model
blender -b --factory-startup --python scripts/s6fix.py -- SRC.glb OUT_DIR R4W_Basic scripts/fix_gun1c.json   # naprawa działa 01 (04: fix_gun4b.json, 06: fix_gun6.json)
blender -b --factory-startup --python scripts/s6fix.py -- E_Toon.glb OUT_DIR R4W_Basic scripts/fix_gun5.json 0.012   # działo 05: naprawa + kontur 12 mm
blender -b --factory-startup --python scripts/s6lod.py -- MODEL.glb OUT_DIR R4W_Basic [keep|dissolve:A|collapse:0.5]  # LOD1 z naprawionego modelu
blender -b --factory-startup --python scripts/s6keep.py -- SRC_DIR TEX_DIR OUT_DIR <CID> "notatka"   # kopia zachowanego modelu + QA
blender -b --factory-startup --python scripts/s6sense.py -- MODEL <CID>.glb <CID> OUT.json           # kontrola sensu modelu
blender -b --factory-startup --python scripts/s6sense.py -- PAIR DEP.glb DRONE.glb DOCK_Z OUT.json   # kontrola pary wyrzutnik + dron
blender -b --factory-startup --python scripts/s6render.py -- scripts/render_specs/<spec>.json       # rendery (final2_spec, insp2_spec)
python3 scripts/boards.py renders boards                                                         # plansze
```

Ścieżki w `render_specs` wskazują na katalogi stacji roboczej (zastąpione `<WORKDIR>`); po przeniesieniu trzeba je podmienić.
Kopia robocza na stacji: `C01-Blender-Automation\R4_Six1\f1\` (modele, źródła, tekstury), `R4_Six1\rf\` (rendery),
`R4_Six1\insp\` (widoki kontroli) i `R4_Six1\sense_final\` (raporty); działa 05/06 i LOD1 dział także w `R4_Six1\f2\`.

# #1239 R4 Style Atlas 5 — HERO BENCHMARK PASS 4 (nowa iteracja modelowania)

> **W TOKU (commit roboczy):** w repozytorium jest komplet 17 modeli części (17/17 `BLENDER_PASS`, testy GLB 17/17). Zestawy pancerza kadłuba, rendery Brutal / Scrap, plansze i `MODELS.md` zostaną dodane w następnym commicie — ich wypalanie trwa na stacji roboczej.

Status: **propozycja do odbioru właściciela**. Blender: @@PASS@@ `BLENDER_PASS`. Testy GLB (numpy): @@TESTS@@. Unity: **UNITY_NOT_RUN**. Windows player: **WINDOWS_PLAYER_NOT_RUN**.
Zakres: tylko `Tools/ModelFactory/R4StyleAtlas5/` (`hero4/` + skrypty `scripts/h4*.py`). Bez zmian w `Assets/`, kodzie gry, fizyce, montażu, Save/Load,
identyfikatorach i parametrach broni. Wcześniejsze dostawy (`hero1/` = PR #1270, `hero2/` = PR #1274 i starsze) są nietknięte — sprawdza to test.
Gałąź `art/1239-atlas5-hero4` wychodzi z `art/1239-atlas5-hero2`; nie zawiera PR #1278.

## Co to jest, a co nie

PR #1278 (HYBRID) składał roboty z gotowych modeli Pass 1 i Pass 2. **Tutaj nie ma ani jednego modelu skopiowanego z poprzednich etapów.**
Oceny z ankiety 1 są potraktowane jako *kierunek*: wskazana wersja jest wzorcem (lewa kolumna), a prawa kolumna to model zbudowany od nowa
na jej podstawie — nowy przepis geometrii, nowy model źródłowy high-poly, nowa siatka gry, nowe wypalenie i tekstury.

**23 propozycje: 17 modeli części + 6 zestawów pancerza kadłuba.** Remis w ankiecie = dwie odrębne propozycje (P z wersji starej, Q z nowej);
zwycięzca **nie** jest wybrany.

| Styl | Kadłub | Działko | Koło | Bateria |
|---|---|---|---|---|
| A Sleek | NEW → 1 | OLD → 1 | NEW → 1 | OLD → 1 |
| B Brutal | remis → **P i Q** | NEW → 1 | remis → **P i Q** | OLD → 1 |
| C Arcade | NEW → 1 | NEW → 1 | NEW → 1 | remis → **P i Q** |
| D Scrap | OLD → 1 | OLD → 1 | NEW → 1 | OLD → 1 |
| E Heroic Toon | NEW → 1 | NEW → 1 | NEW → 1 | NEW → 1 |

Co dokładnie zostało ze wzorca, a co jest nowe w każdej z 23 propozycji: **[`MODELS.md`](MODELS.md)** (to samo jako dane: `report/models.json`).
Test P5 potwierdza liczbowo, że geometria każdego węzła różni się od wzorca, a żaden plik GLB nie jest kopią pliku z Pass 1 / Pass 2.

## Co obejrzeć

Wszędzie: **LEWA = kierunek preferowany w ankiecie 1, PRAWA = nowy model Pass 4**; ta sama kamera, skala, światło i ekspozycja
(arena i ujęcia wzięte bez zmian z `hero2/shots.json`).

| Plansza (`boards/`) | Zawartość |
|---|---|
| `01_robots_left_right.jpg` | cały robot, 5 stylów + drugie pary dla remisów |
| `02_gun.jpg`, `03_wheel.jpg`, `04_battery.jpg` | każda kategoria z bliska, część sama na gołym rdzeniu kostki; pod spodem: co zachowane, co nowe |
| `05_hull_armour.jpg` | pancerz kadłuba: panele, dziób, pojedyncza kostka z własnymi płytami |
| `06_gameplay_distance.jpg` | kamera rozgrywki 1080p, 20 / 36 / 60 m, wycinki 1:1 |
| `07_geometry_clay.jpg` | sama geometria bez tekstur (glina): robot i dziób |
| `08a/b/c_source_*.jpg` | model źródłowy high-poly, siatka gry, wireframe, LOD1 — każda propozycja |
| `08d_hull_kit_sheets.jpg` | arkusze sześciu zestawów pancerza |
| `09_animation_frames.jpg` | klatki klipów (mechanizmy w ruchu) |

Pojedyncze rendery 1080p: `renders/<Styl>/<ujęcie>_<L|R|Lq|Rq>_1080.jpg`.

## Kadłub: nowy modułowy pancerz, te same reguły

Stary i nowy „kadłub” z ankiety mają **tę samą geometrię warstwy pancerza** (płaskie płyty z `hero1/hull_kit`), różni je tylko paleta. Dlatego
jedyna realna poprawa to wymodelowanie pancerza od nowa — i to zostało zrobione:

- `hull_kit/<styl>/R4H_HullKit.glb` — komplet siatek o **tych samych nazwach i układach odniesienia** co dotychczasowa warstwa
  (`plate_00…15` wg 4-bitowej maski sąsiadów, `gasket`, `lip`, `ramp`, `cheek`, `nose_0…3`). Reguła rozmieszczania (`scene_source`) jest
  użyta bez zmian: każda płyta należy do jednej ściany jednej kostki, więc kostkę można wyjąć lub zniszczyć razem z jej płytami.
- Kostka `R4B_Cube` pozostaje **zatwierdzonym rdzeniem konstrukcyjnym 44 trójkąty** (port, kolizja, siatka) — nie została dotknięta.
  Pancerz jest odwracalną warstwą wizualną: bez kolizji, bez wpływu na montaż; po jego wyłączeniu zostaje dotychczasowy wygląd.
- Każdy zestaw ma model źródłowy high-poly, siatkę gry, wypalone mapy (normal, AO, krawędzie) i tekstury base / ORM / normal / emisja
  (jeden atlas 2048² na zestaw), plik `.blend`, FBX i GLB.
- Brutal P/Q i Scrap mają po 2–3 **warianty zużycia** każdej płyty (`plate_MM_V`), żeby sąsiednie płyty się nie powtarzały. Wybór wariantu
  z pozycji kostki to jedyny dodatek do reguły — czysto kosmetyczny; wymaga akceptacji przed ewentualnym wdrożeniem w Unity.

## Metoda (ten sam potok co Pass 2, nowe przepisy)

`h4defs.py` (17 przepisów części) i `h4kit.py` (pancerz) → model źródłowy high-poly i siatka gry (`hf.py`) → wypalanie w Cycles (`hbake.py`)
→ skóra modelu z wypalonych map (`hskin.py`, palety `h4pal.py`) → eksport, przeniesienie zatwierdzonej animacji i QA (`pbuild.py`, bez zmian)
→ sceny porównawcze (`h4scene.py`) → testy (`h4tests.py`) i plansze (`h4boards.py`).

Na każdą propozycję części: `source[_q]/<Styl>/<ID>_SOURCE.blend` (high-poly + siatka gry + LOD1), `variants[_q]/<Styl>/<ID>/` (FBX, GLB,
LOD1, `.blend` z rigiem i klipami, `_QA.json`, rendery kontrolne), `textures[_q]/<Styl>/` (base, ORM, normal, emisja, maska malowania 2048²).
Sufiks `_q` = druga propozycja remisu.

## Kontrakt — zachowany

Identyfikatory (`R4W_Basic`, `R4M_WheelM_Side_Offroad`, `R4X_Battery`), nazwy i hierarchia węzłów, pivoty, porty i ich powierzchnie styku,
obwiednia komórki, obrysy węzłów, klipy, zdarzenia i krzywe animacji są wzięte z zatwierdzonych assetów i sprawdzane dwa razy: przez
`pbuild.py` w Blenderze oraz niezależnie na wyeksportowanych GLB (`tests/`). Animacje: klucze są **identyczne** ze wzorcem — przebudowana
jest tylko geometria mechanizmów, które te klucze poruszają (np. pierścień fazowy Sleek jedzie teraz po płaszczu lufy, dzwon garłacza Toon
puchnie przy strzale, zacisk hamulca Brutal się zaciska). Nowych klipów nie ma, bo wymagałyby zmiany kontraktu animacji.

## Gdzie kontrakt wymiarów ogranicza poprawę (konkretnie)

To nie są wady modeli, tylko granice, których nie przekroczyłem bez zgody. Każda z nich blokuje jedną konkretną poprawę:

1. **Lufa działka: węzeł `Barrel` ma obrys 80 × 74 mm** (x ±40 mm, z 98–172 mm). Lufa nie może być grubsza niż Ø74 mm, a dzwon, hamulec
   wylotowy czy lufa wielolufowa muszą się w tym zmieścić. Czytelność uzbrojenia zrobiłem więc płaszczem na węźle `Pitch` (ten może mieć
   do Ø100 mm), który niesie linię lufy; sama lufa wystaje z niego tylko końcówką. Gdyby `Barrel` mógł mieć np. 120 × 120 mm, lufa mogłaby
   być główną bryłą broni we wszystkich stylach.
2. **Węzeł `Pitch`: dół na z ≥ 85 mm przy osi lufy na 135 mm** — współosiowy płaszcz ma najwyżej Ø100 mm. To ogranicza ciężkie lufy Brutal
   i garłacz Toon (dlatego płaszcze są przesunięte o 15 mm w górę względem osi).
3. **Koło: wirnik r ≤ 390 mm przy oponie 350 mm** — na bieżnik zostaje 40 mm, a płaski klocek „rośnie” na narożnikach (klocek
   o długości ok. 100 mm po obwodzie i wysokości 38 mm sięga już 391 mm). Bardzo głębokie łapy / kolce wymagałyby większego promienia.
4. **Bateria: port tylny** — prostokąt 130 × 130 mm na tylnej ścianie musi być płaski i zlicowany w ≥ 65 %. Każda bateria ma więc z tyłu
   gładki blok gniazda; kapsuły, butle i klatki nie mogą tam mieć formy. Poza tym pompująca nakrętka (Arcade) musi kończyć się 16 mm poniżej
   stropu komórki, bo zatwierdzony klip podnosi ją o 26 mm.
5. **Pancerz kadłuba nie ma kolizji i nie zmienia obrysu kostki** — płyty mają do 38 mm grubości, a jedyne formy wychodzące dalej to
   istniejące sloty `nose` / `lip` / `ramp`. Prawdziwie obłe lub skośne bryły kadłuba (kliny, nadkola zintegrowane z burtą, zwężany dziób)
   wymagają nowych typów bloków konstrukcyjnych, czyli zmiany zasad gry.
6. **Mechanizmy poruszają się po zatwierdzonych ścieżkach** — mogłem zmienić to, *co* się rusza, ale nie *jak*. Mocniejszy charakter
   animacji (odrzut całej kołyski, obrót bębna, podwójne ugięcie zawieszenia) wymaga nowych kluczy.

Jeżeli któraś z tych granic ma zostać przesunięta, potrzebna jest decyzja właściciela — nie zrobiłem tego samodzielnie.

## Testy: co jest sprawdzone, a co nie

**Sprawdzone w Blenderze** (`pbuild.py`, wynik w `variants*/…/_QA.json`): obwiednia, obrysy węzłów względem zatwierdzonych, powierzchnie
styku portów (LOD0 i LOD1), interfejs, przeniesienie animacji i jej zasięg, szwy pętli, eksport i ponowny import FBX/GLB. Wynik: @@PASS@@.

**Sprawdzone niezależnie na plikach GLB** (`h4tests.py`, numpy, bez Blendera): P1 węzły i hierarchia, P2 pivoty i port montażowy, P3 obrys
funkcjonalny, P4 animacja identyczna ze wzorcem, P5 geometria faktycznie nowa, P6 komplet plików; dla pancerza K1–K4 (nazwy, grubość, zasięg,
pliki). Wynik: @@TESTS@@ — `tests/HERO4_TEST_SUMMARY.md`.

**NIE sprawdzone:** import do Unity, materiały URP, prefaby, kolizje i montaż w grze, wydajność, Windows player. Nic z tej dostawy nie
zostało wstawione do `Assets/`.

## Uczciwie o ograniczeniach

- Rdzeń kostki `R4B_Cube` i `R4X_ExplosiveTrapDeployer` to zatwierdzone modele Pass 1 — w scenach są tylko przemalowane na paletę pancerza.
  Nie są propozycjami Pass 4.
- W robotach remisowych druga para (`Lq` / `Rq`) zestawia propozycje Q ze sobą (Brutal: pancerz Q + koło Q; Arcade: bateria Q). To
  zestawienie jest umowne — każdą kategorię należy oceniać osobno na planszach `02`–`05`.
- Kontur „toon” w stylu E to efekt sceny (look-dev), nie cecha modelu.
- Tekstury pancerza: jeden atlas 2048² na zestaw 24–56 siatek; w największych zbliżeniach widać mniejszą gęstość tekseli niż na częściach.
- To pięć zestawów benchmarkowych. **Biblioteka nie została rozszerzona** na pozostałe części.

## Pytania do właściciela (bramka)

1. Dla każdej z 23 propozycji: czy PRAWA jest lepsza od LEWEJ? (tak / nie / poprawić — co)
2. Remisy: Brutal kadłub P czy Q, Brutal koło P czy Q, Arcade bateria P czy Q?
3. Czy nowy pancerz kadłuba (z wariantami zużycia) jest właściwym kierunkiem dla wszystkich stylów?
4. Czy przesunąć którąś z granic kontraktu z listy powyżej (zwłaszcza obrys lufy)?

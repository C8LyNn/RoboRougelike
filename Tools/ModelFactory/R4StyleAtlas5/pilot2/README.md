# R4 Style Atlas 5 — pilot 2: pięć niezależnych konstrukcji (#1239)

Decyzja właściciela po pierwszej wersji atlasu: zamiast jednej konstrukcji przekształcanej parametrami stylu — **osiem typów,
każdy zaprojektowany pięć razy od zera**. Ten katalog zawiera 40 wariantów, plansze do odbioru, interaktywny podgląd i wyniki QA.

Tylko art. Nic w `Assets/`, bez kodu gry, bez integracji z Unity, bez scalania. Unity: **UNITY_NOT_RUN**.
Odbiór artystyczny: **OCZEKUJE NA WŁAŚCICIELA** — testy techniczne poniżej go nie zastępują.

Poprzednie 600 wariantów (`../atlas/`), ich raporty QA, plansze i rejestr **nie zostały zmienione** — są wersją porównawczą.
Pozostałe 29 typów z rigiem i przebudowa 120 wykonanych wcześniej są wstrzymane do decyzji.

## Co obejrzeć

| | |
|---|---|
| `boards/00_overview_full.jpg`, `_clay`, `_wire`, `_silhouette` | wszystkie 8 typów × 5 stylów na jednej planszy |
| `boards/01_Cube.jpg` … `08_Trap.jpg` | jeden typ, pięć wariantów w identycznej kamerze: pełne materiały (3/4, przód, bok), neutralny materiał, wireframe, czarne sylwetki (przód, bok, 3/4), cztery klatki rzeczywistej animacji |
| `boards/09_Robot_wheeled.jpg`, `10_Robot_hover.jpg` | ta sama konstrukcja robota złożona z elementów pilota w pięciu stylach |
| `preview/R4_ATLAS5_PILOT2_PREVIEW.zip` | podgląd interaktywny: rozpakuj i otwórz `index.html` (bez serwera i internetu) |

Podgląd pokazuje pięć wariantów obok siebie w jednej scenie, z odtwarzaniem animacji zsynchronizowanym między stylami
(wybór klipu, tempo), przełącznikiem materiały / neutralny / wireframe / sylwetka, widokami przód / bok / góra, LOD1,
gniazdami oraz dwoma robotami. Osadzone są te same pliki GLB, które przeszły QA.

## Co jest wspólne, a co osobne

Wspólny dla pięciu wariantów typu jest wyłącznie kontrakt gry: obrys (envelope), płaszczyzny i pola kontaktu, nazwy i pivoty
węzłów, gniazda (`Socket_*`, `Port_*`, `Muzzle`, `Deploy_Out`), hierarchia, oś czasu i zdarzenia animacji. Dla działka, koła i
silnika pochodzą one z zatwierdzonych plików źródłowych (#939, #1018, #941) i są porównywane z nimi w Blenderze.

Osobny jest projekt: każdy wariant ma własną funkcję w `scripts/pdefs.py` (40 funkcji), własną listę części, proporcje
i układ detali. Wspólna jest biblioteka prymitywów (`akit`) i potok produkcji. Automatyczne „stylizatory” starego atlasu
(pogrubianie, pomijanie części, dodatki) są w pilocie wyłączone (`authored=True`) — model jest dokładnie taki, jak go
zaprojektowano.

## 40 wariantów

| typ | styl | konstrukcja | tris / LOD1 | części | kontakty | interfejs źródła | Blender | GLB | 
|---|---|---|---|---|---|---|---|---|
| Klocek sześcienny | A | lakierowana skorupa: 3 x 3 panele o nierównym podziale, w szczelinach paski światła | 2912 / 572 | 13 | 6 PASS | zgodny | PASS | PASS |
|  | B | cztery przykręcone płyty pancerne na twarz, krzyżowy kanał ramy, śruby w narożach | 5372 / 44 | 97 | 6 PASS | zgodny | PASS | PASS |
|  | C | jedna gładka bryła z miękkim zaokrągleniem, bez paneli | 204 / 44 | 1 | 6 PASS | zgodny | PASS | PASS |
|  | D | pozszywane blachy różnej wielkości, łaty, nity, rdza | 1992 / 44 | 36 | 6 PASS | zgodny | PASS | PASS |
|  | E | jedna wypukła poducha na twarz w grubej kremowej ramie | 1548 / 44 | 1 | 6 PASS | zgodny | PASS | PASS |
| Łącznik konstrukcyjny STRUT | A | sześciokątna belka-kręgosłup z kołnierzami, szynami światła i pierścieniem | 7380 / 940 | 12 | 10 PASS | zgodny | PASS | PASS |
|  | B | dwuteownik z żebrami, krzyżulcami z prętów i śrubami pasów | 12324 / 964 | 231 | 10 PASS | zgodny | PASS | PASS |
|  | C | jedna gruba rura między klockami | 520 / 144 | 3 | 10 PASS | zgodny | PASS | PASS |
|  | D | dwie niedopasowane rury, przyspawane blachy, pręt zbrojeniowy i łańcuch | 5008 / 712 | 89 | 10 PASS | zgodny | PASS | PASS |
|  | E | belka-kość: dwa kielichy z talią i obręczą | 4198 / 452 | 4 | 10 PASS | zgodny | PASS | PASS |
| Działko podstawowe (Basic Gun) | A | niska kopuła, wieża-dysk z dwoma płetwami, sześciokątny smukły korpus, cienka lufa emitera z pierścieniami światła | 3672 / 1794 | 20 | 1 PASS | zgodny (węzły +0.0 mm) | PASS | PASS |
|  | B | pierścień łożyska na śrubach, jarzmo w kształcie U z zastrzałami, skrzynka amunicyjna, żebrowany płaszcz lufy i hamulec wylotowy | 3032 / 2040 | 54 | 1 PASS | zgodny (węzły +0.0 mm) | PASS | PASS |
|  | C | walec, zaokrąglony blok i gruba lufa - trzy bryły | 580 / 332 | 7 | 1 PASS | zgodny (węzły +0.1 mm) | PASS | PASS |
|  | D | beczka-podstawa, rama z rurek, asymetryczna osłona z blachy, kanister, celownik na opasce, lufa z rury w tulejach | 2052 / 1708 | 33 | 1 PASS | zgodny (węzły +2.3 mm) | PASS | PASS |
|  | E | pękata podstawa, bębny czopów po bokach, baniasty blaster z płetwą i rozszerzonym wylotem | 3142 / 1034 | 15 | 1 PASS | zgodny (węzły +0.9 mm) | PASS | PASS |
| Koło z napędem | A | gładka opona-torus, pełna tarcza aero, pierścienie światła na obręczy, ramię-stożek | 3748 / 1762 | 8 | 1 PASS | zgodny (węzły +0.0 mm) | PASS | PASS |
|  | B | opona z 16 klockami, obręcz ze szprychami na śrubach, tarcza hamulca z zaciskiem, mocowanie z zastrzałami | 3024 / 2212 | 59 | 1 PASS | zgodny (węzły +0.0 mm) | PASS | PASS |
|  | C | masywna opona ze ściętym profilem, prosta piasta z pięcioma kołkami | 788 / 544 | 9 | 1 PASS | zgodny (węzły +0.0 mm) | PASS | PASS |
|  | D | łatana opona z nierównymi klockami, kolce, łańcuch, pokrywa piasty na śrubach, resor piórowy i kątownik | 2768 / 2124 | 48 | 1 PASS | zgodny (węzły +0.0 mm) | PASS | PASS |
|  | E | opona-balon z ośmioma obłymi wypustkami, pękata piasta i kołpak-gwiazda | 3104 / 1076 | 13 | 1 PASS | zgodny (węzły +0.0 mm) | PASS | PASS |
| Silnik poduszkowy (HoverEngine) | A | zamknięta gładka gondola z pierścieniem światła, kopuła piasty, 9 cienkich łopatek | 3486 / 1692 | 25 | 1 PASS | zgodny (węzły +0.0 mm) | PASS | PASS |
|  | B | kwadratowa klatka z kształtowników, kanał, żebrowany silnik, zastrzały, 4 łopaty-wiosła | 3384 / 2232 | 52 | 1 PASS | zgodny (węzły +0.0 mm) | PASS | PASS |
|  | C | gruby pierścień, kolumna i trzy szerokie łopaty | 1364 / 752 | 17 | 1 PASS | zgodny (węzły +0.0 mm) | PASS | PASS |
|  | D | kanał z beczki z obręczami, krzywe rozpory z rur, boczny silnik z paskiem, 5 niedopasowanych łopat | 1904 / 1356 | 32 | 1 PASS | zgodny (węzły +0.0 mm) | PASS | PASS |
|  | E | kanał-pączek z obręczą, piasta-grzyb, 4 płatki | 4964 / 1830 | 20 | 1 PASS | zgodny (węzły +1.3 mm) | PASS | PASS |
| Bateria | A | pozioma kapsuła w dwóch kołyskach, widoczny świecący rdzeń między tulejami | 5864 / 1976 | 18 | 2 PASS | — | PASS | PASS |
|  | B | dwa stojące bębny z kołnierzami, zaciski, szyna łącząca, opaski na śrubach, radiatory po bokach | 4000 / 1552 | 63 | 2 PASS | — | PASS | PASS |
|  | C | jedno grube ogniwo z opaską i biegunem | 752 / 304 | 5 | 2 PASS | — | PASS | PASS |
|  | D | akumulator samochodowy w taśmach, przechylony kanister, kable rozruchowe | 3016 / 640 | 61 | 2 PASS | — | PASS | PASS |
|  | E | przechylona pękata kapsuła z piorunem i grubymi obręczami | 3126 / 642 | 9 | 2 PASS | — | PASS | PASS |
| Wyrzutnik pułapek wybuchowych | A | kołyska-dysk z bocznymi płetwami, pierścień magnetyczny, szyny światła | 3336 / 1822 | 24 | 2 PASS | — | PASS | PASS |
|  | B | pochylnia rolkowa w ramie z blach, koła łańcuchowe, tłok dociskowy, magazyn z żebrami | 3564 / 2140 | 62 | 2 PASS | — | PASS | PASS |
|  | C | klin-rampa i blok magazynu | 320 / 192 | 5 | 2 PASS | — | PASS | PASS |
|  | D | prowadnice z dwóch rur, magazyn z beczki, łaty, pręt | 2026 / 1262 | 36 | 2 PASS | — | PASS | PASS |
|  | E | misa na sprężynie z obłą wieżą i gwiazdą | 4084 / 1210 | 16 | 2 PASS | — | PASS | PASS |
| Pułapka wybuchowa | A | cienki dysk z trzema płatami i kopułką światła | 1236 / 708 | 9 | 0 PASS | — | PASS | PASS |
|  | B | mina morska: kula z rogami zapalników na kołnierzu ze śrubami | 1352 / 1176 | 23 | 0 PASS | — | PASS | PASS |
|  | C | płaski krążek z dużym przyciskiem | 198 / 92 | 2 | 0 PASS | — | PASS | PASS |
|  | D | butla z gwoździami na skrzyżowanych blachach, zapalnik z puszki na kablu | 722 / 722 | 15 | 0 PASS | — | PASS | PASS |
|  | E | gwiazda z kulami na ramionach i dużą kopułą | 1544 / 440 | 8 | 0 PASS | — | PASS | PASS |

„interfejs źródła”: pozycje gniazd < 0,01 mm i obroty < 0,1° względem zatwierdzonego GLB; „węzły +x mm” to największe
wyjście węzła poza obrys węzła źródłowego (tolerancja 3 mm).

## Wyniki techniczne

| | |
|---|---|
| Blender 5.3.0: budowa, eksport FBX + GLB (LOD0, LOD1), reimport, QA | **40 / 40 BLENDER_PASS** (pierwsze podejście i po poprawkach projektu) |
| Testy numpy na wyeksportowanych GLB (T1–T6, `tests/PILOT2_TEST_SUMMARY.md`) | **8 / 8 typów z kompletem 5/5** |
| Pola kontaktu, linijka `patch_test` (LOD0 i LOD1) | wszystkie PASS |
| Gniazda i hierarchia identyczne w pięciu stylach (T3) | odchyłka < 0,05 mm |
| Podgląd: autotest (każdy typ, każdy tryb, oba roboty) | bez błędów (jedyny wpis konsoli to brak `favicon`) |
| Unity / Windows player | **NOT_RUN** |

## Sylwetki — liczby

IoU czarnych sylwetek w identycznej kamerze (1,00 = ten sam obrys). Poprzedni atlas miał średnio 0,96–0,97.

| typ | IoU sylwetek 3/4 (średnio) | max pary | wszystkie widoki (średnio) | poprzedni atlas 3/4 |
|---|---|---|---|---|
| Klocek sześcienny | 0.98 | 1.00 | 0.98 | 0.98 |
| Łącznik konstrukcyjny STRUT | 0.95 | 0.98 | 0.95 | 0.98 |
| Działko podstawowe (Basic Gun) | 0.84 | 0.88 | 0.83 | 0.98 |
| Koło z napędem | 0.88 | 0.96 | 0.87 | 0.99 |
| Silnik poduszkowy (HoverEngine) | 0.84 | 0.94 | 0.79 | 0.99 |
| Bateria | 0.84 | 0.91 | 0.80 | 0.95 |
| Wyrzutnik pułapek wybuchowych | 0.83 | 0.87 | 0.79 | 0.98 |
| Pułapka wybuchowa | 0.66 | 0.82 | 0.58 | 0.91 |


Średnio dla siedmiu urządzeń (bez klocka): **0.83** wobec 0.97 poprzednio.

Jak to czytać uczciwie:

- Klocek ma ten sam obrys celowo (standard montażu) — różni się podziałem paneli, detalem i materiałem.
- STRUT (0,95) to w 2/3 dwa klocki węzłowe; różni się tylko człon między nimi.
- Koło (0,88): promień toczny i płyta mocowania są wspólne z kontraktu; różni się szerokość, bieżnik i piasta.
  Z przodu Sleek odstaje wyraźnie (0,61–0,70), pozostałe cztery są do siebie podobne w obrysie (0,87–0,95).
- Moduły 1×1×1 z pełnym kontaktem od dołu (bateria, wyrzutnik, działko) mają wspólną płytę podstawy — ona zawyża IoU.
  Powyżej płyty konstrukcje są różne.
- IoU nie mierzy podziału brył wewnątrz obrysu. To dowód pomocniczy, nie ocena.

## Materiały i tekstury

Generowane, kafelkowane zestawy w `textures/` (jeden kafel = jedna ściana klocka 0,52 m, mapowanie trójpłaszczyznowe):

| styl | base colour | ORM (szorstkość, metaliczność) | normal | charakter |
|---|---|---|---|---|
| A Sleek | `A_base` | `A_orm` | `A_normal` (0,35) | lakier, siatka linii paneli, mikrowgłębienia |
| B Brutal | `B_base` | `B_orm` | `B_normal` (1,0) | szczotkowana i odlewana stal, wżery, olej, rysy |
| C Arcade | — | — | — | celowo bez tekstur: matowe, spokojne kolory |
| D Scrap | `D_base` | `D_orm` | `D_normal` (1,3) | odpryski farby, wykwity rdzy z gołym metalem na krawędziach, rysy, wgniecenia |
| E Toon | `E_base` | — | — | malowana rampa światła po wysokości, pociągnięcia pędzla |

Kolor farby strefy pozostaje współczynnikiem materiału (tekstura go mnoży), więc malowanie drużyn nadal działa. W GLB są to
`baseColorTexture`, `metallicRoughnessTexture` i `normalTexture` ze współczynnikami — sprawdza to test T5. Nowa strefa
`R4P_Wear` (rdza w Scrap) jest **propozycją**.

## Ograniczenia, o których trzeba wiedzieć

1. **Animacja mechanizmów nie jest jeszcze projektowana per styl.** Działko, koło i silnik odtwarzają zatwierdzony ruch
   funkcjonalny (przeniesiony z plików źródłowych), a styl zmienia tylko charakter czasu w klipach przejściowych.
   Bateria, wyrzutnik, pułapka, klocek i STRUT mają klipy proceduralne: rusza się ładunek, lampka i korpus, ale np. tłok
   wyrzutnika Brutal, sprężyna Toon czy pierścień Sleek stoją. „Wyrazista animacja” Toon jest więc spełniona częściowo.
2. **Tekstury są proceduralne i kafelkowane**, bez unikalnego rozwinięcia UV i bez wypalania AO / krzywizny: rdza i
   przetarcia nie podążają za krawędziami konkretnego modelu, a rampa Toon zależy tylko od wysokości.
3. **Budżety trójkątów są nierówne**: od 198 (pułapka Arcade) do 12 324 (STRUT Brutal, 231 części ze śrubami). Robot
   kołowy z 15 elementów ma w LOD0 od ok. 7 tys. (Arcade) do ok. 69 tys. (Brutal) trójkątów. LOD1 istnieje dla każdego wariantu.
4. Porównanie robotów jest renderowane w three.js z plików GLB, nie w Unity. Zderzenia, kolizje i zachowanie w grze nie
   były sprawdzane.
5. Presety malowania drużyn nie były ponownie strojone pod nowe palety pilota.
6. Interfejs baterii, wyrzutnika i pułapki pochodzi z #1238 i sam jest propozycją.
7. Po kroku Blendera pliki zostały oczyszczone z lokalnych ścieżek komputera budującego: w FBX (odwołania do tekstur,
   podmiana bajtów o tej samej długości) i w metadanych JPG. Reimport FBX był sprawdzany przed tą podmianą; sumy w `*_QA.json`
   opisują pliki dostarczone. Pliki `.blend` zawierają ścieżkę wewnątrz skompresowanych danych — nie były zmieniane.

## Odtworzenie

```
cd Tools/ModelFactory/R4StyleAtlas5/scripts
python3 ptextures.py                 # zestawy tekstur
python3 pcheck.py                    # kontrola generatora: bryły, obrys, kontakty, obrysy węzłów
python3 pgen.py                      # pilot2/mesh_source (ignorowane przez git)
blender -b --factory-startup --python pbuild.py -- <pilot2/variants> <pilot2/mesh_source> <katalog z repo źródeł>
python3 pboards.py prep boards       # plansze i metryki sylwetek
python3 ptests.py                    # testy GLB
python3 pviewer.py                   # podgląd i plansze robotów
python3 pregistry.py                 # rejestr
```

Nowy typ w pilocie = pięć funkcji `@D("Typ", "A".."E")` w `pdefs.py` i wpis w `CID`; reszta potoku jest wspólna.

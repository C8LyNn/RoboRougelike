# R4 Style Atlas 5 — Visual Polish Pass 1 (#1239)

Odpowiedź na ocenę pilota 2: kierunki pięciu stylów zostają, ale roboty z dystansu wyglądały jak zabawki z klocków.
Ta paczka poprawia formę, spójność, materiały i światło dla wybranego zakresu i pokazuje wynik w skali gry.

Tylko art. Nic w `Assets/`, bez kodu gry, bez zmian montażu, fizyki i kolizji, bez integracji z Unity (**UNITY_NOT_RUN**).
Pilot 2 (`../pilot2/`) i 600 wariantów atlasu (`../atlas/`) są bez zmian. Odbiór artystyczny: **oczekuje na właściciela**.

## Co obejrzeć

| plansza (`boards/`) | co pokazuje |
|---|---|
| `01_before_after_wheeled.jpg` | robot kołowy 28 części, BEFORE / AFTER × 5 stylów |
| `02_before_after_hover.jpg` | robot poduszkowy 27 części, BEFORE / AFTER |
| `03_before_after_parts.jpg` | zbliżenie: klocek, broń, koło, silnik |
| `04_before_after_gameplay.jpg` | kamera rozgrywki: gracz, trzech przeciwników, boss 96 części, przeszkody |
| `05_scale_ladder_after.jpg` | 28 → 96 → 196 części → kamera rozgrywki |
| `06_gameplay_720_pixels.jpg`, `07_gameplay_1080_distant.jpg` | wycinki 1:1 bez skalowania — tyle widać, gdy robot zajmuje mały fragment ekranu |

Pełne klatki są w `renders/<Styl>/`: każde ujęcie w 1920×1080 i 1280×720 (100 plików). BEFORE i AFTER mają identyczną
konstrukcję, kamerę, arenę i światło; różnią się wyłącznie assetami.

## Decyzje projektowe

**1. Klocek przestał być „ramką z panelem”.** W pilocie 2 każdy klocek miał własną ramę i własny podział paneli, więc 14
klocków dawało 14 ram i siatkę podwójnych krawędzi — to był główny powód wyglądu „z klocków”. Teraz klocek jest gładkim
rdzeniem konstrukcyjnym w ciemnym kolorze podbudowy (44 trójkąty), a wygląd kadłuba przejmuje warstwa pancerza.

**2. Pancerz łączony ponad blokami (propozycja).** Na odsłoniętych ścianach generowane są płyty obejmujące kilka klocków
naraz (do 3×2 w Sleek, 2×2 w Brutal i Toon, 3×3 w Arcade, nieregularne arkusze z brakami w Scrap). Linie pancerza biegną
przez sąsiednie klocki, a liczba krawędzi spada. Każdy styl ma własny charakter płyty: cienka z linią światła, gruba ze
śrubami i żebrem, zaokrąglona, łatana z nitami, poduszkowa.

**3. Zamknięte szwy.** 30-milimetrowe szczeliny między częściami, przez które było widać tło, wypełnia cofnięta, ciemna
uszczelka. Szew czyta się jak zaprojektowana linia podziału, a nie dziura.

**4. Napęd zintegrowany z podwoziem.** Nad kołami pojawiają się nadkola w stylu danej estetyki, jeśli komórka nad kołem
jest wolna. Płyty mocowania kół, silników i broni mają kolor podbudowy, więc część „wyrasta” z kadłuba, zamiast stać na
własnej tacce z ramą. To samo dotyczy klocków węzłowych łącznika STRUT.

**5. Hierarchia wartości zamiast pasteli.** Każdy styl ma teraz trzy czytelne poziomy: kolor kadłuba, ciemną podbudowę
i jeden akcent. Arcade przeszło z jasnego błękitu i kremu na głęboki granat z ochrą i prawie czarną podbudową; Sleek z
bieli na perłowy, lekko metaliczny szary z grafitem; Brutal ma jaśniejszą stal na pancerzu, żeby detale nie ginęły w czerni.

**6. Cieniowanie związane z geometrią.** Każdy model ma wypalone w kolorach wierzchołków zacienienie zagłębień i
przetarcie wypukłych krawędzi (osobna receptura na styl: brązowy brud w Scrap, chłodny malowany cień w Toon, jasne
krawędzie w Brutal). Trafia to do GLB jako `COLOR_0` i do FBX jako kolory wierzchołków.

**7. Broń Sleek.** Cienki pręt zastąpiły dwie szyny akceleratora ze świecącym rdzeniem — czytelne z kamery rozgrywki.

**8. Scena testowa.** Neutralna arena: płyty betonowe 2,5 m, filary, skrzynie, bariery i ściany dla skali, jedno słońce
z miękkim cieniem, niebo jako światło wypełniające, cieniowanie otoczenia w czasie rzeczywistym. Poprzednie plansze
stały w szarej pustce bez punktów odniesienia, co samo w sobie wzmacniało efekt zabawki.

## Co powstało jako assety, a co jest propozycją

- **Assety po pełnym QA:** 25 wariantów w `variants/` — Cube, STRUT, Basic Gun, Wheel, HoverEngine × 5 stylów.
  Blender 5.3: **25 / 25 BLENDER_PASS** (budowa, eksport FBX + GLB, LOD1, reimport, pola kontaktu, zgodność gniazd ze
  źródłem). Testy GLB: **5 / 5 typów z kompletem 5/5** (`tests/POLISH1_TEST_SUMMARY.md`), w tym obecność `COLOR_0`.
  Kontrakt jest ten sam co w pilocie 2: obrysy, pola kontaktu, węzły, gniazda i oś czasu animacji bez zmian.
- **Propozycja wizualna, nie asset:** warstwa kadłuba (płyty, uszczelki, nadkola) jest generowana z układu robota
  (`scripts/qdefs.py: hull_layer`) i istnieje tylko w renderach. Nie ma kolizji i nie zmienia zasad montażu, ale w grze
  wymagałaby kodu: przebudowy przy każdej zmianie konstrukcji oraz reguły, co dzieje się z płytą obejmującą kilka
  klocków, gdy jeden zostanie odłączony lub zniszczony. To decyzja projektowa do podjęcia, zanim pójdzie dalej.
- **Bez zmian:** bateria, wyrzutnik i pułapka są w scenach w wersji z pilota 2.

## Liczba trójkątów

| scena | A przed → po | B przed → po | C przed → po | D przed → po | E przed → po |
|---|---|---|---|---|---|
| robot kołowy (28) | 106 → 58 tys. | 139 → 45 tys. | 12 → 12 tys. | 69 → 34 tys. | 68 → 42 tys. |
| robot poduszkowy (27) | 93 → 51 tys. | 120 → 46 tys. | 15 → 14 tys. | 56 → 29 tys. | 74 → 50 tys. |
| kamera rozgrywki (207) | 742 → 367 tys. | 1002 → 302 tys. | 87 → 86 tys. | 479 → 222 tys. | 485 → 282 tys. |
| robot 96 części (po) | 140 tys. (w tym pancerz 14) | 118 tys. (w tym pancerz 17) | 33 tys. (w tym pancerz 10) | 88 tys. (w tym pancerz 11) | 104 tys. (w tym pancerz 12) |
| robot 196 części (po) | 257 tys. (w tym pancerz 25) | 223 tys. (w tym pancerz 34) | 63 tys. (w tym pancerz 20) | 165 tys. (w tym pancerz 22) | 194 tys. (w tym pancerz 23) |

Spadek bierze się z klocka (z 204–5 372 do 44 trójkątów) i z węzłów STRUT; płyty pancerza są tańsze niż detal, który
zastąpiły. Arcade jest bez zmian, bo już było lekkie.

## Ograniczenia

1. **Zakres jest wąski.** Dopracowany jest kadłub i jego spójność. Broń, koło i silnik dostały integrację mocowania,
   nowe palety i cieniowanie; poza bronią Sleek ich bryły są takie jak w pilocie 2. „Osobowość” broni i napędów Toon
   oraz „duże mechaniczne formy” Brutal nie są jeszcze zrobione.
2. **Niedopracowane typy widać.** Baterie i wyrzutniki na dachu mają stare palety — w Arcade i Sleek wyraźnie odstają.
3. **Proporcji całych robotów nie da się zmienić samym artem.** Promień koła, obrys broni i rozmiar komórki są częścią
   kontraktu kolizji. Lufa działka mieści się w walcu o średnicy 74 mm przy klocku 520 mm — dlatego uzbrojenie wygląda
   drobno. Większa broń oznaczałaby zmianę obrysu, czyli decyzję gameplayową.
4. **Z kamery rozgrywki nadkola zasłaniają koła od góry.** Robot czyta się jako bryła z błotnikami; do sprawdzenia, czy
   to pożądane.
5. **Kolory wierzchołków wymagają shadera, który je mnoży.** glTF robi to standardowo; domyślny URP Lit w Unity nie.
6. **Cieniowanie Toon to nadal PBR** z malowaną rampą i wypalonym cieniem. Shader toon (pasma światła, kontur) jest
   pracą po stronie silnika i tu go nie ma.
7. **Animacje mechanizmów** są bez zmian (osobny etap). Węzły i pivoty zostały zachowane, więc można je rozbudować.
8. Rendery pochodzą z Blendera (EEVEE), nie z Unity. Wydajność w grze nie była mierzona — podane są tylko trójkąty.

## Odtworzenie

```
cd Tools/ModelFactory/R4StyleAtlas5/scripts
python3 qgen.py                      # polish1/mesh_source + polish1/scene_source (ignorowane przez git)
blender -b --factory-startup --python pbuild.py -- <polish1/variants> <polish1/mesh_source> <katalog źródeł>
blender -b --factory-startup --python qscene.py -- <polish1/renders> <polish1/scene_source> <pilot2/variants> <polish1/variants> <polish1/textures>
python3 pscrub.py ../polish1 <nazwa konta>   # usuwa lokalne ścieżki z FBX, JPG i logów
python3 ptests.py polish1
python3 qboards.py
```

Układy robotów, kamery, arena i światło: `shots.json` oraz `scripts/qdefs.py`, `scripts/qgen.py`.

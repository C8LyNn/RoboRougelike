# R4 Style Atlas 5 — Visual Polish Pass 2 (#1239)

Zakres wg zlecenia: Basic Gun, Wheel, HoverEngine, Battery, ExplosiveTrapDeployer, ExplosiveTrap × 5 stylów, dopasowanie do
warstwy kadłuba, animacje mechanizmów, warstwa kadłuba z przypisaniem fragmentów do części, testy BEFORE / AFTER.

Tylko art. Nic w `Assets/`, bez kodu gry, bez zmian mechaniki, liczby części i zasad konstrukcji. **UNITY_NOT_RUN.**
`pilot2/`, `polish1/` i `atlas/` bez zmian. Odbiór artystyczny: **oczekuje na właściciela** — QA techniczne go nie zastępuje.

## Co obejrzeć (`boards/`)

| plansza | co pokazuje |
|---|---|
| `01_models_gun_wheel_hover.jpg`, `02_models_battery_deployer_trap.jpg` | pojedyncze modele, BEFORE (pass 1) / AFTER |
| `03_buggy.jpg`, `04_skimmer.jpg` | robot kołowy (29 części) i poduszkowy (36) |
| `05_crawler_100.jpg`, `06_dreadnought_200.jpg` | roboty 103 i 208 części |
| `07_gameplay.jpg` | kamera rozgrywki |
| `08_distant_1to1.jpg` | sześć odległych pojazdów (33–94 m), każdy w całości w kadrze, wycinki 1:1 z klatki 1080p |
| `09_destruction.jpg` | siedem scenariuszy niszczenia części pod wielokomórkowymi płytami |
| `10a…10f_mechanisms_*.jpg` | klatki animacji mechanizmów, pięć stylów |
| `../preview/R4_ATLAS5_POLISH2_PREVIEW.zip` | podgląd interaktywny (rozpakuj, `index.html`): animacje zsynchronizowane między stylami, wybór klipu |

Pełne klatki: `renders/<Styl>/` (1920×1080 i 1280×720). BEFORE = stan z Visual Polish Pass 1 na tych samych blueprintach.

## Co zmieniono i dlaczego — przykłady

- **Broń wypełnia swój obrys.** W pilocie 2 korpusy działek były cienkie wewnątrz dużego obrysu węzła. Teraz korpus sięga
  granic węzła `Pitch` (28 × 15 × 48 cm), więc broń ma masę z dystansu bez powiększania obrysu kolizji ani lufy.
- **Sleek:** spłaszczony korpus-kropla z zagłębionymi kanałami bocznymi, odsłonięte ogniwo zasilające za kołyską, dwie szyny
  akceleratora; koło z turbinową obręczą i podświetlonym kanałem; bateria z szeroko otwartym, świecącym rdzeniem.
- **Brutal:** wieniec zębaty łożyska, jarzmo z zastrzałami, bęben amunicyjny, zamek i zderzaki odrzutu; koło z amortyzatorem
  i zaciskiem; silnik na czterech siłownikach z odsłoniętą przekładnią; drugie wykończenie stali (oksydowana) obok szczotkowanej,
  polerowanej i żeliwa.
- **Arcade:** nadal 5–9 części na model, ale broń i piasty mają kolor akcentu, a kadłub pasek akcentu — funkcja czyta się kolorem.
- **Scrap:** tarcza działka z dwóch spawanych blach, rama z różnych profili, koło z naspawanymi płytami kolców i łańcuchem,
  bateria w klatce z prętów; na planszach AFTER wzór rdzy jest przesunięty dla każdego obiektu (wymóg dla shadera, patrz niżej).
- **Toon:** granatowa podbudowa zamiast kremowej (krem czytał się jak plastik), jedna duża bryła na model z wyraźnym dzwonem
  wylotu / kopułą / dzbanem, grube obręcze, mniej drobnych kulek.
- **Mocowania jako gniazda.** Płyty mocowania wszystkich sześciu typów są rdzeniem w kolorze podbudowy, więc moduły siedzą
  w pancerzu, a nie na własnych tackach.
- **Nadkola nie zasłaniają kół.** Zostało wąskie nadkole przy burcie (94 mm); koło jest w całości widoczne z kamery rozgrywki.
- **Blueprinty.** Cztery pojazdy zbudowane jak w grze: wnęki kół w kadłubie, nadbudówki, wieże, pomosty STRUT, uzbrojenie
  na sponsonach — sprawdzane funkcją `validate` (podparcie każdej części, spójność kadłuba).

## Mechanizmy (węzły dodatkowe, ruch pisany osobno dla każdego stylu)

| typ | A | B | C | D | E |
|---|---|---|---|---|---|
| Basic Gun | 6292 tris · Ring, VentL, VentR | 4528 tris · Bolt, Buffer, Gear | 568 tris · Slide | 2098 tris · Crank, Rattle | 5158 tris · Antenna, Bell |
| Wheel | 5540 tris · Halo, VaneL, VaneU | 3232 tris · Caliper, Shock | 740 tris · Arm | 2984 tris · Flap, Spring | 4668 tris · Coil |
| HoverEngine | 4934 tris · Halo, Iris1, Iris2, Iris3 | 4564 tris · Gear, Rods | 1508 tris · Collar | 1644 tris · Flap, Motor | 4340 tris · Cap, Lip |
| Battery | 4120 tris · Core, Sleeve | 1720 tris · Fan, Relay | 656 tris · Cap | 1276 tris · Clamp, Needle | 2056 tris · Emblem, Jug |
| ExplosiveTrapDeployer | 1926 tris · ClampL, ClampR, Lift | 3092 tris · HookL, HookR, Ram, Sprocket | 532 tris · Gate, Kicker | 1514 tris · Arm, Latch | 2726 tris · Coil, Paddle |
| ExplosiveTrap | 1368 tris · Petal1, Petal2, Petal3 | 1824 tris · Collar, Horns | 598 tris · Ring | 826 tris · Plate, Wire | 1604 tris · Crown |

Razem 62 węzły mechanizmów w 30 wariantach. Zdarzenia, gniazda, pivoty i czasy klipów są bez zmian; działko, koło i silnik
nadal odtwarzają zatwierdzony ruch funkcjonalny, a mechanizmy są do niego doczepione. Ruch nie wychodzi poza obrys o więcej niż
10 mm (test w builderze; cztery pierwsze wersje go nie przeszły i zostały poprawione).

## Wyniki

| | |
|---|---|
| Blender 5.3.0: budowa, eksport FBX + GLB, LOD1, reimport, pola kontaktu, gniazda, ruch w obrysie | **40 / 40 BLENDER_PASS** (8 typów × 5; Cube i STRUT to rdzenie z pass 1 w nowej palecie) |
| Testy numpy na GLB, w tym `COLOR_0` (`tests/POLISH2_TEST_SUMMARY.md`) | **8 / 8 typów z kompletem 5/5** |
| Test niszczenia warstwy kadłuba (generator) | PASS, 7 scenariuszy × 5 stylów |
| Podgląd: wczytanie i dwa klipy bez błędów konsoli | OK |
| Unity / Windows player | **NOT_RUN** |

Test T3 porównuje teraz węzły kontraktu; węzły mechanizmów są z założenia różne w każdym stylu.

## Trójkąty (LOD0)

| scena | A przed → po | B przed → po | C przed → po | D przed → po | E przed → po |
|---|---|---|---|---|---|
| buggy (29) | 45 → 58 tys. | 40 → 45 tys. | 10 → 12 tys. | 29 → 25 tys. | 34 → 43 tys. |
| skimmer (36) | 63 → 85 tys. | 57 → 70 tys. | 17 → 19 tys. | 37 → 30 tys. | 59 → 66 tys. |
| crawler (103) | 166 → 198 tys. | 141 → 135 tys. | 36 → 37 tys. | 101 → 79 tys. | 115 → 143 tys. |
| dreadnought (208) | 248 → 320 tys. | 223 → 235 tys. | 64 → 66 tys. | 159 → 134 tys. | 183 → 242 tys. |
| kamera rozgrywki (226) | 367 → 459 tys. | 321 → 343 tys. | 87 → 93 tys. | 226 → 187 tys. | 277 → 342 tys. |
| fragmenty kadłuba: buggy / crawler / dreadnought | 83 / 224 / 494 | 83 / 224 / 494 | 83 / 224 / 494 | 76 / 216 / 483 | 83 / 224 / 494 |
| zestaw kafli: siatek / trójkątów | 18 / 1444 | 18 / 2512 | 18 / 828 | 50 / 1188 | 18 / 864 |

W odróżnieniu od pass 1 liczba trójkątów **wzrosła** w Sleek i Toon, nieznacznie w Brutal (bogatsza broń, koła, baterie); Scrap spadł.

## Warstwa kadłuba

Płyty wyglądają jak wielokomórkowe, ale składają się z kafli po jednym na komórkę, każdy z właścicielem. Opis, manifest
i reguły dla prototypu runtime: [`hull_layer/README.md`](hull_layer/README.md). Zestawy siatek: `hull_kit/<Styl>/`.

## Integracja z Unity

Wymagania (URP, emission i bloom, metaliczność, odbicia, toon, `COLOR_0`, materiały, LOD): [`UNITY_INTEGRATION.md`](UNITY_INTEGRATION.md).

## Ograniczenia

1. **Toon nadal nie ma shadera toon.** Poprawione są bryły, paleta i wypalony cień; pasma światła i kontur to praca po stronie
   silnika. To najsłabszy punkt względem celu „jak dopracowana stylizowana gra”.
2. **Modele są z prymitywów** (bryły obrotowe, profile, skrzynki), bez rzeźbienia i bez unikalnych UV. Złożone, płynne powierzchnie
   Sleek i miękkie formy Toon są przybliżone, nie wymodelowane ręcznie.
3. **Zestaw kafli kadłuba nie ma wypalonego `COLOR_0`**; płyty na planszach cieniuje tylko oświetlenie sceny.
4. **Przesunięcie wzoru rdzy w Scrap** na planszach AFTER jest funkcją shadera w Blenderze, nie cechą plików.
5. **Scena jest dość ciemna**, bez bloomu; światła Sleek wyglądają skromniej niż będą w silniku z bloomem.
6. Animacje mechanizmów są proceduralne (klucze co klatkę), nie ręcznie animowane; nie były oceniane w ruchu poza podglądem.
7. Rendery pochodzą z Blendera (EEVEE); wydajność w grze nie była mierzona.

## Odtworzenie

```
cd Tools/ModelFactory/R4StyleAtlas5/scripts
python3 rcheck.py && python3 rcheck.py Battery,Deployer,Trap sweep
python3 rgen.py                       # polish2/mesh_source, kit_source, scene_source (git-ignored), hull_layer/, shots.json
blender -b --factory-startup --python pbuild.py -- <polish2/variants> <polish2/mesh_source> <katalog źródeł>
blender -b --factory-startup --python rscene.py -- <renders> <scene_source> <kit_source> <shots.json> <polish1/variants> <pilot2/variants> <polish2/variants> <textures> <hull_kit>
python3 pscrub.py ../polish2 <nazwa konta> && python3 ptests.py polish2 && python3 rboards.py
```

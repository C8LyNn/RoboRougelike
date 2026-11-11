# R4 Sleek — pierwsza biblioteka modułów 1×1×1

Pakiet assetów do oceny. **Nie jest zintegrowany z grą**: nic w `Assets/`, żadnego kodu, prefabów, scen ani zmian mechaniki.
Wszystko leży w `Tools/ModelFactory/R4ModulesSleek1x1/`.

| | |
|---|---|
| Moduły montowane na robocie | **61** (katalog 1–61, grupy A–F) |
| Pułapki rozstawiane w świecie gry | **6** (doprecyzowanie zlecenia) |
| Blender 5.3.0 — budowa, eksport, reimport, QA | **67 / 67 BLENDER_PASS** |
| Testy numpy na wyeksportowanych GLB | **67 / 67 PASS** |
| Unity / Windows player | **UNITY_NOT_RUN / WINDOWS_PLAYER_NOT_RUN** |

Nie powstały modele wieżyczek ani dronów — zgodnie z doprecyzowaniem będą składane z istniejących broni, napędów i klocków.
Wyrzutniki rampy, ściany i jump pada (59–61) zostały w bibliotece po potwierdzeniu.

## Co jest w pakiecie

```
modules/R4X_<Nazwa>/        61 modułów: .blend, .fbx, .glb, _LOD1.fbx, _LOD1.glb, renders/ (front, side, 34front, 34rear),
                            _QA.json, _model_manifest.json, _animation_manifest.json (tylko modele z częściami ruchomymi)
world_traps/R4D_<Nazwa>/    6 pułapek stojących na ziemi, ten sam komplet plików
compare/                    wave1..3_board.jpg (rendery Blender), wave1..3_on_blocks.jpg i catalog_family.jpg (three.js)
preview/                    R4X_MODULES_PREVIEW.zip — podgląd offline (otwórz index.html)
tests/                      TEST_SUMMARY.md, module_tests.json, log i podsumowanie budowy w Blenderze
mesh_source/                siatki źródłowe z generatora (wejście do Blendera)
scripts/                    generator, budowa, testy, plansze, podgląd, manifesty
CATALOG_TABLES.md           pełna tabela 67 modeli: sylwetka, kontakty, trójkąty, części ruchome, wynik
INDEX.json, SHA256SUMS.txt, CHANGED_FILES.txt
```

## Standard rodziny

**Zgodność ze stylem A_Sleek.** Duże bryły modułów powstają z tych samych generatorów co klocki (`r4sleek`, `r4convex` z #1235
i `r4panel` z pilota #1232, importowane bez zmian): płyta na ramie, zaokrąglone fazy stałej szerokości, zaokrąglone narożniki
płyt. Parametry są skalowane do wielkości części (`mkit.style`), przy dużych bryłach równe parametrom klocków
(faza 20 mm, 4 segmenty, rama 4 mm). Małe elementy techniczne to proste bryły toczone lub fazowane w jednej strefie.

**Obrys.** Każdy moduł mieści się w bryle klocka 0,52 m (±0,26 m od środka komórki), także części ruchome w całym
zakresie podglądowej animacji (sprawdzane klatka po klatce). Pułapki: obrys jednej komórki, początek układu na ziemi,
wysokość do 0,30 m.

**Strefy malowania.** `R4P_Base`, `R4P_Secondary`, `R4P_Trim` — te same trzy strefy co w klockach, z tymi samymi
domyślnymi kolorami R41. Dwie dodatkowe strefy to **proposal**: `R4P_Metal` (gołe elementy mechaniczne, domyślnie
`Steel_Light`) i `R4P_Glow` (akcent emisyjny, kolor per moduł — główny nośnik czytelności funkcji).

**Kontakty i mocowanie (proposal).** Kontakt to płyta leżąca dokładnie na płaszczyźnie styku ±0,26 m, mierzona tą samą
miarą co klocki (`patch_test`: 9×9 promieni, ≥80 % dla kontaktu pełnego, ≥65 % dla częściowego, nic nie wystaje).
Każdy kontakt ma pusty obiekt `Port_<kierunek>_000` (`_part` dla częściowego), a moduł — `Socket_Mount` na głównej
ścianie montażowej. Liczba kontaktów zależy od funkcji:

| kontakty | liczba modułów | przykłady |
|---|---|---|
| 1 | 40 | Radar, Blink, Gyroscope (dół); JumpDrive, HoverEngine (góra) |
| 2 | 12 | Battery (dół + tył), Suspension i CoreBooster (dół + góra) |
| 3 | 7 | DrivePower, StructuralScanner, WallDeployer (dół + boki), IndependentSteering |
| 4 | 1 | HullBooster (dół, góra, boki) |
| 5 | 1 | Booster (przód pełny + cztery częściowe) |

Mocowanie główne: dół — 57, góra — 3 (JumpDrive, HoverEngine, IndependentSteering), przód — 1 (Booster).

**Części ruchome i animacja (proposal).** 25 modeli ma osobne obiekty z własnym pivotem (np. `R4X_Gyroscope_Rotor`)
i jeden zapętlony klip podglądowy `Active` (klatki 1–49, 24 fps; klatka 1 = poza spoczynkowa). Klip jest zapisany w FBX
i GLB i sprawdzony po reimporcie. To wyłącznie warstwa wizualna — nie definiuje żadnych czasów ani efektów gry.

**Punkty funkcyjne (proposal).** `FX_Thrust`, `FX_Emit`, `FX_Grapple`, `Deploy_Out` — puste obiekty z osią +Z w kierunku
działania, tam gdzie funkcja ma oczywisty kierunek.

**Budżet.** Moduły: średnio 2 672 trójkąty LOD0 (556–4 952), LOD1 średnio 785. Pułapki: 660–1 596 LOD0.
LOD1 to te same bryły bez płyt, z pojedynczą fazą i o połowę rzadszym podziałem obrotowym.

## Czytelność sylwetek

Zasada: żaden moduł nie jest „tą samą skrzynką w innym kolorze". Sprawdza to test M6 — rzuty z przodu, z boku i z góry
wszystkiego powyżej wspólnej podstawy montażowej; model nie przechodzi, jeśli we **wszystkich trzech** rzutach pokrywa się
z innym w ponad 80 %. Najbliższa para w bibliotece to HullBooster / Booster (0,79).

Ten test wymusił przebudowę w trakcie pracy: pierwsza wersja wyrzutników pułapek miała trzy wspólne podwozia na sześć
modeli (pokrycie do 0,99), a cztery wyrzutniki wieżyczek różniły się tylko głowicą (0,85–0,89). Teraz każdy z sześciu
wyrzutników pułapek ma własne podwozie, a wyrzutniki wieżyczek — inny układ zacisków i inną głowicę.

Wyrzutnik pułapki pokazuje jako ładunek **miniaturę swojej pułapki** — zbudowaną tą samą funkcją co model świata gry,
więc para wyrzutnik–pułapka jest rozpoznawalna bez opisu.

## Pułapki w świecie gry

| plik | pułapka | sylwetka |
|---|---|---|
| `R4D_ExplosiveTrap` | wybuchowa | płaska mina z sześcioma rogami zapalników i lampą |
| `R4D_EMPTrap` | EMP | krążek z cewką i trzema łukami trzymającymi kulę ładunku |
| `R4D_KnockbackTrap` | odrzucająca | kwadratowa płyta z szewronami na czterech tłokach |
| `R4D_SnareTrap` | unieruchamiająca | pierścień z dwiema zębatymi szczękami |
| `R4D_SlowTrap` | spowalniająca | sześciokątna podstawa z kopułą żelu i trzema dyszami |
| `R4D_BurnTrap` | podpalająca | trzy butle paliwa wokół komina z płomieniem |

## QA

W Blenderze (rekord `_QA.json` przy każdym modelu): topologia każdego obiektu i LOD1 (brak krawędzi niemanifoldowych,
brzegowych, ścian zerowych i odwróconych normalnych), obrys, przemiatanie animacji, test kontaktów LOD0 i LOD1, strefy
materiałów, reimport FBX i GLB w świeżej scenie (liczba siatek, trójkąty, skala, puste obiekty, akcje animacji).

Niezależnie, na wyeksportowanych GLB (`tests/TEST_SUMMARY.md`): M1 plik i budżet, M2 obrys, M3 kontakty, M4 gniazdo
montażowe, M5 odstęp 30 mm od klocka A_Sleek w kierunku montażu, M6 sylwetka, M7 animacja.

## Rzeczy oznaczone jako proposal

1. Zestaw kontaktów każdego modułu i ściana `Socket_Mount`.
2. Strefy `R4P_Metal` i `R4P_Glow` oraz kolor akcentu per moduł.
3. Podział na części ruchome, pivoty i klip `Active`.
4. Punkty `FX_*` i `Deploy_Out`.
5. Prefiksy nazw: `R4X_` (moduł na robocie), `R4D_` (obiekt rozstawiony).
6. Moduły ruchu 9–16 w wersji 1×1×1 istnieją obok zatwierdzonych modeli `R4M_*` z biblioteki napędów — które wersje
   trafiają do gry, to decyzja projektowa, nie rozstrzygana tutaj.
7. `DeployableShieldGenerator` (29) i `DeployableAmplifier` (43) są wykonane jako wersje montowane na robocie, tak jak
   wyrzutniki z grupy F.

## Czego ten pakiet nie zawiera

- Importu do Unity, prefabów, materiałów Unity, kolizji — nie wykonano i nie sprawdzano.
- Tekstur: modele używają kolorów materiałów; UV są proste (rozwinięcie planarne / obrotowe), wystarczające pod kolor
  i ewentualny późniejszy detal, nie pod malowaną teksturę.
- Obiektów po rozstawieniu innych niż pułapki — patrz niżej.

## Następny etap

- Obiekty świata dla pozostałych urządzeń rozstawianych: generator tarczy (29), wzmacniacz (43), rampa (59), ściana (60),
  jump pad (61).
- Wieżyczki i drony jako zestawy z istniejących broni, napędów i klocków (bez nowych modeli) — układy zestawów do
  uzgodnienia.
- Import do Unity i test w buildzie Windows po akceptacji kształtów i interfejsu.
- Kolizje: do decyzji, czy moduł dostaje własny uproszczony collider, czy pudełko komórki.

## Odtworzenie

```
cd scripts
python3 precheck.py                 # topologia, obrys, kontakty (numpy)
python3 gen_mesh_source.py          # -> mesh_source/*.json
blender -b --factory-startup --python build_modules.py -- <OUT> ../mesh_source
python3 tests_modules.py            # testy na GLB
python3 render_modules.py           # plansze
python3 build_viewer.py             # podgląd offline
python3 make_manifests.py           # manifesty, INDEX, tabele, sumy
```

Wymaga katalogów `R4BlocksRedesign1232` i `R4BlocksSleek1235` (gałąź bazowa tego PR) — generatory stylu są stamtąd
importowane, nie kopiowane.

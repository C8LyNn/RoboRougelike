# M03-012 — Zderzak sprężynujący U (MODEL-03)

**Katalog:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`. Kolejka **M03**, pozycja **012**. **Claim CREATE**: `2423919115c0caa01fc0fb852eb36813fe82b442` (2026-10-24T21:51:27.108Z), bez nadpisania innych rezerwacji. Model wizualny; nie dodaje fizyki, zdrowia ani mechaniki zderzeń.

## Konstrukcja

Moduł 1m na froncie ramy, **U z jednego zamkniętego wklęsłego profilu**, dwa niezależne wałki ślizgowe w rzeczywiście wydrążonych prowadnicach i dwie geometryczne sprężyny śrubowe. Dodatkowo dwa pierścienie montażowe z prawdziwymi otworami, wspornik poprzeczny, osadzenia sprężyn, ceramiczny pas odporny na ścieranie, gumowe końcówki i bursztynowe znaczniki. Pięć materiałów: stal grafitowa, ceramika, miedź, elastomer i bursztynowe akcenty emisyjne. Kolor nie jest jedynym wyróżnikiem elementów.

**Współrzędne Blendera:** X lewo/prawo, Y przód/tył, Z góra; 1 jednostka = 1 metr. Pivot `ROOT=(0,0,0)` na tylnej płaszczyźnie mocowania `Y=0`. Osie eksportu GLB/FBX konwertowane do Unity Y-up. Dwa fizyczne pierścienie montażowe z otworami osi Y: `(-0.4,0,0)`, `(+0.4,0,0)`, promień otworu **38 mm**. Lokatory `SNAP_FRAME_LEFT/RIGHT` i `SNAP_FRAME_ORIGIN` oznaczają płaszczyznę mocowania do ramy. Nie deklaruje się prefabów Unity ani rzeczywistego systemu SNAP gry.

**Animacja:** `U_SLIDER` przesuwa cały zderzak U, oba wałki i ruchome talerzyki sprężyn o **60 mm w +Y** (do wnętrza pojazdu). `SPRING_SCALE_L/R` skracają zamknięte, pięciozwojowe sprężyny z **150 do 90 mm**. Klucze 1/16/31/46/61 przy 30 FPS; dwa pełne cykle kompresji. Każdy wałek ma promień 24 mm i pracuje w otworze o promieniu 27 mm (**3 mm luzu promieniowego**). Sprężyny o promieniu środka 64 mm i promieniu drutu 5,5 mm omijają zewnętrzne powierzchnie prowadnic o promieniu 45 mm (**13,5 mm luzu radialnego**). Minimalny odstęp U od przedniej krawędzi prowadnicy: **90 mm**. Przy maksymalnym ugięciu skok zwoju wynosi 18 mm, wobec średnicy drutu 11 mm.

**Świadome styki montażowe:** stalowy wspornik zachodzi na zewnętrzne obrzeża pierścieni (bez zasłaniania otworów), prowadnice są osadzone we wsporniku, wałki zachodzą na 10 mm w zderzak U, a talerzyki sprężyn kontaktują się z korpusem i ruchomą osłoną. Takie styki są zamierzone; nie należy mylić ich z przypadkowymi przenikaniami.

## Generowanie i QA

```bash
python3 Tools/ModelFactory/M03/M03-012/qa_m03_012.py
python3 Tools/ModelFactory/M03/M03-012/generate_m03_012.py --out build/M03-012
blender -b --factory-startup --python-exit-code 1 --python Tools/ModelFactory/M03/M03-012/generate_m03_012.py -- --out build/M03-012 --blender
```

Skrypt Python 3 tworzy deterministyczny **OBJ/MTL, manifest i raport QA**, nawet bez Blendera. Tryb `--blender` uruchamia rzeczywiste `bpy`, tworzy hierarchię animacji i eksportuje GLB/FBX/BLEND. Generator usuwa wyłącznie własną kolekcję `RRL_M03_012_U_SPRING_BUMPER` i odtwarza ją bez mnożenia siatek, zachowując cudze kolekcje i sceny. Walidator niezależnie parsuje OBJ, sprawdza krawędzie każdej siatki, normalne, dodatnie objętości, prześwity i 61 pozycji ruchu oraz SHA-256 powtarzalnych eksportów statycznych.

**QA:** wyniki CI i dokładny status Blender/Unity należy odczytać z `.github/workflows/ci-m03-012.yml`. Brak potwierdzonego uruchomienia oznacza **NOT RUN**, a nie PASS. Nie uruchamia się Unity w workflow, więc Unity EditMode, PlayMode, import i ocena wizualna są **NOT RUN**.

**Zakres zmian:** tylko `Tools/ModelFactory/M03/M03-012/` i izolowany workflow; bez wspólnych scen, prefabów, ProjectSettings, gameplayu i zmian w `develop/main`. Oddzielny DRAFT PR do `develop`.

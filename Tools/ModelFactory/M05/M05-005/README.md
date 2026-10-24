# MODEL-05 / M05-005 — Generator tarczy pierścieniowej

**Katalog:** [M05-005, CATALOG_V1.md](https://github.com/C8LyNn/RoboRougelike/blob/feature/rrl-model-factory-queue-v1/Docs/ModelFactory/CATALOG_V1.md).
**Rezerwacja:** \`Docs/ModelFactory/claims/M05-005.json\` na gałęzi kontrolnej, atomowy CREATE commit \`5f5ab76e14322bc1a5a1840a7f4d625fb0d46850\`, 2026-10-24 14:51:24.846 UTC, agent M05.
**Zakres:** wyłącznie model wizualny. Nie implementuje ani nie zastępuje DEF-001, fizycznej tarczy, kolizji, obrażeń, energii gameplayowej ani AI.

## Projekt wizualny

Wewnętrzny generator pola z trzema miedzianymi toroidalnymi pierścieniami, trzema półprzezroczystymi plastrami pola, trzema emisyjnymi ścieżkami, pulsującym dwustożkowym kryształem, czterema słupami nośnymi, dwunastoma promieniowymi wspornikami, śrubami i oznaczeniami LED. Sylwetka przypomina zamkniętą przemysłową komorę generatora, a nie antenę lub projektor.

- **47 oddzielnych zamkniętych brył**, **4358 wierzchołków**, **4106 poligonów**, **8572 trójkąty** w neutralnym OBJ.
- **7 materiałów:** Graphite, Titanium, Copper, Gasket, TealGlass, CyanEnergy, AmberStatus.
- **Wymiary obwiedni:** 0,96 × 0,96 × 0,584 m. Jedna jednostka Blender = jeden metr Unity.
- **Układ współrzędnych:** Blender Z-up; GLB zgodny z glTF (Y-up). Początek mocowania wewnątrz ramy: (0,0,0) — środek spodniej płaszczyzny gumowej podkładki.
- **Animacja:** \`M05-005_CORE_PULSE\`, pivot (0,0,0.36), równomierne skalowanie kryształu \`0.90 → 1.04 → 0.90 → 1.04 → 0.90\` w klatkach 1,16,31,46,61. Żaden ring ani wspornik nie jest dzieckiem pulsującego pivotu.
- **Minimalny obliczeniowy odstęp radialny kryształu od elementów na wysokości ruchu:** 0,04724 m, przy maksymalnym powiększeniu 1.04. Kontrola obejmuje obwiednię wierzchołków i wysokość elementów.

## Pliki

\`geometry.json\` — jedyne źródło prawdy dla materiałów, geometrii i klatek.

\`generate_m05_005.py\` — deterministyczny generator Python bez bpy (OBJ/MTL i QA) oraz skrypt Blender bpy tworzący pełną scenę modułu i animowany GLB.

\`M05-005.obj\` i \`M05-005.mtl\` — **rzeczywiste, zapisane pliki statyczne**; OBJ w neutralnej pozie kryształu.

\`M05-005.static-qa.json\` — metryki geometrii i jawny stan testów.

\`blender_smoke.py\` — test rzeczywistego Blendera: dwa kolejne uruchomienia w jednej scenie, zachowanie obcego obiektu, poprawna animacja, własna kolekcja w bibliotece .blend, obecność animacji i materiałów w GLB.

\`.github/workflows/ci-model-m05-005.yml\` — statyczny test Python oraz instalacja Blendera na runnerze Ubuntu do osobnego testu GLB.

## Generowanie

\`\`\`bash
python3 Tools/ModelFactory/M05/M05-005/generate_m05_005.py --self-test
python3 Tools/ModelFactory/M05/M05-005/generate_m05_005.py --output-dir /tmp/m05-005
blender -b --python Tools/ModelFactory/M05/M05-005/generate_m05_005.py -- --output-dir /tmp/m05-005-blender
blender -b --python Tools/ModelFactory/M05/M05-005/blender_smoke.py -- /tmp/m05-005-blender
\`\`\`

Blender tworzy \`M05-005.blend\` jako **bibliotekę kolekcji do Append** i \`M05-005_animated.glb\` z animacją. Generator nie czyści całej sceny: usuwa tylko kolekcję \`M05-005_ASSET\`, przywraca aktywny obiekt, zaznaczenie i klatki oraz eksportuje wyłącznie własne obiekty.

## Import do Unity 6

OBJ/MTL można importować jako statyczną wizualizację. Dla pulsacji należy użyć animowanego GLB z kompatybilnym importerem glTF, ewentualnie otworzyć kolekcję w Blenderze i wyeksportować FBX z animacją. Po imporcie sprawdzić konwersję osi, skalę 1 m, pivot mocowania, alpha blend materiału TealGlass i emission w URP. Do czasu rzeczywistego importu Unity nie deklarujemy poprawności w runtime gry.

## QA i ograniczenia

**Statyczna geometria:** zamknięte manifoldy, orientacja ścian, 0 zdegenerowanych trójkątów, obwiednia, materiał i konserwatywny odstęp pulsującego rdzenia — weryfikowane w \`validate()\` oraz w GitHub Actions. Deterministyczny OBJ/MTL porównywany byte-for-byte.

**Blender smoke:** testuje rzeczywistą animację, idempotencję, brak kasowania obcych obiektów, poprawną bibliotekę kolekcji i obecność animacji w GLB. Wyniki należy potwierdzić na bieżącym commicie w GitHub Actions.

**NOT RUN:** Unity 6 import, URP material visual inspection, Unity EditMode/PlayMode, ocena estetyki z renderów i integracja z systemem budowania. Model nie jest zintegrowany z mechaniką DEF-001.

**Granice zakresu:** nowa własna gałąź od develop, Draft PR do develop; bez merge, bez zmian w scenach, ProjectSettings, Assets istniejących mechanik, main/develop i cudzych modelach.

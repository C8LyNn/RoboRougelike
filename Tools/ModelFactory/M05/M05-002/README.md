# M05-002 — Radar panelowy płaski (MODEL-05)

**Zadanie:** M05-002 z `Docs/ModelFactory/CATALOG_V1.md` na gałęzi `feature/rrl-model-factory-queue-v1`.
**Rezerwacja:** `Docs/ModelFactory/claims/M05-002.json`, commit `2a0eefb39d559f0c56fb29f100a06f8799f80046`.
**Zakres:** wyłącznie wizualny model sensora; **bez implementacji skanowania, sygnałów lub AI**.

## Projekt

Płaska antena radarowa z 50 prostokątnymi emiterami w układzie 10 × 5, 50 ścieżkami emisyjnymi (cyjan i bursztyn), dwoma ramkami bocznymi, pięcioma żebrami chłodzenia, mechanizmem wychylenia oraz bazą mocowaną od góry. Panel ma obudowę ceramiczną, miedziane elementy, metalową ramę i ciemne szkło.

- 7 materiałów: Gunmetal, Ceramic, Copper, Rubber, Glass, CyanLED, AmberLED.
- 127 osobnych zamkniętych komponentów; 1160 wierzchołków i 834 wielokąty w neutralnym statycznym OBJ.
- Skala: **1 Blender unit = 1 metr**. Układ źródłowy **Z-up**; glTF/GLB eksportowany do **Y-up**.
- Główne gniazdo mocowania: `M05-002_MOUNT_ROOT`, pozycja `(0,0,0)` (środek spodniej powierzchni podstawy).
- Przegub: `M05-002_TILT_X` przy `(0,0.235,0.535)`; wychylenie w osi X `-22° → 0° → +22° → 0°` (klatki 1/25/49/73).
- Kinematyka V2: **45 próbek** od −22° do +22° co 1°, **66 690** konserwatywnych testów obwiedni AABB każdej części stałej względem ruchomej. **PASS** — bez niezamierzonych przecięć między zespołami. Wymagana inspekcja wizualna Blendera.

## Pliki

- `geometry.json` — pojedyncze źródło prawdy dla geometrii, materiałów i animacji.
- `generate_m05_002.py` — generator deterministyczny: Python bez Blendera (statyczne OBJ/MTL) lub Blender `bpy` (scena, animacja, GLB, BLEND).
- `M05-002.obj`, `M05-002.mtl` — faktycznie zapisane pliki statyczne w neutralnym wychyleniu.
- `M05-002.static-qa.json` — metryki obliczone przy tworzeniu modelu; niezależny test CI znajduje się w `.github/workflows/ci-model-m05-002.yml`.

## Uruchamianie

```bash
python3 Tools/ModelFactory/M05/M05-002/generate_m05_002.py --self-test
python3 Tools/ModelFactory/M05/M05-002/generate_m05_002.py --output-dir /tmp/m05-002
blender -b --python Tools/ModelFactory/M05/M05-002/generate_m05_002.py -- --output-dir /tmp/m05-002-blender
```

W Blenderze generator zapisze `M05-002.blend` jako **bibliotekę kolekcji do dołączenia przez Append** (nie kompletne, samodzielne wnętrze sceny) i `M05-002_animated.glb` zawierający tylko własne obiekty. Bez Blendera wygeneruje tylko statyczne OBJ/MTL i jawnie zgłosi brak `bpy`.

## Import do Unity 6

1. Zweryfikuj `M05-002_animated.glb` w Blenderze (przebieg animacji, normalne, brak przenikania). Unity 6 może wymagać importera glTF/GLB — dobierz go osobno do projektu; sam plik GLB nie gwarantuje natywnego importu animacji.
2. Alternatywnie w Blenderze wyeksportuj FBX z odpowiednią konwersją osi i animacją `M05-002_TILT_X`, a następnie importuj go do Unity.
3. Dopasuj materiał emissive do URP i sprawdź, czy pivot podstawy, skala i ruch wychylenia są zgodne z systemem budowania.
4. Nie zastępuj istniejącego sensora SEN-001 ani M05-001 bez odrębnej decyzji integracyjnej.

## QA

- **PASS (generator statyczny):** geometria 127 zamkniętych brył, orientacja ścian, 0 zdegenerowanych trójkątów, 7 materiałów, unikalne nazwy, rezerwacja GitHub.
- **CI:** wykonuje test składni, `--self-test`, odtwarzalność OBJ/MTL przez `cmp`, integralność indeksów.
- **NOT RUN:** Blender, render i ocena wizualna, eksport animowanego GLB, Unity EditMode/PlayMode/import.
- **Integracja:** tylko Draft PR do `develop`, bez zmian w scenach i istniejących mechanikach.

**Uwaga:** statyczny OBJ nie zawiera animacji, a kontrola prześwitu jest analityczna, nie zastępuje wizualnej weryfikacji animacji.

## Naprawa V2 — wychylenie anteny i izolacja w Blenderze (24.10.2026)

**Usterka geometrii:** w pierwotnej wersji belka przegubu przy Y=0,12 m przechodziła przez antenę w czasie wychylenia; kołnierze nacinały ramki boczne, a przy skrajnym wychyleniu pojawiały się dodatkowe kolizje. Poprzedni test sprawdzał wyłącznie przybliżoną odległość centralnego masztu od tyłu anteny, a **nie** łączną geometrię mocowań.

**Naprawa mechaniczna:** rzeczywista oś wychylenia X została wyrównana w Y=**0,235 m**, Z=0,535 m. Skorygowano belkę przegubu (rozstaw do 1,20 m), jej kołnierze (X=±0,61 m), łącznik z masztem i dwa przednie zestawy śrub. Lokalna pozycja 114 ruchomych elementów została przeliczona względem nowej osi tak, aby **zachować neutralną światową pozycję i sylwetkę panelu**. Fizyczne mocowanie ma teraz spójną oś kinematyczną, a elementy stałe nie wchodzą w obszar ruchu anteny.

**Nowy test:** `swept_clearance()` pobiera **rzeczywiste wierzchołki** każdej bryły (w tym cylindrów), przelicza 45 kątów od −22° do +22° co 1° i kontroluje obwiednie 13 stałych × 114 ruchomych części, razem **66 690 porównań AABB**. Kontrola wykazała brak przecięć pomiędzy zespołem bazowym a anteną. Obejmuje mocowanie, belkę, kołnierze, śruby, obudowę, żebra i emitery. AABB daje konserwatywne przybliżenie, nie zastępuje Blender/Unity i fizyki.

**Usterka bezpieczeństwa:** poprzedni generator wykonywał `bpy.ops.object.delete()` na **wszystkich obiektach otwartej sceny**. Po poprawce usuwa wyłącznie własną kolekcję `M05-002_ASSET`; generuje nazwy z prefiksem `M05-002_`. GLB eksportuje z `use_selection=True` wyłącznie własny moduł, przywraca wcześniejsze zaznaczenie, aktywny obiekt i stan klatek. Plik `M05-002.blend` zapisuje przez `bpy.data.libraries.write()` jako dołączalną bibliotekę kolekcji, zamiast kopiować przypadkowe cudze modele.

**Artefakty:** zaktualizowano źródło prawdy `geometry.json`, rzeczywisty `M05-002.obj`, skrypt generatora, raport statyczny i workflow CI. Liczebność pozostaje **127 części / 1160 wierzchołków / 834 poligony / 7 materiałów**, ale geometria bazowego przegubu jest już poprawiona.

**Dowody:** [GitHub Actions — PASS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40103852222) potwierdził wykonanie testu 45 pozycji, odtworzenie OBJ/MTL, składnię Pythona i kontrolę liczby brył. Eksport wykonywany przez Blender `bpy`, kontrola wizualna i Unity 6: **NOT RUN**. Zmiana ochrony sceny i eksportu jest testowana statycznie; wykonanie w GUI Blendera pozostaje warunkiem zamknięcia QA.

**Integracja:** poprawki należy ocenić i przenieść do macierzystej gałęzi Draft PR #46, bez zmian w grze, scenach, ProjectSettings lub pozostałych modelach. ID `M05-002` zachowuje dotychczasowy claim.

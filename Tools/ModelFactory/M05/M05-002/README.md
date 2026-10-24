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
- Przegub: `M05-002_TILT_X` przy `(0,0,0.535)`; wychylenie w osi X `-22° → 0° → +22° → 0°` (klatki 1/25/49/73).
- Obliczeniowy minimalny prześwit obudowy panela od centralnego masztu przy skrajnym wychyleniu: **0,11164 m**. Nie jest to pełna analiza kolizji wszystkich komponentów; wymagany przegląd w Blenderze.

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

W Blenderze generator zapisze `M05-002.blend` i `M05-002_animated.glb`. Bez Blendera wygeneruje tylko statyczne OBJ/MTL i jawnie zgłosi brak `bpy`.

## Import do Unity 6

1. Zweryfikuj `M05-002_animated.glb` w Blenderze (przebieg animacji, normalne, brak przenikania). Unity 6 może wymagać importera glTF/GLB — dobierz go osobno do projektu; sam plik GLB nie gwarantuje natywnego importu animacji.
2. Alternatywnie w Blenderze wyeksportuj FBX z odpowiednią konwersją osi i animacją `M05-002_TILT_X`, a następnie importuj go do Unity.
3. Dopasuj materiał emissive do URP i sprawdź, czy pivot podstawy, skala i ruch wychylenia są zgodne z systemem budowania.
4. Nie zastępuj istniejącego sensora SEN-001 ani M05-001 bez odrębnej decyzji integracyjnej.

## QA

- **PASS (generator statyczny):** geometra 127 zamkniętych brył, orientacja ścian, 0 zdegenerowanych trójkątów, 7 materiałów, unikalne nazwy, rezerwacja GitHub.
- **CI:** wykonuje test składni, `--self-test`, odtwarzalność OBJ/MTL przez `cmp`, integralność indeksów.
- **NOT RUN:** Blender, render i ocena wizualna, eksport animowanego GLB, Unity EditMode/PlayMode/import.
- **Integracja:** tylko Draft PR do `develop`, bez zmian w scenach i istniejących mechanikach.

**Uwaga:** statyczny OBJ nie zawiera animacji, a kontrola prześwitu jest analityczna, nie zastępuje wizualnej weryfikacji animacji.

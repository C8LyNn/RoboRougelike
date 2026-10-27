# ENV01-001 — niska bariera ochronna (ENV concept pilot)

**Status:** `ART_REVIEW_PENDING`. Jedyny zatwierdzony pilot ENV01; nie jest częścią M01–M05, PARTSIZE ani CHL. Bez importu do Unity, bez integracji, bez merge.

## Sylwetka i zastosowanie
Autorska, niska, stalowo-betonowa bariera ochronna do krawędzi areny lub toru przeszkód. Szeroki trapezowy korpus z betonu; grafitowe stalowe stopy, słupki i poręcze; cztery bursztynowe reflektory. Jedna nieruchoma całość, bez HP, destrukcji, rigów, animacji i skryptów gry. Zastosowanie wyłącznie jako przyszły asset statyczny po ręcznej decyzji HQ.

## Kontrakt geometryczny (propozycja, nie kanon gry)
- **Blender:** 1 unit = 1 m, +Z up, +Y = strona od drogi / przód, +X = długość bariery.
- **glTF/GLB:** exporter Blender z `export_yup=True` konwertuje +Z up na +Y up; przód Blender +Y odpowiada glTF -Z. Unity importer może stosować własne konwersje; **Unity NOT_RUN**.
- **Pivot:** `ENV01_001_ROOT` w (0,0,0), środek rzutu bariery na poziomie gruntu.
- **Bryła:** ok. 2.4 m długości × 0.7 m głębokości × 0.94 m wysokości; szczegółowe AABB w raporcie QA.
- **Proponowana kolizja:** jeden `BoxCollider`, w układzie Blender center (0,0,0.47), size (2.46,0.70,0.96) m. To tylko propozycja — brak testu PhysX.
- **Materiały:** maksymalnie 3: beton chłodnoszary, grafitowa stal, bursztynowe oznaczenia.
- **Budżet:** 200–1500 trójkątów, weryfikacja w Blender QA. Bez tekstur zewnętrznych.
- **Ruch:** `ANIMATION_NOT_APPLICABLE`. Statyczny obiekt.

## Odtworzenie
W Ubuntu z Blender 4.x:
```bash
blender --background --python-exit-code 1 --python Tools/EnvironmentFactory/ENV01/ENV01-001/generate_env01_001.py -- --out Tools/EnvironmentFactory/ENV01/ENV01-001/generated
blender --background --python-exit-code 1 --python Tools/EnvironmentFactory/ENV01/ENV01-001/qa_env01_001.py -- --out Tools/EnvironmentFactory/ENV01/ENV01-001/generated
```
Generator zapisuje rzeczywisty `.blend`, `.glb`, `preview_front.png`, `preview_rear.png` oraz `build_report.json`. Walidator w oddzielnym procesie sprawdza manifold/winding, budżet trójkątów, materiały, root pivot, brak animacji, nagłówek GLB i faktyczne PNG; zapisuje `qa_report.json` z SHA-256. Workflow dodaje `SHA256SUMS` i publikuje binaria na **tej samej własnej gałęzi**. Bez sukcesu CI nie deklarować gotowego modelu.

## Pochodzenie i licencja
Cała geometria, nazewnictwo i skrypt są oryginalną konstrukcją przygotowaną dla projektu RoboRougelike; nie korzystano z modeli, geometrii, tekstur ani plików konkurencji. Licencja **CC0-1.0** dla niniejszego assetu i jego skryptów; zobacz `LICENSE.txt`.

## Ograniczenia
`UNITY_EDITMODE_NOT_RUN`, `UNITY_PLAYMODE_NOT_RUN`, `WINDOWS_NOT_RUN`, `MANUAL_ART_REVIEW_PENDING`. Nie zakładać kompatybilności z kamerą, siatką PARTSIZE, kolizjami toru ani shaderami URP bez odrębnych testów. PR jest wyłącznie do przeglądu, **bez zgody na integrację**.

# M01-005 — Boczny miotacz tarcz (MODEL-01)

**Źródło:** katalog V1 na gałęzi feature/rrl-model-factory-queue-v1, wiersz M01-005.  
**Claim:** Docs/ModelFactory/claims/M01-005.json, CREATE commit 2d1e2926ec3842c2ee21c24568334b3c17e096ac, 2026-10-24T14:51:43.997Z.  
**Zakres:** wyłącznie nowy model wizualny, bez pocisków, obrażeń, logiki WPN, zmian scen i ProjectSettings.

## Sylwetka i konstrukcja

Bocznie mocowany, niski miotacz z **dwoma płaskimi, przeciwbieżnymi rotorami magazynka**. W każdym magazynku jest sześć widocznych, metalowo-ceramicznych gniazd z sygnalizacją. Obudowa ma grafitowy korpus, miedziane prowadnice, jasne ceramiczne osłony, pierścienie ochronne, boczne mocowanie na cztery śruby i płaską szczelinę wylotową. Kolory LED to turkus i bursztyn — akcenty, nie zamiennik materiałów fizycznych.

- Mocowanie: boczne, płaszczyzna X=0; punkt montażu i root (0,0,0).
- Kierunek wylotu: lokalne −Y; układ Blender Z-up, 1 jednostka = 1 metr. GLB używa glTF Y-up.
- Osie rotorów: lokalne X, pivot lewego (0.755,-0.255,0.070), prawego (0.755,0.255,0.070).
- Animacja: rotory przeciwbieżne 0→±360° w klatkach 1–97 (25 kluczy co 15°, 4 klatki). Brak deformacji siatki, tylko obrót rodzica.
- Materiały: Graphite, Steel, Ceramic, Copper, Rubber, TealLED, AmberLED.
- Statyczny OBJ: 80 osobnych zamkniętych brył, 2864 wierzchołki, 1936 wielokątów, 7 materiałów. Wersja statyczna nie zawiera animacji.
- Ochrona sceny: generator usuwa wyłącznie swoją kolekcję M01-005_ASSET przy ponownym uruchomieniu. Nie wykonuje globalnego usuwania obiektów.

## Pliki

- geometry.json — źródło prawdy geometrii, pivotów, materiałów i animacji.
- generate_m01_005.py — kompletny generator Python, działa bez Blendera dla OBJ/MTL, w Blenderze tworzy również animowany GLB i bibliotekę .blend.
- M01-005.obj, M01-005.mtl — **faktycznie wygenerowane i zapisane w repozytorium** statyczne pliki 3D.
- .github/workflows/ci-model-m01-005.yml — testy statyczne i próba rzeczywistego eksportu przez Blender na GitHub Actions; artefakty binarne po udanym wykonaniu są dostępne przy runie jako M01-005-side-disc-launcher-3D.

## Generowanie

    python3 Tools/ModelFactory/M01/M01-005/generate_m01_005.py --self-test
    python3 Tools/ModelFactory/M01/M01-005/generate_m01_005.py --output-dir /tmp/m01-005
    blender -b --python Tools/ModelFactory/M01/M01-005/generate_m01_005.py -- --output-dir /tmp/m01-005-blender

Plik .blend jest biblioteką zawierającą tylko kolekcję modelu; otwieraj przez File → Append. Animowany GLB eksportowany jest z zaznaczenia wyłącznie własnych obiektów. W Unity 6 może być wymagany importer glTF/GLB; alternatywą jest eksport FBX z Blendera po zweryfikowaniu osi, skali i animacji.

## QA i ograniczenia

- Wewnętrzna kontrola generatora sprawdza topologię, orientację wszystkich zamkniętych brył przez dodatnią objętość skierowaną, unikalność nazw, liczbę materiałów i pozycję mount.
- **73 próbki** ruchu od 0 do 360° co 5°, z dwoma przeciwbieżnymi rotorami. **130 816** konserwatywnych testów AABB: każda z 32 części ruchomych względem 48 stałych oraz 16×16 par między rotorami, w każdej próbce.
- Pierścienie ochronne mają osobne przestrzenie osiowe od wirujących magazynków. Między ruchomymi zespołami i nieruchomą obudową nie powinno być przecinania; kontrola AABB jest konserwatywna, ale nie zastępuje przeglądu wizualnego.
- Test GitHub Actions po wykonaniu weryfikuje faktyczne OBJ, MTL, .blend, binarny GLB, liczbę siatek, materiałów i animacji. **Nie deklaruj PASS Blendera, dopóki odpowiedni run nie zakończy się sukcesem.**
- **Unity 6 import, EditMode/PlayMode, render artystyczny i gameplay: NOT RUN** do odrębnej weryfikacji.
- Brak zmian w istniejących mechanikach WPN-001..006, scenach, ProjectSettings i modelach innych agentów.

**Integracja:** wyłącznie własna gałąź i osobny Draft PR do develop, bez automatycznego merge.

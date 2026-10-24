# M01-004 — Moździerz kasetowy (tylko model wizualny)

**Źródło przydziału:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M01-004.json`, commit `686a6a6a4c15730708ad53d707ca2325f6e963e7`. **Agent:** MODEL-01.

## Sylwetka

Kompaktowy moździerz na górnym gnieździe konstrukcyjnym, z otwartą, prawidłowo wydrążoną lufą i masywnym miedzianym hamulcem wylotowym. Po prawej stronie znajduje się wyraźny prostokątny zasobnik 2×3 z sześcioma ozdobnymi komorami i podświetlanymi wskaźnikami. Po lewej: asymetryczna szyna, miedziany profil i radiator. Materiały: grafit, stal, miedź, ceramika, guma i błękitny sygnał emisyjny. Wyłącznie wizualizacja: nie implementuje pocisków, ładowania ani istniejących mechanik WPN.

## Jednostki, pivot i animacja

1 jednostka = 1 metr. Blender: **+Z góra, -Y przód/lufa, +X prawa strona**. Obiekt `M01-004_MOUNT_TOP` ma pivot w `(0,0,0)` na środku płyty montażowej. `M01-004_TILT_PIVOT` jest przesunięty o 0,70 m w +Z i obraca się wokół **lokalnej osi X**. Klatki 1 → 20 → 40: **0° → -18° → 0°**. Dodatkowe puste obiekty `MUZZLE_SOCKET` i `CASSETTE_SERVICE` to punkty referencyjne, nie komponenty gameplay. Przy imporcie do Unity sprawdź mapowanie osi przez glTF (Blender Z-up do Unity Y-up) i nie zakładaj, że samo OBJ przenosi animację.

Weryfikacja generatora obejmuje **21 pozycji** pośrednich, konserwatywne testy obwiedni AABB wszystkich ruchomych części względem nieruchomej podstawy, dodatni prześwit pionowy, niezerowe pola wielokątów i zamkniętą topologię każdego elementu. Ruchome elementy mają wspólny pivot, a detale osadzone w korpusie są świadomym montażem materiałów, nie symulacją kontaktu fizycznego.

## Odtwarzanie

```bash
python3 Tools/ModelFactory/M01/M01-004/generate_m01_004.py --self-test --output-dir /tmp/m01-004
blender --background --factory-startup --python Tools/ModelFactory/M01/M01-004/generate_m01_004.py -- --output-dir /tmp/m01-004 --require-blender
```

Pierwsza komenda tworzy rzeczywiste `M01-004.obj`, `M01-004.mtl` oraz `M01-004.static-qa.json`, bez wymagania Blendera. Druga dodatkowo generuje `M01-004_animated.glb` i `M01-004.blend`. Wbudowany eksport GLB ogranicza się do kolekcji tego modelu (`use_selection=True`). Przy ponownym uruchomieniu skrypt usuwa poprzednią własną kolekcję i osierocone siatki tego ID. Nie dotyka innych kolekcji ani scen Unity.

## QA i ograniczenia

Workflow `.github/workflows/ci-model-m01-004.yml` uruchamia CPython i rzeczywisty Blender na GitHub Actions, a następnie publikuje artefakty `M01-004-static-obj` i `M01-004-animated-glb-blend` po pozytywnych testach. Wynik należy ustalić z faktycznego przebiegu CI, nie z samej obecności tego pliku. Lokalnie w środowisku agenta Blender i Unity są niedostępne. Test importu do Unity 6, fizyki, prefabów i jakości wizualnej wymaga osobnej weryfikacji. Nie ma zmian w `Assets/`, `ProjectSettings/` ani systemach walki.

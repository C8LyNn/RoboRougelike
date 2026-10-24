# M03-011 — Narożnik pochłaniający uderzenia

**Agent:** MODEL-03 · **Katalog:** `Docs/ModelFactory/CATALOG_V1.md` z gałęzi `feature/rrl-model-factory-queue-v1` · **Claim:** `Docs/ModelFactory/claims/M03-011.json` (CREATE 2026-10-24 20:50:45.482 UTC, commit `4a4a2759aebeefd25cb5e66e82a9c075c7b24319`). Nie przejmować ponownie identyfikatora.

## Funkcja i montaż

Moduł narożny dla ramy pojazdu Unity 6. Dwie sztywne, spawane stalowe belki tworzą **jedną zamkniętą siatkę L** z ramionami do 1 m w osiach Blender X/Y. Pivot główny to **(0,0,0)**, punkty SNAP: `SNAP_CORNER_ROOT` (0,0,0), `SNAP_FRAME_X` (1,0,0.2) oraz `SNAP_FRAME_Y` (0,1,0.2). Każdy SNAP jest obiektem `EMPTY` w skrypcie Blender i odpowiada rzeczywistej końcowej płytce mocującej (nie deklarujemy otworów przelotowych). Blender: Z-up; GLB/FBX: konwersja do Unity Y-up. Jednostka = metr. Brak zmian istniejących prefabów, fizyki i systemu obrażeń.

**Wygląd:** ciemna rama grafitowa, gruby zamknięty gumowy zderzak L otaczający zewnętrzny narożnik, dwie jasne ceramiczne listwy ścieralne, miedziane znaczniki, dwa bloki elastomerowe z żebrami i dwa bursztynowe wskaźniki stanu. Pięć odrębnych materiałów PBR (metal/ceramika/guma/miedź/akcent emisyjny). Oznaczenia nie zastępują podstawowej geometrii.

## Ruch / animacja

`IMPACT_COMPRESS`: klatki 1 → 17 → 33 → 49 → 65, 30 FPS. Obiekt `BUMP_GUARD` przesuwa się po przekątnej wewnątrz narożnika o +0.022 m X i +0.022 m Y, po czym wraca; drugi cykl jest identyczny. Łączny skok po przekątnej = 0.031113 m. Oba elastomerowe bloki `SHOCK_X` i `SHOCK_Y` jednocześnie skracają się z 50 do 28 mm i przesuwają o połowę skoku, utrzymując kontakt zarówno ze stalą, jak i zderzakiem. **Minimalny odstęp sztywnej osłony od nieruchomej ramy: 28 mm**, niezależnie od fazy animacji. Ruch jest czysto wizualny; nie implementuje fizyki uderzenia.

## Budowa i walidacja

```sh
python3 Tools/ModelFactory/M03/M03-011/qa_m03_011.py
python3 Tools/ModelFactory/M03/M03-011/generate_m03_011.py --out build/M03-011
blender -b --factory-startup --python-exit-code 1 \
  --python Tools/ModelFactory/M03/M03-011/generate_m03_011.py \
  -- --blender --out build/M03-011
```

`qa_m03_011.py` uruchamia generator w izolowanym katalogu tymczasowym, **czyta rzeczywisty OBJ** i niezależnie sprawdza każdy zamknięty manifold, dodatnią objętość, normalne, liczbę materiałów, 3 SNAP-y, oba bloki i 65 pozycji animacji. Powtórne uruchomienie musi dać bajtowo identyczne OBJ, MTL i manifest. Workflow `.github/workflows/ci-m03-011.yml` wykonuje statyczne QA oraz (jeśli dostępne) rzeczywisty Blender i inspekcję wygenerowanego GLB.

Pliki wynikowe: `M03-011_impact_corner.obj`, `.mtl`, `.glb`, `.fbx`, `.blend`, `M03-011_manifest.json`, `M03-011_qa.json`. Format OBJ jest **statyczną pozą spoczynkową**; animacja znajduje się w plikach eksportowanych przez Blender. Weryfikacja importu w Unity 6 / EditMode / PlayMode: **NOT RUN** bez faktycznego wykonania.

## Granice odpowiedzialności

Tylko `Tools/ModelFactory/M03/M03-011/` i osobny workflow. Żadnych zmian w `Assets/Scenes`, `ProjectSettings`, wspólnych prefabach ani mechanikach. Gałąź modelu jest wyłącznie pochodną `develop`, a PR pozostaje DRAFT do niezależnego przeglądu. Brak automatycznego merge.

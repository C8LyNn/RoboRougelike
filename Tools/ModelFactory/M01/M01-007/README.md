# M01-007 — Impulsowa armata pierścieniowa

**Źródło:** `Docs/ModelFactory/CATALOG_V1.md` na gałęzi `feature/rrl-model-factory-queue-v1`.
**Claim:** `Docs/ModelFactory/claims/M01-007.json`; CREATE commit `29fb7a652d063bd026c58d3b9a91cb480ef6f893`.
**Agent:** MODEL-01 (M01). **Zakres:** wyłącznie wizualny model broni; bez zmian WPN, scen i ProjectSettings.

## Wygląd

Kompaktowy grafitowy korpus z asymetrycznymi panelami z kości słoniowej, miedzianymi okuciami, odsłoniętą wydrążoną lufą i trzema niezależnymi pierścieniami impulsowymi. Każdy pierścień ma tytanową ramę, jasną turkusową warstwę emisyjną, miedzianą krawędź oraz dwanaście ciemnych nitów. Zastosowano siedem materiałów PBR: `Graphite_Armor`, `Titanium`, `Copper`, `Ceramic_Ivory`, `Cyan_Pulse`, `Amber_Status`, `Black_Bore`.

## Mocowanie i animacja

Unity: **1 jednostka = 1 metr**. Lokalny pivot `M01_007_MOUNT_TOP_Z0` w `(0,0,0)` na płaszczyźnie górnego gniazda. W Blenderze **+Y przód**, **+Z góra**. Trzy niezależne kontrolery `M01_007_RING_01_SLIDE_Y`, `M01_007_RING_02_SLIDE_Y`, `M01_007_RING_03_SLIDE_Y` przesuwają pierścienie wzdłuż +Y w zakresie **±0,04 m**, z przesunięciem fazowym 120°. Środki pierścieni: Y=0,68; 0,98; 1,28 m. Animacja ma klatki 1–61 przy 60 FPS. Prześwit promieniowy od zewnętrznej powierzchni lufy do wewnętrznej pierścienia to co najmniej 72 mm (0,178−0,106 m).

## Pliki

`generate_m01_007.py` — samowystarczalny generator geometrii, walidator kontaktów i generator statycznego OBJ/MTL, z opcjonalnym eksportem `bpy` do animowanego GLB, FBX i biblioteki BLEND.

`validate_m01_007.py` — niezależny test topologii zapisanych OBJ/MTL, geometrii i animowanych węzłów glTF 2.0.

`.github/workflows/ci-model-m01-007.yml` — 49-próbkowy test zakresu animacji, Blender eksport i publikacja artefaktu `M01-007-impulse-ring-cannon-3D`. Workflow zapisuje wygenerowane pliki tylko na własnej gałęzi `feature/rrl-model-m01-007-impulse-ring-cannon`.

## Odtwarzanie

```bash
python3 Tools/ModelFactory/M01/M01-007/generate_m01_007.py --self-test
python3 Tools/ModelFactory/M01/M01-007/generate_m01_007.py --out /tmp/m01-007-static
python3 Tools/ModelFactory/M01/M01-007/validate_m01_007.py /tmp/m01-007-static
blender -b --python Tools/ModelFactory/M01/M01-007/generate_m01_007.py -- --out /tmp/m01-007-blender
python3 Tools/ModelFactory/M01/M01-007/validate_m01_007.py /tmp/m01-007-blender
```

Blender usuwa wyłącznie poprzednią kolekcję `M01-007_ASSET`, zachowuje obce obiekty, przywraca scenę i zaznaczenie. BLEND jest biblioteką kolekcji do dołączenia przez **File → Append**. Unity 6 importuje FBX; GLB może wymagać kompatybilnego importera. Brak mechaniki strzelania.

## QA

Testy statyczne sprawdzają manifold, dodatnią objętość, brak zdegenerowanych trójkątów, 49 pozycji pierścieni, rozdział pierścieni od lufy i obudowy oraz separację pierścieni między sobą. Testy w CI mają potwierdzić faktyczny eksport GLB/FBX/BLEND i węzły animacji. **Unity EditMode/PlayMode, integracja z grą i ręczna inspekcja renderu: NOT RUN**, dopóki nie zostaną dostarczone logi i zrzuty z aplikacji. Nie scalać Draft PR bez QA.

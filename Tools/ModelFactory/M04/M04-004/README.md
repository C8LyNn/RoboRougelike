# M04-004 — Dysza odrzutowa z wektorowaniem (MODEL-04)

**Źródło:** `Docs/ModelFactory/CATALOG_V1.md` na `feature/rrl-model-factory-queue-v1`. **Claim:** `Docs/ModelFactory/claims/M04-004.json` na tej samej gałęzi, commit `271bd43f9516bc0f416e2470e5a9ee7a2337c593`.

**Zastosowanie:** wyłącznie nowy moduł wizualny dla RoboRougelike (Unity 6); bez implementacji fizyki, ruchu pojazdu ani nowych mechanik. Punkt mocowania jest **z tyłu ramy**, nie na górze.

## Geometria i stylistyka

- Profilowany, **pusty w środku** korpus dyszy o rosnącej średnicy, ceramiczna gardziel wylotowa i zewnętrzny pierścień ostrzegawczy.
- Sześć przestrzennych stateczników, metaliczne obejmy chłodzące, śruby blokujące, nity i asymetryczna w odbiorze mechaniczna rama jarzma.
- Pięć materiałów: gunmetal, titanium, ceramic, safety_yellow, ion_cyan (emisja wyłącznie w wewnętrznym pierścieniu).
- Metry: **1 jednostka = 1 m**. Lokalna oś wydechu **+Y w Blenderze**, **+Z w Blenderze jest do góry**. Eksporter glTF `export_yup=True`; orientację zweryfikuj po imporcie do Unity, nie zakładaj, że lokalna oś pozostanie +Y.
- Pivot montażu `M04-004_MOUNT_REAR` w `(0,0,0)`; oś yaw `M04-004_YAW_Z` w `(0,0.46,0)`, podległa oś pitch `M04-004_PITCH_X`. Oba Empty są częścią eksportowanej hierarchii.

## Ruch i bezpieczeństwo

Dysza ma dwie osie wychyłu: **yaw ±12°** wokół Blender Z i **pitch ±9°** wokół lokalnej Blender X. Animacja 61 klatek (1–61) wprowadza sinusoidalny ruch, przy czym wszystkie elementy wylotu są dziećmi pivotu pitch, a wsporniki pozostają nieruchome.

Statyczny walidator wykonuje **49 kombinacji ekstremów i wartości pośrednich** (siatka 7×7) oraz **61 próbek klatek animacji**, łącznie 110 próbek pozy. Każdy ruchomy element sprawdzany jest względem każdego nieruchomego przy użyciu konserwatywnych AABB. To **nie jest** test fizyki Unity ani ciągła matematyczna gwarancja braku kontaktu między próbkami. Elementy składające się na tę samą mechaniczną część mają celowe zakładki montażowe.

## Wytwarzanie

Bez Blendera (wystarcza Python 3.10+):

```bash
python Tools/ModelFactory/M04/M04-004/generate_m04_004.py --self-test --output-dir /tmp/m04-004
```

Z Blenderem (preferowane 4.x/5.x):

```bash
blender --background --factory-startup --python Tools/ModelFactory/M04/M04-004/generate_m04_004.py -- --output-dir /tmp/m04-004 --require-blender
```

Generator jest powtarzalny: usuwa wyłącznie własną kolekcję `M04-004_ASSET`, zachowuje selekcję innych obiektów i ogranicza eksport GLB do własnej kolekcji. Zapis `.blend` zapisuje otwartą scenę; dlatego do izolowanego assetu używaj `--factory-startup`. Wyniki: `M04-004.obj`, `M04-004.mtl`, `M04-004.static-qa.json`; w Blenderze dodatkowo `M04-004.blend` i `M04-004_animated.glb`.

## Stan QA

- Claim CREATE: **PASS**, commit `271bd43f9516bc0f416e2470e5a9ee7a2337c593`.
- Python static sweep / deterministic OBJ: **PENDING CI**, nie deklarować PASS przed rzeczywistym uruchomieniem.
- Blender bpy, animowany GLB: **PENDING CI** (workflow próbuje zainstalować Blender i wykonać prawdziwy eksport).
- Import Unity 6, EditMode, PlayMode, test wizualny: **NOT RUN**.
- Zmiany nie obejmują `Assets/Scenes`, `ProjectSettings` ani kodu rozgrywki.

## Integracja

Statyczny OBJ jest neutralną pozą demonstracyjną. Animację i strukturę pivotów zapewnia GLB eksportowany z Blendera. Nie łączyć do `develop` bez sprawdzenia po imporcie osi montażu, interpolacji oraz prześwitów pod obciążeniem/animacją w Unity.

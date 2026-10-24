# M03-002 — Telescopic structural beam (1×2×1 m)

Source: MODEL-03 exclusive queue. Rear frame mount at Blender local (0,0,0); beam longitudinal axis +Y, Blender +Z up. Retracted envelope X=1m, Y=2m, Z=1m. Front frame mount (0,2,0) when retracted; extension +0.55m along local +Y, with minimum 0.29m sliding overlap. Six defined materials; the revised geometry uses five: graphite, titanium, ceramic, safety-yellow and rubber (the new liner material). Yellow calibration stripes are attached to the sliding component. This is a visual asset, not a new vehicle physics mechanic.

## Files

- `generate_m03_002.py`: reproducible Python generator, stdlib static OBJ/MTL and bpy animated .blend/GLB.
- `M03-002.obj` and `M03-002.mtl`: static retracted geometry produced from the same declarative component specification; editable Blender generator is the animation source.
- `M03-002.static-qa.json`: conservative 21-position sliding-vs-fixed AABB checks; no Blender/Unity runtime verification.

## Generate / validate

```bash
python generate_m03_002.py --self-test --output-dir /tmp/m03-002
blender --background --python generate_m03_002.py -- --output-dir /tmp/m03-002 --require-blender
```

In Blender, use `M03-002_MOUNT_REAR` as rear port and `M03-002_MOUNT_FRONT` as moving front port. Slider `M03-002_SLIDER` animates Y=0 to Y=0.55m (frames 1–30), then returns by frame 60. The two tubes are deliberately nested with 0.034m minimum lateral clearance from the external guide strips to the sleeve interior; the rear connector does not touch the sliding assembly. Bevel modifier is applied to render geometry. Static OBJ is box-faced and not beveled; for game-ready visual evaluation generate Blender GLB.

## QA status

- Static component dimensions, overall retracted 1×2×1 envelope, 21 sampled extension positions, conservative AABB moving-vs-fixed collision checks: PASS (see JSON).
- Python generator syntax/runtime, deterministic regeneration of OBJ/MTL and 21-step clearance validation: **PASS**, GitHub Actions [Model Factory M03-002 Static Geometry QA](https://github.com/C8LyNn/RoboRougelike/actions/runs/40091466179).
- CI-01 Repository Validation: **PASS** ([run](https://github.com/C8LyNn/RoboRougelike/actions/runs/40091466119)).
- Unity EditMode and PlayMode GitHub Actions: **BLOCKED_UNITY_ACTIVATION** (credential check failed before Unity started; [EditMode](https://github.com/C8LyNn/RoboRougelike/actions/runs/40091466188), [PlayMode](https://github.com/C8LyNn/RoboRougelike/actions/runs/40091466128)).
- Blender geometry/animation/GLB export, Unity import, EditMode/PlayMode and visual QA: NOT RUN.
- Unity GLB importer axis conversion should be checked: Blender Z-up, +Y longitudinal. No gameplay code, scenes, or ProjectSettings were modified.

## Iteracja V2 — prowadzenie teleskopu i eksport tylko własnego modelu (24.10.2026)

Oryginalny model zachowywał nominalnie około 34 mm prześwitu pomiędzy zewnętrzną prowadnicą ruchomej części a nieruchomym rękawem, ale nie zawierał reprezentacji fizycznych wkładek prowadzących. Dodano **8 bloków ślizgowych** `Sleeve_WearLiner_*` (po cztery na stronę; po dwa poziomy Z na dwóch stacjach Y). Wkładki wykonano jako oddzielne zamknięte bryły wizualne w materiale `rubber` o wymiarach 0,026 × 0,30 × 0,060 m.

Ich powierzchnia wewnętrzna znajduje się przy |X| = **0,312 m**, podczas gdy zewnętrzna krawędź ruchomej listwy prowadzącej kończy się przy |X| = **0,299 m**. Powstaje nominalny **13 mm prześwit** na całej długości skoku 550 mm. Zewnętrzne 3 mm każdej wkładki są świadomie osadzone w nieruchomej ściance rękawa (zakotwienie). Osiem wkładek ma grupę `sleeve`, nie poruszają się podczas wysuwania.

Generator `generate_m03_002.py` został zaktualizowany, a statyczny `M03-002.obj` zregenerowany bez potrzeby Blendera: **77 osobnych brył, 616 wierzchołków, 462 quady i 5 używanych materiałów**. Istniejący `M03-002.mtl` już zawierał materiał `rubber`, więc nie wymagał zmiany.

**Poprawka eksportu:** poprzednia funkcja `export_scene.gltf()` obejmowała domyślnie całą otwartą scenę, co mogło omyłkowo włączyć cudze obiekty lub niepowiązane assety. Teraz exporter selekcjonuje wyłącznie własną kolekcję `M03-002_ASSET` i używa `use_selection=True`; po eksporcie przywraca dotychczasową selekcję i aktywny obiekt. Plik .blend zapisuje nadal aktualną scenę Blendera — gdy zależy Ci na wyizolowanym .blend, użyj `--factory-startup`.

**Potwierdzone QA:** [GitHub Actions #40099230544 — PASS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40099230544). CI uruchomił generator źródłowy i walidator, porównał wygenerowany OBJ/MTL z plikami repozytorium, zweryfikował osiem wkładek, **30 240 porównań par ruchomych i nieruchomych części w 21 próbkach wysunięcia**, minimalny prześwit 13 mm oraz statyczne ograniczenie zakresu eksportu. To test konserwatywnych prostokątnych obwiedni AABB, nie symulacja fizyki.

**Nadal NOT RUN:** wykonanie `bpy` w Blenderze, wyeksportowanie faktycznie animowanego GLB, kontrola wizualna przy otwartych klapach oraz import i PlayMode Unity 6. Model służy wyłącznie do wizualizacji i nie modyfikuje żadnego istniejącego systemu budowy, mechaniki ani scen gry. Zmiany powinny zostać zrewidowane i włączone do macierzystej gałęzi PR #44 przed ewentualną integracją z `develop`.

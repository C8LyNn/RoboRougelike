# M05-001 — Radar obrotowy talerzowy

**Agent:** MODEL-05 | **Queue ID:** M05-001 | **Type:** 3D visual module, no gameplay changes

**Claim:** [M05-001.json](https://github.com/C8LyNn/RoboRougelike/blob/feature/rrl-model-factory-queue-v1/Docs/ModelFactory/claims/M05-001.json)
**Claim commit:** `b2fd7b5d29f52d745d5d6ca46a2b8ee5eda5c342`

## Delivered files

- `generate.py`: authoritative Blender bpy procedural generator, rig hierarchy, materials, azimuth/elevation animation, validation checks, FBX and BLEND export.
- `M05-001.obj`: actual generated **static** neutral-pose geometry (3208 vertices, 2696 faces, 54 OBJ objects), created without Blender. This is an approximation of the same design; not a Blender export.
- `M05-001.mtl`: five material definitions for OBJ (graphite, alloy, dark reflector, copper, teal status light).

The OBJ is usable as a static mesh. **OBJ does not contain animations**; generate the animated FBX using Blender.

## Geometry and mechanical design

- Mount plane at world origin `(0, 0, 0)`; plate approx. `0.78 × 0.65 m`.
- Local Blender axes: **Z up**, dish forward **-Y**, yaw about **Z**, tilt about local **X**.
- Reflector diameter `1.32 m`, 12 alternating reflective sectors, reinforced rim, 12 rear spokes, three feed support rods, illuminated receiver.
- Separate yaw gimbal and ±12° elevation pivot. All dish parts are children of the tilt pivot; yaw assembly is a child of the base mount. The whole dish rotates as one rigid structure.
- Neutral-pose OBJ geometry is placed at the same origin and approximate dimensions as the Blender generator.
- The module is visual-only. Existing scanner/sensor game logic is **not** modified or duplicated.

## Blender / Unity

Blender 4.x+ (version not verified by execution):

```sh
blender --background --python generate.py
```

Expected output in the same folder: `M05-001.blend`, `M05-001.fbx`.
Blender FBX export settings: forward `-Z`, up `Y`, animated empty pivots, frames 1–121 at 30 fps.

For immediate **static** use, place `M05-001.obj` and `M05-001.mtl` together in the desired Unity `Assets/` subfolder, let Unity import, assign URP/Lit materials if needed, then create a prefab with a mount at its origin. If the MTL emission is not imported, set the teal material's Emission explicitly in URP/Lit. For moving yaw/elevation, use the Blender-generated FBX and inspect imported animation curves.

No file in `Assets/` was modified by this PR; importing and integrating into the live vehicle prefab is intentionally separate work.

## QA and limitations

**PASS (static, executed in JavaScript):** OBJ mesh face-index bounds, finite vertex coordinates, nondegenerate first triangles, counts: 3208 vertices / 2696 faces / 54 parts / 5 materials. The material file exists in the same folder.

**PASS (source inspection):** Blender generator defines six procedural materials, separate yaw/tilt pivots, keyframes, mesh validation, and FBX export.

**NOT RUN:** Blender Python syntax compilation, Blender scene build, animated collision sweep, actual FBX export, Unity import, URP material inspection, and gameplay test. Blender/Unity executables were unavailable in this environment. The generated OBJ has not been visually inspected in a 3D viewer.

**Integration gate:** Hold merge until Blender runs successfully, FBX is imported into Unity, yaw/tilt clips are verified without penetration, and a reviewer accepts the visual silhouette and scale.

## Poprawka animacji i ponownego generowania — 2026-10-24

Oryginalny generator ustawiał azymut radaru wyłącznie między 0° a 360°. Po konwersji do kwaternionów te same pozycje mogą oznaczać brak obrotu zamiast pełnego obrotu. Poprawka dodaje **13 klatek kluczowych** co 10 ramek (30°) pomiędzy klatkami 1–121. Ruch pochylenia ±12° pozostaje bez zmian.

Poprawiono też `reset()`: wcześniej usuwano obiekty sceny, ale pozostawiano nieużywane dane siatek i `Action`, które mogły narastać przy wielokrotnym generowaniu. Teraz czyszczone są wyłącznie nieużywane zasoby z prefiksem tego modelu. Do walidatora dodano weryfikację łańcucha pivotów `ROOT→YAW→ELEVATION`.

**Status:** poprawka kodu i audyt statyczny; rzeczywiste wykonanie w Blenderze, eksport FBX i import Unity pozostają NOT RUN. Zmiany należy ocenić i włączyć do macierzystej gałęzi PR #38 przed integracją do develop.

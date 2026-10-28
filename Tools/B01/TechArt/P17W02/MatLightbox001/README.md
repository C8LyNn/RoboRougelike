# B01-30 PASS17 WAVE02 — MAT-LIGHTBOX-001 R1

**New working technical-art preview, not another material generator nor 3D MODEL_JOB.** This standalone site actually displays the *original eight unchanged* MAT-A1-001 PBR PNG assets from [producer Draft #336](https://github.com/C8LyNn/RoboRougelike/pull/336) on an interactive WebGL2 sphere. It supports rubber/alloy switching, directional light angle, normal intensity, UV repeats, exposure, orbit dragging, and reset. Source images are copied byte-identically and SHA256 checked.

This is a research shaderball, not Unity 6000.3.14f1 URP Lit or game footage. Shader math is an independent approximation for lighting and packed original metallic/smoothness/AO; visual parity, actual robot mesh UV and final A1 color decisions remain NOT_RUN/NOT_APPROVED. It creates no new robot/module mesh and does not touch Codex, B01-31, M04, CHL or PARTSIZE sources.

## Actual original source dependency

MAT-A1-001 producer immutable original #336 commit `171983f85ff41df6ef33b95939cc877ba68e03ca`, generator Git blob `171d53bc7d5a4840a067af5c934a8fd1c7ad774e`. The workflow fetches these originals, refuses different SHA, executes the actual generator into disposable build storage, then copies exactly 8 PNGs. The viewer NEVER substitutes stock textures or gradients when a file is missing: it reports a blocking error.

## Build and operate

In a disposable clone of RoboRougelike, retrieve exact producer source #336 commit using `git fetch origin refs/pull/336/head`, extract `Tools/B01/TechArt/P17/materials/generate_materials.py`, and run:

```bash
python3 Tools/B01/TechArt/P17/materials/generate_materials.py --size 512 --out build/B01-30/P17W02-original
python3 Tools/B01/TechArt/P17W02/MatLightbox001/build_site.py --original build/B01-30/P17W02-original --out build/B01-30/P17W02-lightbox/site
cd build/B01-30/P17W02-lightbox/site
python3 -m http.server 8765 --bind 127.0.0.1
```

Open `http://127.0.0.1:8765/index.html` in a **WebGL2-capable** browser. Local HTTP avoids insecure `file://` texture restrictions. You can also download the generated `MAT-LIGHTBOX-001-website.zip` and serve its included `site/` directory. No external library, CDN or network calls are made by the viewer.

## Real QA, evidence, and exact receipt

Own scoped GitHub Actions workflow `.github/workflows/b01-30-pass17w02-mat-lightbox.yml` runs original source generator, builds real HTML+JS+8-PNG site, launches actual Chromium with WebGL2 SwiftShader, and checks on three viewports 1280×720, 1920×1080, 720×900: nonblank original 3D image, original two material profiles render differently, light controls change pixels, UV repeat changes pixels, pointer orbit and reset work, all 8 PNGs are loaded, 0 browser JS exceptions and page does not overflow. It saves twelve physical screenshot PNGs. A separate corrupted-input negative removes an original normal-map PNG; the renderer must refuse startup until the byte-identical file is restored. Output includes a deterministic source identity and screenshot JSON with SHA256.

**No native Unity import, URP Shader Graph/lighting parity, vehicle mesh UV, physical properties, independent B01-60, or gameplay.** B01-40 inspects exact PNG and source receipt; B01-60 performs independent browser/visual QA, B01-00 schedules optional Codex consideration. No OFFERED/INTEGRATED state until accepted.

## Safe rollback

Only delete the generated `build/B01-30/P17W02-lightbox/` folder; this own Draft PR consists of independent `Tools/B01/TechArt/P17W02/MatLightbox001/**` sources and one own workflow. No game `Assets/`, previously released producers or Codex run files are changed.

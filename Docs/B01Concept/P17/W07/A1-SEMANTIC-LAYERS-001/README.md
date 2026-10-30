# B01-31 — A1-SEMANTIC-LAYERS-001 / PASS17 W07

**Owner:** B01-31 Concept Art. **Purpose:** real exported 2D part-specific art from immutable original SVGs, not an approved 3D part.

## Different from previous #385

Previous #385 contains a whole A1 module silhouette used for UI selection and external focus outlines. This package preserves four *original colored semantic SVG groups* individually. Its 24 new transparent PNGs contain only the mount, support arm, damper or wheel from each of three perspectives at 128 or 256 pixels. Each sprite shares the *original full image canvas*, so compositing in painter's order visually reconstructs the full original SVG. Unlike a physical exploded view, this does not imply that a visible surface was mechanically isolated or that a wheel and support arm are independent game objects.

**Physical deliverable:** 3 views x 4 groups x 2 resolutions = 24 actual RGBA part PNGs; three 256px original source-assembly PNGs; one labeled 12-panel contact-sheet PNG, and receipt.json with exact SHA-256/size/pixel checks and source commit. In total: 28 PNG assets.

## Exact provenance and real execution

Only original SVG images from B01-31 #377 at commit 759c3471aab998b8ae1f153a9d4921401dddc749 are used; original face, side and top Git blobs are in manifest.json. Original sources are fetched by Git SHA and checked *before* rendering. No copies of upstream sources are changed.

Run in an isolated branch checkout (not Codex's worktree):

    git fetch --no-tags --depth=1 origin refs/heads/art/b01-31-p17-w02-a1-view-icons-001
    git rev-parse FETCH_HEAD
    # result must be 759c3471aab998b8ae1f153a9d4921401dddc749
    python -m pip install 'CairoSVG==2.8.2' 'Pillow==11.3.0'
    python Docs/B01Concept/P17/W07/A1-SEMANTIC-LAYERS-001/build_semantic_layers.py

CI runs generation twice and verifies identical receipt SHA256 for repeatability. The script performs original Git blob/semantic checks, real raster PNG roundtrip checks, size, alpha, transparent background, all 28 expected distinct filenames, all saved SHA256 values, and original-vs-layered 2D alpha coverage >= 98.5% at each of 2 sizes x 3 views. Negative controls: absent wheel group, duplicated arm group, opaque background, altered Git source and altered output bytes. On error/failed check, old receipt and partial art are removed (fail closed). No producer-only test is portrayed as independent QA.

## Scope, QA, restrictions, rollback

Research-only A1 subject under #287, G_DESIGN=PENDING, MODEL_JOB=NOT_AUTHORIZED. Does NOT create a new module or a Unity sprite/PBR/3D prefab, physical collider, socket, mechanical projection, gameplay mechanic, approved UI placement or Codex integration.

B01-40: exact Git source/manifest, scoped branch, reversible physical archive. B01-60: independently decode and inspect all PNG alpha/components/registration, examine contact sheet, verify per-file SHA and any actual occlusion; B01-12 only optional UI relevance review. B01-00 decides offering via #79 after QA. Native Unity, Player, PlayMode, human accessibility and physical testing: NOT_RUN. Codex alone can integrate an authorized package; NO merge.

Rollback: delete only this folder and its dedicated workflow in disposable checkout; leave #377, #385, original game Assets, Codex and CHL/PARTSIZE untouched.

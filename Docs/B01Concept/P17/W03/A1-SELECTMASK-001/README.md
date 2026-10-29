# B01-31 / A1-SELECTMASK-001 — original 2D selection-mask and focus-sprite export

**Producer:** B01-31 CONCEPT ART · **Version:** P17-W03-R2.1 · **Status:** ART TOOL / SOURCE-ONLY.

This tool turns **actual, immutable SVG art from [PR #377](https://github.com/C8LyNn/RoboRougelike/pull/377)** into usable pixel-perfect *UI* selection-mask PNGs and separate, transparent selection-focus border PNGs. It does **not** create a physical vehicle collider or install a Unity prefab. This is a **new original export function** with concrete output assets, not another wheel concept, pose, palette, or untested report.

## Strict upstream contract

- Input PR #377 exact commit: `759c3471aab998b8ae1f153a9d4921401dddc749`.
- Original face/side/top SVG blobs must be `3b18e0b2e02c550bdd667fe16f9c8501b88c4520`, `9037da9ae1a44776475437b1c968a74ea84bbf9a`, `8adb61ccdc4bd1a4dc962579184a83a7a8564236`.
- This PR references the **existing source Git objects** via `git show exactSHA:path`. It does **not copy, fork, edit, or claim ownership of PR #377**. Availability of its commit is an explicit dependency.

## Real export and tests

In a disposable checkout of this branch at an immutable PR commit:

```sh
git fetch --no-tags --depth=1 origin refs/heads/art/b01-31-p17-w02-a1-view-icons-001
git rev-parse FETCH_HEAD
# Must equal 759c3471aab998b8ae1f153a9d4921401dddc749
python -m pip install 'CairoSVG==2.8.2' 'Pillow==11.3.0'
python Docs/B01Concept/P17/W03/A1-SELECTMASK-001/build_and_test.py
```

**Generated real assets:** 3 views × 3 pixel sizes (48, 96, 192) × 2 PNG types (alpha UI hit area and non-overlapping turquoise focus border) = **18 individual transparent sprites**, plus three 96px composited focus previews and one combined inspection contact PNG, **22 PNGs total**, and a JSON receipt. A hit-mask pixel is white and alpha 255 if original art alpha is at least 32/255, otherwise fully transparent. Focus outlines derive from morphological dilation minus the exact mask (2/3/4px radius across scales). PNG SHA256, CRC32 and pixel dimensions are recorded from actual bytes. R2 removes previous generated `A1-*.png` files and any earlier `receipt.json` at the start of each run; an unsuccessful run removes partial A1 PNGs and writes no receipt. A separate negative regression verifies that an old PASS receipt and obsolete A1 art cannot survive a rerun while unrelated files are preserved. An exact inventory check rejects extra/stale PNG files. CI uploads generated art only on successful export. R2.1 also corrects a prior vacuous focus-clipping check: the receipt records `focus_touches_canvas_edge` for each focus PNG and a `focus_edge_touch_count` in the summary, with synthetic edge and non-edge controls. An edge warning is NOT silently treated as proof of an unclipped ring; UI padding and final placement require independent QA / Codex review.

**Acceptance:** true original source Git blob SHA, original four semantic groups, nonempty masked silhouettes, transparent backgrounds, hit/miss query semantics, non-overlapping focus border, distinct views, 22 real PNGs, PNG signature/hashes, and strong negative controls detecting full-opaque, all-empty, bad upstream SHA, lost part semantics, and a one-pixel alpha mutation. PNGs are source G0 tests; neither human accessibility nor real Unity UI behavior is claimed.

## Codex receiver, limitations and rollback

1. **B01-40** checks immutable SHA and the actual upstream dependency on #377, file scope, rollback and emitted PNG receipt.
2. **B01-60** independently replays the renderer and tests masked hit/corner/non-overlap on the real PNG bytes.
3. **B01-12** may review focus presentation in the current garage UX prototype, but only in its *own* authorized scope. This work does **not modify B01-12's code**, and there is **no proof of an in-game accessibility improvement**.
4. **B01-00** decides whether independently accepted assets enter CODEX INBOX #79. **Codex is sole canonical Unity integrator**. No `Assets/**` modification, no G1/G2, no `OFFERED`, no `CODEX_ACK`.
5. **Rollback:** remove only `Docs/B01Concept/P17/W03/A1-SELECTMASK-001/**` and `.github/workflows/b01-31-p17-w03-selectmask.yml` from a disposable checkout. Other PRs unchanged.

**Gates:** HQ [#287](https://github.com/C8LyNn/RoboRougelike/issues/287#issuecomment-6463914298) permits research-only A1 single part. This is a 2D visual selection aid, **not** an approved physical socket, wheel pivot, collision bounds, mass, PartSize claim, MODEL_JOB or gameplay policy. Unity native, PhysX, Windows Player and actual human UI playtest **NOT_RUN**.

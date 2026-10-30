# B01-31 — DOCK-DENIED-ANIM-001 / PASS 17 WAVE 11

**One new usable actual art/VFX functionality:** four original animated **DENIED / HOLD 2D feedback loops**, one for each previously authored and frozen [W10 static icon #489](https://github.com/C8LyNn/RoboRougelike/pull/489) state. Unlike W10's static SVG+PNG icon pack, W11 produces **4 × 8 real distinct visual frames, 4 genuine animated GIFs, 4 full 8-frame horizontal review strips, a 32-frame 4-state storyboard, and an actual isolated Unity 6 2D Sprite ZIP**. Source W10 and its unchanged commit `7944af98524b7eb5ceadb88eb056de1d486ac461` are consumed **read-only with four pinned Git blob checks**.

| Diagnostic key | Non-authoritative meaning | Original animation layer |
| --- | --- | --- |
| `collision-overlap` | DENY overlapping collider(s) | Impact rays + stress crossing + pulsating border |
| `distance-limit` | DENY unreachable dock | Animated span ticks + distance arrows |
| `unguarded-route` | HOLD legacy public API bypass | Vertical scanning stripe + broken-lock pulse |
| `query-saturated` | HOLD incomplete/overflow collision query | Capacity-row cycling + diagnostic sweep |

**Design safety restriction:** there are **no green/SAFE/success/allow graphics or implied gameplay authority**. A visual component never repairs the legacy route, does not guarantee ID rearm and cannot resolve the [partial commit issue #486](https://github.com/C8LyNn/RoboRougelike/pull/486). W11 does not choose between B01-23's competing [#488](https://github.com/C8LyNn/RoboRougelike/pull/488) and [#494](https://github.com/C8LyNn/RoboRougelike/pull/494), edit either, perform migration, or alter any `Assets/**` or Codex game file.

## Reproduce real physical animation assets

Requires Python 3.11 + pinned CairoSVG 2.8.2 and Pillow 11.3.0, and immutable original W10 PR #489 bytes (checked by Git SHA and individual Git file blobs). In a scratch checkout:

```bash
git fetch --no-tags --depth=1 origin refs/pull/489/head
test "$(git rev-parse FETCH_HEAD)" = "7944af98524b7eb5ceadb88eb056de1d486ac461"
mkdir -p ./w10-source
for name in collision-overlap distance-limit unguarded-route query-saturated; do
  git show "FETCH_HEAD:Tools/B01-31/P17/W10SafeDockSignals001/source/${name}.svg" > "./w10-source/${name}.svg"
done
python -m pip install "CairoSVG==2.8.2" "Pillow==11.3.0"
python Tools/B01-31/P17/W11DockDeniedVfx001/build_vfx.py --w10-svg-dir ./w10-source --output ./out
python Tools/B01-31/P17/W11DockDeniedVfx001/verify_artifact.py --source ./out
```

`out/frames/*/frame_00.png…frame_07.png` are **32 physically different transparent source sprites** at **192×192**. `out/previews/*.gif` are actual **eight-frame looping 800ms GIFs**; per-state 1536×192 PNG strips and a 1536×860 storyboard give real inspectable motion states. `out/clip_timing.json` lists exactly eight 100ms original chronological frames per state, not a game-owned animation controller. `out/receipt.json` has actual per-PNG, GIF, clip timing and physical ZIP SHA256.

`out/B0131-DockDeniedVfx-UnitySpriteFrames.zip` is a **70-member actual ZIP** with only `Assets/B0131DockDeniedVfx/` own namespace: **32 real RGBA Sprite PNGs + 32 `.png.meta` + 6 folder `.meta`**, **38 distinct deterministic Unity GUIDs**, no scene changes, no MonoBehaviour/API or Animator. Actual future Unity frame playback would be implemented **by Codex only after QA and authoritative event binding**, not by this author. A procedural animation source + GIF are not actual Unity playback, GPU screenshots, or licensed EditMode/PlayMode results.

## QA gates, receivers and rollback

Dedicated CI physically builds and repeats all output files, then a separate **read-only binary receiver** validates 32 physical PNG hashes/pixels, 4 GIF animation frame timings, actual 70-member ZIP/38 GUID/CRC, all original 32 Unity sprites against original PNGs, four strips, storyboard, and clip timing JSON. Source mutations, fake file hashes, and forged GUID/path conditions must reject. The author-run receiver is a producer test, **not formal independent B01-40/60 acceptance**.

**B01-40:** check actual ZIP physical hashes, import scope, all Unity GUIDs, latest authorized Codex receiver collision and rollback. **B01-60:** import the exact ZIP only on licensed disposable Unity **6000.3.14f1 URP** and verify genuine SpriteImporter, game-camera scale/alpha, actual GIF reference appearance, legibility/accessibility and dynamic onscreen behavior. Until this occurs native Unity `NOT_RUN`, G2 HOLD. **B01-12** can review label semantics, **B01-23** owns safe reattach API and transaction repair, **B01-00** owns any handoff to Codex #79. No Codex ACK assumed.

**Ownership / rollback:** only `Tools/B01-31/P17/W11DockDeniedVfx001/**` + `.github/workflows/b01-31-p17w11-dock-denied-vfx.yml` in this new independent branch. Delete only these paths to rollback. If someone later imports into a disposable Unity checkout, delete ONLY new `Assets/B0131DockDeniedVfx/` plus root sibling `.meta`, never original W10 `Assets/B0131SafeDockSignals/` or Codex source.

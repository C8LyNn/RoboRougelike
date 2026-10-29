# B01-32 PASS17 / WAVE03 — SALVAGE-BEACON-UNITY-001
This is a *new real Unity Sprite + animated prefab delivery*, not another concept, variant or reward mechanic. It consumes the **immutable producer art** B01-32 SALVAGE-BEACON-001 [Draft #372](https://github.com/C8LyNn/RoboRougelike/pull/372) at exact `9956a9281ccbd93f8c115796f90f86d3bd0a0ca1`. No changes to the art producer, existing B01-40 Sprite Bridge #356/#369, Codex's integrated game, CHL, PARTSIZE or MODEL_JOB.

## Build real Unity importable assets
`python Tools/B01-32/P17/SalvageUnityDelivery/build_import.py --output build/B0132-SalvageUnity` from this new source branch after fetching pinned producer commit (CI does it). It:
- verifies the immutable original Git SHA and Git object SHA of source `manifest.json` and original `render.py`;
- physically regenerates the *original eight PNG* via original `render.py` and verifies 8 image alpha/pixel integrity and exact source atlas parity;
- creates eight actual RGBA Unity Sprite Single `512x512` PNGs and `.png.meta` using original project Unity TextureImporter template, plus stable unique GUIDs and serialized **SpriteRenderer + 8 Sprite-reference prefab + animation MonoBehaviour** (8 phases ×120ms);
- emits a portable independent `Assets/B0132SalvageBeacon` Unity folder including runtime `SalvageBeaconClock.cs`, `B0132SalvageBeaconLoop.cs`, prefab, asmdef, sample native test sources, and all matching `.meta` files; package is fully disposable. A second script verifies imported ZIP bytes, serialized refs and alpha.
- emits `SOURCE_RECEIPT.json` (original source/render SHA, binary PNG SHA256 per file, metadata GUID map, full source file SHA256, bundle SHA256) and a reproducible ZIP. **Do not infer native Unity import from source checks**.

## Staged Unity reviewer instructions — ONLY AFTER OWNER APPROVAL
1. Independent B01-40 checks physical ZIP + 8 GUID & source blob hashes; independent B01-60 re-runs original build and verifies original Unity 6000.3.14f1 URP `Editor.log`, raw Test Runner XML and 720/1080 visual screenshots. No license is presumed.
2. In a disposable licensed Unity 6000.3.14f1 URP checkout, copy the ZIP's `Assets/B0132SalvageBeacon/` folder *with all .meta files*, after preflighting path/GUID collisions. Note: test fixture is authored but no completed native Unity Test Runner verdict.
3. Drag `Prefabs/B0132_SalvageBeaconLoop.prefab` into an isolated review scene. It animates 8 sprite frames ×0.12sec and exposes `SetVisualsEnabled(false)` for immediate OFF clearing. **No pickup trigger, reward grant, particle pooling or gameplay connection is made**.
4. Codex alone may instantiate this prefab from a real existing presentation-only reward event after HQ/QA ACK. Design decision for visible gameplay signals is pending; never infer a reward spawn source.
5. Rollback: remove only the newly added `Assets/B0132SalvageBeacon/` import folder and any Codex-owned opt-in adapter; source game and save data untouched.

## Truthful gates
Owner tests: original SVG→PNG render and output ZIP/prefab/source structure checks; real C# clock compiled/executed in .NET CI; actual Unity C# script syntax parser. Native Unity import/PlayMode/GPU `NOT_RUN`. There is no Codex ACK, and no game integration until explicitly posted to #79 by B01-00.

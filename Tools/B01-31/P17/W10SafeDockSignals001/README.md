# B01-31 SAFE-DOCK-SIGNAL-ART-001 — PASS17 / WAVE10

**Original semantic visual assets for four specific failed/blocked reattachment conditions**, based on [#472](https://github.com/C8LyNn/RoboRougelike/pull/472) and [#476](https://github.com/C8LyNn/RoboRougelike/pull/476), with the original [B01-23 #471](https://github.com/C8LyNn/RoboRougelike/pull/471) demo-only hardening as context. **No success/allowed icon is shipped** because a status graphic cannot secure the legacy public `Reattach` API. This design is visual research only and makes no claim to fix the unguarded public call path or create a functional UI.

## Concrete original deliverables

Four original vector icons in `source/*.svg` (actual author-owned source graphics) with different outer silhouettes and icon symbols, unique titles, no external images or font dependencies:
- `collision-overlap`: intersecting solid modules with explicit X — confirmed physical penetration → DENIED.
- `distance-limit`: separated dock halves, dashed reach and arrows → DENIED.
- `unguarded-route`: broken lock/shield with slash → API not authenticated/gated → HOLD.
- `query-saturated`: multi-row capacity bars and warning → incomplete/saturated collision query → HOLD.

The actual source generator renders **4×3=12 original RGBA 96/192/384px PNG sprite images**, two large `SAFE_DOCK_SIGNALS_DARK.png` and `SAFE_DOCK_SIGNALS_LIGHT.png` comparison boards, and a real ZIP `B0131-SafeDockSignals-UnityImport.zip` containing **4 full-resolution 384px original transparent PNGs** under `Assets/B0131SafeDockSignals/Icons/` plus **6 original stable `.meta` GUID importer entries** (10 real members). Nothing is committed into the canonical game `Assets/**` tree or installed in Unity by the producer. The original vectors are included as separate source files; no copyrighted external material is used.

## How to reproduce original art

On isolated checked-out branch, Python 3.11:

```sh
python -m pip install CairoSVG==2.8.2 Pillow==11.3.0
python Tools/B01-31/P17/W10SafeDockSignals001/build_signals.py --output build/B01-31/P17W10/SafeDockSignal001
```

Dedicated [GitHub Actions workflow](../../../../.github/workflows/b01-31-p17w10-safe-dock-diagnostics.yml) runs this generator twice, compares real output PNG/ZIP/receipt bytes SHA256, and uploads the actual generated art and machine receipt (not just test-source claims). Source validator rejects untrusted/external SVG references; raster validation decodes real PNG pixel alpha/silhouette and tests dark/light contrast. ZIP readback verifies exact 10 entries, PNG importer metadata, 6 unique GUID, actual bytes, ZIP CRC and synthetic malicious variants (script/external image/missing SVG title, GUID change, ZIP tamper). PNG images use stored output to minimize cross-platform binary drift, but true cross-OS/native Unity acceptance requires separate execution evidence.

## Safety and receiver restrictions

These are **diagnostic art concepts**, not actual safety decisions. They may be rendered only after receiving authoritative status from a verified, comprehensive backend gate. In particular, showing a reassuring graphic must never mask the outstanding [#472](https://github.com/C8LyNn/RoboRougelike/pull/472)/[#476](https://github.com/C8LyNn/RoboRougelike/pull/476) legacy public API bypass. `collision-overlap` and `distance-limit` describe denial conditions; `unguarded-route` and `query-saturated` are explicit HOLD/fail-closed warnings. Current demo cannot claim secure integration.

**B01-23** retains exclusive physical API ownership. **B01-40** independently verifies ZIP, GUID/asmdef namespace and rollback/collisions with actual Codex source; **B01-60** performs real licensed Unity 6000.3.14f1 URP Sprite importer/UI rendering and usability review; **B01-10/12** review meanings only, not authorization. **B01-00** decides Codex handoff after source/native gates. Producer does **NOT** edit Unity scenes, demo, gameplay API, Codex, B01-23, B01-32, #471 or original frozen sources. Native Unity G1 and human usability `NOT_RUN`; G2 HOLD; Codex not contacted/ACK.

**Rollback:** remove only this isolated `Tools/B01-31/P17/W10SafeDockSignals001` and its dedicated workflow. If a future authorized disposable Unity import exists, delete only `Assets/B0131SafeDockSignals/` plus root folder sibling `.meta`, with no shared-game modification or changes to original gameplay.

# B01-12 / P17 — GARAGE-REPAIR-TRIAGE-002, R1

**One working, isolated UX research demonstrator.** Never a production game save, inventory mutation, release candidate, Unity scene or integrated Codex implementation. The previous B01-12 `GARAGE-BLUEPRINT-001` PR #339 handles side-by-side configuration comparison and strict blueprint JSON. This new package instead models a *single post-combat multi-part repair transaction* with wallet and selected-drive readiness.

## Run

Open `garage-repair-triage.html` in a browser (no backend or external assets). Use the scenario selector and selected-drive control, choose individual repair checkboxes or `Napraw blokady` (deterministic least-cost subset), inspect the total and after-repair readiness, and **explicitly** click `Zatwierdź naprawy`. The demonstration starts at 32 scrap and dual critical damage: A gun 0 HP and both wheels 0 HP. Suggested operation: gun 11, wheel L 6, wheel R 6, total 23 scrap; wallet 32→9 on acceptance. Undo and redo restore *both* HP and scrap. `Wszystkie uszkodzenia` may exceed budget; in that case commit disables and the internal guard rejects even a programmatic click. `Eksportuj plan`/`Wczytaj plan` handles only a strictly versioned selection, never the game state or wallet.

### Actual browser tests

Requires Python 3 + Playwright Python package and Chromium at `/usr/bin/chromium`.

```sh
cd Research/B01-12/P17Repair
python3 replay.py
```

The original committed replay script reads the original HTML bytes, checks the manifest SHA256 **before any browser test**, then executes four genuine Chromium scenarios at 320×720, 768×1024, 1280×720 and 1920×1080. Check outcomes include visible dual critical warnings in Chromium AX, minimal subset quote, strict per-part calculations, atomic insufficient-funds rejection, wheelchair/track/hover readiness, clean import + malformed/duplicate/unknown/extra field rejection, transaction undo/redo, keyboard focus and no horizontal page overflow. The `qa-results.json` and PNGs are producer **local evidence**, not github workflow or native Unity evidence. If source bytes change, update manifest/test evidence only with a new version and rerun.

### Ownership, import, rollback

- `PRODUCER=B01-12`; only additive `Research/B01-12/P17Repair/` paths; no `Assets/**`, CHL, PARTSIZE, Codex game source, workflows or global CI changed. Source based on current Codex original R3 economics and readiness but **not** a true implementation: current prices from `R3GarageEconomy` (`WheelCost=15`, `GunCost=30`, `CrawlerTrack=45`, `Hover=35`, `Omni=40`), illustrative `ceil((100-HP)/100*price*0.35)` full repair. `Core=5` is a simplified placeholder. This instrument omits structure connectivity/weight, persistent IDs, inventory ownership, cooldown, discounts, actual purchase and score. Zero gameplay money is moved.
- **B01-40** check exact four-file scope, manifest, source provenance and rollback. **B01-60** independently run original committed Chromium tests and security negatives on the PR's exact SHA; report failing checks as RED. **B01-00** sets queue order; Codex alone decides whether to port UX into its own canonical garage on a fresh source SHA.
- Codex optional acceptance when ported: dual no-gun/no-mobility warnings with ordinary tips OFF, no charge for preview, atomic reject when funds short, 0HP not automatically full via detach/reattach, wallet+HP consistent across Undo/Cancel, real 720p/1080p Windows Player + native economy and save regression. The existing **CV-03** covers garage live input/functional readiness; this research package is **not** a new engine-test completion or another CV ID.
- Rollback: close the Draft PR or discard `Research/B01-12/P17Repair/` in a disposable branch. No runtime dependencies, game migration or cleanup required. Do not merge to main/develop without HQ/Codex approval.

### Evidence boundary

`RESEARCH_ONLY / OWNER_BROWSER_PASS / INDEPENDENT_QA_PENDING / UNITY_NOT_RUN / WINDOWS_PLAYER_NOT_RUN / HUMAN_0 / CODEX_ACK_PENDING / G1_NOT_RUN / G2_HOLD`. Old PR #339 is unmodified and its 74/74 native-browser-on-HTML replay was re-executed unchanged before this task.

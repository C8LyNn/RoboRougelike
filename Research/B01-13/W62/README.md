# B01-13 / PASS12 — test instrument/source-grounded fixture correction

Status: research-only; no human playtest, no Unity/native/Player/manual mouse tested.

## Original authoritative inputs
Codex PR #310 HEAD `231005c6d0351945a8ee6684f3856af118f0e94f`.
Original C# Git blobs (fetched at that exact SHA):
- `Assets/MapEditor/R1MapEditor.cs`: `c92e120d9bc3ce26b3eac68cafdfeea4bacefad7`
- `Assets/MapEditor/MapDraft.cs`: `b5f8f395f93972c0ede000701dbbada8fcfcdfdb`
- `Assets/MapEditor/R1MapEditorHUD.cs`: `64105bc77bc35f2a897efbf465919a620dacc30b`
- `Assets/MapEditor/MapWorld.cs`: `8928908db123b35dcab4b324fe9d3a7a8fceee4e`
- `Assets/R3/R3VehicleModel.cs`: `9f1bf190c7535182c6ce375cbfe67f5b2862e811`
- `Assets/MapEditor/MapStorage.cs`: `f31a94c16e8cb244e66e39b0ac344b085095b630`

## Source-grounded protocol changes
- **T03**: functional gun/mobility failures; do not assume an available ordinary-tips OFF toggle in this build. Current HUD presents only first DeployError, a known Codex-owned D03 limitation.
- **T04**: `OpenMapEditor` starts empty (`new MapDraft`): player must place `Spawn`, `Finish` and Ground under Spawn, or enable GlobalFloor. `TestEditedMap` also requires deployable robot (weapon + selected functional drive, intact core/structure). Test-drive return from paused UI via `Esc → Wróć do edytora map`. Moderator checks safe unique map path before save because D02 remains open.
- **T05**: importable JSON v2 `HPT-T05-MAP-v2.json` contains one wall at x0,z0 with GlobalFloor=true. In original MapDraft `Ground(kind)==Ground(a.kind)` overlap rejects placing another Wall in (0,0), while place at (3,0) is predicted valid. **These are source-derived expected outcomes only. The fixture has not been imported in Unity.** Operator must preflight import, visual ghost and both mouse actions and avoid existing save data.

## New score-sheet RED→GREEN
PASS11 originally permitted two sessions with different build/ZIP SHA in the same summary; this is a research data-integrity bug because current `summarize` pools all HUMAN_APPROVED rows. `test_pass12.py` reproduces RED before fix. PASS12 fail-closed guard refuses adding a different build SHA *or* a different ZIP SHA256 to non-empty session memory, preserving existing rows. New build requires explicit export and clear. Mode SYNTHETIC remains default.

## Reproduce
On Python with Playwright installed and an executable `/usr/bin/chromium` (or `chromium`/`chromium-browser` in PATH):

```sh
python test_hpt13.py
python test_pass12_extended.py
```

Current outcomes: old baseline **34/34 PASS**, new browser/fixture assertions **13/13 PASS**, original Codex C# source anchor checks **10/10 static PASS**. RED before fix: session2 with different SHA was added (10 stored rows); GREEN after fix: 5 stored rows. Local Chromium is the **real original B01-13 HTML code under test**, but synthetic form state only. Original C# was fetched and inspected; **Unity C# was not executed**. Historical status `human participants 0`, `native Unity NOT_RUN`, `current Windows Player manual GUI NOT_RUN`, `fixture physical import NOT_RUN`.

## HQ unlock
Use existing signed GATE D checklist: https://github.com/C8LyNn/RoboRougelike/issues/311#issuecomment-6454687028 . Specific HQ reply required: `HUMAN_PLAYTEST: APPROVE | REVISE | HOLD`; assign an adult-participant recruiter, consenting facilitator, independent Windows operator, secure private data location with retention, and verified disposable Player/fixtures. General swarm PASS12 order is not human recruitment authorization. No user desktop control.

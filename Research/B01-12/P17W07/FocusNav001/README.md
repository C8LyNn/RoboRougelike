# B01-12-FOCUSNAV-001 / R1 — deterministic focus/navigation UX

**Owner:** B01-12 Interactive UX Prototypes · **Wave:** B01 PASS17 W07, #433 · **Type:** functional, offline browser UX + DOM-independent JavaScript focus reducer. **Not** Unity assets, runtime HUD, game input adapter, controller hardware, actual garage, Unity UIToolkit or gameplay build.

## Actual working functionality

- Three simulated contexts (Garage / Run HUD / Upgrades), six independently focusable actions in each, stable action identifiers and per-panel focus memory.
- Four-direction spatial navigation. It selects the nearest viable candidate in the directional half-plane (score = forward distance + 3×cross-track distance; deterministic tie-breaking by cross-distance, forward distance and lexical ID). At edges focus remains unchanged.
- Dynamic enable/disable, refusal to disable the last focusable control in any panel, immediate safe fallback and restoring previous remembered focus when feasible.
- Accessible confirmation dialog opens by mouse or Enter/Space; `aria-modal`, real browser `inert` background, trapped Arrow/Tab/Shift+Tab, Escape cancellation, Confirm/Cancel and return-to-origin focus. Repeated dialog creation and background interactions are rejected.
- Deterministic readonly state snapshot for visual UX inspection. Status live region with preserved final confirmation/cancellation message; no backend, localStorage, cookies or network requests.
- A genuine fixed defect found during implementation: programmatic focus was incorrectly re-announcing itself, overwriting confirmation result and altering reset sequence. Fixed by ignoring focus events where the model already holds that focus.

The HTML is a **testing surface** for a working browser mechanism, not an image or a design-only wireframe. It should not be translated into Unity without Codex-owned input/UIToolkit adapter, game test and design review.

## Execute (exact files under this folder)

1. `node test_engine.cjs` — self-contained pure JavaScript reducer tests (Node 18+; no npm install).
2. `python3 test_browser.py` — Playwright and `/usr/bin/chromium`, uses **in-memory HTML** injection of the exact unchanged `focus_engine.js` bytes; this avoids local file / localhost restrictions and still exercises original JS/DOM in a real Chromium process. `pip install playwright` if missing.
3. Negative source-corruption control: make an isolated copy of all script and source files, append one byte to `focusnav.html`, run `python3 test_browser.py` there. Expected `exit 2 / FAIL_CLOSED` **before browser launch**; never alter the immutable published revision.

Original-source positive tests executed locally (producer): **Node reducer 35/35 PASS**; **Chromium real DOM 128/128 PASS**, 0 JavaScript exceptions, across 320×720, 768×1024, 1280×720, 1920×1080; a deliberately damaged original input was rejected with `exit 2`. The full 128 per-case results are in `browser-results.json` (committed as producer evidence). These are NOT independently reproduced B01-60 results and NOT Unity EditMode/PlayMode/Player tests. Screenshot `evidence-1280x720.png` can be generated in the same local folder (not checked in as source); not game footage.

## Interfaces and limitations

`focus_engine.js` is a UMD script: browser global `B01FocusNav`, or Node `require('./focus_engine.js')`. Exported `FocusRouter`, `panels`, `dialog`, `directions`. `FocusRouter` methods: `move(key)`, `switchPanel(name)`, `focusControl(id)`, `setEnabled(id,boolean)`, `openDialog()`, `closeDialog(accepted)`, `activate(id?)`, `snapshot()`, `invariant()`. No Unity APIs, 3D sockets, runtime player control, persistent save, network or game domain modification. 18 sample controls are **fixtures** only and not game-approved behavior. DOM adapter is purpose-built for visual validation. Device controller accessibility, screen reader with human participants, native UI Toolkit and Windows Player are **NOT_RUN**.

## Provenance, acceptance, receiver and rollback

All owned paths are strictly within `Research/B01-12/P17W07/FocusNav001/**`. Nothing in canonical `Assets/**`, previous B01-12 PR #428 or #404, shared workflow or Codex source changed. `manifest.json` pins source SHA256/byte counts; tests fail closed on mismatched original source. **B01-40** independently checks PR exact HEAD, owned paths, immutable Git blobs and manifest/scope/rollback, reports G0. **B01-60** independently executes node/browser original-byte suite and tamper negative on exact HEAD, reports QA; no producer report may substitute independent results. **B01-00** decides whether/when this research-only module is offered in Codex #79; Codex only may implement a native UI adapter and run Unity tests after ACK.

**Intake:** read-only checkout of this Draft PR into an isolated workspace; no install into Unity. **Rollback:** close Draft and discard its `Research/B01-12/P17W07/FocusNav001/` directory. No migrations or gameplay state to revert. **States:** independent G0/QA PENDING, native Unity G1 NOT_RUN, Codex SENT/ACK NONE, GAME_INTEGRATED NO, G2 HOLD, NO MERGE. Do not mistake producer 163 executed checks for 163 game features.

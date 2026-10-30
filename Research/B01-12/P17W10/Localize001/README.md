# B01-12-LOCALIZE-001 / R1 — Game UI localization & pseudo-locale lab

**Owner:** B01-12 — Interactive UX Prototypes. **Wave:** PASS17/W10 [#483](https://github.com/C8LyNn/RoboRougelike/issues/483). **Governance:** [#276](https://github.com/C8LyNn/RoboRougelike/issues/276). **Status:** source producer tested; independent B01-40/B01-60 G0 PENDING; Unity G1 NOT_RUN; Codex ACK none; game integration NO.

## Real implementation

Self-contained offline HTML/JS functional proof of a **28+ key localized UI** (PL, EN, stress pseudo) with garage/combat screen preview. Localized labels, interpolated economy values, Polish plural 1/2–4/5/12/22/112 and English plural 1/other, safe placeholder interpolation, robust catalog parity/type/slot validation, pseudo-locale accent and expansion, live on-screen length/overflow audit, keyboard-focusable language switch, and **state preservation** (screen, quantity, scrap, weapon, input) across language toggles. No dependencies at runtime. Sample data is illustrative, not actual gameplay values.

Files `localize_engine.js` (DOM-free reusable engine), `catalog.js` (30 actual translated keys), `app.js` (UI controller) and `demo.html` (responsive offline UI) are genuine runnable source. The engine rejects malformed catalogs, mismatched plural forms, invalid numeric counts, template/HTML injection and unknown locales/keys. It validates all key parity before UI use. Rendered strings use `textContent`, not HTML injection. Browser state, not localStorage/network/game save. No modifications to current `Assets/**`/Codex Garage/input/combat, previous B01-12 branches, CHL/PARTSIZE or shared CI.

## Test commands and evidence (producer)

From `Research/B01-12/P17W10/Localize001`:

```sh
node test_engine.cjs
python3 -m pip install playwright==1.55.0
python3 -m playwright install chromium
python3 test_browser.py
```

The Python runner uses `/usr/bin/chromium` when installed, otherwise Playwright's installed Chromium. Tests use **real DOM/Chromium**, loading the SHA256-verified unchanged original `demo.html`, `localize_engine.js`, `catalog.js` and `app.js` into a single in-memory HTML page only for browser execution on sandbox environments that cannot navigate `file://`. Opening `demo.html` directly in a modern browser also works offline.

Both tests first verify 4 original files using `manifest.json` SHA256 + exact length; altered bytes yield **exit 2 FAIL_CLOSED** before executing browser/Node tests. `node-results.json` / `browser-results.json` include exact individual results. Test coverage: plural cases, malformed templates and catalogs, code/data injection negatives, 4 viewport keyboard switches, 3 locales, no horizontal scroll, optional action click and preservation of semantic game state. Browser screenshot produced locally: `evidence-1280x720.png`. No actual player/human study.

**Independent receiver:** B01-40 check exact PR SHA, all owned paths, live blob-SHA1 against manifest for 4 runtime inputs, no file collisions/protected paths, workflow scope, disposable rollback. B01-60 fetch exact source SHA and run both original tests/byte-corruption negative in clean workspace; report original command/logs. B01-00 decides whether to offer *only research/design UX* to Codex in [#79](https://github.com/C8LyNn/RoboRougelike/issues/79); Codex alone may adapt to canonical UI Toolkit/TextMeshPro and independently test native Unity G1. This package **must not be imported as a runtime game translation component without an authorized adapter**.

**Install:** no game install; open `demo.html`. **Rollback:** close isolated Draft/remove only `Research/B01-12/P17W10/Localize001/` and `.github/workflows/b01-12-localize.yml`. No game or persistence migrations. Tests do not assert Unity/Player PASS. No merge authorization.

# B01-12 STRUCTURE-IMPACT-001 / R1 — read-only connectivity-loss preview

Producer: **B01-12 — INTERACTIVE UX PROTOTYPES**. Dispatch: [PASS17/WAVE22 #713](https://github.com/C8LyNn/RoboRougelike/issues/713). Governance: [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

## Real functionality

- `impact_engine.js`: independent deterministic pure-JS graph validator + connectivity component scanner and **hypothetical single-part removal impact**. Returns newly disconnected parts relative to baseline, retained modules and sorted connected components. Handles redundant edges and already disconnected baseline; will not mislabel a preexisting detached part as a new casualty. The protected core cannot be selected. Invalid graphs/duplicate edges, IDs, bad types and malformed requests **fail closed** with zero partial results. No input mutation, no side effects.
- `fixture.js`: authored 15-node/17-edge fictional robot graph with a core, braces, wheels, weapon, armor, battery and sensor, including crossbar and rear redundant braces. **Illustrative only** — not source-of-truth Unity Garage, world topology, physics collider/socket graph, Secure Reattach V2, canonical Save/Load or actual balance values.
- `demo.html` + `ui.js`: functional offline interactive diagram (15 actual focusable buttons), SVG edges, click and keyboard Enter, highlighted candidate removal (amber), additional disconnected nodes (red), retained nodes (green), risk ranking, accessible ARIA status, deterministic JSON export and reset. The visual does **not** remove parts.
- No network calls, storage, gameplay changes, Unity Assets, input bindings, native Scene migration, or mutations of canonical/Codex code.

## Local executed tests

```sh
cd Research/B01-12/P17W22/StructureImpact001
node test_engine.cjs
CHROMIUM_PATH=/usr/bin/chromium python3 test_browser.py
```

`manifest.json` SHA256 + original exact byte sizes gate **six executable sources** before either runner. `test_engine.cjs` Node22 tests actual graph input/provenance, adversarial graph/ID/edge and baseline-state rejection, ring/chain redundancy, determinism, immutability, component math and bounded fuzz variations. `test_browser.py` uses actual Playwright Chromium, 4 widths 320x720/768x1024/1280x720/1920x1080 and real click/keyboard, DOM colors, JSON, overflow, ARIA and zero JS exceptions. In restricted Linux sandboxes where `file://` is administrator-blocked, it inlines **the original manifest-verified JS bytes unchanged into original HTML** before opening a real DOM in Chromium. The GitHub Actions Chromium install is used when CHROMIUM_PATH is absent.

A deliberate one-byte append to *disposable-copy* `ui.js` must lead both unchanged runners to `FAIL_CLOSED source mismatch: ui.js` with process exit **2** before Node assertions or the browser. Original source must remain unchanged. `node-results.json` and `browser-results.json` hold actual per-case receipts; screenshots in the producer archive are supplemental (not in Git).

## Dependencies, ownership, receiver and rollback

Dependencies: Node.js 22, Python 3.11+ with `playwright==1.55.0`, installed Chromium for browser QA. The actual UI itself needs only an offline standards-compliant browser. No native Unity package, assemblies, GUIDs or original #510/#556/#596/#647/#690/#691 source dependency. It is deliberately **not** an integration-ready game feature and does not fix any P0 Save/Load issue.

**B01-40:** independently verify exact HEAD, all Git blobs, six manifest SHA256+byte pins, isolated scope and dedicated workflow permissions; G0 formal source-only acceptance or HOLD. **B01-60:** rerun original-byte Node+real Chromium cases and disposable tamper negatives at the exact commit; native Unity G1 `NOT_RUN` for this web-only research feature. **B01-00:** optional integrator intake decision only after authorized QA, no inferred Codex ACK.

Rollback: close Draft and discard **only** `Research/B01-12/P17W22/StructureImpact001/**` plus `.github/workflows/b01-12-structure-impact.yml` in a disposable checkout. Never recursively delete foreign files, symlinks or empty directories in a shared receiver. No protected branch merge/push, no scene installation.

Six stages: `IMPLEMENTED=YES`, `TESTED=PRODUCER_SOURCE_PASS`, `QA_ACCEPTED=INDEPENDENT_PENDING`, `READY_FOR_INTEGRATOR=NO`, `INTEGRATED=NO`, `RELEASED=NO`. `SOURCE_G0=PENDING`, `NATIVE_G1=NOT_RUN`, `CANONICAL_LIVE_SAVE=NOT_APPLICABLE`, `CODEX_ACK=NONE`, `INTEGRATOR_FEEDBACK=NOT_MEASURED`.

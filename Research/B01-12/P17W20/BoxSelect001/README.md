# B01-12 BOXSELECT-001/R1 — group selection in a garage layout

**Role:** B01-12 Interactive UX Prototypes. **Dispatch:** PASS17 WAVE20 [#668](https://github.com/C8LyNn/RoboRougelike/issues/668). **Governance:** [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

## Actually implemented

Deterministic independent JavaScript `box_engine.js` validates a strict layout schema for eleven source-authored module rectangles (one locked reactor), mouse rectangle selection by `touch` or `inside`, keyboard accessible single selections, replace/add/subtract/toggle modes, atomic group movement with collision and board-boundary rejection, bounded Undo/Redo stacks and no-mutating invalid-event rejections. Pure geometry; no DOM in the engine. Demo `demo.html` + `ui.js` are functional offline browser UI for click, genuine mouse-box drag, selection/filters, arrow D-pad, Alt+Arrows, Ctrl+Z/Y, status and JSON-only snapshot display.

**Research data only:** module IDs/dimensions/positions are illustrative, not Unity 6 actual part geometry. This does not create/consume a Unity save, alter garage economy, touch `Assets/**`/R3/CHL, modify Codex-owned logic, nor integrate with actual Unity build. Distinct from prior B01-12 SnapGrid-001 (placement snapping), GARAGE-REPAIR-TRIAGE-002 (repair economics), GARAGE-FILTER-001 (catalogue query), FOCUSNAV and MENUSTACK (focus/navigation).

## Reproduce the producer QA

```
cd Research/B01-12/P17W20/BoxSelect001
node test_engine.cjs
python3 test_browser.py
```

Node v22, Python 3.11+, Playwright 1.55 with installed Chromium. `CHROMIUM_PATH` optionally overrides `/usr/bin/chromium`. Source runners fail closed with exit 2 if any original byte size or SHA256 pin of `box_engine.js`, `ui.js`, `demo.html`, `test_engine.cjs`, `test_browser.py` differs from `manifest.json` (the manifest is deliberately not self-hashed). Browser uses **unchanged exact source bytes** inlined in original HTML to run real Chromium without `file://` restrictions. Test only in disposable checkout.

Negative: copy own research folder into a temporary path, append one byte to `ui.js`, then run **unchanged** two runners; each must `FAIL_CLOSED source mismatch: ui.js` / exit 2. Never mutate original Git HEAD. Confirm GitHub Actions source job original logs and screenshot before claiming remote PASS. Native Unity 6000.3.14f1 EditMode/PlayMode/PhysX and Windows Player = **NOT_RUN**, no game/native claim.

## Source control / receiver / rollback

Scope is exactly own `Research/B01-12/P17W20/BoxSelect001/**` and dedicated path-scoped `.github/workflows/b01-12-boxselect.yml` (if CI enabled). B01-40 independently checks exact Git commit, per-file SHA256 and Git blobs, manifest, isolated paths, permissions and safe scope-only rollback (leave all foreign files, empty folders and symlinks untouched). B01-60 independently executes same exact SHA through Node and Chromium plus corrupted-copy negative, supplies actual logs. B01-00 chooses any eventual Codex intake **only after** source acceptance. Offer is not ACK; ACK is not integration.

**Rollback**: close Draft PR; remove only the package's own new files in a disposable checkout, without recursive deletion of shared receiver paths. No merge/cherry-pick, no destructive mutations, no migration. Previous B01-12 #661 stays frozen. Game #510, Save #556 and selected host lineage remain separate and unmodified.

**Six state fields**: IMPLEMENTED=YES | TESTED=PRODUCER_SOURCE_ONLY | QA_ACCEPTED=PENDING | READY_FOR_INTEGRATOR=NO | INTEGRATED=NO | RELEASED=NO. Additional: G0_INDEPENDENT=PENDING, G1_NATIVE_NOT_RUN, G2_HOLD, LIVE_SAVE_NOT_APPLICABLE, CODEX_ACK_NONE, INTEGRATOR_FEEDBACK_NOT_MEASURED.

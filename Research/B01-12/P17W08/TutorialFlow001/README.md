# B01-12-TUTORIALFLOW-001 / R1 — Interactive tutorial flow research tool

**Owner:** B01-12 — Interactive UX Prototypes. **Dispatch:** [B01 WAVE08 #450](https://github.com/C8LyNn/RoboRougelike/issues/450). **Governance:** [#276](https://github.com/C8LyNn/RoboRougelike/issues/276). **Source scope:** only `Research/B01-12/P17W08/TutorialFlow001/**`, plus the dedicated `b01-12-tutorialflow.yml` workflow. The package does **not** change `Assets/**`, game UI, Unity Input System, game save or Codex's live garage/aim/weapon/economy code.

## Actual executable feature

Two separable functional deliverables:

1. `tutorial_engine.js`: deterministic, DOM-free CommonJS/browser reducer. The API is `initial()` → `transition(state, {type:'ACTION',phase,action,id})` → `view(state)`. It has six actions across Garage, Combat, Results. Only the next expected action in the required phase advances the tutorial; a wrong context/action has no state mutation. Valid event IDs are strictly checked; repeated accepted IDs are rejected. Optional steps (inspect weapon, dodge) can be skipped, mandatory steps cannot. Back operates on the **tutorial tracker only**, never reverting gameplay actions. `RESTART` is only permitted after the completion, while `RESET` always starts a new local session. No external data/API/network access or persistence. `view` returns a plain snapshot; invalid/corrupted state fails closed.
2. `tutorial.html`: responsive standalone offline interactive simulation with six live action buttons, progress bar, skippable-step gate, Back/Reset/Restart, ARIA live feedback, keyboard Enter and diagnostic snapshot. Its actions are expressly **simulated**, not real gameplay/Unity events. Source HTML references the engine as a sibling script. To try it yourself, put both files in one folder and open `tutorial.html` with a normal modern browser. The supplied automated Chromium runner reconstructs the same original source bytes **in memory** because its sandbox blocks `file://` navigation; it never mocks the DOM.

No reference to B01-11 UXFEED-001 queue, B01-12 FOCUSNAV-001, or CONTROLS-001 implementation; these are separate packages and remain untouched. The flow is a gameplay-facing *UX research demonstrator*, **not Unity-importable content and not CODEX_READY**.

## Reproducible producer test commands

Requirements: Node.js 22, Python 3.10+, `pip install playwright`, local Chromium `/usr/bin/chromium` or install Playwright's Chromium (`python -m playwright install chromium`). Optionally set `CHROMIUM_PATH` to a custom executable.

```bash
cd Research/B01-12/P17W08/TutorialFlow001
node test_engine.cjs
python3 test_browser.py
```

Both runners validate exact `manifest.json` SHA256 + source byte counts **before** executing the feature. `test_engine.cjs` writes `node-results.json`; `test_browser.py` writes `browser-results.json` and a local real Chromium `evidence-1280x720.png`. Test cases cover valid 6-step completion, optional and mandatory skip, wrong phase/action, duplicate IDs, back/replay, reset/restart, state corruption and history clone; original Chromium DOM behavior at 320×720, 768×1024, 1280×720 and 1920×1080, keyboard Enter, ARIA, responsive layout and state transitions. Producer tests run on the same exact source bytes delivered for review. Original published case-by-case receipts are included; they are not independent QA.

**Negative proof:** in a disposable full-folder copy change **one byte** in `tutorial.html` without touching the manifest, then run `python3 test_browser.py`; expected **exit 2 / `FAIL_CLOSED` before Chromium**. Restore the unmodified copy for positive testing. Node test rejects a tampered original file too (its runner's thrown assertion exits nonzero). The `build_manifest.py` helper is *producer-only* for source changes; receiver must not run it to hide drift.

## Receiver, boundaries, integration and rollback

- **B01-40:** verify the exact PR head, original `git blob` identities, source manifest SHA256+length, owned paths and no conflict with Codex; disposable clean staging and rollback, G0 verdict.
- **B01-60:** independently fetch original exact-SHA package and run both runners and the expected negative without editing the source; attach real logs, Git SHA and environment, separate receiver verdict.
- **B01-00:** decide acceptance/order. Do not mark SOURCE TESTED as CODEX_ACK/INTEGRATED. Codex alone may choose to adapt into Unity through a separately owned integration after explicit claim. Before native adoption, confirm actual gameplay event names, authoritative state/no-softlock requirements, pause, input policy and accessibility. The demo's 6 steps are **illustrative**.
- **Native Unity EditMode/PlayMode/Windows Player:** `NOT_RUN`, not implied by Node or Chromium. **Human playtest:** `NOT_RUN`.
- **Rollback:** close Draft PR or discard isolated research directory/dedicated workflow. No schema migration, no unity asset removal and no persistent game state changes. No merge to main/develop or touching any frozen source.

**Status (producer):** isolated SOURCE PASS once exact runner logs are attached; `G0_INDEPENDENT=PENDING`, `G1_UNITY=NOT_RUN`, `CODEX_ACK=NONE`, `GAME_INTEGRATED=NO`, `G2=HOLD`.

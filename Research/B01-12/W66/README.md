# B01-12 / W66–W69 PASS13 — D03 session-to-session answer leak

**Research-only.** This is a source-pinned HTML/Chromium experiment, not the Codex garage/Unity/PhysX game implementation. No human study is authorized or represented.

## Original RED → minimal fix → real Chromium GREEN

**Actual old negative:** On original D03 HTML commit `c7ce17d0e9e25a7f8b00c38ca2bbf0a0cfe19fca`, select B, enter participant mode, disclose two warning reasons, press Escape, then return to participant mode. The second presentation **incorrectly starts with the previous participant's two reasons exposed**. Real original headless Chromium reproduced the violation.

**One-line isolated fix:** Entering participant mode calls existing `render()` before focusing `#warnings`; it resets B disclosure to collapsed with the actual current count/assembly, leaving A, the underlying fictional fixture, repair logic and production code untouched.

Original fixed HTML path: `Research/B01-12/W50/D03-critical-warnings.html`; Git blob `889dbd70137aa7eebc0d8b82d01b3bab6ff6ca1d`; SHA256 `5aaece056ffed04985553c9607b2e0fafb4198bfa12c8789fe502607e51f1f11`.

## Independently runnable regression from exact source

Requires Python3 + playwright + Chromium at `/usr/bin/chromium`. On this research branch:

```bash
python3 -m pip install playwright==1.57.0
python3 Research/B01-12/W66/test_d03_session_reset.py \
  --json-out /tmp/b01-12-pass13.json
```

**Optional expected-RED control:** separately extract old HTML from *exact* commit `c7ce17d0...` into `/tmp/D03-old.html` without overwriting the current file, and run:

```bash
python3 Research/B01-12/W66/test_d03_session_reset.py \
  --baseline-html /tmp/D03-old.html \
  --json-out /tmp/b01-12-pass13-red-green.json
```

Test file Git blob `83579984575f0606083352d663ed8bcb927c3c7d`, SHA256 `38da9fc43c86ecc199f38b710af1509488473faa4d825a0897875154f53fb77a`. The script fails closed on source SHA256 and Git blob identity. Its optional baseline test fails closed unless given the original pre-fix SHA256.

## Actual author-run results

**102/102 PASS** with the optional original negative source, **0 JS page errors**; prior D03 source **EXPECTED_RED** observed, current corrected source **PASS**. Actual DOM/AX/Tab/Space/Enter/Escape at **7 widths: 240, 320, 390, 720, 1080, 1280, 1920 CSS px**; includes session 1 disclosure, moderator return, session 2 reentry, hidden reasons and actual status, third-session changed fixture, keyboard focus and no horizontal overflow. Without optional original-negative evidence, expected **101/101**.

Actual local machine JSON, two novel 720/1080 screenshots, old/new original byte snapshots and original script packaged in a **conversation-local** evidence ZIP; not a GitHub Actions artifact and not an independent B01-11 test. The GitHub source allows a third party to replay it.

## Scope and remaining gates

- Zero changes to `Assets/**`, Codex, CHL, PARTSIZE, older frozen H1/H3 v2; no new Unity PR or game GUI.
- B01-11 PASS12 `HUD-3PLUS-PRIORITY-V1` is a **Codex-only product policy**, not a scoped research treatment brief. A1–A3 HUD mock may start only after B01-11 explicitly specifies a comparison experiment. Do not invent variant choices.
- Real people **0/8 NOT_RUN**; real Unity/PhysX/CHL+PARTSIZE G1, real screen reader, independent new-blob B01-11 receiver **NOT_RUN**. Codex game D03 remains OPEN.

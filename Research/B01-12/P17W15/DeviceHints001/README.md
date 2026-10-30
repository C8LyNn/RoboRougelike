# B01-12-DEVICE-HINTS-001 / R1

**Role:** B01-12 Interactive UX Prototypes | **Dispatch:** PASS 17 / WAVE 15 #571 | **Ownership:** #276

## Working feature

`device_hints.html` is a fully offline interactive prototype with a deterministic input-mode reducer (`DH`). It simulates keyboard/mouse and gamepad signals and renders 5 action prompts for each of combat and garage contexts. It does **not** read a real gamepad, modify bindings, intercept gameplay input, send data, write storage, or change Unity source. This is neither the earlier B01-12 CONTROLS-001 remapping editor #428 nor B01-11 native focus navigation #424.

### Transition contract

- Start `kbm` with `combat` prompt context and logical clock 0ms.
- A deliberate `strong` signal from the inactive device arms a candidate; a second strong signal 50–650 ms later switches. Bounces shorter than 50ms and noise events (`mouse movement`, `gamepad analog drift`) never switch.
- After switching, a **850ms** cooldown blocks reverse switching; same-device deliberate input cancels a pending candidate.
- Change combat/garage context without changing active device; this cancels pending candidates.
- `advance` strictly requires positive integer <=60000ms; rejects clock overflow. Valid state/event schemas allow only exact required keys, and invalid input returns original state with reason.
- UI uses `textContent` for labels and status, an `aria-live` announcement, semantic buttons, and visible keyboard focus.
- No persistent capture, real gamepad axis calibration, touch UI, Steam Input glyph set or native Unity Input System adapter in this packet.

## How to run (no build)

Open `device_hints.html` in an offline browser. Click `Pad: naciśnij A`, `+100 ms`, `Pad: naciśnij A` to switch device; then `+900 ms`, `Klawiatura: spacja`, `+100 ms`, `Klawiatura: spacja` to switch back. Use the screen selector to compare hints.

## Original executed producer QA

```bash
cd Research/B01-12/P17W15/DeviceHints001
node test_engine.cjs
python3 test_browser.py
```

Requirements: Node 22, Python 3, Playwright 1.55, Chromium. Locally the runner uses `/usr/bin/chromium`; in CI it uses Playwright's own installed Chromium when `CI` environment is set.

`manifest.json` contains SHA-256 and exact byte length of all three executable original files. **Both test entrypoints deny modified bytes before tests launch.** `node-results.json`, `browser-results.json` are producer-generated receipts; independent B01-60 shall generate its own. `evidence-1280x720.png` is local screenshot, not a source dependency.

Negative check: in a **disposable copy** of these four files, append one byte to `device_hints.html`. Each unchanged test runner must exit with code `2` and `FAIL_CLOSED device_hints.html`. Never modify the immutable source committed to GitHub for this negative test.

## Scope and rollback

Owned new files: `Research/B01-12/P17W15/DeviceHints001/**` and `.github/workflows/b01-12-device-hints.yml` only. No edits to `Assets/**`, prior B01-12 packages, #510 Secure Reattach, #556 Save contract, Codex work, CHL/PARTSIZE or shared CI. No merge. Source candidate SHA freezes on producer handoff.

Rollback: close Draft PR / discard only these own new files and dedicated workflow in a disposable clone. No schema, scene, saved-game migration, empty-directory removal, external file deletion or runtime side effects.

**Receiver:** B01-40 exact-SHA path/blob SHA256 audit + safe rollback; B01-60 independent original Node+Chromium and tampered-copy negative on same exact SHA; B01-00 determines any optional Codex queue. Unity G1/Player/native Input System NOT_RUN, Codex ACK NONE, game integrated NO, G2 HOLD.

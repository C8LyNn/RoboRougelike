# B01-11 / PASS15 — independent original HUD3 Chromium receiver

**Scope:** research-only B01-12 HTML. Does **not** alter game, B01-12's source, native Unity UX or human participants.

## Exact original source identity

- B01-12 standalone original: [HUD3-overflow.html](https://github.com/C8LyNn/RoboRougelike/blob/862bff8c876f977859dc0a1ebd6e8a507046310f/Research/B01-12/W70/HUD3-overflow.html).
- Original commit: `862bff8c876f977859dc0a1ebd6e8a507046310f`.
- Original Git blob: `1caee60184a918d1690a67e363acfc8b213bfe9b`.
- Original SHA256: `3667311e909589f54e0d4122d767eca433e3827979108215b79d1bbb064562af`.
- Independent B01-11 original-browser source: [hud3_receiver_pass15.py](hud3_receiver_pass15.py). The script **refuses** any changed HTML until retested with newly grounded source pins.

## Executed commands and environment

Ran **actual original HTML bytes** under local **Chromium 144.0.7559.96 + Python Playwright 1.57.0**, `page.set_content(original_utf8)`, viewport 320/720/1280/1920 × 800, browser-generated accessible tree and real keyboard Tab/Space/Escape, no external network. Original 12,443 bytes were checked using both SHA256 and Git blob SHA.

To reproduce using an existing local Git clone with B01-11 receiver branch, first obtain the immutable B01-12 source commit (no change to active game):

```bash
git fetch origin research/b01-12-w70-hud3-overflow
git show 862bff8c876f977859dc0a1ebd6e8a507046310f:Research/B01-12/W70/HUD3-overflow.html > /tmp/HUD3-original.html
python3 -m pip install playwright==1.57.0
python3 Research/B01-11/PASS15/hud3_receiver_pass15.py /tmp/HUD3-original.html
```

The **actual independent, original executable first run** covered F0/F1/F2/F3/F8 and participant-only A/B across 4 widths: **108/108 positive checks PASS**, **12/12 actual research-microcopy/participant-blinding EXPECTED_RED**, 0 page JS errors, 0 human participants. Separate **published exact B01-11 test logic**, run directly on the same original HTML, gave **64/64 positive PASS**, **12/12 EXPECTED_RED**, no page errors. Test count is number of cross-width assertions, not participants nor distinct gameplay bugs.

### Three cross-condition acceptance observations

1. **F1 / A and B / all widths — PASS:** Late #5 Beam overheat and #6 Saw jam surfaced in both group reasons, priority rows #5/#6 and accessibility tree before disclosure.
2. **F2 / A and B / all widths — PASS:** Late #9 A EMP and #10 B reload category remains visible. Horizontal overflow absent at the four tested widths; B keyboard disclosure works.
3. **Participant-mode microcopy / all widths — TWO REPRODUCED STUDY DEFECTS:** `#coverage` exposes A/B research condition via visible text/AX; `#participantHelp` promises expandability in A/F0 despite `#expand` hidden. Measured 12 failing study-blindness/truthfulness checks (3 predicates × 4 widths). **Not** native-game defects. B01-12 owner-only fix requested in [#296/6464206638](https://github.com/C8LyNn/RoboRougelike/issues/296#issuecomment-6464206638).

**One expected change from owner:** Research HTML in existing B01-12 branch should show variant-neutral participant language and only true available controls, with new Git blob. Then B01-11 changes its now-expected-RED assertions to required GREEN on the new source. No premature winner, code merge or G1 claim.

**Production boundary:** Canonical Codex #310 old `231005c6d0351945a8ee6684f3856af118f0e94f` 2-row HUD is still not fixed by this research. Unity native 720p/1080p visual is already [CV-06](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223) and remains NOT_RUN. Human participants = 0 / no HQ authorization. B01-11 only owns these `Research/B01-11/PASS15/**` source artifacts.

## PASS15 continuation / cycle 2 — independent garage functional-state source receiver

Source: [B01-12 garage-readiness.html](https://github.com/C8LyNn/RoboRougelike/blob/ff0a7366bd12222cf0a547c82b7715177fd13aa4/Research/B01-12/W74/garage-readiness.html), exact **producer Git blob** `564354278cce2de03cf20a9787e1da88c3bfef05` at commit `ff0a7366bd12222cf0a547c82b7715177fd13aa4`. Do not substitute HEAD silently.

**New executable:** [garage_functional_source_receiver.js](garage_functional_source_receiver.js), standalone Node `git show`/SHA-pin version, plus `verifyAndRun` for embedded exact original JS runtime inspection.

```sh
# With both immutable Git commits available in the same disposable local repo:
node Research/B01-11/PASS15/garage_functional_source_receiver.js .
```

**Actually executed:** fetched the actual producer original HTML source via GitHub; ran its untouched inline `<script>` in a V8 runtime against an intentionally **emulated DOM**, including real original event handlers for part mount/damage/repair, wheel/track/hover switching, tips OFF/ON, disconnect, blocked start and editor return. **19/19 functional logic assertions GREEN**. The single additional schematic visualization assertion **EXPECTED RED**: when both Track L/R are mounted, selected and functional, source renders the system as `READY` but `#schematic` has only `data-piece` IDs `gun, left, core, right, omni, hover` and no trackL/trackR representation at all. The player has track **controls** in the list but no corresponding presence/HP visual on the 3×3 schematic. This is a new, source-confirmed **research prototype** visual mismatch (not canonical Unity). It is not asserted as a browser screenshot, human finding, or new Codex game bug.

**Owner B01-12 minimal acceptance after fix:** with drive = Tracks, both tracks 100HP, schematics must visually distinguish each installed left/right track and its HP from wheels/Omni/Hover. Damage Track R to 0HP must change its *visual* state as well as the already-correct `CRITICAL_MOBILITY` card; swapping drive Wheels ↔ Tracks must keep actual mounted state without lying about active drive. Owner can choose responsive substitution or two additional accessible cells rather than enlarging the fixed 3×3 grid; verify **320/720/1080** in genuine Chromium after changing the exact source SHA. The V8 source predicate for original missing-track cells must be updated if a semantically equivalent alternate schematic is chosen.

No edits to B01-12 files, Codex `Assets/**`, CHL or PARTSIZE. **Native Unity and human participants NOT_RUN.** Current 320–1920 original research HUD3 browser defects still await B01-12 fix and changed SHA; previous 64/64 positive +12/12 EXPECTED_RED not rerun.

## PASS15 continuation / cycle 3 — HUD3 owner's real changed source FIX VERIFIED in Chromium

**NEW owner HTML commit:** [`f7169efa7291641fbae7f5e81a224adc586d56f8`](https://github.com/C8LyNn/RoboRougelike/blob/f7169efa7291641fbae7f5e81a224adc586d56f8/Research/B01-12/W70/HUD3-overflow.html), original Git blob **`e73b57e52ca0706bcf739340f5b06dd66b55e104`**, SHA256 **`df857d206855437608ac94421a354ef9112366e0ac91d120ac03d772bb5a6884`**. Verified **exact source string equality against GitHub connector** and independently reconstructed exact 12,710 bytes from earlier hash-verified 12,443-byte source using only the two actual owner diff replacements (CSS `.participant #coverage{display:none!important}` and truthful `#participantHelp` assignment based on actual disclosure visibility). Reconstructed **Git blob SHA-1 matches the original GitHub blob byte-for-byte**. No B01-12 source modified by receiver.

**Independent real browser run:** published [`hud3_fix_retest.py`](hud3_fix_retest.py) on the immutable exact new HTML with **Chromium `144.0.7559.96` + Python Playwright `1.57.0`**, at 320/720/1280/1920 CSS px. **76/76 browser checks PASS; 12/12 former participant-blinding/help RED → GREEN**, 0 JS errors. Original F1/F2 later guns priorities, group alerts and B disclosure keyboard behavior remain GREEN. Genuine 320, 720, 1280 browser screenshots were captured from **this exact new original source**; zipped private/local evidence (6 files, CRC PASS), not a GitHub Actions or Unity artifact.

```sh
# Git object new owner fix must be available locally:
git show f7169efa7291641fbae7f5e81a224adc586d56f8:Research/B01-12/W70/HUD3-overflow.html >/tmp/HUD3-fixed.html
python3 Research/B01-11/PASS15/hud3_fix_retest.py /tmp/HUD3-fixed.html
```

**Verdict:** both previously independently reproduced **research participant mode** faults now **FIX_VERIFIED at real Chromium/source SHA**. This is **not** a game integration, actual screen-reader user session, HUD third-gun canonical Unity fix, real human preference result, native G1 nor G2. Current Codex #310 original R6RangeHUD still requires Codex ownership and existing CV-06. Garage visual missing Tracks L/R is a **separate** independent prototype issue, unaffected by owner HUD3 change.

## PASS15 continuation — independent GARAGE original-JS receiver

- New executable [garage_functional_source_receiver.js](garage_functional_source_receiver.js), original owner input B01-12 W74 garage commit `ff0a7366bd12222cf0a547c82b7715177fd13aa4`, Git blob `564354278cce2de03cf20a9787e1da88c3bfef05`.
- Executed published exact JS source in V8 + emulated DOM with unchanged original producer inline JS: **19/19 functional workflow checks GREEN**, **1 new actual presentation criterion RED** (TRACKS both mounted functional/READY, but schematic only exposes wheel left/right, not `trackL` or `trackR`). This is not a Chromium or Unity result.
- Run after `git fetch origin research/b01-12-w74-garage-readiness`: `node Research/B01-11/PASS15/garage_functional_source_receiver.js .`. Script fails closed when original Git SHA differs, does not edit any files and returns 0 for current expected 19-GREEN +1-diagnostic-RED fixture.
- **Owner fix:** B01-12 research producer, not B01-11/Codex production. Direct finding: [issue #296/6474012178](https://github.com/C8LyNn/RoboRougelike/issues/296#issuecomment-6474012178).

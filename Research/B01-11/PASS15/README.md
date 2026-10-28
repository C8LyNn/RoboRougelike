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
git fetch origin research/b01-12-w50-d03-critical-feedback
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

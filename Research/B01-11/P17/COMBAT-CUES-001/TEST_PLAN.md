# Actual browser acceptance and pending native checks

Execution host: Linux, Python 3.13, Playwright + headless /usr/bin/chromium. Command: `python3 SOURCE/test_browser.py` from any directory. Browser loads the exact file bytes by `page.set_content`; not a JS reimplementation.

**Executed: PASS 80 / FAIL 0 / SKIP 0.** 20 assertions at each of 320x720, 720x960, 1280x800, 1920x1080: initial state/part count, damage decrement, 0HP weapon outage, critical notice, compact toggle, reset, ally attribution, play-all and disabled Next, core destruction, no horizontal overflow, JS console, plus 6 reducer negative/positive checks (duplicate IDs, unknown part, negative damage, impossible blocked damage, input immutability, clamp HP to zero). Full per-assert result file: `TEST_EVIDENCE/browser-results.json`.

**NOT_RUN:** actual Unity 6000.3.14f1 assembly/edit/play/Windows build, native real damage event connection, real camera and co-op, human participant study. Independent B01-60 QA awaits handoff. Fail-closed rule: browser PASS is not native Unity PASS.

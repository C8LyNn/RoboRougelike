# B01-12 — PASS14 / W70 — HUD third+ weapon research

**Research-only, NOT an alternative Codex implementation.** One standalone micro-demonstrator `HUD3-overflow.html` implements exactly [B01-11's PASS13 bounded A/B brief](https://github.com/C8LyNn/RoboRougelike/issues/296#issuecomment-6454963644).

## Original game source pin (read-only)

- Codex [#310](https://github.com/C8LyNn/RoboRougelike/pull/310) HEAD `231005c6d0351945a8ee6684f3856af118f0e94f`, HUD [`Assets/R6/R6RangeHUD.cs`](https://github.com/C8LyNn/RoboRougelike/blob/231005c6d0351945a8ee6684f3856af118f0e94f/Assets/R6/R6RangeHUD.cs) blob `adfd3c1a69fc4355e055581a736ce10cccc7ba91`. Current HUD reports group A/LPM and B/PPM operational gun counts, but truncates individual rows after the first two in assembly order. No native Unity HUD rendered test or human preference result is claimed.
- Experiment's exact original independent single-file HTML Git blob `1caee60184a918d1690a67e363acfc8b213bfe9b` and SHA256 `3667311e909589f54e0d4122d767eca433e3827979108215b79d1bbb064562af`. Any hash mismatch invalidates test carryover. Old D03 and H1/H3 sources are unrelated and untouched.

## A vs B (a controlled experiment, NOT a product recommendation)

Both treatments always display **both** group alert *reason categories*, assigned, operational and currently fireable counts, and two priority-selected individual statuses. These two statuses are chosen from **all** guns: cannot-fire (EMP, overheated, jammed, empty, destroyed) before recovering (reload/cooldown) before ready, reserve one from each group if both have alerts, tie-break by stable physical gun ID. A contains only those two rows; B adds a keyboard-accessible `Pokaż wszystkie stany dział` control to reveal every other gun. Swapping A/B does not change the fixture's actual states.

The research model counts `DESTROYED` assigned but not operational. `HEAT/JAM/EMP` preserve operational health but may be currently unable to fire. This is an **isolated display model derived from the brief**, not actual runtime-derived weapon state. There is no health/weapon implementation, damage, gameplay firing or network.

### Exact controlled research fixtures

| ID | Config | Principal acceptance |
|---|---|---|
| F0 | 2 guns, 1 per group READY | 2 rows, no phantom alarm |
| F1 | 6 guns A3/B3, #5 A Beam HEAT, #6 B Saw JAM | Both urgent causes visible before disclosure, two slots show #5 and #6 |
| F2 | 12 guns A6/B6, later #9 A EMP, #10 B RELOAD | Both cause categories and exact counts visible |
| F3 | one damaged A Gun HP0, B empty | A 1 assigned/0 operational/0 fireable, B 0/0/0 |
| F4 | 12 READY | No phantom alert |
| F5 | two A exceptions (HEAT + EMP), B READY | Group A two independent categories, two A detail rows |
| F6 | F1 plus actual A↔B assignment swap | Counts/reasons change group label, gun ID/health preserved |
| F7 | A HEAT+B JAM, resolve A only | B issue persists; then resolve B without stale alerts |
| F8 | no mounted guns | Both groups 0/0, no false ready |

A participant-only mode hides all preset/variant/repair answers and moderator controls. Escape returns to moderator focus; next participant entry collapses overflow again.

## ACTUAL producer browser QA

Original same source HTML tested on **headless Chromium 144.0.7559.96** with Python Playwright 1.57.0. **126/126 genuine browser assertions PASS; 0 page JS exceptions**. Screens: **320, 720, 1280, 1920 CSS px**; real DOM, Tab/Space/Escape, `aria-expanded`, `body.aria_snapshot()`, overflow, fixture state changes and categories in both collapsed/expanded variants. Additional negative: **injected DOM mutation deliberately removed all group reasons and restored first-two READY rows**, causing the exact third+ problem to become inaccessible; original renderer immediately restored both causes. This is a **controlled negative mutation**, not a claim of an observed original Unity FAIL.

The full source test `test_hud3.py`, original-machine JSON `results.json`, the HTML and authentic 4 Chromium PNG screenshots are in the **conversation-local** PASS14 evidence ZIP. The ZIP is **not** a GitHub Actions artifact and cannot by itself substantiate an independent B01-11 QA verdict. The repository contains the original immutable HTML and this replay procedure.

### Reproduce independent browser inspection

On a machine with Python3, Playwright and Chromium, place `HUD3-overflow.html` locally or serve over a local HTTP server; the HTML has **no third-party scripts, external media, data storage, network fetch or framework**. Test F1/F2 in A/B at 320/720/1280/1920 CSS px with devtools accessibility tree and keyboard; compare the two groups and top rows before opening details. The attached `test_hud3.py` executable is pinned to this exact SHA256/gitblob and uses `page.set_content` on byte-verified source (no `file://` sandbox assumptions). Re-run as: `python test_hud3.py` when located next to a copy of the original HTML named `index.html`.

## Boundaries and handoff

- **NEXT B01-11:** independent exact-new-Git-blob receiver: true A/B fixture parity, F1/F2 third+ group alerts before overflow expansion, keyboard and 320px checks. No human comprehension/preference winner without HQ-approved volunteers.
- **CODEX VALIDATION QUEUE:** [CV-06](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223) ALREADY covers 3rd+ gun heat/jam HUD and 720p/1080p visual checks; [CV-03](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223) covers garage functional dual-warning and group remount; [CV-07](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223) covers full native GUI and build. **NO duplicate queue entry.** Codex environment and test execution unverified; not code acceptance, no G1/G2.
- No human participants (0), native Unity/Player, actual screen-reader user session, CODEx fixes, art, CHL/PARTSIZE source, merge or release. This UX research artifact is a fixture **for understanding** overflow readability, not permission to integrate a second HUD.

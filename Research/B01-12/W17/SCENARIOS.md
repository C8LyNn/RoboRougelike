# B01-12 W17 — manual scenario sheet / research study

All interactions are UX fiction and should be run using the standalone HTML.

| ID | View/variant | Actions | Expected UI-only behavior | Measured question |
|---|---|---|---|---|
| G01 | Garage A/B/C | Scenario "Konflikt footprint" → select 2×2 armor → hover occupied part | RED invalid logical overlap; click cannot insert overlapping part | Can user distinguish occupied vs inaccessible? |
| G02 | Garage A/B/C | Scenario "Nieznany collider" → hover wheel | AMBER unknown, not physically approved | Does user mistake logical grid for collider proof? |
| G03 | Garage A/B/C | Hover free 2×2 part | GREEN logical free, text rejects PhysX/socket guarantee | Does user still say "physically safe"? |
| G04 | Garage A/B/C | "Tylko rdzeń" → switch tips OFF | Mobility + weapon critical messages remain visible | Can user identify both critical problems? |
| G05 | Garage A/B/C | "Zniszczona broń (0 HP)" → hover gun → Uszkodź/napraw | Weapon warning clears after synthetic repair; mere presence didn't count | Does tester understand functional warnings? |
| G06 | Garage A/B/C | Click activity launch | Research dialog only, no block/start policy adopted | Does confirmation convey owner choice vs hard block? |
| C01 | CHL A/B/C | Select Heavy → Begin → Skip next gate | Event records rejected skip; CP unchanged | Is error noticeable? |
| C02 | CHL A/B/C | CP1 → Pause → try CP2 → Resume | CP disabled during pause; unblocked after resume | Does paused state read clearly? |
| C03 | CHL A/B/C | Progress 4 CP → META → Retry | Mock completed, next click Retry resets READY and 0/4 | Does tester know when new trial begins? |
| C04 | CHL A/B/C | Light → Begin → Fail → Retry | Mock failure; reset, old results retained until clear | Do testers confuse mock ledger with real trial? |

**Three hypotheses:** H01—unambiguous logical-vs-physical feedback reduces false approval; H02—critical warnings remain discoverable with suggestions OFF; H03—entry/retry/finish state diagram eliminates unclear recovery. These are untested hypotheses with humans.

Recommended moderated protocol: 3–5 users, counterbalanced A/B/C order, observe completion time, critical misread rate, incorrect "PhysX cleared" assumption count, wrong clicks, preference + quote; B01-11 should independently decide sample and interpretation. No participant testing performed by B01-12.

**Automation actual:** 28 PASS / 0 FAIL in Chromium (Playwright, viewport 1440×960, direct page set_content); full runnable script/screenshots in W17 research ZIP. **Unity/native/real 3×Light+3×Heavy: NOT_RUN.**

**Exit criteria for future adoption:** B01-23 validates actual typed sockets, colliders, rotated bounds, broadphase fail-closed. B01-20/60 verify real CHL CP finish arbitration, retry, physics and 6 measured drive completions. No implementation claims from UI research.

# B01-12 / W17 — Garage & CHL disposable UX research prototype

**Artifact:** GARAGE-CHL-UX-001 / W17-R1
**Role:** B01-12 INTERACTIVE UX PROTOTYPES
**State:** RESEARCH-ONLY; NOT a Unity/game package, no merge, no G2, no Codex integration.
**Authority:** https://github.com/C8LyNn/RoboRougelike/issues/71#issuecomment-6438657467
**Governance:** https://github.com/C8LyNn/RoboRougelike/issues/276

## Open
Download `index.html` and open in a modern browser. All HTML, CSS and JavaScript are in one offline file; no imports, cookies, external requests or telemetry. GitHub's code viewer is NOT a live page preview.

## View 1 / Garage—PARTSIZE
9×6 symbolic 0.55m logical cells; select/click or drag items, rotate by R/90°, place/remove; test occupied and out-of-range 2×2 logical footprint vs physically UNMEASURED types. Red = logical invalid, amber = unknown real collider, green = logically free **NOT PhysX or typed socket confirmation**. Fictional damage/recovery of 0-HP weapon and functional critical warnings (core/mobility/weapon) persist when non-critical suggestions are switched off. Launch CTA opens research dialog; *block launch or confirm* is **owner decision pending**.

## View 2 / CHL—Light/Heavy
Selectable Light (demo 270kg / 2.00m wheel envelope width) and Heavy (demo 1150kg / 2.82m); READY → RUNNING → 4 ordered **manually pressed** CPs → separate mock META, PAUSE/RESUME, fail, retry and per-rig mock results ledger. No vehicle physics or real-world driving success is simulated. Strict CHL CP[last]+META deferred one FixedUpdate remains a native game requirement, **not proven by clicking**.

## Feedback variants
A short inline, B expanded diagnostic, C compact; compare speed/comprehension, not only aesthetics. Three distinct variants in the SAME single HTML prototype, not three packages.

## Evidence and scope truth
2026-10-27: 28/28 headless Chromium/Playwright **UI-interaction smoke PASS**, using injected local HTML content (`page.set_content`) because browser navigation to file:// and 127.0.0.1 was blocked by container policy. Five real browser screenshots (1440×960) and executable `tests/test_smoke.py` are included in the independently downloadable conversation ZIP. Browser screenshots contain synthetic scenarios, not Unity screenshots. **Unity native compile/Editor/PhysX/manual Light+Heavy completions NOT_RUN; human moderated sessions NOT_RUN.**

Verified research references: PARTSIZE PR #286 at `937838662f79c4c441d682d731fa860e4cc3d0f1`; CHL PR #285 at `30c0355a62764eb37798347612564ee51f3b36e1` (captured 2026-10-27; moving HEAD may supersede). There is no source coupling: this standalone HTML doesn't import any Unity code.

Git source file's actual **Git blob SHA1** `65b84bdfded69eb56771fc529b9a4c5cabfd77a5`, matching ZIP HTML bytes. Published to isolated **research-only branch**, with no change to `Assets/`, `main`, `develop`, #285/#286 producer branches or Codex #275.

**Receivers:** B01-00 evaluate variants/risks, B01-11 run human tests and return evidence, B01-23 assess logical-vs-physical feedback semantics, B01-20 assess CHL entry/retry/result wording. Do not promote to Unity feature without separate HQ decision.

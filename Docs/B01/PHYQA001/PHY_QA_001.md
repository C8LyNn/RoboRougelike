# PHY-QA-001 — B01-00 executed evidence receipt and B01-60 acceptance contract

**Date:** 2026-10-25. **Producer:** Codex. **Independent native QA owner:** B01-60. **Coordination:** #131 / #71. **No game integration, merge, release or edits to PR #125.**

## Frozen source reference
- Codex Draft PR #125 `codex/b01-phy-grounding-core-r1`, reviewed HEAD `916654634d1bfdc2bbc14e64b9eeac856ede1fa8`, base `feature/rrl-mov011-integration@865932fd78ef936493bdc51ea9ca612fe9ec31eb`.
- Product runtime/build source `523046f5bf6ddab0caf62253615c962e808c3053`; Editor fixture SHA `54f0192b89ffac9f6989da36e8f5d893e631d600`.
- Read `R6WheelContacts.cs`, `R6VehicleDrive.cs`, `R6CoreRecovery.cs`, editor test cases and producer report at these pinned SHAs. In product source, tread drive uses actual `Physics.RaycastNonAlloc` on mounted wheel colliders and terrain normals, wheel-available force is zero when no confirmed contacts. Recovery owns the existing Core / body and caps force/torque.
- Published ZIP and Windows operator records are Codex **producer evidence**, not independent B01 executable results.

## Actual B01-00 limited receipt — 11/11 PASS, *metadata only*
The GitHub-connected coordinator read the exact producer JSON at `9166546` and executed the following 11 checks against parsed values on 2026-10-25:
1. Native physics `checks` array has 75 entries.
2. Each of 75 `passed` fields equals JSON true.
3. W8 compatibility `checks` has 47 entries.
4. Each of 47 `passed` fields equals true.
5. Physics `process.json.exitCode` is 0.
6. Physics `process.json.sourceSha` points to gameplay source `523046f5...`.
7. Full EditMode counters reconcile `992 + 7 + 0 = 999`.
8. The original 7 EditMode FAIL remain explicitly recorded.
9. Focused correction record declares 28 PASS / 0 FAIL.
10. Seven original FAIL cases have paired corrected-case identifiers.
11. Reconciliation explicitly records *fresh full rerun NOT_RUN*.

**These are 11 independent checks of published record CONSISTENCY, NOT 11 independent game tests.** They do not prove authenticity of the producer's Unity runtime, file hashes or underlying physics. A reproducible checker has been committed to `Tools/B01/PHYQA001/evidence_receipt.py`; run it on the actual PR #125 checkout:
```sh
python Tools/B01/PHYQA001/evidence_receipt.py --repo-root .
```
Because this audit branch deliberately does not integrate PR #125, copy the standalone Python checker into an **isolated throwaway checkout** of the immutable PR #125 source before running; do not treat that copy as a host-game modification. Raw NUnit XML in `unity-results.zip` and locally retained Player logs still require separate independent inspection by B01-60.

## Independent B01-60 P0 regression matrix — MUST BE RUN OR BLOCKED
| ID | Rig / configuration | Physical stimulus | Observable acceptance evidence |
| --- | --- | --- | --- |
| PHY-01 | Light/Heavy, 4 contact wheels | floor → airborne → floor | contact 4→0→4, no propulsion in air, inertia retained |
| PHY-02 | 1/2/4 mounted/grounded wheels | destroy/disconnect one or more | traction reflects **grounded** wheels only; asymmetric moment measured |
| PHY-03 | inverted / roof-supported | drive commands then Core attempt | roof wheel contact 0, exactly bounded recovery if legitimate contact |
| PHY-04 | EMP/dead Core/pause | request recovery | 0 attempts / no HP or part restoration |
| PHY-05 | Heavy 1312kg, tall/wide/asymmetric | tip to roof, blocked/free surroundings | force/torque/cooldown capped; no teleport, no collider tunnelling |
| PHY-06 | MOV009 raised FullSupport | lift support then lower | support contacts and motion 0 while unloaded; regained on lowering |
| PHY-07 | garage→range→fight→loot→garage | normal operator input | physically movable after ownership repair, destroyed wheel/EMP, no regressions |
| PHY-08 | AI chassis with 1/2/0 wheels | sustained pursuit / push | no phantom drive, physically credible deceleration |
| PHY-09 | wheel on sloped/ramp/edge | climb, hang, recontact | real RaycastNonAlloc grounded counts, slope normals, no vertical force hack |
| PHY-10 | W8 sensors + MOV011 | cross-check events and controlled damage | scanner/rotation/repair/EMP intact |
| PHY-11 | native shutdown | normal close + isolated automation diagnostic | separately record source, attach status, exit; known `C0000005` not marked repaired |

**Required fresh Unity 6000.3.14f1:** full EditMode on exactly pinned reviewed source + PlayMode, XML/test count/exit and Editor.log; Windows manual subsets and 720p/1080p if available. If access lacking, `NATIVE_NOT_RUN/BLOCKED` and precise required environment. Test publisher must be B01-60, not Codex.

## Source review vs run evidence boundaries
- Codex published full EditMode 992/999 with later 28/28 corrected fixture classes. No second full 999/999 PASS execution is available at review.
- Codex published full PlayMode 398/398; native physics75/75 and W8 compatibility47/47; local Windows build and ordinary-input smoke. These may be reproducible but remain producer's claims until independently reviewed/executed.
- Known unresolved automation-attached Alt+F4 `0xc0000005` is a separate diagnostic problem; user has a successful ordinary close, so do not conflate contexts.
- Unity independent execution by B01-00: **NOT_RUN**; by B01-60 on this exact SHA: **PENDING**. G1 **BLOCKED**, G2 **HOLD**.

## Fault loop
B01-60: report `PHY-QA-001/<ID>` with exact tested SHA/rig/commands/time series/screenshots/logs → B01-00 triage source-vs-test-vs-environment; forward product runtime bug to **Codex** as small repro/test packet, not an unauthorized B01 physics patch → Codex new SHA → B01-60 repeat tests → B01-40 compare MAPGEN compatibility.

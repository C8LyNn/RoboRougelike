# RAC-001 — R2 / time-step bypass hardening

Date: 2026-10-22 (Europe/Warsaw)
Branch: `feature/rrl-rac001-isolated-pack`
Base develop: `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`
Parent HEAD: `d57bd0faff2b9f1741af24e5fb468b95f513037e`
Issue #8; Draft PR #13.

## Finding
R1 used `maxSpeed * deltaSeconds + 0.25f` allowed movement per Advance call. Repeating tiny positive or zero-duration calls allowed excessive travel with negligible elapsed time, potentially reaching checkpoints while bypassing the speed sanity guard.

## R2 changes
- Ignore zero-duration ticks after validating inputs: no movement or checkpoint score.
- Replace fixed 0.25 m per-call tolerance with 5% proportional tolerance.
- Add two NUnit EditMode regressions for zero-time and tiny positive steps.
- No new assets, GUIDs, shared file edits, scene changes or runtime auto-installation.

## Verification
- Static patch and scope checks: PASS before commit.
- Unity 6000.3.14f1 EditMode: **NOT RUN** (no Unity executable/licensed runner).
- Unity PlayMode: **NOT RUN**.
- Windows build: **NOT RUN**.
- Main gameplay: **NOT INTEGRATED**.
- Client-side movement sanity guard is not server-authoritative anti-cheat.

## Next
Run EditMode and PlayMode with licensed Unity 6000.3.14f1 in isolated checkout, capture XML/logs/exit codes, verify the unsaved demo, then consider an opt-in real vehicle adapter. Keep PR #13 DRAFT, no merge.

## GitHub Actions for R2 source commit `3c8a44e577df768df69b019ff54a125ada432c0c`

- **CI-01 Repository Validation: PASS** — workflow run 39692365323. Validator regression and Git index integrity both succeeded.
- **CI-02 Unity EditMode: NOT RUN** — workflow run 39692365307. Activation credentials preflight failed; Unity EditMode step skipped.
- **CI-03 Unity PlayMode: NOT RUN** — workflow run 39692365281. Activation credentials preflight failed; Unity PlayMode step skipped.
- Windows build: **NOT RUN**.
- Issue #8 and PR #13 metadata updates: **BLOCKED** by connector safety checks. This file is the durable handoff.

Links: https://github.com/C8LyNn/RoboRougelike/actions/runs/39692365323 · https://github.com/C8LyNn/RoboRougelike/actions/runs/39692365307 · https://github.com/C8LyNn/RoboRougelike/actions/runs/39692365281

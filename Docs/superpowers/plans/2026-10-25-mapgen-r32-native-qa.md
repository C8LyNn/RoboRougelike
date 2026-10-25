# MAPGEN R3.2 native validation plan

> Execute inline with superpowers:executing-plans. User explicitly requires execution now; no new approval loop. Source defect protocol and independent integration gates remain binding.

**Goal:** actual Unity compile/test and real R6 vehicle traversal evidence for immutable R3.2.
**Architecture:** isolated full package on OBJ210/PHY125, original source unchanged; diagnostic-only fixtures call real R3 assembly/R6 drive and observe produced colliders, no active-game wiring.
**Tech:** Unity6000.3.14f1/C#/PhysX, existing retained-process native runner and Python audits.
**Spec:** Docs/superpowers/specs/2026-10-25-mapgen-r32-native-qa.md.

## Constraints / review focus
- Source003bced and target4c9236a exact; preserve GUIDs and all existing checkout/runtime branches, no shared merge.
- No-surrogate/native confusion; real rig mass/COM, actual wheel contacts and terrain colliders, no synthetic teleport or second solver.
- Wall/seam/ramp/corner/spawn placement, vehicle envelope mismatch, contact continuity and stalled/inverted outcomes must remain visible in telemetry.
- Regenerate/clear/invalid-config must preserve old live map on rejected generation and remove old physics safely.
- Original source failures and unrun scope explicitly reported; no independent G1/G2 self-approval.

### Task1 — source/import/original native suites
- [ ] Save target/source/checkouts/gate ACK; verify full manifest SHA256/bytes/meta/GUID and unchanged target shared files.
- [ ] Compile native original EditMode and PlayMode with exact checkout SHA; record XML/logs/exits and actual assertions, source FAIL stops later tests.

### Task2 — physical R6 matrix and visual diagnostics
- [ ] Test-only real-rig harness at dedicated location; inspect actual dimensions/contact authority; no changes to production R3/R6/P12/OBJ or MAPGEN runtime.
- [ ] Run12base cases and historical Heavy normal/hard cases with finite deadlines and full contact/position/collision/stall/finish telemetry.
- [ ] Check regeneration/invalid configs/edge cases;720/1080 diagnostic captures. Preserve failures and distinguish API driver/input/human scope.

### Task3 — reporting
- [ ] One fresh read-only whole-branch scope/evidence review, fix important QA-tool findings without producer runtime edits; no B01-60 acceptance implied.
- [ ] Publish QA Draft and Issues93/79 with exact SHAs/results/table/defects/reproductions/limits; preserve all evidence and stop before integration.

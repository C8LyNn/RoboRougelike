# B01-61 PASS17/W09 — W07 direct Reattach safety bypass regression

Original pinned producers: #421 (W06), #439 at cdcaea750fda1a9d7db6018e34970f4b9f206427 (W07), and #455 at 99dfbd25d471f9983f9472b12d3725f4c002f7e9 (W08). All remain immutable. This is an independent QA-only Draft, not an integration patch.

## Confirmed integration blocker

Original W07 DebrisRigPlayground.Reattach(int) publicly resets live Rigidbody motion, enables kinematic state, changes parent and rearms stable part ID **without checking mount reach or Collider clearance**. W07 OnGUI still calls that method on the original Reattach button. W08 DebrisSafeReattachGate.TrySafeReattach checks Inspect before calling W07; however its installer only adds optional gate component, leaving original button and public API unguarded. Thus an obstacle can block W08 safe route but W07 raw entrypoint remains available. This was identified in W08 producer scope; our new reproducible QA gate prevents falsely accepting combined W07+W08 safe-repair behavior.

## Reproduce original-source RED

From repo root on the stacked QA-only branch:
- python Tools/B01/B01-61/P17W09ReattachBypassQA/verify.py --evidence : exits 0 after checking four immutable Git blobs, eleven QA-only paths, and the exact unsafe code paths.
- python Tools/B01/B01-61/P17W09ReattachBypassQA/verify.py --release : exits 3 with RELEASE_GATE_RED until producer issues a safe revision.

Separate GitHub Actions jobs on Windows2025/Ubuntu24.04 record both logs and deliberately remain RED for the release gate. This is an **actual-source call-path regression**, not a claim of native PhysX execution.

## Authored native Unity6 PlayMode regression (NOT RUN)

Assets/B0161ReattachBypassQA/Tests/PlayMode/ReattachBypassPlayModeTests.cs sets up the original W07 two-rig MonoBehaviour and W08 gate, creates a real BoxCollider obstacle at the mounting pose, proves W08 safe gate refuses docking, then asserts W07 public Reattach also refuses to move or rearm the blocked physical part. On the frozen source the latter assertion is expected RED. Run only in an authorized isolated Unity 6000.3.14f1 host with real Rigidbody and Test Framework; no shim. Do not mark native PASS before actual XML/logs are available.

## Recipient / stop condition

B01-23 producer: propose a separately versioned non-bypassable mount transaction for all UI and public API paths, avoiding circular asmdef dependencies; return for independent B01-60/61 Unity regression on exact revised SHA. B01-40: hold combined W07+W08 safe-dock G0; individual W06 core intake may be evaluated separately. B01-00: record blocker #466 and #79. Codex ACK is not presumed. G1/G2 HOLD. Rollback: close unmerged Draft/remove this branch; do not modify W06/W07/W08 or Codex.

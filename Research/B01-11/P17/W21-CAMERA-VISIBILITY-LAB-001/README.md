# B01-11 / PASS 17 WAVE 21 — CAMERA-VISIBILITY-LAB-001 R1

## Scope and origin
Standalone, executable Polish research tool measuring **synthetic top-down projected occlusion** of a sample robot by simple axis-aligned rectangle obstacles with height. No Unity, no PhysX, no real collider or camera access. Separate original functionality from B01-12 SnapGrid #404 (garage placement), B01-11 Frame-UX #364 (screenshot annotator), B01-11 SaveTrust #658 and RestorePreview #679.

## Actual behavior
- Original visibility.js tests 5 target samples (center and ±radius X/Z), slab-intersects projected camera-to-sample rays against bounded rectangular extrusions, and rejects rays crossing the obstacle vertical range.
- Reports count 0..5, fractional visibility, per-ray blocker ID and an ordered blocker list. Invalid/malicious/nonfinite geometry rejects rather than pretending clear.
- Bounded deterministic 13-position suggestion search (original, left/right shifts, pull/push, height raise and mixed variants). Maximizes visible samples, then minimizes displacement from the original camera. This is an advisory **camera candidate**, NOT a verified in-game camera solution.
- Original index.html is a functional offline interactive canvas with 4 meaningful synthetic scenes: open terrain, high wall, corridor, low cover. Click canvas to reposition the camera; press a suggestion button to apply proposed position. UI provides explicit text count and obstacle names, not color alone; keyboard operable native buttons, polite live announcements, Polish copy.

## Original source execution
From repository root:
`node --check Research/B01-11/P17/W21-CAMERA-VISIBILITY-LAB-001/visibility.js`
`node --test Research/B01-11/P17/W21-CAMERA-VISIBILITY-LAB-001/test_visibility.cjs`

Node22 real original source unit/integration/negative checks cover correct geometry (including vertical overpass, behind camera/robot, identical obstacle ordering, parallel rays), input validation, 120 camera-geometry perturbations, 100 low walls, interactive HTML controls, workflow and manifest SHA256 bytes. Dedicated real Windows2022/Ubuntu24.04 source CI, exact-HEAD evidence only. **No engine, actual occlusion, real participant, FPS, comfort, or 3D camera claims.**

## Design interpretation (hypothesis, not measured player UX)
- **0/5 synthetic rays**: strongly recommend moving the camera to keep the robot readable; do not automatically teleport native gameplay camera without smoothing/safe movement tests.
- **1–4/5**: show some obstruction; consider contextual transparency/outline, camera collision, or minimal correction, subject to gameplay readability and style validation.
- **5/5**: passes this toy geometry sample only. Does **not** mean object visibility is good on screen: no field-of-view, clipping, draw order, occlusion meshes, motion, HUD, user attention, or motion sickness measured.
- B01-20 and camera/gameplay owners must separately validate real Unity Camera + PhysX geometry, player input, obstacle collision and actual screen readability. Never treat model counts as native test evidence.

## Intake, rollback and authority
Five B01-11-owned Research files plus B01-11 dedicated workflow; SHA256-byte manifest excludes itself. Install: copy only separate Research folder into a disposable analysis checkout or open HTML offline. Rollback: remove ONLY that own disposable folder/workflow, preserving third-party files; no destructive receiver modifications. No Assets/**, no canonical code, no #510/#556/#596/#642/#647/#690, no #681/#682/#691, #604/#644/#652 ALT_HOLD, no Codex, no main/develop, no merge.

Receivers: B01-20 and actual camera subsystem (3D/native design study); B01-12 optional future UI adaptation; B01-13 human comprehension test plan; B01-40 manifest source G0; B01-60 independently activated Unity or G1_NOT_RUN; B01-00 research feature handoff. Source G0 is not actual Unity G1, and producer tests are not independent QA.

Six lifecycle fields: IMPLEMENTED=YES, TESTED=SOURCE only after real CI, QA_ACCEPTED=NO, READY_FOR_INTEGRATOR=NO, INTEGRATED=NO, RELEASED=NO. SOURCE_G0=PENDING; NATIVE_G1=NOT_RUN; LIVE_GAME_SAVE=NOT_APPLICABLE; CODEX_ACK=NONE; G2=HOLD; INTEGRATOR_FEEDBACK=NOT_MEASURED.

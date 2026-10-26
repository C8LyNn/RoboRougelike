## W12 CAMERA R1.2 SPECIFIC NEW REGRESSION ADDENDUM
Native QA previous source [#234](https://github.com/C8LyNn/RoboRougelike/pull/234) R1.1 compiler PASS Edit5/5 Play3/3, 8 native rig/res functional FAIL for CAM-01 actual ray26 intersections (pitch transition) and CAM-02 Tall720p corridor9.1732m jump. See TEST_EVIDENCE_R12.md.
Source offline: Actions node geometry_probe.js original840 deterministic framing cases, check_r12_contract.py negative original wrong rotation fixture, method preservation and speed cap analytic <=.25m per1/60s. CI PASS is NOT Unity/PhysX.
Native: existing5 Edit and3 PlayMode retained; 8 distinct new CAM01..CAM08 Light/Heavy/Tall/Wide ×1280x720/1920x1080, 242-frame 360 yaw and -8→18 pitch; CAM09 Tall 71-sample wall corridor with actual world sphere and 15m/s step; CAM10 removing an actual child collider. These 10 new tests require licensed Unity and must produce actual NUnit XML. Native R3 rigs, real LMB/RMB weapons, near-plane world mesh occlusion, FPS30/60/144, actual detach and screenshots require separate authorized Codex QA, not simulated/source-only.

---
# CAMERA001 tests — native execution required
G0: verify complete immutable source tree, GUID/.meta, scene script ref, paths, manifests, no gameplay edits; no collision with receiver target.
EditMode: Camera001FramingTests; 2 rigs x3 aspects x7 pitches x20 azimuth=840 geometry proposals, clearance, 720p/1080p aspect invariance, zoom limit/fit saturation, invalid inputs. Retain NUnit XML + Editor.log + process exit.
PlayMode: Camera001WorldCollisionTests; own colliders remain enabled, world wall shortens distance, smooth reset Light/Heavy. Retain raw logs.
Manual demo: Unity Play CAMERA001_Demo at 720p and 1080p, full yaw360, pitch -8..70, zoom3..18, Light and Heavy with tall weapon, wall, outdoor orbit. Capture PNG/video, HUD diagnostic states, reticle and clipping results.
Receiver regression (Codex only after G2): P12 game camera, actual aim ray/weapon fire, cursor and pause, garage/range/combat, part detach, collision around cover, PHY #125, Windows build.
B01-60 reruns the exact source SHA independently; fails mean B01-20 R2 fix + new SHA + retest. If licensed Unity unavailable => NOT_RUN / G1 BLOCKED, never fabricated PASS.

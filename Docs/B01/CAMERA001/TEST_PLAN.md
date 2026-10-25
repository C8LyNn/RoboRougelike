# CAMERA001 tests — native execution required
G0: verify complete immutable source tree, GUID/.meta, scene script ref, paths, manifests, no gameplay edits; no collision with receiver target.
EditMode: Camera001FramingTests; 2 rigs x3 aspects x7 pitches x20 azimuth=840 geometry proposals, clearance, 720p/1080p aspect invariance, zoom limit/fit saturation, invalid inputs. Retain NUnit XML + Editor.log + process exit.
PlayMode: Camera001WorldCollisionTests; own colliders remain enabled, world wall shortens distance, smooth reset Light/Heavy. Retain raw logs.
Manual demo: Unity Play CAMERA001_Demo at 720p and 1080p, full yaw360, pitch -8..70, zoom3..18, Light and Heavy with tall weapon, wall, outdoor orbit. Capture PNG/video, HUD diagnostic states, reticle and clipping results.
Receiver regression (Codex only after G2): P12 game camera, actual aim ray/weapon fire, cursor and pause, garage/range/combat, part detach, collision around cover, PHY #125, Windows build.
B01-60 reruns the exact source SHA independently; fails mean B01-20 R2 fix + new SHA + retest. If licensed Unity unavailable => NOT_RUN / G1 BLOCKED, never fabricated PASS.

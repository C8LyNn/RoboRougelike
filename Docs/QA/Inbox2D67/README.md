# INBOX QA â€” candidate 2d67ef3c

Source: PR #937, `2d67ef3ca4d5e5d094e7d7c588425cc86e703185`.

- Unity 6000.3.14f1 full PlayMode: 478 PASS, 0 FAIL, 0 skipped, exit 0.
- Targeted real-wheel AI regression: 10 PASS, exit 0. Before the fix, a diagnostic at df3f measured 0.034 m retreat in 2 s with 101/101 grounded samples. Collider penetration and 1 m Rigidbody sweep confirmed the inward path was clear; +/- tangent casts found the walls. Afterward a separate controlled physical test with real wheels confirmed retreat and the existing boundary/ram tests passed. Do not claim all possible corners or obstacles are covered.
- Windows x64 retail (`MOVSeriesBuild`, qa=False) built successfully (0 errors). All 213 files matched SHA256 after ZIP extraction. Normal hidden-window startup and WM_CLOSE exit 0. No human gameplay established.
- ZIP SHA256 `bc0d640cd5efe3b5aae13410a685b455d05b6cf257e0e2189424ff748c70d745`, 39,629,615 bytes. [Download](https://drive.google.com/file/d/1dAXpPmJS4XBHH5Pa1FQGih2UeOZz-uee/view?usp=drivesdk).
- Separate diagnostic Player with acceptance hooks: weapons and energy, 2 API-settled fights and rewards, save/load in separate processes with isolated slot; four process exits 0. API-driven, not manual gameplay, not evidence of six wins.
- Previously failing zoom, acceleration and six physical-encounter pilot tests on this SHA: 3/3 PASS in one targeted run, exit 0. The pilot uses virtual WASD/LMB, directly positions/aims the camera, and physical projectiles against active AI; rewards are API-assisted. This is automated and does not establish a human-playability verdict or exclude intermittent failures.
- Full unfiltered EditMode on this exact SHA: 1394 PASS / 1 FAIL / 1 SKIP, exit 2. The failed B01-24 test invokes UpdateVariantWeapons via reflection with no arguments, but the current method requires List<VariantGun> and string; this test did not execute its intended assertion. The skipped [Explicit] policy test requires independent HQ/B01-10 approval. Both remain open; no G1/G2 approval is implied. Earlier df3f full EditMode had 1390 PASS / 4 FAIL / 1 SKIP; zoom, acceleration and six physical encounters each passed in the 2d67 targeted retest.

Logs and XML redact user paths and session metadata; hashes of originals accompany published evidence. No source branches were merged.
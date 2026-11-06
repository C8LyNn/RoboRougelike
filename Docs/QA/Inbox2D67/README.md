# INBOX QA â€” candidate 2d67ef3c

Source: PR #937, `2d67ef3ca4d5e5d094e7d7c588425cc86e703185`.

- Unity 6000.3.14f1 full PlayMode: 478 PASS, 0 FAIL, 0 skipped, exit 0.
- Targeted real-wheel AI regression: 10 PASS, exit 0. Before the fix, a diagnostic at df3f measured 0.034 m retreat in 2 s with 101/101 grounded samples. Collider penetration and 1 m Rigidbody sweep confirmed the inward path was clear; +/- tangent casts found the walls. Afterward a separate controlled physical test with real wheels confirmed retreat and the existing boundary/ram tests passed. Do not claim all possible corners or obstacles are covered.
- Windows x64 retail (`MOVSeriesBuild`, qa=False) built successfully (0 errors). All 213 files matched SHA256 after ZIP extraction. Normal hidden-window startup and WM_CLOSE exit 0. No human gameplay established.
- ZIP SHA256 `bc0d640cd5efe3b5aae13410a685b455d05b6cf257e0e2189424ff748c70d745`, 39,629,615 bytes. [Download](https://drive.google.com/file/d/1dAXpPmJS4XBHH5Pa1FQGih2UeOZz-uee/view?usp=drivesdk).
- Separate diagnostic Player with acceptance hooks: weapons and energy, 2 API-settled fights and rewards, save/load in separate processes with isolated slot; four process exits 0. API-driven, not manual gameplay, not evidence of six wins.
- Full EditMode on this SHA is still running; no final result yet. Earlier 97f8 sharded EditMode had 1390 pass / 4 fail, including one unintentionally executed explicit B01 policy case. Camera zoom passed isolated; B01-24 fixture mismatch and policy remain open. Do not mark complete or assert independent B01 approval.

Logs and XML redact user paths and session metadata; hashes of originals accompany published evidence. No source branches were merged.
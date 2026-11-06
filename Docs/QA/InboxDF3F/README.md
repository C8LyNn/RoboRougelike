# INBOX QA â€” candidate df3f69fe

Source: PR #937, `df3f69fe57e75e74745d1d922e78eadbf0a73d8a`.

- Unity 6000.3.14f1 full PlayMode: 478 PASS, 0 FAIL, 0 skipped, exit 0.
- Targeted real-wheel AI regression on final code: 9 PASS, exit 0. Before the fix the disarmed enemy traveled 0.01477 m along the boundary in 1.5 seconds and failed; the final test runs 3.2 seconds and verifies grounded wheels and the logical boundary. No claim that all corner/obstacle layouts or inertia are covered.
- Prior 25-test AI/energy suite passed on an intermediate working tree, before the tangent-memory refinement; see its XML for exact tests.
- Windows x64 retail (`MOVSeriesBuild`, qa=False) built successfully (0 errors, 6 warnings). Its ZIP was locally extracted; EXE SHA256 matches the original. Normal hidden-window startup and WM_CLOSE exit 0. No human gameplay established.
- ZIP SHA256 `61aa973db8d22b763047dadbaec86df5ccf1aa209d14d20b647123bc46494e0a`, 39,629,609 bytes. [Download](https://drive.google.com/file/d/1W96a8hr0aI7aHSA8LtiTDnIpqQcMYgTa/view?usp=drivesdk).
- Separate diagnostic Player with acceptance hooks: weapons and energy 4/4, 2 API-settled fights and rewards, save/load in separate processes with isolated slot; all four process exits 0. These are API-driven, not manual gameplay, and do not establish six wins.
- Full EditMode on this SHA is still running. Earlier 97f8 sharded full EditMode: 1390 pass / 4 fail, including one unintentionally executed explicit B01 policy case. The camera zoom passed isolated; B01-24 fixture mismatch and policy remain open. Do not mark complete or assert independent B01 approval.

Logs and XML have user paths and session metadata redacted; hashes of originals accompany the published evidence. The source game branch and release branches were not merged.
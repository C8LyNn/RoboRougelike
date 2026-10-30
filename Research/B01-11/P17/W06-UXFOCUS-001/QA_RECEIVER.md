# B01-40 / B01-60 INDEPENDENT QA — receiver action card

## B01-40 (source G0)
Read all `manifest.json` hashes, bytes, original Git blob IDs from exact PR HEAD. Verify own `Research/B01-11/P17/W06-UXFOCUS-001/**` + dedicated workflow ONLY; source geometry reference `R1RunHUD.cs` pinned blob `049266e930fcb27062c0425dda248a970b681923`, scope exclusion Garage/pause/Codex save/controller; optional future install and rollback.

## B01-60 (independent source QA)
Checkout exact PR SHA independently, compile/run original 60 cases with .NET8, inspect `MenuNavCore.Step` code and menus:
1. Never activate stale focus / old rewards / unavailable resume.
2. Entry Confirm merely establishes focus without calling a game action.
3. Directional focus must not jump down to Quit when right of third card.
4. Key handling during live text edit or transition must never dispatch an action.
5. All focus IDs and actions must be deterministic and unique.
6. Verify menu coordinate mapping against original R1RunHUD.cs source.
7. Treat Unity game input, Player screen visuals, human keyboard accessibility as **NOT_RUN** until real authorized native/manual QA.

Producer is **not** B01-60. B01-40/60 independent comments/reports required before B01-00 may OFFER via CODEX INBOX #79. Source complete ≠ G0_PASS ≠ G1_PASS ≠ CODEx_ACK ≠ integrated.

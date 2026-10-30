# B01-12 RECOVERY-READOUT-001 / R2 — W25 actual UX correction

**Package** `B01-12-RECOVERY-READOUT-001/R2`, owner B01-12; HQ #731 PASS17 WAVE25. Strictly new-only `Research/B01-12/P17W25/RecoveryReadoutR2/**` + dedicated workflow; frozen producer R1 #771 `7628b6bd299c29f937c5d5aa383f58dad6945d92` never modified.

## Reproduced original R1 UX RED (not a gameplay exploit)

On original exact R1, `victoryId:null, freeAvailable:true` returns `ok:true` and displays "Darmowa odbudowa: dostępna" despite displaying "Nie potwierdzono zwycięstwa". Source original R1 test replay file and stdout in `r1-red-reproducer.cjs`. This is **misleading read-only status**, not evidence live game minted a coupon. R2 rejects `UNVERIFIED_FREE_QUOTA` before projecting. Full free lifecycle is authoritative B01-22/23/62 under one session, never this UI.

`victoryId` and `rebuildQuote` remain caller-supplied **UNAUTHENTICATED illustrative** fields. R2 replaces misleading "Potwierdzone zwycięstwo" and "Kwota zatwierdzona" phrasing with explicit source-data/nonverified labels; `freeAvailable` remains displayed as a *declaration* that ECON must verify. Pending same-encounter rebuild and loaner/core rules from R1 remain. Deliberate price=1 paid quote=1 rejected as not cheaper; 0 quote accepted as illustrative integer display. **R2 does not select economic rounding.** Cross-owner conflict B01-22 #764 half-up 1->1 vs B01-10 #686 floor 1->0 and owner cheaper-than-new requirement is a QA blocker for actual ECON, not fixed by this package. Nothing here writes Save, restores world, consumes loaner or grants Scrap.

## Reproduction and test

1. Node22 `node r1-red-reproducer.cjs` (fetches locally bundled exact original R1 engine+fixture, verifies frozen producer SHA256, demands the known R1 RED without pretending expected failure is product success).
2. `node test_engine.cjs` (strict manifest source hashes before executing code).
3. Python3.11 Playwright1.55 + real Chromium `CHROMIUM_PATH=/usr/bin/chromium python3 test_browser.py`; real four viewports and screen reader/DOM checks. Both standard suites require no JS exceptions.
4. In a **disposable** copy append a byte to `ui.js`; both unchanged positive suites MUST exit2 with `FAIL_CLOSED source mismatch: ui.js` before execution.

`manifest.json` pins original runnable byte lengths and SHA256. The dedicated read-only workflow repeats all steps and uploads receipts. `r1-original` contains **exact R1 code** only to make RED reproducible; SHA256 pins recorded in `r1-red-reproducer.cjs`, no edits to original frozen PR #771.

## Bounds, receivers, disposition

- **B01-40** original exact-head source/physical G0, every manifest pin, new-only paths, workflow read-only; safe own-only rollback preserving foreign/empty dirs and links.
- **B01-60** independently reexecute exact-head Node/real Chromium + negative original source and original R1 RED witness. Native Unity G1 **NOT_RUN**: research-only HTML and JS, no Editor test.
- **B01-10/22** receive *one specific approved price P=1 rounding mismatch* from real source #764; B01-22 exclusive source authority for policy repair. **B01-20/22/23/62** own authenticated gameplay UI binding/one real Save writer, no dual locks.
- **B01-00** future authorizations/integrator decision only after QA. Codex unavailable, ACK none, no merge.

Rollback: abandon this Draft or discard **only its new paths** from an isolated disposable checkout, never recursive shared path deletion. R1 source untouched and remains a distinct immutable version. Six lifecycle gates: IMPLEMENTED yes; TESTED producer only; QA_ACCEPTED pending; READY_FOR_INTEGRATOR no; INTEGRATED no; RELEASED no. SOURCE_G0 pending, NATIVE_G1 NOT_RUN, G2 HOLD, CODEX_ACK NONE.

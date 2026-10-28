# Issue #275 — combined native integration receipt

Game/build source `aa7a917f177a9b1f07203868fb7d1d871eb3ee46`; test-only corrections `e94f861825e66bb20881102f9f6c8ea2cc19e805`. Base PR #310 at start `231005c6d0351945a8ee6684f3856af118f0e94f`. No main/develop merge. Unity 6000.3.14f1, Windows x64.

## Native results

- Compile: PASS, process exit 0. Windows build: PASS, process exit 0.
- Full EditMode on `aa7a917f`: **FAIL, 1288/1303 passed, 15 failed, 0 skipped, exit 2.** Raw XML/log retained; this is not reclassified as a full-suite pass.
- Twelve failures were legacy full-price rebuild expectations contradicted by approved cheaper rebuild. Test-only commit corrects their concrete expected charges.
- Of the other three failures, the zoom threshold failed once (1.0355m versus <1m), passed on two later runs; camera responsiveness remains an intermittent/unresolved risk, not fixed. Two physical-input pilots failed because they held primary fire through a transition whose existing input guard requires release. Test pilots now release then press per actual guard, without reducing fight/impact/victory assertions or extending timeouts.
- A targeted rerun of all 15 original failures with these test-only adjustments: **15/15 PASS, exit 0**. The six-encounter test used normal virtual LMB and physical hits with active AI, not a human playthrough. The heavy boss test used API fixture damage in the first five stages and genuine physical combat for the boss, won in 21.26 seconds. One interim retest had 12/15, with a loot test failing following previous failed pilots; that loot test passed alone and in final retest. Full EditMode on the later test-only commit was NOT_RUN.
- Full PlayMode on `aa7a917f`: **474/474 PASS, 0 skipped, exit 0**.
- Extracted Windows Player, real game in hidden batch mode: garage/remount/combat entry, map save/R6 ramp/return, MOV009 status and physical force **3/3 PASS, each process exit 0**. These are API-driven automated checks, not visual/manual testing.

Evidence: process JSON, original NUnit XML, targeted retest XML, compressed Editor/Player logs and batch Player receipts are under `Evidence/`. Human GUI at 720p/1080p, moving-target long-range aim, framewise camera motion and independent B01-60 G1 remain NOT_RUN. No G2 asserted. The earlier camera/aim diagnostic is in `Docs/CX0102/QA.md`; reported intermittent gameplay fault was not reproduced sufficiently to justify camera or weapon changes.

## Delivered fixes and handoffs

CX-03 `d5962a2aa5c6c3c72c9c83a4e3267045fef467e0` malformed weapon transactions; CX-04 `8bbd88630380f45b8e72398d6676294789f6239e` map naming/overwrite; CX-05 `f0047085123389487c597cd49426321182916673` owned rebuild; CX-06 `7a9af454f3a667db8e40d349a6683918f8c05b5b` critical warnings; CX-07 `46e0965cc587b5da8b8d83d4431e7a23e1507d3a` group remount and `aa7a917f177a9b1f07203868fb7d1d871eb3ee46` camera-loss trigger cleanup; CX-08 `a31a1853097da6d20bb1144f4ee7ea6259da758f` hidden floor capacity. Each was reported with test results to Issue #79 for the named B01 receivers. Independent B01-22/62 source gates remain pending; the B01-25 LD02 data loaded in Unity but 8-ray/real vehicle level acceptance remains NOT_RUN. Delayed/free repair queue requires policy and is not implemented. Price=1 owned wreck cannot rebuild until a positive lower-than-purchase policy exists.

## Windows package

[RoboRougelike_CX_Windows_x64.zip](https://drive.google.com/file/d/1D8tCALdstlIYEivxFXdunrRHdALNBzN9/view?usp=drivesdk), 39,623,366 bytes, SHA-256 `a4e1054e09066bb6cd27d3985994ac1a66bcc85caf7ff202af8f59e340bdf442`. All 211 extracted files matched original build SHA-256. Unpack the full archive and run `RoboRougelike/RoboRougelike.exe`. Manual check: garage no guns/wheels and return, two firing groups after remove/remount, camera/zoom and 10/30/60/100m shots, map save overwrite confirmation and global floor, owned 0HP rebuild price, MOV009 modes.

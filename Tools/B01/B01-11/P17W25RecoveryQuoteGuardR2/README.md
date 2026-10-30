# B01-11 — PASS17 / WAVE25 — RECOVERYHUD001/R2 PRICE-TRUTH GUARD

## Confirmed W24 production UX defect and narrow patch
Original immutable B01-11 [W24 #765 @ d1a41d60cd74eefa96915f0c450ce36e70737efd] Unity C#9 RecoveryNoticeBuilder accepted `OriginalPartPriceScrap=1` and `HostQuotedPaidScrap=1` or `price=100/quote=64`, displaying the owner quote as if it obeyed the approved 65% AND strictly-cheaper-than-purchase contract. Both W24 price-policy candidates [B01-10 #686] (floor) and [B01-22 #764] (half-up) are still under owner resolution. R2 **does not choose a rounding policy** and it does NOT implement a quote engine.

Original production RecoveryNotice.cs now accepts the owner's already-provided quote ONLY when `purchase>0`, `0<=quote<purchase`, and `quote` matches either `floor(65*p/100)` or `floor((65*p+50)/100)` using 64-bit intermediate arithmetic. For price=1 only quote=0 is admissible. Exact p100/q65 valid; p100/q64 invalid. For 15 Scrap the currently disputed quotes 9 and 10 are both allowed, but only B01-10/22/HQ may select one policy. **Owner-authoritative economy must enforce exact quote and charge under one physical Save .owner; client display never authorizes repair.** Invalid or out-of-policy quote displays a fail-closed unknown state, never an affordable-looking price.

## Direct original-source RED->GREEN tests, no C# surrogate
- New original .NET8 test project compiles the modified ACTUAL Unity-facing production RecoveryNotice.cs, 28 named boundary/security checks plus 400 deterministic price/rounding/strict-discount cases for purchases 1..100 and Int32.MaxValue.
- New `test_r1_red.py` reads the pinned **actual original #765 Git blob** (SHA1 1bb118783458de15584a913feb29c07ade7b4fc2), compiles it in a separate disposable temporary .NET8 project, and observes original source R1 product RED for `1->1` and `100->64`; compiles updated R2 source separately to prove GREEN rejection, and positive `1->0`, `100->65` preserved. Zero repo edits/scratch leaking. This is test evidence, NOT authorization to modify #765 historical branch.
- Original W24 202-case executable C# direct runtime + synthetic Unity UI presenter tests continue unchanged (no old assertions removed). Two OS GitHub CI checks source manifests, six original Unity `.meta` GUIDs, repository source scope, original and R2 tests.
- W25 R2 versioned manifest pins every new W25 code/test/doc/workflow SHA256 and exact bytes, while historic own W24 source manifest re-pins modified own production and verifier sources. Old parent original SHA and branch are frozen.

Run in authorized isolated checkout from current R2 HEAD:
`python Tools/B01/B01-11/P17W24RecoveryHud001/verify_package.py`
`dotnet run --configuration Release --project Tools/B01/B01-11/P17W24RecoveryHud001/RecoveryHud.Tests.csproj`
`dotnet run --configuration Release --project Tools/B01/B01-11/P17W25RecoveryQuoteGuardR2/QuoteGuard.Tests.csproj`
`python Tools/B01/B01-11/P17W25RecoveryQuoteGuardR2/test_r1_red.py`

## Ownership and integration contract
R2 stacked Draft ONLY on OWN B01-11 #765 exact SHA; only modified originally owned recovery source, verifier and original manifest; new own W25 QA/README/workflow/manifest. No other agent assets, branch changes to #765, main/develop, owner-selected #749, possible successor #773, ALT #753/#741, original #772 Unity physical port, frozen #510/#556/#690/#729, ECON #686/#764 files or unrelated code. No merge, no user PC.

B01-10 + B01-22 OWN resolution of `65%` rounding at price 1, 15, 30 (and cap to strictly cheaper); B01-20 actual UI+game gateway; B01-23/62 only real canonical owner/world+Save+wallet+loaner persistence; B01-40 independent original Git ZIP/GUID/source G0; B01-41/61 independent regression; B01-60 actual activated native Unity G1; B01-00 coordination. A view hints no financial safety/security authority. Original Unity6 source is compiled by .NET8 with synthetic Unity UI stubs for old W24 harness; no genuine Unity/PhysX/NUnit Edit/Play has run.

Six stages IMPLEMENTED=YES | TESTED=source when verified | QA_ACCEPTED=NO | READY_FOR_INTEGRATOR=NO | INTEGRATED=NO | RELEASED=NO. SOURCE_G0=PENDING independent; NATIVE_G1=NOT_RUN; CODEX_ACK=NONE; G2=HOLD; INTEGRATOR_FEEDBACK=NOT_MEASURED.

Safe rollback: discard separate R2 child Draft and own branch without touching original #765 or frozen owner code. Never auto-delete shared source or import to owner PC.

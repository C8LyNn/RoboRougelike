# Conditional source checks, not admitted gameplay

Native Unity snapshot084a64292c0c0db1dd54151b30674d454319e8e9: compile0, Edit22/22. Breakdown:7actual receiver reward cases (including raw-policy bypass RED→GREEN),3unified menu/input cases,6new CAMERA R1.2.1 diagnostic cases,3LEGENDARY cases,3ECON cases. No surrogate CI is counted as native. Actual logs/XML in RC2-ConditionalSources-RewardBoundaryGREEN-20261026T113656Z.

## CAMERA #251 52b5930a04a6e5d9f2fc28fa5b616036b61f0e44

Complete51-entry ZIP61,246B, SHA256b5d74b724d4479a39c8dd8f19414c50b62ad3418782c6f7d67dfeedcbb1e1099; source verified, old parent GUID preserved instead of CAMERA's conflicting parent. Existing Camera001Rig.cs blob is unchanged from R1.2 #247: this revision adds passive diagnostics, not a behavior fix. Six new diagnostic tests pass natively. Existing5Edit+13Play and R1.2 camera probes retain actual external evidence QA249; combined Light tracking still FAILED there. New math supports a finite170deg/s slew hypothesis but matched BASE/R1.1/R1.2 actual aiming comparison remains NOT_RUN. No broad weapon/camera rewrite or assertion relaxation. Keep default P12 camera; remove this conditional package from final Assets/build until that requirement is resolved. Formal HOLD alone is not the reason for exclusion.

## LEGENDARY #242 dffabbc6960304fdb33322b5a23c884feac804c6

Complete16-entry Unity ZIP10,959B, SHA25614eccb49f69b2e67ecb34008fe19689b1b66e629ed94491224fb327216fc1fb2; per-source/meta/GUID pass; native3EditPASS. Actual gameplay dependencies are absent: certified typed WeaponBase/MobilityHub, physical3x2x2/3x3x2 definitions, approved masses/abilities and integrated mutation gates. Tagging ordinary one-cell modules would not implement those functions. No invented ability/mass or second inventory. Remove conditional Assets from release; retain source/tools/evidence for producer handoff. Game PlayMode/real legendary use NOT_RUN, not PASS.

## ECONREBUILD #252 12572117a3dce369257feabc57021cde58023369

Complete16-entry Unity ZIP21,422B, SHA256fdcb15e6b394715f450771cac798b1c02b8efe1f33acb815e9e0957c46bd2d87; native3EditPASS. Active integration needs a defined paid quote, one atomic wallet/reservation ledger, gates on every repair/sell/mount/deploy path, and durable host+metadata checkpoint. The game currently has no run Save/Load. Existing immediate R3 repair does not satisfy delayed rebuild after the next actual victory. No second wallet/queue inventory or invented economics. Remove conditional Assets from release; ordinary repair/recovery remains active. Game PlayMode/rebuild lifecycle NOT_RUN.

## KEEP3 #257

Admitted via canonical R4, not a duplicate inventory or substitute economy. Full source6bb6796ee3ad88a015fb6c8d033023c0ee3b5e94 is unchanged; original R3RewardPort.install.cs reused byte-identically. Source2nativeEditPASS; receiver7native integration casesPASS after the controller-only commit guard. Same physical GUID travels from enemy equipment through projectiles/salvage to inventory. One mandatory bonus plus0..3nonArmor/Core items, atomic capacity refusal/retry, authentic victory and replay defense; all actual ownership lives in R3. This is session transaction behavior; durable restore is not claimed.

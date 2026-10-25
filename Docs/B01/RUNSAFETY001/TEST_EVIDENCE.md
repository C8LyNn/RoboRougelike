# RUN-SAFETY-001 R1 real evidence vs not-run
Producer checked published PR#125 C# source for: EmergencyRepair, RepairAffordable, RepairInventory, Repair, CanDeploy, StartNextEncounter; confirmed immediate 0HP healing and deploy gate conflict with approved ECON #136.
Actual source generated in isolated GitHub branch: RunSafetyState.cs, NUnit EditMode 17 authored test methods, Unity PlayMode 3 authored coroutine tests, opt-in Demo Unity scene and API/docs.
Native Unity compile/PlayMode/EditMode/manual demonstration: **NOT_RUN**, no Editor.log, XML, screenshots or Windows build; no system Unity/C# compiler available in producing environment.
Independent B01-60 native tests: PENDING.
B01-40 GUID/SHA/target overlap: PENDING.
HQ temporary loaner policy: RECOMMENDATION / AWAITING_APPROVAL.
When a source issue is discovered, record exact SHA defect and R1.1 iteration with new hash and new test. No assumption of PASS based solely on the existence of test scripts.

## Producer R1.2 source correction — no native claim
- Case price=1, rebuild quote=0 is valid strictly-cheaper ECON pricing; earlier price lower bound incorrectly required purchase>=2; fixed in commit `9bcec12e744c18bd2acaa2ff39e42d45a180a91f` and authored negative test adjusted in `fcd29591299217a521c1a799e8c285bda017728e`.
- Second source defect found: when free slot was already consumed, a *separate paid quote of zero* was charged zero correctly but erroneously returned `QueuedFree`, conflating free-slot accounting. Corrected by tracking `consumesFreeSlot` explicitly in `c846ec90e19049ac2630a759b856a3ccca8aefe4` and added `ZeroPricePaidRebuildDoesNotDoubleClaimFreeSlot` regression source in `7dd3cc7436a901f86782019a99d955682ca1424e`.
- Current authored tests: **17 EditMode + 3 PlayMode** = 20 declared cases, **0 native executed**. This is SOURCE review/fix, not physical RED→GREEN.

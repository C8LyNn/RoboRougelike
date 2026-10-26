# B01-23 W14 — BUILD R1.4 exact native acceptance (no new gameplay package)

**Why:** RC2 [PR #258](https://github.com/C8LyNn/RoboRougelike/pull/258) found two genuine nested-socket geometry failures in R1.3. B01-23 [R1.4 source PR #260](https://github.com/C8LyNn/RoboRougelike/pull/260), exact `95707c68c8071a949b5d9fec7ff57926e92f8198`, fixes interval intersection. Producer C# surrogate 16/16, but **the genuine R1.4 Unity native case has NOT_RUN**. W14 does not modify frozen R1.4 runtime or regenerate the package.

**Reuse, do not duplicate:** This BUILD-owned script is a *read-only acceptance profile* for existing **B01-60 QA-NATIVE-001** `Run-NativeQA.ps1` and `qa_gate_v2.py` from [B01-60 Draft #245](https://github.com/C8LyNn/RoboRougelike/pull/245), not a competing generic runner. Only B01-60/Codex with a *licensed native Windows Unity 6000.3.14f1* host can run the actual tests.

## Frozen, real source inputs

- R1.4 source SHA `95707c68c8071a949b5d9fec7ff57926e92f8198`.
- Original **complete** R1.4 60-entry ZIP SHA256 `62321794e8f91f1660c32597632b40b9bccdbab1becd3c7e750632f06f8714aa`, not the GitHub outer wrapper: [Actions artifact #12651256344](https://github.com/C8LyNn/RoboRougelike/actions/runs/40325389893/artifacts/12651256344), inner `B01-BUILD001-R14.zip`.
- **Unmodified independent RC2 negative/positive tests** `BuildRC2SafetyTests.cs` from [RC2 code snapshot](https://github.com/C8LyNn/RoboRougelike/blob/5ef18ff27fc3ae4228acfad1e0f132db7c66c0d9/Tools/B01RC2/Blocked/BuildRC2SafetyTests.cs) Git blob SHA1 `7266569bb447a73a523521fa68d1bf615d06ceeb`.
- B01-60 generic runner source/version is separately owned; this profile NEVER modifies it. The R1.4 zip **must not** be replaced by a ZIP generated from this W14 QA-only branch.

## One licensed Windows native validation slot

**Preconditions:** an authorized, disposable (never active Codex) Unity 6000.3.14f1 checkout containing the *full pinned BUILD001 Assets tree* from the R1.4 inner ZIP, preserving .meta GUIDs; the original unmodified RC2 receiver C# test copied into **that disposable project's** `Assets/B01Construction/BUILD001/Tests/EditMode/BuildRC2SafetyTests.cs`. This test file must be in the existing `B01.BUILD001.EditModeTests` assembly. The build scope is never merged and never installed into active R3/R6/Pack. Avoid import to an occupied live Unity project; the handler is read-only and refuses source collisions.

1. Create empty marker `.b01-60-disposable-qa` at the disposable Unity project root. Verify `ProjectSettings/ProjectVersion.txt` states Unity 6000.3.14f1, no `Temp/UnityLockfile`, and note its exact `git rev-parse HEAD` **target SHA**. Ensure an unused evidence folder *outside* the project.
2. Generate the exact pinned source+original receiver plan (write outside the checkout; refuses overwrite):
   ```powershell
   python Tools/B01/BUILD001/W14Acceptance/prepare_build_native.py --project 'C:\QA\RRL-BUILD-isolated' --source-zip 'C:\QA\packages\B01-BUILD001-R14.zip' --target-sha '<exact disposable target HEAD SHA>' --out 'C:\QA\reports\BUILD_R14_plan.json'
   ```
   This compares every Unity asset byte to the **actual** producer ZIP, validates all SHA256s, checks original receiver C# Git blob, and refuses wrong Unity version or active/unmarked project.
3. Run unchanged B01-60 generic runner (external dedicated QA toolkit checkout) with that plan:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File 'C:\QA\B01-60\Run-NativeQA.ps1' -ProjectPath 'C:\QA\RRL-BUILD-isolated' -UnityExe 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe' -PlanPath 'C:\QA\reports\BUILD_R14_plan.json' -EvidencePath 'C:\QA\reports\BUILD_R14_native_run_001' -TimeoutMinutes 30
   ```
   No PlayerExe: this targeted gate covers **Compile + EditMode + PlayMode**, not Windows Player UI gameplay, 720p/1080p screenshots or real canonical R3 installation. These remain **NOT_RUN** separately.
4. Validate the raw evidence with *both* B01-60 generic `qa_gate_v2.py` (already invoked by the runner) and the BUILD-specific verifier:
   ```powershell
   python Tools/B01/BUILD001/W14Acceptance/verify_build_native.py --plan 'C:\QA\reports\BUILD_R14_plan.json' --evidence 'C:\QA\reports\BUILD_R14_native_run_001' --out 'C:\QA\reports\BUILD_R14_native_verdict.json'
   ```
   This additionally requires **exactly 35 genuine EditMode cases** (R1.3 existing 18 + R1.4 14 + original RC2 3), **6 genuine PlayMode**, and original RC2 nested-negative, nested-positive and exact-area controls. It must see original NUnit XML, Editor.log, exit codes, SHA and runner attribution; no missing/failed/skipped test or zero-test PASS. It reports `COMPLETE_EVIDENCE_REVIEW_REQUIRED` rather than independently declaring Unity G1.

## Ownership and handoff

**B01-23:** produce source-only correction if independent native RED demonstrates a real R1.4 defect; never preemptively edit working engine. **B01-60:** independent witnessed Unity G1 or honestly `NOT_RUN`. **B01-40:** G0 source/target GUID and import/rollback, distinct from native evidence. **Codex:** full canonical R3/R6 vehicle/garage and Windows Player regression only after B01-00 authorizes the relevant integration slot. **B01-00:** G2 only after these distinct gates. No merge, release or modification of active Codex worktree by this tool.

### Structural selftests (not native)

`python Tools/B01/BUILD001/W14Acceptance/test_w14_acceptance.py` checks corrupt archive, unsafe path, absent marker, tampered receiver blob, synthetic NUnit negative/positive counts and absent/false evidence. They never simulate actual Unity execution or claim G1 PASS.

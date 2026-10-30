# B01-32-P17-W09-REPAIR-WELD-RUNTIME-QA-001/R1

## Purpose
**Close a real acceptance gap, do NOT create a fifth cosmetic effect.** This independent *producer-side* runtime-harness PR consumes the **actual physical and frozen** [Repair Weld #432](https://github.com/C8LyNn/RoboRougelike/pull/432) **exact HEAD `280ed3f59020432132ea5e156c68590d445009c8`**, [physical GitHub Actions artifact #12966020486](https://github.com/C8LyNn/RoboRougelike/actions/runs/41030815153/artifacts/12966020486). Actual nested Unity ZIP immutable SHA256: `0d82429073722f3402e25d056502917ec7dcd2fb62256355011457f58711d2ed`.

The frozen original source and archive remain **read-only, unchanged**. This separate branch modifies only `Tools/B01-32/P17/W09WeldRuntimeQA/**` and a dedicated CI workflow. It never touches `Assets/**`, Unity canonical project, #432/#446/#462 or Codex code.

## What actually runs
1. GitHub Actions checks out this QA branch, fetches **exact original #432 PR head**, fails closed if the head moves, then downloads the **original physical ZIP artifact**, never regenerates its source.
2. `intake.py` compares actual 17 physical Unity source ZIP entries with the original receipt hashes; verifies 11 GUIDs, prefab script GUID binding, compares three original C# source bytes **exactly to `git show` of immutable SHA**; stages the original C# and prefab ONLY under `build/W09WeldReceiver/Original/` in Actions.
3. `dotnet run -c Release --project Tools/B01-32/P17/W09WeldRuntimeQA/Runner/Runner.csproj` compiles **all three original C#9 sources unchanged**, including `B0132RepairWeldVfx : MonoBehaviour` and the two authored `[UnityTest]` methods. It executes the original NUnit IEnumerator bodies with a **test-only `UnityEngineMock`**, then additional real source lifecycle tests: nine allocated LineRenderer components, unique arc/eight sparks, correct finite position, opacity cooling, explicit VFX OFF/ON, disable/cancel cleanup, primary+fallback shader selection, independent instances, material destruction, 512 source adapter randomized trajectories.
4. `mutate_and_rerun.py` deliberately corrupts the **temporary extracted** original MonoBehaviour source in four ways (skip cancellation, disable cleanup, leave stale sparks), recompiles and MUST see actual assertion failures; **restores original exact bytes** and requires GREEN again.
5. Artifacts are source and test receipts, no new visual prefab, no new geometry or images. No direct changes to previously frozen game-side code.

## Important nonclaims
Test stubs are **not** UnityEngine. Running original authored `[UnityTest]` method bodies under an IEnumerator simulator **is not a native Unity PlayMode test** and does **not** establish shader/URP, render sorting, Unity import or GPU correctness. This is narrower than real licensed Unity G1, but substantially stronger than syntax-only Roslyn or isolated pure-C# core QA.

**Required B01-40/B01-60:** independently confirm exact physical producer ZIP/GUID/script/current Codex conflicts/rollback; run Unity 6000.3.14f1 URP, original two PlayMode tests with XML, Editor.log and screenshots. Return native defect to B01-32 (do not edit frozen source except formal RETURNED_FOR_REWORK). B01-00 alone schedules optional #79 Codex integration; do not assume ACK. Rollback of this QA-only PR: delete this isolated QA folder/workflow; frozen #432 physical asset and all game sources untouched.

**Source G0 proof** will be exact final Actions after CI success; **native Unity G1 NOT_RUN; CODEX_SENT=NO; G2 HOLD**.

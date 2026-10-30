# B01-60 · PASS17 WAVE23 · Exact corrected R5 seven-case native Unity handoff

**PACKAGE_ID:** `B01-60-P17W23-R5-FIXED7-NATIVE-001`. HQ [#731](https://github.com/C8LyNn/RoboRougelike/issues/731), owner authorization [#667](https://github.com/C8LyNn/RoboRougelike/issues/667), original selected frozen R5 [#690 @ `cc7f73541517a18f4479bb155534bd4832d9985e`](https://github.com/C8LyNn/RoboRougelike/pull/690), **NEW owner-corrected fixture-only** [#732 @ `0bee4ce9928354433ec719c45a8186378361d46e`](https://github.com/C8LyNn/RoboRougelike/pull/732).

This is a **new B01-60-only versioned native receiver**. It explicitly reuses the immutable source, stage/rollback, private Unity PlayMode checkpoint-probe, Unity -runTests, NUnit/Editor.log validation and original GUID/source guards of [original B01-60 #718 @ `481bfb1cf2804d94fb49d7e6a57134e96c535b86`](https://github.com/C8LyNn/RoboRougelike/pull/718) and its original hardened W12 runner, rather than replacing the game, Save/Load or earlier agent QA. The new owner #732 corrects ONLY the R5 ColdRestart NUnit fixture's original generation expectation: original g4 Save becomes g5, fixture now decodes original #556 wire and restores g5. The ORIGINAL seven Unity PlayMode test names are retained. No R5 runtime code, original host lease or game physics is changed.

**Source gate:** actual original Git #732 must be an exact #690 descendant with exactly four allowed owner changes (fixture, authored workflow, README and source fixture unit test). Assert original Unity host/AtomicWorldSwap/OwnerWorldLeaseRetirement/#556 originals have identical pinned Git blobs; only native fixture Git blob changes from `c8c603451a0668d50ba3af91ed888a64910bdc9f` to `d096f65eeb37393204a35445dd87cbdffa642ce0`. Keep #693 ALT_HOLD, other canonical alternatives, #729 V3 authority and #712/#669 storage **out of this isolated R5 source package**. Owner #732 formal G0/independent QA may still be pending; source preflight is **not** G0 approval.

## Owner's own Windows PC — one command

Prerequisites: safely reviewed W23 Draft checkout, Python3, Git, original historical **#732** Git commit already available **locally** (runner never automatically fetches it), legitimately installed and activated Unity Hub Editor **6000.3.14f1** with Unity Test Framework/Physics. No direct PC/SSH access is available to ChatGPT.

Source-only (recommended first, does not start Unity):

```powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W23FixedR5SevenNative\Invoke-B0160W23Fixed7.ps1 -RepoPath .
```

Only **AFTER independent B01-40 source G0 of exact #732 and B01-61/41 source security review**, the owner may opt into physical native Unity/PhysX in seven separate disposable, checkpoint-isolated Unity projects:

```powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W23FixedR5SevenNative\Invoke-B0160W23Fixed7.ps1 -RepoPath . -Run -SourceG0Reviewed
```

Requires separate interactive typing `B0160 R5 FIXED7 G0 REVIEWED NATIVE`. `-SourceG0Reviewed` only records **operator's claim of having reviewed G0**, NOT automatic formal source G0 acceptance. Optional exact Hub Editor path `-UnityPath 'D:\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe'`. Windows PowerShell 5.1 may substitute `powershell` for PowerShell7 `pwsh`.

Runner stages **seven** individually unique `B0160W22Case<index><nonce>` private Unity company/product scopes into a new random disjoint scratch under Windows TEMP, never in canonical Git checkout. It first executes `B01.UnitySaveBridge001.Tests.B0160W22ScopeProbe.OnlyPrivateSavePath`, checks the original authoritative checkpoint does not already exist in the private `Application.persistentDataPath`, then runs the matching one ORIGINAL owner #732 PlayMode R5 NUnit test. The set includes corrected `ColdRestart_RestoresRealRigidbodyAndConsumedIds` and six existing R5 physical/lease/ID/rollback tests. Each Unity invocation records actual native Editor.log, stdout/process log and NUnit3 XML, exact original test identity, real Unity version, exit code; source files checked unchanged after each process. Fails closed on missing license/executable, wrong Git blob, wrong test count, blocked source or wrong consent.

**No admin, credentials, SSH, permanent Windows service, automatic network fetch, auto-upload, cleanup of user/foreign directories, protected push, game install or merge**. Source-only stage/rollback deletes only disposable own scratch project files using the previously hardened strict original safe rollback; source-only evidence is kept in a unique random folder. Native Unity caches/evidence remain private until the owner voluntarily inspects/shares redacted logs.

**Evidence:** `P23-fixed7-receipt.json`, `P23-fixed7-sha256-manifest.json`, seven `guard{i}.xml` and `r5case{i}.xml`, corresponding Editor and process logs, selected/source SHA, 7 private project identities. The receipt's hash list is **self-integrity, not a cryptographic signature**, and source success is NOT native success. Even after genuine 7/7 runner success the status is `HOLD_PENDING_INDEPENDENT_SOURCE_G0_AND_OWNER_ATTESTATION` until evidence independently reviewed. Actual user Unity G1 is presently `NOT_RUN` with no genuine Editor.log/NUnit XML.

## QA, release gates and receivers

```bash
python -m unittest discover -s Tools/B01/B01-60/P17W23FixedR5SevenNative -p test_fixed7.py -v
```

15 direct owner original Git/C# fixture contract, seven scratch stages and rollback, foreign EMPTY directory preservation, missing consent and actual standalone source preflight tests. CI Windows2025/Ubuntu24.04 + true PowerShell script source-only, negatives for missing Unity and unsafe paths. CI intentionally has **no Unity Editor/PhysX**. The historical W21 V2 2 EditMode+14 PlayMode suite is independently operated by #711. The original #718 R5 six-of-seven partial suite and independent W23 raw receipt auditors #734/#735 remain separate; do not mix alternative host/Save implementations.

**SOURCE_G0:** `#732_PENDING_INDEPENDENT_FORMAL_G0`; `NATIVE_G1=NOT_RUN`; `R5_FULL_G1=NOT_RUN`; `CANONICAL_729_V3_712_669_G1=NOT_RUN`; `LIVE_510_PRIVATE_RIG_SAVE=HOLD`; `G2=HOLD`; `CODEX_ACK=NONE`; `INTEGRATOR_FEEDBACK=NOT_MEASURED`; no authorized merge. Owner B01-23 corrects fixture and publishes source; B01-61/41 independently test original source/host & receipts; B01-40 formal exact new source/GUID physical intake; B01-00 owner instructions; B01-60 native evidence only after genuine manual operator execution. Safe rollback = discard only this W23 QA-owned Draft PR/files; never touch owner frozen source or user's project.

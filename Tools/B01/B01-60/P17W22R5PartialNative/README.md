# B01-60 / PASS17 WAVE22 — Original R5 Unity partial native G1 handoff

**PACKAGE:** `B01-60-P17W22-R5-NATIVE-PARTIAL-001`. Exact selected frozen host **#690 @ `cc7f73541517a18f4479bb155534bd4832d9985e`**, parent original W21 operator [#711 @ `02272e8b0facc00d3943672770251b1d01182b85`](https://github.com/C8LyNn/RoboRougelike/pull/711). No changes to host #690, canonical #691, frozen #510/#556/#596/#642/#647, W21 #711 or competing ALT #693/#652. New W22 QA-owned files only; no main/develop writes, no merge.

## Proven R5 native-fixture blocker — cannot call seven of seven PASS

The **exact ORIGINAL R5 NUnit** `ColdRestart_RestoresRealRigidbodyAndConsumedIds` restores a checkpoint after `TrySaveBytes` but incorrectly asserts generation **4**; the exact ORIGINAL R5 `AtomicWorldSwap` performs `next=newest+1` and encodes `RunSnapshot(...,next,...)`, therefore it emits generation **5** from a generation 4 world. This is a deterministic source-internal test contradiction, **NOT** observed running Unity. No frozen host or fixture is modified or silently overlaid. The source test pins the original Git blobs and expects this mismatch to remain a release HOLD. B01-23 must publish a distinct owner-versioned corrected fixture and independently QA it, then B01-40/61/41 retest exact corrected SHA before full R5 native acceptance.

The W22 receiver can safely prepare/optionally execute **six** remaining ORIGINAL #690 PlayMode tests (one per isolated project, unique private Unity company+product) after each project's dedicated private Application.persistentDataPath guard. One of these is actual `ComponentOnlyDestroy_RetainsLeaseUntilRealChildRigidbodyDestruction` for #670 R5 lifecycle; another `DisabledSameSlotHost_CannotStealOwnerUntilDestroyed`. Each exact NUnit full name, real Unity Editor.log/Unity version, process exit and staged source-byte invariants are checked; any mismatch fails closed. This 6/7 partial suite CANNOT accept product R5, canonical game Save/Load or G2.

## Operator command (from local repo root on own Windows PC)

First safe source-only mode; requires local Git with already available original R5 history and Python3, no Unity:

```powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W22R5PartialNative\Invoke-B0160W22R5.ps1 -RepoPath .
```

Opt in for licensed Unity 6000.3.14f1 and ONLY original six R5 cases after independent source G0 reviewed:

```powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W22R5PartialNative\Invoke-B0160W22R5.ps1 -RepoPath . -Run
```

Windows PowerShell 5.1 may use `powershell` instead of `pwsh`. Exact Hub Editor default `C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe`; override `-UnityPath 'D:\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe'`. Legitimate Unity Hub activation is manual, no bypass. The `-Run` script requires the exact typed phrase **B0160 R5 NATIVE PARTIAL** before launching Unity; no credentials/admin/SSH/remote service. Python direct native also requires matching consent argument. Avoid running untrusted external PR code on owner machine.

Default scratch is a fresh per-run `B22-<random>` folder under Windows TEMP, disjoint from the Git checkout. All six generated projects are original pinned R5 source plus a strictly QA-only scope guard and individually unique PlayerSettings. There is **no deletion, upload, remote fetch, protected push, global Git configuration modification or user checkpoint access**. Review/possibly redact local logs before deliberately sharing. Source-only preflight stages then safely rolls back its OWN disposable files, with an explicit foreign EMPTY-dir test that fails closed.

Evidence: original git SHA, all authored test names, selected six scope, mismatch HOLD, original raw XML/Editor.log/Unity return log per test if manually run, SHA256 report (self-integrity, **not cryptographic attestation**), `P22-receipt.json` and `P22-sha256-manifest.json`. The old V2 2 EditMode+14 PlayMode native suite remains exclusively under #711; W22 is separate and not an alternative selected host.

## QA and blockers

`python -m unittest discover -s Tools/B01/B01-60/P17W22R5PartialNative -p test_r5_native.py -v` performs direct original Git blob, 7 authored NUnit identities, R5 source contradiction, 34+ GUID, six-private-project physical stage/rollback, foreign empty directory refusal, and missing consent/Unity-negative source checks. CI executes source tests Windows+Ubuntu; Windows additionally executes actual PowerShell safe preflight and negative cases **without starting Unity**. CI source-green **does not mean** native Unity/PhysX ran. Actual user PC action is separate and not available in ChatGPT.

**RECEIVERS:** B01-23 owns generation fixture correction on NEW version; B01-61/41 validate host lifecycle and failed retirement; B01-40 independently checks source G0 + safe migration; B01-00 handles owner instructions; B01-60 reviews genuine operator logs later only when provided. Canonical #691/#712 source authority P0 blockers remain independently HOLD. `SOURCE_G0=R5_699_SOURCE_ONLY; NATIVE_G1=NOT_RUN; FULL_R5_G1=HOLD_COLDRESTART_SOURCE_CONTRADICTION; LIVE_SAVELOAD=HOLD; G2=HOLD; CODEX_ACK=NONE; INTEGRATOR_FEEDBACK=NOT_MEASURED`. No merge.

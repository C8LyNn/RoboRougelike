# B01-40 W58 — executable-operator-independent G1 evidence intake contract
Status: **INTAKE_SPEC_READY / NEW_CHL_SOURCE_G0_DELTA_PASS / CHL_OPERATOR_REPIN_REQUIRED / LICENSED_NATIVE_G1_NOT_RUN / G2_HOLD**. This is a checkable *receiver acceptance contract*, NOT a third package, not B01-60's G1 verdict, not an HQ license grant, and not a command to use Codex's active checkout.

**LIVE W58 SOURCE CHANGE:** CHL producer moved from `2355bff` to `3787433` during PASS11. B01-40 independently verified ORIGINAL [new artifact](https://github.com/C8LyNn/RoboRougelike/actions/runs/40590979515/artifacts/12773288417): outer SHA256 `9235e10c53cf42cb4d75e9a126756efb452150e7fd2036b1e53dd61feb139412`; inner ZIP SHA256 `5150f5ad0fb42b1a249ba4fa252eacb9fa89dbc84437d65a46f9317668c8a630`; original new C# EditMode test Git blob `1a0ebfc5cc1f7fab3bb91fceab3a5cf8e1294a2d` and new manifest Git blob `cc3ef242a1e06ce4a3e11487f03cefa2a37b0ad6`; full 61 archive members, two exact changed, 59 unchanged, 60/60 source hashes/bytes and 26 unique GUID PASS. Previous W30 CHL operator SHA is **STALE** for new producer, no native G1 possible until **B01-20** updates it. Older archived source receipt is history only. This does not change ownership of CHL or grant G2.

## Current source inputs and exact proof
| Package | Original source / physical archive | Existing source G0 | Required native outcome |
| --- | --- | --- | --- |
| **CHL-003 R1** | producer [#285](https://github.com/C8LyNn/RoboRougelike/pull/285) **new exact HEAD** `3787433cb071a9d39015e7f9c9be5457cca4cc9b`; [new original Actions archive](https://github.com/C8LyNn/RoboRougelike/actions/runs/40590979515/artifacts/12773288417), **inner** ZIP SHA256 `5150f5ad0fb42b1a249ba4fa252eacb9fa89dbc84437d65a46f9317668c8a630` | B01-40 independent new-original-byte delta G0: full 61-member new archive CRC PASS, 60/60 SHA256/byte manifest PASS, 59 previous trusted members unchanged, new 2 Git blobs exact, 26 GUID unique; prior rollback validity for 59 unchanged, new ZIP **live-target rollback NOT_RUN** | **20** distinct EditMode and **15** distinct PlayMode cases actually run, every required case Passed; 3 independent real Light (270 kg) + 3 independent real Heavy (1150 kg) trajectories from Spawn through ordered CP0–CP3 to Finish with physical floor, ramp and bridge interactions; original P15 wheel support under gravity |
| **PARTSIZE-001 R1** | producer [#286](https://github.com/C8LyNn/RoboRougelike/pull/286) `be2be0e4802bc3f450179901f9ae91c6d84bcb5b`; [original archive](https://github.com/C8LyNn/RoboRougelike/actions/runs/40484486467/artifacts/12725235814), inner ZIP SHA256 `05b86bf90ba2b1a71ce32b081b91e2f24793ea81cb00f2025757c5c017d95f54` | independent B01-40 40/40 source/Git blob and rollback PASS, 17 GUID | **17** distinct EditMode and **11** distinct PlayMode cases actually run (including 7 parameterized quarters), real Collider.bounds and Physics overlap, 4 cardinal yaw orientations, child-collider omissions, typed/external socket authorization, 11 **unmeasured** moving part classes remain NOT_MEASURED until original physical measurement with evidence |

Current public game Codex [#310](https://github.com/C8LyNn/RoboRougelike/pull/310) `231005c6d0351945a8ee6684f3856af118f0e94f`; B01-40 [full public 1002-vs-43 GUID/114 asmdef gate](https://github.com/C8LyNn/RoboRougelike/actions/runs/40588328223) PASS Windows + Ubuntu. **No confirmation of Codex's private live target or its consent to receive these packs.** Package source and public static compatibility do not become G1/ G2.

## Authorized operator GO / NO-GO checklist (B01-00/HQ → B01-60)
- [ ] Explicit HQ approval for a **separate** legitimate licensed Windows Unity **6000.3.14f1** host, named operator, and authorized evidence storage; don't use owner/Codex active checkout. Without this, **STOP: NATIVE_NOT_RUN**.
- [ ] Two **different** empty disposable producer-exact detached Git worktrees (one each), with canonical real paths, identity/source SHA, separate parent QA repo provenance, clean tracked/Unity source state, correct `ProjectVersion.txt` and `Packages/manifest.json`. No source import or merge. Record original root `.git` worktree file and `git worktree list --porcelain` evidence before run.
- [ ] PARTSIZE existing [owner-safe operator](https://github.com/C8LyNn/RoboRougelike/blob/75b9c7902344eab4a012951aa9f578195b6bb943/Tools/B01/PARTSIZE001/W26Receipt/Run-UnityNative.ps1) Git blob `da575df50d51d5f76776d96e3b5251103b85d923`: run **`-PreflightOnly` first** on unused exact checkout and record output; [independent security test](https://github.com/C8LyNn/RoboRougelike/actions/runs/40552256737) 16/16 Win+Ubuntu remains valid for this SHA.
- [ ] CHL uses B01-20 existing **W30 operator is pinned to previous source `2355bff`; it MUST be independently re-pinned/QA-validated by B01-20 for the NEW CHL HEAD `3787433...` while keeping 20/15**, [owner handoff](https://github.com/C8LyNn/RoboRougelike/issues/91#issuecomment-6447643728). Old operator script checksum SHA256 `47625bf0092f0e9b6c4fa4ad79f2db746a12ad8e7c34272c37bf48ba47093f0c` is HISTORICAL / **DO NOT RUN on new CHL source**; obtain and verify fresh owner script with exact new SHA before any Unity run; this operator is **not** in Codex's main game Git tree. Missing physical QA script => STOP for CHL until original file obtained, not a substitute self-written operator.
- [ ] Evidence output path canonicalized, outside and disjoint from both source checkouts; **fresh and empty** per test candidate, not a reused previous receipt. No symlink/reparse tricks, no captured secrets.
- [ ] Unity CLI per stage: `-batchmode -nographics -projectPath <producer_exact_disposable_path> -runTests -testPlatform EditMode/PlayMode -assemblyNames <exact owner test assembly> -testResults <absolute outside-path XML> -logFile <absolute outside-path Editor.log>`. **Never combine `-quit` with `-runTests`**. One real Unity process and exit code per mode.
- [ ] Unity executable full path/version and OS/host identity recorded, process started/finished UTC with monotonic duration, all actual command argument tokens minus any sensitive activation data, exact source git SHA, tested assembly identity. No foreign pipeline success transferred.
- [ ] Per-stage raw original NUnit XML **not reserialized**, full original Editor log, signed/hashed SHA256, native process return code, all individual `test-case` IDs and expected counts, no 0/unknown/skipped/failed/duplicate/foreign cases; wrong source or Editor version is NO-GO.
- [ ] PARTSIZE original `receipt.json` has stage `exit_code`, `xml_sha256`, `log_sha256`, `assembly` and top `source_sha`, `unity_version`, `status=RAW_NATIVE_FILES_ONLY_NOT_G1`. B01-23 [owner `check_receipt.py`](https://github.com/C8LyNn/RoboRougelike/blob/75b9c7902344eab4a012951aa9f578195b6bb943/Tools/B01/PARTSIZE001/W26Receipt/check_receipt.py) may return only `STRUCTURAL_EVIDENCE_REVIEW_REQUIRED`. **Do not automatically promote that to G1**; a B01-40 supplemental original parser negative review is pending.
- [ ] CHL physical telemetry must identify **six distinct run identities**, each rig mass, actual physics run start/end times, ordered CP evidence, reset/Retry/grounded contacts, ramp and bridge; no simulated CSV, copied rows, source-only analytic geometry or authored-but-not-executed test substitutes.
- [ ] PARTSIZE physical telemetry must record actual collider object names and enabled state, `Collider.bounds` in world units with **x/y/z**, yaw **0/90/180/270°**, `Physics.OverlapBox`/narrowphase and child-collider inclusion; functional typed socket tested separately from visually flush grid. Missing measured kinds stay `UNKNOWN/NOT_RUN`, never extrapolated from C# dimensions.
- [ ] Two per-package independent evidence manifests: original Git commit, SHA256 of immutable source ZIP, raw file path/size/hash, Unity executable binary hash (when permitted), version, host/time, process original exit, stage assertion counts, physical telemetry, witness provenance and self-report of any incomplete artifact.
- [ ] B01-60 independently inspects **raw** XML/log/physical captures; B01-40 independently checks exact SHA/manifest/evidence integrity and accepted operator provenance. Any nonzero exit, missing logs, absent distinct physical trials, missing/inconsistent source identity, or parser ambiguity => `FAIL/BLOCKED/NOT_RUN` as appropriate, **G2_HOLD**.
- [ ] Read-only receipt/final status goes to B01-00/HQ for **separate G2 decision** only after authentic G1. Explicit Codex ACK and any target import are distinct later gates. No merge, OFFERED or CODEX_ACK inferred from any of these checks.

## Exact evidence directory layout (logical contract, not artifact claim)
```text
<approved-outside-evidence-root>/
  CHL003_3787433_<unique-session>/
    identity.json                 # source ZIP SHA256 + Git HEAD + disposable ID
    EditMode.xml                  # raw original NUnit, 20 required cases
    EditMode.Editor.log           # complete raw Editor log
    EditMode.exitcode             # real process exit
    PlayMode.xml                  # raw original NUnit, 15 required cases
    PlayMode.Editor.log           # complete raw Editor log
    PlayMode.exitcode             # real process exit
    CHL003_REAL_TRIALS.csv        # real Light3+Heavy3; 6 independent runs
    evidence.sha256.json          # SHA256 raw file and provenance list
  PARTSIZE001_be2be0e_<unique-session>/
    identity.json
    receipt.json                  # owner raw-stage receipt, NOT G1 approval
    EditMode.xml                  # 17 cases; original owner name
    EditMode.log                  # original owner log
    PlayMode.xml                  # 11 cases; original owner name
    PlayMode.log                  # original owner log
    physical_4yaw.csv             # verified physical orientations and dimensions
    socket_collider_witness.json  # typed sockets and physical child overlap facts
    evidence.sha256.json
```
The layout is an **intake contract**; the native operators may name files differently. The receiver maps original files without renaming/rewriting their bytes; preserve original owner file paths in the manifest.

## Closure and ownership
B01-40 owns source/provenance/G1 intake verification, **not** B01-23 PARTSIZE operator/parser fixes, B01-20 CHL implementation or B01-60's independent licensed-run verdict. Current CHL physical-source delta G0 PASS @3787433 (new archive, SHA256/bytes/Git-delta, not fresh real target rollback), PARTSIZE source G0 PASS @be2be0e, PARTSIZE original preflight script unchanged and PASS, licensed Unity G1 NOT_RUN, public target metadata static PASS for unchanged GUID sets, **private target not verified**, G2 HOLD. This document is an operator-independent checklist in the **existing B01-40 draft PR #284 only**. No autonomous HQ approval or Unity action.

# B01-41 / W58–W61 PASS11 — safe CI evidence gate (QA ONLY)

This branch contains **two distinct new tools**: (A) an activation failure decision tree and (B) a *cross-workflow* evidence consistency validator for CI-02/03. (C) below gives an HQ unlock procedure.

All 28 tests use synthetic test fixtures **except two actual public GitHub Actions job-step snapshots**. No Unity binary was started. The synthetic "successful native receipt" intentionally returns only STRUCTURAL_RECEIPT_ONLY / PENDING_INDEPENDENT_NATIVE_REVIEW. Never promote it to G1 or G2.

Scope isolation: only Tools/B01/CI41/W58; no changes to Codex, PARTSIZE, CHL or existing CI YAML. This tool does not replace Tools/CI/check_results.py, PARTSIZE's #298 native operator/parser or B01-40's original source/archive G0 gate.

## A. Activation failure classifier

Two public and sanitized original job observations are pinned in observed_ci_20261027.json:

- CI-02 https://github.com/C8LyNn/RoboRougelike/actions/runs/40588337207
- CI-03 https://github.com/C8LyNn/RoboRougelike/actions/runs/40588337178

The GitHub Actions HEAD for both is d95d3660c6e575349d4d825b3522a05e8a650ac3 (B01-40 QA branch, **not Codex/CHL/PARTSIZE source**).

Extract either EditMode or PlayMode child object to a standalone job.json; run:

    python ci41_evidence.py activation job.json

- Exit 3 + NO_LICENSE_INPUT/NOT_RUN: failed credentials preflight, explicit public "BLOCKED: Unity NOT RUN." marker, and skipped engine. This describes unavailable required inputs IN THAT RUN, NOT a finding that the owner lacks a valid Unity license.
- Exit 3 + ACTIVATION_PREFLIGHT_FAILED_UNKNOWN: preflight failure without the known marker; never guess its cause.
- Exit 0 + ENGINE_STEP_SUCCEEDED/UNVERIFIED: an engine step is recorded as successful, but there is no G1 until source-pinned original XML/Editor.log/native process exit are independently checked.
- Exit 2 + FAIL_CLOSED: contradictory or invalid evidence.

This classifier never accesses or stores secret contents.

## B. Cross-workflow native evidence consistency gate

This is NOT the existing PARTSIZE per-package NUnit method-name validator. It checks a **different seam**: that CI-02 and CI-03 evidence actually refers to the same pinned source SHA and Unity version, distinct original Actions run IDs, correct workflow stage, successful engine-step classification, original native exit code 0, SHA256-matching XML/log bytes, valid nonempty passing NUnit cases and coherent run totals.

Expected directory: EVIDENCE/receipt.json, EditMode.xml, EditMode.log, PlayMode.xml, PlayMode.log.

receipt.json must have keys source_sha, unity_version and jobs with EditMode / PlayMode. For each job include workflow (CI-02 Unity EditMode or CI-03 Unity PlayMode), head_sha, numeric run_id, activation (ENGINE_STEP_SUCCEEDED), conclusion (success), native_exit_code (0, **from the actual Unity process**), xml_sha256 and log_sha256. Do not infer native_exit_code from the GitHub job conclusion. The checker takes the expected full source SHA from the operator, separate from receipt data:

    python ci41_evidence.py receipt EVIDENCE --expected-sha FULL_40_CHARACTER_GIT_SHA

A 0 exit ONLY means STRUCTURAL_RECEIPT_ONLY, G1 PENDING_INDEPENDENT_NATIVE_REVIEW, G2 HOLD. Synthetic XML/logs can trivially spoof a structurally correct receipt; the validator cannot attest to Unity provenance by itself.

The regression suite includes negative cases for cross-stage SHA splice, mismatched source/version, duplicate run ID, swapped Edit/Play workflow, missing/nonzero native exit, failed engine-step classification, tampered bytes, symlinked or absent evidence, short log, missing/mismatched NUnit aggregate, duplicate and skipped test cases.

Run real portable regression:

    cd Tools/B01/CI41/W58
    python -m unittest -v test_ci41_evidence.py

No external Python packages required. The pinned public job snapshots are actual GitHub observations; all NUnit XML, Editor.log and exit-0 "success" fixtures are explicitly **SYNTHETIC**.

## C. One HQ unblock instruction — no secret exposure

1. HQ explicitly authorizes and assigns an independently licensed, disposable Windows Unity 6000.3.14f1 QA host and named operator (not Codex's live worktree, not the owner's active desktop). Existing B01-40 static G0 and #298 PARTSIZE preflight have already passed; do not rerun them without changed inputs.
2. If cloud activation is supported by the actual license, the authorized admin configures credentials privately under GitHub Repository → Settings → Secrets and variables → Actions, following Docs/CI_SETUP.md and the legitimate GameCI activation instructions. Never post UNITY_EMAIL, UNITY_PASSWORD, UNITY_LICENSE, UNITY_SERIAL, .ulf or tokens to GitHub issues, PRs, raw logs or conversation. A nonempty secret or successful preflight is not proof of activation. Do not expose credentials to untrusted PRs or switch to pull_request_target.
3. If cloud activation is not supported, use a separate properly licensed local Windows host; do not bypass Unity licensing. B01-40 pins producer SHA and external original archive/manifests, two distinct detached single-use worktrees and rollback. B01-23's operator #298 remains the producer-specific PARTSIZE receipt verifier.
4. B01-60 independently checks CHL #285 exact SHA (20 Edit, 15 Play and 3 Light + 3 Heavy genuine continuous physics trials) and PARTSIZE #286 exact SHA (17 Edit, 11 Play and real collider/PhysX/socket/yaw tests). Record original NUnit XML, full Editor.log, Unity process exit, editor version, source SHA, run ID, hashes, real scene/telemetry/screenshot evidence and exact rollback for each **separate source**.
5. B01-40 independently checks original provenance/receiver. Only then can B01-00/HQ consider G1/G2 in separate decisions. No auto OFFERED, no Codex ACK, no merge or release.

**Current status:** HQ G1 host authorization not observed; current public GitHub CI-02/03 activation gates fail before Unity. No native G1 is claimed.

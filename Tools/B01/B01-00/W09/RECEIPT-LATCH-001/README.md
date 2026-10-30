# B01-00 RECEIPT-LATCH-001 v1.3.0 / PASS17 W12

Offline Python 3.11 standard-library executable for B01-00 coordinating multi-agent package receipts. `receipt_latch.py` validates offline snapshot consistency; `github_live.py` now checks a bounded subset of original GitHub PR, Actions workflow, job and step metadata online. Neither can authenticate the independence or authorization of a B01-40/B01-60 review, Codex ACK or merge. Source-ready is a **candidate for external review**, never formal G0, G1, Codex ACK or integration.

## Working commands

```sh
python -m unittest discover -s Tools/B01/B01-00/W09/RECEIPT-LATCH-001 -p 'test_*.py' -v
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/receipt_latch.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/example_hold.json
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/receipt_latch.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/example_hold.json --require-source-ready
```

The last command deliberately returns exit **2** because actual B01-40 and B01-60 independent reviews for the example remain PENDING. The example is a time-scoped factual W08 snapshot of [Draft PR #427](https://github.com/C8LyNn/RoboRougelike/pull/427) with exact producer SHA and original CI, **not an authorization to offer the package to Codex**.

### Exact input contract

The JSON requires **distinct independent intake and QA comment URLs**, an offer receipt separate from QA and a Codex ACK comment separate from both, plus package ID/version, original producer and immutable lowercase 40-character SHA, exact PR ID/URL, original producer CI PASS with positive test count/zero failures, independent B01-40 intake and B01-60 QA receipts both bound to same source SHA and original comment URLs, explicit native-test requirement, optional native EditMode/PlayMode executed PASS on the same SHA, and separately ordered immutable OFFERED → explicit CODEX ACK → integration records. It will never equate sent, ACK, native PASS, source PASS or integration automatically. Reused QA comments, offered-comment-as-Codex-ACK and reused Codex acknowledgement are rejected.

The tool rejects inconsistent paths, roles, source hashes, missing test counts, invalid receipts, unavailable native gates, stage jumps, and overwritten output. Its `--output` mode uses write-once `open("x")` to a new file; this is local user-controlled storage, not a transactional, signed receipt. It never touches Github, Unity, main, develop, Codex code, or B01-40/60 authored artifacts. An output report includes input SHA-256 for evidence correlation.

### Rollback and limitations

Installation: copy only this isolated directory and run Python 3.11. Rollback: delete the copied tool and any self-generated non-canonical report files; **never** remove frozen handoff or QA receipts. No external writes or permanent side effects. GitHub links and actor names can be forged in local JSON; a result indicating consistent declared receipts is only advisory until reviewers verify the real GitHub objects/commits and provenance. Do not use this as an automated release gate without integrating authenticated source verification and authorized sign-off. Native Unity tests are NOT_RUN for this Python tool.

Receiver: **B01-40** checks source/GitHub receipts, **B01-60** independently reproduces tests and negative cases, **B01-00** uses decision output to avoid premature source offer. Formal source freeze/offer only by authorized independent process under [#276](https://github.com/C8LyNn/RoboRougelike/issues/276), not by this CLI.

## WAVE 10 — actual read-only GitHub provenance (new executable)

`github_live.py` adds a new, separate real capability to the existing W09 JSON structure gate. It performs **GET-only** requests using Python stdlib to the fixed `api.github.com/repos/C8LyNn/RoboRougelike` host and a three-family path allowlist (pull request, Actions workflow run, issue comment), never an input-controlled external URL. The client forbids redirects and overly large replies, uses a finite timeout, and optionally accepts `GITHUB_TOKEN` from the environment. Do not place credentials in repository files or CLI arguments.

```sh
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/github_live.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/example_hold.json --require-source-objects
python -m unittest discover -s Tools/B01/B01-00/W09/RECEIPT-LATCH-001 -p "test_*.py" -v
```

The supplied exact historical W08 source receipt points to [PR #427](https://github.com/C8LyNn/RoboRougelike/pull/427) and [its original passing workflow](https://github.com/C8LyNn/RoboRougelike/actions/runs/41046087105). This command verifies that the **current** PR head and its linked Actions run match the same exact immutable SHA, original repository, successful completion and run URL. If PR head is later replaced or moved, the tool **HOLDs** conservatively; it does not claim historical ancestry. Original CI test counts are **not** independently replayed or read from GitHub job logs by this command; the run status is only a source-object check.

For QA comments, the tool checks existence, canonical comment URL, exact SHA mentioned in the body, and presence of a GitHub user login. **This cannot authenticate role identity or independent review content**, especially because multiple swarm roles can share a GitHub account. Its result therefore remains `HOLD_MANUAL_INDEPENDENT_QA`, `CODEX_ACK_NOT_AUTHENTICATED` and `G2_HOLD` regardless of passing GET checks. An OFFLINE JSON status never promotes to `OFFERED`, `CODEX_CLAIMED`, `INTEGRATED` or native `UNITY_PASS`. Authorized B01-40/60 evidence review is still required.

The W10 workflow exercises the **original 76-case Python suite** on Windows 2025 and Ubuntu 24.04 and performs a real read-only API call for the example PR #427. A successful live check proves only accessible PR/CI metadata, not formal acceptance. If GitHub permission, network, token, rate limits, source head drift, or API objects prevent validation, the process exits nonzero with `HOLD`. Installation and rollback still consist solely of copying/deleting the own isolated tool and reports; no GitHub content is modified by either CLI.

## WAVE 11: strict original workflow and dual-platform test-step contract

v1.1.0 trusted any successful Actions workflow running at the claimed source SHA. An unrelated CI workflow could pass without executing the original tests. W11 changes the decision to **fail closed** unless the declared `producer.ci.contract` matches GitHub's actual workflow ID, on-repository YAML workflow path and trigger event **and** the original `GET /actions/runs/{id}/jobs` response contains every contract-listed Windows and Ubuntu job, with matching source SHA, completed/success result and every named test step completed/success. Incomplete first-page job lists, duplicate job names and missing/failed test steps force HOLD.

The example pins real original source [PR #427](https://github.com/C8LyNn/RoboRougelike/pull/427) and [Actions run #41046087105](https://github.com/C8LyNn/RoboRougelike/actions/runs/41046087105): `workflow_id=401553591`, `path=.github/workflows/b01-00-dispatch-w06.yml`, `event=pull_request`; required jobs `dispatch (windows-2025)`, `dispatch (ubuntu-24.04)`; required original steps `Original source full dispatch and freeze regression suite`, `Produce deterministic example plan`. New `source_workflow_jobs_verified` and `source_gates_label` fields distinguish actual step-level evidence from a generic successful run. This tool checks GitHub metadata and does **not** download/replay actual workflow logs or prove individual test counts. The declared contract itself must be independently inspected against actual source; the result is never a formal QA approval.

The W11 source suite is **101 original tests** (44 W09 + 32 W10 + 25 W11). Actual source-level RED and GREEN on Windows/Ubuntu and real API job checks are linked in `TEST_EVIDENCE/W11-WORKFLOW-RED-GREEN.md`. Runs on Python 3.11 stdlib; no native Unity execution, no GitHub writes, no gameplay changes. No independent acceptance yet. B01-40 and B01-60 must reproduce this exact proposed revision, validate manifest/GitHub source against actual original jobs, and grant their own separate approval before an authorized offer.

## WAVE 12 — runner-label + exact attempt binding (security hardening)

An Actions job `name` is display text, **not evidence that it ran on Windows or Ubuntu**. In W12, the live verifier additionally requires a declared `required_runner_labels` mapping that binds exactly one original Windows 2025 and one Ubuntu 24.04 job. Their *actual GitHub REST* `job.labels` must identify the declared runner and not include the opposite platform. The original `run_attempt` must be a positive integer; each actual job's `run_id` and `run_attempt` must agree with the pinned source workflow run. Duplicate GitHub job IDs fail closed. This prevents proof from being recycled across reruns or OS jobs simply renamed to bypass an unexecuted platform.

The live fixture remains actual [Draft PR #427](https://github.com/C8LyNn/RoboRougelike/pull/427) and source [Actions #41046087105](https://github.com/C8LyNn/RoboRougelike/actions/runs/41046087105), where GitHub returned `labels=["windows-2025"]` and `labels=["ubuntu-24.04"]` with `run_id=41046087105`, `run_attempt=1` on the corresponding real jobs. `runner_identity_proof` now distinguishes this verification from checking mere job names.

The suite grows to **123 original Python tests** (W09 44 + W10 32 + W11 25 + W12 22), run on Windows 2025 and Ubuntu 24.04. Real adversarial [RED](https://github.com/C8LyNn/RoboRougelike/actions/runs/41072839179) and corrected [GREEN](https://github.com/C8LyNn/RoboRougelike/actions/runs/41073097313) results are recorded under `TEST_EVIDENCE/W12-RUNNER-RED-GREEN.md`. Exact final manifest-HEAD CI should be checked separately after final commit. **No Unity Editor or PhysX tests were executed by this source-only Python utility.**

The GitHub API metadata is a stronger source-evidence prerequisite, **not an authenticated role endorsement or independent QA**. B01-40/B01-60 still decide whether original test logs/source match and may HOLD. The tool itself reports `decision=HOLD` for the example with QA PENDING, never fabricates `OFFERED`, `CODEX_CLAIMED` or `INTEGRATED`. There are no writes outside its own optional output report and no GitHub or Unity mutations. Rollback consists of deleting only the isolated checkout and disposable reports.

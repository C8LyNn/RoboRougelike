# B01-00 RECEIPT-LATCH-001 v1.7.1 / PASS17 W16

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

## W13 source-only Codex offer protocol — separate from G1/G2

New original Python stdlib `offer_protocol.py` and real static offer `offer_secure_v2_source_only.json` record ONE source-only inbox offer, *not* source acceptance logic for Unity or a Codex ACK. It provides a **fail-closed declarative audit** of the cross-agent handoff state: exact single source SHA/PR, original physical archive SHA256+entry count, independent B01-40 source-only G0 reviewer SHA/PR, separate B01-61 transaction-source QA SHA/PR, canonical Codex Inbox issue #79 comment identifier, explicit alternative PR holds and eight release constraints.

```sh
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/offer_protocol.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/offer_secure_v2_source_only.json --require-structural-offer
python -m unittest discover -s Tools/B01/B01-00/W09/RECEIPT-LATCH-001 -p "test_*.py" -v
```

Actual package selected: **B01-23-SECURE-REATTACH-V2 v2.0.0** [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) immutable original SHA `d94400d95d13db7f27e1533df05c9292d5380431`; 35-member 99,666-byte source ZIP `d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387`. B01-40 independently accepted source-only [#521](https://github.com/C8LyNn/RoboRougelike/pull/521) and B01-61 source regression [#517](https://github.com/C8LyNn/RoboRougelike/pull/517), **not** native Unity. [Exactly one W13 source-only offer](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6526629723) was posted to CODEX INBOX after those G0/source QA results; #511 and #494 alternatives still HOLD. Any ACK requires an *explicit real Codex reply*. Neither comment creation nor this CLI proves Codex claimed scope.

**Physical rollback warning:** selected original #510 `package.py rollback` may delete *foreign empty subdirectories* because it verifies files before `shutil.rmtree`. Until separate B01-40/62 original negative and safe-receiver confirmation, **never run original rollback on shared/occupied game workspace**; do not let the source offer become installation permission. Save/Load consumed/detached ID contract remains HOLD, true Unity Editor/PhysX G1 NOT_RUN, canonical Codex scene migration NOT_DONE, G2 HOLD, NOT INTEGRATED and NO MERGE. This v1 protocol requires precisely those eight HOLD states: promoting native, Save/Load, rollback, migration, G2, ACK, integration or merge without a distinct independently reviewed new state format fails the CLI.

**Security boundary:** `offer_protocol.py` uses no network and only checks internal shape/consistency of supplied JSON. `source_only_offer_record_consistent=true` explicitly **does not authenticate** any GitHub comment, person, review text, artifact bytes or actual ACK. The original B01-00 GitHub live verifier validates *producer source CI metadata* separately; neither replaces B01-40/60/61/62 independent approvals.

**Rollback of this coordinator tool only:** discard the unmerged B01-00-owned Python directory and disposable CLI reports. Do not remove the actual frozen #510 producer source, selected original ZIP, receiver review/QA evidence or Codex Inbox immutable record. Receiver: B01-40 for source and rollback, B01-61 for transaction QA, B01-62 for ID persistence, B01-60 for actual licensed Unity PhysX, and B01-00 for ledger inventory/offer, with Codex as read-only offer recipient.

## WAVE 14: One Save/Load source-contract admission — independent security RED outranks producer PASS

The separate, executable `save_admission.py` maintains a **pre-integration source-only** comparison of the *two competing additive Save/Load proposals*, #544 and #549, against a fixed immutable gameplay candidate #510. Its companion `save_contract_w14_hold.json` has actual pinned GitHub PR SHA, independent QA state, and eight explicit release blockers. The B01-00 tool does **not** choose a design on behalf of B01-23; it records `owner_choice=UNDECIDED` until B01-23 publishes an exclusive, independently source-tested selection.

```sh
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/save_admission.py Tools/B01/B01-00/W09/RECEIPT-LATCH-001/save_contract_w14_hold.json
python -m unittest discover -s Tools/B01/B01-00/W09/RECEIPT-LATCH-001 -p "test_*.py" -v
```

**Current correct output:** `#544=HOLD_INDEPENDENT_SOURCE_QA`, `#549=REJECT_EXACT_SOURCE_SECURITY_RED`, `verdict=HOLD_NO_ACCEPTED_SINGLE_SAVE_CONTRACT`. Although #549 producer tests passed, B01-61 [#550](https://github.com/C8LyNn/RoboRougelike/pull/550) independently executed the unmodified original C# and exposed mutable validated `RunSnapshot.Parts` and a public mutating W06 ledger accessor. Crucially the QA-tested earlier revision and #549 current branch HEAD have **identical Git blob** `6c036985411164372bf3d21d467780ff3777544e` for the original vulnerable `DetachedSnapshotContract.cs`. This pin avoids pretending #550 ran against the final producer commit while preserving the real security finding across identical original source bytes. Any new fixed-version SHA requires a separate B01-23 owner decision and fresh independent B01-61 testing.

The new **53 original W14 unittest cases** reject wrong #510, two simultaneously eligible proposals, missing B01-23 choice, replayed older QA PASS, cross-SHA RED with different source blob, missing/duplicate RED evidence, false G1/SaveLoad/rollback/G2/ACK/merge, malformed comments and unauthorized overwrite. The full original Python test suite is **225 cases** on Windows2025 and Ubuntu24.04, with the existing real read-only GitHub #427 source-run checks. [Real W14 source run #41087521336](https://github.com/C8LyNn/RoboRougelike/actions/runs/41087521336) 225/225 PASS per OS. The CLI has no network or GitHub writes and cannot authenticate independent reviewer identity. A structurally eligible future candidate is **MANUAL_G0_REVIEW_REQUIRED**, never automatic approval.

**Remaining source and native gates:** actual #510 source-only G0 previously accepted and offered in #79, **not Codex ACK**; #511/#494 held. Actual canonical Save/Load of detached/consumed IDs is NOT_IMPLEMENTED; original #510 rollback deletes foreign empty dirs and must NEVER be used on an occupied receiver; B01-40/62 safe wrappers require explicit integration decision; Unity native G1 Edit/Play/PhysX remains NOT_RUN where legal activation is unavailable; canonical host migration NOT_DONE; G2 HOLD. This gate must not be treated as an engine/native test or as a B01-23 owner selection.

**Install/rollback:** copy only owner-controlled standalone Python tool in a separate checkout and run on a local receipt; discard its unmerged directory and disposable output on rollback. Never delete frozen #510, original ZIP, QA evidence, Codex Inbox, game Assets or an occupied receiver. Receiver B01-23 selects exactly one versioned Save contract, B01-61 tests its **original exact SHA**, B01-40/62 approve source/migration/rollback, B01-60 native G1, B01-00 checks source-only queue and prevents fake ACK.

## WAVE 15: corrected source selection receipt #556 with strict host boundary

W14's `save_contract_w14_hold.json` and its original tests remain **historical, not a current selection instruction**: B01-23 subsequently froze **ONE corrected Save contract #556** at exactly `33094f154c7b91119b36d58aab285e6e83c5de57`, retaining gameplay #510 at `d94400d95d13db7f27e1533df05c9292d5380431`. The old #544/#549 R1 and competing #554 remain alternative HOLD; #549 and #544 original vulnerable source RED witnesses remain true historical evidence, **not** a claim of RED on corrected #556.

A new explicitly versioned mode in the **existing** `save_admission.py` uses `audit_selected_v2()` and the grounded `save_contract_w15_selected.json`. It checks the exact selected SHA, source-only physical G0 [B01-40 #560](https://github.com/C8LyNn/RoboRougelike/pull/560) SHA `1a218df9e287a8a254b7d84c228c962ab232b9b5` with **17-member ZIP SHA256 `20c3c17c252f084014e9766010ab4651770a41f73ce534fc2aa7fd2edc697060`**, and independent exact-original-C# source tests [B01-61 #563](https://github.com/C8LyNn/RoboRougelike/pull/563) SHA `42aa590a2f6b08e33886027ea452ef3392a2f2c0`, plus separate [#570](https://github.com/C8LyNn/RoboRougelike/pull/570) SHA `413bee4c232145be4cdf5745088fe1538a5f421b`, **closed but unmerged**, not a staged alternative.

```sh
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/save_admission.py --selected-v2 Tools/B01/B01-00/W09/RECEIPT-LATCH-001/save_contract_w15_selected.json
python -m unittest discover -s Tools/B01/B01-00/W09/RECEIPT-LATCH-001 -p "test_*.py" -v
```

The current source-only receipt can be internally consistent, **but its verdict is necessarily `SOURCE_G0_RECORDED_HOST_GATE_HOLD`**. Existing #556 only serializes/stages *logical* immutable snapshots and original W06 rehydration in an isolated source test; a real atomic Unity host adapter into frozen #510 private rig/ledger/Transform/Rigidbody is not implemented. No authorized public hydration hook exists in the frozen #510 adapter. Forbid reflection/private access, partial physical commits, false native G1, fictitious Codex ACK, old destructive rollback, or reuse of these source G0 results as full host acceptance. **Any real host adapter needs a NEW owner-versioned source with B01-23/62/40/61/60 review, never edits to frozen #510 or #556.** Do not run unsafe original #510 rollback against a shared workspace; B01-40/#536 and B01-60/#542 wrappers remain isolated disposable scratch tests.

The original W15 `test_save_selected_v2.py` adds **62** positive and adversarial regression cases (total 287 W09–W15). A missing or wrong SHA, forged QA/G0 ZIP, historical alternatives, false G1/SaveLive, fake migration, ACK or merge fails closed. **Limitations:** the CLI validates a declared JSON receipt **offline**, does not authenticate GitHub identity, actual source ZIP bytes, Codex actor or Unity host. Actual original GitHub source and QA were inspected separately by the coordinator. The `source_g0_recorded` label is a *recorded claim*, not an independent review conducted by this program. No new source-only offer is posted to #79 by this CLI and it has no network calls.

**Rollback**: discard only the unmerged coordinator Python tool and disposable local reports; leave frozen #510, #556, B01-40/61 evidence, Codex Inbox, and all production assets intact. No branch merge/release. The B01-00 tool remains pending its own independent B01-40/B01-60 code review even if producer tests pass.

## W16 single Unity bridge source evidence: exact RED vs changed-code QA gap

Wave16 [#589](https://github.com/C8LyNn/RoboRougelike/issues/589) retains immutable #510 gameplay and #556 Save schema, and assigns B01-23/62 (NOT B01-00) the exclusive decision among additive Unity host bridges [#585](https://github.com/C8LyNn/RoboRougelike/pull/585) and [#587](https://github.com/C8LyNn/RoboRougelike/pull/587). **No new bridge version has been accepted, offered, installed or merged**. The previous W15 selected Save v2 source G0 still does not mean live Unity Save/Load.

The **existing** original `save_admission.py` adds `--bridge-w16` to maintain current, original-source-exact QA inventory. Its `bridge_w16_hold.json` records:
- **#585 current core SHA `e9e9dc6d437e96dad6b08c1639e8bd94d62c4f4b`**: independent [#572](https://github.com/C8LyNn/RoboRougelike/pull/572) tested the *same source commit and Git blob*, demonstrated post-visibility-activation mutation not revalidated before publication: `HOLD_CURRENT_EXACT_SOURCE_SECURITY_RED`.
- **#587 current core SHA `16a7d7043a2ecb59a41bbfb063170235872fd7fc`**: independent [#588](https://github.com/C8LyNn/RoboRougelike/pull/588) tested *earlier* Git blob `db69bae2bf4cf106da68f13e0b067499f0da02f3`. Its same-generation world-fork RED is genuine **for the earlier version**, but the current SHA changed: `HOLD_CHANGED_CORE_NEW_EXACT_SHA_QA_REQUIRED`. The latest source includes post-activation Capture validation but still appears to accept different same-generation snapshots; obtain **new original source execution on exact current or newer selected SHA** before an independent verdict.
- Separate [#582](https://github.com/C8LyNn/RoboRougelike/pull/582) targets alternative B01-62 #577 code and found same-generation fork plus misreported cleanup failure when `Abort()` throws. Do not automatically assign these findings to #585/#587 or install #577 as third bridge.

```sh
python Tools/B01/B01-00/W09/RECEIPT-LATCH-001/save_admission.py --bridge-w16 Tools/B01/B01-00/W09/RECEIPT-LATCH-001/bridge_w16_hold.json
python -m unittest discover -s Tools/B01/B01-00/W09/RECEIPT-LATCH-001 -p "test_*.py" -v
```

The coordinator CLI *only validates declared JSON offline*, using source SHAs independently read from GitHub; it does NOT authenticate reviewer identity, run real Unity, provide an atomic host adapter, infer B01-23 owner choice or send a Codex handoff. It always reports `source_g0=HOLD`, `native_unity_g1=NOT_RUN`, `live_game_save_load=HOLD`, `g2=HOLD`, `codex_ack=UNCONFIRMED`. It rejects a false G0, fabricated owner selection, substituted original QA commit/blob, alternative source injection, unsafe rollback, fake native G1 and premature Codex ACK. A decision by B01-23 later needs a **new source SHA, exact re-run independent QA and B01-40 physical source acceptance**; never mutate the frozen #510/#556.

New original `test_bridge_w16.py` contributes **57 positive/adversarial cases**, taking the full executable suite to **344 source-only original tests**. Real GitHub Actions exact functional SHA [#41096301536](https://github.com/C8LyNn/RoboRougelike/actions/runs/41096301536) ran **344/344 PASS each Windows2025/Ubuntu24.04**, actual GitHub read-only producer source/OS proof remained PASS, and true W16 CLI returned both correct source HOLD outcomes. See `TEST_EVIDENCE/W16-BRIDGE-PROVENANCE-RED-HOLD.md`. Exact final manifest SHA CI requires separate post-commit check.

**Rollback / authority:** discard only unmerged B01-00 Python tool checkout and disposable outputs. No changes to original frozen #510/#556, B01-23 bridge files, B01-61 test files, `Assets/**`, Codex scenes or main/develop. Never run original #510 unsafe recursive rollback on occupied targets; only separately reviewed B01-40/60 safe wrappers in disposable sandbox. **Receiver** B01-23/62 selects/corrects bridge, B01-61 exact selected new SHA tests, B01-40/62 physical archive+safe rollback G0, B01-60 activated real Unity PhysX or NOT_RUN, B01-00 tracks owner/ACK without inventing one.

## W16 post-snapshot OWNER DECISION / NEW EXACT ORIGINAL C# QA — supersedes preceding initial queue state

B01-23 **selected only the #587 lineage** and published **new unmerged Draft [#596](https://github.com/C8LyNn/RoboRougelike/pull/596)** for new R2 v2.0.0 repairs. Original #587 SHA `ad7904eca571c742257551fa8103f266fcb9b752` and #510/#556 frozen parents remain immutable. #585, #577, #584 alternate HOLD. The #596 code is **NOT independently accepted**; its observed *mutable Draft* HEAD when this source fixture was written was `7bd598fde2204d6a397a50b199be12f072b5cd66`. A newer owner commit must be verified anew; this is not a frozen release identity.

Independent B01-61 [**#590 at QA HEAD `c28565639bef834e64c092ce142c449acdfb7ae5`**](https://github.com/C8LyNn/RoboRougelike/pull/590) **actually executed original CURRENT #587 core Git blob `16a7d7043a2ecb59a41bbfb063170235872fd7fc`** with immutable #556 and real W06 in C#9/.NET8. [Original dual-OS Actions #41096343649](https://github.com/C8LyNn/RoboRougelike/actions/runs/41096343649): **15/15 executed source assertions PASS EACH Windows+Ubuntu**, plus separately executed `--release` **EXIT 3 / `HOLD_P0_RED`** on both, meaning independent source release DENIED despite green test detection. Proven ORIGINAL #587 source bugs:
1. Different consumed Light/Heavy world with *same run/generation* accepted, old source retired, generation not incremented. Requires durable checkpoint identity/revision enforcement and exact-source new regression.
2. After post-activation mismatch, candidate `Deactivate()` throws in rollback; `Quarantined=true` but public caller receives `HostException` rather than explicit `RollbackQuarantine`. New source revision must provide unambiguous fatal result or receiver contract, with independent QA of the final R2.

Original previous [#588](https://github.com/C8LyNn/RoboRougelike/pull/588) remains true **historical QA against older #587 core**, but the now-current separate #590 verifies two P0 security REDs against *exact* #587 HEAD; therefore final coordinator status is `#585=HOLD_CURRENT_EXACT_SOURCE_SECURITY_RED`, `#587=HOLD_CURRENT_EXACT_SOURCE_SECURITY_RED`, **#596 R2 SOURCE_QA_PENDING / SOURCE_G0_HOLD**. The modified CLI `--bridge-w16` validates the new #590 result and rejects a fake green or early #596 source receipt. Source/producer compilation or physical transport [B01-40 #594](https://github.com/C8LyNn/RoboRougelike/pull/594) success does not cure these independent release REDs; B01-40 issued physical intake **FORMAL HOLD**, not approved bridge G0.

No new #79 OFFERED, ACK or merge; native Unity PhysX/EditMode/PlayMode NOT_RUN absent activated Editor, full canonical #510 private ledger hydration NOT_IMPLEMENTED, G2 HOLD. Only one selected *lineage* (#587), while a safely corrected/reviewed selected *version* is still pending. The coordinator tool does **not** authenticate GitHub actors by reading its own JSON; original GitHub PR/CI was checked separately.

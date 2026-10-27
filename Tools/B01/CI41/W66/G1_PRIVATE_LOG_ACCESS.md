# B01-41 PASS13 — fail-closed access policy for raw G1 logs and ZIP

Scope: CI-only evidence governance; not approval or implementation of licensed Unity native execution. No Codex/CHL/PART gameplay source changes.

## Actual public CI evidence at pinned SHAs
- B01-41 source QA: Actions 40593574499 at ac836763ec70ab97bf623619051ae267e697b6ef, 28/28 on each of Windows and Ubuntu; 14-day sanitized test-output archives; native NOT_RUN.
- B01-61 #315: f31db751dd02228c11427e404862dcb6e3121b5d, Actions 40592587507, 29/29 STATIC source-pin checks; 0 uploaded archives; original NUnit XML and Editor.log absent.
- B01-62 #316: 3829c12c9f6894c6739483b3ba6155f730882128, Actions 40592675734, Windows and Ubuntu original Codex MapStorage C# with Unity shims; 7-day log ZIP artifacts 12775645292 and 12774254108; D02 KNOWN_DEFECT_RED, native NOT_RUN.
- CHL producer #285: c7b2a234a286089383503da41883d624fe650551; Actions 40591508332, 21-day original source archive; B01-40 independent SOURCE G0 60/60 PASS; native G1 NOT_RUN.
- PARTSIZE QA #298: f3a40810a178c11696360c32921872bfacecf21b; NEW parser blob b4ca19bd1b9142a364dbed2fb6bb2dd30f2b53ac fixes NEG01–03. Owner original QA GitHub Actions 40593831331: 16/16 parser fixtures PASS on Windows and Ubuntu. Secure operator PS1 Git blob da575df50d51d5f76776d96e3b5251103b85d923 remains byte-identical. Independent B01-40 NEW-PARSER receiver still required.

## Deny-by-default intake for any future real G1
1. AUTHORITY_FIRST: Require explicit HQ approval, a named legally licensed Windows Unity 6000.3.14f1 host/operator, two separate pristine detached disposable exact-source worktrees, and HQ-approved restricted raw-evidence storage located OUTSIDE ALL registered Git worktrees, not merely outside the currently executed project. If missing, G1=NOT_RUN and do not access Unity activation secrets.
2. PIN_BEFORE_EXECUTE: Compare the full 40-hex HQ/B01-40 producer SHA with detached HEAD, original Git blobs, Unity project version, assembly and test inventory, and actual run SHA. A job on a QA branch is not a run at the producer SHA. Reject mismatch; preserve exact mismatch reason.
3. NO_PUBLIC_RAW_LOGS: Do not publish complete Unity Editor.log, environment dumps, .ulf/ALF, UNITY_EMAIL/PASSWORD/LICENSE/SERIAL, tokens, private machine or user data on public Actions, issues or PRs. An approved private source-authentic store with named B01-60/B01-40 access, bounded retention and audit trail is mandatory. If no private store exists, do not upload/distribute private raw logs. Public CI may publish only redacted safe status. Never use pull_request_target to expose secrets to untrusted changes.
4. ORIGINAL_BYTES: Independently validate full original XML/log/process-exit/physical telemetry, SHA256, byte lengths, timestamp, Unity version, OS, source SHA, run id, and manifest. Reject absent raw bytes, duplicate names, casefold aliases, zip-slip/absolute paths, symlinks, invalid CRC, oversized entries and reused identities. Before any actual evidence write, compare canonicalized EvidenceDir against every root from git worktree list --porcelain and reject equality or descendants for ANY registered worktree (including sibling CHL/PART checkout), with linked/reparse-path negative cases. Raw SHA256 alone does not attest to Unity provenance.
5. INDEPENDENT_RECEIVER: B01-20 must repin CHL W30 operator from old 2355 to current c7b2; B01-23 has authored and owner-tested three structural parser fixes in #298 at f3a40810; B01-40 must independently re-run the original NEG01–03 witnesses on the NEW source blob before receiver signoff; B01-40 checks provenance and B01-60 independently reviews original Unity stage results, actual 3Light+3Heavy and PART PhysX/socket/44-yaw measures. A successful synthetic structural check never grants G1.
6. NO_AUTO_G2: Native G1 requires authentic, independent results. G2, OFFERED, Codex ACK and merge each require separately applicable HQ governance. Correctly detecting D02 KNOWN_DEFECT_RED is not a game fix. A successful preflight/CI01/static job cannot be promoted to a native Unity result.

## Public redacted output schema
TASK_ID | PRODUCER_FULL_SHA | QA_HEAD_SHA | WORKFLOW_RUN_URL | ARTIFACT_ID | REDACTED_SUMMARY | CASE_COUNT | SOURCE_G0 | NATIVE_G1_STATUS | PRIVATE_RECEIPT_SHA256 (not a private URL or path) | BLOCKED_BY | NEXT_ROLE

Status must be one of NOT_RUN, PENDING_INDEPENDENT, PASS, FAIL and must not be inferred from a pipeline badge alone. Public repository artifact storage is not an approved secret store.

Exact handoff: HQ/B01-00 approves authorized host/private store -> B01-20 repins + B01-23 first closes the separate B01-60-reproduced sibling-worktree EvidenceDir preflight bypass on original Run-UnityNative.ps1 (https://github.com/C8LyNn/RoboRougelike/pull/298#issuecomment-6455035157), with B01-40 independent receiver; B01-40 also independently validates the newly repaired B01-23 QA parser -> B01-40 provenance -> B01-60 native G1 -> HQ considers G2.

## PASS13 new P0 isolation issue discovered independently by B01-60

Original PowerShell launcher blob da575df50d51d5f76776d96e3b5251103b85d923 incorrectly allows EvidenceDir equal to or inside a sibling registered producer worktree (despite refusing the executing ProjectPath). Original-script preflight reproduced on actual Windows+Ubuntu runner https://github.com/C8LyNn/RoboRougelike/actions/runs/40593918328 (6/6 witness per OS), not by B01-41 and not an executed Unity run. Require B01-23 owner to implement registered-all-worktrees deny check; B01-40 independently retest new runner source. Existing parser NEG01-03 FIXED BY OWNER on f3a40810; these do not close this launcher gap. Native G1 remains NOT_RUN and G2 HOLD.

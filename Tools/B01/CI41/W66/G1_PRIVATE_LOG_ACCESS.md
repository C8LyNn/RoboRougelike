# B01-41 PASS13 — fail-closed access policy for raw G1 logs and ZIP

Scope: CI-only evidence governance; not approval or implementation of licensed Unity native execution. No Codex/CHL/PART gameplay source changes.

## Actual public CI evidence at pinned SHAs
- B01-41 source QA: Actions 40593574499 at ac836763ec70ab97bf623619051ae267e697b6ef, 28/28 on each of Windows and Ubuntu; 14-day sanitized test-output archives; native NOT_RUN.
- B01-61 #315: f31db751dd02228c11427e404862dcb6e3121b5d, Actions 40592587507, 29/29 STATIC source-pin checks; 0 uploaded archives; original NUnit XML and Editor.log absent.
- B01-62 #316: 3829c12c9f6894c6739483b3ba6155f730882128, Actions 40592675734, Windows and Ubuntu original Codex MapStorage C# with Unity shims; 7-day log ZIP artifacts 12775645292 and 12774254108; D02 KNOWN_DEFECT_RED, native NOT_RUN.
- CHL producer #285: c7b2a234a286089383503da41883d624fe650551; Actions 40591508332, 21-day original source archive; B01-40 independent SOURCE G0 60/60 PASS; native G1 NOT_RUN.
- PARTSIZE QA #298: 63fd1f082661a6549ccf85c9af2d75a5ea989602; secure operator PS1 original blob da575df50d51d5f76776d96e3b5251103b85d923; source parser NEG01–03 remediation pending B01-23.

## Deny-by-default intake for any future real G1
1. AUTHORITY_FIRST: Require explicit HQ approval, a named legally licensed Windows Unity 6000.3.14f1 host/operator, two separate pristine detached disposable exact-source worktrees, and HQ-approved restricted raw-evidence storage. If missing, G1=NOT_RUN and do not access Unity activation secrets.
2. PIN_BEFORE_EXECUTE: Compare the full 40-hex HQ/B01-40 producer SHA with detached HEAD, original Git blobs, Unity project version, assembly and test inventory, and actual run SHA. A job on a QA branch is not a run at the producer SHA. Reject mismatch; preserve exact mismatch reason.
3. NO_PUBLIC_RAW_LOGS: Do not publish complete Unity Editor.log, environment dumps, .ulf/ALF, UNITY_EMAIL/PASSWORD/LICENSE/SERIAL, tokens, private machine or user data on public Actions, issues or PRs. An approved private source-authentic store with named B01-60/B01-40 access, bounded retention and audit trail is mandatory. If no private store exists, do not upload/distribute private raw logs. Public CI may publish only redacted safe status. Never use pull_request_target to expose secrets to untrusted changes.
4. ORIGINAL_BYTES: Independently validate full original XML/log/process-exit/physical telemetry, SHA256, byte lengths, timestamp, Unity version, OS, source SHA, run id, and manifest. Reject absent raw bytes, duplicate names, casefold aliases, zip-slip/absolute paths, symlinks, invalid CRC, oversized entries and reused identities. Raw SHA256 alone does not attest to Unity provenance.
5. INDEPENDENT_RECEIVER: B01-20 must repin CHL W30 operator from old 2355 to current c7b2; B01-23 must fix three original structural parser fail-opens in #298 without changing the secure PowerShell launcher; B01-40 checks provenance and B01-60 independently reviews original Unity stage results, actual 3Light+3Heavy and PART PhysX/socket/44-yaw measures. A successful synthetic structural check never grants G1.
6. NO_AUTO_G2: Native G1 requires authentic, independent results. G2, OFFERED, Codex ACK and merge each require separately applicable HQ governance. Correctly detecting D02 KNOWN_DEFECT_RED is not a game fix. A successful preflight/CI01/static job cannot be promoted to a native Unity result.

## Public redacted output schema
TASK_ID | PRODUCER_FULL_SHA | QA_HEAD_SHA | WORKFLOW_RUN_URL | ARTIFACT_ID | REDACTED_SUMMARY | CASE_COUNT | SOURCE_G0 | NATIVE_G1_STATUS | PRIVATE_RECEIPT_SHA256 (not a private URL or path) | BLOCKED_BY | NEXT_ROLE

Status must be one of NOT_RUN, PENDING_INDEPENDENT, PASS, FAIL and must not be inferred from a pipeline badge alone. Public repository artifact storage is not an approved secret store.

Exact handoff: HQ/B01-00 approves authorized host/private store -> B01-20 repins + B01-23 fixes owned QA parser -> B01-40 provenance -> B01-60 native G1 -> HQ considers G2.

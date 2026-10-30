# B01-00 / PASS17 WAVE10 — source-only live GitHub proof (producer evidence)

This is a real added executable in previous W09 package #468, not a separately approved new game source stream.

## Verified remote object fixture (live GitHub connector read, not a fake in-memory match)
The producer checked two actual read-only GitHub REST responses:
- PR #427 `GET /repos/C8LyNn/RoboRougelike/pulls/427`: number 427, `head.sha=3c4b576bb5d7cac5bb9ddc941da3dba0f6f2e07f`, `base.repo.full_name=C8LyNn/RoboRougelike`, `head.repo.full_name=C8LyNn/RoboRougelike`, `html_url=https://github.com/C8LyNn/RoboRougelike/pull/427`.
- Original Actions `GET /repos/C8LyNn/RoboRougelike/actions/runs/41046087105`: `head_sha=3c4b576bb5d7cac5bb9ddc941da3dba0f6f2e07f`, `status=completed`, `conclusion=success`, `repository.full_name=C8LyNn/RoboRougelike`. The original external run [#41046087105](https://github.com/C8LyNn/RoboRougelike/actions/runs/41046087105) itself passed 65/65 source tests per OS in W08.

## New W10 functional path
`github_live.py` reads these resources through hard-coded read-only, limited API endpoints. It never uses user-supplied URLs for network fetch, disallows redirects, limits response bytes, and records an input SHA256. It validates PR head exact SHA, GitHub repo identity and workflow exact SHA/conclusion. It may check QA comment *existence and SHA mention*, but **cannot prove a role or an independent endorsement**, and always reports `HOLD_MANUAL_INDEPENDENT_QA`. It does not authenticate Codex ACK or integration.

`example_hold.json` intentionally contains the real PR #427 evidence and still says independent B01-40 / B01-60 QA PENDING. It is expected to remain `decision=HOLD` even when `source_git_objects_verified=true`. The Actions workflow includes a separate live read-only API check of original PR #427 and run #41046087105 with strict source-object matching. A PASS there is not G0 independent acceptance.

## Source CI
A W10 32-case `test_github_live.py` suite was created in the PR. It exercises fake-but-structured remote responses and malicious/negative cases without network calls, while the dedicated Actions live step verifies the real public repository resources. Historical W09 44/44 tests on Windows/Ubuntu remain unchanged. Total current original source suite expected = 76 tests per OS; **do not assert this PASS without an exact-final-head workflow and job logs**.

Rollback: discard this disposable standalone tool checkout and own reports; no GitHub or Unity mutation. Source: `Tools/B01/B01-00/W09/RECEIPT-LATCH-001/**`. Reviewer B01-40 source/auth checks, B01-60 independent negative replay, B01-00 coordination. Neither `OFFERED`, `CODEX_ACK` nor `INTEGRATED` established.

# W11 — source-workflow spoof RED→GREEN / PASS17

## Why the source was changed

Original v1.1 `github_live.py` verified the producer PR exact SHA, plus a *successful Actions run* with the same SHA. An unrelated workflow (e.g. CI-01 lint) could pass on the same source while the required original Python tests were NOT_RUN. Source claims could therefore be overpromoted. This fix is restricted to the same B01-00-owned Draft #468 and does not touch canonical Unity files.

## RED — same original source, new concrete executable adversarial cases

- Test addition SHA `4347e14d22e4a321818c82fdf95a0ebb10b8e96c`.
- [Original GitHub Actions #41068708656](https://github.com/C8LyNn/RoboRougelike/actions/runs/41068708656) **FAIL**: Ubuntu 24.04 and Windows 2025 both **101 cases, 22 FAIL + 2 ERROR**. Old code accepted the wrong workflow ID/path/event and ignored absent or failed Windows/Ubuntu original test steps. RED was observed on published source, not only simulated on paper.
- Existing 76 W09/W10 tests retained and executed in same original test discover.

## GREEN — original 101 cases plus live GitHub API

- Source+test+workflow correction `d59f19ecb19a6e39c4af69f05a5fb9f8454dd4ba`.
- [GitHub Actions #41069154521](https://github.com/C8LyNn/RoboRougelike/actions/runs/41069154521) **SUCCESS**:
  - Windows 2025 job #124108361641: **101/101 PASS**, original read-only API script PASS.
  - Ubuntu 24.04 job #124108361104: **101/101 PASS**, original read-only API script PASS.
- New output `source_workflow_jobs_verified=true` + `source_gates_label=ORIGINAL_WORKFLOW_AND_JOBS_MATCH`. Still `decision=HOLD`, `independent_qa_verdict=MANUAL_REVIEW_REQUIRED`, `codex_ack_verdict=NOT_AUTHENTICATED`.

## Exact remote fixture — actual producer #427

Using publicly accessible GitHub REST original objects:
- [Producer PR #427](https://github.com/C8LyNn/RoboRougelike/pull/427), source SHA `3c4b576bb5d7cac5bb9ddc941da3dba0f6f2e07f`.
- [Actions run 41046087105](https://github.com/C8LyNn/RoboRougelike/actions/runs/41046087105) same source SHA; `workflow_id=401553591`, `path=.github/workflows/b01-00-dispatch-w06.yml`, event `pull_request`, run completed SUCCESS.
- [Real Jobs GET](https://api.github.com/repos/C8LyNn/RoboRougelike/actions/runs/41046087105/jobs): 2 jobs, `dispatch (windows-2025)` / `dispatch (ubuntu-24.04)`, each exact source SHA, completed SUCCESS, each with successful `Original source full dispatch and freeze regression suite` and `Produce deterministic example plan` steps.

This gate now refuses a same-SHA unrelated workflow, absent/in-progress/failed test jobs, missing required OS, missing/failed test steps, duplicate job names or paginated/incomplete job lists. It rejects unknown path parameters and follows no redirects. **Limitations:** step names and job status are GitHub-recorded metadata, not replayed test logs; only the externally verified workflow source and actual job logs can establish what code ran. A declared contract is not a signed independent QA decision. GitHub user login cannot prove a swarm role. No Unity test executed; no authorization to offer/merge/ACK/integrate.

**Receiver:** B01-40 independent exact-SHA/manifest/workflow provenance review; B01-60 independent negative replay and workflow bypass testing. **Current production status:** producer source executed; `G0_INDEPENDENT_PENDING / G1_NOT_RUN / G2_HOLD / CODEX_NO_ACK / UNMERGED`. Installation copies the isolated Python utility; rollback deletes only that unmerged tool and disposable output without altering offered/frozen gameplay or reviews.

# W12 original GitHub RED→GREEN — runner OS and workflow-attempt identity

The existing W11 gate could treat GitHub Actions jobs **named** `dispatch (windows-2025)` and `dispatch (ubuntu-24.04)` as distinct operating systems even if a job's **actual runner labels** referred to the opposite OS. It also failed to bind job objects to the exact run ID and attempt, making an unrelated/retried job a possible source-evidence spoof. This is a source-only advisory gate, not Unity or formal QA.

## Real RED
- Source test addition SHA: `108b1ebd14f2066ef7d1aed3dbcc990494a36619`.
- [GitHub Actions #41072839179](https://github.com/C8LyNn/RoboRougelike/actions/runs/41072839179), **FAIL** on both actual Windows2025 and Ubuntu24.04; each executed `Ran 123 tests`, **21 FAIL + 1 ERROR**.
- Added **22 real original Python unittest cases** covering mislabeled OS, both jobs running on one runner type, unbound run/attempt, duplicate job IDs, removed metadata, malformed runner contracts and positive controls.

## Real GREEN
- Fixed original `github_live.py` SHA update `c718a49f64ecb5c04293ea216add2fe6a2ccc3df`, updated historical and live fixtures to use runner labels, `run_id` and `run_attempt`.
- Actual original [GitHub Actions #41073097313](https://github.com/C8LyNn/RoboRougelike/actions/runs/41073097313) on source/fixture commit `259b1f5bb2a0a02852ec2787b513a29b1d1f9844` **COMPLETED SUCCESS**:
  - Windows2025 job #124122019627: **123/123 PASS**, real GitHub API PR427 workflow/jobs check PASS.
  - Ubuntu24.04 job #124122020004: **123/123 PASS**, real GitHub API PR427 workflow/jobs check PASS.
- Both real output receipts show `runner_identity_proof=RUNNER_LABELS_AND_ATTEMPT_MATCH`, `source_workflow_jobs_verified=true`, `source_git_objects_verified=true`, **but** `decision=HOLD` because B01-40/B01-60 independent QA is missing, `independent_qa_verdict=MANUAL_REVIEW_REQUIRED`, `codex_ack_verdict=NOT_AUTHENTICATED`.
- GitHub original PR427 [Actions #41046087105](https://github.com/C8LyNn/RoboRougelike/actions/runs/41046087105) jobs actually have `labels=["windows-2025"]` and `labels=["ubuntu-24.04"]`, each `run_id=41046087105`, `run_attempt=1`. The updated tool binds these fields to the declared two-OS contract and original workflow run.

## Gate notes
- This validates **reported GitHub-runner labels** and exact attempt metadata, not hardware or source-level authenticity of every line in the logs.
- A successful source workflow is NOT `G0_ACCEPTED`, `UNITY_G1_PASS`, `CODEX_ACK`, `INTEGRATED` or license availability. Unity EditMode/PlayMode remain NOT_RUN in repo CI when activation preflight fails.
- Receiver B01-40 must verify the exact SHA, workflow source and physical artefacts independently. B01-60 must independently attempt corrupted runner/attempt responses, source CI provenance and no forged acceptance. No game source, legacy reattach #510/#511/#494 or frozen handoff was mutated.
- Rollback: discard this isolated owner Python tool checkout and optional local report. It performs read-only GitHub API operations; no automated offer or merge.

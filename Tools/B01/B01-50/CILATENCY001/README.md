# B01-50 CILATENCY-001/R1 | PASS 17 / WAVE 20

This original Python 3.11 build operations analyzer measures actual observed GitHub Actions job scheduling and execution, and never infers game frame times, GPU, Unity native G1, Codex integration or game release. It is NOT a duplicate of source game profilers PERFVIEW/PERFRUN or physical Unity scene-cost audit SCENECOST.

The real evidence file evidence/github_runs_w15_w19.json was collected through the GitHub plugin from exact REST actions/runs/{id} and actions/runs/{id}/jobs responses for SIX specifically chosen historical B01-50 source test runs W15–W19. It includes one original failed test-fixture run and its later successful follow-up. Each run has original ID, exact code SHA, outcome, created_at, updated_at and attempt. Each job has its own original ID, started_at, completed_at, created_at, conclusion and runner. No credentials, repository secrets or owner location included.

Definitions:
- wait_until_start_seconds = job.started_at - job.created_at, including all delay before start. This is NOT necessarily due to GitHub congestion and cannot establish why time was spent.
- execution_seconds = job.completed_at - job.started_at. Includes checkout/dependency setup/testing/uploads/cleanup, not just compilation.
- created_to_last_update_seconds = run.updated_at - run.created_at. Can include post-processing, not identical to a sum of job durations.
- median and p95 use nearest rank, on only 12 job observations or 6 runs, so they are NOT representative SLOs or future predictions.

Run original Python implementation offline:
    python -m unittest discover -s Tools/B01/B01-50/CILATENCY001/tests -v
    python Tools/B01/B01-50/CILATENCY001/cilatency.py --input Tools/B01/B01-50/CILATENCY001/evidence/github_runs_w15_w19.json --output ./example-receipt.json

The CLI verifies exact schema/repo, run+job uniqueness, exact 40-char source SHAs, terminal states, valid UTC timestamps and chronological relationships, refuses same-file/output overwrites, input symlinks and oversized evidence. Output contains SHA-256 of original JSON input bytes and explicit NOT_ASSESSED native Unity, NOT_MEASURED FPS/GPU and NOT_OBSERVED integrator state. Original Git blob pins in MANIFEST-GIT-BLOBS.tsv also cover source/test/evidence/README/workflow. Dedicated Windows2025 and Ubuntu24.04 CI tests original source and regenerates the actual historical output receipt.

Producer B01-50. New-only owned paths Tools/B01/B01-50/CILATENCY001/** plus dedicated .github/workflows/b01-50-w20-cilatency.yml. No existing Assets, Unity save/ID, Codex, frozen #510/#556/#596/#642, competing R4 #647/#652, or earlier B01-50 immutable PRs modified. Rollback remove only explicitly owned new files on a disposable branch, never unrelated or empty foreign directories. No protected push, merge, browser native Unity run, secrets or game integration. Receivers B01-00 (queue/throughput experiment), B01-40 (source/G0), B01-60 (independent source QA). CODEX_ACK_NONE / G1_NATIVE_NOT_RUN / G2_HOLD. Integrator actual accept/rework/reject/blocked feedback and timing NOT_MEASURED.

No metric in this tool proves Unity/native success. A workflow conclusion=success is only a GitHub Actions result for that exact source workflow; it is never a game-native pass or an integrator ACK.

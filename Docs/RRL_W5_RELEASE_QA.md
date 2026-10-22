# W5 Release Candidate QA

Status: IN PROGRESS. No release-readiness claim yet.

Inputs verified on 2026-10-23: develop `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`; W2 PR1 `a0f0a6590b07ed2f5007361a7e761f133e52ff99`; W3/W4 PR19 `916bf6d195bb7647f159a4dc50a4d6b2ab18b063` (verified game/test source `5d4b61d1160e9cceac888e070147aead71dfb92b`). Both input PRs remain open Drafts, unmerged.

Release branch: `release/rrl-w5-candidate`. W2 and then PR19 were merged with history into merge commit `4eba68b3a79e749adeb7cfc7d6ecaf9d60cb440f`. No conflict occurred. PR17/18 were not imported separately. The initial Assets/Packages/ProjectSettings tree exactly matches PR19, so the prior 434/434 EditMode and 176/176 PlayMode results are inherited baseline evidence, not fresh W5 executions.

Pending: shutdown classification, complete six-stage native campaign/restart, performance observation, final full suites/build, exact requested ZIP/extraction/retail launch and Draft release PR.

# B01 native Unity CI: Draft producer vs integration candidate

Status: proposed; only effective after this branch is accepted and merged.
Scope: `.github/workflows/ci-02-editmode.yml` and `ci-03-playmode.yml`.

## Problem confirmed in actual GitHub Actions logs

GitHub Actions `CI-02 Unity EditMode` and `CI-03 Unity PlayMode` trigger on every Draft PR synchronisation even when the repository has no configured Unity activation credentials. The repository validator succeeds, then GameCI preflight fails with `Unity activation is missing. No tests or build executed.` This creates repeated failure notifications without executing Unity. Source correctness cannot be inferred from these red runs.

## Trigger contract after the proposed change

- `CI-01 Repository Validation`: unchanged, runs on pushes to main/develop and PR changes. This is **not** an engine validation.
- `CI-02/CI-03`: run automatically on `push` to main/develop and on the `pull_request.ready_for_review` event. They no longer run on every draft PR update.
- `CI-02/CI-03`: remain manually executable via `workflow_dispatch`. For additional commits **after** a PR has been marked ready, dispatch both from Actions selecting that candidate branch/ref. Confirm actual checked SHA from the workflow run.
- If a PR was already in Ready state before this change, perform a manual dispatch; adding this workflow does not create a new ready-for-review event.
- This change **does not** configure missing Unity credentials, create a runner or deliver native test results. No native PASS until a real engine executes and its XML/logs are checked.

## Fail-closed integration handoff

Do not treat absent checks, a skipped job or green CI-01 as native Unity PASS. Producer Draft = SOURCE_ONLY, NATIVE_UNITY_NOT_RUN. Integrator must **require real CI-02 and CI-03 pass for the exact selected candidate SHA**, or equivalent independently verified native logs and source SHA from an isolated, trusted local runner, before G1/integration authorization. If this cannot be proved, status `NATIVE_G1_HOLD` and `NO_MERGE`.

Review branch-protection required checks/rulesets before accepting trigger changes: if CI-02/CI-03 are required for *all* PRs, a Draft may remain pending; do not weaken release protections as a workaround.

## Configuration requiring repository owner

Inspect Settings -> Secrets and variables -> Actions to set up approved GameCI Unity credentials and license according to official GameCI documentation. Do not print or commit secrets. Alternatively configure a hardened self-hosted runner under an isolated Windows account with least privilege and no repository-write access to execute licensed Unity. GitHub-hosted runners do not inherit the owner's desktop Unity activation.

## Validation after merging

1. Update a Draft PR which targets main, and confirm CI-01 runs while CI-02/03 do **not** start from that commit.
2. Mark a disposable PR Ready for review, confirm both native workflows are invoked, report BLOCKED until licensed.
3. After licensing, run both on the exact chosen candidate SHA and confirm Unity NUnit XML, actual nonempty tests and logs.
4. Verify main/develop push remains covered, and that protected release gate rejects missing native evidence.

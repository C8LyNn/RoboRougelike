# CI41-NATIVE-CONTRACT-001 R1 — PASS17/WAVE10

Role/owner: **B01-41 CI/CD & TEST AUTOMATION**. Dispatch [#483](https://github.com/C8LyNn/RoboRougelike/issues/483), governance [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

## Implemented source gate
`native_ci_contract.py` reads **actual committed immutable Git objects** (not uncommitted working-tree YAML) for original CI-02 Unity EditMode, CI-03 Unity PlayMode and CI-04 Windows build. It verifies all 6 required ordered pipeline stages: pinned checkout with credentials disabled → repository validation → fail-closed Unity activation / complete secret bindings → actual pinned GameCI 6000.3.14f1 runner or builder → required nonempty original NUnit / native Windows executable check → always-run artifact evidence upload. It also checks branch trigger, minimal permissions, native test mode and path, prevents conditional-skipped execution and public activation secret echo. Unknown structure/changed critical contract fails closed with nonzero exit code and JSON blocker rather than falsely awarding native test PASS.

**Purpose:** catch workflow changes that silently remove a native test/build gate **before a Unity license is available**. This is distinct from [#482](https://github.com/C8LyNn/RoboRougelike/pull/482), which selects which test families to precheck after a source diff. Does not alter or invoke canonical workflows, Unity game assets or Codex work.

## Reproduce using real Git
```bash
python3 Tools/B01/CI41/P17W10/native_ci_contract.py --repo . --head-sha "$(git rev-parse HEAD)" --out contract-result.json
python3 -m unittest discover -s Tools/B01/CI41/P17W10 -p test_native_ci_contract.py -v
```
Test suite makes real original Git copies and commits; changes source triggers, activation, secrets, testMode, output check, pinning, runner skip and artifact upload; proves fail-closed plus positive evidence. Original canonical workflow bytes read from the checked-out current source SHA; source gate and CI evidence are **NOT** real Unity EditMode/PlayMode/Player runs.

## Ownership and acceptance
Paths: only `Tools/B01/CI41/P17W10/**` and `.github/workflows/b01-41-p17-w10-native-ci-contract.yml`, independent isolated Draft PR. Native tests: **NOT_RUN** when activation missing. Independent source G0: B01-40; independent negative QA: B01-60/61. B01-00 alone controls #79 Codex queue after independent acceptance. No CLAIMED/ACK/MERGE/INTEGRATED.

Rollback: do not merge Draft; remove only this independent isolated folder and its dedicated workflow if no longer needed. Never rewrite #482 or any Codex/main/develop source. Contract is currently intentionally strict and must be updated in a new version with tests for approved legitimate workflow architecture changes.

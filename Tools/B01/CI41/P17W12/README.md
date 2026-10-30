# CI41-ONE-CANDIDATE-LOCK-001/R1 — PASS17/WAVE12

Producer B01-41 (CI/CD & Test Automation). Dispatch [#518](https://github.com/C8LyNn/RoboRougelike/issues/518), immutable governance [#276](https://github.com/C8LyNn/RoboRougelike/issues/276).

## Real executable functionality (not another ZIP/physical QA receiver)
`one_candidate_lock.py` is a **single-source-admission lock for a proposed staging plan**. B01-00 selected exactly [#510@d94400d95d13db7f27e1533df05c9292d5380431](https://github.com/C8LyNn/RoboRougelike/pull/510), frozen descendant of [#488@33a32b8bd9ab40467ab1a17b5684926a37dbd4d3](https://github.com/C8LyNn/RoboRougelike/pull/488). This tool does not amend that decision and cannot change the source.

It validates an **exact JSON schema** and only permits exactly one import of #510 with the original frozen SHA. Any attempt to import **#511**, **#494**, old #488, multiple candidate entries, or set `native_unity_g1=PASS`, `independent_g0=ACCEPTED`, `codex_ack=true` or `intended_operation=integrate` FAILS CLOSED (exit 2). Actual original Git commit objects, SHA lineage (#488 → #510), exact original selected source C# atomic ledger / host / policy / PlayMode script and Unity `.meta` are read by Git rather than trusting a plan, and the source/target Git trees must exclude old W07/W08/W09 sources or conflicting V2 assets. A positive report records actual source Git blob SHA and fingerprint, and explicitly says `SELECTED_SOURCE_PREFLIGHT_PASS` **not** approved G0, G1, release or integration.

This is a different gate from W11 [#506](https://github.com/C8LyNn/RoboRougelike/pull/506), which stages competing W10 alternatives for comparison. **W12 policy forbids any alternate source adoption at all.** The #506 source stays frozen, untouched.

## Reproduce
```bash
git fetch origin +refs/pull/510/head:refs/remotes/origin/ci41-readonly-510 +refs/pull/488/head:refs/remotes/origin/ci41-readonly-488
python3 Tools/B01/CI41/P17W12/one_candidate_lock.py --repo . --plan Tools/B01/CI41/P17W12/selected-plan.json --out receipt.json
python3 -m unittest discover -s Tools/B01/CI41/P17W12 -p test_one_candidate_lock.py -v
```
`receipt.json` is source-only review evidence. The runner never writes `Assets/**`, does not install Unity scripts, and never runs licensed Editor/PhysX. Native G1 NOT_RUN.

## Ownership / rollback / receivers
Exactly files `Tools/B01/CI41/P17W12/**` and dedicated `.github/workflows/b01-41-p17w12-one-candidate.yml`. New isolated branch and Draft PR; base immutable `main@5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`. Rollback = close unmerged Draft or drop only W12-owned files and workflow; any generated receipt outside repo can be discarded. No deletion or mutation of selected #510, alternatives #511/#494, Codex or main/develop.

**Owner/receiver chain:** B01-23 owns frozen #510; B01-40 G0 exact 35-entry ZIP/GUID/asmdef/migration/rollback; B01-61/62 independent #486 regression; B01-60 actual licensed Unity PhysX G1; B01-00 exclusively handles #79 offer after proper approval. Selection lock never substitutes those gates. `CODEX_ACK=NO; INTEGRATED=NO; G2=HOLD`.

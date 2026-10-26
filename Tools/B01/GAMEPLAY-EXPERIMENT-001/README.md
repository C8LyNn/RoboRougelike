# GAMEPLAY-EXPERIMENT-001 / B01-10 — standalone quantitative design QA

**Authorized autonomous role:** B01-10 Game Design & Balance. Goal: make #250 gameplay differentiation **measurable** without asking Codex to build another dashboard or interfering with owned Unity source.

## Run (Node ≥20, zero dependencies)

```sh
node Tools/B01/GAMEPLAY-EXPERIMENT-001/generate.mjs
node Tools/B01/GAMEPLAY-EXPERIMENT-001/test.mjs
node Tools/B01/GAMEPLAY-EXPERIMENT-001/analyze.mjs Tools/B01/GAMEPLAY-EXPERIMENT-001/SYNTHETIC_FIXTURES.json
```

The generate step writes 900 *synthetic* event records into ignored/untracked worktree (CI generated fixture). The test step runs negative/oracle checks; the analyze step prints metrics and explicit `SYNTHETIC_ONLY_NO_PLAYTEST`. For actual captured records use `node analyze.mjs <external-real-data.json> --require-real`; `--require-real` **rejects any synthetic records** and refuses empty datasets, duplicates, >3 nonarmor retained and salvage armor. No real game evidence exists today.

The gate intentionally blocks incomplete 3-build×3-activity×100-seed data, and flags possible dominant builds/functional damage that does not measurably change handling. Cross-build comparisons are screening, not automatic changes to balancing values.

**Read** `GAMEPLAY_USP_PLAYTEST_PLAN.md` for experimental design, required independent Unity proof and human UX test, `event.schema.json` for telemetry shape and `EVIDENCE.json` for actual Node test results. Isolated `Tools/B01/GAMEPLAY-EXPERIMENT-001/**` with one own workflow, no `Assets/**`/Codex host modifications, no merge or G2.

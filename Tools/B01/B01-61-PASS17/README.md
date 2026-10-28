# B01-61 — PASS17 / INPUT-LATCH R1

**Package:** `B01-61-P17-INPUT-LATCH` v1.0.0. **Independent owner:** B01-61 regression QA. **Canonical gameplay owner:** Codex only. No gameplay edits, no integration, no duplicate prior garage→map or standalone Unity-only fixtures.

## New runnable deliverable

[`run_original_input_contract.py`](run_original_input_contract.py) reads **the exact immutable Git source** `Assets/Pack/P12_PipelineSafeTankController.cs` at Codex [`682dc0c5c13615577334375ffb6c27be5cdebf4a`](https://github.com/C8LyNn/RoboRougelike/commit/682dc0c5c13615577334375ffb6c27be5cdebf4a), verifies original Git blob `8beeedb4db40b7b33c8f685d7b6aab1247a4b2d7`, extracts **without editing either source method** `SetGameplayInputEnabled(bool active)` and `Update()`, and compiles/runs both actual C# methods under a temporary .NET 8 project with minimal simulated Unity dependencies.

Both Unity input compiler alternatives are exercised independently: `ENABLE_INPUT_SYSTEM` true and false. The test executes actual extracted statements across positive and negative input situations: LPM/PPM held across disabled/enabled state, independent button release/repress, movement input coexistence, disabled trigger clearing, and deterministic random 2400-frame state sequence **per input alternative**. A separate deliberately broken source snippet (generated *in memory only*, never committed) switches the both-buttons release latch to false; the harness must **reject** this regression for each C# compiler alternative. Refuses Git SHA/API changes rather than silently testing different game code.

This is genuinely executed **original gameplay C# input-method behavior** under an independent .NET isolation host, **not** native Unity, physical weapon damage or actual player validation. Counts are assertions on the original methods, not game test-suite passes.

## Reproduce from any checkout

Requirements: Python >=3.11, Git (fetch allowed), .NET SDK 8. No Unity license, nonstandard NuGet package, npm, Python dependency, or Windows desktop access required.

```bash
git fetch --no-tags origin 682dc0c5c13615577334375ffb6c27be5cdebf4a
python Tools/B01/B01-61-PASS17/run_original_input_contract.py --repo .
```

Exit `0` = both actual original methods/branches pass and both deliberately broken-latch variants are detected. Exit `2` = source missing/drift, compiler/tool failure, real observed counterexample or mutation not detected. Every test uses a fresh temporary directory; the tool does not touch the Unity `Assets/**` tree or `Application.persistentDataPath`.

Run CI: [`.github/workflows/b01-61-pass17-original-input.yml`](../../../.github/workflows/b01-61-pass17-original-input.yml). Two real independent Windows and Ubuntu .NET host runs upload raw per-run console logs, including exact original source blob, invoked commands, exit and case counts. Matrix success is proof only for this isolated harness.

## Integration / Codex handoff

**Option A (no game source changes):** Codex can copy the isolated `Tools/B01/B01-61-PASS17/**` folder to its own disposable checkout and run command above. The manifest pins the original source sha; if Codex gameplay changed, evaluate the diff first and **create a separate new package revision** rather than rewriting this immutable v1.

**Option B (genuine native operator, existing CODEX INBOX #79):** When an authorized licensed Unity 6000.3.14f1 test machine is available, translate only the scenarios into a suitable temporary Editor test assembly, press both real Unity Input System mouse buttons through pause and mission changes, inspect actual group trigger states and physical projectile/beam/saw effects, record true original NUnit XML, Editor.log, license/version, exit, and exact tested gameplay SHA. **Native status remains NOT_RUN until then**. No new gameplay patch, no claims of fixed real input bug, no duplicate existing CV item.

**Rollback:** delete only `Tools/B01/B01-61-PASS17/` and `.github/workflows/b01-61-pass17-original-input.yml`. No game source/history change. **B01-40:** verify immutable scope/manifest and CI artifacts; **B01-60:** independently reproduce the source-original executable harness; **B01-00:** decide whether to forward as supplementary test to existing CODEX INBOX #79. HQ G1/G2 and merging remain on hold.

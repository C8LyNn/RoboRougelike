# B01-60 PASS17 — R5-PLAN-ISOLATION-QA-001

**New executable independent QA for actual original R5 game code + ten B01-10 encounter options.** Owner B01-10's existing `validate_deck.cjs` mirrors the threat formula in JavaScript; its nine Unity tests were *authored but not executed*. Our new independent test compiles the **unchanged original Codex `R5RunPlan.cs`, `R1RunDefinition.cs` and `R3EnemyArchetype.cs`** using .NET 8, then exercises the original game implementation against the **exact producer B01-10 original JSON**. No code modified in `Assets/**` or in B01-10's `Research/**`.

## Actual execution

```sh
python Tools/B01/R5-PLAN-ISOLATION-QA-001/run_original.py --output ./r5-qa-original.json
```

Requires Git, Python 3.11+ and .NET SDK 8. Runs in an isolated OS temporary directory, never in Codex's active workspace. On Windows and Ubuntu, source-pinned Git fetch/show and Git-blob verification lock **all three original C# files and the original ten-option deck**. The test runner then launches a real `dotnet run -c Release` against all original .cs files and the minimal standalone `UnityJsonShim.cs` substitute for Unity math, ScriptableObject/attributes and field serialization. This **executes real C# run-planning**, not the B01-10 translated Node/JS formula. The shim is QA-only and **is not a real Unity engine, JsonUtility or PhysX**. The JSON receipt is attached to CI.

The executable independently validates 6 stages / 10 options, calls original C# `R5RunPlan.Validate`, creates 128 seeded plans and replays each seed from scratch, checks seeded offer equality, stage sizes, original C# `Threat` <= `ThreatBudget`, risk labels, reward-jitter boundaries and boss / nonboss extra-escort counts. It feeds four **in-memory** invalid catalogs to original C# `Validate`: duplicate id, impossible budget, incompatible elite archetype and invalid reward jitter; the method must reject all with `ArgumentException`. It checks that replacing a returned `Offers` array element does not replace the internal plan.

**NEW source isolation probe (not in producer's nine authored tests):** mutate a returned `Offers(stage)[0].ScrapReward` or `Roster[0]`, retrieve `Offers(stage)` again, and report if the plan's internally retained offer changed. The original `R5RunPlan.Offers` returns `stages[stage].ToArray()`, a shallow array copy, so the nested `R1EncounterDefinition` and `Roster` can still alias the plan. Any observed leak is **a reproducible original source-code risk**, not an exploit established in Unity/Player. The harness resets only its disposable in-memory objects; it never writes or patches the original game files.

## Acceptance and limits

- **PASS** if exact source blobs match, original code compiles, all seed/variant legality and invalid-catalog negatives execute, and JSON result is stored with original input SHAs.
- **ORIGINAL_SOURCE_RISK** if a caller can mutate internal state via a returned object; this may require a Codex change to avoid nondeterministic save/plan or reward corruption. **Do not call the entire feature integrated or GREEN** based on a successful test runner.
- **NOT_RUN**: actual Unity 6000.3.14f1 `JsonUtility` parity, real R1 6-stage run, live enemy AI, reward transactions, real Windows Player and independent Unity G1. Do not present .NET8 shim as a native Unity test.
- **OWNERS:** B01-60 only creates files in `Tools/B01/R5-PLAN-ISOLATION-QA-001/**` and its new workflow. Codex exclusively owns the game `Assets/R5/R5RunPlan.cs` and any repair. B01-10 exclusively owns the original R5 encounter deck.
- **Receiver:** B01-40 reviews original source and manifest; B01-00 prioritizes; Codex may reproduce in actual disposable native Unity test and decide whether to change production. No automatic CODEX_ACK, merge or G2.
- **Rollback:** delete/close this isolated QA PR only. No original C# source, `Assets/Resources/R5/Encounters.json` or `.meta` is touched.

**Input SHA:** Codex `682dc0c5c13615577334375ffb6c27be5cdebf4a`; B01-10 `c44f98bb5328848f1d9fc15ed19d6facf3cbee39`. Exact Git blob IDs are in `manifest.json`.

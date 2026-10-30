# B01-22 / KITREADINESS-001 R1 — Virtual Kit Stock Readiness

**Scope**: independently owned read-only Unity-ready C#9 tool, no garage, wallet,
shop, save, damage, run, rewards or authoritative stock writes. Frozen catalog
source [#411](https://github.com/C8LyNn/RoboRougelike/pull/411)
at `b1d4e341d846c9397e236809a1d7e4c4fff9c935` (Codex gameplay #383
`203a0805251ed28293888e43f2d49f0df63767c6`). Stacked PR against #411
for source reuse only; parent must not be unintentionally merged into game.

## Real functionality
`KitReadinessPlanner.Preview(sourceSha, exact10Catalog, stockCounts, orderedRequests)`
returns up to 100 immutable-preview decisions for real EK01–EK10 physical kits.
Every result reports which physical kinds/weapon families are missing, required
vs available counts, and **reference** price for missing pieces. Successful
requests reserve parts in a private in-memory ledger. Failed requests reserve
**nothing** (atomic), so later eligible requests can still succeed. Duplicate
ordered requests are allowed; the first uses stock, later may be blocked.
Changing request order can change the attainable assortment, intentionally.
The function does not mutate caller stock and has no UnityEngine dependency.
It does not decide spending and should NOT be bound to a live wallet or save.

## Data/reproduce
Three real authored scenarios in `scenarios/`: starter-cache (first EK01 ready
and second blocked), siege-cache (EK08 ready, EK02 blocked), shared-gyro
(EK01 ready, EK08 blocked, EK02 ready). The original C# source test executable
loads real frozen #411 catalog, computes real reference unit prices and emits
three deterministic JSON `*-preview.json` receipts. No fake baked outcome.
All source C# compiled under .NET8 in GitHub Actions (C#9 compatibility).
These source tests are NOT native Unity Editor/PlayMode/Player tests.

```sh
dotnet run --project Research/B01-22/P17/KITREADINESS001/KitReadinessTests.csproj --configuration Release -- --out readiness-previews
python -m unittest discover -s Research/B01-22/P17/KITREADINESS001 -p test_pack_readiness.py -v
python Research/B01-22/P17/KITREADINESS001/pack_readiness.py --stage readiness-artifact --previews readiness-previews
python Research/B01-22/P17/KITREADINESS001/pack_readiness.py --rollback readiness-artifact
```

Physical GitHub Actions artifact includes original importable C# and two stable
Unity GUID .meta files in `kitreadiness-unity.zip`, plus the THREE real scenario
outputs and SHA-256 receipt, reproduced on Windows2025 and Ubuntu24.04.

## Import/rollback contract
B01-40 must independently verify 2 unique GUIDs and no collision with actual
target before importing only `Assets/B01KitReadiness001.meta` and the
`Assets/B01KitReadiness001/**` files in a disposable licensed Unity project.
NO automatic import, no gameplay binding and no Codex code changes.
To roll back source copy, delete only these isolated owned files after verifying
destination absent before install. For source zip stage, `--rollback` refuses
foreign or modified files; no blind deletes. To remove this Git-only research,
close its Draft PR and discard its own new branch; never merge to main/develop.
Native Unity G1 and independent B01-40/B01-60 QA remain NOT_RUN/PENDING
until actual execution. B01-00 alone decides #79 OFFERED; Codex ACK not assumed.

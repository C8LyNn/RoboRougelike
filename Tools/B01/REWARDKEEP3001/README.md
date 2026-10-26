# REWARDKEEP3001 / B01-22 — one-phase keep up to 3 after battle (source-only)

**Owner gameplay requirement:** after a *verified winning encounter*, one bounded recovery choice: keep **0–3 physical items other than Armor**, never convert it to unlimited auto-loot. Domain evidence #250, previously approved B01 design discussions. This release is an independently compiled **portable policy package**, not live game integration or visual UI.

### Code
Unity 6 C# `Assets/B01Packages/REWARDKEEP3001/Runtime/RewardKeep3Service.cs`, isolated runtime + authored EditMode NUnit asmdefs, `.meta` canonical GUID. `IAuthoritativeRewardPort` is **one-way authority** over the existing R3 owned Parts + Inventory plus other saved robots. Reward offers held temporarily for UI decisions are NOT owned physical parts; commit delegates **one atomic** batch to existing R3 salvage mechanism via trusted host hooks. No secondary stash, no free money/repair, no automatic bonus drops.

- `TryOpen` demands genuine victory proof from authoritative completed encounter and rejects repeated event, invalid/duplicate/colliding physical instance IDs, corrupt offers. Can present Armor in UI but **never select** it; Core also rejected.
- `TryToggleSelection` supports deselect+reselect but strictly caps selected count at **3** across all kinds; allows several distinct physical IDs of the same legendary type in INVENTORY, does not bypass one-per-type EQUIPPED cap in separate LEGENDARY #242.
- `TryCommit` sends only selected real copies once, includes event key in same atomic R3 host transaction. Failed host operation cannot lose selected options or grant a partial reward (host must enforce). Broken postcondition quarantines service. Empty selection commits only victory event.
- `TrySave/TryRestore` sidecar protects active choice snapshot, selected IDs, and event ledger. Host physical ownership + event store + sidecar must share ONE crash-consistent save transaction. Exact original prefab/stats/Health/UpgradeId/Weapon are HOST-owned; this standalone does not fabricate R3PartState.
- Extra game mechanics not yet approved: total offer count, salvage chance, source rarity, conditions for drop after defeat, sell/repair conversion, more than 3 due to modules (#137), persistence across abandoned runs, UI layout. Do not add automatically.

### Actual tests and limits
`dotnet build Tools/B01/REWARDKEEP3001/tests/RewardKeep3.SurrogateTests.csproj -c Release` and `dotnet run --project Tools/B01/REWARDKEEP3001/tests/RewardKeep3.SurrogateTests.csproj -c Release --no-build -- artifacts/reward-keep3/results.json`.
Test executable compiles actual Unity C# source with explicitly declared `UnityEngine.JsonUtility` .NET8 surrogate; includes named positive/negative and 3000 seeded victory cycles. Licensed native Unity EditMode/PlayMode/Windows **NOT_RUN** until genuine runner.

GitHub Actions produces a deterministic Unity ZIP+GUID validation, source SHA256 manifest, test JSON, CI report. No material effects, Unity scene or game UI implemented here.

**Integration example** `Tools/B01/REWARDKEEP3001/R3RewardPort.install.cs` is NOT in Unity source ZIP and **fail-closed** unless Codex supplies trusted victory proof, complete ownership index and original physical R3 salvage transaction. `INSTALL_FOR_CODEX.md` covers rest. B01-40 independent G0, B01-60 native QA, B01-00 G2 HOLD; **no merge**.
